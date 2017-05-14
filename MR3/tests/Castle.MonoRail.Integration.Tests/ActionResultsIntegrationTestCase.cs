namespace Castle.MonoRail.Integration.Tests
{
	using System;
	using System.IO;
	using System.Net;
	using FluentAssertions;
	using NUnit.Framework;
	using WebSiteForIntegration.Controllers;

	[TestFixture, Category("Integration")]
	public class ActionResultsIntegrationTestCaseWithVPath : ActionResultsIntegrationTestCase
	{
		protected override string AppPath
		{
			get { return "/app"; }
		}
	}

	[TestFixture, Category("Integration")]
	public class ActionResultsIntegrationTestCase : BaseServerTest
	{
		[Test]
		public void OutputWriterResult_WritesBack()
		{
			var controller = new RootController();
			controller.Index().Should().BeOfType<TextWriterResult>();

			var req = WebRequest.CreateDefault(new Uri(BuildUrl("/")));
			var reply = (HttpWebResponse) req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			reply.ContentType.Should().StartWithEquivalent("text/html");
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Be("Howdy");
		}

		[Test, ExpectedException(typeof(WebException), ExpectedMessage = "The remote server returned an error: (304) Not Modified.")]
		public void HttpResult_WritesStatusCode()
		{
			var controller = new RootController();
			controller.ReplyWith304().Should().BeOfType<HttpResult>();

			var req = WebRequest.CreateDefault(new Uri(BuildUrl("/root/replywith304")));
			var reply = (HttpWebResponse)req.GetResponse();
		}

		[Test]
		public void RedirectResult_WritesBack()
		{
			var req = (HttpWebRequest) WebRequest.CreateDefault(new Uri(BuildUrl("/root/actionwithredirect")));
			req.AllowAutoRedirect = false;
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.Found);
			reply.Headers["Location"].Should().Be(BuildVirtualPath(""));
		}

		[Test]
		public void RedirectResult2_WritesBack()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/root/ActionWithRedirect2")));
			req.AllowAutoRedirect = false;
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.Found);
			reply.Headers["Location"].Should().Be(BuildVirtualPath("/Root/ReplyWith304"));
		}

		[Test]
		public void PermRedirectResult_WritesBack()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/root/ActionWithRedirectPerm")));
			req.AllowAutoRedirect = false;
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
			reply.Headers["Location"].Should().Be(BuildVirtualPath(""));
		}

		[Test]
		public void PermRedirectResult2_WritesBack()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/root/ActionWithRedirectPerm2")));
			req.AllowAutoRedirect = false;
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
			reply.Headers["Location"].Should().Be(BuildVirtualPath("/Root/ReplyWith304"));
		}
	}
}