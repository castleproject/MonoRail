namespace WebSiteForIntegration.Controllers
{
	using System;
	using System.IO;
	using System.Net;
	using Castle.MonoRail;

	public partial class RootController
	{
		public ActionResult Index()
		{
			return new TextWriterResult(WriteMsg);
		}

		public ActionResult ReplyWith304()
		{
			return new HttpResult(HttpStatusCode.NotModified);
		}

		public ActionResult ActionWithRedirect()
		{
			return new RedirectResult(Urls.Index.Get());
		}

		public ActionResult ActionWithRedirect2()
		{
			return new RedirectResult(Urls.ReplyWith304.Get());
		}

		public ActionResult ActionWithRedirectPerm()
		{
			return new PermRedirectResult(Urls.Index.Get());
		}

		public ActionResult ActionWithRedirectPerm2()
		{
			return new PermRedirectResult(Urls.ReplyWith304.Get());
		}


		private void WriteMsg(TextWriter writer)
		{
			writer.Write("Howdy");
			writer.Flush();
		}
	}
}