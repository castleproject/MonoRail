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
open System.Xml


type SerializerFactory() = 

    static member Create(contentType:string, overriding:string, wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc) : Serializer = 

        match contentType.ToLowerInvariant() with
        | "application/atom+xml" -> 
            upcast AtomSerialization.AtomSerializer(wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc)
            
        | "application/json" -> 
            let useSimplerFormat = overriding === "simplejson"
            upcast JSonSerialization.JsonSerializer(wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc, useSimplerFormat)
        
        | "text/xml"  
        | "application/xml" ->
            upcast XmlSerialization.XmlSerializer(wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc)

        | "text/plain" ->
            upcast PlainTextSerialization.PlainTextSerializer(wrapper, serviceBaseUri, containerUri, rt, propertiesToExpand, writer, enc)

        | _ -> failwithf "unsupported content type %s" contentType


type DeserializerFactory() = 

    static member Create(contentType:string) : Deserializer = 
        match contentType.ToLowerInvariant() with
        | "application/atom+xml" -> 
            AtomSerialization.DeserializerInstance

        | "application/json" -> 
            JSonSerialization.DeserializerInstance

        | "application/xml"
        | "text/xml" -> 
            XmlSerialization.CreateDeserializer()

        | _ -> failwithf "unsupported content type %s" contentType

