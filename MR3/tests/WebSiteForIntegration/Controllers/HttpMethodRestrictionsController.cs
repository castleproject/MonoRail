namespace WebSiteForIntegration.Controllers
{
	using Castle.MonoRail;

	public partial class HttpMethodRestrictionsController
	{
		public ActionResult Get_Index()
		{
			return new TextWriterResult(w => w.WriteLine("succeeded - action prefixed with Get_"));
		}

		[HttpMethod(HttpVerb.Get)]
		public ActionResult Index2()
		{
			return new TextWriterResult(w => w.WriteLine("succeeded - action with httpmethodattribute (get)"));
		}

		public ActionResult Index3()
		{
			return new TextWriterResult(w => w.WriteLine("succeeded - action without att or prefix"));
		}

		public ActionResult Post_Index()
		{
			return new TextWriterResult(w => w.WriteLine("succeeded post - action prefixed"));
		}

		[HttpMethod(HttpVerb.Post | HttpVerb.Put)]
		public ActionResult Index4()
		{
			return new TextWriterResult(w => w.WriteLine("succeeded post/put - action with httpmethodattribute"));
		}
	}
}