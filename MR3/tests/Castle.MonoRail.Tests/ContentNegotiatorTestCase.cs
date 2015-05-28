namespace Castle.MonoRail.Tests
{
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class ContentNegotiatorTestCase
    {
		[Test]
		public void QualityTest_WhenAllEqual_SelectFirst()
		{
			var negotiator = new ContentNegotiator();
			var mime = negotiator.ResolveBestContentType(
						accept: new[] { "text/html", "text/plain", "app/xml" },
						supportedMediaTypes: new[] { "text/html", "text/plain", "app/xml" });
			mime.Should().Be("text/html");
		}

		[Test]
		public void QualityTest_WithFactors_SelectsBest()
		{
			var negotiator = new ContentNegotiator();
			var mime = negotiator.ResolveBestContentType(
						accept: new[] { "text/html;q=0.5", "text/plain;q=0.6", "app/xml;q=0.7" },
						supportedMediaTypes: new[] { "text/html", "text/plain", "app/xml" });
			mime.Should().Be("app/xml");

			mime = negotiator.ResolveBestContentType(
						accept: new[] { "text/html;q=0.5", "text/plain;q=0.6", "app/xml;q=0.7" },
						supportedMediaTypes: new[] { "app/xml", "text/html", "text/plain" });
			mime.Should().Be("app/xml");

			mime = negotiator.ResolveBestContentType(
						accept: new[] { "text/html;q=0.5", "text/plain;q=0.6", "app/xml;q=0.7" },
						supportedMediaTypes: new[] { "text/html", "app/xml", "text/plain" });
			mime.Should().Be("app/xml");
		}

		[Test]
		public void QualityTest_WithFactorsAndWildcards_SelectsBest()
		{
			var negotiator = new ContentNegotiator();
			var mime = negotiator.ResolveBestContentType(
						accept: new[] { "application/xml", "text/plain;q=0.9", "*/*;q=0.5" },
						supportedMediaTypes: new[] { "text/html", "text/plain", "application/xml" });
			mime.Should().Be("application/xml");

			mime = negotiator.ResolveBestContentType(
						accept: new[] { "application/xml", "text/plain;q=0.9", "*/*;q=0.5" },
						supportedMediaTypes: new[] { "text/html", "text/plain" });
			mime.Should().Be("text/plain");

			// ambiguous
//			mime = negotiator.ResolveBestContentType(
//						accept: new[] { "application/xml", "text/plain;q=0.9", "*/*;q=0.5" },
//						supportedMediaTypes: new[] { "text/html", "text/rdf" });
//			mime.Should().Be("text/html");
		}

        [Test]
        public void QualityTest_ShouldSelectTextHtml()
        {
            var negotiator = new ContentNegotiator();
            var mime = negotiator.ResolveBestContentType(
                        accept: new[] { "text/*", "text/html", "text/html;level=1", "*/*" },
						supportedMediaTypes: new[] { "text/html" });
			mime.Should().Be("text/html");
        }

        /**
        Accept: text/*;q=0.3, text/html;q=0.7, text/html;level=1,
                text/html;level=2;q=0.4, * / *;q=0.5
        would cause the following values to be associated:

        text/html;level=1         = 1
        text/html                 = 0.7
        text/plain                = 0.3

        image/jpeg                = 0.5
        text/html;level=2         = 0.4
        text/html;level=3         = 0.7
         */
        [Test]
		public void QualityTest_ShouldSelectTextHtml2()
        {
			var negotiator = new ContentNegotiator();
			var accept = "text/*;q=0.3, text/html;q=0.7, text/html;level=1, text/html;level=2;q=0.4, */*;q=0.5".Split(new [] {','}, System.StringSplitOptions.RemoveEmptyEntries);
			var mime = negotiator.ResolveBestContentType(
						accept,
						supportedMediaTypes: new[] { "text/html" });
			mime.Should().Be("text/html");
        }

		[Test]
		public void QualityTest_DenormalizingInputs()
		{
			var negotiator = new ContentNegotiator();
			var accept = "application/atom+xml, application/atom-xml, text/*;q=0.3, text/html; q=0.7, text/html;level= 1, text/html;level=2;q = 0.4, */*;q =0.5, text/*;charset= utf-8".Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
			negotiator.ResolveBestContentType(
						accept,
						supportedMediaTypes: new[] { "text/html" });
		}

		[TestCase("application/json")]
		[TestCase("application/json;level=1")]
		[TestCase("application/json; level=1")]
		[TestCase("application/json; charset=UTF-8")]
		[TestCase("application/json;charset=UTF-8")]
		public void NormalizeRequestContentType(string input)
		{
			var negotiator = new ContentNegotiator();
			negotiator.NormalizeRequestContentType(input).Should().Be("application/json");
		}

    }
}
