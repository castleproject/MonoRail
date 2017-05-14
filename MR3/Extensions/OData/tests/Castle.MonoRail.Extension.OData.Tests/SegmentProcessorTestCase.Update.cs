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
		public void PropCollection_Update_Atom_Atom_Success()
		{
			var prod = new Product1()
			           	{
							Id = 1,
							Modified = DateTime.Now, 
							IsCurated = false, 
							Name = "testing again", Price = 3.3m
			           	};
			prod.ToSyndicationItem();

			Process("/catalogs(1)/Products(1)/", SegmentOp.Update, _modelWithMinimalContainer, inputStream: prod.ToSyndicationItem().ToStream() );

			Assertion.ResponseIs(204);

			// TODO: need to collect the containers, so controller can get all of them in the action call

			// Assertion.Callbacks.SingleWasCalled(2);
			Assertion.Callbacks.UpdateWasCalled(1);
		}

		[Test]
		public void EntitySetSingle_Update_Atom_Atom_Success()
		{
			var prod = new Product1()
			{
				Id = 1,
				Modified = DateTime.Now,
				IsCurated = false,
				Name = "testing again",
				Price = 3.3m
			};
			prod.ToSyndicationItem();

			Process("/Products(1)/", SegmentOp.Update, _model, inputStream: prod.ToSyndicationItem().ToStream());

			Assertion.ResponseIs(204);
			// Assertion.Callbacks.SingleWasCalled(1);
			Assertion.Callbacks.UpdateWasCalled(1);
		}
	}
}
