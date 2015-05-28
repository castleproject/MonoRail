#region License
//  Copyright 2004-2012 Castle Project - http://www.castleproject.org/
//  Hamilton Verissimo de Oliveira and individual contributors as indicated. 
//  See the committers.txt/contributors.txt in the distribution for a 
//  full listing of individual contributors.
// 
//  This is free software; you can redistribute it and/or modify it
//  under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 3 of
//  the License, or (at your option) any later version.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this software; if not, write to the Free
//  Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
//  02110-1301 USA, or see the FSF site: http://www.fsf.org.
#endregion

namespace Castle.MonoRail.Tests.Mvc
{
	using System.Linq;
	using FluentAssertions;
	using NUnit.Framework;
	using ViewEngines;

	[TestFixture]
	public class DefaultViewFolderLayoutTestCase
	{
		IViewFolderLayout serviceNoVPath = new DefaultViewFolderLayout("") as IViewFolderLayout;
		IViewFolderLayout serviceWithVPath = new DefaultViewFolderLayout("/app") as IViewFolderLayout;
		IViewFolderLayout serviceWithComplexVPath = new DefaultViewFolderLayout("/0.0.0/Service/") as IViewFolderLayout;

		[Test]
		public void DefaultVPath_ProcessLocationForControllerDefaultName_OutputsCorrectPaths()
		{
			var req = new ViewRequest() {ViewFolder = "home", DefaultName = "index"};
			serviceNoVPath.ProcessLocations(req, new StubHttpContext());
			req.ViewLocations.Count().Should().Be(3);
			req.ViewLocations.Contains("/Views/home/index");
            req.ViewLocations.Contains("/Views/home/_index");
            req.ViewLocations.Contains("/Views/Shared/index");
		}

		[Test]
		public void AppVPath_ProcessLocationForControllerDefaultName_OutputsCorrectPaths()
		{
			var req = new ViewRequest() { ViewFolder = "home", DefaultName = "index" };
			serviceWithVPath.ProcessLocations(req, new StubHttpContext());
			req.ViewLocations.Count().Should().Be(3);
			req.ViewLocations.Contains("/app/Views/home/index");
            req.ViewLocations.Contains("/app/Views/home/_index");
            req.ViewLocations.Contains("/app/Views/Shared/index");
		}

		[Test]
		public void ComplexVPath_ProcessLocationForControllerDefaultName_OutputsCorrectPaths()
		{
			var req = new ViewRequest() { ViewFolder = "home", DefaultName = "index" };
			serviceWithComplexVPath.ProcessLocations(req, new StubHttpContext());
			req.ViewLocations.Count().Should().Be(3);
            req.ViewLocations.Contains("/0.0.0/Service/Views/home/index");
            req.ViewLocations.Contains("/0.0.0/Service/Views/home/_index");
            req.ViewLocations.Contains("/0.0.0/Service/Views/Shared/index");
		}
	}
}
