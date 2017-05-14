//  Copyright 2004-2012 Castle Project - http://www.castleproject.org/
//  Hamilton Verissimo de Oliveira and individual contributors as indicated. 
//  See the committers.txt/contributors.txt in the distribution for a 
//  full listing of individual contributors.
// 
//  This is free software; you can redistribute it and/or modify it
//  under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 3 of
//  the License, or (at your option) any later version.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this software; if not, write to the Free
//  Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
//  02110-1301 USA, or see the FSF site: http://www.fsf.org.

namespace Castle.MonoRail.Extension.OData

open System
open System.Collections
open System.Collections.Generic
open System.Linq
open System.Xml
open System.IO
open System.Text
open System.ServiceModel.Syndication
open System.Data.OData
open System.Data.Services.Providers
open System.Data.Services.Common
open Newtonsoft.Json


module JSonSerialization = 
    begin
        let private set_up (writer:JsonTextWriter) = 
            writer.DateFormatHandling <- DateFormatHandling.IsoDateFormat
            writer.Formatting <- Formatting.Indented
            writer.IndentChar <- '\t'
            writer.Indentation <- 1

        let private write_meta (writer:JsonTextWriter) (uri:Uri) (rt:ResourceType) = 
            writer.WritePropertyName "__metadata"
            writer.WriteStartObject()
            if uri <> null then
                writer.WritePropertyName "uri"
                writer.WriteValue uri.AbsoluteUri
            writer.WritePropertyName "type"
            writer.WriteValue rt.FullName
            writer.WriteEndObject()


        let rec private write_primitive_and_complex_properties (writer:JsonTextWriter) (instance) (uri:Uri) (rt:ResourceType) = 
            for prop in rt.Properties do
                let otherRt = prop.ResourceType
                
                if prop.IsOfKind ResourcePropertyKind.ComplexType then
                    // todo: add case for collection of complex types

                    writer.WritePropertyName prop.Name

                    // TODO: is collection?
                    // if prop.ResourceType.InstanceType.IsColl then 

                    let innerinstance = prop.GetValue(instance)

                    write_complextype writer innerinstance uri otherRt

                elif prop.IsOfKind ResourcePropertyKind.Primitive then
                    
                    writer.WritePropertyName prop.Name

                    let originalVal = (prop.GetValue(instance))
                    
                    writer.WriteValue originalVal


        and private write_ref_properties (writer:JsonTextWriter) (instance) (uri:Uri) (rt:ResourceType) = 
            for prop in rt.Properties do
                let otherRt = prop.ResourceType

                if prop.IsOfKind ResourcePropertyKind.ResourceReference || prop.IsOfKind ResourcePropertyKind.ResourceSetReference then
                    writer.WritePropertyName prop.Name
                    writer.WriteStartObject ()

                    writer.WritePropertyName "__deferred"
                    writer.WriteStartObject ()
                    writer.WritePropertyName "uri"
                    writer.WriteValue (uri.AbsoluteUri + "/" + prop.Name)
                    writer.WriteEndObject ()

                    writer.WriteEndObject ()

        and private write_complextype (writer:JsonTextWriter) (instance) (uri:Uri) (rt:ResourceType) = 
        
            if instance = null then 
                writer.WriteNull()
            else
                writer.WriteStartObject()
                write_meta writer null rt
                write_primitive_and_complex_properties writer instance uri rt
                writer.WriteEndObject()


        let private write_js_item (writer:JsonTextWriter) (wrapper:DataServiceMetadataProviderWrapper) (instance) (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) appendKey = 
            writer.WriteStartObject()

            let resourceSet = wrapper.ResourceSets |> Seq.tryFind (fun rs -> rs.ResourceType = rt)
            let resourceUri = 
                match resourceSet with 
                | Some rs -> 
                    // for this case, we always want to append the key
                    Uri(svcBaseUri, rs.Name + rt.GetKey(instance))
                | _ -> 
                    System.Diagnostics.Debug.Assert (containerUri <> null)
                    if appendKey 
                    then Uri(containerUri.AbsoluteUri + rt.GetKey(instance))
                    else containerUri
            
            write_meta writer resourceUri rt
            write_primitive_and_complex_properties writer instance resourceUri rt 
            write_ref_properties writer instance resourceUri rt 

            writer.WriteEndObject()

        let internal write_items (wrapper:DataServiceMetadataProviderWrapper) (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) 
                                 (items:IEnumerable) (writer:TextWriter) (enc:Encoding) = 
            use jsonWriter = new JsonTextWriter(writer)
            set_up jsonWriter
            jsonWriter.WriteStartObject()

            jsonWriter.WritePropertyName "d"
            jsonWriter.WriteStartArray()

            for item in items do
                write_js_item jsonWriter wrapper item svcBaseUri containerUri rt true

            jsonWriter.WriteEndArray()

            jsonWriter.WriteEndObject()
        

        let internal write_item (wrapper:DataServiceMetadataProviderWrapper) (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) 
                                (item:obj) (writer:TextWriter) (enc:Encoding) = 

            use jsonWriter = new JsonTextWriter(writer)
            set_up jsonWriter

            jsonWriter.WriteStartObject()

            jsonWriter.WritePropertyName "d"

            write_js_item jsonWriter wrapper item svcBaseUri containerUri rt false

            jsonWriter.WriteEndObject()

        let internal write_property (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) 
                                    (prop:ResourceProperty)
                                    (instance:obj) (writer:TextWriter) (enc:Encoding) =
            use jsonWriter = new JsonTextWriter(writer)
            set_up jsonWriter
            jsonWriter.WriteStartObject()

            jsonWriter.WritePropertyName "d"
            jsonWriter.WriteStartObject()

            jsonWriter.WritePropertyName prop.Name
            jsonWriter.WriteValue instance

            jsonWriter.WriteEndObject() // d
            jsonWriter.WriteEndObject()
            
        let internal read_item (rt:ResourceType) (reader:TextReader) (enc:Encoding) = 
            
            use jsonReader = new JsonTextReader(reader)
            let instance = Activator.CreateInstance rt.InstanceType

            // { "d": { Prop: a, Prop2: 2 } }
            // { Prop: a, Prop2: 2 }

            let getToPropertyStart () = 
                let doContinue = ref true
                while !doContinue && jsonReader.Read() do
                    if jsonReader.TokenType = JsonToken.PropertyName && jsonReader.Value.ToString() <> "d" then
                        doContinue := false
                
            getToPropertyStart()

            let doContinue = ref true
            while !doContinue do
                if jsonReader.TokenType = JsonToken.PropertyName then 
                    match rt.Properties |> Seq.tryFind (fun p -> p.Name = jsonReader.Value.ToString()) with
                    | Some prop -> 
                        jsonReader.Read() |> ignore
                        // todo: assert is not comment or property name

                        let value = jsonReader.Value

                        if prop.IsOfKind (ResourcePropertyKind.Primitive) then 
                            
                            let sanitizedVal = Convert.ChangeType(value, prop.ResourceType.InstanceType)

                            prop.SetValue(instance, sanitizedVal)

                        elif prop.IsOfKind (ResourcePropertyKind.ComplexType) then 
                            
                            ()
                        else 
                            ()

                        

                        doContinue := jsonReader.Read()

                    | _ ->  
                        // could not find property: should this be an error?
                        doContinue := false

                else
                    doContinue := jsonReader.Read()

            instance


        let CreateDeserializer () = 
            { new Deserializer() with 
                override x.DeserializeMany (rt, reader, enc) = 
                    raise(NotImplementedException())
                override x.DeserializeSingle (rt, reader, enc) = 
                    read_item rt reader enc
            }

        let CreateSerializer () = 
            { new Serializer() with 
                override x.SerializeMany(wrapper, svcBaseUri, containerUri , rt, items, writer, enc) = 
                    write_items wrapper svcBaseUri containerUri rt items writer enc
                override x.SerializeSingle(wrapper, svcBaseUri, containerUri, rt, item, writer, enc) = 
                    write_item wrapper svcBaseUri containerUri rt item writer enc 
                override x.SerializePrimitive(wrapper, svcBaseUri, containerUri, rt, prop, item, writer, enc) = 
                    write_property svcBaseUri containerUri rt prop item writer enc
            }


    end
