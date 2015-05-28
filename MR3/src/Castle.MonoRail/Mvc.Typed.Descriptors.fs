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


    type [<AllowNullLiteral>]
        TypedControllerDescriptor(controller:Type) =
            inherit ControllerDescriptor(Helpers.to_controller_name controller)

            interface ICustomAttributeProvider with                 
                member x.IsDefined(attType, ``inherit``) = 
                    controller.IsDefined(attType, ``inherit``)
                member x.GetCustomAttributes(``inherit``) = 
                    controller.GetCustomAttributes(``inherit``)
                member x.GetCustomAttributes(attType, ``inherit``) = 
                    controller.GetCustomAttributes(attType, ``inherit``)


    and [<AllowNullLiteral>]
        MethodInfoActionDescriptor(methodInfo:MethodInfo, controllerDesc) = 
            inherit ControllerActionDescriptor(methodInfo.Name, controllerDesc)

            let mutable _lambda = Lazy<Func<obj,obj[],obj>>()
            let mutable _canonicalName : string = null
            let _allowedVerbs = HashSet<string>()

            do 
                _lambda <- lazy 
                    let instance = Expression.Parameter(typeof<obj>, "instance") 
                    let args = Expression.Parameter(typeof<obj[]>, "args")

                    let parameters = 
                        // TODO: refactor to not use seq
                        seq {   let ps = methodInfo.GetParameters()
                                for index = 0 to ps.Length - 1 do
                                    let p = ps.[index]
                                    let pType = p.ParameterType
                                    let indexes = [|Expression.Constant(index)|]:Expression[]
                                    let paramAccess = Expression.ArrayAccess(args, indexes)
                                    yield Expression.Convert(paramAccess, pType) :> Expression  } 
                        
                    let call = 
                        if methodInfo.IsStatic 
                        then Expression.Call(methodInfo, parameters)
                        else Expression.Call(Expression.TypeAs(instance, methodInfo.DeclaringType), methodInfo, parameters)

                    let lambda_args = [|instance; args|]
                    let block_items = [|call; Expression.Constant(null, typeof<obj>)|]:Expression[]

                    if (methodInfo.ReturnType = typeof<System.Void>) then
                        let block = Expression.Block(block_items) :> Expression
                        Expression.Lambda<Func<obj,obj[],obj>>(block, lambda_args).Compile()
                    else 
                        Expression.Lambda<Func<obj,obj[],obj>>(call, lambda_args).Compile()
                    
                let httpAtt = 
                    let items = 
                        methodInfo.GetCustomAttributes(typeof<HttpMethodAttribute>, false) 
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
                
            override this.ReturnType = methodInfo.ReturnType    

            override this.SatisfyRequest(context:HttpContextBase) = 
                if _allowedVerbs.Count = 0 
                then true
                else
                    let requestVerb = Helpers.get_effective_http_method context.Request
                    _allowedVerbs |> Seq.exists (fun v -> String.CompareOrdinal(v, requestVerb) = 0)

            override this.HasAnnotation<'t>() = 
                // todo: slow, should we cache it?
                methodInfo.IsDefined(typeof<'t>, true)

            override this.Execute(instance:obj, args:obj[]) = 
                _lambda.Force().Invoke(instance, args)

            override this.IsMatch(actionName:string) =
                if String.IsNullOrEmpty(_canonicalName)
                then String.Compare(this.Name, actionName, StringComparison.OrdinalIgnoreCase) = 0
                else String.Compare(_canonicalName, actionName, StringComparison.OrdinalIgnoreCase) = 0

            interface ICustomAttributeProvider with                 
                member x.IsDefined(attType, ``inherit``) = 
                    methodInfo.IsDefined(attType, ``inherit``)
                member x.GetCustomAttributes(``inherit``) = 
                    methodInfo.GetCustomAttributes(``inherit``)
                member x.GetCustomAttributes(attType, ``inherit``) = 
                    methodInfo.GetCustomAttributes(attType, ``inherit``)
                    


