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

namespace Castle.MonoRail.Tests.Serializers
{
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Web;
	using Castle.MonoRail.Serialization;
	using FluentAssertions;
	using Newtonsoft.Json;
	using NUnit.Framework;

	[TestFixture]
	public class ModelSerializerResolverTests
	{
		private IModelSerializerResolver resolver;

		[SetUp]
		public void Init()
		{
			resolver = new ModelSerializerResolver() as IModelSerializerResolver;
		}

		[Test]
		public void CreateSerializer_DefaultSerializers_AlwaysCreatesSerializerForJson()
		{
			var serializer = resolver.CreateSerializer<Customer>(MediaTypes.JSon);
			Assert.IsNotNull(serializer);
		}

		[Test]
		public void CreateSerializer_DefaultSerializers_AlwaysCreatesSerializerForXml()
		{
			var serializer = resolver.CreateSerializer<Customer>(MediaTypes.Xml);
			Assert.IsNotNull(serializer);
		}

		[Test]
		public void CreateSerializer_DefaultSerializers_AlwaysCreatesSerializerForFormData()
		{
			var serializer = resolver.CreateSerializer<Customer>(MediaTypes.FormUrlEncoded);
			Assert.IsNotNull(serializer);
		}

		[Test, ExpectedException(typeof(MonoRailException))]
		public void CreateSerializer_DefaultSerializers_NoSerializerForJS()
		{
			resolver.CreateSerializer<Customer>(MediaTypes.Js);
		}

		[Test]
		public void Register_OfJsonSerializer_TakesPrecedenceOverDefault()
		{
			resolver.Register<Customer>(MediaTypes.JSon, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer<Customer>(MediaTypes.JSon);
			Assert.IsNotNull(serializer);
			Assert.AreEqual(typeof(StubSerializer<Customer>), serializer.GetType());
		}

		[Test]
		public void Register_OfJsonSerializer_TakesPrecedenceOverDefault_DiffSignature()
		{
			resolver.Register<Customer>(MediaTypes.JSon, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer(typeof(Customer), MediaTypes.JSon);
			Assert.IsNotNull(serializer);
			Assert.AreEqual(typeof(NonGenericSerializerAdapter), serializer.GetType());
		}

		[Test]
		public void CreateSerializer_ForUntypedSerializer_ReturnsFunctionalAdapter()
		{
			resolver.Register<Customer>(MediaTypes.JSon, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer(typeof(Customer), MediaTypes.JSon);
			var writer = new StringWriter();
			serializer.Serialize(new Customer(), "application/json", writer, null);
			writer.GetStringBuilder().ToString().Should().Be("hello");
		}

		[Test]
		public void Register_OfJsSerializer_IsReturnedWhenRequested()
		{
			resolver.Register<Customer>(MediaTypes.Js, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer<Customer>(MediaTypes.Js);
			Assert.IsNotNull(serializer);
			Assert.AreEqual(typeof(StubSerializer<Customer>), serializer.GetType());
		}

		[Test]
		public void Register_OfSerializerForModel_DoesNotAffectOtherModel()
		{
			resolver.Register<Customer>(MediaTypes.JSon, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer<Customer>(MediaTypes.JSon);
			Assert.AreEqual(typeof(StubSerializer<Customer>), serializer.GetType());

			var serializer2 = resolver.CreateSerializer<Supplier>(MediaTypes.JSon);
			Assert.AreNotEqual(typeof(StubSerializer<Customer>), serializer2.GetType());
		}

		[Test]
		public void CreateSerializer_ForCollectionDefaultingToDefaultSerializer_UsesSpecificTypeSerializerIfExistent_SerializationCase()
		{
			resolver.Register<Customer>(MediaTypes.JSon, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer<IEnumerable<Customer>>(MediaTypes.JSon);

			var writer = new StringWriter();
			serializer.Serialize(new[] { new Customer(), new Customer() }, "application/json", writer, new StubModelMetadataProvider(null));

			writer.GetStringBuilder().ToString().Should().Be("[hellohello]");
		}

		[Test, Ignore("Recursive deserialization not impl for json")]
		public void CreateSerializer_ForCollectionDefaultingToDefaultSerializer_UsesSpecificTypeSerializerIfExistent_DeserializationCase()
		{
			resolver.Register<Customer>(MediaTypes.JSon, typeof(StubSerializer<Customer>));
			var serializer = resolver.CreateSerializer<IEnumerable<Customer>>(MediaTypes.JSon);

			var context = new ModelSerializationContext(
				new MemoryStream(Encoding.UTF8.GetBytes("[ {} ]")), new NameValueCollection());
			var model = serializer.Deserialize("", "application/json", context, new StubModelMetadataProvider(null));
			model.Should().NotBeNull();
			model.Count().Should().Be(2);
		}

		class Customer {}
		class Supplier { }

		class StubSerializer<T> : IModelSerializer<T> where T : new()
		{
			public void Serialize(T model, string contentType, TextWriter writer, ModelMetadataProvider metadataProvider)
			{
				writer.Write("hello");
			}

			public T Deserialize(string prefix, string contentType, ModelSerializationContext context, ModelMetadataProvider metadataProvider)
			{
				var serializer = JsonSerializer.Create(new JsonSerializerSettings());

				var s = new StreamReader(context.InputStream).ReadLine();

				return (T) serializer.Deserialize(new StreamReader(context.InputStream), typeof (T));
			}
		}
	}
}
