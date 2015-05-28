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

	[TestFixture]
	public class QueryExpressionParserTestCase
	{
		[Test]
		public void Edm_Null()
		{
			var exp = QueryExpressionParser.parse_filter("City ne null ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Null []");
		}

		[Test]
		public void Edm_Binary1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne X'FFca' ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Binary [FFca]");
		}

		[Test]
		public void Edm_Binary2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne binary'FF' ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Binary [FF]");
		}

		[Test]
		public void Edm_Binary3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne binary'caFa01' ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Binary [caFa01]");
		}

		[Test]
		public void Edm_Bool1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne true ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Boolean [true]");
		}

		[Test]
		public void Edm_Bool2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne false ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Boolean [false]");
		}

		[Test]
		public void Edm_Datetime1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne datetime'2012-05-02T04:12:22.102' ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal DateTime [05/02/2012 04:12:22]");
		}

		[Test]
		public void Edm_Datetime2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne datetime'2012-05-02T04:12:22' ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal DateTime [05/02/2012 04:12:22]");
		}

		[Test]
		public void Edm_Decimal1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 132m ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Decimal [132]");
		}

		[Test]
		public void Edm_Decimal2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 132M ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Decimal [132]");
		}

		[Test]
		public void Edm_Decimal3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 13.2m");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Decimal [13.2]");
		}

		[Test]
		public void Edm_Decimal4()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 13.2M ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Decimal [13.2]");
		}

		[Test, Ignore("Should this use the unary negate?")]
		public void Edm_Decimal6()
		{
			var exp = QueryExpressionParser.parse_filter("City ne -13.2M");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Decimal [-13.2]");
		}

		[Test]
		public void Edm_Double1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 12d ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Double [12]");
		}

		[Test]
		public void Edm_Double2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 12.1d ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Double [12.1000000014901]");
		}

		[Test, Ignore("Should this use the unary negate?")]
		public void Edm_Double3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne -12.1d");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Double [-12.1]");
		}

		[Test, Ignore("missing support for e*digits")]
		public void Edm_Double6()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 1.1e21d ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Double [1.1e21d]");
		}

		[Test]
		public void Edm_Single1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 12f ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Single [12]");
		}

		[Test]
		public void Edm_Single2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 12.1");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Single [12.1]");
		}

		[Test, Ignore("Should this use the unary negate?")]
		public void Edm_Single3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne -12.1 ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Single [-12.1]");
		}

		[Test, Ignore("missing support for e*digits")]
		public void Edm_Single6()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 1.1e21 ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Single [1.1e21]");
		}

		[Test]
		public void Edm_Guid1()
		{
			var id = Guid.NewGuid().ToString();

			var exp = QueryExpressionParser.parse_filter("City ne guid'" + id + "' ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Guid [" + id + @"]");
		}

		[Test]
		public void Edm_Guid2()
		{
			var id = Guid.NewGuid().ToString();

			var exp = QueryExpressionParser.parse_filter("City ne guid'" + id + "'");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Guid [" + id + @"]");
		}

		[Test]
		public void Edm_Int32_1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 123");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Int32 [123]");
		}

		[Test]
		public void Edm_Int32_2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 123 ");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Int32 [123]");
		}

		[Test, Ignore("should this use unary negate")]
		public void Edm_Int32_3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne -123");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Int32 [-123]");
		}

		[Test]
		public void Edm_Int64_1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 123l");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Int64 [123]");
		}

		[Test, Ignore("should this use unary negate")]
		public void Edm_Int64_3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne -123l");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal Int64 [-123]");
		}

		[Test]
		public void Edm_String_1()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 'Redmond'");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal SString [Redmond]");
		}

		[Test]
		public void Edm_String_2()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 'Redmond '");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal SString [Redmond ]");
		}

		[Test]
		public void Edm_String_3()
		{
			var exp = QueryExpressionParser.parse_filter("City ne ' Redmond '");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal SString [ Redmond ]");
		}

		// TODO: support for Edm.Time
		// TODO: support for Edm.DateTimeOffset

		// TODO: OData v3: support for Geography / GeographyPoint / GeographyLineString / GeographyPolygon / GeographyCollection / GeographyMultiPoint
		// TODO: OData v3: support for Geometry / ...




		[Test]
		public void LogicalOp_Ne()
		{
			var exp = QueryExpressionParser.parse_filter("City ne 'Redmond'");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id City
    Literal SString [Redmond]");
		}

		[Test]
		public void LogicalOp_Eq()
		{
			var exp = QueryExpressionParser.parse_filter("City eq 'Redmond'");
			// Console.WriteLine(exp.ToStringTree());
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Eq 
    MemberAccess 
      Element
      Id City
    Literal SString [Redmond]");
		}

		[Test]
		public void LogicalOp_GreaterThan()
		{
			var exp = QueryExpressionParser.parse_filter("Price gt 10");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary GreatT 
    MemberAccess 
      Element
      Id Price
    Literal Int32 [10]");
			
		}

		[Test]
		public void LogicalOp_GreaterThanOrEqual()
		{
			var exp = QueryExpressionParser.parse_filter("Price ge 10");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary GreatET 
    MemberAccess 
      Element
      Id Price
    Literal Int32 [10]");

		}

		[Test]
		public void LogicalOp_LessThan()
		{
			var exp = QueryExpressionParser.parse_filter("Price lt 10");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary LessT 
    MemberAccess 
      Element
      Id Price
    Literal Int32 [10]");

		}

		[Test]
		public void LogicalOp_LessThanOrEqual()
		{
			var exp = QueryExpressionParser.parse_filter("Price le 10");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary LessET 
    MemberAccess 
      Element
      Id Price
    Literal Int32 [10]");

		}

		[Test]
		public void LogicalOp_And()
		{
			var exp = QueryExpressionParser.parse_filter("Price le 200 and Price gt 3.5");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary And 
    Binary LessET 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [200]
    Binary GreatT 
      MemberAccess 
        Element
        Id Price
      Literal Single [3.5]");
		}

		[Test]
		public void LogicalOp_Or()
		{
			var exp = QueryExpressionParser.parse_filter("Price le 3.5 or Price gt 200");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Or 
    Binary LessET 
      MemberAccess 
        Element
        Id Price
      Literal Single [3.5]
    Binary GreatT 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [200]");
		}

		[Test, Ignore("'not' not implemented - ditto for func calls")]
		public void LogicalOp_Not()
		{
			var exp = QueryExpressionParser.parse_filter("not endswith(Description,'milk')");
			Console.WriteLine(exp.ToStringTree());
		}

		[Test]
		public void Arithmetic_Add()
		{
			var exp = QueryExpressionParser.parse_filter("Price add 5 gt 10");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary GreatT 
    Binary Add 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [5]
    Literal Int32 [10]");
		}
		
		[Test]
		public void Arithmetic_Sub()
		{
			var exp = QueryExpressionParser.parse_filter("Price sub 5 gt 10");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary GreatT 
    Binary Sub 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [5]
    Literal Int32 [10]");
		}

		[Test]
		public void Arithmetic_Mul()
		{
			var exp = QueryExpressionParser.parse_filter("Price mul 2 gt 2000");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary GreatT 
    Binary Mul 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [2]
    Literal Int32 [2000]");
		}

		[Test]
		public void Arithmetic_Div()
		{
			var exp = QueryExpressionParser.parse_filter("Price div 2 gt 4");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary GreatT 
    Binary Div 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [2]
    Literal Int32 [4]");
		}

		[Test]
		public void Arithmetic_Mod()
		{
			var exp = QueryExpressionParser.parse_filter("Price mod 2 eq 0");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Eq 
    Binary Mod 
      MemberAccess 
        Element
        Id Price
      Literal Int32 [2]
    Literal Int32 [0]");
		}

		[Test]
		public void OperatorPrecedence_Mul_Preceeds_Eq()
		{
			var exp = QueryExpressionParser.parse_filter("1 eq 2 mul 3");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Eq 
    Literal Int32 [1]
    Binary Mul 
      Literal Int32 [2]
      Literal Int32 [3]");
		}

		[Test]
		public void OperatorPrecedence_Mul_Preceeds_Add()
		{
			var exp = QueryExpressionParser.parse_filter("1 add 2 mul 3");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Add 
    Literal Int32 [1]
    Binary Mul 
      Literal Int32 [2]
      Literal Int32 [3]");

			exp = QueryExpressionParser.parse_filter("1 mul 2 add 3");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Add 
    Binary Mul 
      Literal Int32 [1]
      Literal Int32 [2]
    Literal Int32 [3]");
		}

		[Test]
		public void OperatorPrecedence_GroupingAdd_Add_Preceeds_Mul()
		{
			var exp = QueryExpressionParser.parse_filter("(1 add 2) mul 3");
			exp.ToStringTree().Should().BeEquivalentTo(@"
  Binary Mul 
    Binary Add 
      Literal Int32 [1]
      Literal Int32 [2]
    Literal Int32 [3]");
		}

		[Test]
		public void OperatorPrecedence()
		{
			// Equivalent
			var exp0 = QueryExpressionParser.parse_filter("1 add 2 add 3");
			Console.WriteLine(exp0.ToStringTree());

			// Equivalent
			exp0 = QueryExpressionParser.parse_filter("1 sub 2 add 3");
			Console.WriteLine(exp0.ToStringTree());

			// parens
			exp0 = QueryExpressionParser.parse_filter("1 mul (2 add 3)");
			Console.WriteLine(exp0.ToStringTree());
		}

		[Test]
		public void PropNavigation()
		{
			var exp1 = QueryExpressionParser.parse_filter("Name ne 'Cat1'");
			exp1.ToStringTree().Should().BeEquivalentTo(@"
  Binary Neq 
    MemberAccess 
      Element
      Id Name
    Literal SString [Cat1]");

			var exp2 = QueryExpressionParser.parse_filter("Customers/Name eq 'John'");
			exp2.ToStringTree().Should().BeEquivalentTo(@"
  Binary Eq 
    MemberAccess 
      MemberAccess 
        Element
        Id Customers
      Id Name
    Literal SString [John]");

			var exp3 = QueryExpressionParser.parse_filter("Customers/Address/Street eq 'London Ave'");
			exp3.ToStringTree().Should().BeEquivalentTo(@"
  Binary Eq 
    MemberAccess 
      MemberAccess 
        MemberAccess 
          Element
          Id Customers
        Id Address
      Id Street
    Literal SString [London Ave]");
		}

	}
}
