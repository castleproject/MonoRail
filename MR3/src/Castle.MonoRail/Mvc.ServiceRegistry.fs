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
    
    open System.Web
    open System.Collections.Generic
    open System.Collections.Specialized
    open Castle.MonoRail
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.ViewEngines
    open Castle.MonoRail.Serialization
    open Castle.MonoRail.Hosting.Mvc.Typed

    [<Interface;AllowNullLiteral>]
    type public IServiceRegistry =
        abstract member ViewEngines : IViewEngine seq with get
        abstract member ViewFolderLayout : IViewFolderLayout
        abstract member ViewRendererService : ViewRendererService
        abstract member ModelSerializerResolver : IModelSerializerResolver
        abstract member ModelHypertextProcessorResolver : ModelHypertextProcessorResolver
        abstract member ContentNegotiator : ContentNegotiator
        abstract member ViewComponentExecutor : ViewComponentExecutor
        abstract member ModelMetadataProvider : ModelMetadataProvider
        // abstract member ModelValidationMetadataProvider : ModelValidationMetadataProvider
        abstract member ControllerProvider : ControllerProviderAggregator
        abstract member ControllerExecutorProvider : ControllerExecutorProviderAggregator
        abstract member ControllerDescriptorBuilder : TypedControllerDescriptorBuilder
        // abstract member Get : service:'T -> 'T
        // abstract member GetAll : service:'T -> 'T seq
        abstract member SatisfyImports : instance:obj -> unit

