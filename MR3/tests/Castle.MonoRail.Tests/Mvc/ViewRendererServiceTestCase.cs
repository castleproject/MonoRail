namespace Castle.MonoRail.Tests.Mvc
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using FluentAssertions;
	using NUnit.Framework;
	using ViewEngines;

	[TestFixture]
	public class ViewRendererServiceTestCase
	{
		[Test, ExpectedException(typeof(ViewEngineException), ExpectedMessage = "No view engines found.")]
		public void Render_NoViewEngineSet_Throws()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.Render(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index" }, 
				new StubHttpContext(), 
				new Dictionary<string, object>(), 
				new object(), 
				new StringWriter());
		}

		[Test, ExpectedException(typeof(ViewEngineException), ExpectedMessage = "No views found. Searched for [location.fake]")]
		public void Render_NotFindingView_Throws()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] {new StubViewEngine(
				(views, layouts) => new ViewEngineResult(new [] { "location.fake" }), 
				(views) => false)};

			service.Render(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index" },
				new StubHttpContext(),
				new Dictionary<string, object>(),
				new object(),
				new StringWriter());
		}

		[Test, ExpectedException(typeof(ViewEngineException), ExpectedMessage = "No views found. Searched for [location.fake]")]
		public void RenderPartial_NotFindingView_Throws()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] {new StubViewEngine(
				(views, layouts) => new ViewEngineResult(new [] { "location.fake" }), 
				(views) => false)};

			service.RenderPartial(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index", ViewName = "_some" },
				new StubHttpContext(),
				new Dictionary<string, object>(),
				new object(),
				new StringWriter());
		}

		[Test]
		public void Render_FindingView_Renders()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] { new StubViewEngine(
				(views, layouts) =>
					{
						views.ElementAt(0).Should().Be("/Views/home/index");
						views.ElementAt(1).Should().Be("/Views/home/_index");
                        views.ElementAt(2).Should().Be("/Views/Shared/index");

						return new ViewEngineResult(new StubView( (w) => w.Write("Done") ), null);
					}, 
				(views) => false) 
			};

			var writer = new StringWriter();

			service.Render<object>(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index" },
				new StubHttpContext(),
				new Dictionary<string, object>(),
				new object(),
				writer);

			writer.GetStringBuilder().ToString().Should().Be("Done");
		}

		[Test]
		public void RenderPartial_FindingView_Renders()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] { new StubViewEngine(
				(views, layouts) =>
					{
                        views.ElementAt(0).Should().Be("/Views/home/_some");
                        views.ElementAt(1).Should().Be("/Views/home/__some");
                        views.ElementAt(2).Should().Be("/Views/Shared/_some");

						return new ViewEngineResult(new StubView( (w) => w.Write("Done") ), null);
					}, 
				(views) => false) 
			};

			var writer = new StringWriter();

			service.RenderPartial(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index", ViewName = "_some" },
				new StubHttpContext(),
				new Dictionary<string, object>(),
				new object(),
				writer);

			writer.GetStringBuilder().ToString().Should().Be("Done");
		}

		[Test]
		public void HasView_FindingView_ReturnsTrue()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] { new StubViewEngine(
				(views, layouts) => null, 
				(views) =>
					{
                        views.ElementAt(1).Should().Be("/Views/home/_index");
                        views.ElementAt(2).Should().Be("/Views/Shared/index");
						
						return true;
					}) 
			};

			service.HasView(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index" },
				new StubHttpContext())
				.Should().BeTrue();
		}

		[Test]
		public void HasPartialView_FindingView_ReturnsTrue()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] { new StubViewEngine(
				(views, layouts) => null, 
				(views) =>
					{
						views.Contains("/Views/home/_some");
                        views.Contains("/Views/home/__some");
                        views.Contains("/Views/Shared/_some");
						
						return true;
					}) 
			};

			service.HasPartialView(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index", ViewName = "_some" },
				new StubHttpContext())
				.Should().BeTrue();
		}

		[Test]
		public void HasView_WithTwoVEs_FindingView_ReturnsTrue()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] { new StubViewEngine(
				(views, layouts) => null, 
				(views) =>
					{
                        views.Contains("/Views/home/index");
                        views.Contains("/Views/home/_index");
                        views.Contains("/Views/Shared/index");
						return false;
					}),

				new StubViewEngine(
				(views, layouts) => null, 
				(views) =>
					{
						views.Contains("/Views/home/index");
                        views.Contains("/Views/home/_index");
                        views.Contains("/Views/Shared/index");
						return true;
					}) 
			};

			service.HasView(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index" },
				new StubHttpContext())
				.Should().BeTrue();
		}

		[Test]
		public void HasView_WithTwoVEs_NotFindingView_ReturnsFalse()
		{
			var service = new ViewRendererService() { ViewFolderLayout = new DefaultViewFolderLayout("/") };
			service.ViewEngines = new[] { new StubViewEngine(
				(views, layouts) => null, 
				(views) =>
					{
                        views.Contains("/Views/home/index");
                        views.Contains("/Views/Shared/index");
						return false;
					}),

				new StubViewEngine(
				(views, layouts) => null, 
				(views) =>
					{
                        views.Contains("/Views/home/index");
                        views.Contains("/Views/home/_index");
                        views.Contains("/Views/Shared/index");
						return false;
					}) 
			};

			service.HasView(
				new ViewRequest() { ViewFolder = "home", DefaultName = "index" },
				new StubHttpContext())
				.Should().BeFalse();
		}
	}
}
