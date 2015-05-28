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

module Castle.MonoRail.Generator.Api

    open System
    open System.Collections.Generic
    open System.IO
    open System.Text
    open System.Linq
    open System.Reflection
    open System.Web
    open System.CodeDom
    open System.CodeDom.Compiler
    open Castle.MonoRail
    open Castle.MonoRail.Routing
    open Microsoft.CSharp
    open Castle.MonoRail.Hosting.Mvc
    open Castle.MonoRail.Hosting.Mvc.Typed
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.MonoRail.Hosting
    open Castle.MonoRail.Hosting.Container

    type ActionComparer() =
        interface IEqualityComparer<ControllerActionDescriptor> with
            member x.Equals(a:ControllerActionDescriptor, b:ControllerActionDescriptor) =
                a.NormalizedName = b.NormalizedName

            member x.GetHashCode(a:ControllerActionDescriptor) =
                a.NormalizedName.GetHashCode()
        

    type ActionDef(controller:Type, action:MethodInfoActionDescriptor, route:Route, index:int) = 
        let get_method (verb:string) : CodeMemberMethod =
            let mmethod = CodeMemberMethod()
            mmethod.Name <- verb
            mmethod.Attributes <- MemberAttributes.Static ||| MemberAttributes.Public
            mmethod.ReturnType <- CodeTypeReference("TargetUrl")

            mmethod

        let append (mmethod: CodeMemberMethod) (includeparams: bool) (fieldAccessor: CodeStatementCollection) (urlType: CodeTypeDeclaration) = 

            mmethod.Statements.AddRange fieldAccessor

            if includeparams then
                for p in action.Parameters do
                    let pDecl = CodeParameterDeclarationExpression(p.ParamType, p.Name)
                    //pDecl.CustomAttributes.Add(CodeAttributeDeclaration("Optional")) |> ignore
                    mmethod.Parameters.Add pDecl |> ignore

            // targetTypeDecl.Members.Add field |> ignore
            urlType.Members.Add mmethod |> ignore

        let get_targeturl_stmt (includeAllParams:bool) =
            let getControllerName (name:string) = 
                if name.EndsWith "Controller" then 
                    name.Substring(0, name.Length - "Controller".Length)
                else 
                    name

            let urlParams (stmts:CodeStatementCollection) = 
                let exps = List<CodeExpression>()
                exps.Add(CodePrimitiveExpression(getControllerName(controller.Name)))
                exps.Add(CodePrimitiveExpression(action.NormalizedName))

                if includeAllParams then
                    let argsDecl = CodeVariableDeclarationStatement(typeof<List<KeyValuePair<string,string>>>, "args", CodeObjectCreateExpression(typeof<List<KeyValuePair<string,string>>>))
                    stmts.Add argsDecl |> ignore

                    for p in action.Parameters do 
                        let addToListCall : CodeStatement = 
                            let keyPairCreation : CodeExpression = 
                                upcast CodeObjectCreateExpression(
                                        CodeTypeReference(typeof<KeyValuePair<string,string>>), 
                                        CodePrimitiveExpression(p.Name),
                                        CodeMethodInvokeExpression(CodeVariableReferenceExpression(p.Name), "ToString"))
                            upcast CodeExpressionStatement( 
                                CodeMethodInvokeExpression(
                                    CodeVariableReferenceExpression("args") :> CodeExpression, 
                                    "Add", 
                                    [| keyPairCreation |]) )
                                         
                        if p.ParamType.IsValueType then
                            stmts.Add addToListCall |> ignore
                        else
                            let nullCheck : CodeStatement = 
                                let binaryCmp : CodeExpression = 
                                    upcast CodeBinaryOperatorExpression(
                                        CodeVariableReferenceExpression(p.Name), CodeBinaryOperatorType.IdentityInequality, CodePrimitiveExpression(null))
                                upcast CodeConditionStatement(binaryCmp, [|addToListCall|])
                            stmts.Add nullCheck  |> ignore

                    exps.Add(CodeMethodInvokeExpression( CodeVariableReferenceExpression("args"), "ToArray" ))
                    
                exps.ToArray()
            
            let stmts = CodeStatementCollection()
                
            let routeParams = CodeObjectCreateExpression(CodeTypeReference(typeof<UrlParameters>), (urlParams stmts))
                
            stmts.Add(CodeMethodReturnStatement(
                            CodeObjectCreateExpression(
                                CodeTypeReference(typeof<RouteBasedTargetUrl>), 
                                CodePropertyReferenceExpression(PropertyName =  "VirtualPath"),
                                CodeIndexerExpression(
                                    CodePropertyReferenceExpression(
                                        CodePropertyReferenceExpression(PropertyName = "CurrentRouter"),
                                        "Routes"),
                                    CodePrimitiveExpression(route.Name)),
                                    routeParams
                                ))
                     ) |> ignore
            stmts

        let generate_route_for_verb (verb:string) (urlType: CodeTypeDeclaration) =
            
            append (get_method verb) false (get_targeturl_stmt false) urlType

            if (verb = "Get" || verb = "Delete") && action.Parameters.Count > 0 then
                append (get_method verb) true (get_targeturl_stmt true) urlType
        
        member x.Action = action
    
        member x.Generate (targetTypeDecl:CodeTypeDeclaration, imports) = 
            let urlType = CodeTypeDeclaration(action.NormalizedName)
            urlType.BaseTypes.Add(typeof<GeneratedUrlsBase>)
            urlType.TypeAttributes <- TypeAttributes.Abstract ||| TypeAttributes.Public
            targetTypeDecl.Members.Add urlType |> ignore

            generate_route_for_verb "Get" urlType
            generate_route_for_verb "Post" urlType
            generate_route_for_verb "Delete" urlType
            generate_route_for_verb "Put" urlType

        member x.Generate () =
            RouteBasedTargetUrl("", route, UrlParameters(Helpers.to_controller_name (controller), action.NormalizedName)).ToString()


    let discover_types (inputAssemblyPath:string)  = 
        let blacklisted = [
            "Castle.MonoRail.dll";
            "Castle.MonoRail.Mvc.ViewEngines.Razor.dll";
            "Castle.MonoRail.Routing.dll";
            "System.ComponentModel.Composition.Codeplex.dll";
            "System.Reflection.Context.dll";
            "Microsoft.Web.Infrastructure.dll";
            "System.Web.Helpers.dll";
            "System.Web.Razor.dll";
            "System.Web.WebPages.dll";
            "System.Web.WebPages.Razor.dll"]

        let types = List<Type>()

        let file = Path.GetFileName inputAssemblyPath
        let found = Seq.exists (fun f -> f = file) blacklisted

        if not found then
            try
                Console.WriteLine ("Processing " + file)
                let asm = Assembly.LoadFrom inputAssemblyPath

                let loaded_types = 
                    try
                        asm.GetExportedTypes()
                    with
                    | :? ReflectionTypeLoadException as ex -> ex.Types

                for t in loaded_types do
                    if t != null then
                        types.Add t

            with 
            | ex -> Console.Error.WriteLine ("could not load " + inputAssemblyPath)

        types |> box :?> Type seq

    let init_httpapp (types:Type seq) =
        let appTypes = 
            types
            |> Seq.filter (fun t -> not (t.IsAbstract) && typeof<HttpApplication>.IsAssignableFrom( t ) )

        let flags = BindingFlags.DeclaredOnly|||BindingFlags.Public|||BindingFlags.NonPublic|||BindingFlags.Instance|||BindingFlags.Static

        for app in appTypes do
            let appinstance = Activator.CreateInstance app :?> HttpApplication
            let startMethod = app.GetMethod("ConfigureRoutes", flags)
            if startMethod != null then
                // executing for side effects only, so we get the configured routes
                let args = 
                    if startMethod.GetParameters().Length = 1 then
                        [|Router.Instance|]:obj[]
                    else
                        [||]
                try
                    if startMethod.IsStatic then
                        startMethod.Invoke(null, args) |> ignore
                    else 
                        startMethod.Invoke(appinstance, args) |> ignore
                with 
                | ex -> 
                    Console.Error.WriteLine ("could not run ConfigureRoutes for " + app.FullName)
                    Console.Error.WriteLine "Routes may have not been evaluated"
                    Console.Error.WriteLine (ex.ToString())

        if Seq.isEmpty Router.Instance.Routes then
            Console.Error.WriteLine "No routes found"
            Environment.Exit -5

        Console.WriteLine ""
        Console.WriteLine "Routes discovered"

        for r in Router.Instance.Routes do
            Console.WriteLine ("\t" + r.Name + "  " + r.Path)

    let (|Prefix|_|) (prefix:string) (path:string) =
        if String.IsNullOrEmpty prefix then 
            None 
        elif path.StartsWith("/" + prefix, StringComparison.OrdinalIgnoreCase) then
            Some(path)
        else
            None

    let best_route_for (controller:ControllerDescriptor) (action:ControllerActionDescriptor) = 
        // try to find a route that start with area or controller name (this should be recursive)
        let result = 
            Router.Instance.Routes 
            |> Seq.tryFind (fun r -> 
                                match r.Path with 
                                | Prefix controller.Name path -> true
                                | Prefix controller.Area path -> true
                                | _ -> false
                        ) 
        match result with 
        | Some route -> route 
        // if nothing was found, default to the "default" route
        | _ -> Router.Instance.Routes |> Seq.find (fun r -> r.Name = "default") 
            

    let csharp_generator (controller2route:Dictionary<Type, List<ActionDef>>) (targetFolder:string) = 
        let compilationUnit = CodeCompileUnit()
        let imports = HashSet<string>()
        imports.Add "System" |> ignore
        imports.Add "System.Web" |> ignore
        imports.Add "Castle.MonoRail" |> ignore
        imports.Add "Castle.MonoRail.Routing" |> ignore
        imports.Add "System.Runtime.InteropServices" |> ignore

        for pair in controller2route do
            let ct = pair.Key
            let defs = pair.Value
            let ns = 
                let found = 
                    compilationUnit.Namespaces 
                    |> Seq.cast 
                    |> Seq.filter (fun (ns:CodeNamespace) -> ns.Name = ct.Namespace)

                if Seq.isEmpty found then 
                    let newns = CodeNamespace(ct.Namespace)
                    compilationUnit.Namespaces.Add newns |> ignore
                    newns
                else 
                    Seq.head found
    
            let typeDecl = CodeTypeDeclaration(ct.Name)
            typeDecl.TypeAttributes <- TypeAttributes.Public
            typeDecl.IsPartial <- true
            ns.Types.Add typeDecl |> ignore

            let staticType = CodeTypeDeclaration("Urls")
            staticType.TypeAttributes <- TypeAttributes.Abstract ||| TypeAttributes.Public
            typeDecl.Members.Add staticType |> ignore
    
            for def in defs do
                def.Generate(staticType, imports)
      
        for ns in compilationUnit.Namespaces do
            for import in imports do
                ns.Imports.Add (CodeNamespaceImport(import)) |> ignore

        let csprovider = new CSharpCodeProvider()

        //csprovider.GenerateCodeFromCompileUnit(compilationUnit, Console.Out, CodeGeneratorOptions())
        if targetFolder <> null then
            use stream = File.CreateText(Path.Combine(targetFolder, "GeneratedRoutes.cs"))
            csprovider.GenerateCodeFromCompileUnit(compilationUnit, stream, CodeGeneratorOptions())
        else
            csprovider.GenerateCodeFromCompileUnit(compilationUnit, Console.Out, CodeGeneratorOptions())

    let js_generator (controller2route:Dictionary<Type, List<ActionDef>>) (targetFolder:string) = 
        let root_tmpl = "
        
        var mrRoutes = {};

        function initializeRouteModule(vpath) {
            vpath = vpath === '/' ? '' : vpath;

            function appendNamespace (namespaceString) {
                var parts = namespaceString.split('.'),
                    parent = mrRoutes,
                    currentPart = '';    

                for (var i = 0, length = parts.length; i < length; i++) {
                    currentPart = parts[i];
                    parent[currentPart] = parent[currentPart] || {};
                    parent = parent[currentPart];
                }

                return parent;
            }"

        let controller_tmpl = "
            var {0} = {{
                {1}
            }};
        "

        let append_tmpl = "
            var ns = appendNamespace('{0}');

            ns.{1} = {1};
        "

        let action_tmpl = "
                {0}: {{
                    get: function (params) {{
                        return vpath + (params == undefined ? '{1}' : '{1}?' + jQuery.param(params));
                    }},
                    post: function() {{ return vpath + '{1}'; }},
                    del: function() {{ return vpath + '{1}'; }},
                    put: function() {{ return vpath + '{1}'; }}
                }}"

        let file = StringBuilder()

        file.Append(root_tmpl) |> ignore

        for pair in controller2route do
            let ct = pair.Key
            let defs = pair.Value

            let actions = StringBuilder()

            for def in defs do
                if actions.Length > 0 then
                    actions.AppendLine(",") |> ignore

                actions.AppendFormat(action_tmpl, def.Action.NormalizedName, def.Generate()) |> ignore

            file.AppendFormat(controller_tmpl, Helpers.to_controller_name (ct), actions.ToString()) |> ignore
            file.AppendFormat(append_tmpl, ct.Namespace, Helpers.to_controller_name (ct)) |> ignore

        file.AppendLine("
        }"
        ) |> ignore
        
        if targetFolder <> null then
            use fstream = File.CreateText(Path.Combine(targetFolder, "GeneratedRoutes.js"))
            fstream.Write(file.ToString())
        else
            Console.WriteLine(file.ToString())

    let generate_routes (inputAssemblyPath:string) (targetFolder:string) = 
        Console.WriteLine ("Assembly: " + inputAssemblyPath)
        Console.WriteLine ("Target folder: " + targetFolder)

        let types = discover_types inputAssemblyPath 

        init_httpapp types
        
        let controllers = 
            types 
            |> Seq.filter (fun t -> not t.IsAbstract && (t.Name.EndsWith("Controller") || typeof<IViewComponent>.IsAssignableFrom(t)))
            |> Seq.sortBy (fun t -> t.FullName)
            |> Seq.map (fun t -> (t, t.Name.Substring(0, t.Name.Length - "Controller".Length)))

        let controller2route = Dictionary<Type, List<ActionDef>>()

        Console.WriteLine()

        Console.WriteLine("Initializing container")

        let tempContainer : IContainer = upcast new Container(DirectoryInfo(inputAssemblyPath).Parent.FullName) 

        Console.WriteLine("Fetching ControllerDescriptorBuilder")
        let descBuilder = tempContainer.Get<TypedControllerDescriptorBuilder>()

        for ct,name in controllers do
            Console.WriteLine ("Processing " + ct.FullName)

            let desc = descBuilder.Build ct
            
            let defs = List<ActionDef>()
            controller2route.[ct] <- defs

            let mutable index = 1;
            let actions = 
                desc.Actions.Distinct(ActionComparer()) |> Seq.sortBy (fun a -> a.NormalizedName) 

            for action in actions do    
                Console.WriteLine ("    Action: " + action.NormalizedName)
                let def = ActionDef(ct, action :?> MethodInfoActionDescriptor, (best_route_for desc action), index)
                index <- index + 1
                defs.Add def

        csharp_generator controller2route targetFolder
        js_generator controller2route targetFolder

            