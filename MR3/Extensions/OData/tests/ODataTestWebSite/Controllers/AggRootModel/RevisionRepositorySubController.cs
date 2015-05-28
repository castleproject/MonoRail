namespace ODataTestWebSite.Controllers.AggRootModel
{
	using System.Collections.Generic;
	using Castle.MonoRail;

	public partial class RevisionRepositoryController : IODataEntitySubController<Revision>
	{
		public ActionResult AuthorizeMany(IEnumerable<Revision> revisions)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Authorize(Revision revision)
		{
			return EmptyResult.Instance;
		}

		// POST /Repositories(1)/Branches(2)/revisions
		// Atom xml
		public ActionResult Create(Repository repos, Branch branch, Model<Revision> revision)
		{

			return EmptyResult.Instance;
		}
	}
}