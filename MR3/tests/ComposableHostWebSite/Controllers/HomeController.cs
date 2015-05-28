namespace ComposableHostWebSite.Controllers
{
	using Castle.MonoRail;

	public class HomeController
	{
		public ActionResult Index()
		{
			return new ViewResult();
		}
	}
}