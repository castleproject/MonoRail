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


// Read first: 

// Exposed on this namespace for usability
namespace Castle.MonoRail 

    open System.IO
    open System.Web
    open System
    open System.Collections.Specialized
    open Castle.MonoRail

    [<AllowNullLiteral>]
    type ModelSerializationContext(inputStream, formValues) = 
        member x.InputStream : Stream = inputStream
        member x.FormValues : NameValueCollection = formValues


// Exposed in this namespace since it's not supposed to be used directly
namespace Castle.MonoRail.Serialization

    open System.IO
    open System.Web
    open Castle.MonoRail

    /// This non generic version is for internal use only and should not be
    /// implemented by 3rd parties/users. Hence the reason it's hidden in this namespace
    [<Interface;AllowNullLiteral>]
    type IModelSerializer = 
        abstract member Serialize : model:obj * contentType:string * writer:System.IO.TextWriter * metadataProvider:ModelMetadataProvider -> unit
        abstract member Deserialize : prefix:string * contentType:string * context:ModelSerializationContext * metadataProvider:ModelMetadataProvider -> obj


// Exposed on this namespace for usability
namespace Castle.MonoRail 

    open System.IO
    open System.Web
    open System
    open System.Collections.Specialized
    open Castle.MonoRail
    open Castle.MonoRail.Serialization

    [<Interface;AllowNullLiteral>]
    type IModelSerializer<'a> = 
        abstract member Serialize : model:'a * contentType:string * writer:TextWriter * metadataProvider:ModelMetadataProvider -> unit
        abstract member Deserialize : prefix:string * contentType:string * context:ModelSerializationContext * metadataProvider:ModelMetadataProvider -> 'a


    [<Interface;AllowNullLiteral>]
    type IModelSerializerResolver = 
        abstract member HasCustomSerializer : model:Type * mediaType:string -> bool
        abstract member Register<'a> : mediaType:string * serializer:Type -> unit 
        abstract member CreateSerializer<'a> : mediaType:string -> IModelSerializer<'a>
        abstract member CreateSerializer : modelType:Type * mediaType:string -> IModelSerializer


