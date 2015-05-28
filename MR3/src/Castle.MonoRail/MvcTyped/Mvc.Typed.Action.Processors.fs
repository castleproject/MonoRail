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
    open System.Runtime.InteropServices
        
    
    [<Export(typeof<ActionProcessor>)>]
    [<Export(typeof<ActionParameterBinderProcessor>)>]
    [<ExportMetadata("Order", Constants.ActionProcessor_ActionParameterBinder)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type ActionParameterBinderProcessor() = 
        inherit ActionProcessor()
        let mutable _valueProviders = Seq.empty<Lazy<IParameterValueProvider,IComponentOrder>>

        let try_provide_param_value (valueProvider:IParameterValueProvider) (name) (paramType) = 
            let succeeded, value = valueProvider.TryGetValue(name, paramType)
            if succeeded then Some value else None

        let try_process_param (param:KeyValuePair<string,obj>) (paramDesc:ActionParameterDescriptor)  = 
            if param.Value = null then // if <> null, then a previous processor filled the value
                let name = param.Key
                match _valueProviders |> Seq.tryPick (fun vp -> try_provide_param_value vp.Value name paramDesc.ParamType) with
                | Some value -> Some(name, value) 
                | _ -> None
            else None

        [<ImportMany(AllowRecomposition=true)>]
        member x.ValueProviders 
            with get() = _valueProviders and set v = _valueProviders <- Helper.order_lazy_set v

        override x.Process(context:ActionExecutionContext) = 
            // uses the IParameterValueProvider to fill parameters for the actions
            let paramDescMap = context.ActionDescriptor.ParametersByName

            context.Parameters 
            |> Seq.choose (fun param -> try_process_param param (paramDescMap.[param.Key]) )
            |> Seq.toList
            |> List.iter   (fun (name, value) -> context.Parameters.[name] <- value)

            x.ProcessNext(context)


    [<Export(typeof<ActionProcessor>)>]
    [<Export(typeof<ActionExecutorProcessor>)>]
    [<ExportMetadata("Order", Constants.ActionProcessor_ActionExecutorProcessor)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type ActionExecutorProcessor [<ImportingConstructor>] ([<Import>] flash:Flash) =
        inherit ActionProcessor()

        override x.Process(context:ActionExecutionContext) = 
            
            let parameters = 
                context.ActionDescriptor.Parameters 
                |> Seq.map (fun p -> context.Parameters.[p.Name])
                |> Seq.toArray

            let mutable processNext = true

            try
                try
                    context.Result <- context.ActionDescriptor.Execute(context.Prototype.Instance, parameters)
                with
                | ex -> 
                    context.Exception <- ex

                    processNext <- false
                    x.ProcessNext(context)

                    if not context.ExceptionHandled then
                      reraise()

                if processNext then
                    x.ProcessNext(context)
            finally
                flash.Sweep()
            

    [<Export(typeof<ActionProcessor>)>]
    [<Export(typeof<ActionResultExecutorProcessor>)>]
    [<ExportMetadata("Order", Constants.ActionProcessor_ActionResultExecutorProcessor)>]
    [<PartMetadata("Scope", ComponentScope.Request)>]
    type ActionResultExecutorProcessor 
        [<ImportingConstructor>] (arExecutor:ActionResultExecutor) = 
        inherit ActionProcessor()

        let _actionResultExecutor = arExecutor

        override x.Process(context:ActionExecutionContext) = 
            // if context.Exception = null then
            let res = context.Result

            match res with 
            | :? ActionResult as ar -> 
                _actionResultExecutor.Execute(ar, context.ActionDescriptor, 
                                                context.Prototype, 
                                                context.RouteMatch, context.HttpContext)
            | _ -> 
                // we shouldnt really ignore, instead, do a default kind of action - rendering a view?
                ()

            x.ProcessNext(context)
