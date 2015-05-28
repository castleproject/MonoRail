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

namespace Castle.MonoRail.Helpers

    open System
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.IO
    open System.Text
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.ViewEngines
    open Castle.MonoRail.Hosting


    [<Interface;AllowNullLiteral>]
    type IHtmlStringEx = 
        inherit IHtmlString
        abstract member WriteTo : writer:TextWriter -> unit


    type HtmlResult (ac:Action<TextWriter>) = 
        
        new(content:string) = 
            HtmlResult((fun (w:TextWriter) -> w.Write content))

        override x.ToString() = 
            use writer = new StringWriter() 
            ac.Invoke(writer)
            writer.ToString()

        member x.WriteTo(writer:TextWriter) =
            ac.Invoke(writer)

        interface IHtmlStringEx with 
            member x.WriteTo(writer:TextWriter) =
                ac.Invoke(writer)

            member x.ToHtmlString() = x.ToString()


    type HelperContext(context:ViewContext, registry:IServiceRegistry) = 
        member x.ViewContext = context
        member x.ModelMetadataProvider = registry.ModelMetadataProvider
        member x.ServiceRegistry = registry
        member x.HttpContext = context.HttpContext
        member x.Writer = context.Writer


    [<AbstractClass>]
    type BaseHelper(context:HelperContext) = 

        let _appPath : Ref<string> = ref null
        let _contextualPath : Ref<string> = ref null
        static let trim_path (path:string) = 
            if not <| String.IsNullOrEmpty(path) then 
                if path.EndsWith("/", StringComparison.Ordinal)
                then path.Substring(0, path.Length - 1)
                else path
            else path

        [<Import("AppPath", AllowDefault=true, AllowRecomposition=true)>]
        member x.AppPath with get() = !_appPath and set v = _appPath := trim_path(v)

        [<Import("ContextualAppPath", AllowDefault=true)>]
        member x.ContextualAppPath with get() = !_contextualPath and set(v) = _contextualPath := v

        // instead of internal we should be using protected!
        member internal x.Context = context
        member internal x.ViewContext = context.ViewContext
        member internal x.ModelMetadataProvider = context.ModelMetadataProvider
        member internal x.ServiceRegistry = context.ServiceRegistry
        member internal x.Writer = context.Writer
        member internal x.HttpContext = context.HttpContext

        member internal x.HtmlEncode str = 
            context.HttpContext.Server.HtmlEncode str
        
        member internal x.UrlEncode str = 
            context.HttpContext.Server.UrlEncode str
        
        member internal x.AttributesToString(attributes:IDictionary<string,string>) = 
            if attributes = null then
                ""
            else
                let buffer = StringBuilder()
                for pair in attributes do
                    buffer.Append(" ").Append(pair.Key).Append("=\"").Append(pair.Value).Append("\"") |> ignore
                buffer.ToString()

        member internal x.Merge (html:IDictionary<string,string>) (kv:(string * string) seq) : IDictionary<string,string> = 
            let dict = 
                if html <> null then Dictionary<string,string>(html, StringComparer.OrdinalIgnoreCase) else Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
            for k in kv do
                dict.[fst k] <- snd k
            upcast dict

        member internal x.ToId (name:string) =
            // super inneficient!
            name.Replace("[", "_").Replace("]", "").Replace(".", "_")





