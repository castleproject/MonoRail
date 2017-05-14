namespace BundledWebSite1.Controllers
{
	using Castle.MonoRail;

	public class BundleController
	{
		public ActionResult Index()
		{
			return new ViewResult();
		}
	}
}