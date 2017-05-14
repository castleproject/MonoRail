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
    open System.Web
    open System.Collections.Generic
    open System.Collections.Specialized
    
    [<Flags>]
    type HttpVerb =
        | Head  = 1
        | Get   = 2
        | Post  = 4
        | Put   = 8
        | Delete = 16
        | Options = 32
        | Patch = 64
        | Merge = 128

    [<AttributeUsage(AttributeTargets.Class|||AttributeTargets.Module, AllowMultiple=false, Inherited=true); AllowNullLiteralAttribute>]
    type AreaAttribute(area:string) = 
        inherit Attribute()
        member x.Area = area

    [<AttributeUsage(AttributeTargets.Class|||AttributeTargets.Module, AllowMultiple=false, Inherited=true); AllowNullLiteralAttribute>]
    type ControllerAttribute(name:string) = 
        inherit Attribute()
        member x.Name = name

    [<AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=true); AllowNullLiteralAttribute>]
    type HttpMethodAttribute(verb:HttpVerb) = 
        inherit Attribute()
        member x.Verb = verb
        
