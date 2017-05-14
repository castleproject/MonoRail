namespace Castle.MonoRail.Extension.OData.Tests
{
	using System;
	using System.Collections.Specialized;

	static class Utils
	{
		public static NameValueCollection BuildFromQS(string qs)
		{
			var parameters = new NameValueCollection();
			foreach (var entry in qs.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries))
			{
				var parts = entry.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
				parameters.Add(parts[0], parts[1]);
			}
			return parameters;
		}
	}
}
