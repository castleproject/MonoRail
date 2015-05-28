namespace ODataTestWebSite
{
	using Castle.MonoRail;
	using Castle.MonoRail.Routing;

	public class Global : MrBasedHttpApplication
	{
		public override void ConfigureRoutes(Router router)
		{
			router.Match("/models/:controller/**", "odataroute",
				c => c.Invariables(d => d.Action("process")))
				;

			router.Match("(/:controller(/:action(/:id)))", "default",
				c => c.Defaults(d => d.Controller("home").Action("index")))
				;

			/*
			router.Match("/viewcomponents/:controller(/:action(/:id))",
								  c =>
								  c.Match("(/:area/:controller(/:action(/:id)))", "viewcomponents",
										  ic => ic.Defaults(d => d.Action("index"))));
			*/
		}
	}
}