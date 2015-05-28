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

namespace Castle.MonoRail.Routing

    open System
    open System.Collections.Generic
    open System.Threading
    open System.Text
    open System.Web
    open Internal
    open Helpers
    open Castle.MonoRail

    [<AllowNullLiteral>]
    type RouteCollection(routes:IList<Route>) = 
        inherit System.Collections.ObjectModel.ReadOnlyCollection<Route>(routes)
        let _dict = lazy let d = Dictionary<string,Route>()
                         routes |> Seq.iter (fun r -> if r.Name <> null then d.[r.Name] <- r )
                         d
        member x.Item with get(name:string) = _dict.Force().[name]

    and [<AbstractClass; AllowNullLiteral>] 
        RouteOperations(parent:Route) = 
        let _routes = List<Route>()

        let merge_inplace (dict1:IDictionary<string,string>) (dict2:IDictionary<string,string>) =
            for pair in dict2 do
                dict1.[pair.Key] <- pair.Value

        let try_match (route:Route) (request:RequestInfo) =
            let matchReqs, hasChildren = 
                if route.HasConfig then 
                    let cfg = (route.RouteConfig :> RouteConfig)
                    cfg.TryMatchRequirements request, cfg.HasChildren
                else
                    true, false
        
            if not matchReqs then
                false, null, 0, -1
            else
                let path = request.Path
                let namedParams = Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
                let start = request.PathStartIndex
                let res, index, cut = RecursiveMatch path start start 0 route.RouteNodes namedParams route.DefaultValues hasChildren false
                if res then
                    merge_inplace namedParams route.InvariablesValues
                    true, namedParams, index, cut
                else
                    false, null, 0, -1

        let rec rec_try_match index (routes:List<Route>) (request:RequestInfo) : RouteMatch =
            if (index > routes.Count - 1) then
                null
            else
                let route = routes.[index]
                // let merged_defaults = (Helpers.merge_dict acc_defaults route.DefaultValues)
                let res, namedParams, newindex, cutIndex = try_match route request 
                if res then
                    let haschildren = 
                        if route.HasConfig then route.RouteConfig.HasChildren else false
                    
                    if haschildren then
                        request.PathStartIndex <- newindex
                        let ops = route.RouteConfig :> RouteOperations
                        let nodes = ops.InternalRoutes
                        let innermatch = rec_try_match 0 nodes request 

                        if innermatch <> null then
                            merge_inplace innermatch.RouteParams namedParams
                            merge_inplace namedParams route.InvariablesValues
                            innermatch
                        else null
                    else
                        let matchPartial = request.Path.Substring(0, cutIndex)
                        // Console.WriteLine("matchPartial " + matchPartial)
                        RouteMatch(route, namedParams, (fun _ -> Uri(request.BaseUri, matchPartial) ))
                else
                    rec_try_match (index + 1) routes request

        member this.Routes = RouteCollection(_routes)
        member internal this.InternalRoutes = _routes

        member internal this.InternalTryMatch (request:RequestInfo) : RouteMatch = 
            rec_try_match 0 _routes request 

        member this.Match(path:string, handlerMediator:IRouteHttpHandlerMediator)  = 
            Assertions.ArgNotNullOrEmpty path "path"
            Assertions.ArgNotNull handlerMediator "handlerMediator"
            this.InternalMatch (path, null, null, handlerMediator)

        member this.Match(path:string, name:string, handlerMediator:IRouteHttpHandlerMediator) = 
            Assertions.ArgNotNullOrEmpty path "path"
            Assertions.ArgNotNullOrEmpty name "name"
            Assertions.ArgNotNull handlerMediator "handlerMediator"
            this.InternalMatch (path, name, null, handlerMediator)

        member this.Match(path:string, config:Action<RouteConfig>, handlerMediator:IRouteHttpHandlerMediator) = 
            Assertions.ArgNotNullOrEmpty path "path"
            Assertions.ArgNotNull config  "config"
            Assertions.ArgNotNull handlerMediator "handlerMediator"
            this.InternalMatch (path, null, config, handlerMediator)

        member this.Match(path:string, name:string, config:Action<RouteConfig>, handlerMediator:IRouteHttpHandlerMediator) = 
            Assertions.ArgNotNullOrEmpty path "path"
            Assertions.ArgNotNullOrEmpty name "name"
            Assertions.ArgNotNull config  "config"
            Assertions.ArgNotNull handlerMediator "handlerMediator"
            this.InternalMatch (path, name, config, handlerMediator)

        member internal this.InternalMatch(path:string, name:string, config:Action<RouteConfig>, handlerMediator:IRouteHttpHandlerMediator) = 
            let routeNode = ParseRouteDefinition(path)
            let route = new Route(parent, routeNode, name, path, handlerMediator)
            if config <> null then
                let cfg = RouteConfig(route)
                config.Invoke(cfg)
                route.RouteConfig <- cfg
            _routes.Add(route)
            route

    and [<AllowNullLiteral>]
        Route internal (parent, routeNodes, name, path, handlerMediator:IRouteHttpHandlerMediator) = 
        let _defValues = lazy Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        let _invariablesValues = lazy Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        let _extraData = lazy Dictionary<string,obj>()
        let mutable _config : RouteConfig = null
    
        // this is very order dependant, but shouldnt be a problem in practice
        let _children = lazy ( let children : IList<Route> = 
                                   if _config <> null then upcast (_config.Routes) else upcast List()
                               RouteCollection(children) )

        let encode (value:string) = 
            System.Web.HttpUtility.UrlEncode value 

        let build_qs (parameters:IDictionary<string,string>) = 
            if parameters = null || parameters.Count = 0 
            then String.Empty
            else 
                parameters 
                |> Seq.fold (fun acc pair -> let entry = (sprintf "%s=%s" (encode pair.Key) (encode pair.Value) ) 
                                             if acc.Length = 1 
                                             then acc + entry 
                                             else acc + "&" + entry) "?"


        let rec rec_generate_url (route:Route) (buffer:StringBuilder) (parameters:IDictionary<string,string>) = 
            if route.Parent <> null then 
                rec_generate_url route.Parent buffer parameters
            let r, msg = RecursiveGenerate buffer 0 route.RouteNodes (List<string>()) parameters (route.DefaultValues)
            if not r then 
                ExceptionBuilder.RaiseRouteException msg

        member this.Redirect(url:string) = 
            ExceptionBuilder.RaiseNotImplemented()
            ignore

        member this.PermanentRedirect(url:string) = 
            ExceptionBuilder.RaiseNotImplemented()
            ignore

        member this.Parent = parent
        member this.Children = _children.Force() 
        member this.Name = name
        member this.Path = path
        member this.RouteConfig 
            with get() = 
                if _config = null then _config <- RouteConfig(this)
                _config
            and internal set(v) = _config <- v
        member this.HandlerMediator = handlerMediator
        member this.ExtraData = _extraData.Force()

        member this.Generate(virtualDir:string, parameters:IDictionary<string,string>) : string = 
            Assertions.ArgNotNull virtualDir "virtualDir"
            Assertions.ArgNotNull parameters "parameters"

            let buffer = 
                if (virtualDir.EndsWith("/", StringComparison.OrdinalIgnoreCase)) 
                then StringBuilder(virtualDir.Substring(0, virtualDir.Length - 1))
                else StringBuilder(virtualDir)

            rec_generate_url this buffer parameters 

            let result = buffer.ToString()
            if result = String.Empty 
            then "/" + build_qs parameters
            else result + build_qs parameters

        member internal x.HasConfig = _config <> null
        member internal this.DefaultValues = _defValues.Force()
        member internal this.InvariablesValues = _invariablesValues.Force()
        member internal this.RouteNodes = routeNodes


    and [<AllowNullLiteral>] RouteConfig(route:Route) =
        inherit RouteOperations(route)
        let mutable _controller:string = null
        let mutable _domain:string = null
        // let mutable _method:string = null
        let mutable _protocol:string = null
        let mutable _action:string = null
        let mutable _haschildren = false

        member this.Protocol(protocol:string) = 
            _protocol <- protocol; this

        member this.Domain(domain:string) = 
            _domain <- domain; this

        member this.Controller(name:string) : RouteConfig =
            _controller <- name; this

        member this.Controller<'T>() : RouteConfig =
            // need to be reviewed
            _controller <- typeof<'T>.Name; this

        member this.Action(name:string) : RouteConfig =
            _controller <- name; this

        // member this.Param(name:string) : ParamConfig = 
        //    ParamConfig(this)

        member this.Defaults(configExp:Action<DefaultsConfig>) : RouteConfig = 
            let defConfig = DefaultsConfig(route.DefaultValues)
            configExp.Invoke(defConfig); this

        member this.Invariables(configExp:Action<DefaultsConfig>) : RouteConfig = 
            let defConfig = DefaultsConfig(route.InvariablesValues)
            configExp.Invoke(defConfig); this

        member internal this.HasChildren = base.InternalRoutes.Count <> 0

        member internal this.TryMatchRequirements(request:RequestInfo) = 
            // if ((_method <> null) && (String.Compare(request.HttpMethod, _method, StringComparison.OrdinalIgnoreCase) <> 0)) then
            //    false
            if ((_protocol <> null) && (String.Compare(request.Protocol, _protocol, StringComparison.OrdinalIgnoreCase) <> 0)) then
                false
            elif ((_domain <> null) && (String.Compare(request.Domain, _domain, StringComparison.OrdinalIgnoreCase) <> 0)) then
                false
            else
                true


    and DefaultsConfig(dict) = 
        member this.Controller(name:string) = 
            Assertions.ArgNotNullOrEmpty name "name"
            dict.["controller"] <- name
            this

        member this.Action(name:string) = 
            Assertions.ArgNotNullOrEmpty name "name"
            dict.["action"] <- name
            this

        member this.Area(name:string) = 
            Assertions.ArgNotNullOrEmpty name "name"
            dict.["area"] <- name
            this

        member this.Param(name:string, value:string) = 
            Assertions.ArgNotNullOrEmpty name "name"
            Assertions.ArgNotNullOrEmpty value "value"
            dict.[name] <- value
            this


    and [<AllowNullLiteral>] 
        RouteMatch (route:Route, namedParams:IDictionary<string,string>, lazyUri:Func<Uri>) = 
            let _uri = lazy lazyUri.Invoke()
            member this.Route = route
            member this.RouteParams = namedParams
            member this.Uri = _uri.Force()


    and [<Interface; AllowNullLiteral>] 
        IRouteHttpHandlerMediator = 
            abstract GetHandler : request:HttpRequest * routeData:RouteMatch -> IHttpHandler 




