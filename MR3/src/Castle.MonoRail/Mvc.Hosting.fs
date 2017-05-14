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

namespace Castle.MonoRail.Hosting.Mvc

    open System
    open System.Linq
    open System.Collections
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.Hosting
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Hosting.Mvc.Extensibility
    open Castle.MonoRail.Hosting


    [<Export; AllowNullLiteral>]
    type PipelineRunner() = 
        let mutable _controllerProviderAggregator : Ref<ControllerProviderAggregator> = ref null
        let mutable _controllerExecProviderAggregator : Ref<ControllerExecutorProviderAggregator> = ref null

        [<Import(AllowRecomposition=true)>]
        member this.ControllerProviderAggregator
            with get() = !_controllerProviderAggregator and set(v) = _controllerProviderAggregator := v

        [<Import(AllowRecomposition=true)>]
        member this.ControllerExecutorProviderAggregator
            with get() = !_controllerExecProviderAggregator and set(v) = _controllerExecProviderAggregator := v

        member this.TryExecute(route_data:RouteMatch, context:HttpContextBase) =
            let _, area = route_data.RouteParams.TryGetValue "area"
            let hasCont, controller = route_data.RouteParams.TryGetValue "controller"

            if not hasCont then raise(MonoRailException("Expecting route to have at least a 'controller' entry"))

            let spec = NamedControllerCreationSpec( area, controller )
            let prototypeFunc = (!_controllerProviderAggregator).CreateController spec
            
            if prototypeFunc = null then 
                // context.AddError( ExceptionBuilder.ControllerProviderNotFound() )
                false 
            else
                let prototype = prototypeFunc.Invoke()
                use executor = (!_controllerExecProviderAggregator).CreateExecutor (prototype)
                
                if executor = null then
                    // context.AddError( ExceptionBuilder.ControllerExecutorProviderNotFound() )
                    false
                else
                    let action_name = route_data.RouteParams.["action"]
                    executor.Execute(action_name, prototype, route_data, context) |> ignore
                    true


    [<AllowNullLiteral>]
    [<Castle.Extensibility.BundleExport(typeof<IComposableHandler>)>]
    type MvcComposableHandler() = 
        inherit ComposableHandler()

        let mutable _pipeline : PipelineRunner = null

        [<Import>]
        member this.Pipeline with set(v) = _pipeline <- v

        override this.TryProcessRequest(context:HttpContextBase) =
            context.Response.Buffer <- true
            let route_data = context.Items.[Constants.MR_Routing_Key] :?> Castle.MonoRail.Routing.RouteMatch
            _pipeline.TryExecute (route_data, context)


    
    type MonoRailHandlerMediator() = 
        interface IRouteHttpHandlerMediator with
            member this.GetHandler(request:HttpRequest, routeData:RouteMatch) : IHttpHandler =
                MonoRailHandler() :> IHttpHandler
    
