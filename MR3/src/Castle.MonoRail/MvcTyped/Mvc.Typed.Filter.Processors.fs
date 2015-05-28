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
    
    open System
    open System.Collections.Generic
    open System.Reflection
    open System.Web
    open System.ComponentModel.Composition
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Extensibility


    [<AbstractClass>]
    type BaseFilterProcessor
        (provider:FilterProvider, activators:Lazy<IFilterActivator, IComponentOrder> seq) =
        inherit ActionProcessor()

        let _ordered = Helper.order_lazy_set activators

        let _compositeActivator = 
            { 
                new IFilterActivator with 
                    member x.Activate(filterType) = 
                        match 
                            _ordered 
                            |> Seq.map (fun o -> o.Force()) 
                            |> Seq.tryPick (fun act -> let r = act.Activate<'TFilter>(filterType) 
                                                       if r != null then Some(r) else None)
                            with 
                        | Some(f) -> f
                        | None -> raise(MonoRailException((sprintf "Could not instantiate filter %O" filterType)))
            } 

        member x.CompositeActivator = _compositeActivator
        member x.CreateFilters(context) : 'TFilter seq = 
            provider.Provide(_compositeActivator, context)


    [<Export(typeof<ActionProcessor>)>]
    [<Export(typeof<AuthorizationFilterProcessor>)>]
    [<ExportMetadata("Order", Constants.ActionProcessor_AuthorizationFilter)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type AuthorizationFilterProcessor 
        [<ImportingConstructor>]
        (provider, [<ImportMany>] activators, resultProcessor:ActionResultExecutorProcessor) =
        inherit BaseFilterProcessor(provider, activators)

        override x.Process(context) = 
            let filters : IAuthorizationFilter seq = x.CreateFilters(context)
                
            if not <| Seq.isEmpty filters then 
                let canProceed = ref true
                let filterCtx = PreActionFilterExecutionContext(context)
                for f in filters do 
                    f.AuthorizeRequest(filterCtx)

                    // if the filter returned an action context, 
                    // we process it and stop right here
                    if filterCtx.ActionResult <> null then
                        canProceed := false
                        context.Result <- filterCtx.ActionResult
                        resultProcessor.Process(context)
                        
                if !canProceed then
                    base.ProcessNext(context)
            else
            
                base.ProcessNext(context)


    [<Export(typeof<ActionProcessor>)>]
    [<Export(typeof<ActionFilterProcessor>)>]
    [<ExportMetadata("Order", Constants.ActionProcessor_ActionFilter)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type ActionFilterProcessor
        [<ImportingConstructor>]
        (provider, [<ImportMany>] activators, resultProcessor:ActionResultExecutorProcessor) =
        inherit BaseFilterProcessor(provider, activators)

        override x.Process(context) = 
            let filters : IActionFilter seq = x.CreateFilters(context)

            try
                if not <| Seq.isEmpty filters then 
                    let filterCtx = PreActionFilterExecutionContext(context)
                    let canProceed = ref true
                    for f in filters do 
                        f.BeforeAction(filterCtx)
                        if filterCtx.ActionResult <> null then
                            canProceed := false
                            context.Result <- filterCtx.ActionResult
                            resultProcessor.Process(context)

                    if !canProceed then base.ProcessNext(context)
                else
                    base.ProcessNext(context)
            
            finally 
                // even in err, invoke the after action
                if not <| Seq.isEmpty filters then 
                    let filterCtx = AfterActionFilterExecutionContext(context)
                    for f in filters do 
                        f.AfterAction(filterCtx)

                
    [<Export(typeof<ActionProcessor>)>]
    [<Export(typeof<ExceptionFilterProcessor>)>]
    [<ExportMetadata("Order", Constants.ActionProcessor_ExecutionFilterProcessor)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type ExceptionFilterProcessor
        [<ImportingConstructor>]
        (provider, [<ImportMany>] activators, resultProcessor:ActionResultExecutorProcessor) as self =
        inherit BaseFilterProcessor(provider, activators)

        let run_filters (filters:IExceptionFilter seq) (context:ActionExecutionContext) (exc:Exception) =
            if not context.ExceptionHandled then
                context.Exception <- exc 
                let filterCtx = ExceptionFilterExecutionContext(context)
                for f in filters do 
                    f.HandleException(filterCtx)

                    if filterCtx.ExceptionHandled then
                        context.ExceptionHandled <- true

                    // we only process the action result if the filter signaled that it handled the exception
                    if filterCtx.ExceptionHandled && filterCtx.ActionResult <> null then
                        context.Result <- filterCtx.ActionResult
                        // context.Exception <- null
                        resultProcessor.Process(context)

            if not context.ExceptionHandled then
                raise(HttpUnhandledException("Unhandled exception processing request", context.Exception))

        override x.Process(context) = 
            let filters : IExceptionFilter seq = self.CreateFilters(context) 

            if Seq.isEmpty filters then 
                base.ProcessNext(context)
            else
                try
                    base.ProcessNext(context)

                    if context.Exception <> null && not context.ExceptionHandled then
                        run_filters filters context context.Exception
                with 
                | :? HttpUnhandledException as exc -> 
                    () // we dont catch this one
                | exc -> 
                    run_filters filters context exc
        
    

