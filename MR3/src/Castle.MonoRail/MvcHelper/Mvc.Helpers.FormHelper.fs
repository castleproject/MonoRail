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
    open System.IO
    open System.Collections.Generic
    open System.ComponentModel.DataAnnotations
    open System.Text
    open System.Globalization
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.ViewEngines


    type ModelHelper(ctx) = 
        inherit BaseHelper(ctx) 
        
        member x.For<'a when 'a : null>() = 
            ModelHelperContext<'a>(null)

        member x.For<'a when 'a : null>(model:'a) = 
            ModelHelperContext<'a>(model)

    and ModelHelperContext<'a when 'a : null>(model:'a) = 
        class
            
            member x.Model = model

            member x.Form() = 
                null

            member x.Display() = 
                null

            member x.DisplayFor( propExp:Expression<Func<'a, obj>> ) = 
                null
        end


    type FormHelper(ctx) = 
        inherit BaseHelper(ctx)

        let _formTagHelper = FormTagHelper(ctx)

        let extract_prefix (modelType:Type) = 
            modelType.Name.ToLowerInvariant()

        // takes model
        member x.For(model:'TModel, url:TargetUrl, block:Func<GenFormBuilder<'TModel>, HtmlResult>) : IHtmlStringEx = 
            let prefix = extract_prefix typeof<'TModel>
            let writer = new StringWriter()
            let builder = x.InternalFormFor(prefix, model, (url.Generate null), "post", null, writer)
            block.Invoke(builder).WriteTo(writer)
            writer.WriteLine "\r\n</form>"
            upcast HtmlResult( writer.ToString() )

        // non generic version
        (*
        member x.For(url:TargetUrl, prefix, block:Func<FormBuilder, HtmlResult>) : IHtmlStringEx = 
            let writer = new StringWriter()
            let builder : FormBuilder = x.InternalFormFor(prefix, (url.Generate null), "post", null, writer)
            block.Invoke(builder).WriteTo(writer)
            writer.WriteLine "</form>"
            upcast HtmlResult( writer.ToString() )
        *)

        member internal x.InternalFormFor(prefix:string, model:'TModel, url:string, ``method``, html:IDictionary<string,string>, writer:TextWriter) = 
            _formTagHelper.FormTag(url, ``method``, prefix + "_form", html).WriteTo writer
            writer.WriteLine()
            GenFormBuilder(prefix, writer, ctx, model)
        (*
        member internal x.InternalFormFor(prefix:string, url:string, ``method``, html:IDictionary<string,string>, writer:TextWriter) = 
            _formTagHelper.FormTag(url, ``method``, prefix + "_form", html).WriteTo writer
            writer.WriteLine()
            FormBuilder(prefix, writer, _formTagHelper)
        *)


    and FormBuilder(prefix, writer) = 
        class
            (* 
            member this.Label(name:string) : IHtmlStringEx =
                failwithf "not implemented"
                upcast HtmlResult ""

            member this.TextField(name:string) : IHtmlStringEx =
                // should we access the metadata here? I vote for no
                failwithf "not implemented"
                upcast HtmlResult ""
            member this.TextArea() : IHtmlStringEx =
                failwithf "not implemented"
                upcast HtmlResult ""

            member this.FileField() : IHtmlStringEx =
                failwithf "not implemented"
                upcast HtmlResult ""
            *)
        end
        
    and private TemplateHelper(ctx) = 
        inherit BaseHelper(ctx) 
        (*  
            ~/Areas/AreaName/Views/ControllerName/DisplayTemplates/TemplateName.aspx
            ~/Areas/AreaName/Views/Shared/DisplayTemplates/TemplateName.aspx
            ~/Views/ControllerName/DisplayTemplates/TemplateName.aspx
            ~/Views/Shared/DisplayTemplates/TemplateName.aspx           *)
        
        member x.HasView (viewName:string, templateType:string) = 
            let baseReq = ctx.ViewContext.ViewRequest
            let vr = ViewRequest(GroupFolder = baseReq.GroupFolder, ViewFolder = baseReq.ViewFolder, ViewName = templateType + "/" + viewName)
            ctx.ServiceRegistry.ViewRendererService.HasPartialView(vr, ctx.HttpContext)

        member x.RenderTemplate (viewName:string, templateType:string, model, writer) = 
            let baseReq = ctx.ViewContext.ViewRequest
            let vr = ViewRequest(GroupFolder = baseReq.GroupFolder, ViewFolder = baseReq.ViewFolder, ViewName = templateType + "/" + viewName)
            ctx.ServiceRegistry.ViewRendererService.RenderPartial(vr, ctx.HttpContext, null, model, writer)
            

    and GenFormBuilder<'TModel>(prefix, writer, ctx:HelperContext, model:'TModel) = 
        inherit FormBuilder(prefix, writer)

        let _formTagHelper  = lazy( FormTagHelper(ctx) )
        let _templateHelper = lazy( TemplateHelper(ctx) )
        let _metadataProvider = ctx.ModelMetadataProvider
        let mutable _formTemplate : Func<TemplateFormBuilder, HtmlResult> = null

        let ensure_setup() = 
            if _formTemplate = null then failwith "No template set. Call FormTemplate first"

        let try_resolve_template (propMetadata:ModelMetadata) (templateType) = 
            if propMetadata.ModelType.IsPrimitive then 
                _templateHelper.Force().HasView( propMetadata.ModelType.Name, templateType )
            else 
                _templateHelper.Force().HasView( propMetadata.ModelType.Name, templateType )

        let getModelMeta (memberAccesses:PropertyInfo[]) = 
            let targetProp = Array.get memberAccesses (memberAccesses.Length - 1)
            _metadataProvider.Create(targetProp.ReflectedType).GetPropertyMetadata targetProp

        let buildNames (memberAccesses:PropertyInfo[]) = 
            let namebuf = StringBuilder(prefix)
            let idbuf = StringBuilder(prefix)
            for memberAccess in memberAccesses do
                let name = memberAccess.Name.ToLowerInvariant()
                namebuf.Append (sprintf "[%s]" name) |> ignore
                idbuf.Append (sprintf "_%s" name) |> ignore
            (namebuf.ToString(), idbuf.ToString())

        (* 
        member x.FieldsFor(inner:Func<FormBuilder<'a>, HtmlResult>) : IHtmlStringEx =
            // let writer = new StringWriter()
            // inner.Invoke(this).WriteTo(writer)
            // HtmlString( (writer.ToString()) ) :> IHtmlString
            upcast HtmlResult ""
        *)
        
        member x.FormTemplate (template:Func<TemplateFormBuilder, HtmlResult>) = 
            _formTemplate <- template

        // @builder.TemplateFor("Password", @=> input <text> input.PasswordField("password") </text> )
        member x.TemplateFor(label:Func<FormTagHelper, IHtmlStringEx>, inputSelection:Func<FormTagHelper, IHtmlStringEx>) : IHtmlStringEx = 
            ensure_setup()
            
            let templateCtx = 
                TemplateFormBuilder( 
                    (fun _ -> label.Invoke(_formTagHelper.Force())), 
                    (fun _ -> inputSelection.Invoke(_formTagHelper.Force())))
            upcast _formTemplate.Invoke(templateCtx)

        member x.TemplateFor(propertyAccess:Expression<Func<'TModel, obj>>) : IHtmlStringEx =
            ensure_setup()

            let memberAccesses = properties_from_exp propertyAccess
            let name, idVal = buildNames memberAccesses
            let modelMeta = getModelMeta memberAccesses
            
            let templateCtx = 
                TemplateFormBuilder(
                    (fun _ -> x.InternalLabelFor(modelMeta, idVal)), 
                    (fun _ -> x.InternalEditorFor(name, idVal, modelMeta)))
            upcast _formTemplate.Invoke(templateCtx)

        member this.LabelFor(propertyAccess:Expression<Func<'TModel, obj>>) : IHtmlStringEx =
            let memberAccesses = properties_from_exp propertyAccess
            let _, idVal = buildNames memberAccesses
            let modelMeta = getModelMeta memberAccesses
            this.InternalLabelFor(modelMeta, idVal)

        member x.DisplayForModel() : IHtmlStringEx =
            upcast HtmlResult ""

        member x.EditorForModel() : IHtmlStringEx =
            upcast HtmlResult ""

        member x.DisplayFor() : IHtmlStringEx =
            upcast HtmlResult ""

        member x.EditorFor(propertyAccess:Expression<Func<'TModel, obj>>) : IHtmlStringEx =
            let memberAccesses = properties_from_exp propertyAccess
            let propMetadata = getModelMeta memberAccesses
            let nameVal, idVal = buildNames memberAccesses
            x.InternalEditorFor (nameVal, idVal, propMetadata)

        member internal this.InternalLabelFor (modelMeta:ModelMetadata, id:string) : IHtmlStringEx =
            _formTagHelper.Force().LabelTag(modelMeta.DisplayName, id)

        member internal x.InternalEditorFor (nameVal, idVal, propMetadata:ModelMetadata) : IHtmlStringEx =
            // todo: remove usage of seq
            let tuples = 
                seq {
                        if propMetadata.DefaultValue <> null then 
                            yield ("placeholder", propMetadata.DefaultValue.ToString()) 
                    }
            let isRequired = propMetadata.Required <> null
            let htmlAtts = Map(tuples)
            // TODO: only supports one level of depth
            let propVal = propMetadata.GetValue(model) // propertyAccess.Compile().Invoke(model)
            let valueStr = if propVal = null then null else propVal.ToString()

            // let existing = try_resolve_partial propMetadata

            match propMetadata.DataType with 
            | DataType.Text ->
                _formTagHelper.Force().
                    TextFieldTag(
                                    name = nameVal, 
                                    id = idVal, 
                                    value = valueStr, required = isRequired, html = htmlAtts)
            | DataType.Date ->
                _formTagHelper.Force().
                    DateYMDFieldTag(
                                    name = nameVal, 
                                    id = idVal, 
                                    value = (propVal :?> DateTime), required = isRequired, html = htmlAtts)
            | DataType.EmailAddress ->
                _formTagHelper.Force().
                    EmailFieldTag(
                                    name = nameVal, 
                                    id = idVal, 
                                    value = valueStr, required = isRequired, html = htmlAtts)
            | DataType.Password ->
                _formTagHelper.Force().
                    PasswordFieldTag(
                                    name = nameVal, 
                                    id = idVal, 
                                    value = valueStr, required = isRequired, html = htmlAtts)
            | DataType.PhoneNumber ->
                _formTagHelper.Force().
                    PhoneFieldTag(
                                    name = nameVal, 
                                    id = idVal, 
                                    value = valueStr, required = isRequired, html = htmlAtts)
            | DataType.Url ->
                _formTagHelper.Force().
                    UrlFieldTag(
                                    name = nameVal, 
                                    id = idVal, 
                                    value = valueStr, required = isRequired, html = htmlAtts)
            | _ ->
                failwithf "DataType not support for FieldFor. DataType: %O" (propMetadata.DataType)



    and TemplateFormBuilder ( label:unit -> IHtmlStringEx, field:unit -> IHtmlStringEx ) = 
        member x.Label() : IHtmlStringEx =
            label()

        member x.Field() : IHtmlStringEx =
            field()
            







