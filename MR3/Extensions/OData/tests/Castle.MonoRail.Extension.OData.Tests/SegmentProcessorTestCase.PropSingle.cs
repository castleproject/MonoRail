namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.IO;
	using System.Linq;
	using System.ServiceModel.Syndication;
	using System.Xml;
	using FluentAssertions;
	using NUnit.Framework;

	public partial class SegmentProcessorTestCase
	{
		// naming convention for testing methods
		// [EntitySet|EntityType|PropSingle|PropCollection|Complex|Primitive]_[Operation]_[InputFormat]_[OutputFormat]__[Success|Failure]

		[Test]
		public void EntityType_PropertySingle_View_None_Xml__Success()
		{
			Process("/catalogs(1)/Id", SegmentOp.View, _model, accept: "application/xml");

			_response.contentType.Should().Be("application/xml");
			_body.ToString().Should().BeEquivalentTo(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<Id p1:type=""Edm.Int32"" xmlns:p1=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2007/08/dataservices"">1</Id>");
		}

		[Test]
		public void EntityType_PropertySingle_View_None_Xml__Success_2()
		{
			Process("/catalogs(1)/Name", SegmentOp.View, _model, accept: "application/xml");

			_response.contentType.Should().Be("application/xml");
			_body.ToString().Should().BeEquivalentTo(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<Name xmlns=""http://schemas.microsoft.com/ado/2007/08/dataservices"">Cat1</Name>");
		}

		[Test]
		public void EntityType_PropertySingle_View_None_JSon__Success()
		{
			Process("/catalogs(1)/Id", SegmentOp.View, _model, accept: "application/json");

			_response.contentType.Should().Be("application/json");
			_body.ToString().Replace('\t', ' ').Should().BeEquivalentTo(
@"{
 ""d"": {
  ""Id"": 1
 }
}");
		}

		[Test]
		public void EntityType_PropertySingle_View_None_JSon__Success_2()
		{
			Process("/catalogs(1)/Name", SegmentOp.View, _model, accept: "application/json");

			_response.contentType.Should().Be("application/json");
			_body.ToString().Replace('\t', ' ').Should().BeEquivalentTo(
@"{
 ""d"": {
  ""Name"": ""Cat1""
 }
}");
		}

		[Test, Description("Id for products needs to refer back to EntityContainer.Products")]
		public void PropCollAsSingle_View_Atom_Atom_Success()
		{
			Process("/catalogs(1)/Products(1)/", SegmentOp.View, _model);

			// Console.WriteLine(_body.ToString());

			var feed = SyndicationItem.Load(XmlReader.Create(new StringReader(_body.ToString())));
			feed.Should().NotBeNull();

			feed.Id.Should().BeEquivalentTo("http://localhost/base/products(1)");

			feed.Links.Count.Should().Be(2);
			feed.Links.ElementAt(0).Title.Should().Be("Product1");
			feed.Links.ElementAt(0).RelationshipType.Should().Be("edit");
			feed.Links.ElementAt(0).Uri.OriginalString.Should().Be("products(1)");

			feed.Links.ElementAt(1).Title.Should().Be("Catalog1");
			feed.Links.ElementAt(1).MediaType.Should().Be("application/atom+xml;type=entry");
			feed.Links.ElementAt(1).RelationshipType.Should().Be("http://schemas.microsoft.com/ado/2007/08/dataservices/related/Catalog1");
			feed.Links.ElementAt(1).Uri.OriginalString.Should().Be("products(1)/Catalog");
		}

		[Test, Description("The EntityContainer only has Catalog, so the ids for products will be under catalog(id)")]
		public void PropCollAsSingle_View_Atom_Atom_Success_2()
		{
			Process("/catalogs(1)/Products(1)/", SegmentOp.View, _modelWithMinimalContainer);
			
			// Console.WriteLine(_body.ToString());

			var item = SyndicationItem.Load(XmlReader.Create(new StringReader(_body.ToString())));
			item.Should().NotBeNull();

			item.Id.Should().BeEquivalentTo("http://localhost/base/catalogs(1)/products(1)");

			item.BaseUri.OriginalString.Should().Be("http://localhost/base/");

			item.Links.ElementAt(0).BaseUri.OriginalString.Should().Be("http://localhost/base/");
			item.Links.ElementAt(0).RelationshipType.Should().Be("edit");
			item.Links.ElementAt(0).Title.Should().Be("Product1");
			item.Links.ElementAt(0).Uri.OriginalString.Should().Be("catalogs(1)/Products(1)");

			item.Links.ElementAt(1).BaseUri.OriginalString.Should().Be("http://localhost/base/");
			item.Links.ElementAt(1).RelationshipType.Should().Be("http://schemas.microsoft.com/ado/2007/08/dataservices/related/Catalog1");
			item.Links.ElementAt(1).Title.Should().Be("Catalog1");
			item.Links.ElementAt(1).MediaType.Should().Be("application/atom+xml;type=entry");
			item.Links.ElementAt(1).Uri.OriginalString.Should().BeEquivalentTo("Catalogs(1)/Products(1)/Catalog");
		}
	}
}
