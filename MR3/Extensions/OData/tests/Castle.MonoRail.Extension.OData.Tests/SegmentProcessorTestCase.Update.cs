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
