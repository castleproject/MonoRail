
        
        var mrRoutes = {};

        function initializeRouteModule(vpath) {
            vpath = vpath === '/' ? '' : vpath;

            function appendNamespace (namespaceString) {
                var parts = namespaceString.split('.'),
                    parent = mrRoutes,
                    currentPart = '';    

                for (var i = 0, length = parts.length; i < length; i++) {
                    currentPart = parts[i];
                    parent[currentPart] = parent[currentPart] || {};
                    parent = parent[currentPart];
                }

                return parent;
            }
            var AggRootModel = {
                
                Index: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/AggRootModel' : '/AggRootModel?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/AggRootModel'; },
                    del: function() { return vpath + '/AggRootModel'; },
                    put: function() { return vpath + '/AggRootModel'; }
                },

                Process: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/AggRootModel/Process' : '/AggRootModel/Process?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/AggRootModel/Process'; },
                    del: function() { return vpath + '/AggRootModel/Process'; },
                    put: function() { return vpath + '/AggRootModel/Process'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.AggRootModel');

            ns.AggRootModel = AggRootModel;
        
            var BranchRepository = {
                
                Authorize: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/Authorize' : '/BranchRepository/Authorize?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/Authorize'; },
                    del: function() { return vpath + '/BranchRepository/Authorize'; },
                    put: function() { return vpath + '/BranchRepository/Authorize'; }
                },

                Create: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/Create' : '/BranchRepository/Create?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/Create'; },
                    del: function() { return vpath + '/BranchRepository/Create'; },
                    put: function() { return vpath + '/BranchRepository/Create'; }
                },

                New: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/New' : '/BranchRepository/New?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/New'; },
                    del: function() { return vpath + '/BranchRepository/New'; },
                    put: function() { return vpath + '/BranchRepository/New'; }
                },

                Remove: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/Remove' : '/BranchRepository/Remove?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/Remove'; },
                    del: function() { return vpath + '/BranchRepository/Remove'; },
                    put: function() { return vpath + '/BranchRepository/Remove'; }
                },

                View: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/View' : '/BranchRepository/View?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/View'; },
                    del: function() { return vpath + '/BranchRepository/View'; },
                    put: function() { return vpath + '/BranchRepository/View'; }
                },

                ViewAll: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/ViewAll' : '/BranchRepository/ViewAll?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/ViewAll'; },
                    del: function() { return vpath + '/BranchRepository/ViewAll'; },
                    put: function() { return vpath + '/BranchRepository/ViewAll'; }
                },

                _Put_Update: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/BranchRepository/_Put_Update' : '/BranchRepository/_Put_Update?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/BranchRepository/_Put_Update'; },
                    del: function() { return vpath + '/BranchRepository/_Put_Update'; },
                    put: function() { return vpath + '/BranchRepository/_Put_Update'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.AggRootModel');

            ns.BranchRepository = BranchRepository;
        
            var CodeRepository = {
                
                Authorize: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/Authorize' : '/CodeRepository/Authorize?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/Authorize'; },
                    del: function() { return vpath + '/CodeRepository/Authorize'; },
                    put: function() { return vpath + '/CodeRepository/Authorize'; }
                },

                AuthorizeMany: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/AuthorizeMany' : '/CodeRepository/AuthorizeMany?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/AuthorizeMany'; },
                    del: function() { return vpath + '/CodeRepository/AuthorizeMany'; },
                    put: function() { return vpath + '/CodeRepository/AuthorizeMany'; }
                },

                Create: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/Create' : '/CodeRepository/Create?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/Create'; },
                    del: function() { return vpath + '/CodeRepository/Create'; },
                    put: function() { return vpath + '/CodeRepository/Create'; }
                },

                Edit: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/Edit' : '/CodeRepository/Edit?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/Edit'; },
                    del: function() { return vpath + '/CodeRepository/Edit'; },
                    put: function() { return vpath + '/CodeRepository/Edit'; }
                },

                New: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/New' : '/CodeRepository/New?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/New'; },
                    del: function() { return vpath + '/CodeRepository/New'; },
                    put: function() { return vpath + '/CodeRepository/New'; }
                },

                Remove: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/Remove' : '/CodeRepository/Remove?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/Remove'; },
                    del: function() { return vpath + '/CodeRepository/Remove'; },
                    put: function() { return vpath + '/CodeRepository/Remove'; }
                },

                Update: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/Update' : '/CodeRepository/Update?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/Update'; },
                    del: function() { return vpath + '/CodeRepository/Update'; },
                    put: function() { return vpath + '/CodeRepository/Update'; }
                },

                View: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/View' : '/CodeRepository/View?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/View'; },
                    del: function() { return vpath + '/CodeRepository/View'; },
                    put: function() { return vpath + '/CodeRepository/View'; }
                },

                ViewAll: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/ViewAll' : '/CodeRepository/ViewAll?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/ViewAll'; },
                    del: function() { return vpath + '/CodeRepository/ViewAll'; },
                    put: function() { return vpath + '/CodeRepository/ViewAll'; }
                },

                ViewMany: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/ViewMany' : '/CodeRepository/ViewMany?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/ViewMany'; },
                    del: function() { return vpath + '/CodeRepository/ViewMany'; },
                    put: function() { return vpath + '/CodeRepository/ViewMany'; }
                },

                _Put_Update: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/CodeRepository/_Put_Update' : '/CodeRepository/_Put_Update?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/CodeRepository/_Put_Update'; },
                    del: function() { return vpath + '/CodeRepository/_Put_Update'; },
                    put: function() { return vpath + '/CodeRepository/_Put_Update'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.AggRootModel');

            ns.CodeRepository = CodeRepository;
        
            var RevisionRepository = {
                
                Authorize: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/Authorize' : '/RevisionRepository/Authorize?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/Authorize'; },
                    del: function() { return vpath + '/RevisionRepository/Authorize'; },
                    put: function() { return vpath + '/RevisionRepository/Authorize'; }
                },

                AuthorizeMany: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/AuthorizeMany' : '/RevisionRepository/AuthorizeMany?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/AuthorizeMany'; },
                    del: function() { return vpath + '/RevisionRepository/AuthorizeMany'; },
                    put: function() { return vpath + '/RevisionRepository/AuthorizeMany'; }
                },

                Create: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/Create' : '/RevisionRepository/Create?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/Create'; },
                    del: function() { return vpath + '/RevisionRepository/Create'; },
                    put: function() { return vpath + '/RevisionRepository/Create'; }
                },

                Remove: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/Remove' : '/RevisionRepository/Remove?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/Remove'; },
                    del: function() { return vpath + '/RevisionRepository/Remove'; },
                    put: function() { return vpath + '/RevisionRepository/Remove'; }
                },

                View: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/View' : '/RevisionRepository/View?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/View'; },
                    del: function() { return vpath + '/RevisionRepository/View'; },
                    put: function() { return vpath + '/RevisionRepository/View'; }
                },

                ViewAll: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/ViewAll' : '/RevisionRepository/ViewAll?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/ViewAll'; },
                    del: function() { return vpath + '/RevisionRepository/ViewAll'; },
                    put: function() { return vpath + '/RevisionRepository/ViewAll'; }
                },

                _Put_Update: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/RevisionRepository/_Put_Update' : '/RevisionRepository/_Put_Update?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/RevisionRepository/_Put_Update'; },
                    del: function() { return vpath + '/RevisionRepository/_Put_Update'; },
                    put: function() { return vpath + '/RevisionRepository/_Put_Update'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.AggRootModel');

            ns.RevisionRepository = RevisionRepository;
        
            var Categories = {
                
                Access: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/Access' : '/Categories/Access?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/Access'; },
                    del: function() { return vpath + '/Categories/Access'; },
                    put: function() { return vpath + '/Categories/Access'; }
                },

                AccessMany: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/AccessMany' : '/Categories/AccessMany?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/AccessMany'; },
                    del: function() { return vpath + '/Categories/AccessMany'; },
                    put: function() { return vpath + '/Categories/AccessMany'; }
                },

                Authorize: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/Authorize' : '/Categories/Authorize?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/Authorize'; },
                    del: function() { return vpath + '/Categories/Authorize'; },
                    put: function() { return vpath + '/Categories/Authorize'; }
                },

                Create: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/Create' : '/Categories/Create?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/Create'; },
                    del: function() { return vpath + '/Categories/Create'; },
                    put: function() { return vpath + '/Categories/Create'; }
                },

                Remove: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/Remove' : '/Categories/Remove?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/Remove'; },
                    del: function() { return vpath + '/Categories/Remove'; },
                    put: function() { return vpath + '/Categories/Remove'; }
                },

                Update: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/Update' : '/Categories/Update?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/Update'; },
                    del: function() { return vpath + '/Categories/Update'; },
                    put: function() { return vpath + '/Categories/Update'; }
                },

                View: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/View' : '/Categories/View?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/View'; },
                    del: function() { return vpath + '/Categories/View'; },
                    put: function() { return vpath + '/Categories/View'; }
                },

                ViewAll: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/ViewAll' : '/Categories/ViewAll?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/ViewAll'; },
                    del: function() { return vpath + '/Categories/ViewAll'; },
                    put: function() { return vpath + '/Categories/ViewAll'; }
                },

                _Put_Update: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/Categories/_Put_Update' : '/Categories/_Put_Update?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/Categories/_Put_Update'; },
                    del: function() { return vpath + '/Categories/_Put_Update'; },
                    put: function() { return vpath + '/Categories/_Put_Update'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.HierarchicalModel');

            ns.Categories = Categories;
        
            var HierarchicalModel = {
                
                Process: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/HierarchicalModel/Process' : '/HierarchicalModel/Process?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/HierarchicalModel/Process'; },
                    del: function() { return vpath + '/HierarchicalModel/Process'; },
                    put: function() { return vpath + '/HierarchicalModel/Process'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.HierarchicalModel');

            ns.HierarchicalModel = HierarchicalModel;
        
            var Home = {
                
                Index: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/' : '/?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/'; },
                    del: function() { return vpath + '/'; },
                    put: function() { return vpath + '/'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers');

            ns.Home = Home;
        
            var SingleEntitySetModel = {
                
                Process: {
                    get: function (params) {
                        return vpath + (params == undefined ? '/SingleEntitySetModel/Process' : '/SingleEntitySetModel/Process?' + jQuery.param(params));
                    },
                    post: function() { return vpath + '/SingleEntitySetModel/Process'; },
                    del: function() { return vpath + '/SingleEntitySetModel/Process'; },
                    put: function() { return vpath + '/SingleEntitySetModel/Process'; }
                }
            };
        
            var ns = appendNamespace('ODataTestWebSite.Controllers.SingleEntitySet');

            ns.SingleEntitySetModel = SingleEntitySetModel;
        
        }
