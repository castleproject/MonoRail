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

namespace Castle.MonoRail.Extension.OData.Serialization

open System
open System.Collections
open System.Collections.Specialized
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


        type JsonSerializer(wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc, useSimplerFormat:bool) as self = 
            class 
                inherit Serializer(wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc) 

                let jwriter = new JsonTextWriter(writer)
                
                do
                    set_up jwriter

                let write_meta (uri:Uri) (rt:ResourceType) = 
                    if not useSimplerFormat then
                        jwriter.WritePropertyName "__metadata"
                        jwriter.WriteStartObject()
                        if uri <> null then
                            jwriter.WritePropertyName "uri"
                            jwriter.WriteValue uri.AbsoluteUri
                        jwriter.WritePropertyName "type"
                        jwriter.WriteValue rt.FullName
                        jwriter.WriteEndObject()

                let rec write_primitive_and_complex_properties (instance) (uri:Uri) (rt:ResourceType) = 
                    
                    for prop in rt.Properties do
                        let otherRt = prop.ResourceType
                
                        if prop.IsOfKind ResourcePropertyKind.ComplexType then
                            // todo: add case for collection of complex types

                            jwriter.WritePropertyName prop.Name

                            match InternalUtils.getEnumerableElementType prop.ResourceType.InstanceType with
                            | Some elementType -> 
                                let innerItems = prop.GetValue(instance) :?> IEnumerable
                                // start Properties
                                if innerItems <> null then
                                        write_set (Uri(uri.AbsoluteUri + "/" + prop.Name)) prop.ResourceType innerItems true 
                                    else
                                        jwriter.WriteStartArray ()
                                        jwriter.WriteEndArray ()

                            | _ -> 
                                let innerinstance = prop.GetValue(instance)
                                write_complextype innerinstance uri otherRt

                        elif prop.IsOfKind ResourcePropertyKind.Primitive then
                    
                            jwriter.WritePropertyName prop.Name

                            let originalVal = prop.GetValue(instance)

                            if originalVal <> null then
                                let valType = originalVal.GetType()
                                // special support for dictionaries (not on OData spec!)
                                if valType.IsGenericType && valType.GetGenericTypeDefinition() == typedefof<Dictionary<_,_>> then
                                    let items = originalVal :?> IEnumerable
                                    write_dictionary (Uri(uri.AbsoluteUri + "/" + prop.Name)) prop.ResourceType items true 
                                else
                                    jwriter.WriteValue originalVal
                            else
                                jwriter.WriteValue originalVal
                            

                and write_ref_properties (instance) (uri:Uri) (rt:ResourceType) = 
            
                    for prop in rt.Properties do
                        let otherRt = prop.ResourceType

                        if prop.IsOfKind ResourcePropertyKind.ResourceReference || prop.IsOfKind ResourcePropertyKind.ResourceSetReference then
                            jwriter.WritePropertyName prop.Name
                            if not useSimplerFormat then jwriter.WriteStartObject ()

                            // spec wise, we need to output additional metadata in the end (after the properties) 
                            // to reference the associations used for the expanded properties, but I'm skipping that for now

                            if self.ShouldExpand (prop) then
                                if prop.IsOfKind ResourcePropertyKind.ResourceSetReference then 
                                    if not useSimplerFormat then jwriter.WritePropertyName "results" 

                                    let innerItems = prop.GetValue(instance) :?> IEnumerable
                                    if innerItems <> null then
                                        write_set (Uri(uri.AbsoluteUri + "/" + prop.Name)) prop.ResourceType innerItems true 
                                    else
                                        jwriter.WriteStartArray ()
                                        jwriter.WriteEndArray ()
                                else
                                    if not useSimplerFormat then jwriter.WritePropertyName "result" 

                                    let inner = prop.GetValue(instance) 
                                    if inner <> null then
                                        write_js_item inner (Uri(uri.AbsoluteUri + "/" + prop.Name)) prop.ResourceType true 
                                    else
                                        jwriter.WriteNull()

                            else
                                if not useSimplerFormat then 
                                    jwriter.WritePropertyName "__deferred"
                                    jwriter.WriteStartObject ()
                                    jwriter.WritePropertyName "uri"
                                    jwriter.WriteValue (uri.AbsoluteUri + "/" + prop.Name)
                                    jwriter.WriteEndObject ()
                                else
                                    if prop.IsOfKind ResourcePropertyKind.ResourceSetReference then 
                                        jwriter.WriteStartArray ()
                                        jwriter.WriteEndArray ()
                                    else
                                        jwriter.WriteStartObject ()
                                        jwriter.WriteEndObject ()
                                

                            if not useSimplerFormat then jwriter.WriteEndObject ()


                and write_complextype (instance) (uri:Uri) (rt:ResourceType) = 
        
                    if instance = null then 
                        jwriter.WriteNull()
                    else
                        jwriter.WriteStartObject()
                        write_meta null rt
                        write_primitive_and_complex_properties instance uri rt
                        jwriter.WriteEndObject()


                and write_js_item (instance) (containerUri:Uri) (rt:ResourceType) appendKey = 
                    jwriter.WriteStartObject()

                    let resourceSet = wrapper.ResourceSets |> Seq.tryFind (fun rs -> rs.ResourceType = rt)
                    let resourceUri = 
                        match resourceSet with 
                        | Some rs -> 
                            // for this case, we always want to append the key
                            Uri(serviceBaseUri, rs.Name + rt.GetKey(instance))
                        | _ -> 
                            System.Diagnostics.Debug.Assert (containerUri <> null)
                            if appendKey 
                            then Uri(containerUri.AbsoluteUri + rt.GetKey(instance))
                            else containerUri
            
                    write_meta resourceUri rt
                    write_primitive_and_complex_properties instance resourceUri rt 
                    write_ref_properties instance resourceUri rt 

                    jwriter.WriteEndObject()

                and write_dictionary (containerUri:Uri) (rt:ResourceType) (items:IEnumerable) appendKey = 
                    
                    jwriter.WriteStartObject()

                    for item in items do
                        jwriter.WritePropertyName (item?Key)
                        jwriter.WriteValue ((item?Value).ToString())

                    jwriter.WriteEndObject()

                and write_set (containerUri:Uri) (rt:ResourceType) (items:IEnumerable) appendKey = 
            
                    jwriter.WriteStartArray()

                    for item in items do
                        write_js_item item containerUri rt appendKey 

                    jwriter.WriteEndArray()

                let wrap_in_d (f) = 
                    if not useSimplerFormat then 
                        jwriter.WriteStartObject()
                        jwriter.WritePropertyName "d"

                    f()
            
                    if not useSimplerFormat then 
                        jwriter.WriteEndObject()

                override x.SerializeMany(items) =
                    wrap_in_d (fun _ -> write_set containerUri rt items true )

                override x.SerializeSingle(item) =
                    wrap_in_d (fun _ -> write_js_item item containerUri rt false  )
                
                override x.SerializeProperty(prop:ResourceProperty, value) =
                    
                    let write_d () = 
                        jwriter.WriteStartObject()
                        jwriter.WritePropertyName prop.Name
                        jwriter.WriteValue value
                        jwriter.WriteEndObject() 

                    wrap_in_d (fun _ -> write_d ()  )

            end
        
        let internal read_item (rt:ResourceType) target (reader:TextReader) (enc:Encoding) = 
            
            use jsonReader = new JsonTextReader(reader)
            let instance = 
                if target = null 
                then Activator.CreateInstance rt.InstanceType
                else target

            // the two formats we support
            // odata verbose json:
            // { "d": { Prop: a, Prop2: 2 } }
            // standard json:
            // { Prop: a, Prop2: 2 }

            let getToPropertyStart () = 
                let doContinue = ref (jsonReader.TokenType <> JsonToken.PropertyName)
                while !doContinue && jsonReader.Read() do
                    if jsonReader.TokenType = JsonToken.PropertyName && jsonReader.Value.ToString() <> "d" then
                        doContinue := false
                

            let rec rec_read_object (instance) (rt:ResourceType) = 
                
                getToPropertyStart()
                
                let doContinue = ref true
                while !doContinue do
                    if jsonReader.TokenType = JsonToken.PropertyName then 
                        
                        match rt.Properties |> Seq.tryFind (fun p -> p.Name = jsonReader.Value.ToString()) with
                        | Some prop -> 

                            if prop.IsOfKind (ResourcePropertyKind.Primitive) then 
                                jsonReader.Read() |> ignore
                                let value = jsonReader.Value
                                if value <> null then
                                    let nullableType = Nullable.GetUnderlyingType(prop.ResourceType.InstanceType)
                                    let t = if nullableType == null then prop.ResourceType.InstanceType else nullableType
                                    let sanitizedVal = Convert.ChangeType(value, t)
                                    prop.SetValue(instance, sanitizedVal)
                                else
                                    prop.SetValue(instance,  null)
                                
                            elif prop.IsOfKind (ResourcePropertyKind.ComplexType) || 
                                 prop.IsOfKind (ResourcePropertyKind.ResourceReference) then 
                                
                                // for complex types, we need to check if it's a collection
                                // use getEnumerableElementType 

                                doContinue := jsonReader.Read()
                                if !doContinue = true then
                                    if jsonReader.TokenType = JsonToken.Null then
                                        prop.SetValue(instance, null)
                                        
                                    elif jsonReader.TokenType = JsonToken.StartObject then
                                        let inner = 
                                            let existinval = prop.GetValue(instance)
                                            if existinval = null then
                                                let newVal = Activator.CreateInstance prop.ResourceType.InstanceType
                                                newVal
                                            else existinval

                                        rec_read_object inner prop.ResourceType
                                        prop.SetValue(instance, inner)

                                    else 
                                        failwithf "Unexpected json node type %O" jsonReader.TokenType

                                ()
                        
                            elif prop.IsOfKind (ResourcePropertyKind.ResourceSetReference) then 
                                let list = prop.GetValue(instance)
                                if list = null then 
                                    failwithf "Null collection property. Please set a default value for property %s on type %s" prop.Name rt.InstanceType.FullName
                                
                                // empty the collection, since this is expected to be a HTTP PUT
                                list?Clear() |> ignore

                                doContinue := jsonReader.Read()
                                if !doContinue = true then
                                    if jsonReader.TokenType = JsonToken.Null then
                                        // nothing to do, since it was cleared already
                                        ()
                                        
                                    elif jsonReader.TokenType = JsonToken.StartArray then

                                        while (jsonReader.Read() && jsonReader.TokenType = JsonToken.StartObject) do
                                            let inner = Activator.CreateInstance prop.ResourceType.InstanceType
                                            rec_read_object inner prop.ResourceType
                                            list?Add(inner) |> ignore
                                    else 
                                        failwithf "Unexpected json node type %O" jsonReader.TokenType

                                prop.SetValue(instance, list)
                        
                            else 
                                failwithf "Unsupported property kind. Expecting Primitive, or ComplexType or ResourceRef/Set"

                            doContinue := jsonReader.Read()

                        | _ -> failwithf "Property not found on model %s: %O" rt.Name jsonReader.Value 

                    else
                        doContinue := jsonReader.TokenType <> JsonToken.EndObject && jsonReader.Read()

            rec_read_object instance rt 

            instance


        let DeserializerInstance = 
            { 
              new Deserializer() with 
                override x.DeserializeMany (rt, reader, enc) = 
                    raise(NotImplementedException())
                override x.DeserializeSingle (rt, reader, enc, target) = 
                    read_item rt target reader enc
            }

    end
