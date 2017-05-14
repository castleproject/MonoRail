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

namespace Castle.MonoRail.Extension.Windsor

    open System
    open System.Collections.Generic
    open System.Web
    open System.Threading
    open System.ComponentModel.Composition
    open Castle.MicroKernel.Facilities
    open Castle.Windsor
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Extensibility
    open Castle.MonoRail.Hosting.Mvc.Typed
    open Castle.Extensibility

    module ContainerAccessorUtil = 
        begin
            let internal InternalObtainContainer (accessor:IContainerAccessor) : IWindsorContainer =     
                if obj.ReferenceEquals(accessor, null) then
                    raise (MonoRailException("You must extend the HttpApplication in your web project " +
                                             "and implement the IContainerAccessor to properly expose your container instance"))
                else 
                    let container = accessor.Container

                    if obj.ReferenceEquals(container, null) then
                        raise(MonoRailException("The container seems to be unavailable in " +
                                                "your HttpApplication subclass"))
                    else container
            
            let ObtainContainer() : IWindsorContainer = 
                let accessor = HttpContext.Current.ApplicationInstance |> box :?> IContainerAccessor
                InternalObtainContainer accessor
        end

    module WindsorUtil = 
        begin
            let internal BuildArguments (context:HttpContextBase) : Dictionary<string, obj> = 
                let args = Dictionary<string, obj>()
                args.Add("context", context)
                args.Add("principal", lazy (context.User))
                args
        end

    [<ControllerProviderExport(900000)>] 
    type WindsorControllerProvider() =
        inherit ControllerProvider()

        let _desc_builder : Ref<TypedControllerDescriptorBuilder> = ref null
        let _container : Ref<IWindsorContainer> = ref null

        let normalize_name (cname:string) =
            if cname.EndsWith("Component", StringComparison.OrdinalIgnoreCase) 
            then cname
            else cname + "Controller" 

        let _containerInstance = 
            lazy ( if !_container <> null 
                   then !_container
                   else ContainerAccessorUtil.ObtainContainer() )

        let _entries = 
            lazy ( let set = Dictionary<string,Type>()
                   let naming = _containerInstance.Force().Kernel.GetSubSystem(Castle.MicroKernel.SubSystemConstants.NamingKey) :?> Castle.MicroKernel.SubSystems.Naming.DefaultNamingSubSystem
                   naming.GetAllHandlers() 
                   |> Array.iter (fun h -> (set.Add (h.ComponentModel.Name, h.ComponentModel.Implementation) ))
                   set :> IDictionary<_,_> )

        [<Import>]
        member this.ControllerDescriptorBuilder with get() = !_desc_builder and set(v) = _desc_builder := v
 
        [<BundleImport("WindsorContainer", AllowDefault = true, AllowRecomposition = true)>]
        member this.Container with get() = !_container and set(v) = _container := v
             
        override this.Create(spec) = 
            // for now, we support only named spec
            // the following will throw, which is a friendly reminder to support the other spec case
            let container = _containerInstance.Force()

            if spec :? NamedControllerCreationSpec then
                let spec = spec |> box :?> NamedControllerCreationSpec
                let key = (normalize_name spec.CombinedName).ToLowerInvariant()
                if container.Kernel.HasComponent(key) then
                    let instance = container.Resolve<obj>(key) //, WindsorUtil.BuildArguments(context))
                    let cType = instance.GetType()
                    let desc = (!_desc_builder).Build(cType)
                    Func<_>(fun _ -> upcast TypedControllerPrototype(desc, instance))
                else null
            else 
                let cType = spec.Match (_entries.Force())
                if cType <> null then
                    let instance = container.Resolve(cType)
                    let desc = (!_desc_builder).Build(cType)
                    Func<_>(fun _ -> upcast TypedControllerPrototype(desc, instance))
                else null
                
            


    [<Export(typeof<IFilterActivator>)>]
    [<ExportMetadata("Order", 10000)>]
    type WindsorFilterActivator() =
        let _container : Ref<IWindsorContainer> = ref null
        let _containerInstance = 
            lazy ( if !_container <> null 
                   then !_container
                   else ContainerAccessorUtil.ObtainContainer() ) 

        let activate (filterType:Type) : 'a  = 
            let container = _containerInstance.Force()

            if container.Kernel.HasComponent(filterType) 
            then container.Resolve(filterType) :?> 'a
            else null
           
        [<BundleImport("WindsorContainer", AllowDefault = true, AllowRecomposition=true)>]
        member this.Container with get() = !_container and set(v) = _container := v
        
        interface IFilterActivator with
            member x.Activate(filter) =
                activate filter
    