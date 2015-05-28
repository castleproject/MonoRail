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

namespace Castle.MonoRail.Hosting.Container

    open System
    open System.IO
    open System.Linq
    open System.Reflection
    open System.Threading
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Hosting
    open Castle.Extensibility.Hosting

      
    // Since MEF's DirectoryCatalog does not guard against Assembly.GetTypes failing
    // I had to write my own
    type DirectoryCatalogGuarded(folder) = 
        inherit ComposablePartCatalog()

        let _catalogs = List<TypeCatalog>()
        let mutable _parts : ComposablePartDefinition seq = null

        let load_assembly_guarded (file:string) : Assembly = 
            try
                let name = AssemblyName.GetAssemblyName(file);
                Assembly.Load name
            with | ex -> null

        do 
            let files = Directory.GetFiles(folder, "*.dll")
            for file in files do
                let asm = load_assembly_guarded file
                if asm <> null then
                    let types = RefHelpers.guard_load_types(asm) |> Seq.filter (fun t -> t <> null) 
                    if not (Seq.isEmpty types) then
                        _catalogs.Add (new TypeCatalog(types))
            _parts <- _catalogs |> Seq.collect (fun c -> c.Parts) |> Linq.Queryable.AsQueryable

        override x.Parts = _parts.AsQueryable()
        override x.GetExports(definition) = 
            _catalogs |> Seq.collect (fun c -> c.GetExports(definition))


    /// Creates scopes based on the Scope Metadata. 
    /// Root/App is made of parts with Scope = App or absence of the marker
    /// Everything else goes to the Request scope
    type MetadataBasedScopingPolicy private (catalog, children, pubsurface) =
        inherit CompositionScopeDefinition(catalog, children, pubsurface) 

        new (ubercatalog:ComposablePartCatalog) = 
            let appDefault = 
                ubercatalog.Filter(fun cpd -> 
                    (not (cpd.ContainsPartMetadataWithKey("Scope")) || cpd.ContainsPartMetadata("Scope", ComponentScope.Application)))
            (*
            // App      App-Override
            // Request  Request-Override
            let appOverride = 
                appDefault.Complement.Filter(fun cpd -> cpd.ContainsPartMetadata("Scope", ComponentScope.ApplicationOverride))
            let requestDefault = 
                appOverride.Complement.Filter(fun cpd -> cpd.ContainsPartMetadata("Scope", ComponentScope.Request))
            let requestOverride = 
                requestDefault.Complement.Filter(fun cpd -> cpd.ContainsPartMetadata("Scope", ComponentScope.Request))
            *)
            
            let childcat = appDefault.Complement
            let childexports = childcat.Parts |> Seq.collect ( fun cpd -> cpd.ExportDefinitions )
            let childdef = new CompositionScopeDefinition(childcat, [], childexports)
            let psurface = appDefault.Parts |> Seq.collect (fun cpd -> cpd.ExportDefinitions)

            new MetadataBasedScopingPolicy(appDefault, [childdef], psurface)


    type MRCatalog(parts:ComposablePartDefinition seq) = 
        inherit ComposablePartCatalog()  

        let partsAsQueryable = lazy Queryable.AsQueryable( parts )

        override x.Parts = partsAsQueryable.Force()


    type MRModuleContext() = 
        inherit Castle.Extensibility.ModuleContext()

        override x.GetService() = null
        override x.HasService() = false


    [<AllowNullLiteral>]
    type IContainer = 
        interface 
            abstract member Get<'T> : unit -> 'T
            // abstract member GetAll<'T> : unit -> 'T seq
            abstract member GetAll<'T, 'TM> : unit -> Lazy<'T, 'TM> seq
            // abstract member SatisfyImports : target:obj -> unit
        end

    type HostingContainerAdapter(hostingContainer:HostingContainer) = 
        interface IContainer with 
            member x.Get() = hostingContainer.GetExportedValue()
            member x.GetAll() = hostingContainer.GetExports<'T, 'TM>()
            

    [<AllowNullLiteral>]
    type Container (path:string) = 
        class
            static let build_metadata (typ:Type) : IDictionary<string,obj> = 
                let typeId   = AttributedModelServices.GetTypeIdentity(typ)
                let metadata = Dictionary<string,obj>()
                metadata.[CompositionConstants.ExportTypeIdentityMetadataName] <- typeId
                upcast metadata
            
            let mutable mef_container : CompositionContainer = null

            static let build_catalog (path) = 
                // todo: allow one to customize the catalog before using it?
                let uber_catalog : ComposablePartCatalog = upcast new DirectoryCatalogGuarded(path)
                let catalog : ComposablePartCatalog = upcast new MetadataBasedScopingPolicy(uber_catalog)

                let ctx = MRModuleContext()
                let exports = catalog.Parts |> Seq.collect (fun cpd -> cpd.ExportDefinitions)
                let imports : ImportDefinition seq = Seq.empty
                let def = MefBundlePartDefinition(catalog, exports, imports, Manifest("default", Version(), null, ""), Dictionary(), ctx, Seq.empty)
                new MRCatalog([ def ])

            do
                let catalog = build_catalog(path)                
                let opts = CompositionOptions.IsThreadSafe ||| CompositionOptions.DisableSilentRejection ||| CompositionOptions.ExportCompositionService
                mef_container <- new CompositionContainer(catalog, opts)
            
            new () = 
                let binFolder = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "bin")
                Container(binFolder)

            static member BuildMrCatalog(path) = build_catalog(path)

            interface IContainer with 
                member x.Get() = 
                   mef_container.GetExportedValue()

                member x.GetAll<'T, 'TM>() = 
                   mef_container.GetExports<'T, 'TM>()

        end


    module MRComposition = 
        begin
            let _defcomposer = lazy ( Container() :> IContainer )
            let mutable _customContainer : IContainer = null
            let _customSet = ref false

            let public SetCustomContainer (container) = 
                if not !_customSet then
                    _customContainer <- container
                    _customSet := true
                else raise(InvalidOperationException("A custom container has already beem set, and cannot be replaced at this time"))

            let GetAllWithMetadata<'T, 'TM> () = 
                if _customSet.Value 
                then _customContainer.GetAll<'T, 'TM>() 
                else _defcomposer.Force().GetAll<'T, 'TM>()
        end


    /// Used by Castle.Extensibility
    type MefComposerBuilder(parameters:string seq) = 
        inherit Castle.Extensibility.Hosting.MefComposerBuilder(parameters)

        new () = MefComposerBuilder([||])

        override x.BuildCatalog(types, manifest) = 
            let dirCatalog : ComposablePartCatalog = upcast new DirectoryCatalogGuarded(manifest.DeploymentPath)
            let catalog = new MetadataBasedScopingPolicy(dirCatalog)
            upcast catalog
            


