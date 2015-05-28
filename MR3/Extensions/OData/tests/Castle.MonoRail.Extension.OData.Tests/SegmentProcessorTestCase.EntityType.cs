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

using Castle.MonoRail.Tests;

namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.IO;
	using System.ServiceModel.Syndication;
	using System.Xml;
	using FluentAssertions;
	using NUnit.Framework;

	public partial class SegmentProcessorTestCase
	{
		[Test]
		public void EntityType_View_Atom_Atom_Success()
		{
			Process("/catalogs(2)/", SegmentOp.View, _model);

			// Console.WriteLine(_body.ToString());
			var entry = SyndicationItem.Load(XmlReader.Create(new StringReader(_body.ToString())));
			entry.Should().NotBeNull();

			Assertion.Callbacks.ViewSingleWasCalled(1);
			Assertion.Entry(entry, Id: "http://localhost/base/catalogs(2)");
			Assertion.EntryLink(entry, Title: "Catalog1", Rel: "edit", Href: "catalogs(2)");
			Assertion.EntryLink(entry, Title: "Product1", Rel: "http://schemas.microsoft.com/ado/2007/08/dataservices/related/Product1", Href: "catalogs(2)/Products", Media: "application/atom+xml;type=feed");


			Process("/catalogs(1)", SegmentOp.View, _model);
			entry = SyndicationItem.Load(XmlReader.Create(new StringReader(_body.ToString())));
			entry.Should().NotBeNull();

			Assertion.Callbacks.ViewSingleWasCalled(2);

			Assertion.Entry(entry, Id: "http://localhost/base/catalogs(1)");
			Assertion.EntryLink(entry, Title: "Catalog1", Rel: "edit", Href: "catalogs(1)");
			Assertion.EntryLink(entry, Title: "Product1", 
				Rel: "http://schemas.microsoft.com/ado/2007/08/dataservices/related/Product1", 
				Href: "catalogs(1)/Products", Media: "application/atom+xml;type=feed");
		}

		[Test]
		public void EntityType_View_Atom_Atom_Success2()
		{
			var model = new StubModel(
				m =>
				{
					m.EntitySet("catalogs", _catalog1Set);
					m.EntitySet("products", _product1Set);
					m.EntitySet("suppliers", _supplier1Set);
				});
			model.Initialize(new StubServiceRegistry());

			Process("/suppliers(1)/", SegmentOp.View, model);

			// Console.WriteLine(_body.ToString());
			var entry = SyndicationItem.Load(XmlReader.Create(new StringReader(_body.ToString())));

			Assertion.Entry(entry, Id: "http://localhost/base/suppliers(1)");
			Assertion.EntryLink(entry, Title: "Supplier1", Rel: "edit", Href: "suppliers(1)");
			Assertion.Callbacks.ViewSingleWasCalled(1);
		}

		[Test]
		public void EntityType_View_Json_Success()
		{
			Process("/catalogs(2)/", SegmentOp.View, _model, accept: "application/json");

			_response.contentType.Should().Be(MediaTypes.JSon);

			_body.ToString().Replace('\t', ' ').Should().Be(
@"{
 ""d"": {
  ""__metadata"": {
   ""uri"": ""http://localhost/base/catalogs(2)"",
   ""type"": ""TestNamespace.Catalog1""
  },
  ""Id"": 2,
  ""Name"": ""Cat2"",
  ""Products"": {
   ""__deferred"": {
    ""uri"": ""http://localhost/base/catalogs(2)/Products""
   }
  }
 }
}");
		}

		[Test, ExpectedException(ExpectedMessage = "Lookup of entity Catalog1 for key 1000 failed.")]
		public void EntityType_NonExistinEntityById()
		{
			// TODO: this should return a xml response with the error details
			Process("/catalogs(1000)/", SegmentOp.View, _model);
		}
	}
}
