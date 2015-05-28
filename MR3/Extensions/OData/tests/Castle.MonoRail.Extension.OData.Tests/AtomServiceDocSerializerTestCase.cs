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
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Data.Services.Common;
	using System.Data.Services.Providers;
	using System.IO;
	using System.Linq;
	using System.Text;
	using NUnit.Framework;
	using Castle.MonoRail.Extension.OData.Serialization;

	[TestFixture]
	public class AtomServiceDocSerializerTestCase
	{
		[Test]
		public void OnlyWritesEntitySets_()
		{
			var model = new StubModel(m =>
			{
				m.EntitySet("products", new List<Product2>().AsQueryable())
					.AddAttribute(new EntityPropertyMappingAttribute("Name", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, true));
				m.EntitySet("catalogs", new List<Catalog2>().AsQueryable());
				m.EntitySet("suppliers", new List<Supplier2>().AsQueryable());
			});
			var services = new StubServiceRegistry();
			model.Initialize(services);
			var writer = new StringWriter();

			AtomServiceDocSerializer.serialize(writer, new Uri("http://localhost/app"),  new DataServiceMetadataProviderWrapper(model), Encoding.UTF8);

			Console.WriteLine(writer.GetStringBuilder().ToString());
		}

		// -------------------------------------

		public class Address2
		{
			public string Street { get; set; }
			public string Zip { get; set; }
			public string Country { get; set; }
		}

		public class Supplier2
		{
			[Key]
			public int Id { get; set; }
			public Address2 Address { get; set; }
		}

		public class Catalog2
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public IList<Product2> Products { get; set; }
		}
		public class Product2
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public Catalog2 Catalog { get; set; }
		}
	}
}
