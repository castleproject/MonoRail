namespace Castle.MonoRail.Integration.Tests
{
	using System;
	using System.IO;
	using CassiniDev;
	using NUnit.Framework;

	public abstract class BaseServerTest
	{
		private Server _server;

		protected BaseServerTest()
		{
			WebSiteFolder = "WebSiteForIntegration";
		}

		public Server Server
		{
			get { return _server; }
		}

		public string WebSiteFolder { get; set; }

		protected virtual int Port { get { return 1302; } }
		protected virtual string AppPath { get { return "/";  } }

		protected string BuildUrl(string path)
		{
			return "http://localhost:" + this.Port + BuildVirtualPath(path);
		}

		protected string BuildVirtualPath(string path)
		{
			var vpath = AppPath;
			if (vpath.EndsWith("/") && path.StartsWith("/"))
			{
				if (vpath.Length == 1)
					vpath = "";
				else
					vpath = vpath.Substring(0, vpath.Length - 1);
			}
			return vpath + path;
		}

		[TestFixtureSetUp]
		public void Init()
		{
			var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
			while (true)
			{
				if (!dir.Name.Equals("tests", StringComparison.OrdinalIgnoreCase))
				{
					dir = dir.Parent;
				}
				else break;
			}

			_server = new Server(this.Port, this.AppPath, Path.Combine(dir.FullName, this.WebSiteFolder), false, true);
			_server.Start();
		}

		[TestFixtureTearDown]
		public void Terminate()
		{
			if (_server != null) _server.Dispose();
		}
	}
}
