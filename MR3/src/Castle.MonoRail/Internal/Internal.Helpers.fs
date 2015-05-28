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

[<AutoOpen>]
module Helpers
    
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Reflection
    open System.Dynamic

    let inline (==) a b = Object.ReferenceEquals(a, b)
    let inline (===) (a:string) (b:string) = StringComparer.OrdinalIgnoreCase.Equals(a, b)
    let inline (!=) a b = not (Object.ReferenceEquals(a, b))
    let inline (<?>) (a:'a) (b:'a) = if a <> null then a else b

    let arg_not_null (a:'a) (paramName:string) = 
        if a == null then raise(ArgumentNullException(paramName))

    let assumes_concrete (t:Type) = 
        if t.IsInterface || t.IsAbstract then raise(Exception("Expecting a concrete type, but got " + t.FullName))

    let internal merge_dict over orig : Dictionary<string,string> = 
        if over = null then 
            orig 
        elif orig = null then 
            over
        else 
            let dict = Dictionary(orig, orig.Comparer)
            for pair in over do
                dict.[pair.Key] <- pair.Value
            dict

    let to_controller_name (typ:System.Type) = 
        let name = typ.Name
        if name.EndsWith "Controller" then
            name.Substring (0, name.Length - 10)
        else 
            name

    let internal url_path_combine (url1:string) (url2:string) = 
        url1.TrimEnd([|'/'|]) + "/" + url2.TrimStart([|'/'|])
   
    let internal get_effective_http_method (req:System.Web.HttpRequestBase) = 
        let met = req.HttpMethod
        let override1 = req.Form.["_method"] // rails style
        let override2 = req.Headers.["X-HTTP-Method"] // o-data style
        let override3 = req.Headers.["X-HTTP-Method-Override"] // g-data
        if met === "POST" && (not (String.IsNullOrEmpty override1) || not (String.IsNullOrEmpty(override2)) || not (String.IsNullOrEmpty(override3)))  then
            if not <| String.IsNullOrEmpty override1 
            then override1
            elif not <| String.IsNullOrEmpty override2 
            then override2 
            else override3
        else met

    // see http://www.trelford.com/blog/post/Exposing-F-Dynamic-Lookup-to-C-WPF-Silverlight.aspx
    // this type is NOT thread safe and doesn't need to be
    [<AllowNullLiteral>]
    type DynamicLookup() =
        inherit DynamicObject()
        let _props = Dictionary<string,obj>()

        member private x.Props = _props

        member private this.GetValue name = 
            let res, value = _props.TryGetValue name
            if res then 
                value
            else 
                null

        member private this.SetValue (name,value) =
            _props.[name] <- value

        override this.TryGetMember (binder:GetMemberBinder, result:obj byref) =     
            let r, tmp = _props.TryGetValue binder.Name
            result <- tmp
            r
    
        override this.TrySetMember(binder:SetMemberBinder, value:obj) =        
            this.SetValue(binder.Name, value)
            true
    
        override this.GetDynamicMemberNames() =
            upcast _props.Keys
    
        interface IDictionary<string,obj> with 
            member x.Add (key, value) = 
                _props.Add (key, value)

            member x.Remove key = 
                _props.Remove key

            member x.ContainsKey key = 
                _props.ContainsKey key

            member x.TryGetValue (key, result:obj byref) =
                _props.TryGetValue (key, ref result)

            member x.Item 
                with get(key) = _props.Item(key) 
                and  set key v = _props.[key] <- v

            member x.Keys = upcast _props.Keys
            member x.Values = upcast _props.Values

        interface ICollection<KeyValuePair<string,obj>> with 
            member x.Add (pair) = 
                (_props |> box :?> ICollection<KeyValuePair<string,obj>>).Add pair
            member x.IsReadOnly = 
                (_props |> box :?> ICollection<KeyValuePair<string,obj>>).IsReadOnly
            member x.Clear() = 
                (_props |> box :?> ICollection<KeyValuePair<string,obj>>).Clear()
            member x.Contains(item) = 
                (_props |> box :?> ICollection<KeyValuePair<string,obj>>).Contains(item)
            member x.Remove(item) = 
                (_props |> box :?> ICollection<KeyValuePair<string,obj>>).Remove(item)
            member x.Count = _props.Count
            member x.CopyTo (array, index) = 
                (_props |> box :?> ICollection<KeyValuePair<string,obj>>).CopyTo(array, index)

        interface IEnumerable<KeyValuePair<string,obj>> with 
            member x.GetEnumerator() =
                (_props |> box :?> IEnumerable<KeyValuePair<string,obj>>).GetEnumerator()

        interface Collections.IEnumerable with 
            member x.GetEnumerator() =
                (_props |> box :?> Collections.IEnumerable).GetEnumerator()

        (*
        static member (?) (lookup:#DynamicLookup, name:string) =
            let r, tmp = lookup.Props.TryGetValue name
            if r then
                tmp
            else
                raise (new System.MemberAccessException())
    
        static member (?<-) (lookup:#DynamicLookup, name:string, value:'v) =
            lookup.SetValue (name,value)

        static member GetValue (lookup:DynamicLookup, name) =
            lookup.GetValue(name)
        *)
    
    type System.Collections.Generic.IDictionary<'a, 'b> with 
        member x.GetAndRemove(key) = 
            let value = x.[key]
            x.Remove(key) |>ignore
            value