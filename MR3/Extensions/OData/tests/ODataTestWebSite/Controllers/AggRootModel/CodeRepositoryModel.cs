namespace ODataTestWebSite.Controllers.AggRootModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Castle.MonoRail;

	public partial class CodeRepositoryModel : ODataModel
	{
		public CodeRepositoryModel() : base("ns", "container")
		{
			
		}

		public override void Initialize()
		{
			var source = new List<Repository>()
			{
			    new Repository() { Id = 1, Name = "repo1", 
			             		    Branches = new List<Branch>{ 
			             		    new Branch() { Id = 100, Name = "Initial Spike", 
			             		                    Revisions = new List<Revision>()
			             		                                {
			             		                                    new Revision() { FileName = "File1", Id = 3000, UserId = 102 }, 
			             		                                    new Revision() { FileName = "File2", Id = 3001, UserId = 102 },
			             		                                    new Revision() { FileName = "File1", Id = 3002, UserId = 101 },
			             		                                }},
			             		    new Branch() { Id = 101, Name = "develop", 
			             		                    Revisions = new List<Revision>()
			             		                                {
			             		                                    new Revision() { FileName = "File31", Id = 4000, UserId = 102 }, 
			             		                                    new Revision() { FileName = "File21", Id = 4001, UserId = 102 },
			             		                                    new Revision() { FileName = "File11", Id = 4002, UserId = 101 },
			             		                                }}} },
			};

			// how to map/configure:
			// MongoDbRef Single
			// IList<MongoDbRef> Many
			// IDictionary...

			this.EntitySet("Repositories", source.AsQueryable());
		}
	}
}