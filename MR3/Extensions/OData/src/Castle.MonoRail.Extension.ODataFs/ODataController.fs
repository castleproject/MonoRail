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

namespace Castle.MonoRail

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Data.OData
    open System.Data.Services.Providers
    open System.Linq
    open System.Text
    open System.Reflection
    open System.Web
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.OData
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Extension.OData


    /// Entry point for exposing EntitySets through OData
    [<AbstractClass>]
    type ODataController<'T when 'T :> ODataModel>(model:'T) =  

        let _provider = model :> IDataServiceMetadataProvider
        let _wrapper  = DataServiceMetadataProviderWrapper(_provider)
        let _services : Ref<IServiceRegistry> = ref null

        let resolveHttpOperation (httpMethod) = 
            match httpMethod with 
            | SegmentProcessor.HttpGet    -> SegmentOp.View
            | SegmentProcessor.HttpPost   -> SegmentOp.Create
            | SegmentProcessor.HttpPut    -> SegmentOp.Update
            | SegmentProcessor.HttpDelete -> SegmentOp.Delete
            | _ -> failwithf "Unsupported http method %s" httpMethod

        let _executors = List<ControllerExecutor>()
        let _invoker_cache   = Dictionary<ResourceType, string ->  bool  ->  IList<Type * obj> -> RouteMatch -> HttpContextBase -> obj>()
        //                                              action  isCollection    params              route           context       result

        let get_action_invoker rt = 
            let create_controller_prototype (rt:ResourceType) = 
                let creator = model.GetControllerCreator (rt)
                if creator <> null 
                then creator.Invoke()
                else null

            // we will have issues with object models with self referencies
            // a better implementation would "consume" the items used, taking them off the list
            let tryResolveParamValue (paramType:Type) isCollection (parameters:IList<Type * obj>) = 
                let entryType =
                    
                    if isCollection then
                        match InternalUtils.getEnumerableElementType paramType with
                        | Some t -> t
                        | _ -> paramType
                    elif paramType.IsGenericType then
                        paramType.GetGenericArguments().[0]
                    else paramType

                match parameters |> Seq.tryFind (fun (ptype, _) -> ptype = entryType || entryType.IsAssignableFrom(ptype)) with 
                | Some (_, value) ->
                    // param is Model<T>
                    if paramType.IsGenericType && paramType.GetGenericTypeDefinition() = typedefof<Model<_>> 
                    then Activator.CreateInstance ((typedefof<Model<_>>).MakeGenericType(paramType.GetGenericArguments()), [|value|])
                    else // entryType <> paramType && paramType.IsAssignableFrom(entryType) then
                        value
                | _ -> null

            // returns a function able to invoke actions
            let create_executor_fn (rt:ResourceType) prototype = 
                let executor = (!_services).ControllerExecutorProvider.CreateExecutor(prototype)
                Diagnostics.Debug.Assert ( executor <> null && executor :? ODataEntitySubControllerExecutor )
                _executors.Add executor
                let odataExecutor = executor :?> ODataEntitySubControllerExecutor
                (fun action isCollection parameters routeMatch context -> 
                    let callback = Func<Type,obj>(fun ptype -> tryResolveParamValue ptype isCollection parameters)
                    odataExecutor.GetParameterCallback <- callback
                    executor.Execute(action, prototype, routeMatch, context))
            let succ, existing = _invoker_cache.TryGetValue rt
            if succ then existing
            else
                let prototype = create_controller_prototype rt
                let executor = create_executor_fn rt prototype 
                _invoker_cache.[rt] <- executor
                executor

        let invoke_action rt action parameters route context = 
            let invoker = get_action_invoker (rt)
            invoker action parameters route context

        let invoke_controller (action:string) isCollection (rt:ResourceType) parameters optional route context = 
            if model.SupportsAction(rt, action) then
                let result = invoke_action rt action isCollection parameters route context
                if result = null || ( result <> null && result :? EmptyResult )
                // if the action didn't return anything meaningful, we consider it a success
                then true 
                // else, the action took over, and we should therefore end our execution
                else false
            else
                // if we couldnt run the action, then the results 
                // depends on whether the call was optional or not 
                if optional then true else false

        let clean_up =
            _executors |> Seq.iter (fun exec -> (exec :> IDisposable).Dispose() )

        member x.Model = model
        member internal x.MetadataProvider = _provider
        member internal x.MetadataProviderWrapper = _wrapper

        member x.Process(services:IServiceRegistry, httpMethod:string, greedyMatch:string, 
                         routeMatch:RouteMatch, context:HttpContextBase) = 
            
            _services := services

            model.SetServiceRegistry services

            let request = context.Request
            let response = context.Response
            response.AddHeader("DataServiceVersion", "2.0")

            let writer = response.Output
            let qs = request.Url.Query
            let baseUri = routeMatch.Uri
            let requestContentType, reqEncoding = 
                if request.ContentType.IndexOf(";", StringComparison.Ordinal) <> -1 then 
                    let content = request.ContentType.Split([|';'|]).[0]
                    content, request.ContentEncoding
                else request.ContentType, request.ContentEncoding

            let negotiate_content (isSingle) = 
                let supported = 
                    if isSingle 
                    then [|"application/atom+xml";"application/json";"application/xml";"text/plain"|]
                    else [|"application/atom+xml";"application/json"|]
                services.ContentNegotiator.ResolveBestContentType (request.AcceptTypes, supported)

            let invoke action isColl (rt:ResourceType) (parameters:(Type*obj) seq) value isOptional = 
                let newParams = List(parameters)
                if value <> null then
                    newParams.Add (rt.InstanceType, value)
                invoke_controller action isColl rt newParams isOptional routeMatch context

            let callbacks = {
                authorize     = Func<ResourceType,(Type*obj) seq,obj,bool>(fun rt ps o         -> invoke "Authorize"     false rt ps o true);  
                authorizeMany = Func<ResourceType,(Type*obj) seq,IEnumerable,bool>(fun rt ps o -> invoke "AuthorizeMany" true  rt ps o true);  
                view          = Func<ResourceType,(Type*obj) seq,obj,bool>(fun rt ps o         -> invoke "View"          false rt ps o true);
                viewMany      = Func<ResourceType,(Type*obj) seq,IEnumerable,bool>(fun rt ps o -> invoke "ViewMany"      true  rt ps o true);
                create        = Func<ResourceType,(Type*obj) seq,obj,bool>(fun rt ps o         -> invoke "Create"        false rt ps o false);
                update        = Func<ResourceType,(Type*obj) seq,obj,bool>(fun rt ps o         -> invoke "Update"        false rt ps o false);
                remove        = Func<ResourceType,(Type*obj) seq,obj,bool>(fun rt ps o         -> invoke "Remove"        false rt ps o false);
                operation     = Action<ResourceType,(Type*obj) seq,string>(fun rt ps action    -> invoke action          false rt ps null false |> ignore);
                negotiateContent = Func<bool, string>(negotiate_content)
            }

            let requestParams = { 
                model = model; 
                provider = x.MetadataProvider; 
                wrapper = x.MetadataProviderWrapper; 
                contentType = requestContentType; 
                contentEncoding = reqEncoding;
                input = request.InputStream; 
                baseUri = baseUri; 
                accept = request.AcceptTypes;
            }
            let responseParams = { 
                contentType = null ;
                contentEncoding = response.ContentEncoding;
                writer = writer;
                httpStatus = 200;
                httpStatusDesc = "OK";
                location = null;
            }

            try
                try
                    let op = resolveHttpOperation httpMethod
                    let segments, meta, metaquery = SegmentParser.parse (greedyMatch, request.QueryString, model, baseUri)
 
                    SegmentProcessor.Process op segments meta metaquery request.QueryString callbacks requestParams responseParams

                    if responseParams.httpStatus <> 200 then
                        response.StatusCode <- responseParams.httpStatus
                        response.StatusDescription <- responseParams.httpStatusDesc
                    if not <| String.IsNullOrEmpty responseParams.contentType then
                        response.ContentType <- responseParams.contentType
                    if responseParams.contentEncoding <> null then 
                        response.ContentEncoding <- responseParams.contentEncoding
                    if responseParams.location <> null then
                        response.AddHeader("Location", responseParams.location)

                    EmptyResult.Instance
                
                finally 
                    clean_up
            with 
            | :? HttpException as ht -> reraise()
            | exc -> 

                reraise()

                // todo: instead of raising, we should serialize error e write it back

                // TODO: use responseContentType to resolve output mime type

                // in json: 
                (* 
                    { "error": {
                        "code": "", 
                        "message": {
                            "lang": "en-US", 
                            "value": "Resource not found for the segment 'People'."
                        } } 
                    }
                *)

                // in xml: 
                (* 
                    <?xml version="1.0" encoding="utf-8" standalone="yes"?>
                    <error xmlns="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata">
                      <code></code>
                      <message xml:lang="en-US">Resource not found for the segment 'People'.</message>
                    </error>                
                *)

                // if html, let the exception filters handle it, otherwise, let it bubble to asp.net



