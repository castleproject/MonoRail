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
    open System.Collections
    open System.Collections.Generic
    open System.Text
    open System.Linq
    open System.Linq.Expressions
    open System.Web
    open Castle.MonoRail
    open Castle.MonoRail.ViewEngines

    type public FormTagHelper(ctx) = 
        inherit BaseHelper(ctx)

        static member Required(required:bool) = 
            if required then " required aria-required=\"true\"" else ""

        member x.Input(itype:string, name:string, id:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            let validId = if id <> null then id else x.ToId name
            let dict = x.Merge html [("id", validId)]
            let encval = if not (value = null) then x.HtmlEncode((value.ToString())) else ""; 

            upcast HtmlResult( 
                (sprintf "<input type=\"%s\" name=\"%s\" value=\"%s\"%s%s/>" 
                            itype name encval (x.AttributesToString dict) (FormTagHelper.Required(required))) )

        member x.FormTag(url:string, ``method``:string, id:string, html:IDictionary<string, string>) : IHtmlStringEx =
            upcast HtmlResult( (sprintf "<form id=\"%s\" action=\"%s\" method=\"%s\"%s>" id url ``method`` (base.AttributesToString html)) )
        member x.FormTag(url:TargetUrl, ``method``:string, id:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.FormTag( url.Generate(null), ``method``, id, html)
        member x.FormTag(url:TargetUrl) : IHtmlStringEx =
            x.FormTag( url.Generate(null), "post", "form_id", null)
        member x.FormTag(url:TargetUrl, html:IDictionary<string, string>) : IHtmlStringEx =
            x.FormTag( url.Generate(null), "post", "form_id", html)
        member x.FormTag(url:TargetUrl, id:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.FormTag( url.Generate(null), "post", id, html)
        member x.FormTag(url:TargetUrl, id:string) : IHtmlStringEx =
            x.FormTag( url.ToString(), "post", id, null)
        member x.FormTag(url, ``method``, id) : IHtmlStringEx =
            x.FormTag( url.ToString(), ``method``, id, null)
        member x.FormTag(id:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.FormTag (ctx.HttpContext.Request.Path, "post", id, html)
        member x.FormTag() : IHtmlStringEx =
            x.FormTag (ctx.HttpContext.Request.Path, "post", "form_id", null)

        member x.EndFormTag() : IHtmlStringEx =
            upcast HtmlResult ("</form>")

        member x.TextFieldTag(name:string) : IHtmlStringEx =
            x.TextFieldTag(name, null)
        member x.TextFieldTag(name:string, value:obj) : IHtmlStringEx =
            x.TextFieldTag(name, base.ToId(name), value)
        member x.TextFieldTag(name:string, id:string, value:obj) : IHtmlStringEx =
            x.TextFieldTag(name, id, value, false, null)
        member x.TextFieldTag(name:string, id:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.TextFieldTag(name, id, null, false, html)
        member x.TextFieldTag(name:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.TextFieldTag(name, false, html)
        member x.TextFieldTag(name:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.TextFieldTag(name, base.ToId(name), null, required, html)
        member x.TextFieldTag(name:string, value:obj, html:IDictionary<string, string>) : IHtmlStringEx =
            x.TextFieldTag(name, base.ToId(name), value, false, html)
        member x.TextFieldTag(name:string, id:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("text", name, id, value, required, html)

        member x.EmailFieldTag(name:string, id:string, value:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("email", name, id, value, required, html)
        member x.EmailFieldTag(name:string, value:string) : IHtmlStringEx =
            x.EmailFieldTag(name, base.ToId(name), value)
        member x.EmailFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.EmailFieldTag(name, id, value, false, null)

        member x.UrlFieldTag(name:string, id:string, value:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("url", name, id, value, required, html)
        member x.UrlFieldTag(name:string, value:string) : IHtmlStringEx =
            x.UrlFieldTag(name, base.ToId(name), value)
        member x.UrlFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.UrlFieldTag(name, id, value, false, null)

        member x.PhoneFieldTag(name:string, id:string, value:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("tel", name, id, value, required, html)
        member x.PhoneFieldTag(name:string, value:string) : IHtmlStringEx =
            x.PhoneFieldTag(name, base.ToId(name), value)
        member x.PhoneFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.PhoneFieldTag(name, id, value, false, null)

        member x.SearchFieldTag(name:string, id:string, value:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("search", name, id, value, required, html)
        member x.SearchFieldTag(name:string, value:string) : IHtmlStringEx =
            x.SearchFieldTag(name, base.ToId(name), value)
        member x.SearchFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.SearchFieldTag(name, id, value, false, null)

        // consider exposing min, max and step as int
        member x.NumberFieldTag(name:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("number", name, base.ToId(name), null, false, html)
        member x.NumberFieldTag(name:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("number", name, base.ToId(name), null, required, html)
        member x.NumberFieldTag(name:string, value:obj, required:bool) : IHtmlStringEx =
            x.NumberFieldTag(name, base.ToId(name), value, required, null)
        member x.NumberFieldTag(name:string, id:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("number", name, id, value.ToString(), required, html)
        member x.NumberFieldTag(name:string, value:obj) : IHtmlStringEx =
            x.NumberFieldTag(name, base.ToId(name), value)
        member x.NumberFieldTag(name:string, id:string, value:obj) : IHtmlStringEx =
            x.NumberFieldTag(name, id, value, false, null)

        member x.HiddenFieldTag(name:string, id:string, value:string, html:IDictionary<string, string>) : IHtmlStringEx =
            let merged = x.Merge html [("id", id)]
            x.Input("hidden", name, id, value, false, html)
        member x.HiddenFieldTag(name:string, value:string) : IHtmlStringEx =
            x.HiddenFieldTag(name, base.ToId(name), value)
        member x.HiddenFieldTag(name:string) : IHtmlStringEx =
            x.HiddenFieldTag(name, base.ToId(name), null)
        member x.HiddenFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.HiddenFieldTag(name, id, value, null)
        member x.HiddenFieldTag(name:string, html:IDictionary<string, string>) : IHtmlStringEx =
            x.HiddenFieldTag(name, base.ToId(name), null, html)

        member x.RangeFieldTag(name:string, id:string, min:int, max:int, value:int, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            let merged = x.Merge html [("min", min.ToString());("max", max.ToString())]
            x.Input("range", name, id, value.ToString(), required, html)
        member x.RangeFieldTag(name:string, id:string, min:int, max:int, value:int) : IHtmlStringEx =
            x.NumberFieldTag(name, id, value, false, null)
        member x.RangeFieldTag(name:string, min:int, max:int, value:int) : IHtmlStringEx =
            x.RangeFieldTag(name, base.ToId(name), min, max, value)

        member x.ColorFieldTag(name:string, id:string, value:int, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            let dict = x.Merge html [("pattern", "#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})")]
            x.Input("color", name, id, value.ToString(), required, dict)
        member x.ColorFieldTag(name:string, value:int) : IHtmlStringEx =
            x.ColorFieldTag(name, base.ToId(name), value)
        member x.ColorFieldTag(name:string, id:string, value:int) : IHtmlStringEx =
            x.ColorFieldTag(name, id, value, false, null)

        member x.DateFieldTag(name:string, id:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.DateFieldTag(name, id, null, required, html)
        member x.DateFieldTag(name:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.DateFieldTag(name, base.ToId(name), null, required, html)
        member x.DateFieldTag(name:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.DateFieldTag(name, base.ToId(name), value, required, html)
        member x.DateFieldTag(name:string, id:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("date", name, id, value, required, html)

        // all dates ISO 8601
        // YYYY-MM-DD
        // Consider min max for these too. As DateTime each
        member x.DateYMDFieldTag(name:string, id:string, value:DateTime, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            x.Input("date", name, id, value.ToString("YYYY-MM-DD"), required, html)
        // YYYY-MM
        member x.DateYMFieldTag(name:string, id:string, value:DateTime, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            x.Input("month", name, id, value.ToString("YYYY-MM-DD"), required, html)
        // HH:mm
        member x.TimeFieldTag(name:string, id:string, value:DateTime, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            x.Input("time", name, id, value.ToString("HH:mm"), required, html)
        // 2011-03-17T10:45-5:00
        member x.DateTimeFieldTag(name:string, id:string, value:DateTime, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            x.Input("datetime", name, id, value.ToString(), required, html)
        // 2011-03-17T10:45
        member x.DateTimeLocalFieldTag(name:string, id:string, value:DateTime, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            x.Input("datetime-local", name, id, value.ToString("YYYY-MM-DD"), required, html)


        member x.LabelTag(label:string, targetid:string, html:IDictionary<string, string>) : IHtmlStringEx =
            upcast HtmlResult ( sprintf "<label for=\"%s\" %s>%s</label>" targetid (base.AttributesToString html) (x.HtmlEncode label))
        member x.LabelTag(label:string, targetid:string) : IHtmlStringEx =
            x.LabelTag(label, targetid, null)
        member x.LabelTag(label:string) : IHtmlStringEx =
            x.LabelTag(label, (x.ToId label))


        member x.FileFieldTag(name:string, id:string, value:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("file", name, id, value, required, html)
        member x.FileFieldTag(name:string, value:string) : IHtmlStringEx =
            x.FileFieldTag(name, base.ToId(name), value)
        member x.FileFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.FileFieldTag(name, id, value, false, null)
            

        // consider adding role="checkbox" aria-labelledby="labelB"
        // investigate more aria attributes
        member x.CheckboxTag(name:string, id:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("checkbox", name, id, value, required, html)
        member x.CheckboxTag(name:string, value:obj) : IHtmlStringEx =
            x.CheckboxTag(name, base.ToId(name), value)
        member x.CheckboxTag(name:string, value:obj, isChecked:bool) : IHtmlStringEx =
            let html = Attributes()
            if isChecked then
                html.Add ("checked", "checked")

            x.CheckboxTag(name, base.ToId(name), value, false, html)
        member x.CheckboxTag(name:string, id:string, value:obj) : IHtmlStringEx =
            x.CheckboxTag(name, id, value, false, null)

        member x.PasswordFieldTag(name:string, id:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("password", name, id, value, required, html)
        member x.PasswordFieldTag(name:string, value:obj, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.PasswordFieldTag(name, base.ToId(name), value, required, html)
        member x.PasswordFieldTag(name:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.PasswordFieldTag(name, base.ToId(name), null, required, html)
        member x.PasswordFieldTag(name:string, value:string) : IHtmlStringEx =
            x.PasswordFieldTag(name, base.ToId(name), value)
        member x.PasswordFieldTag(name:string, id:string, value:obj) : IHtmlStringEx =
            x.PasswordFieldTag(name, id, value, false, null)

        member x.RadioFieldTag(name:string, id:string, value:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.Input("radio", name, id, value, required, html)
        member x.RadioFieldTag(name:string, id:string, value:string, required:bool) : IHtmlStringEx =
            x.Input("radio", name, id, value, required, null)
        member x.RadioFieldTag(name:string, value:string) : IHtmlStringEx =
            x.RadioFieldTag(name, base.ToId(name), value)
        member x.RadioFieldTag(name:string, id:string, value:string) : IHtmlStringEx =
            x.RadioFieldTag(name, id, value, false, null)


        member x.ButtonTag(caption:string, id:string) : IHtmlStringEx = 
            upcast HtmlResult(sprintf "<button type=\"button\" id=\"%s\">%s</button>" id caption)

        (* 
        <label for="favcolor">Favorite Color</label>
        <input type="text" list="colors" id="favcolor" name="favcolor">

        <datalist id="colors">
            <option value="Blue">
            <option value="Green">
            <option value="Pink">
            <option value="Purple">
        </datalist>        
        *)
        member x.DataList(id:string, values:'a seq) : IHtmlStringEx =
            let sb = StringBuilder()
            
            sb.AppendLine(sprintf "<datalist id=\"%s\">" id) |> ignore
            values |> Seq.iter (fun v -> ( sb.AppendLine(sprintf "\t<option value=\"%s\">" (x.HtmlEncode(v.ToString()))) |> ignore ) )
            sb.AppendLine "</datalist>" |> ignore
            
            upcast HtmlResult( sb.ToString() )

        member x.DataList(id:string, values:'a seq, valueSelector:Func<'a, string>) : IHtmlStringEx =
            let sb = StringBuilder()
            
            sb.AppendLine(sprintf "<datalist id=\"%s\">" id) |> ignore
            values |> Seq.iter (fun v -> ( sb.AppendLine(sprintf "\t<option value=\"%s\">" (x.HtmlEncode( valueSelector.Invoke( v ))) ) |> ignore))
            sb.AppendLine "</datalist>" |> ignore
            
            upcast HtmlResult( sb.ToString() )

        (*
        member x.DataList(id:string, values:string * string seq) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            upcast HtmlResult( "" )

        member x.DataList(id:string, values:string * int seq) : IHtmlStringEx =
            failwith "not implemented - figure out right format"
            upcast HtmlResult( "" )
        *)

        member x.SelectTag(name:string, values:IEnumerable<Object>) : IHtmlStringEx =
            x.SelectTag(name, values, Map.empty)

        member x.SelectTag(name:string, values:IDictionary) : IHtmlStringEx =
            x.SelectTag(name, values, Map.empty)

        member x.SelectTag(name:string, values:IDictionary, html:IDictionary<string, string>) : IHtmlStringEx =
            x.SelectTag(name, (values :> IEnumerable), html)

        member x.SelectTag(name:string, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            x.SelectTag(name, null, Array.empty, required, html)

        member x.SelectTag(name:string, values:IEnumerable, html:IDictionary<string, string>) : IHtmlStringEx =
            x.SelectTag(name, null, values, html)

        member x.SelectTag(name:string, selected:obj, values:IEnumerable, html:IDictionary<string, string>) : IHtmlStringEx =
            x.SelectTag(name, selected, values, false, html)

        member x.SelectTag(name:string, selected:obj, values:IEnumerable, required:bool, html:IDictionary<string, string>) : IHtmlStringEx =
            let read_firstoption (sb:StringBuilder) = 
                if html.ContainsKey("firstOption") then
                    let fopt = html.GetAndRemove("firstOption") 
                    let foptvalue = if html.ContainsKey("firstOptionValue") then html.GetAndRemove("firstOptionValue") else null

                    sb.AppendLine(sprintf "<option value=\"%s\" selected>%s</option>" (if foptvalue = null then fopt else foptvalue) fopt) |> ignore
            
            let sb = StringBuilder()
            
            sb.AppendLine(sprintf "<select id=\"%s\" name=\"%s\" %s %s>" (base.ToId(name)) name (base.AttributesToString html) (FormTagHelper.Required(required))) |> ignore

            read_firstoption sb

            if typeof<IDictionary>.IsAssignableFrom(values.GetType()) then
                let dict = values :?> IDictionary
                for value in dict do
                    let entry = value :?> DictionaryEntry
                    sb.AppendLine(sprintf "<option value=\"%s\" %s>%s</option>" (entry.Key.ToString()) (if entry.Key.Equals(selected) then "selected=\"selected\"" else "") (entry.Value.ToString())) |> ignore
            else
                for v in values do
                    sb.AppendLine(sprintf "<option value=\"%s\" %s>%s</option>" (v.ToString()) (if v.Equals(selected) then "selected=\"selected\"" else "") (v.ToString())) |> ignore

            sb.AppendLine("</select>") |> ignore

            upcast HtmlResult (sb.ToString())

        member x.ImageSubmitTag() : IHtmlStringEx =
            failwithf "not implemented"
            upcast HtmlResult ""

        member x.SubmitTag() : IHtmlStringEx =
            upcast HtmlResult "<input type=\"submit\" />"

        member x.SubmitTag(value:string) : IHtmlStringEx =
            upcast HtmlResult (sprintf "<input type=\"submit\" value=\"%s\" />" value)

        member x.SubmitTag(value:string, html:IDictionary<string, string>) : IHtmlStringEx =
            upcast HtmlResult (sprintf "<input type=\"submit\" value=\"%s\" %s/>" value (base.AttributesToString html))
