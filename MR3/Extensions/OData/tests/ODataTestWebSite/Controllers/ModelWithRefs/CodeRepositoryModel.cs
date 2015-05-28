namespace ODataTestWebSite.Controllers.ModelWithRefs
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
				new Repository() 
				{ 
					Id = 1, Name = "repo1", 
			        Branches = new List<Ref<Branch>>{ 
			        new Ref<Branch>(new Branch() { Id = 100, Name = "Initial Spike", 
			             		    Revisions = new List<Ref<Revision>>()
			             		                {
			             		                    new Ref<Revision>(new Revision() { FileName = "File1", Id = 3000, UserId = 102 }), 
			             		                    new Ref<Revision>(new Revision() { FileName = "File2", Id = 3001, UserId = 102 }),
			             		                    new Ref<Revision>(new Revision() { FileName = "File1", Id = 3002, UserId = 101 })
			             		                }}),
			        new Ref<Branch>(new Branch() { Id = 101, Name = "develop", 
			             		    Revisions = new List<Ref<Revision>>()
			             		                {
			             		                    new Ref<Revision>(new Revision() { FileName = "File31", Id = 4000, UserId = 102 }), 
			             		                    new Ref<Revision>(new Revision() { FileName = "File21", Id = 4001, UserId = 102 }),
			             		                    new Ref<Revision>(new Revision() { FileName = "File11", Id = 4002, UserId = 101 }),
			             		                }})} },
			};

			this.EntitySet("Repositories", source.AsQueryable())
				.ForProperty<IList<Ref<Branch>>, IList<Branch>>(
					r => r.Branches, 
					bs => bs.Select(r => r.Entity).ToList(), 
					bs => bs.Select(v => new Ref<Branch>(v)).ToList())
				.ForProperty<IList<Ref<Revision>>, IList<Revision>>(
					r => r.Branches.Single().Entity.Revisions,
					bs => bs.Select(r => r.Entity).ToList(),
					bs => bs.Select(v => new Ref<Revision>(v)).ToList())
				;
		}
	}
}