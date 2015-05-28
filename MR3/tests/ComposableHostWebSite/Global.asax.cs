namespace ComposableHostWebSite
{
	using System;
	using System.IO;
	using Castle.Extensibility.Hosting;
	using Castle.Extensibility.Services.Configuration;
	using Castle.MonoRail;
	using Castle.MonoRail.Hosting;
	using Castle.MonoRail.Hosting.Container;
	using Castle.MonoRail.Routing;

	public class Global : MrBasedHttpApplication
	{
		public override void ConfigureRoutes(Router router)
		{
			Router.Instance.Match(
				path: "/secure/:controller(/:action(/:key))",
				name: "sec_default",
				config: c => c.Defaults(d => d.Action("index")).Invariables(d => d.Area("secure"))
			)
				// .WithAuthorizationFilter<AuthorizationFilter>()
			;

			Router.Instance.Match(
				path: "(/:controller(/:action(/:key)))",
				name: "default",
				config: c => c.Defaults(d => d.Controller("home").Action("index"))
			);
		}

		public override void Initialize()
		{
			var bin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
			var app = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundles");
			var appdata = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");

			var rootCatalog = Castle.MonoRail.Hosting.Container.Container.BuildMrCatalog(bin);
			var container = new HostingContainer(app, rootCatalog);

			container.AddFrameworkService<IConfigurationService>(name => new DotNetBasedConfigService(name, appdata));
			container.AddSupportedBehavior(new MonoRailBundleBehavior());

			this.CustomContainer = new HostingContainerAdapter(container);
		}	
	}
}