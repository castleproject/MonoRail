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
open System.Xml


type SerializerFactory() = 

    static member Create(contentType:string) : Serializer = 

        match contentType.ToLowerInvariant() with
        | "application/atom+xml" -> 
            AtomSerialization.CreateSerializer()
            
        | "application/json" -> 
            JSonSerialization.CreateSerializer()
            
        | "text/xml"  
        | "application/xml" ->
            XmlSerialization.CreateSerializer()

        | "text/plain" ->
            PlainTextSerialization.CreateSerializer()
            
        | _ -> failwithf "unsupported content type %s" contentType



type DeserializerFactory() = 

    static member Create(contentType:string) : Deserializer = 

        match contentType.ToLowerInvariant() with
        | "application/atom+xml" -> 
            AtomSerialization.CreateDeserializer()
            
        | "application/json" -> 
            JSonSerialization.CreateDeserializer()
            
        | "application/xml"
        | "text/xml" -> 
            XmlSerialization.CreateDeserializer()
            
        | _ -> failwithf "unsupported content type %s" contentType