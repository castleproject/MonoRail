namespace Castle.MonoRail.Tests.Internal
{
	using System.Collections.Generic;
	using System.Linq;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class RefHelpersTestCase
	{
		[Test]
		public void properties_from_exp__SimpleProp()
		{
			var propInfp = RefHelpers.properties_from_exp<Catalog1, object>(c => c.Name);
			propInfp.Should().NotBeNull();
			propInfp.Length.Should().Be(1);
			propInfp.ElementAt(0).Name.Should().Be("Name");
		}

		[Test]
		public void properties_from_exp__NestedObj()
		{
			var propInfp = RefHelpers.properties_from_exp<Catalog1, object>(c => c.Address.City);
			propInfp.Should().NotBeNull();
			propInfp.Length.Should().Be(2);
			propInfp.ElementAt(0).Name.Should().Be("Address");
			propInfp.ElementAt(1).Name.Should().Be("City");
		}

		[Test]
		public void lastpropinfo_from_exp__SimpleProp()
		{
			var propInfp = RefHelpers.lastpropinfo_from_exp<Catalog1, object>(c => c.Name);
			propInfp.Should().NotBeNull();
			propInfp.Name.Should().Be("Name");
		}

		[Test]
		public void lastpropinfo_from_exp__NestedObj()
		{
			var propInfp = RefHelpers.lastpropinfo_from_exp<Catalog1, object>(c => c.Address.City);
			propInfp.Should().NotBeNull();
			propInfp.Name.Should().Be("City");
		}

		[Test]
		public void lastpropinfo_from_exp__CollNestedObj()
		{
			var propInfp = RefHelpers.lastpropinfo_from_exp<Catalog1, object>(c => c.Addresses.Single().Street);
			propInfp.Should().NotBeNull();
			propInfp.Name.Should().Be("Street");
		}

		class Catalog1
		{
			public string Name { get; set; }
			public Address1 Address { get; set; }
			public IEnumerable<Address1> Addresses { get; set; }
		}
		class Address1
		{
			public string City { get; set; }
			public string Street { get; set; }
		}
	}
}
