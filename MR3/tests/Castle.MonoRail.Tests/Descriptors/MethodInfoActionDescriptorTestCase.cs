namespace Castle.MonoRail.Tests.Descriptors
{
	using System.Linq;
	using System.Reflection;
	using Castle.MonoRail.Hosting.Mvc;
	using Castle.MonoRail.Hosting.Mvc.Typed;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class MethodInfoActionDescriptorTestCase
	{
		private ControllerDescriptor _controllerDesc;
		private MethodInfo _indexAction, _actionPutAction, _action2Action;

		public MethodInfoActionDescriptorTestCase()
		{
			_controllerDesc = new TypedControllerDescriptorBuilder().Build(typeof(FakeController));
			_indexAction = typeof (FakeController).GetMethod("Index");
			_actionPutAction = typeof(FakeController).GetMethod("ActionPut");
			_action2Action = typeof(FakeController).GetMethod("Action2");

			Assert.NotNull(_indexAction);
			Assert.NotNull(_actionPutAction);
			Assert.NotNull(_action2Action);
		}

		[Test]
		public void SimpleAction()
		{
			var desc = new MethodInfoActionDescriptor(_indexAction, _controllerDesc);
			desc.Should().NotBeNull();
			desc._allowedVerbs.Should().BeEmpty();
		}

		[Test]
		public void ActionDecoratedWithHttpMethodAtt_Put()
		{
			var desc = new MethodInfoActionDescriptor(_actionPutAction, _controllerDesc);
			desc.Should().NotBeNull();
			desc._allowedVerbs.Should().NotBeEmpty();
			desc._allowedVerbs.Should().HaveCount(1);
			desc._allowedVerbs.ElementAt(0).Should().Be("PUT");
		}

		[Test]
		public void ActionDecoratedWithHttpMethodAtt_PutPost()
		{
			var desc = new MethodInfoActionDescriptor(_action2Action, _controllerDesc);
			desc.Should().NotBeNull();
			desc._allowedVerbs.Should().NotBeEmpty();
			desc._allowedVerbs.Should().HaveCount(2);
			desc._allowedVerbs.Should().Contain("PUT");
			desc._allowedVerbs.Should().Contain("POST");
		}

		public class FakeController
		{
			public ActionResult Index()
			{
				return null;
			}

			[HttpMethod(HttpVerb.Put)]
			public ActionResult ActionPut()
			{
				return null;
			}

			[HttpMethod(HttpVerb.Put | HttpVerb.Post)]
			public ActionResult Action2()
			{
				return null;
			}
		}
	}
}
