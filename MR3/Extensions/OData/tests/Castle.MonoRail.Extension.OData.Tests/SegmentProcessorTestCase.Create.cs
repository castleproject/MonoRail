namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.Data.Services.Common;
	using System.IO;
	using System.Linq;
	using System.ServiceModel.Syndication;
	using System.Text;
	using System.Xml;
	using FluentAssertions;
	using NUnit.Framework;


	public partial class SegmentProcessorTestCase
	{
		// naming convention for testing methods
		// [EntitySet|EntityType|PropSingle|PropCollection|Complex|Primitive]_[Operation]_[InputFormat]_[OutputFormat]__[Success|Failure]

		[Test, Description("The EntityContainer only has Catalog, so creation is for nested object")]
		public void PropCollection_Create_Atom_Atom_Success()
		{
			var prod = new Product1()
			           	{
			           		Created = DateTime.Now, 
							Modified = DateTime.Now, 
							IsCurated = true, 
							Name = "testing", Price = 2.3m
			           	};

			Process("/catalogs(1)/Products/", SegmentOp.Create, _modelWithMinimalContainer, inputStream: prod.ToSyndicationItem().ToStream() );

			Assertion.ResponseIs(201, "application/atom+xml");

			// TODO: need to collect the containers, so controller can get all of them in the action call

			Assertion.Callbacks.CreateWasCalled(1);

			var deserializedProd = (Product1)_created.ElementAt(0).Item3;

			deserializedProd.Name.Should().Be(prod.Name);
			deserializedProd.IsCurated.Should().Be(prod.IsCurated);
			deserializedProd.Modified.Should().Be(prod.Modified);
			deserializedProd.Created.Should().Be(prod.Created);
			deserializedProd.Price.Should().Be(prod.Price);
		}

		[Test, Description("Id for products needs to refer back to EntityContainer.Products")]
		public void EntitySet_Create_Atom_Atom_Success()
		{
			var prod = new Product1()
			{
				Created = DateTime.Now,
				Modified = DateTime.Now,
				IsCurated = true,
				Name = "testing",
				Price = 2.3m
			};

			Process("/Products/", SegmentOp.Create, _model, inputStream: prod.ToSyndicationItem().ToStream());

			Assertion.ResponseIs(201, "application/atom+xml");

			Assertion.Callbacks.CreateWasCalled(1);

			var deserializedProd = (Product1)_created.ElementAt(0).Item3;

			deserializedProd.Name.Should().Be(prod.Name);
			deserializedProd.IsCurated.Should().Be(prod.IsCurated);
			deserializedProd.Modified.Should().Be(prod.Modified);
			deserializedProd.Created.Should().Be(prod.Created);
			deserializedProd.Price.Should().Be(prod.Price);
		}

		[Test, Description("Id for products needs to refer back to EntityContainer.Products")]
		public void EntitySet_Create_Json_Json_Success()
		{
			var prod = new Product1()
			{
				Created = DateTime.Now,
				Modified = DateTime.Now,
				IsCurated = true,
				Name = "testing",
				Price = 2.3m
			};

			Process("/Products/", SegmentOp.Create, _model, 
					accept: MediaTypes.JSon, contentType: MediaTypes.JSon, 
					inputStream: new MemoryStream(Encoding.UTF8.GetBytes(prod.ToJSon())));

			Assertion.ResponseIs(201, MediaTypes.JSon);

			Assertion.Callbacks.CreateWasCalled(1);

			var deserializedProd = (Product1)_created.ElementAt(0).Item3;

			deserializedProd.Name.Should().Be(prod.Name);
			deserializedProd.IsCurated.Should().Be(prod.IsCurated);
			deserializedProd.Modified.Should().BeWithin(TimeSpan.FromSeconds(1.0)).After(prod.Modified);
			deserializedProd.Created.Should().BeWithin(TimeSpan.FromSeconds(1.0)).After(prod.Created);
			deserializedProd.Price.Should().Be(prod.Price);
		}

	}
}
