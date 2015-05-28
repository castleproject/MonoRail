namespace Castle.MonoRail.Tests.Mvc
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using FluentAssertions;
	using Framework;
	using Hosting.Mvc;
	using Hosting.Mvc.Typed;
	using NUnit.Framework;

	[TestFixture]
	public class FilterProviderTestCase
	{
		private StubFilterActivator _activator;
		private StubDescriptorProvider _descProvider;
		private FilterProvider _filterProviderWithNoDescProvider, _filterProviderWith1DescProvider;

		[SetUp]
		public void Init()
		{
			_activator = new StubFilterActivator(Activator.CreateInstance);
			_descProvider = new StubDescriptorProvider();
			_filterProviderWithNoDescProvider = new FilterProvider(new Lazy<FilterDescriptorProvider, IComponentOrder>[0]);
			_filterProviderWith1DescProvider = new FilterProvider(new[]
			                            	{
			                            		new Lazy<FilterDescriptorProvider, IComponentOrder>(() => _descProvider, new ComponentOrder(1))
			                            	});
		}

		private IEnumerable<TFilter> GetFilters<TFilter>(FilterProvider from) where TFilter : class
		{
			return from.Provide<TFilter>(
				_activator,
				new ActionExecutionContext(
						new FakeActionDescriptor("index"), 
						new TypedControllerPrototype(new TypedControllerDescriptor(typeof(object)), new object()), new StubHttpContext(), null));
		}

		[Test]
		public void Provide_EmptyDescriptorProviders_ReturnsEmptySet()
		{
			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWithNoDescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void Provide_OneDescriptorProviderThatReturnZeroProviders_ReturnsEmptySet()
		{
			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void Provide_OneDescriptorProviderThatReturn1AuthProvider_ReturnsAuthFilter()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().HaveCount(1);
			var filter = filters.ElementAt(0);
			filter.Should().NotBeNull();
			filter.Should().BeOfType<FakeAuthFilter>();
		}

		[Test]
		public void ProvideForActionFilter_OneDescriptorProviderThatReturn1AuthProvider_ReturnsEmptySet()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));

			var filters = GetFilters<IActionFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void ProvideForAuthFilter_OneWithFilterAndMatchingSkip_ReturnsEmptySet()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter)));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void ProvideForAuthFilter_Ordering_ReturnsCorrectOrdering()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter2), 2, null));
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().HaveCount(2);
			filters.ElementAt(0).Should().BeOfType<FakeAuthFilter>();
			filters.ElementAt(1).Should().BeOfType<FakeAuthFilter2>();
		}

		[Test]
		public void ProvideForAuthFilter_MultipleFiltersWithOneMatchingSkip_ReturnsNonSkippedFilters()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter2), 2, null));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter)));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().HaveCount(1);
			filters.ElementAt(0).Should().BeOfType<FakeAuthFilter2>();
		}

		[Test]
		public void ProvideForAuthFilter_MultipleFiltersWithMatchingSkips_ReturnsEmptySet()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter2), 2, null));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter)));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter2)));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void Provide_OneDescriptorProviderThatReturn1AuthProvider_Instance_ReturnsAuthFilter()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeType(typeof(FakeAuthFilter), 1, null));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().HaveCount(1);
			var filter = filters.ElementAt(0);
			filter.Should().NotBeNull();
			filter.Should().BeOfType<FakeAuthFilter>();
		}

		[Test]
		public void ProvideForActionFilter_OneDescriptorProviderThatReturn1AuthProvider_Instance_ReturnsEmptySet()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter(), 1));

			var filters = GetFilters<IActionFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void ProvideForAuthFilter_OneWithFilterAndMatchingSkip_Instance_ReturnsEmptySet()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter(), 1));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter)));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}

		[Test]
		public void ProvideForAuthFilter_Ordering_Instance_ReturnsCorrectOrdering()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter2(), 2));
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter(), 1));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().HaveCount(2);
			filters.ElementAt(0).Should().BeOfType<FakeAuthFilter>();
			filters.ElementAt(1).Should().BeOfType<FakeAuthFilter2>();
		}

		[Test]
		public void ProvideForAuthFilter_MultipleFiltersWithOneMatchingSkip_Instance_ReturnsNonSkippedFilters()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter(), 1));
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter2(), 2));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter)));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().HaveCount(1);
			filters.ElementAt(0).Should().BeOfType<FakeAuthFilter2>();
		}

		[Test]
		public void ProvideForAuthFilter_MultipleFiltersWithMatchingSkips_Instance_ReturnsEmptySet()
		{
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter(), 1));
			_descProvider.Descriptors.Add(FilterDescriptor.NewIncludeInstance(new FakeAuthFilter2(), 2));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter)));
			_descProvider.Descriptors.Add(FilterDescriptor.NewSkip(typeof(FakeAuthFilter2)));

			var filters = GetFilters<IAuthorizationFilter>(_filterProviderWith1DescProvider);

			filters.Should().NotBeNull();
			filters.Should().BeEmpty();
		}


		class FakeAuthFilter : IAuthorizationFilter
		{
			public void AuthorizeRequest(PreActionFilterExecutionContext context)
			{
			}
		}

		class FakeAuthFilter2 : IAuthorizationFilter
		{
			public void AuthorizeRequest(PreActionFilterExecutionContext context)
			{
			}
		}
	}
}
