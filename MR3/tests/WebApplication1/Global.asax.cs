namespace WebApplication1
{
	using System;
	using System.Web;
	using Castle.MonoRail;
	using Castle.MonoRail.Routing;
	using Filters;
	using Microsoft.Web.Optimization;

	public class Global : MrBasedHttpApplication
	{
		public override void Initialize()
		{
			BundleTable.Bundles.EnableDefaultBundles();

			Bundle indexBundle = new Bundle("~/styles", typeof(CssMinify));
			indexBundle.AddFile("~/Content/css/style.css");
			BundleTable.Bundles.Add(indexBundle);
		}

		public override void ConfigureRoutes(Router router)
		{
			Router.Instance.Match("/content/**", "toignore", Ignore.Instance);

			Router.Instance.Match("(/:controller(/:action(/:id)))", "default", 
								  c => c.Defaults(d => d.Controller("todo").Action("index")))
								  .WithActionFilter<TestActionFilter>()
								  .WithAuthorizationFilter<TestAuthFilter>()
								  .WithExceptionFilter<TestExceptionFilter>()
								  ;

			Router.Instance.Match("/viewcomponents/:controller(/:action(/:id))",
								  c =>
								  c.Match("(/:area/:controller(/:action(/:id)))", "viewcomponents",
										  ic => ic.Defaults(d => d.Action("index"))));
		}

		class Ignore : IRouteHttpHandlerMediator
		{
			public static Ignore Instance = new Ignore();

			public IHttpHandler GetHandler(HttpRequest request, RouteMatch routeData)
			{
				return null;
			}
		}
	}
}
