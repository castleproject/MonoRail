//  Copyright 2004-2012 Castle Project - http://www.castleproject.org/
//  Hamilton Verissimo de Oliveira and individual contributors as indicated. 
//  See the committers.txt/contributors.txt in the distribution for a 
//  full listing of individual contributors.
// 
//  This is free software; you can redistribute it and/or modify it
//  under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 3 of
//  the License, or (at your option) any later version.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this software; if not, write to the Free
//  Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
//  02110-1301 USA, or see the FSF site: http://www.fsf.org.

namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.Data.Services.Common;
	using System.Data.Services.Providers;
	using System.Linq;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class ODataMetadataBuilderTestCase
	{
		[Test]
		public void SingleEntityWith0PropsWithKey_BuildsResource()
		{
			var model = new StubModel(t =>
			                          	{
											t.EntitySet("name", new List<EntWithKey>().AsQueryable());
			                          	});

			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			var resType = result.ElementAt(0);
			resType.Should().NotBeNull();
			resType.Name.Should().Be("EntWithKey");
			resType.Namespace.Should().Be("TestNamespace");
			resType.ResourceTypeKind.Should().Be(ResourceTypeKind.EntityType);
			resType.Properties.Count.Should().Be(1);
			resType.Properties.ElementAt(0).Kind.Should().Be(ResourcePropertyKind.Primitive | ResourcePropertyKind.Key);
			resType.Properties.ElementAt(0).Name.Should().Be("Id");
		}

		[Test]
		public void SingleEntityWithAllSupportedEdmTypes_BuildsResource()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("MyEntity", new List<EntityWithSupportedEdmTypes>().AsQueryable());
			});

			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			var resType = result.ElementAt(0);
			resType.Should().NotBeNull();
			resType.Name.Should().Be("EntityWithSupportedEdmTypes");
			resType.Namespace.Should().Be("TestNamespace");
			resType.ResourceTypeKind.Should().Be(ResourceTypeKind.EntityType);
			resType.Properties.Count.Should().Be(7);
		}

		[Test]
		public void SingleEntityWithPrimitiveProperties_BuildsResource()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("name", new List<EntWithPropsKey>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			var resType = result.ElementAt(0);
			resType.Should().NotBeNull();
			resType.Name.Should().Be("EntWithPropsKey");
			resType.Namespace.Should().Be("TestNamespace");
			resType.ResourceTypeKind.Should().Be(ResourceTypeKind.EntityType);
			resType.Properties.Count.Should().Be(4);

			var id = resType.Properties.FirstOrDefault(p => p.Name == "Id");
			id.Kind.Should().Be(ResourcePropertyKind.Primitive | ResourcePropertyKind.Key);
			id.ResourceType.InstanceType.Should().Be<int>();
			
			var name = resType.Properties.FirstOrDefault(p => p.Name == "Name");
			name.Kind.Should().Be(ResourcePropertyKind.Primitive);
			name.ResourceType.InstanceType.Should().Be<string>();
			
			var age = resType.Properties.FirstOrDefault(p => p.Name == "Age");
			age.Kind.Should().Be(ResourcePropertyKind.Primitive);
			age.ResourceType.InstanceType.Should().Be<int>();
			
			var dob = resType.Properties.FirstOrDefault(p => p.Name == "DoB");
			dob.Kind.Should().Be(ResourcePropertyKind.Primitive);
			dob.ResourceType.InstanceType.Should().Be<DateTime>();
		}


		[Test]
		public void SingleEntityWithPrimitivePropertiesAndAttributes_BuildsResource()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("name", new List<EntWithPropsKey>().AsQueryable()).AddAttribute(
					new EntityPropertyMappingAttribute("Name", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, true));
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			var resType = result.ElementAt(0);
			resType.Should().NotBeNull();
			resType.Name.Should().Be("EntWithPropsKey");
			resType.Namespace.Should().Be("TestNamespace");
			resType.ResourceTypeKind.Should().Be(ResourceTypeKind.EntityType);
			resType.Properties.Count.Should().Be(4);
			;
			var d = resType.OwnEpmAttributes.Should().HaveCount(1);

			var id = resType.Properties.FirstOrDefault(p => p.Name == "Id");
			id.Kind.Should().Be(ResourcePropertyKind.Primitive | ResourcePropertyKind.Key);
			id.ResourceType.InstanceType.Should().Be<int>();

			var name = resType.Properties.FirstOrDefault(p => p.Name == "Name");
			name.Kind.Should().Be(ResourcePropertyKind.Primitive);
			name.ResourceType.InstanceType.Should().Be<string>();
			

			var age = resType.Properties.FirstOrDefault(p => p.Name == "Age");
			age.Kind.Should().Be(ResourcePropertyKind.Primitive);
			age.ResourceType.InstanceType.Should().Be<int>();

			var dob = resType.Properties.FirstOrDefault(p => p.Name == "DoB");
			dob.Kind.Should().Be(ResourcePropertyKind.Primitive);
			dob.ResourceType.InstanceType.Should().Be<DateTime>();
		}

		[Test]
		public void SingleEntityWithComplexTypeProperty_BuildsResource()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("name", new List<EntWithComplexPropKey>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			var resType = result.ElementAt(0);
			resType.Should().NotBeNull();
			resType.Name.Should().Be("EntWithComplexPropKey");
			resType.Namespace.Should().Be("TestNamespace");
			resType.ResourceTypeKind.Should().Be(ResourceTypeKind.EntityType);
			resType.Properties.Count.Should().Be(2);
			
			var id = resType.Properties.FirstOrDefault(p => p.Name == "Id");
			id.Kind.Should().Be(ResourcePropertyKind.Primitive | ResourcePropertyKind.Key);
			id.ResourceType.InstanceType.Should().Be<int>();

			var address = resType.Properties.FirstOrDefault(p => p.Name == "AddressProp");
			address.Kind.Should().Be(ResourcePropertyKind.ComplexType);
			address.ResourceType.InstanceType.Should().Be<Address>();

			address.ResourceType.Properties.Count.Should().Be(2);
			var address1 = address.ResourceType.Properties.FirstOrDefault(p => p.Name == "Address1");
			address1.Should().NotBeNull();
			address1.Kind.Should().Be(ResourcePropertyKind.Primitive);
			address1.ResourceType.InstanceType.Should().Be<string>();

			var city = address.ResourceType.Properties.FirstOrDefault(p => p.Name == "City");
			city.Should().NotBeNull();
			city.Kind.Should().Be(ResourcePropertyKind.Primitive);
			city.ResourceType.InstanceType.Should().Be<string>();
		}

		[Test]
		public void TwoEntitiesNotRelated_BuildsTwoResources()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("name1", new List<EntWithKey>().AsQueryable());
				t.EntitySet("name2", new List<EntWithPropsKey>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			result.Count().Should().Be(2);
		}

		[Test]
		public void TwoEntitiesRelatedByDirectResourceRef_CardinalityOne_BuildsTwoResources()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("product", new List<Product1>().AsQueryable());
				t.EntitySet("supplier", new List<Supplier1>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			result.Count().Should().Be(2);

			var productRT = result.FirstOrDefault(rt => rt.Name == "Product1");
			productRT.Should().NotBeNull();
			var supplierRT = result.FirstOrDefault(rt => rt.Name == "Supplier1");
			supplierRT.Should().NotBeNull();
			
			var supplierProp = productRT.Properties.FirstOrDefault(p => p.Name == "Supplier");
			supplierProp.Should().NotBeNull();
			supplierProp.Kind.Should().Be(ResourcePropertyKind.ResourceReference);
			supplierProp.ResourceType.Should().BeSameAs(supplierRT);
		}

		[Test]
		public void TwoEntitiesRelatedByDirectResourceRef_CardinalityMany_BuildsTwoResources()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("product", new List<Product2>().AsQueryable());
				t.EntitySet("supplier", new List<Supplier2>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			result.Count().Should().Be(2);

			var productRT = result.FirstOrDefault(rt => rt.Name == "Product2");
			productRT.Should().NotBeNull();
			var supplierRT = result.FirstOrDefault(rt => rt.Name == "Supplier2");
			supplierRT.Should().NotBeNull();

			var productsProp = supplierRT.Properties.FirstOrDefault(p => p.Name == "Products");
			productsProp.Should().NotBeNull();
			productsProp.Kind.Should().Be(ResourcePropertyKind.ResourceSetReference);
			productsProp.ResourceType.Should().BeSameAs(productRT);
		}

		[Test]
		public void TwoEntitiesReferencingEachOther_CardinalityOneAndMany_BuildsTwoResources()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("product", new List<Product3>().AsQueryable());
				t.EntitySet("supplier", new List<Supplier3>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			result.Count().Should().Be(2);

			var productRT = result.FirstOrDefault(rt => rt.Name == "Product3");
			productRT.Should().NotBeNull();
			var supplierRT = result.FirstOrDefault(rt => rt.Name == "Supplier3");
			supplierRT.Should().NotBeNull();

			var productsProp = supplierRT.Properties.FirstOrDefault(p => p.Name == "Products");
			productsProp.Should().NotBeNull();
			productsProp.Kind.Should().Be(ResourcePropertyKind.ResourceSetReference);
			productsProp.ResourceType.Should().BeSameAs(productRT);
		}

		[Test]
		public void ComplexType_()
		{
			var model = new StubModel(t =>
			{
				t.EntitySet("supplier", new List<Supplier4>().AsQueryable());
			});
			var result = ResourceMetadataBuilder.build(model.SchemaNamespace, model.Entities, Enumerable.Empty<Type>());

			result.Should().NotBeNull();
			result.Count().Should().Be(2);

			var addressRT = result.FirstOrDefault(rt => rt.Name == "Address4");
			addressRT.Should().NotBeNull();
			addressRT.ResourceTypeKind.Should().Be(ResourceTypeKind.ComplexType);

			var supplierRT = result.FirstOrDefault(rt => rt.Name == "Supplier4");
			supplierRT.Should().NotBeNull();

			var productsProp = supplierRT.Properties.FirstOrDefault(p => p.Name == "Address");
			productsProp.Should().NotBeNull();
			productsProp.Kind.Should().Be(ResourcePropertyKind.ComplexType);
			productsProp.ResourceType.Should().BeSameAs(addressRT);
		}


		public class EntWithKey
		{
			[Key]
			public int Id { get; set; }
		}

		public class EntWithPropsKey
		{
			[Key]
			public int Id { get; set; }

			public string Name { get; set; }
			public int Age { get; set; }
			public DateTime DoB { get; set; }
		}

		public class Address
		{
			public string Address1 { get; set; }
			public string City { get; set; }
		}

		public class EntWithComplexPropKey
		{
			[Key]
			public int Id { get; set; }

			public Address AddressProp { get; set; }
		}

		// ---------------------------------

		public class Supplier1
		{
			[Key]
			public int Id { get; set; }
		}

		public class Product1
		{
			[Key]
			public int Id { get; set; }
			public Supplier1 Supplier { get; set; }
		}

		// ---------------------------------

		public class Supplier2
		{
			[Key]
			public int Id { get; set; }
			public IList<Product2> Products { get; set; }
		}

		public class Product2
		{
			[Key]
			public int Id { get; set; }
		}
		
		// ---------------------------------

		public class Supplier3
		{
			[Key]
			public int Id { get; set; }
			public IEnumerable<Product3> Products { get; set; }
		}

		public class Product3
		{
			[Key]
			public int Id { get; set; }
			public Supplier3 Supplier { get; set; }
		}

		// ---------------------------------

		public class Address4
		{
			public string Street { get; set; }
			public string Zip { get; set; }
			public string Country { get; set; }
		}
		public class Supplier4
		{
			[Key]
			public int Id { get; set; }
			public Address4 Address { get; set; }
		}
		public class Catalog4
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public IList<Product4> Products { get; set; }
		}
		public class Product4
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
		}

		// ---------------------------------

		public class EntityWithSupportedEdmTypes
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public long LongProp { get; set; }
			public decimal DecimalProp { get; set; }
			public Single SingleProp { get; set; }
			public Double DoubleProp { get; set; }
			public DateTime DtProp { get; set; }
		}

	}
}
