namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using FluentAssertions;
	using NUnit.Framework;

	public partial class SegmentProcessorTestCase
	{
		[Test]
		public void EntityType_PropertySingle_withValue_View_None_None__Success()
		{
			Process("/catalogs(1)/Id/$value", SegmentOp.View, _model, accept: "*/*");

			_response.contentType.Should().Be("text/plain");
			_body.ToString().Should().Be(@"1");
		}

		[Test]
		public void EntityType_PropertySingle_withValue_View_None_Xml__Success_2()
		{
			Process("/catalogs(1)/Name/$value", SegmentOp.View, _model, accept: "*/*");

			_response.contentType.Should().Be("text/plain");
			_body.ToString().Should().Be(@"Cat1");
		}


//		[Test]
//		public void ValueOperation_ForKeyInResource()
//		{
//			Process("/catalogs(1)/Products(1)/Id/$value", SegmentOp.View, model);
//
			// assert last segment is product name in raw format
//		}
//
//		[Test]
//		public void ValueOperation_ForPropOfComplexType()
//		{

//			Process("/suppliers(1)/Address/Street/$value", SegmentOp.View, model);
//
			// Assert last segment value is ''
//		}
//
//		[Test]
//		public void ValueOperation_ForPropertyOfResource()
//		{
//			Process("/products(1)/Catalog/Name/$value", SegmentOp.View, model);
//
			// Assert last segment value is single value = name in raw format
//		}
//
//		[Test]
//		public void InvalidValueOperation_ForEntityType_()
//		{
//			Process("/catalogs(1)/$value", SegmentOp.View, model);
//		}
//
//		[Test]
//		public void InvalidValueOperation_ForResourceMultiResult_()
//		{
//			Process("/catalogs(1)/Products(1)/$value", SegmentOp.View, model);
//
			// assert last segment is product name
//		}
//
//		[Test]
//		public void InvalidValueOperation_ForResourceSingleResult_()
//		{
//			Process("/products(1)/Catalog/$value", SegmentOp.View, model);
//
// assert for 
//
//			<error xmlns="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata">
//			<code></code>
//			<message xml:lang="en-US">Resource not found for the segment 'Invalid'.</message>
//			</error>
//		}
	}
}
