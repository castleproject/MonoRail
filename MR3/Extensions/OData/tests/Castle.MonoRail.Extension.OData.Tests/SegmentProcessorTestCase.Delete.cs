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
		public void EntityType_PropertySingle_Delete__Success()
		{
			Process("/catalogs(1)/Products(1)/", SegmentOp.Delete, _modelWithMinimalContainer );

			// TODO: need to collect the containers, so controller can get all of them in the action call

			// Assertion.Callbacks.SingleWasCalled(2);
			Assertion.Callbacks.RemoveWasCalled(1);
			Assertion.ResponseIs(204);
		}

		[Test]
		public void EntitySetSingle_Delete__Success()
		{
			Process("/Products(1)/", SegmentOp.Delete, _model);

			// Assertion.Callbacks.SingleWasCalled(1);
			Assertion.Callbacks.RemoveWasCalled(1);
			Assertion.ResponseIs(204);
		}

	}
}
