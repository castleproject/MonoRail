#region License
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
#endregion

namespace Castle.MonoRail.Tests
{
	using System;
	using System.Collections.Generic;
    using Castle.MonoRail.Serialization;
    using Castle.MonoRail.ViewEngines;
	using Hosting.Mvc;
	using Hosting.Mvc.Typed;

	public class StubServiceRegistry : IServiceRegistry
    {
        public IEnumerable<IViewEngine> _viewEngines;
        public IViewFolderLayout _viewFolderLayout;
        public ViewRendererService _viewRendererService;
        public IModelSerializerResolver _modelSerializerResolver;
        public ModelHypertextProcessorResolver _modelHypertextProcessorResolver;
        public ContentNegotiator _contentNegotiator;
        public ViewComponentExecutor _viewComponentExecutor;
        public ModelMetadataProvider _modelMetadataProvider;
		private readonly Dictionary<string, object> _lifetime = new Dictionary<string, object>();

        public StubServiceRegistry()
        {
            _viewFolderLayout = new DefaultViewFolderLayout("");
            _viewRendererService = new ViewRendererService();
            _viewRendererService.ViewFolderLayout = _viewFolderLayout;

			this.ControllerDescriptorBuilder = new TypedControllerDescriptorBuilder();
			this.ControllerProvider = new ControllerProviderAggregator();
        }

        #region IServiceRegistry

		public Dictionary<string, object> LifetimeItems
		{
			get { return _lifetime; }
		}

		public IEnumerable<IViewEngine> ViewEngines
        {
            get { return _viewEngines; }
        }

        public IViewFolderLayout ViewFolderLayout
        {
            get { return _viewFolderLayout; }
        }

        public ViewRendererService ViewRendererService
        {
            get { return _viewRendererService; }
        }

        public IModelSerializerResolver ModelSerializerResolver
        {
            get { return _modelSerializerResolver; }
        }

        public ModelHypertextProcessorResolver ModelHypertextProcessorResolver
        {
            get { return _modelHypertextProcessorResolver; }
        }

        public ContentNegotiator ContentNegotiator
        {
            get { return _contentNegotiator; }
        }

        public ViewComponentExecutor ViewComponentExecutor
        {
            get { return _viewComponentExecutor; }
        }

        public ModelMetadataProvider ModelMetadataProvider
        {
            get { return _modelMetadataProvider; }
        }

    	public void SatisfyImports(object instance)
    	{
    		throw new NotImplementedException();
    	}

		public ControllerProviderAggregator ControllerProvider { get; set; }
		public ControllerExecutorProviderAggregator ControllerExecutorProvider { get; set; }
		public TypedControllerDescriptorBuilder ControllerDescriptorBuilder { get; set; }

		#endregion
    }
}
