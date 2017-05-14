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
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Reflection
    open System.Web
    open System.Runtime.InteropServices
    open System.ComponentModel.Composition
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Castle.MonoRail.Framework
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Extensibility
    open Microsoft.FSharp.Reflection

    [<Interface>]
    type IAspNetHostingBridge = 
        abstract member AddExcludedAssembly : asm:Assembly -> unit
        abstract member ReferencedAssemblies : IEnumerable<Assembly>
        abstract member GetCompiledType : path:string -> Type


    [<Interface>]
    type IControllerDiscriminator = 
        abstract member IsController : candidate:Type * [<Out>] name:string byref -> bool
    

    [<Export(typeof<IControllerDiscriminator>)>]
    type PocoControllerDiscriminator() = 

        static member CheckForAttributeOrName (t:Type) =
            if t.IsDefined(typeof<ControllerAttribute>, true) then
                let att = 
                    t.GetCustomAttributes(typeof<ControllerAttribute>, false) 
                    |> Seq.cast<ControllerAttribute> 
                    |> Seq.head
                true, att.Name
            elif t.Name.EndsWith("Controller", StringComparison.Ordinal) then
                true, t.Name.Substring(0, t.Name.Length - "controller".Length)
            else
                false, null

        interface IControllerDiscriminator with
            member x.IsController (typ, name) = 
                if not typ.IsAbstract && typ.IsPublic  then
                    let res, tmp = PocoControllerDiscriminator.CheckForAttributeOrName (typ)
                    name <- tmp
                    res
                else
                    false


    [<Export(typeof<IControllerDiscriminator>)>]
    type FSharpControllerDiscriminator() = 
        inherit PocoControllerDiscriminator()

        interface IControllerDiscriminator with
            override x.IsController (t, name) = 
                if FSharpType.IsModule t && t.IsPublic then
                    let res, tmp = PocoControllerDiscriminator.CheckForAttributeOrName (t)
                    name <- tmp
                    res
                else
                    false
    
    [<Export(typeof<IControllerDiscriminator>)>]
    type ViewComponentControllerDiscriminator() = 

        interface IControllerDiscriminator with
            override x.IsController (t, name) = 
                name <- t.Name
                t.IsClass && typeof<IViewComponent>.IsAssignableFrom(t)
                

    [<Export(typeof<IAspNetHostingBridge>)>]
    type BuildManagerAdapter() = 
        let exclusionList = List<Assembly>()

        interface IAspNetHostingBridge with 

            member x.AddExcludedAssembly(asm) = 
                exclusionList.Add asm 
                
            member x.ReferencedAssemblies 
                with get() = 
                    let assemblies = System.Web.Compilation.BuildManager.GetReferencedAssemblies()
                    assemblies.Cast<Assembly>() 
                    |> Seq.filter (fun asm -> not (exclusionList |> Seq.exists (fun excludedAsm -> excludedAsm.FullName = asm.FullName)) )

            member x.GetCompiledType path = 
                System.Web.Compilation.BuildManager.GetCompiledType path


    [<AbstractClass>]
    type BaseTypeBasedControllerProvider() = 
        inherit ControllerProvider()
        let mutable _desc_builder : TypedControllerDescriptorBuilder = null

        [<Import>]
        member this.ControllerDescriptorBuilder
            with get() = _desc_builder and set(v) = _desc_builder <- v

        abstract ResolveControllerType : spec:ControllerCreationSpec -> System.Type
        abstract ActivateController : cType:System.Type * desc:ControllerDescriptor -> obj
        abstract BuildPrototype : inst:obj * desc:ControllerDescriptor -> ControllerPrototype

        default this.BuildPrototype(inst:obj, desc:ControllerDescriptor) = 
            TypedControllerPrototype(desc, inst) :> ControllerPrototype
            
        default this.ActivateController(cType:Type, desc:ControllerDescriptor) = 
            if not cType.IsAbstract then
                try Activator.CreateInstance(cType) 
                with | ex -> raise (MonoRailException((sprintf "Could not activate controller %s" cType.FullName), ex))
            else null

        override this.Create (spec) = 
            let cType = this.ResolveControllerType spec
            if cType <> null then
                let desc = _desc_builder.Build(cType)
                let instance = this.ActivateController(cType, desc)
                Func<ControllerPrototype>(fun _ -> this.BuildPrototype(instance, desc))
            else null
    

    and [<ControllerProviderExport(9000000)>] 
        ReflectionBasedControllerProvider [<ImportingConstructor>] (hosting:IAspNetHostingBridge) =
        class 
            inherit BaseTypeBasedControllerProvider()

            let mutable _discriminators : IControllerDiscriminator seq = null
            let getTypes asm = 
                RefHelpers.typesInAssembly asm (fun t -> t.IsPublic && not (t.FullName.StartsWith ("System.", StringComparison.Ordinal)) && 
                                                                       not (t.FullName.StartsWith ("Microsoft.", StringComparison.Ordinal)))
            let discriminate t (dict:Dictionary<string,Type>) = 
                _discriminators 
                |> Seq.iter (fun (d:IControllerDiscriminator) -> 
                                    // todo: resolve area name and combine with key
                                    let res, name = d.IsController t
                                    if res then dict.[name] <- t )
            let _entries = lazy ( let dict = Dictionary<string,Type>(StringComparer.OrdinalIgnoreCase)
                                  hosting.ReferencedAssemblies 
                                  |> Seq.collect getTypes
                                  |> Seq.iter (fun t -> discriminate t dict )
                                  dict )
            
            [<ImportMany(AllowRecomposition=true)>]
            member x.ControllerDiscriminators with get() = _discriminators and set v = _discriminators <- v

            override this.ResolveControllerType(spec) = 
                // todo: look up by area + controller  

                let resolved = spec.Match( _entries.Force() )
                resolved

                // let _, controllerType = _entries.Force().TryGetValue spec.CombinedName
                // controllerType
        end
                

    and TypedControllerPrototype(desc, instance) = 
        class 
            inherit ControllerPrototype(instance)
            member t.Descriptor = desc

            override x.SupportsAction (name) = 
                desc.HasAction name

        end

