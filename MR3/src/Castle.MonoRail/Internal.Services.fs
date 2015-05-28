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

module Internal

    open System.ComponentModel.Composition
    open System.Web
    open System.Collections.Generic
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Serialization
    open Castle.MonoRail.ViewEngines
    open Castle.MonoRail.Hosting
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Typed

    [<Export(typeof<IServiceRegistry>)>]
    type ServiceRegistry() =
        let _viewEngines = ref (System.Linq.Enumerable.Empty<IViewEngine>())
        let _viewFolderLayout : Ref<IViewFolderLayout> = ref null
        let _viewRendererService : Ref<ViewRendererService> = ref null
        let _modelSerializerResolver : Ref<IModelSerializerResolver> = ref null
        let _modelHypertextProcessorResolver : Ref<ModelHypertextProcessorResolver> = ref null
        let _contentNegotiator : Ref<ContentNegotiator> = ref null
        let _vcExecutor : Ref<ViewComponentExecutor> = ref null
        let _modelMetadataProvider : Ref<ModelMetadataProvider> = ref null
        let _controllerProvider : Ref<ControllerProviderAggregator> = ref null
        let _controllerExecProvider : Ref<ControllerExecutorProviderAggregator> = ref null
        let _typedControllerDescriptorBuilder : Ref<TypedControllerDescriptorBuilder> = ref null
        let _expService : Ref<ICompositionService> = ref null
        let _lifetimeItems = Dictionary<string,obj>()

        [<Import(AllowRecomposition=true, AllowDefault=true)>]
        member x.ExpService                 with set v = _expService := v
        [<Import(AllowRecomposition=true, AllowDefault=true)>]
        member x.ControllerProvider         with set v = _controllerProvider := v
        [<Import(AllowRecomposition=true, AllowDefault=true)>]
        member x.ControllerExecutorProvider with set v = _controllerExecProvider := v
        [<Import(AllowRecomposition=true)>]
        member x.ModelMetadataProvider      with set v = _modelMetadataProvider := v
        [<ImportMany(AllowRecomposition=true)>]
        member x.ViewEngines                with set v = _viewEngines := v
        [<Import(AllowRecomposition=true)>]
        member x.ViewFolderLayout           with set v = _viewFolderLayout := v
        [<Import(AllowRecomposition=true)>]
        member x.ViewRendererService        with set v = _viewRendererService := v
        [<Import(AllowRecomposition=true)>]
        member x.ModelSerializerResolver    with set v = _modelSerializerResolver := v
        [<Import(AllowRecomposition=true)>]
        member x.ContentNegotiator          with set v = _contentNegotiator := v
        [<Import(AllowRecomposition=true)>]
        member x.ViewComponentExecutor      with set v = _vcExecutor := v
        [<Import(AllowRecomposition=true)>]
        member x.ModelHypertextProcessorResolver with set v = _modelHypertextProcessorResolver := v
        [<Import(AllowRecomposition=true)>]
        member x.ControllerDescriptorBuilder with set v = _typedControllerDescriptorBuilder := v

        interface IServiceRegistry with 
            member x.ViewEngines                = !_viewEngines
            member x.ViewFolderLayout           = !_viewFolderLayout
            member x.ViewRendererService        = !_viewRendererService
            member x.ModelSerializerResolver    = !_modelSerializerResolver
            member x.ContentNegotiator          = !_contentNegotiator
            member x.ViewComponentExecutor      = !_vcExecutor
            member x.ModelMetadataProvider      = !_modelMetadataProvider
            member x.ControllerProvider         = !_controllerProvider
            member x.ControllerExecutorProvider = !_controllerExecProvider
            member x.ModelHypertextProcessorResolver = !_modelHypertextProcessorResolver
            member x.ControllerDescriptorBuilder = !_typedControllerDescriptorBuilder
            member x.LifetimeItems              = _lifetimeItems

            member x.SatisfyImports (instance) = 
                if !_expService = null then failwith "_expService not set. Make sure your container exposes an implementation of ICompositionService"
                (!_expService).SatisfyImportsOnce(instance) |> ignore


    type EnvironmentServicesAppLevelBridge() =
        let _deploymentInfo : Ref<IDeploymentInfo> = ref null

        [<Import(AllowDefault=true)>]
        member x.DeploymentInfo with get() = !_deploymentInfo and set(v) = _deploymentInfo := v

        [<Export("AppPath")>]
        member x.AppPath = 
            if HttpContext.Current <> null 
            then HttpContext.Current.Request.ApplicationPath
            else ""

        [<Export("ContextualAppPath")>]
        member x.ContextualAppPath = 
            if !_deploymentInfo = null 
            then x.AppPath
            else (!_deploymentInfo).VirtualPath

        [<Export>]
        member x.HttpServer : HttpServerUtilityBase = 
            if HttpContext.Current <> null 
            then upcast HttpServerUtilityWrapper(HttpContext.Current.Server) 
            else null
            
        [<Export>]
        member x.Router = Router.Instance 

        [<Export>]
        member x.HttpApp = 
            if HttpContext.Current <> null 
            then HttpContext.Current.ApplicationInstance
            else null



    [<PartMetadata("Scope", ComponentScope.Request)>]
    type EnvironmentServicesRequestLevelBridge() =

        let flash = lazy 
                        let session = HttpContext.Current.Session
                        let flash = 
                            if session <> null 
                            then Flash(session.["flash__"] :?> Flash);
                            else Flash()
                        session.["flash__"] <- flash
                        flash
        [<Export>]
        member x.HttpContext : HttpContextBase = 
            upcast HttpContextWrapper(HttpContext.Current) 

        [<Export>]
        member x.HttpRequest : HttpRequestBase = 
            upcast HttpRequestWrapper(HttpContext.Current.Request) 

        [<Export>]
        member x.HttpResponse : HttpResponseBase = 
            upcast HttpResponseWrapper(HttpContext.Current.Response) 

        [<Export>]
        member x.HttpSession : HttpSessionStateBase = 
            upcast HttpSessionStateWrapper(HttpContext.Current.Session) 

        [<Export>]
        member x.RouteMatch : RouteMatch = 
            HttpContext.Current.Items.[Constants.MR_Routing_Key] :?> RouteMatch

        [<Export(typeof<Flash>)>]
        member x.Flash = 
            // bad property with side effects
            flash.Force()


