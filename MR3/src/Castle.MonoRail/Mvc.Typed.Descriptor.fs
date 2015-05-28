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
    open Castle.MonoRail.Hosting.Mvc.Extensibility
    open System.Text.RegularExpressions



    [<AbstractClass;AllowNullLiteral>] 
    type BaseDescriptor(name) = 
        let _meta = lazy Dictionary<string,obj>(StringComparer.Ordinal)

        member x.Name = name
        member x.Metadata = _meta.Force()


    and [<AllowNullLiteral>]
        ControllerDescriptor(controller:Type) =
            inherit BaseDescriptor(Helpers.to_controller_name controller)
            let mutable _area : String = null
            let _actions = List<ControllerActionDescriptor>() 

            member this.Actions = _actions

            member this.Area
                with get() = _area and set(v) = _area <- v

            interface ICustomAttributeProvider with                 
                member x.IsDefined(attType, ``inherit``) = 
                    controller.IsDefined(attType, ``inherit``)
                member x.GetCustomAttributes(``inherit``) = 
                    controller.GetCustomAttributes(``inherit``)
                member x.GetCustomAttributes(attType, ``inherit``) = 
                    controller.GetCustomAttributes(attType, ``inherit``)


    and [<AbstractClass;AllowNullLiteral>] 
        ControllerActionDescriptor(name:string, controllerDesc:ControllerDescriptor) = 
            inherit BaseDescriptor(name)
            let _params = lazy List<ActionParameterDescriptor>()
            let _paramsbyName = lazy (
                    let dict = Dictionary<string,ActionParameterDescriptor>(StringComparer.Ordinal)
                    let temp = _params.Force()
                    for p in temp do
                        dict.[p.Name] <- p
                    dict
                )

            member this.ControllerDescriptor = controllerDesc
            member this.Parameters = _params.Force()
            member this.ParametersByName = _paramsbyName.Force()

            abstract member SatisfyRequest : context:HttpContextBase -> bool
            abstract member Execute : instance:obj * args:obj[] -> obj
            abstract member IsMatch : actionName:string -> bool
            abstract NormalizedName : string with get

            default x.NormalizedName with get() = name

            default x.IsMatch(actionName:string) =
                String.Compare(name, actionName, StringComparison.OrdinalIgnoreCase) = 0

    and [<AllowNullLiteral>]
        MethodInfoActionDescriptor(methodInfo:MethodInfo, controllerDesc) = 
            inherit ControllerActionDescriptor(methodInfo.Name, controllerDesc)
            let mutable _lambda = Lazy<Func<obj,obj[],obj>>()
            let mutable _canonicalName : string = null
            let _allowedVerbs = HashSet<string>()

            do 
                _lambda <- lazy ( 
                        
                        let instance = Expression.Parameter(typeof<obj>, "instance") 
                        let args = Expression.Parameter(typeof<obj[]>, "args")

                        let parameters = 
                            // TODO: refactor to not use seq
                            seq { 
                                    let ps = methodInfo.GetParameters()
                                    for index = 0 to ps.Length - 1 do
                                        let p = ps.[index]
                                        let pType = p.ParameterType
                                        let indexes = [|Expression.Constant(index)|]:Expression[]
                                        let paramAccess = Expression.ArrayAccess(args, indexes)
                                        yield Expression.Convert(paramAccess, pType) :> Expression
                                } 
                        
                        let call = 
                            if methodInfo.IsStatic then
                                Expression.Call(methodInfo, parameters)
                            else
                                Expression.Call(
                                    Expression.TypeAs(instance, methodInfo.DeclaringType), methodInfo, parameters)

                        let lambda_args = [|instance; args|]
                        let block_items = [|call; Expression.Constant(null, typeof<obj>)|]:Expression[]

                        if (methodInfo.ReturnType = typeof<System.Void>) then
                            let block = Expression.Block(block_items) :> Expression
                            Expression.Lambda<Func<obj,obj[],obj>>(block, lambda_args).Compile()
                        else
                            Expression.Lambda<Func<obj,obj[],obj>>(call, lambda_args).Compile()
                    )
                
                let httpAtt = 
                    let items = methodInfo.GetCustomAttributes(typeof<HttpMethodAttribute>, false) 
                                |> Seq.cast<HttpMethodAttribute>
                    if Seq.isEmpty items 
                    then null
                    else Seq.head items

                if httpAtt <> null then
                    let add_to_allow_list_if_defined (verb) = 
                        if httpAtt.Verb.HasFlag verb then
                            _allowedVerbs.Add ( verb.ToString().ToUpperInvariant() ) |> ignore
                    Enumerable.Cast<HttpVerb>( Enum.GetValues(typeof<HttpVerb>) ) 
                    |> Seq.iter add_to_allow_list_if_defined
                    
                let declared_verb = 
                    (Enum.GetNames(typeof<HttpVerb>) 
                        |> Seq.filter (fun v -> methodInfo.Name.StartsWith(v + "_", StringComparison.OrdinalIgnoreCase))).FirstOrDefault() 

                if not (String.IsNullOrEmpty(declared_verb)) then
                    _canonicalName <- methodInfo.Name.Replace(declared_verb + "_", "")
                    _allowedVerbs.Add(declared_verb.ToUpperInvariant()) |> ignore

            override this.NormalizedName 
                with get() = if String.IsNullOrEmpty(_canonicalName) then base.Name else _canonicalName
                    
            override this.SatisfyRequest(context:HttpContextBase) = 
                if _allowedVerbs.Count = 0 then
                    true
                else
                    let requestVerb = Helpers.get_effective_http_method context.Request
                            
                    _allowedVerbs |> Seq.exists (fun v -> String.CompareOrdinal(v, requestVerb) = 0)

            override this.Execute(instance:obj, args:obj[]) = 
                _lambda.Force().Invoke(instance, args)

            override this.IsMatch(actionName:string) =
                if String.IsNullOrEmpty(_canonicalName) then
                    String.Compare(this.Name, actionName, StringComparison.OrdinalIgnoreCase) = 0
                else
                    String.Compare(_canonicalName, actionName, StringComparison.OrdinalIgnoreCase) = 0

            interface ICustomAttributeProvider with                 
                member x.IsDefined(attType, ``inherit``) = 
                    methodInfo.IsDefined(attType, ``inherit``)
                member x.GetCustomAttributes(``inherit``) = 
                    methodInfo.GetCustomAttributes(``inherit``)
                member x.GetCustomAttributes(attType, ``inherit``) = 
                    methodInfo.GetCustomAttributes(attType, ``inherit``)
                    


    and [<AllowNullLiteral>]
        ActionParameterDescriptor(para:ParameterInfo) = 
            member this.Name = para.Name
            member this.ParamType = para.ParameterType
            // this is not adding any value. Consider removing it



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
    type ControllerDescriptorBuilder() = 
        let mutable _typeContributors = Enumerable.Empty<Lazy<ITypeDescriptorBuilderContributor, IComponentOrder>>()
        let mutable _actionContributors = Enumerable.Empty<Lazy<IActionDescriptorBuilderContributor, IComponentOrder>>()
        let mutable _paramContributors = Enumerable.Empty<Lazy<IParameterDescriptorBuilderContributor, IComponentOrder>>()

        let _locker = obj()
        let _builtDescriptors = ConcurrentDictionary<Type, ControllerDescriptor>()

        let build_descriptor (controller:Type) = 
            let desc = ControllerDescriptor(controller)

            _typeContributors 
            |> Seq.iter (fun contrib -> contrib.Force().Process (controller, desc))

            desc.Actions
            |> Seq.iter (fun action -> _actionContributors 
                                       |> Seq.iter (fun contrib -> contrib.Force().Process(action, desc))
                                       
                                       action.Parameters 
                                       |> Seq.iter (fun param -> _paramContributors 
                                                                 |> Seq.iter (fun contrib ->  contrib.Force().Process(param, action, desc)))
                        )
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
                                  desc
                    )


    [<Export(typeof<ITypeDescriptorBuilderContributor>)>]
    [<ExportMetadata("Order", 10000);AllowNullLiteral>]
    type PocoTypeDescriptorBuilderContributor() = 

        interface ITypeDescriptorBuilderContributor with

            member this.Process(target:Type, desc:ControllerDescriptor) = 

                let potentialActions = target.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)

                for a in potentialActions do
                    if not a.IsSpecialName && a.DeclaringType <> typeof<obj> then 
                        let method_desc = MethodInfoActionDescriptor(a,desc)
                        desc.Actions.Add method_desc

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
                            desc.Actions.Add method_desc

                            a.GetParameters() 
                            |> Seq.iter (fun p -> method_desc.Parameters.Add (ActionParameterDescriptor(p)))



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
                let att : AreaAttribute = RefHelpers.read_att(target)
                att.Area
            elif typeof<IViewComponent>.IsAssignableFrom(target) then
                "viewcomponents"
            else
                // potentially cpu intensive. is there a simpler way?
                let regex = Regex(rootns + ".(?<area>.*?).Controllers." + target.Name)
                let matches = regex.Matches(target.FullName)
                if matches.Count = 0 then 
                    null
                else
                    let mtch = matches.Cast<Match>() |> Seq.head 
                    let areans = mtch.Groups.["area"].Value.ToLowerInvariant()

                    if areans.Length > 0 then
                        areans.Replace(".", "\\")
                    else
                        null
            
        interface ITypeDescriptorBuilderContributor with
            member this.Process(target:Type, desc:ControllerDescriptor) = 
                let rootns = get_root target
                desc.Area <- discover_area target rootns

