namespace ODataTestWebSite.Controllers.HierarchicalModel
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using Castle.MonoRail;

	public class Category
	{
		public Category()
		{
			Children = new List<Category>();
		}

		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public Category Parent { get; set; }
		public IList<Category> Children { get; set; }
	}


	public class HierarchicalODataModel : ODataModel
	{
		public HierarchicalODataModel() : base("ns", "container")
		{
			
		}

		public override void Initialize()
		{
			var source = new List<Category>();
			this.EntitySet("Categories", source.AsQueryable());
		}
	}


	public partial class HierarchicalModelController : ODataController<HierarchicalODataModel>
	{
		public HierarchicalModelController() : base(new HierarchicalODataModel())
		{
		}
	}

	public partial class CategoriesController : IODataEntitySubController<Category>
	{
		public ActionResult Access(Model<Category> category)
		{
			return EmptyResult.Instance;
		}

		public ActionResult AccessMany(IEnumerable<Category> categories)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Create(Model<Category> category)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Update(Model<Category> category)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Remove(Category category)
		{
			return EmptyResult.Instance;
		}
	}

}