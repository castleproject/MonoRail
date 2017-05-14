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

namespace Castle.MonoRail.Extension.OData

    open System
    open System.Linq
    open System.Linq.Expressions
    open System.Collections
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.Data.OData
    open System.Data.Services.Providers
    open System.Reflection
    open System.Xml
    open Castle.MonoRail
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Extensibility
    open Castle.MonoRail.Hosting.Mvc.Typed


    type ODataParameterValueProvider(callback:Func<Type, obj>) = 
        interface IParameterValueProvider with
            member x.TryGetValue(name, paramType, value) = 
                let attempt = callback.Invoke(paramType)
                if attempt = null then 
                    value <- null
                    false
                else 
                    value <- attempt
                    true

    [<Export>] 
    [<PartMetadata("Scope", ComponentScope.Request)>] 
    type ODataEntitySubControllerExecutor 
        [<ImportingConstructor>] 
        ([<ImportMany(RequiredCreationPolicy=CreationPolicy.NonShared)>] actionMsgs:Lazy<ActionProcessor, IComponentOrder> seq) = 
        inherit ProcessorBasedControllerExecutor(actionMsgs)
        
        let mutable _getParamCallback : Func<Type, obj> = null
        let mutable _lifetime : ExportLifetimeContext<ODataEntitySubControllerExecutor> = null

        member this.GetParameterCallback with get() = _getParamCallback and set(v) = _getParamCallback <- v
        member this.Lifetime with get() = _lifetime and set(v) = _lifetime <- v

        override this.PruneProcessorList(original) = 
            // processors to consider removing:
            // - ExceptionFilterProcessor 
            match original |> Seq.tryFind (fun o -> o.Value :? ActionParameterBinderProcessor) with
            | Some processor -> 
                // We insert a ODataParameterValueProvider as the head of the value provider list
                let paramBinderProcessor = processor.Value :?> ActionParameterBinderProcessor
                let valueProviders = paramBinderProcessor.ValueProviders
                let head = 
                    Lazy<IParameterValueProvider, IComponentOrder>(
                        (fun _ -> ODataParameterValueProvider(_getParamCallback) :> IParameterValueProvider), 
                        { new IComponentOrder with member x.Order = 0 })
                
                paramBinderProcessor.ValueProviders <- head :: (List.ofSeq valueProviders)
            | _ -> ()
            original

        override x.Dispose() = 
            if _lifetime <> null then
                _lifetime.Dispose()
                _lifetime <- null


    [<ControllerExecutorProviderExport(8000000)>]
    type ODataEntitySubControllerExecutorProvider() = 
        inherit ControllerExecutorProvider()
        let mutable _execFactory : ExportFactory<ODataEntitySubControllerExecutor> = null
        let mutable _getParamCallback : Func<Type, obj> = null

        [<Import(RequiredCreationPolicy=CreationPolicy.NewScope)>]
        member this.ExecutorFactory
            with get() = _execFactory and set(v) = _execFactory <- v

        member x.GetParameterCallback with get() = _getParamCallback and set(v) = _getParamCallback <- v

        override this.Create(prototype) = 
            match prototype with
            | :? TypedControllerPrototype as inst_prototype ->
                let instance = inst_prototype.Instance
                let ctype = instance.GetType()
                let baseTypes = seq {    
                                    let baseType = ref ctype.BaseType
                                    if !baseType <> typeof<obj> then 
                                        yield !baseType
                                        baseType := (!baseType).BaseType
                                }
                let isSubController = 
                    baseTypes 
                    |> Seq.exists (fun t -> t.IsGenericType && typedefof<ODataEntitySubController<_>>.IsAssignableFrom( t.GetGenericTypeDefinition() ))

                if isSubController then
                    let exp = _execFactory.CreateExport()
                    let executor = exp.Value
                    executor.GetParameterCallback <- _getParamCallback
                    executor.Lifetime <- exp
                    upcast executor 
                else null
            | _ -> null



