namespace BundledWebSite1
{
	using System;
	using Castle.MonoRail;
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

	}
}