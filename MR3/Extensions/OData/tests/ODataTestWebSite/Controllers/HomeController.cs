namespace ODataTestWebSite.Controllers
{
	using Castle.MonoRail;

	public partial class HomeController
	{
		public ActionResult Index()
		{
			return new ViewResult();
		}
	}
}