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

module AtomSerialization = 
    begin
        let private emptyPerson = SyndicationPerson(null, String.Empty, null)
        let private qualifiedNullAttribute = XmlQualifiedName("null", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")
        let private qualifiedDataWebPrefix = XmlQualifiedName("d", "http://www.w3.org/2000/xmlns/")
        let private qualifiedDataWebMetadataPrefix = XmlQualifiedName("m", "http://www.w3.org/2000/xmlns/")
        let private linkRelResource = Uri("http://schemas.microsoft.com/ado/2007/08/dataservices/related/")
        let private categoryScheme = "http://schemas.microsoft.com/ado/2007/08/dataservices/scheme"

        type ContentDict (items:(string*string*obj) seq) = 
            inherit SyndicationContent()
            let _items = List(items)

            let rec write_primitive_prop (writer:XmlWriter) name typename (value:obj) = 
                writer.WriteStartElement (name, "http://schemas.microsoft.com/ado/2007/08/dataservices")
                
                if typename <> "Edm.String" then 
                    writer.WriteAttributeString ("type", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata", typename)

                if value = null then 
                    writer.WriteAttributeString("null", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata", "true")
                elif value :? ContentDict then
                    let inner = value :?> ContentDict
                    inner.InternalWrite (writer, name)
                else 
                    writer.WriteString(value.ToString())
                
                writer.WriteEndElement()

            new () = ContentDict(Seq.empty)

            member x.Add(name, typename, value) = 
                _items.Add( (name, typename, value) )

            member internal x.InternalWrite (writer, name) = 
                _items |> Seq.iter (fun (name,typename,value) -> write_primitive_prop writer name typename value) 

            override x.Type = "application/xml"
            override x.Clone() = upcast ContentDict(_items)
            override x.WriteContentsTo (writer) = 
                if _items.Count > 0 then 
                    writer.WriteStartElement("properties", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")
                    _items |> Seq.iter (fun (name,typename,value) -> write_primitive_prop writer name typename value) 
                    writer.WriteEndElement()

        let private get_string_value (reader:XmlReader) = 
            let doContinue = ref true
            let buffer = StringBuilder()
            while !doContinue && reader.Read() do
                match reader.NodeType with 
                | XmlNodeType.SignificantWhitespace | XmlNodeType.CDATA | XmlNodeType.Text ->
                    buffer.Append reader.Value |> ignore
                | XmlNodeType.Comment | XmlNodeType.Whitespace -> ()
                | XmlNodeType.EndElement -> doContinue := false
                | _ -> failwithf "Unexpected token parsing element value"
            buffer.ToString()

        let rec private build_content_from_properties (relResUri:Uri) instance (rt:ResourceType) (item:SyndicationItem) = 
            let content = ContentDict()
            let skipContent = ref false

            for prop in rt.Properties do
                let otherRt = prop.ResourceType
                skipContent := false
                
                let atts = rt.OwnEpmAttributes |> Seq.filter (fun epm -> epm.SourcePath = prop.Name)

                for att in atts do
                    skipContent := not att.KeepInContent
                    match att.TargetSyndicationItem with
                    // todo, lots of extra cases
                    | SyndicationItemProperty.Title -> 
                        item.Title <- TextSyndicationContent (prop.GetValueAsStr(instance))
                    | _ -> ()

                if prop.IsOfKind ResourcePropertyKind.ResourceReference then
                    // <link rel="http://schemas.microsoft.com/ado/2007/08/dataservices/related/Supplier" type="application/atom+xml;type=entry" title="Supplier" href="Products(0)/Supplier" />
                    item.Links.Add (SyndicationLink(Uri(relResUri.OriginalString + "/" + prop.Name, UriKind.Relative), 
                                                    linkRelResource.AbsoluteUri + otherRt.Name, 
                                                    otherRt.Name, 
                                                    "application/atom+xml;type=entry", 0L))
                    
                
                elif prop.IsOfKind ResourcePropertyKind.ResourceSetReference then
                    // <link rel="http://schemas.microsoft.com/ado/2007/08/dataservices/related/Products" type="application/atom+xml;type=feed" title="Products" href="Categories(2)/Products" />
                    item.Links.Add (SyndicationLink(Uri(relResUri.OriginalString + "/" + prop.Name, UriKind.Relative), 
                                                    linkRelResource.AbsoluteUri + otherRt.Name, 
                                                    otherRt.Name, 
                                                    "application/atom+xml;type=feed", 0L))


                elif prop.IsOfKind ResourcePropertyKind.ComplexType then
                    // <d:Address m:type="[namespace].Address"> ... </d:Address>

                    // todo: add case for collection of complex types

                    match InternalUtils.getEnumerableElementType prop.ResourceType.InstanceType with
                    | Some elementType -> 
                        let innerContent = ContentDict()
                        let elements = prop.GetValue(instance) :?> IEnumerable
                        // start Properties
                        for element in elements do
                            let contentElement = build_content_from_properties relResUri element otherRt item
                            innerContent.Add ("element", prop.ResourceType.FullName, contentElement)

                        // end Properties
                        content.Add (prop.Name, prop.ResourceType.FullName, innerContent)

                    | _ -> 
                        let innerinstance = prop.GetValue(instance)
                        let inner = build_content_from_properties relResUri innerinstance otherRt item
                        content.Add (prop.Name, prop.ResourceType.FullName, inner)

                elif prop.IsOfKind ResourcePropertyKind.Primitive && not !skipContent then
                    let originalVal = (prop.GetValue(instance))
                    let strVal = XmlSerialization.to_xml_string prop.ResourceType.InstanceType originalVal 
                    content.Add (prop.Name, prop.ResourceType.FullName, strVal)
            
            content

        let internal build_item (wrapper:DataServiceMetadataProviderWrapper) (instance) (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) addNs appendKey = 
            let item = SyndicationItem()
            let resourceSet = wrapper.ResourceSets |> Seq.tryFind (fun rs -> rs.ResourceType = rt)

            if addNs then
                item.BaseUri <- svcBaseUri
                item.AttributeExtensions.Add (qualifiedDataWebPrefix, "http://schemas.microsoft.com/ado/2007/08/dataservices")
                item.AttributeExtensions.Add (qualifiedDataWebMetadataPrefix, "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")

            item.Title <- TextSyndicationContent(String.Empty)

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
            let relativeUri = svcBaseUri.MakeRelativeUri(resourceUri)

            item.Id <- resourceUri.AbsoluteUri

            item.Links.Add(SyndicationLink(relativeUri, "edit", rt.InstanceType.Name, null, 0L))
            item.Authors.Add emptyPerson
            item.Categories.Add(SyndicationCategory(rt.Namespace + "." + rt.InstanceType.Name, categoryScheme, null))
            item.Content <- build_content_from_properties relativeUri instance rt item
            item

        let internal write_items (wrapper:DataServiceMetadataProviderWrapper) (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) 
                                 (items:IEnumerable) (writer:TextWriter) (enc:Encoding) = 

            let rootUri = if containerUri <> null then containerUri else svcBaseUri
            let resourceSet = wrapper.ResourceSets |> Seq.tryFind (fun rs -> rs.ResourceType = rt)

            System.Diagnostics.Debug.Assert (rootUri <> null)

            let syndicationItems = 
                let lst = List<SyndicationItem>()
                for item in items do
                    lst.Add (build_item wrapper item svcBaseUri containerUri rt false true)
                lst

            let feed = SyndicationFeed(syndicationItems)

            feed.BaseUri <- svcBaseUri
            feed.AttributeExtensions.Add (qualifiedDataWebPrefix, "http://schemas.microsoft.com/ado/2007/08/dataservices")
            feed.AttributeExtensions.Add (qualifiedDataWebMetadataPrefix, "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")

            feed.Title <- TextSyndicationContent(rt.Name)
            feed.Id    <- rootUri.AbsoluteUri
            
            let selfLink = 
                match resourceSet with
                | Some rs -> Uri(rs.Name, UriKind.Relative)
                | _ -> containerUri

            feed.Links.Add(SyndicationLink(selfLink, "self", rt.Name, null, 0L))
            
            let xmlWriter = SerializerCommons.create_xmlwriter writer enc
            feed.GetAtom10Formatter().WriteTo( xmlWriter )
            xmlWriter.Flush()

        let internal write_item (wrapper:DataServiceMetadataProviderWrapper) (svcBaseUri:Uri) (containerUri:Uri) (rt:ResourceType) 
                                (item:obj) (writer:TextWriter) (enc:Encoding) = 

            let syndicationItem = build_item wrapper item svcBaseUri containerUri rt true false
            let xmlWriter = SerializerCommons.create_xmlwriter writer enc
            syndicationItem.GetAtom10Formatter().WriteTo( xmlWriter )
            xmlWriter.Flush()

        let private populate_properties (reader:XmlReader) (rt:ResourceType) (instance:obj) = 

            while reader.ReadToElement() do
                let doContinue = ref true

                while !doContinue do
                    if reader.NodeType = XmlNodeType.None then
                        doContinue := false

                    elif reader.NodeType <> XmlNodeType.Element then
                        doContinue := true
                        reader.Skip()
                    else 
                        match rt.Properties |> Seq.tryFind (fun p -> p.Name = reader.LocalName) with
                        | Some prop -> 
                            let value : obj = 
                                let rawStringVal = 
                                    let att = reader.GetAttribute("null", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")
                                    if att = null || XmlConvert.ToBoolean(att) = false then 
                                        if reader.IsEmptyElement
                                        then String.Empty
                                        else get_string_value reader 
                                    else null

                                if rawStringVal <> null then 
                                    match prop.ResourceType.ResourceTypeKind with
                                    | ResourceTypeKind.Primitive -> 
                                        let targetType = 
                                            let nulType = Nullable.GetUnderlyingType(prop.ResourceType.InstanceType)
                                            if nulType = null then prop.ResourceType.InstanceType
                                            else nulType

                                        if targetType = typeof<string>     then rawStringVal :> obj
                                        elif targetType = typeof<int32>    then XmlConvert.ToInt32 rawStringVal |> box
                                        elif targetType = typeof<int16>    then XmlConvert.ToInt16 rawStringVal |> box
                                        elif targetType = typeof<int64>    then XmlConvert.ToInt64 rawStringVal |> box
                                        elif targetType = typeof<byte>     then XmlConvert.ToByte rawStringVal |> box
                                        elif targetType = typeof<bool>     then XmlConvert.ToBoolean rawStringVal |> box
                                        elif targetType = typeof<DateTime> then XmlConvert.ToDateTime (rawStringVal, XmlDateTimeSerializationMode.RoundtripKind) |> box
                                        elif targetType = typeof<decimal>  then XmlConvert.ToDecimal rawStringVal |> box
                                        elif targetType = typeof<float>    then XmlConvert.ToSingle rawStringVal |> box
                                        else null

                                    | ResourceTypeKind.ComplexType -> failwithf "complex type support needs to be implemented"
                                    | ResourceTypeKind.EntityType  -> failwithf "entity type is not a supported property type"
                                    | _ -> failwithf "Unsupported type"
                                
                                else null

                            prop.SetValue(instance, value)

                            doContinue := reader.Read()

                        | _ ->  
                            // could not find property: should this be an error?
                            doContinue := false
                            

        let internal read_item (rt:ResourceType) (reader:TextReader) (enc:Encoding) = 
            let reader = SerializerCommons.create_xmlreader reader enc
            let fmt = Atom10ItemFormatter()
            fmt.ReadFrom(reader)
            let item = fmt.Item

            let instance = Activator.CreateInstance rt.InstanceType
            let content = 
                if item.Content :? XmlSyndicationContent 
                then item.Content :?> XmlSyndicationContent
                else null

            // todo: remapping of atom attributes
            for prop in rt.PropertiesDeclaredOnThisType do
                // rt.OwnEpmAttributes
                ()

            if content <> null then
                let reader = content.GetReaderAtContent()
                reader.ReadStartElement ("content", "http://www.w3.org/2005/Atom")
                if reader.IsStartElement ("properties", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata") then
                    reader.ReadStartElement("properties", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")

                    populate_properties reader rt instance

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
                    raise(NotImplementedException())
            }

    end
