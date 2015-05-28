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
