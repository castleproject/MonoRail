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
    open System.Collections.Specialized
    open System.Dynamic
    open System.Runtime.Serialization


    [<AbstractClass; AllowNullLiteral>]
    type TargetUrl() = 
        abstract member Generate : parameters:IDictionary<string,string> -> string

    type Attributes() =
        inherit Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)

    type Options() = 
        inherit Dictionary<obj,obj>()


    [<Interface; AllowNullLiteral>]
    type IModelAccessor<'a> = 
        abstract member Model : 'a


    // very early incarnation 
    [<AllowNullLiteral>]
    type PropertyBag<'TModel>() = 
        inherit DynamicObject()
        let mutable _model : 'TModel = Unchecked.defaultof<_>
        let _props = Dictionary<string,obj>()

        member private x.Props = _props

        member private this.GetValue name = 
            let res, value = _props.TryGetValue name
            if res 
            then value
            else null

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

        interface IEnumerable with 
            member x.GetEnumerator() =
                (_props |> box :?> IEnumerable).GetEnumerator()

        member x.Model 
            with get() = _model and set v = _model <- v

        member x.Item 
            with get(key) = _props.Item(key) 
            and  set key v = _props.[key] <- v

        interface IModelAccessor<'TModel> with 
            member x.Model = _model



    [<AllowNullLiteral>]
    type PropertyBag() = 
        inherit PropertyBag<obj>()


    [<AllowNullLiteral>]
    type Model<'TModel>(model:'TModel) = 
        let _model = model
        member x.Value = _model
        member x.IsValid = true
        // validation stuff here


    // Allows content to be persisted across requests. Useful for error messages
    [<AllowNullLiteral;Serializable>]
    type Flash (copy:Flash) as self = 
        class
            [<DefaultValue;NonSerialized>] val mutable _hasSwept : bool 
            [<DefaultValue;NonSerialized>] val mutable _hasItemsToKeep : bool 
            [<DefaultValue;NonSerialized>] val mutable _keep : HashSet<string> 
            [<DefaultValue>] val mutable _items : Dictionary<string,obj>

            // new (info:SerializationInfo, ctx:StreamingContext) = 
            
            new () = Flash(null)    

            do
                if copy <> null then 
                    self._items <- Dictionary<string,obj>(copy._items, StringComparer.OrdinalIgnoreCase)
                else 
                    self._items <- Dictionary<string,obj>(StringComparer.OrdinalIgnoreCase)

                self._keep <- HashSet<string>(StringComparer.OrdinalIgnoreCase)

            member private x.InternalAdd(key:string, value:obj, keep:bool) = 
                x._items.[key] <- value
                if keep then x._keep.Add(key) |> ignore

            member x.Item 
                with get(name:string) = let _, v = x._items.TryGetValue(name) 
                                        v 
                 and set (name:string) value = x.InternalAdd(name, value, true)

            member x.Now(key, value) = 
                x.InternalAdd (key, value, false)

            member x.Add(key, value) = 
                x.InternalAdd (key, value, true)

            member x.ContainsKey(key) = x._items.ContainsKey(key)

            member x.Count = x._items.Count

            member x.Clear() = 
                x._items.Clear()

            member x.Remove(key) = 
                x._items.Remove(key) 

            // Remove any element thats not marked to be kept.
            // This method is automatically called by the framework after the controller is processed.
            member x.Sweep() = 
                if x._hasSwept then 
                    x.Keep()

                if x._keep.Count = 0 then
                    x.Clear()
                else
                    x._items.Keys 
                    |> Seq.choose (fun k -> if not (x._keep.Contains k) then Some(k) else x._hasItemsToKeep <- true; None)
                    |> Seq.toArray
                    |> Seq.iter (fun k -> (x.Remove(k) |> ignore))

                    x._keep.Clear()
                x._hasSwept <- true

            member x.Keep() = 
                x._keep.Clear()
                x._items.Keys 
                |> Seq.iter (fun k -> x._keep.Add k |> ignore) 

            member x.Keep(key:string) = 
                x._keep.Add key |> ignore

            member x.Discard() = 
                x._keep.Clear()

            member x.Discard(key:string) = 
                x._keep.Remove key |> ignore

            interface IEnumerable<KeyValuePair<string,obj>> with 
                member x.GetEnumerator() =
                    (x._items |> box :?> IEnumerable<KeyValuePair<string,obj>>).GetEnumerator()
            
            interface IEnumerable with 
                member x.GetEnumerator() =
                    (x._items |> box :?> IEnumerable).GetEnumerator()
        end

    (*
    [<AbstractClass>]
    type Controller() = 
        let mutable _req = Unchecked.defaultof<HttpRequestBase>
        
        member x.Request 
            with get() = _req and set v = _req <- v
    *)

    module MediaTypes = 
        begin
            [<Literal>] 
            let public Html  = "text/html"
            [<Literal>] 
            let public XHtml = "application/xhtml+xml"
            [<Literal>] 
            let public Xml   = "application/xml"
            [<Literal>] 
            let public Atom  = "application/atom+xml"
            [<Literal>] 
            let public JSon  = "application/json"
            [<Literal>] 
            let public Js    = "application/javascript"
            [<Literal>] 
            let public Soap  = "application/soap+xml"
            [<Literal>] 
            let public Rss   = "application/rss+xml"
            [<Literal>] 
            let public FormUrlEncoded = "x-www-form-urlencoded"
            [<Literal>] 
            let public MultipartForm  = "multipart/form-data"
            
            
            
        end

    [<AllowNullLiteral>]
    type HttpError(statusCode:HttpStatusCode, errorCode:string, description:string) = 
        let _statusCode  = ref statusCode
        let _errorCode   = ref errorCode
        let _description = ref description

        new (statusCode:HttpStatusCode) = HttpError(statusCode, null, null)

        member x.StatusCode  with get() = !_statusCode  and set(v) = _statusCode := v  
        member x.ErrorCode   with get() = !_errorCode   and set(v) = _errorCode  := v  
        member x.Description with get() = !_description and set(v) = _description := v   
    

    [<Serializable>]
    type MonoRailException = 
        inherit Exception
        new (msg) = { inherit Exception(msg) }
        new (msg, ex:Exception) = { inherit Exception(msg, ex) }
        new (info:SerializationInfo, context:StreamingContext) = 
            { 
                inherit Exception(info, context)
            }

    [<Serializable>]
    type ViewEngineException = 
        inherit MonoRailException 
        new (msg) = { inherit MonoRailException(msg) }
        new (msg, ex:Exception) = { inherit MonoRailException(msg, ex) }
        new (info:SerializationInfo, context:StreamingContext) = 
            { 
                inherit MonoRailException(info, context)
            }

    [<Serializable>]
    type RouteException = 
        inherit Exception
        new (msg) = { inherit Exception(msg) }
        new (info:SerializationInfo, context:StreamingContext) = 
            { 
                inherit Exception(info, context)
            }


