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
    open System.Net
    open System.Collections
    open System.Collections.Generic
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Hosting


    [<AbstractClass>]
    type GeneratedUrlsBase() = 
        static let mutable _vpath = HttpContext.Current.Request.ApplicationPath
        static let mutable _router = Router.Instance 

        static member VirtualPath 
            with get() = _vpath and set v = _vpath <- v

        static member CurrentRouter
            with get() = _router and set v = _router <- v


    [<AllowNullLiteral>]
    type UrlParameters(controller:string, action:string, [<ParamArray>] entries:KeyValuePair<string,string>[]) as self =
         inherit Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
         
         do
            base.Add("controller",  controller)
            base.Add("action",  action)

            entries 
            |> Seq.iter (fun p -> if p.Value <> null then self.Add(p.Key, p.Value) )



    and [<AllowNullLiteral>]
        RouteBasedTargetUrl(vpath:string, route:Route, parameters:IDictionary<string,string>) = 
        inherit TargetUrl()

        let merge (newParams:IDictionary<string,string>) = 
            if newParams <> null then
                let dict = Dictionary<string,string>(parameters, StringComparer.OrdinalIgnoreCase)
                for pair in newParams do
                    dict.[pair.Key] <- pair.Value
                dict :> IDictionary<string,string>
            else
                parameters 

        override x.Generate parameters = 
            route.Generate (vpath, (merge parameters))

        override x.ToString() =
            route.Generate (vpath, parameters)
