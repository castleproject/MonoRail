namespace ODataTestWebSite.Controllers.AggRootModel
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;

	public class Revision
	{
		[Key]
		public int Id { get; set; }
		public string FileName { get; set; }
		public int UserId { get; set; }
	}

	public class Branch
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public IList<Revision> Revisions { get; set; }
	}

	public class Repository
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public IList<Branch> Branches { get; set; }
	}
}