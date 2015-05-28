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
