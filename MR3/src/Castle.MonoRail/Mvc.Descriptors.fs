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

namespace Castle.MonoRail.Hosting.Mvc

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


    and [<AbstractClass;AllowNullLiteral>]
        ControllerDescriptor(name) =
            inherit BaseDescriptor(name)
            let mutable _area : String = null
            let _actions = List<ControllerActionDescriptor>()
            let _name2Action = Dictionary<string, ControllerActionDescriptor>(StringComparer.OrdinalIgnoreCase)

            member this.Area with get() = _area and set(v) = _area <- v
            member this.AddAction (action:ControllerActionDescriptor) = 
                _actions.Add action
                _name2Action.[action.NormalizedName] <- action
            member this.HasAction (name) = _name2Action.ContainsKey name
            member this.Actions : ControllerActionDescriptor seq = upcast _actions



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
        ActionParameterDescriptor(para:ParameterInfo) = 
            member this.Name = para.Name
            member this.ParamType = para.ParameterType
            // this is not adding any value. Consider removing it



