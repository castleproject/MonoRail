namespace Castle.MonoRail.Tests
{
	using System;
	using System.Web;
	using Castle.MonoRail.Hosting.Mvc;
	using Hosting.Mvc.Typed;

	public class FakeActionDescriptor : ControllerActionDescriptor
	{
		public FakeActionDescriptor(string name)
			: base(name, new TypedControllerDescriptor(typeof(FakeController)))
		{
		}

		public override bool SatisfyRequest(HttpContextBase context)
		{
			return true;
		}

		public override object Execute(object instance, object[] args)
		{
			return instance;
		}

		public class FakeController
		{
			
		}
	}
}
