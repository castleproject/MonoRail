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

[<System.Runtime.CompilerServices.ExtensionAttribute>]
module public ExtensionMethods = 

    open System
    open Castle.MonoRail
    open Castle.MonoRail.Hosting.Mvc
    open System.Runtime.CompilerServices

    [<ExtensionAttribute>]
    [<CompiledName("Match")>]
    let MatchExt1(router:RouteOperations, path:string) = 
        router.Match(path, MonoRailHandlerMediator())

    [<ExtensionAttribute>]
    [<CompiledName("Match")>]
    let MatchExt2(router:RouteOperations, path:string, name:string) = 
        router.Match(path, name, MonoRailHandlerMediator())

    [<ExtensionAttribute>]
    [<CompiledName("Match")>]
    let MatchExt3(router:RouteOperations, path:string, config:Action<RouteConfig>) = 
        router.Match(path, config, MonoRailHandlerMediator())

    [<ExtensionAttribute>]
    [<CompiledName("Match")>]
    let MatchExt4(router:RouteOperations, path:string, name:string, config:Action<RouteConfig>) = 
        router.Match(path, name, config, MonoRailHandlerMediator())

    [<ExtensionAttribute>]
    [<CompiledName("MatchIgnore")>]
    let MatchIgnore1(router:RouteOperations, path:string) = 
        router.Match(path, IgnoreRoute.Instance)


    (*

    [<ExtensionAttribute>]
    [<CompiledName("Resource")>]
    let ResourceExt1(router:RouteOperations, name:string) = 
        Assertions.ArgNotNullOrEmpty name "name"
        // router.Resource(name, MonoRailHandlerMediator())
        failwith "Not implemented"

    [<ExtensionAttribute>]
    [<CompiledName("Resources")>]
    let ResourcesExt1(router:RouteOperations, name:string) = 
        Assertions.ArgNotNullOrEmpty name "name"

        router.Match("/" + name, 
            (fun (cfg:RouteConfig) -> 
                (
                    cfg.Match("/new", (fun (cf:RouteConfig) -> 
                        cf.HttpMethod("GET") |> ignore), MonoRailHandlerMediator()) |> ignore
                    
                    cfg.Match("/create", (fun (cf:RouteConfig) -> 
                        cf.HttpMethod("POST") |> ignore), MonoRailHandlerMediator()) |> ignore
                    
                    cfg.Match("/edit/:id", (fun (cf:RouteConfig) -> 
                        cf.HttpMethod("GET") |> ignore), MonoRailHandlerMediator()) |> ignore
                    
                    cfg.Match("/update/:id", (fun (cf:RouteConfig) -> 
                        cf.HttpMethod("PUT") |> ignore), MonoRailHandlerMediator()) |> ignore
                    
                    cfg.Match("/view/:id", (fun (cf:RouteConfig) -> 
                        cf.HttpMethod("GET") |> ignore), MonoRailHandlerMediator()) |> ignore
                    
                    cfg.Match("/delete", (fun (cf:RouteConfig) -> 
                        cf.HttpMethod("DELETE") |> ignore), MonoRailHandlerMediator()) |> ignore
                    ()
                )), MonoRailHandlerMediator())

    [<ExtensionAttribute>]
    [<CompiledName("Resources")>]
    let ResourcesExt2(router:RouteOperations, name:string, identifier:string) = 
        Assertions.ArgNotNullOrEmpty name "name"
        Assertions.ArgNotNullOrEmpty identifier "identifier"
        failwith "Not implemented"

    *)

    [<ExtensionAttribute>]
    let Redirect(response:System.Web.HttpResponseBase, url:TargetUrl) = 
        response.Redirect(url.ToString(), false)