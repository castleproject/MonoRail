namespace ODataTestWebSite.Controllers.AggRootModel
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using Castle.MonoRail;

	[Area("models")]
	public partial class RootModelController : ODataController<CodeRepositoryModel>
	{
		public RootModelController() : base(new CodeRepositoryModel())
		{
		}

		public ActionResult Index()
		{
			return EmptyResult.Instance;
		}

//		public ActionResult<IEnumerable<Products>> GetProductsByRating()
//		{
//			
//		}
	}

	// /repositories(1)/branches(2)/revisions
}