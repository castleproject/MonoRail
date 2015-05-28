
namespace Castle.MonoRail.Tests.ParameterValueProviders
{
	using System.Collections.Generic;
	using FluentAssertions;
	using Hosting.Mvc.Typed;
	using NUnit.Framework;

	[TestFixture]
	public class SerializerValueProviderTestCase
	{
		private IParameterValueProvider _provider;
		private StubModelSerializerResolver _resolver;
		private StubHttpContext.HttpRequestStub _request;

		[SetUp]
		public void Init()
		{
			_request = new StubHttpContext.HttpRequestStub();
			_request.ContentType = MediaTypes.JSon;

			var provider = new SerializerValueProvider(_request);
			_resolver = new StubModelSerializerResolver();
			provider.ContentNegotiator = new ContentNegotiator();
			provider.ModelSerializerResolver = _resolver;
			_provider = provider;
		}

		[Test]
		public void ModelWrappedElement_()
		{
			object value = null;
			var result = _provider.TryGetValue("account", typeof (Model<Account>), out value);
			result.Should().BeTrue();
			value.Should().NotBeNull();
			value.Should().BeOfType<Model<Account>>();
		}

		[Test, Ignore("Deserialize cant handle IEnumerable")]
		public void ModelWrappedEnumerable_()
		{
			object value = null;
			var result = _provider.TryGetValue("account", typeof(Model<IEnumerable<Account>>), out value);
			result.Should().BeTrue();
			value.Should().NotBeNull();
			value.Should().BeOfType<Model<IEnumerable<Account>>>();
		}

		[Test]
		public void ModelWrappedCollection_()
		{
			object value = null;
			var result = _provider.TryGetValue("account", typeof(Model<List<Account>>), out value);
			result.Should().BeTrue();
			value.Should().NotBeNull();
			value.Should().BeOfType<Model<List<Account>>>();
		}

		class Account { }
	}
}
