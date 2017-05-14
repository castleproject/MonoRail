namespace Castle.MonoRail.Integration.Tests
{
	using System;
	using System.IO;
	using System.Net;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture, Category("Integration")]
	public class HostingWebSite : BaseServerTest
	{
		public HostingWebSite()
		{
			this.WebSiteFolder = "ComposableHostWebSite";
		}

		[Test]
		public void InvokesControllerOnShell()
		{
			var req = WebRequest.CreateDefault(new Uri(BuildUrl("/")));
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("This is content from the shell app");			
		}
	}

	
}
