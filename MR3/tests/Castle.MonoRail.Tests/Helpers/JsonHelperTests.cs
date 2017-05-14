#region License
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
#endregion

namespace Castle.MonoRail.Tests.Helpers
{
	using System;
	using System.IO;
	using System.Web;
	using Castle.MonoRail.Helpers;
	using FluentAssertions;
	using NUnit.Framework;
	using Serialization;

	[TestFixture]
	public class JsonHelperTests : HelperTestsBase
	{
		private JsonHelper _jsonHelper;
		private IModelSerializerResolver _resolver;

		[SetUp]
		public override void Init()
		{
			base.Init();
			_jsonHelper = new JsonHelper(_helperContext);
			_resolver = new ModelSerializerResolver();
			_jsonHelper.ModelSerializer = _resolver;
		}

		[Test]
		public void ToJson_ForGraph_SerializesToJson()
		{
			var model = new Customer() {Name = "hammett"};
			var json = _jsonHelper.ToJson(model);

			Assert.AreEqual(@"{""Name"":""hammett""}", json.ToHtmlString());
		}

		[Test]
		public void ToJson_GraphWithCustomSerializer_SerializesToJson()
		{
			_resolver.Register<Customer>(MediaTypes.JSon, typeof(CustomCustomerSerializer));

			var model = new Customer() { Name = "hammett" };
			var json = _jsonHelper.ToJson(model);

			Assert.AreEqual(@"hello", json.ToHtmlString());
		}

		class Customer
		{
			public string Name { get; set; }
		}

		class CustomCustomerSerializer : IModelSerializer<Customer>
		{
			public void Serialize(Customer model, string contentType, TextWriter writer, ModelMetadataProvider metadataProvider)
			{
				contentType.Should().Be("application/json");
				writer.Write("hello");
			}

			public Customer Deserialize(string prefix, string contentType, ModelSerializationContext request, ModelMetadataProvider metadataProvider)
			{
				throw new NotImplementedException();
			}
		}
	}
}
