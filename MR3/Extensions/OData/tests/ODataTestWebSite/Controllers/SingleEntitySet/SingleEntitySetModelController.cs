namespace ODataTestWebSite.Controllers.SingleEntitySet
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using Castle.MonoRail;

	public class Vendor
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public sbyte F1 { get; set; }
		public byte F2 { get; set; }
		public Int16 F3 { get; set; }
		public Int32 F4 { get; set; }
		public Int64 F5 { get; set; }
		public float F6 { get; set; }
		public double F7 { get; set; }
		public decimal F8 { get; set; }
		public DateTime F9 { get; set; }
		public byte[] F10 { get; set; }
	}

	public class SingleESODataModel : ODataModel
	{
		public SingleESODataModel() : base("ns", "container")
		{
			var source = new List<Vendor>();
			this.EntitySet("Vendors", source.AsQueryable());
		}
	}

	public partial class SingleEntitySetModelController : ODataController<SingleESODataModel>
	{
		public SingleEntitySetModelController() : base(new SingleESODataModel())
		{
		}
	}
}