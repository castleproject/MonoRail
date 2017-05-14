namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Data.Services.Providers;
	using System.Linq;
	using System.Linq.Expressions;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class AstLinqTranslatorTestCase
	{
		private StubModel _model;
		private ResourceType _catalogRt;
		private List<Catalog2> _catalogs;

		[SetUp]
		public void Init()
		{
			_catalogs = new List<Catalog2>
			            	{
			            		new Catalog2() { Id = 1, Name = "catalog 1", IsPublished = false, Owner = new User2() { Email = "email1@m.com", Name = "Mary"} }, 
								new Catalog2() { Id = 2, Name = "catalog 2", IsPublished = true , Owner = new User2() { Email = "email2@m.com", Name = "John"} }, 
								new Catalog2() { Id = 3, Name = "catalog 3", IsPublished = false, Owner = new User2() { Email = "email3@m.com", Name = "Jeff"} }, 
								new Catalog2() { Id = 4, Name = "catalog 4", IsPublished = true , Owner = new User2() { Email = "email4@m.com", Name = "Andrew"} }, 
								new Catalog2() { Id = 5, Name = "catalog 5", IsPublished = false, Owner = new User2() { Email = "email5@m.com", Name = "Mary"} }, 
			            	};

			_model = new StubModel(
				m => m.EntitySet("catalogs", _catalogs.AsQueryable())
			);

			_catalogRt = _model.GetResourceType("Catalog2").Value;
		}

		private Expression<Func<T, bool>> BuildLinqExpressionPredicate<T>(string expression, ResourceType rt)
		{
			var exp = QueryExpressionParser.parse_filter(expression);
			// Console.WriteLine(exp4.ToStringTree());

			var tree = QuerySemanticAnalysis.analyze_and_convert(exp, rt);
			Console.WriteLine(tree.ToStringTree());
			
			return AstLinqTranslator.build_linq_exp_predicate<T>(typeof(T), tree);
		}

		private static IQueryable<T> ApplyOrderByExpression<T>(IQueryable<T> source, string expression, ResourceType rt)
		{
			var exp = QueryExpressionParser.parse_orderby(expression);
			// Console.WriteLine(exp4.ToStringTree());
			var tree = QuerySemanticAnalysis.analyze_and_convert_orderby(exp, rt);
			// Console.WriteLine(tree.ToStringTree());
			return AstLinqTranslator.typed_queryable_orderby<T>(source, tree) as IQueryable<T>;
		}

		[Test]
		public void OrderBy_Asc_StringProperty()
		{
			var result = ApplyOrderByExpression(_catalogs.AsQueryable(), "Name", _catalogRt);
			result.Should().NotBeNull();
			var names = result.Select(c => c.Name);
			var expected = _catalogs.OrderBy(c => c.Name).Select(c => c.Name);
			names.Should().Equal(expected);
		}

		[Test]
		public void OrderBy_Desc_StringProperty()
		{
			var result = ApplyOrderByExpression(_catalogs.AsQueryable(), "Name desc", _catalogRt);
			result.Should().NotBeNull();
			var names = result.Select(c => c.Name);
			var expected = _catalogs.OrderByDescending(c => c.Name).Select(c => c.Name);
			names.Should().Equal(expected);
		}

		[Test]
		public void OrderBy_Asc_Int32Property()
		{
			var result = ApplyOrderByExpression(_catalogs.AsQueryable(), "Id", _catalogRt);
			result.Should().NotBeNull();
			var resultElems = result.Select(c => c.Id);
			var expected = _catalogs.OrderBy(c => c.Id).Select(c => c.Id);
			resultElems.Should().Equal(expected);
		}

		[Test]
		public void OrderBy_Desc_Int32Property()
		{
			var result = ApplyOrderByExpression(_catalogs.AsQueryable(), "Id desc", _catalogRt);
			result.Should().NotBeNull();
			var resultElems = result.Select(c => c.Id);
			var expected = _catalogs.OrderByDescending(c => c.Id).Select(c => c.Id);
			resultElems.Should().Equal(expected);
		}

		[Test]
		public void OrderBy_Asc_StringProperty_AndThen_Desc_Int32Property()
		{
			var result = ApplyOrderByExpression(_catalogs.AsQueryable(), "Name, Id desc", _catalogRt);
			result.Should().NotBeNull();
			var names = result.Select(c => c.Name);
			var expected = _catalogs.OrderBy(c => c.Name).ThenByDescending(c => c.Id).Select(c => c.Name);
			names.Should().Equal(expected);
		}


		[Test]
		public void Property_Eq_String()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Name eq 'catalog 1'", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(1);
			results.ElementAt(0).Name.Should().Be("catalog 1");
		}

		[Test]
		public void NestedProperty_Eq_String()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Owner/Name eq 'Mary'", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(2);
			results.Where(c => c.Owner.Name == "Mary").Should().HaveCount(2);
		}

		[Test]
		public void Property_Eq_Int32()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id eq 1", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(1);
			results.ElementAt(0).Name.Should().Be("catalog 1");
		}

		[Test]
		public void Property_Eq_Bool()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("IsPublished eq true", _catalogRt);
			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(2);

			exp = BuildLinqExpressionPredicate<Catalog2>("IsPublished eq false", _catalogRt);
			results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count - 2);
		}

		[Test]
		public void BinaryNumericPromotion_Property_Eq_Int64()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id eq 1L", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(1);
			results.ElementAt(0).Name.Should().Be("catalog 1");
		}

		[Test]
		public void BinaryNumericPromotion_Property_Eq_Single()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id eq 1.0", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(1);
			results.ElementAt(0).Name.Should().Be("catalog 1");
		}

		[Test]
		public void Property_Ne_Int32()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id ne 1", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count - 1);
		}

		[Test]
		public void Property_Ne_NegateOfInt32()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id ne -1", _catalogRt);
			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count);

			exp = BuildLinqExpressionPredicate<Catalog2>("-Id ne 1", _catalogRt);
			results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count);
		}

		[Test]
		public void Property_Ne_NegateOfSingle()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id ne -1.0", _catalogRt);
			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count);
		}

		[Test]
		public void Property_Eq_Not()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("not (Id eq 1)", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count - 1);
		}

		[Test]
		public void Property_GreaterThan()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id gt 1", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count - 1);
		}

		[Test]
		public void Property_GreaterEqualThan()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id ge 1", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(_catalogs.Count);
		}

		[Test]
		public void Property_LessThan()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id lt 1", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(0);
		}

		[Test]
		public void Property_LessEqualThan()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id le 1", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(1);
		}


		[Test]
		public void Property_GreaterThan_And_Eq()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id gt 1 and Name eq 'catalog 2'", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(1);
		}

		[Test]
		public void Property_GreaterThan_Or_Eq()
		{
			var exp = BuildLinqExpressionPredicate<Catalog2>("Id eq 2 or Name eq 'catalog 1'", _catalogRt);

			var results = _catalogs.Where(exp.Compile());
			results.Count().Should().Be(2);
		}

		private readonly string[] numericOps = new[] { "sub", "add", "mul", "div", "mod" };
		private readonly string[] relationalOps = new[] { "lt", "gt", "le", "ge" };
		private readonly string[] eqOps = new[] { "eq", "ne" };
		private readonly string[][] numericTypes = new[]
		                                           	{
		                                           		new [] { "Edm.Int32", "2", "PropInt32" },
														new [] { "Edm.Int64", "3L", "PropInt64" },
														new [] { "Edm.Decimal", "4m", "PropDec" },
														new [] { "Edm.Double", "5.0D", "PropSingle" },
														new [] { "Edm.Single", "6F", "PropDouble" },
		                                           	};

		[Test]
		public void EqualityOps_On_NumericTypes_Properties()
		{
			foreach (var op in eqOps)
				foreach (var left in numericTypes)
					foreach (var right in numericTypes)
					{
						var rawEx = left[2] + " " + op + " " + right[2];
						var exp = BuildLinqExpressionPredicate<Catalog2>(rawEx, _catalogRt);
						var results = _catalogs.Where(exp.Compile());
						results.Count();
					}
		}

		[Test]
		public void RelationalsOps_On_NumericTypes_Properties()
		{
			foreach (var op in relationalOps)
				foreach (var left in numericTypes)
					foreach (var right in numericTypes)
					{
						var rawEx = left[2] + " " + op + " " + right[2];
						var exp = BuildLinqExpressionPredicate<Catalog2>(rawEx, _catalogRt);
						var results = _catalogs.Where(exp.Compile());
						results.Count();
					}
		}

		[Test]
		public void NumericOps_On_NumericTypes_Properties()
		{
			foreach (var op in numericOps)
				foreach (var left in numericTypes)
					foreach (var right in numericTypes)
					{
						var rawEx = left[2] + " eq " + left[2] + " " + op + " " + right[2];
						var exp = BuildLinqExpressionPredicate<Catalog2>(rawEx, _catalogRt);
						var f = exp.Compile();
					}
		}

		[Test]
		public void NumericOps_On_NumericTypes_Literal()
		{
			foreach (var op in numericOps)
				foreach (var left in numericTypes)
					foreach (var right in numericTypes)
					{
						var rawEx = left[2] + " eq " + left[1] + " " + op + " " + right[1];
						var exp = BuildLinqExpressionPredicate<Catalog2>(rawEx, _catalogRt);
						var results = _catalogs.Where(exp.Compile());
						results.Count();
					}
		}


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
			public bool IsPublished { get; set; }
			public int PropInt32 { get; set; }
			public long PropInt64 { get; set; }
			public decimal PropDec { get; set; }
			public Single PropSingle { get; set; }
			public double PropDouble { get; set; }
		}
	}
}
