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

namespace Castle.MonoRail.Hosting.Mvc.Typed

    open System
    open System.Reflection
    open System.Collections.Generic
    open System.Collections.Concurrent
    open System.Linq
    open System.Linq.Expressions
    open System.ComponentModel.Composition
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Extensibility
    open System.Text.RegularExpressions


    [<Interface;AllowNullLiteral>]
    type ITypeDescriptorBuilderContributor = 
        abstract member Process : target:Type * desc:ControllerDescriptor -> unit

    [<Interface;AllowNullLiteral>]
    type IActionDescriptorBuilderContributor = 
        abstract member Process : action:ControllerActionDescriptor * desc:ControllerDescriptor -> unit

    [<Interface;AllowNullLiteral>]
    type IParameterDescriptorBuilderContributor = 
        abstract member Process : paramDesc:ActionParameterDescriptor * actionDesc:ControllerActionDescriptor * desc:ControllerDescriptor -> unit



    [<Export;AllowNullLiteral>]
    type TypedControllerDescriptorBuilder() = 
        let mutable _typeContributors = Enumerable.Empty<Lazy<ITypeDescriptorBuilderContributor, IComponentOrder>>()
        let mutable _actionContributors = Enumerable.Empty<Lazy<IActionDescriptorBuilderContributor, IComponentOrder>>()
        let mutable _paramContributors = Enumerable.Empty<Lazy<IParameterDescriptorBuilderContributor, IComponentOrder>>()

        let _locker = obj()
        let _builtDescriptors = ConcurrentDictionary<Type, ControllerDescriptor>()

        let build_descriptor (controller:Type) = 
            let desc = TypedControllerDescriptor(controller)

            _typeContributors 
            |> Seq.iter (fun contrib -> contrib.Force().Process (controller, desc))

            let build_action_desc (action) = 
                _actionContributors
                |> Seq.iter (fun contrib -> contrib.Force().Process(action, desc))
                action.Parameters
                |> Seq.iter (fun param -> _paramContributors 
                                          |> Seq.iter (fun contrib ->  contrib.Force().Process(param, action, desc)))

            desc.Actions |> Seq.iter build_action_desc
            desc

        [<ImportMany(AllowRecomposition=true)>]
        member this.TypeContributors
            with get() = _typeContributors and set(v) = _typeContributors <- Helper.order_lazy_set v

        [<ImportMany(AllowRecomposition=true)>]
        member this.ActionContributors
            with get() = _actionContributors and set(v) = _actionContributors <- Helper.order_lazy_set v

        [<ImportMany(AllowRecomposition=true)>]
        member this.ParamContributors
            with get() = _paramContributors and set(v) = _paramContributors <- Helper.order_lazy_set v

        member this.Build(controller:Type) = 
            Assertions.ArgNotNull controller "controller"

            let res, desc = _builtDescriptors.TryGetValue(controller)

            if res then desc
            else
                lock(_locker) 
                    (fun _ -> let res, desc = _builtDescriptors.TryGetValue(controller)
                              if res then desc
                              else 
                                  let desc = build_descriptor controller
                                  _builtDescriptors.[controller] <- desc
                                  upcast desc )


    [<Export(typeof<ITypeDescriptorBuilderContributor>)>]
    [<ExportMetadata("Order", 10000);AllowNullLiteral>]
    type PocoTypeDescriptorBuilderContributor() = 

        interface ITypeDescriptorBuilderContributor with

            member this.Process(target:Type, desc:ControllerDescriptor) = 

                let potentialActions = target.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)

                for a in potentialActions do
                    if not a.IsSpecialName && a.DeclaringType <> typeof<obj> then 
                        let method_desc = MethodInfoActionDescriptor(a,desc)
                        desc.AddAction method_desc

                        for p in a.GetParameters() do 
                            method_desc.Parameters.Add (ActionParameterDescriptor(p))


    [<Export(typeof<ITypeDescriptorBuilderContributor>)>]
    [<ExportMetadata("Order", 20000);AllowNullLiteral>]
    type FsharpDescriptorBuilderContributor() = 

        interface ITypeDescriptorBuilderContributor with

            member this.Process(target:Type, desc:ControllerDescriptor) = 

                if (Microsoft.FSharp.Reflection.FSharpType.IsModule target) then
                    let potentialActions = target.GetMethods(BindingFlags.Public ||| BindingFlags.Static)

                    for a in potentialActions do
                        if a.DeclaringType <> typeof<obj> then 
                            let method_desc = MethodInfoActionDescriptor(a, desc)
                            desc.AddAction method_desc

                            a.GetParameters() 
                            |> Seq.iter (fun p -> method_desc.Parameters.Add (ActionParameterDescriptor(p)))


    (*
    [<Export(typeof<IActionDescriptorBuilderContributor>)>]
    [<ExportMetadata("Order", 10000);AllowNullLiteral>]
    type ActionDescriptorBuilderContributor() = 
        interface IActionDescriptorBuilderContributor with
            member this.Process(desc:ControllerActionDescriptor, parent:ControllerDescriptor) = 
                ()


    [<Export(typeof<IParameterDescriptorBuilderContributor>)>]
    [<ExportMetadata("Order", 10000);AllowNullLiteral>]
    type ParameterDescriptorBuilderContributor() = 

        interface IParameterDescriptorBuilderContributor with
            member this.Process(paramDesc:ActionParameterDescriptor, actionDesc:ControllerActionDescriptor, parent:ControllerDescriptor) = 
                ()
    *)
    
    [<Export(typeof<ITypeDescriptorBuilderContributor>)>]
    [<ExportMetadata("Order", 30000);AllowNullLiteral>]
    type AreaTypeDescriptorBuilderContributor() = 
        
        let get_root (target:Type) =
            // TODO: what to do if not found?
            // TODO: cache since guard_load_public_types is expensive
            let httpapp = 
                RefHelpers.guard_load_public_types(target.Assembly)
                |> Seq.tryPick (fun t -> if typeof<HttpApplication>.IsAssignableFrom(t) then Some(t) else None )
               
            match httpapp with 
            | Some app -> app.Namespace
            | None -> failwithf "Could not find subclass of HttpApplication on %O" target.Assembly

        let discover_area (target:Type) (rootns:string) =
            if target.IsDefined(typeof<AreaAttribute>, true) then
                let att : AreaAttribute = RefHelpers.read_att target
                att.Area
            elif typeof<IViewComponent>.IsAssignableFrom(target) then
                "viewcomponents"
            else
                // potentially cpu intensive. is there a simpler way?
                let regex = Regex(rootns + ".(?<area>.*?).Controllers." + target.Name)
                let matches = regex.Matches(target.FullName)
                if matches.Count = 0 
                then null
                else
                    let mtch = matches.Cast<Match>() |> Seq.head 
                    let areans = mtch.Groups.["area"].Value.ToLowerInvariant()
                    if areans.Length > 0 
                    then areans.Replace(".", "\\")
                    else null
            
        interface ITypeDescriptorBuilderContributor with
            member this.Process(target:Type, desc:ControllerDescriptor) = 
                let rootns = get_root target
                desc.Area <- discover_area target rootns

