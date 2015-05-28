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
    open System.IO
    open System.Web
    open Castle.MonoRail.Hosting 


    type RoutingHttpModule () as self = 

        [<DefaultValue>] val mutable _router : Router

        let OnPostResolveRequestCache(sender:obj, args) : unit = 
            let app = sender :?> HttpApplication
            let context = app.Context
            let httpRequest = context.Request

            if not (File.Exists(httpRequest.PhysicalPath)) then 
                let request = RequestInfo(httpRequest)
                let route_match = self._router.TryMatch(request)

                if route_match <> null then
                    context.Items.[Constants.MR_Routing_Key] <- route_match
                    let handlerMediator = route_match.Route.HandlerMediator
                    let httpHandler = handlerMediator.GetHandler(httpRequest, route_match)
                    if httpHandler <> null then
                        context.RemapHandler httpHandler

        let OnPostResolveRequestCache_Handler = 
            new EventHandler( fun obj args -> OnPostResolveRequestCache(obj, args) )

        do 
            self._router <- Router.Instance

        interface IHttpModule with
            member this.Dispose() = 
                ()

            member this.Init(app:HttpApplication) =
                app.PostResolveRequestCache.AddHandler OnPostResolveRequestCache_Handler
                



