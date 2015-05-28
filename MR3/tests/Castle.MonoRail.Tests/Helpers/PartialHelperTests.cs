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

namespace Castle.MonoRail.Tests.Helpers
{
    using System.Collections.Generic;
    using Castle.MonoRail.Helpers;
    using Castle.MonoRail.ViewEngines;
    using NUnit.Framework;

    [TestFixture]
    public class PartialHelperTests : HelperTestsBase
    {
        private StubView _view;
        private IEnumerable<string> _views;
        private IEnumerable<string> _layouts;

        public void SetUp(string controllerName, string areaName, string viewName)
        {
            Init(new ViewRequest() { ViewFolder = controllerName, GroupFolder = areaName, ViewName = viewName });

            _view = new StubView();

            var viewEngine = new StubViewEngine(
                (v, l) =>
                    {
                        _views = v;
                        _layouts = l;
                        return new ViewEngineResult(_view, null);
                    },
                v =>
                    {
                        _views = v;
                        return true;
                    });

            var viewEngines = new List<IViewEngine>();
            _serviceRegistry._viewRendererService.ViewEngines = viewEngines;
            viewEngines.Add(viewEngine);
        }

        [Test]
        public void Render_WithPartialName_RendersPartialView()
        {
            SetUp("home", null, "viewName");

            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());
            helper.Render("name");
            Assert.IsNotNull(_view._ctx); // if has a context, the view was rendered
        }

        [Test, ExpectedException]
        public void Render_WithPartialName_ThrowsIfViewDoesNotExist()
        {
            // does not return view instance
            var viewEngine = new StubViewEngine((v, l) => new ViewEngineResult(new [] { "location" }), null); 
            var viewEngines = new List<IViewEngine>();
            _serviceRegistry._viewRendererService.ViewEngines = viewEngines;
            viewEngines.Add(viewEngine);
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());
            
            helper.Render("name");
        }

        [Test]
        public void Render_WithoutAreaName_RestrictsSearchToControllerAndSharedFolder()
        {
            SetUp("home", null, "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());
            
            helper.Render("name");

            var views = new List<string>(_views);
            Assert.AreEqual(3, views.Count);
            Assert.Contains("/Views/home/name", views);
            Assert.Contains("/Views/home/_name", views);
            Assert.Contains("/Views/Shared/name", views);
        }

        [Test]
        public void Render_WithAreaName_SearchesInAreaControllerAndSharedFolder()
        {
            SetUp("home", "admin", "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());

            helper.Render("name");

            var views = new List<string>(_views);
            Assert.AreEqual(6, views.Count);
            Assert.Contains("/admin/Views/home/name", views);
            Assert.Contains("/admin/Views/Shared/name", views);
            Assert.Contains("/admin/Views/home/_name", views);
			Assert.Contains("/Views/admin/Shared/name", views);
            Assert.Contains("/Views/admin/home/_name", views);
			Assert.Contains("/Views/admin/home/name", views);
        }

        [Test]
        public void Render_WithModel_PassModelAlongThroughContext()
        {
            SetUp("home", null, "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());
            
            helper.Render("name", model);
            
            Assert.AreSame(model, _view._ctx.Model);
        }

        [Test]
        public void Render_WithBag_PassBagAlongThroughContext()
        {
            SetUp("home", null, "viewName");
            var helper = new PartialHelper<Customer>(_helperContext, new Customer(), new Dictionary<string, object>());
            var bag = new Dictionary<string, object>();

            helper.Render("name", bag);
            
            Assert.AreSame(bag, _view._ctx.Bag);
        }

        [Test]
        public void Render_WithModelAndBag_PassThemAlongThroughContext()
        {
            SetUp("home", null, "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());
            var bag = new Dictionary<string, object>();

            helper.Render("name", model, bag);

            Assert.AreSame(model, _view._ctx.Model);
            Assert.AreSame(bag, _view._ctx.Bag);
        }

        [Test]
        public void Exists_ForExistingView_ReturnsTrue()
        {
            SetUp("home", null, "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());

            bool result = helper.Exists("name");

            Assert.IsTrue(result);
        }

        [Test]
        public void Exists_ForNonExistingView_ReturnsFalse()
        {
            SetUp("home", null, "viewName");
			var viewEngine = new StubViewEngine((v, l) => new ViewEngineResult(new[] { "location" }), (v) => false);
            var viewEngines = new List<IViewEngine>();
            viewEngines.Add(viewEngine);
            _serviceRegistry._viewRendererService.ViewEngines = viewEngines;
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());

            bool result = helper.Exists("name");

            Assert.IsFalse(result);
        }

        [Test]
        public void Exists_WithoutArea_TakesOnlyControllerAndShared()
        {
            SetUp("home", null, "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());

            helper.Exists("name");

            var views = new List<string>(_views);
            Assert.AreEqual(3, views.Count);
            Assert.Contains("/Views/home/name", views);
            Assert.Contains("/Views/home/_name", views);
            Assert.Contains("/Views/Shared/name", views);
        }

        [Test]
        public void Exists_WithArea_UsesAreaName()
        {
            SetUp("home", "admin", "viewName");
            var model = new Customer();
            var helper = new PartialHelper<Customer>(_helperContext, model, new Dictionary<string, object>());

            helper.Exists("name");

            var views = new List<string>(_views);
            Assert.AreEqual(6, views.Count);
            Assert.Contains("/admin/Views/home/name", views);
            Assert.Contains("/admin/Views/home/_name", views);
            Assert.Contains("/admin/Views/Shared/name", views);
			Assert.Contains("/Views/admin/Shared/name", views);
			Assert.Contains("/Views/admin/home/name", views);
            Assert.Contains("/Views/admin/home/_name", views);
        }

        class Customer
        {
        }
    }
}
