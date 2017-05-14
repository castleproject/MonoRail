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

namespace Castle.MonoRail.Helpers

    open System
    open System.Collections.Generic
    open System.IO
    open System.Text
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.ViewEngines
    open Castle.MonoRail.Serialization
    open Castle.MonoRail.Hosting.Mvc.Typed

    type JsonHelper(ctx) = 
        inherit SerializerBasedHelper(ctx)
        
        static let JsonContentType = "application/json"

        member x.ToJson(targetType:Type, graph:obj) : IHtmlStringEx = 
            arg_not_null targetType "targetType"
            arg_not_null graph "graph"

            let serializer = x.ModelSerializer.CreateSerializer(targetType, MediaTypes.JSon)
            let writer = new StringWriter()
            serializer.Serialize(graph, JsonContentType, writer, x.ModelMetadataProvider)
            upcast HtmlResult( writer.GetStringBuilder().ToString() )

        member x.ToJson(graph:obj) : IHtmlStringEx = 
            arg_not_null graph "graph"

            x.ToJson(graph.GetType(), graph)

        member x.ToJson<'T>(graph:'T) : IHtmlStringEx = 
            arg_not_null graph "graph"
            let serializer = x.ModelSerializer.CreateSerializer<'T>(MediaTypes.JSon)

            let writer = new StringWriter()
            serializer.Serialize(graph, JsonContentType, writer, x.ModelMetadataProvider)
            upcast HtmlResult( writer.GetStringBuilder().ToString() )