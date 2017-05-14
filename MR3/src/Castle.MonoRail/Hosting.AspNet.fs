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

namespace Castle.MonoRail.Hosting

    open System
    open System.Collections.Generic
    open System.Web
    open System.Web.SessionState
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.MonoRail.Hosting.Container
    open Castle.MonoRail
    open Castle.Extensibility
    open Castle.Extensibility.Hosting


    [<AllowNullLiteral>]
    type MonoRailHandler() = 

        let try_process (handler:IComposableHandler) (metadata:IDictionary<string,obj>) request = 
            try
                match handler.TryProcessRequest( request ) with
                | true -> Some(handler)
                | _ -> None
            with 
            | exc ->
                let bundleName = 
                    let _, v = metadata.TryGetValue("_BundleSource")
                    if v = null then "default"
                    else v.ToString()
                raise(MonoRailException((sprintf "Error processing IComposableHandler.TryProcessRequest. Bundle (if within bundle) %s" bundleName), exc))
        
        interface IRequiresSessionState
         
        interface IHttpHandler with
            member this.ProcessRequest(context:HttpContext) : unit =
                let handlers = MRComposition.GetAllWithMetadata<IComposableHandler, IDictionary<string, obj>>()
                let ctxWrapped = HttpContextWrapper(context)
                
                match handlers |> Seq.tryPick (fun handler -> try_process handler.Value handler.Metadata ctxWrapped) with
                | Some _ -> ()
                | None -> 
                    raise(new MonoRailException("Could not find a IComposableHandler able to process this request") )

            member this.IsReusable = 
                true

        
    // For use in conjuction with Castle.Extensibility / HostingContainer
    // This exposes a custom IDeploymentInfo to bundles (contexts) 
    type MonoRailBundleBehavior() = 
        
        interface Castle.Extensibility.Hosting.IBehavior with
            
            member x.GetBehaviorExports(imports, exports, manifest) = 
                if exports |> Seq.exists (fun e -> e.ContractName.StartsWith("Castle.MonoRail", StringComparison.OrdinalIgnoreCase)) then
                    let deployInfo = BundleDeploymentInfo(manifest)
                    let contract = AttributedModelServices.GetContractName(typeof<IDeploymentInfo>)
                    let typeId   = AttributedModelServices.GetTypeIdentity(typeof<IDeploymentInfo>)
                    let metadata = Dictionary<string,obj>()
                    metadata.[CompositionConstants.ExportTypeIdentityMetadataName] <- typeId

                    [Export(contract, metadata, (fun _ -> deployInfo |> box))] |> List.toSeq
                else Seq.empty

    // Allows the path of the bundle to be considered for Content (static/views)
    and BundleDeploymentInfo(manifest:Manifest) = 
        
        let trailing_start (path:string) = 
            if path.StartsWith("/", StringComparison.OrdinalIgnoreCase) 
            then path
            else "/" + path

        let vpath = lazy(   let vpathRoot = HttpRuntime.AppDomainAppVirtualPath
                            let fsPath = HttpRuntime.AppDomainAppPath
                            let diff = manifest.DeploymentPath.Substring(fsPath.Length)
                            trailing_start(diff.Replace('\\', '/'))
                        )

        interface IDeploymentInfo with 
            member x.FSPathOffset = manifest.DeploymentPath
            member x.VirtualPath = vpath.Force() 
                

