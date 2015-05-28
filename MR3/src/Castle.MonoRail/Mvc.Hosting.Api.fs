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
    open System.Web
    open System.Collections.Generic
    open Castle.MonoRail.Routing


    [<AbstractClass>]
    type ControllerCreationSpec() = 
        abstract member Match : entries:IDictionary<string,Type> -> Type


    type NamedControllerCreationSpec(area:string, name:string) = 
        inherit ControllerCreationSpec()
        let _combined = lazy ( if String.IsNullOrEmpty area 
                               then name else area + "\\" + name )
        member x.Area = area
        member x.ControllerName = name
        member x.CombinedName = _combined.Force()

        override x.Match (entries) = 
            let key = _combined.Force()
            let _, entry = entries.TryGetValue(key)
            entry

    type PredicateControllerCreationSpec(predicate:Func<Type, bool>) = 
        inherit ControllerCreationSpec()
        
        override x.Match (entries) =
            entries.Values.FirstOrDefault(predicate)
            

    [<AllowNullLiteral>]
    type ControllerCreationContext(route_data:RouteMatch, context:HttpContextBase) = 
        member x.RouteMatch = route_data
        member x.HttpContext = context
        

    [<AbstractClass; AllowNullLiteral>]
    type ControllerProvider() = 
        abstract member Create : spec:ControllerCreationSpec -> Func<ControllerCreationContext, ControllerPrototype>



    and [<AbstractClass; AllowNullLiteral>]
        ControllerExecutor() = 
        class
            abstract member Execute : action:string * controller:ControllerPrototype * route_data:RouteMatch * context:HttpContextBase -> obj

            interface IDisposable with 
                override this.Dispose() = ()
        end


    
    and [<AbstractClass; AllowNullLiteral>]
        ControllerExecutorProvider() = 
        class
            abstract member Create : prototype:ControllerPrototype -> ControllerExecutor
        end



    and [<AbstractClass; AllowNullLiteral>]
        ControllerPrototype(controller:obj) =
        class
            let _meta = lazy Dictionary<string,obj>(StringComparer.OrdinalIgnoreCase)
            member this.Metadata = _meta.Force() :> IDictionary<string,obj>
            member this.Instance = controller
            abstract member SupportsAction : name:string-> bool
            
        end



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
    open Castle.MonoRail.Hosting

    /// Aggregates all the Controller Providers to expose a single resolution API
    [<Export; AllowNullLiteral>]
    type ControllerProviderAggregator() = 
        let mutable _controllerProviders = Enumerable.Empty<Lazy<ControllerProvider, IComponentOrder>>()

        let try_create spec =
            let try_create_controller (p:Lazy<ControllerProvider, IComponentOrder>) = 
                let controller = p.Value.Create spec
                if controller <> null then Some(controller) else None
            match _controllerProviders |> Seq.tryPick try_create_controller with
            | Some controller -> controller
            | None -> null

        [<ImportMany(AllowRecomposition=true)>]
        member this.ControllerProviders
            with get() = _controllerProviders and set(v) = _controllerProviders <- Helper.order_lazy_set v
        
        member this.CreateController(spec) = 
            try_create spec


    /// Aggregates all the Controller Executor Providers to expose a single resolution API
    [<Export; AllowNullLiteral>]
    type ControllerExecutorProviderAggregator() = 
        let mutable _controllerExecProviders = Enumerable.Empty<Lazy<ControllerExecutorProvider, IComponentOrder>>()

        let select_executor_provider prototype = 
            let try_create_executor (p:Lazy<ControllerExecutorProvider, IComponentOrder>) = 
                let executor = p.Value.Create(prototype)
                if executor <> null then Some(executor) else None
            match _controllerExecProviders |> Seq.tryPick try_create_executor with
            | Some executor -> executor
            | None -> null

        [<ImportMany(AllowRecomposition=true)>]
        member this.ControllerExecutorProviders
            with get() = _controllerExecProviders and set(v) = _controllerExecProviders <- Helper.order_lazy_set v

        member x.CreateExecutor (prototype) = 
            select_executor_provider prototype

