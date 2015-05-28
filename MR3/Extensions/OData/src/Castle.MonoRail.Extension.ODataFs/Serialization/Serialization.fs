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
open System.Collections.Generic
open System.Linq
open System.Xml
open System.IO
open System.Text
open System.Xml
open System.Data.OData
open System.Data.Services.Providers
open Castle.MonoRail
open Castle.MonoRail.OData


type ResponseToSend = {
    mutable QItems : IQueryable;
    mutable SingleResult : obj;
    ResType : ResourceType;
    FinalResourceUri : Uri;
    ResProp : ResourceProperty;
    PropertiesToExpand : HashSet<ResourceProperty>;
}

[<AbstractClass;AllowNullLiteral>]
type Deserializer() = 
    class 
        abstract member DeserializeMany : rt:ResourceType * reader:TextReader * enc:Encoding -> IEnumerable
        abstract member DeserializeSingle : rt:ResourceType * reader:TextReader * enc:Encoding * target:obj -> obj
    end

[<AbstractClass;AllowNullLiteral>]
type Serializer(wrapper:DataServiceMetadataProviderWrapper, serviceBaseUri:Uri, containerUri:Uri, rt:ResourceType, 
                propertiesToExpand:HashSet<ResourceProperty>, writer:TextWriter, enc:Encoding) = 

    abstract member SerializeMany : items:IEnumerable -> unit
    abstract member SerializeSingle : item:obj -> unit
    abstract member SerializeProperty : prop:ResourceProperty * value:obj -> unit

    member x.ShouldExpand (property:ResourceProperty) = propertiesToExpand.Contains property

    member x.Serialize (response:ResponseToSend) =
        let items : IQueryable = response.QItems
        let item = response.SingleResult
        let rt = response.ResType

        if items <> null then 
            x.SerializeMany (items)
        elif response.ResProp <> null && response.ResProp.IsOfKind(ResourcePropertyKind.Primitive) then 
            x.SerializeProperty (response.ResProp, item)
        else 
            x.SerializeSingle (item)

[<AutoOpen>]
module SerializerCommons = 
    begin

        type ResourceProperty
            with
                member x.GetValue(instance:obj) = 
                    if instance = null then
                        raise(ODataException("Trying to get value from property with a null instance. Resource Property " + x.Name))

                    if x.CanReflectOnInstanceTypeProperty then 
                        let insType = instance.GetType()
                        let prop = insType.GetProperty(x.Name)
                        if prop = null then
                            raise(ODataException("Could not resolve property through reflection. Resource Property " + x.Name + " on type " + insType.FullName))

                        if prop.PropertyType.IsEnum 
                        then Enum.GetName(prop.PropertyType, prop.GetValue(instance, null)) |> box
                        else prop.GetValue(instance, null)
                    else
                        let config = x.CustomState :?> PropConfigurator
                        System.Diagnostics.Debug.Assert(config <> null)
                        config.GetValue(instance)

                member x.GetValueAsStr(instance:obj) = 
                    let value = x.GetValue(instance)
                    if value = null 
                    then null 
                    else value.ToString()

                member x.SetValue(instance:obj, value:obj) = 
                    if x.CanReflectOnInstanceTypeProperty then 
                        let prop = instance.GetType().GetProperty(x.Name)
                        
                        if prop.PropertyType.IsEnum then 
                            if value <> null then
                                let v = Enum.Parse(prop.PropertyType, value.ToString(), true) 
                                prop.SetValue(instance, v, null)
                        else prop.SetValue(instance, value, null)

                    else
                        let config = x.CustomState :?> PropConfigurator
                        System.Diagnostics.Debug.Assert(config <> null)
                        config.SetValue(instance, value)
                    
        type ResourceType 
            with 
                member x.GetKey (instance:obj) = 
                    let keyValue = 
                        if x.KeyProperties.Count = 1 
                        then x.KeyProperties.[0].GetValueAsStr(instance)
                        else failwith "Composite keys are not supported"
                    sprintf "(%s)" keyValue

        type XmlReader with
            member x.ReadToElement() = 
                let doCont = ref true
                let isElement = ref false
                while !doCont do
                    match x.NodeType with 
                    | XmlNodeType.None | XmlNodeType.ProcessingInstruction 
                    | XmlNodeType.Comment | XmlNodeType.Whitespace 
                    | XmlNodeType.XmlDeclaration -> 
                        ()
                    | XmlNodeType.Text -> 
                        if String.IsNullOrEmpty x.Value || x.Value.Trim().Length <> 0 
                        then isElement := false; doCont := false
                    | XmlNodeType.Element -> 
                        isElement := true; doCont := false
                    | _ -> 
                        isElement := false; doCont := false
                    if !doCont then doCont := x.Read()
                !isElement

        let internal create_xmlwriter(writer:TextWriter) (encoding) = 
            let settings = XmlWriterSettings(CheckCharacters = false,
                                             ConformanceLevel = ConformanceLevel.Fragment,
                                             Encoding = encoding,
                                             Indent = true,
                                             NewLineHandling = NewLineHandling.Entitize)
            let xmlWriter = XmlWriter.Create(writer, settings)
            xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"" + encoding.WebName + "\" standalone=\"yes\"")
            xmlWriter

        let internal create_xmlreader(reader:TextReader) (encoding) = 
            let settings = XmlReaderSettings()
            XmlReader.Create(reader, settings)
    end

