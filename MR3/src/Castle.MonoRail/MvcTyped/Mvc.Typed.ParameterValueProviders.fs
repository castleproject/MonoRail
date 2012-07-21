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

namespace Castle.MonoRail.Hosting.Mvc.Typed

    open Helpers
    open System
    open System.Collections.Generic
    open System.Reflection
    open System.Web
    open System.ComponentModel.Composition
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Serialization
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Extensibility


    [<Export(typeof<IParameterValueProvider>)>]
    [<ExportMetadata("Order", 100)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type RoutingValueProvider [<ImportingConstructor>] (route_match:RouteMatch) = 

        interface IParameterValueProvider with
            member x.TryGetValue(name, paramType, value) = 
                if paramType = typeof<RouteMatch> then 
                    value <- route_match
                    true
                else
                    let res, routeVal = route_match.RouteParams.TryGetValue name
                    // Todo:refactor
                    if res then
                        let succeeded, tmp = Conversions.convert routeVal paramType
                        if succeeded then
                            value <- tmp
                        succeeded
                    else false


    [<Export(typeof<IParameterValueProvider>)>]
    [<ExportMetadata("Order", 100000)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type FrameworkObjectsValueProvider [<ImportingConstructor>] (context:HttpContextBase, servRegistry:IServiceRegistry) = 

        interface IParameterValueProvider with
            member x.TryGetValue(name:string, paramType:Type, value:obj byref) = 
                let paramTypeDef = 
                    if paramType.IsGenericType then paramType.GetGenericTypeDefinition() else paramType

                if paramTypeDef = typeof<System.Security.Principal.IPrincipal> then
                    value <- context.User; true
                elif paramTypeDef = typeof<System.Web.HttpContextBase> then
                    value <- context; true
                elif paramTypeDef = typeof<System.Web.HttpResponseBase> then
                    value <- context.Response; true
                elif paramTypeDef = typeof<System.Web.HttpRequestBase> then
                    value <- context.Request; true
                elif paramTypeDef = typeof<System.Web.HttpSessionStateBase> then
                    value <- context.Session; true
                elif paramTypeDef = typeof<System.Web.HttpServerUtilityBase> then
                    value <- context.Server; true
                elif paramTypeDef = typeof<PropertyBag> then 
                    value <- PropertyBag(); true
                elif paramTypeDef = typedefof<PropertyBag<_>> then
                    value <- Activator.CreateInstance(paramType); true
                elif paramTypeDef = typedefof<IServiceRegistry> then
                    value <- servRegistry; true
                elif paramTypeDef = typeof<string> && name === "httpMethod" then
                    value <- Helpers.get_effective_http_method context.Request; true
                else
                    false


    [<Export(typeof<IParameterValueProvider>)>]
    [<ExportMetadata("Order", 10)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type RequestBoundValueProvider [<ImportingConstructor>] (request:HttpRequestBase) = 

        interface IParameterValueProvider with
            member x.TryGetValue(name:string, paramType:Type, value:obj byref) = 
                let reqVal = request.Params.[name]

                if reqVal = null then 
                    false
                else
                    let succeeded, tmp = Conversions.convert reqVal paramType
                    if succeeded then
                        value <- tmp
                    succeeded


    [<Export(typeof<IParameterValueProvider>)>]
    [<ExportMetadata("Order", 100000)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type SerializerValueProvider [<ImportingConstructor>] (request:HttpRequestBase) = 
        let mutable _resolver : IModelSerializerResolver = null
        let mutable _contentNeg : ContentNegotiator = null

        [<Import>]
        member x.ModelSerializerResolver with set v = _resolver <- v

        [<Import>]
        member x.ContentNegotiator with set v = _contentNeg <- v

        interface IParameterValueProvider with
            member x.TryGetValue(name:string, paramType:Type, value:obj byref) = 
                let contentType = request.ContentType

                if String.IsNullOrEmpty contentType || paramType = typeof<string> || paramType.IsValueType || paramType.IsPrimitive then
                    false
                else
                    let mime = _contentNeg.NormalizeRequestContentType contentType
                    let modelType = 
                        if paramType.IsGenericType then paramType.GetGenericArguments() |> Seq.head else paramType
                           
                    let serializer = _resolver.CreateSerializer(modelType, mime)
                                   
                    if serializer <> null then
                        // TODO: should obtain modelmetadata from DataAnnotationsModelMetadataProvider
                        // and validation metadata 
                        // and passed to deseializer
                        // let model = deserializeMethod.Invoke(serializer, [|name; contentType; request; DataAnnotationsModelMetadataProvider()|])
                        let serializationCtx = ModelSerializationContext(request.InputStream, request.Form)
                        let model = serializer.Deserialize(name, contentType, serializationCtx, DataAnnotationsModelMetadataProvider())
                        if paramType = modelType 
                        then value <- model
                        else 
                            // TODO: Support more collection types
                            if typedefof<IList<_>>.MakeGenericType(paramType.GetGenericArguments()).IsAssignableFrom(paramType) || 
                               typedefof<IEnumerable<_>>.MakeGenericType(paramType.GetGenericArguments()).IsAssignableFrom(paramType) then
                                let targetT = paramType.GetGenericArguments().[0]
                                let listType = typedefof<List<_>>.MakeGenericType( [|targetT|] )
                                if model <> null then
                                    value <- Activator.CreateInstance(listType, [|model|])
                                else
                                    value <- Activator.CreateInstance listType
                            else
                                failwithf "Collection type not supported %s" (paramType.FullName)

                        true
                    else 
                        false