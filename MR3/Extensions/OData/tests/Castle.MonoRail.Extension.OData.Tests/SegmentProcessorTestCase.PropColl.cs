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
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Data.Services.Providers;
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

		[Test, Description("Id for products needs to refer back to EntityContainer.Products")]
		public void PropCollection_View_Atom_Atom_Success()
		{
			Process("/catalogs(1)/Products/", SegmentOp.View, _model);

			// Console.WriteLine(_body.ToString());
			var feed = SyndicationFeed.Load(XmlReader.Create(new StringReader(_body.ToString())));
			feed.Should().NotBeNull();

			feed.Id.Should().BeEquivalentTo("http://localhost/base/catalogs(1)/products");
			feed.Items.Should().HaveCount(2);

			feed.Items.ElementAt(0).Id.Should().BeEquivalentTo("http://localhost/base/products(1)");
			feed.Items.ElementAt(1).Id.Should().BeEquivalentTo("http://localhost/base/products(2)");
		}

		[Test, Description("The EntityContainer only has Catalog, so the ids for products will be under catalog(id)")]
		public void PropCollection_View_Atom_Atom_Success_2()
		{
			Process("/catalogs(1)/Products/", SegmentOp.View, _modelWithMinimalContainer);

			// Console.WriteLine(_body.ToString());
			var feed = SyndicationFeed.Load(XmlReader.Create(new StringReader(_body.ToString())));
			feed.Should().NotBeNull();

			feed.Id.Should().BeEquivalentTo("http://localhost/base/catalogs(1)/products");
			feed.Items.Should().HaveCount(2);

			feed.Items.ElementAt(0).Id.Should().BeEquivalentTo("http://localhost/base/catalogs(1)/products(1)");
			feed.Items.ElementAt(1).Id.Should().BeEquivalentTo("http://localhost/base/catalogs(1)/products(2)");
		}

		[Test, Description("Id for products needs to refer back to EntityContainer.Products"), Ignore("JSon format differs")]
		public void PropCollection_View_JSon_Success()
		{
			Process("/catalogs(1)/Products/", SegmentOp.View, _model, accept: MediaTypes.JSon);

			_response.contentType.Should().Be(MediaTypes.JSon);
			_body.ToString().Replace('\t', ' ').Should().Be(
@"{
 ""d"": [
  {
   ""__metadata"": {
    ""uri"": ""http://localhost/base/products(1)"",
    ""type"": ""TestNamespace.Product1""
   },
   ""Id"": 1,
   ""Name"": ""Product1"",
   ""Price"": 0.0,
   ""Created"": ""0001-01-01T00:00:00"",
   ""Modified"": ""0001-01-01T00:00:00"",
   ""IsCurated"": false,
   ""Catalog"": {
    ""__deferred"": {
     ""uri"": ""http://localhost/base/products(1)/Catalog""
    }
   }
  },
  {
   ""__metadata"": {
    ""uri"": ""http://localhost/base/products(2)"",
    ""type"": ""TestNamespace.Product1""
   },
   ""Id"": 2,
   ""Name"": ""Product2"",
   ""Price"": 0.0,
   ""Created"": ""0001-01-01T00:00:00"",
   ""Modified"": ""0001-01-01T00:00:00"",
   ""IsCurated"": false,
   ""Catalog"": {
    ""__deferred"": {
     ""uri"": ""http://localhost/base/products(2)/Catalog""
    }
   }
  }
 ]
}");
		}

		[Test, Description("The EntityContainer only has Catalog, so the ids for products will be under catalog(id)"), Ignore("JSon format differs")]
		public void PropCollection_View_JSon_Success_2()
		{
			Process("/catalogs(1)/Products/", SegmentOp.View, _modelWithMinimalContainer, accept: MediaTypes.JSon);

			_response.contentType.Should().Be(MediaTypes.JSon);
			_body.ToString().Replace('\t', ' ').Should().Be(
@"{
 ""d"": [
  {
   ""__metadata"": {
    ""uri"": ""http://localhost/base/catalogs(1)/Products(1)"",
    ""type"": ""TestNamespace.Product1""
   },
   ""Id"": 1,
   ""Name"": ""Product1"",
   ""Price"": 0.0,
   ""Created"": ""0001-01-01T00:00:00"",
   ""Modified"": ""0001-01-01T00:00:00"",
   ""IsCurated"": false,
   ""Catalog"": {
    ""__deferred"": {
     ""uri"": ""http://localhost/base/catalogs(1)/Products(1)/Catalog""
    }
   }
  },
  {
   ""__metadata"": {
    ""uri"": ""http://localhost/base/catalogs(1)/Products(2)"",
    ""type"": ""TestNamespace.Product1""
   },
   ""Id"": 2,
   ""Name"": ""Product2"",
   ""Price"": 0.0,
   ""Created"": ""0001-01-01T00:00:00"",
   ""Modified"": ""0001-01-01T00:00:00"",
   ""IsCurated"": false,
   ""Catalog"": {
    ""__deferred"": {
     ""uri"": ""http://localhost/base/catalogs(1)/Products(2)/Catalog""
    }
   }
  }
 ]
}");
		}

		[Test, Description("Id for products needs to refer back to EntityContainer.Products"), Ignore("JSon format differs")]
		public void PropCollection_ViewWithFilter_JSon_Success()
		{
			Process("/catalogs(1)/Products/", SegmentOp.View, _model, qs: "$filter=Name eq 'Product1'", accept: MediaTypes.JSon);

			_response.contentType.Should().Be(MediaTypes.JSon);
			_body.ToString().Replace('\t', ' ').Should().Be(
@"{
 ""d"": [
  {
   ""__metadata"": {
    ""uri"": ""http://localhost/base/products(1)"",
    ""type"": ""TestNamespace.Product1""
   },
   ""Id"": 1,
   ""Name"": ""Product1"",
   ""Price"": 0.0,
   ""Created"": ""0001-01-01T00:00:00"",
   ""Modified"": ""0001-01-01T00:00:00"",
   ""IsCurated"": false,
   ""Catalog"": {
    ""__deferred"": {
     ""uri"": ""http://localhost/base/products(1)/Catalog""
    }
   }
  }
 ]
}");
		}

	}
}
