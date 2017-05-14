namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Data.Services.Providers;
	using System.Linq;
	using NUnit.Framework;

	[TestFixture]
	public class QuerySemanticAnalysisTestCase
	{
		private StubModel _model;

		[SetUp]
		public void Init()
		{
			_model = new StubModel(
				m => m.EntitySet("catalogs", new List<Catalog2>().AsQueryable())
			);
		}

		private QueryAst AnalyzeAndConvert(string expression, ResourceType rt)
		{
			var exp = QueryExpressionParser.parse_filter(expression);
			// Console.WriteLine(exp4.ToStringTree());
			var tree = QuerySemanticAnalysis.analyze_and_convert(exp, rt);
			// Console.WriteLine(tree.ToStringTree());
			return tree;
		}

		//

		private readonly string[] numericOps	= new[] { "sub", "add", "mul", "div", "mod" };
		private readonly string[] relationalOps = new[] { "lt", "gt", "le", "ge" };

		private readonly string[][] numericTypes = new []
		                                           	{
		                                           		new [] { "Edm.Int32", "2" },
														new [] { "Edm.Int64", "3L" },
														new [] { "Edm.Decimal", "4m" },
														new [] { "Edm.Double", "5.0D" },
														new [] { "Edm.Single", "6F" },
		                                           	};

		[Test]
		public void NumericOps_On_NumericTypes()
		{
			foreach (var op in numericOps)
			foreach (var left in numericTypes)
			foreach (var right in numericTypes)
			{
				var exp = left[1] + " " + op + " " + right[1];
				var tree = AnalyzeAndConvert(exp, _model.GetResourceType("Catalog2").Value);
				Console.WriteLine("\t{0}\r\n{1}", exp, tree.ToStringTree());
			}
		}


//		[Test]
//		public void Valid_Add_of_bools()
//		{
//			var tree = AnalyzeAndConvert("true add false", _model.GetResourceType("Catalog2").Value);
//		}
//
//		[Test]
//		public void Invalid_Add_of_bools()
//		{
//			var tree = AnalyzeAndConvert("true add false", _model.GetResourceType("Catalog2").Value);
//		}
//		[Test]
//		public void Invalid_Sub_of_bools()
//		{
//			var tree = AnalyzeAndConvert("true sub false", _model.GetResourceType("Catalog2").Value);
//		}
//		[Test]
//		public void Invalid_Mul_of_bools()
//		{
//			var tree = AnalyzeAndConvert("true mul false", _model.GetResourceType("Catalog2").Value);
//		}
//		[Test]
//		public void Invalid_Div_of_bools()
//		{
//			var tree = AnalyzeAndConvert("true div false", _model.GetResourceType("Catalog2").Value);
//		}
//		[Test]
//		public void Invalid_Mod_of_bools()
//		{
//			var tree = AnalyzeAndConvert("true mod false", _model.GetResourceType("Catalog2").Value);
//		}
//
//
//
//		[Test]
//		public void Binary_Eq_ForInt32s()
//		{
//			var tree = AnalyzeAndConvert("1 add 2 eq 3", _model.GetResourceType("Catalog2").Value);
//		}
//
//		[Test]
//		public void Binary_Eq_ForDecimals()
//		{
//			var tree = AnalyzeAndConvert("1.2 add 2.3 eq 3.5", _model.GetResourceType("Catalog2").Value);
//
//		}
//
//		[Test]
//		public void PropNavigation()
//		{
//			var tree = AnalyzeAndConvert("Owner/Email eq 'ema@mail.com'", _model.GetResourceType("Catalog2").Value);
//
//		}

		public class Product2
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class User2
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public string Email { get; set; }
		}

		public class Catalog2
		{
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public IList<Product2> Products { get; set; }
			public User2 Owner { get; set; }
		}
	}
}
