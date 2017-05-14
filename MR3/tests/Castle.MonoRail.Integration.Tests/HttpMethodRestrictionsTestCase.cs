namespace Castle.MonoRail.Integration.Tests
{
	using System;
	using System.IO;
	using System.Net;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture, Category("Integration")]
	public class HttpMethodRestrictionsTestCase : BaseServerTest
	{
		[Test]
		public void InvokesGetActionPrefixed()
		{
			var req = WebRequest.CreateDefault(new Uri(BuildUrl("/HttpMethodRestrictions/index")));
			var reply = (HttpWebResponse) req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("succeeded - action prefixed with Get_");			
		}

		[Test]
		public void InvokesGetActionNotPrefixedAndNotAnnotated()
		{
			var req = WebRequest.CreateDefault(new Uri(BuildUrl("/HttpMethodRestrictions/Index3")));
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("succeeded - action without att or prefix");
		}

		[Test]
		public void InvokesGetActionAnnotated()
		{
			var req = WebRequest.CreateDefault(new Uri(BuildUrl("/HttpMethodRestrictions/Index2")));
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("succeeded - action with httpmethodattribute (get)");
		}

		[Test]
		public void InvokesPostActionPrefixed()
		{
			var req = (HttpWebRequest) WebRequest.CreateDefault(new Uri(BuildUrl("/HttpMethodRestrictions/index")));
			req.Method = "POST";
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("succeeded post - action prefixed");
		}

		[Test]
		public void InvokesPostActionAnnotated()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/HttpMethodRestrictions/index4")));
			req.Method = "POST";
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("succeeded post/put - action with httpmethodattribute");
		}

		[Test]
		public void InvokesPutActionAnnotated()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/HttpMethodRestrictions/index4")));
			req.Method = "PUT";
			var reply = (HttpWebResponse)req.GetResponse();

			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			new StreamReader(reply.GetResponseStream()).ReadToEnd().Should().Contain("succeeded post/put - action with httpmethodattribute");
		}
	}
}
