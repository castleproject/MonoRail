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

namespace Castle.MonoRail
    
    open System
    open Castle.Windsor
    open Castle.MicroKernel
    open Castle.MicroKernel.Facilities
    open Castle.MicroKernel.Registration
    open Castle.MonoRail.Hosting.Mvc.Typed

    [<System.Runtime.CompilerServices.ExtensionAttribute>]
    module ExtensionMethods = 
        let private areaBuilder = AreaTypeDescriptorBuilderContributor() :> ITypeDescriptorBuilderContributor

        let internal get_controller_name (impl) = 
            let descriptor = TypedControllerDescriptor(impl)
            areaBuilder.Process(impl, descriptor)

            if String.IsNullOrEmpty(descriptor.Area) 
            then impl.Name
            else sprintf "%s\\%s" descriptor.Area impl.Name

        [<System.Runtime.CompilerServices.ExtensionAttribute>]
        let WhereTypeIsController(fromAssembly:FromAssemblyDescriptor) =
            let configurer = 
                fun (r:ComponentRegistration) -> 
                     r.LifeStyle.Transient.Configuration().Named(get_controller_name(r.Implementation)) |> ignore 

            fromAssembly.
                Where(fun t -> t.Name.EndsWith("Controller")).
                Configure(fun r -> configurer(r))

        [<System.Runtime.CompilerServices.ExtensionAttribute>]
        let WhereTypeIsViewComponent(fromAssembly:FromAssemblyDescriptor) =
            let configurer = 
                fun (r:ComponentRegistration) -> 
                    r.LifeStyle.Transient.Configuration().Named("viewcomponents\\" + r.Implementation.Name) |> ignore

            fromAssembly.
                // todo: constraing by interface IViewComponent
                Where(fun t -> t.Name.EndsWith("Component")).
                Configure(fun r -> configurer(r))

        [<System.Runtime.CompilerServices.ExtensionAttribute>]
        let WhereTypeIsFilter(fromAssembly:FromAssemblyDescriptor) =
            let configurer = fun (r:ComponentRegistration) -> r.LifeStyle.Transient |> ignore

            fromAssembly.
                // todo: constraint by interfaces that we support IExceptionFilter, IActionFilter, etc
                Where(fun t -> t.Name.EndsWith("Filter")).
                Configure(fun r -> configurer(r))


