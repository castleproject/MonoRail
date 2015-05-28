namespace ODataTestWebSite.Controllers.AggRootModel
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security.Principal;
	using System.Web;
	using Castle.MonoRail;

	public class RepositorySearchResult
	{
	}

	public partial class CodeRepositoryController : IODataEntitySubController<Repository>
	{
		[ODataOperation]
		public IEnumerable<Repository> Get_Search(string terms)
		{
			return Enumerable.Empty<Repository>();
		}

		[ODataOperation]
		public IEnumerable<RepositorySearchResult> Get_AdvancedSearch(string terms)
		{
			return Enumerable.Empty<RepositorySearchResult>();
		}

		public ActionResult Authorize(Repository repos, IPrincipal user, HttpRequestBase requestBase)
		{
			return EmptyResult.Instance;
		}

		public ActionResult AuthorizeMany(IEnumerable<Repository> repos, IPrincipal user, HttpRequestBase requestBase)
		{
			return EmptyResult.Instance;
		}

		// not meaningful for odata
		public ActionResult New()
		{
			return new ViewResult();
		}

		public ActionResult Get_View(Repository repos, HttpRequestBase requestBase)
		{
			if (requestBase.AcceptTypes.Any(at => at == "text/html"))
			{
				return new ViewResult<Repository>(repos);
			}

			return EmptyResult.Instance;
		}

		public ActionResult ViewMany(IEnumerable<Repository> repos)
		{
			return EmptyResult.Instance;
		}

		public ActionResult Post_Create(Model<Repository> repos, HttpRequestBase req, HttpResponseBase res)
		{
			var newModel = repos.Value;
			newModel.Id = 1000;

			return new ContentNegotiatedResult<Repository>(newModel) 
				{ StatusCode = HttpStatusCode.Created }
				.When(new [] { MediaTypes.Atom, MediaTypes.JSon, MediaTypes.Xml }, () => EmptyResult.Instance).
				ResolveActionResult(req.AcceptTypes, res);
		}

		// not meaningful for odata
		public ActionResult Edit(int id)
		{
			var repo = new Repository();

			return new ViewResult<Repository>(repo) { };
		}

		public ActionResult Put_Update(Model<Repository> repos)
		{
			// pretend to save it
			return EmptyResult.Instance;
		}

		[HttpMethod(HttpVerb.Delete)]
		public ActionResult Remove(Repository repos)
		{
			// pretend to delete it
			return EmptyResult.Instance;
		}
	}
}