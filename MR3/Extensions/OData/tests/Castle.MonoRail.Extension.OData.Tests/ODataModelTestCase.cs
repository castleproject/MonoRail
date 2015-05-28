using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Castle.MonoRail.Extension.OData.Tests
{
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class ODataModelTestCase
	{
		[Test]
		public void properties_reflect_construct_args()
		{
			var model = new SubModel("ns", "container");

			model.SchemaNamespace.Should().Be("ns");
			model.ContainerName.Should().Be("container");
		}

		class SubModel : ODataModel
		{
			public SubModel(string schemaNamespace, string containerName) : 
				base(schemaNamespace, containerName)
			{
			}

			public override void Initialize()
			{
			}
		}
	}
}
