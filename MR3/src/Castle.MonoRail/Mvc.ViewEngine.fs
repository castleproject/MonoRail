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

namespace Castle.MonoRail.ViewEngines

    open System
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.IO
    open System.Web
    open Castle.MonoRail.Hosting

    // optional extension point to allow for custom layouts in projects (is it worthwhile?)
    [<Interface; AllowNullLiteral>]
    type IViewFolderLayout = 
        abstract member ProcessLocations : req:ViewRequest * http:HttpContextBase -> unit
        abstract member ProcessPartialLocations : req:ViewRequest * http:HttpContextBase -> unit

    
    // defines the default structure for views location
    [<Export(typeof<IViewFolderLayout>)>]
    type DefaultViewFolderLayout 
        [<ImportingConstructor>] ([<Import("AppPath")>] appPath:string) = 
        
        let _contextualAppPath : Ref<string> = ref null

        let pre_process paths = 
            let appliedVP = ( paths |> List.map (fun path -> Helpers.url_path_combine appPath path ) ) 
            
            if (!_contextualAppPath) = null then 
                appliedVP
            else 
                ( paths |> List.map (fun path -> Helpers.url_path_combine appPath ((!_contextualAppPath) + path)) ) @ appliedVP

        let compute_view_locations areaname (viewname:string) (controller:string) (verb:string) = 
            let hasSlash = viewname.IndexOf '/' <> -1
            let spec_view = 
                if areaname <> null then 
                    pre_process [
                        (areaname + "/Views/" + (if hasSlash then viewname else controller + "/" + viewname))
                        (areaname + "/Views/" + (if hasSlash then viewname else controller + "/" + verb + "_" + viewname))
                        ("/Views/" + areaname + "/" + (if hasSlash then viewname else controller + "/" + viewname))
                        ("/Views/" + areaname + "/" + (if hasSlash then viewname else controller + "/" + verb + "_" + viewname))
                    ] 
                    
                else 
                    pre_process [
                        ("/Views/" + (if hasSlash then viewname else controller + "/" + viewname))
                        ("/Views/" + (if hasSlash then viewname else controller + "/" + verb + "_" + viewname))
                    ]
            let shared_view = 
                if areaname <> null then 
                    pre_process [
                        "/" + areaname + "/Views/Shared/" + viewname
                        "/Views/" + areaname + "/Shared/" + viewname
                    ]
                else 
                    pre_process [
                        "/Views/Shared/" + viewname
                    ]
            spec_view @ shared_view
        
        let compute_layout_locations areaname (layout:string) (controller:string) = 
            let hasSlash = layout.IndexOf '/' <> -1
            let lpath = 
                if areaname <> null then 
                    pre_process [
                        "/" + areaname + "/Views/" + (if hasSlash then layout else controller + "/" + layout)
                        "/Views/" + areaname + "/" + (if hasSlash then layout else controller + "/" + layout)
                    ]
                else 
                    pre_process [
                        "/Views/" + (if hasSlash then layout else controller + "/" + layout)
                    ]
            let lshared = 
                if areaname <> null then 
                    pre_process [
                        "/" + areaname + "/Views/Shared/" + layout
                        "/Views/" + areaname + "/Shared/" + layout
                    ]
                else 
                    pre_process [
                        "/Views/Shared/" + layout
                    ]
            lpath @ lshared

        [<Import("ContextualAppPath", AllowDefault=true)>]
        member x.ContextualAppPath with get() = !_contextualAppPath and set(v) = _contextualAppPath := v

        interface IViewFolderLayout with
            member x.ProcessLocations (req:ViewRequest, http:System.Web.HttpContextBase) = 
                if not req.WasProcessed then
                    if req.ViewName = null then
                        req.ViewName <- req.DefaultName
                    req.ViewLocations <- compute_view_locations req.GroupFolder req.ViewName req.ViewFolder http.Request.HttpMethod
                    let layout = req.OuterViewName
                    if (layout <> null) then 
                        req.LayoutLocations <- compute_layout_locations req.GroupFolder layout req.ViewFolder
                    req.SetProcessed()

            member x.ProcessPartialLocations (req:ViewRequest, http:System.Web.HttpContextBase) = 
                if not req.WasProcessed then
                    if req.ViewName = null then
                        req.ViewName <- req.DefaultName
                    req.ViewLocations <- compute_view_locations req.GroupFolder req.ViewName req.ViewFolder http.Request.HttpMethod
                    req.SetProcessed()


    [<Export;AllowNullLiteral>]
    type ViewRendererService () =
        let mutable _viewEngines = System.Linq.Enumerable.Empty<IViewEngine>()
        let mutable _viewFolderLayout : IViewFolderLayout = null

        let rec rec_find_viewengine viewLocations layoutLocations (enumerator:IEnumerator<IViewEngine>) (reslist:List<ViewEngineResult>) : List<ViewEngineResult> =
            if enumerator.MoveNext() then
                let item = enumerator.Current
                let res = item.ResolveView (viewLocations, layoutLocations)
                if (res.IsSuccessful) then
                    reslist.Clear()
                    reslist.Add res
                    reslist
                else
                    reslist.Add res
                    rec_find_viewengine viewLocations layoutLocations enumerator reslist
            else 
                reslist

        and resolve_viewengine viewLocations layoutLocations : ViewEngineResult = 
            use enumerator = _viewEngines.GetEnumerator()
            let searched = List<ViewEngineResult>()
            let results = rec_find_viewengine viewLocations layoutLocations enumerator searched

            if Seq.isEmpty(results) then
                ExceptionBuilder.NoViewEnginesFound()
            else 
                let h = Seq.head(results)
                if (h.IsSuccessful) 
                then h
                else 
                    let allLocationsSearched = 
                        searched 
                        |> Seq.collect (fun s -> s.LocationsSearch)
                        |> Seq.toArray // |> Array.toSeq // forcing eval
                    ExceptionBuilder.ViewNotFound( allLocationsSearched ) 

        [<ImportMany(AllowRecomposition=true)>]
        member x.ViewEngines  with set v = _viewEngines <- v

        [<Import>]
        member x.ViewFolderLayout  with set v = _viewFolderLayout <- v

        member x.HasView (viewreq:ViewRequest, context:HttpContextBase) = 
            _viewFolderLayout.ProcessLocations (viewreq, context)
            x.InternalHasView(viewreq)

        member x.HasPartialView (viewreq:ViewRequest, context:HttpContextBase) = 
            _viewFolderLayout.ProcessPartialLocations (viewreq, context)
            x.InternalHasView(viewreq)

        member internal x.InternalHasView(viewreq:ViewRequest) = 
            let result = _viewEngines |> Seq.tryFindIndex (fun ve -> ve.HasView(viewreq.ViewLocations) )
            result.IsSome

        member x.RenderPartial (viewreq:ViewRequest, context:HttpContextBase, propbag:IDictionary<string,obj>, model, output:TextWriter) =
            _viewFolderLayout.ProcessPartialLocations (viewreq, context)

            let res = resolve_viewengine (viewreq.ViewLocations) null 
            let view = res.View
            let viewCtx = ViewContext(context, propbag, model, viewreq)
            view.Process (output, viewCtx)

        member x.Render (viewreq:ViewRequest, context:HttpContextBase, propbag:IDictionary<string,obj>, model, output:TextWriter) = 
            _viewFolderLayout.ProcessLocations (viewreq, context)
        
            let res = resolve_viewengine (viewreq.ViewLocations) (viewreq.LayoutLocations) 
            let view = res.View
            let viewCtx = ViewContext(context, propbag, model, viewreq)
            view.Process (output, viewCtx)

        member x.Render (viewreq:ViewRequest, context:HttpContextBase, propbag:IDictionary<string,obj>, model) = 
            x.Render (viewreq, context, propbag, model, (context.Response.Output))


