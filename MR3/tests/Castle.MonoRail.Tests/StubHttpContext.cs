namespace Castle.MonoRail.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Web;

    public class StubHttpContext : HttpContextBase
    {
        public class HttpRequestStub : HttpRequestBase
        {
            private string _path;
            private readonly NameValueCollection _form = new NameValueCollection();

            private string _httpMethod;

            public void _SetPath(string v)
            {
                _path = v;
            }
            public override string Path
            {
                get { return _path; }
            }
            public override NameValueCollection Form
            {
                get { return _form; }
            }

            public override string ContentType { get; set; }

            public override Stream InputStream
            {
                get { return new MemoryStream(); }
            }

            public override string HttpMethod
            {
                get { return _httpMethod; }
            }
            public void SetHttpMethod(string value)
            {
                _httpMethod = value;
            }
        }

        public class HttpResponseBaseStub : HttpResponseBase
        {
            private TextWriter _outputWriter = new StringWriter();

            public override TextWriter Output
            {
                get { return _outputWriter; }
                set { _outputWriter = value; }
            }
        }
        public class HttpServerUtilityStub : HttpServerUtilityBase
        {
            public override string HtmlEncode(string s)
            {
                return HttpUtility.HtmlEncode(s);
            }
        }

        public HttpRequestStub RequestStub = new HttpRequestStub();
        public HttpResponseBaseStub ResponseStub = new HttpResponseBaseStub();
        public HttpServerUtilityStub ServerUtilityStub = new HttpServerUtilityStub();

        public override HttpRequestBase Request
        {
            get { return RequestStub; }
        }

        public override HttpResponseBase Response
        {
            get { return ResponseStub; }
        }

        public override HttpServerUtilityBase Server
        {
            get { return ServerUtilityStub; }
        }
    }
}