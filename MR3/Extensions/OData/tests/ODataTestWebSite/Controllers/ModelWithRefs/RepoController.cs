namespace ODataTestWebSite.Controllers.ModelWithRefs
{
	using Castle.MonoRail;

	public class RepoController : ODataController<CodeRepositoryModel>
	{
		public RepoController() : base(new CodeRepositoryModel())
		{
		}
	}

	public class RepositorySubController : IODataEntitySubController<Repository>
	{
		public ActionResult Create(Model<Repository> repository)
		{
			return EmptyResult.Instance;
		}
		
		public ActionResult Update(Model<Repository> repository)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Remove(Model<Repository> repository)
		{
			return EmptyResult.Instance;
		}
	}

	public class BranchSubController : IODataEntitySubController<Branch>
	{
		public ActionResult Create(Repository repository, Model<Branch> branch)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Update(Repository repository, Model<Branch> branch)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Remove(Repository repository, Model<Branch> branch)
		{
			return EmptyResult.Instance;
		}
	}

	public class RevisionSubController : IODataEntitySubController<Revision>
	{
		public ActionResult Create(Repository repository, Branch branch, Model<Revision> revision)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Update(Repository repository, Branch branch, Model<Revision> revision)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Remove(Repository repository, Branch branch, Model<Revision> revision)
		{
			return EmptyResult.Instance;
		}
	}
}