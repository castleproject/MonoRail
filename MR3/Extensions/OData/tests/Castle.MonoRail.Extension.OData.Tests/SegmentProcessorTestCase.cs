namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel.DataAnnotations;
	using System.Data.Services.Providers;
	using System.IO;
	using System.Linq;
	using System.ServiceModel.Syndication;
	using System.Text;
	using System.Xml;
	using FluentAssertions;
	using Newtonsoft.Json;
	using NUnit.Framework;

	static class SyndicationExtensions
	{
		public static Stream ToStream(this SyndicationItem item)
		{
			var stream = new MemoryStream();
			var writer = XmlWriter.Create(stream, new XmlWriterSettings() { Indent = true });
			item.SaveAsAtom10(writer);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}
	}

	[TestFixture]
	public partial class SegmentProcessorTestCase
	{
		private const string DataSvsNs = "http://schemas.microsoft.com/ado/2007/08/dataservices";
		private const string MetadataNs = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

		private StringBuilder _body;
		private ResponseParameters _response;
		private RequestParameters _request;
		private IQueryable<Catalog1> _catalog1Set;
		private IQueryable<Product1> _product1Set;
		private IQueryable<Supplier1> _supplier1Set;
		private StubModel _model;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>> _authorize;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, IEnumerable>> _authorizeMany;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>> _view;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, IEnumerable>> _viewMany;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>> _created;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>> _updated;
		private List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>> _removed;
		private List<Tuple<ResourceType, string>> _invoked;
		protected Func<bool,string> _negotiate = null;

		private StubModel _modelWithMinimalContainer;

		public class ProcessorAssertions
		{
			private static SegmentProcessorTestCase state;
			public CallbackAsserts Callbacks = new CallbackAsserts();

			public ProcessorAssertions(SegmentProcessorTestCase state)
			{
				ProcessorAssertions.state = state;
			}

			public class CallbackAsserts
			{
				public void AuthorizeSingleWasCalled(int howManyTimes)
				{
					state._authorize.Should().HaveCount(howManyTimes);
				}
				public void AuthorizeManyWasCalled(int howManyTimes)
				{
					state._authorizeMany.Should().HaveCount(howManyTimes);
				}
				public void ViewSingleWasCalled(int howManyTimes)
				{
					state._view.Should().HaveCount(howManyTimes);
				}
				public void ViewManyWasCalled(int howManyTimes)
				{
					state._viewMany.Should().HaveCount(howManyTimes);
				}
				public void CreateWasCalled(int howManyTimes)
				{
					state._created.Should().HaveCount(howManyTimes);
				}
				public void UpdateWasCalled(int howManyTimes)
				{
					state._updated.Should().HaveCount(howManyTimes);
				}
				public void RemoveWasCalled(int howManyTimes)
				{
					state._removed.Should().HaveCount(howManyTimes);
				}
			}

			public void Entry(SyndicationItem item, string Id)
			{
				item.Should().NotBeNull();
				item.BaseUri.OriginalString.Should().Be("http://localhost/base/");
				item.Id.Should().BeEquivalentTo(Id);
			}

			public void EntryLink(SyndicationItem item, string Title, string Rel = null, string Href = null, string Media = null)
			{
				var link = item.Links.Where(l => l.Title == Title).SingleOrDefault();
				link.Should().NotBeNull("Could not find link with title " + Title);
				link.BaseUri.OriginalString.Should().Be("http://localhost/base/");
				link.Title.Should().Be(Title);
				if (Rel != null)
					link.RelationshipType.Should().Be(Rel);
				if (Media != null)
					link.MediaType.Should().Be(Media);
				if (Href != null)
					link.Uri.OriginalString.Should().BeEquivalentTo(Href);
			}

			public void Feed(SyndicationFeed item, string id)
			{
				item.Should().NotBeNull();
				item.BaseUri.OriginalString.Should().Be("http://localhost/base/");
				item.Id.Should().BeEquivalentTo(id);
			}

			public void FeedLink(SyndicationFeed item, string title, string rel = null, string href = null, string media = null)
			{
				var link = item.Links.Where(l => l.Title == title).SingleOrDefault();
				link.Should().NotBeNull("Could not find link with title " + title);
				link.BaseUri.OriginalString.Should().Be("http://localhost/base/");
				link.Title.Should().Be(title);
				if (rel != null)
					link.RelationshipType.Should().Be(rel);
				if (media != null)
					link.MediaType.Should().Be(media);
				if (href != null)
					link.Uri.OriginalString.Should().BeEquivalentTo(href);
			}

			public void ResponseIs(int code, string contentType = null)
			{
				state._response.httpStatus.Should().Be(code);
				if (contentType != null)
					state._response.contentType.Should().BeEquivalentTo(contentType);
			}
		}

		public ProcessorAssertions Assertion;

		[SetUp]
		public void Init()
		{
			Assertion = new ProcessorAssertions(this);

			_negotiate = (isSingle) => _request.accept[0];

			_authorize = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>>();
			_authorizeMany = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, IEnumerable>>();
			_view = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>>();
			_viewMany = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, IEnumerable>>();
			_created = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>>();
			_updated = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>>();
			_removed = new List<Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>>();
			_invoked = new List<Tuple<ResourceType, string>>();

			_product1Set = new List<Product1>
			               	{
			               		new Product1() { Id = 1, Name = "Product1" },
								new Product1() { Id = 2, Name = "Product2" },
			               	}.AsQueryable();
			_catalog1Set = new List<Catalog1>
			               	{
			               		new Catalog1() { Id = 1, Name = "Cat1"}, 
								new Catalog1() { Id = 2, Name = "Cat2" }
			               	}.AsQueryable();
			_supplier1Set = new List<Supplier1>
							{
								new Supplier1() { Id = 1, Address = new Address1() { Street = "wilson ave", Zip = "vxxxx", Country = "canada"} },
								new Supplier1() { Id = 2, Address = new Address1() { Street = "kingsway ave", Zip = "zxxxx", Country = "canada"} },
							}.AsQueryable();

			_catalog1Set.ElementAt(0).Products = new List<Product1>(_product1Set);
			_catalog1Set.ElementAt(1).Products = new List<Product1>(_product1Set);
			_product1Set.ElementAt(0).Catalog = _catalog1Set.ElementAt(0);
			_product1Set.ElementAt(1).Catalog = _catalog1Set.ElementAt(1);

			_model = new StubModel(
				m =>
				{
					m.EntitySet("catalogs", _catalog1Set);
					m.EntitySet("products", _product1Set);
					m.EntitySet("suppliers", _supplier1Set);
				});

			_modelWithMinimalContainer = new StubModel(
				m =>
				{
					m.EntitySet("catalogs", _catalog1Set);
				});

			_body = new StringBuilder();
		}

		public void Process(string fullPath, SegmentOp operation, ODataModel model, 
							string contentType = "application/atom+xml", 
							string accept = "application/atom+xml", 
							string qs = "",
							Stream inputStream = null)
		{
			_body = new StringBuilder();
			var baseUri = new Uri("http://localhost/base/");
			var parameters = Utils.BuildFromQS(qs);

			var segments = SegmentParser.parse(fullPath, parameters, model, baseUri);
			_response = new ResponseParameters(null, Encoding.UTF8, new StringWriter(_body), 200, "OK", null);

			var callbacks = new ProcessorCallbacks(
					(rt, ps, item) => 
					{
						_authorize.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>(rt, ps, item));
						return true; 
					},
					(rt, ps, items) =>
					{
						_authorizeMany.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, IEnumerable>(rt, ps, items));
						return true;
					},
					(rt, ps, item) =>
					{
						_view.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>(rt, ps, item));
						return true;
					},
					(rt, ps, items) =>
					{
						_viewMany.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, IEnumerable>(rt, ps, items));
						return true;
					},
					(rt, ps, item) =>
					{
						_created.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>(rt, ps, item));
						return true;
					},
					(rt, ps, item) =>
					{
						_updated.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>(rt, ps, item));
						return true;
					},
					(rt, ps, item) =>
					{
						_removed.Add(new Tuple<ResourceType, IEnumerable<Tuple<Type, object>>, object>(rt, ps, item));
						return true;
					},
					(rt, ps, action) => _invoked.Add(new Tuple<ResourceType, string>(rt, action)),
					_negotiate );

			_request = new RequestParameters(
					model, 
					model as IDataServiceMetadataProvider,
					new DataServiceMetadataProviderWrapper(model), 
					contentType, 
					Encoding.UTF8,
					inputStream,
					baseUri, 
					new [] { accept }
				);

			SegmentProcessor.Process(operation, 
									 segments.Item1, segments.Item2, segments.Item3, 
									 parameters, callbacks, _request, _response);
		}

		// -------------------------------------

		public class Address1
		{
			public string Street { get; set; }
			public string Zip { get; set; }
			public string Country { get; set; }

			public AtomSerialization.ContentDict ToContentDict()
			{
				var content = new AtomSerialization.ContentDict();
				content.Add("Street", "Edm.String", this.Street);
				content.Add("Country", "Edm.String", this.Country);
				content.Add("Zip", "Edm.String", this.Zip);
				return content;
			}
		}

		public class Supplier1
		{
			public Supplier1()
			{
				this.Address = new Address1();
			}

			[Key]
			public int Id { get; set; }
			public Address1 Address { get; set; }

			public SyndicationItem ToSyndicationItem()
			{
				var item = new SyndicationItem();
				var content = new AtomSerialization.ContentDict();
				item.Content = content;

				content.Add("Address", "TestNamespace.Address1", this.Address.ToContentDict());

				return item;
			}
		}

		public class Catalog1
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public IList<Product1> Products { get; set; }

			public SyndicationItem ToSyndicationItem()
			{
				var item = new SyndicationItem();
				var content = new AtomSerialization.ContentDict();
				item.Content = content;
				content.Add("Name", "Edm.String", this.Name);
				return item;
			}
		}

		public class Product1
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public decimal Price { get; set; }
			public DateTime Created { get; set; }
			public DateTime Modified { get; set; }
			public bool IsCurated { get; set; }

			public Catalog1 Catalog { get; set; }

			public SyndicationItem ToSyndicationItem()
			{
				var item = new SyndicationItem();
				var content = new AtomSerialization.ContentDict();
				item.Content = content;
				content.Add("Name", "Edm.String", this.Name);
				content.Add("Price", "Edm.Decimal", XmlConvert.ToString(this.Price));
				content.Add("Created", "Edm.DateTime", XmlConvert.ToString(this.Created, XmlDateTimeSerializationMode.RoundtripKind));
				content.Add("Modified", "Edm.DateTime", XmlConvert.ToString(this.Modified, XmlDateTimeSerializationMode.RoundtripKind));
				content.Add("IsCurated", "Edm.Boolean", XmlConvert.ToString(this.IsCurated));
				return item;
			}

			public string ToJSon()
			{
				var writer = new StringWriter();
				Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings()).Serialize(writer, this);
				return writer.GetStringBuilder().ToString();
			}
		}
	}
}
