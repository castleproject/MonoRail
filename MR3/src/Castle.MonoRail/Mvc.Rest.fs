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
    open System.Collections.Generic
    open System.Web
    open System.Runtime.InteropServices
    open Castle.MonoRail.Routing
    open System.Text
    open System.Globalization
    open System.Collections.Generic
    open System.Runtime.Serialization
    open FParsec


    type ResourceLink(uri:string, rel:string, contenttype:string, label:string) = 
        let mutable _uri = uri
        let mutable _rel = rel
        let mutable _label = label
        let mutable _contenttype = contenttype

        new() = 
            ResourceLink(null, null, null, null)

        member x.Uri 
            with get() = _uri and set(v) = _uri <- v
        member x.Label 
            with get() = _label and set(v) = _label <- v
        member x.Rel
            with get() = _rel and set(v) = _rel <- v
        member x.ContentType 
            with get() = _contenttype and set(v) = _contenttype <- v


    [<Serializable>]
    type Resource<'a>(resource:'a) = 
        let _links = lazy List<ResourceLink>()
        member x.Links = _links.Force()
        member x.Resource = resource


    [<Interface>]
    type IModelHypertextProcessor<'a> = 
        abstract member AddHypertext : model:'a -> unit
        

    [<System.ComponentModel.Composition.Export;AllowNullLiteral>]
    type ModelHypertextProcessorResolver() = 
        let _dict = lazy Dictionary<Type, obj>()

        member x.Register<'TModel> (processor:IModelHypertextProcessor<'TModel>) = 
            _dict.Force().[typeof<'TModel>] <- box processor

        member x.TryGetProcessor<'a>([<Out>] value:IModelHypertextProcessor<'a> byref) =
            if _dict.IsValueCreated then
                let res, boxedVal = _dict.Value.TryGetValue typeof<'a>
                if res then
                    value <- boxedVal :?> IModelHypertextProcessor<'a>
                    true
                else
                    false
            else 
                false

    module AcceptHeaderParser = 
        begin
            [<LiteralAttribute>]
            let All = "*"
            
            type AcceptHeaderInfo = {
                media : string;
                sub : string;
                quality : float32;
                level : int;
                charset : string;
                // tokens : (string*string) seq;
                mutable valueCache : int;
            } with
                member x.HasWildcard = x.media = All || x.sub = All 
                member x.MediaType = 
                    let v = x.media + "/" + x.sub
                    v
                member x.ValAsInt() = 
                    if x.valueCache = 0 then 
                        // media/sub quality level charset
                        // 1 bit
                        //       1 bit
                        //            2 bytes?
                        //                   1 byte
                        //                          1 bit
                        let v : int = 
                            let v1 = if x.media = All then 0uy else 1uy
                            let v2 = if x.sub   = All then 0uy else 1uy
                            let v3 = if x.quality = 1.0f then 0xFFFF 
                                     elif x.quality = 0.0f then 0x0000
                                     else int(x.quality * 100.0f)
                            let v4 = if x.charset <> null then 1uy else 0uy
                            // let v5 = if not <| Seq.isEmpty x.tokens then 1uy else 0uy
                            int((byte) v1 >>> 8) + int((byte) v2 >>> 8) + int((byte) v3 >>> 8) + int((byte) v4 >>> 8) // + (int)v5
                        x.valueCache <- v
                    x.valueCache
                member internal x.Rate(mediaType:string) = 
                    if x.MediaType === mediaType then 
                        x.ValAsInt()
                    elif x.HasWildcard then
                        let pieces = mediaType.Split([|'/'|], 2)
                        if x.media <> All && x.media === pieces.[0] then 
                            int(float(x.ValAsInt()) * 0.5)
                        elif x.media = All then 
                            int(float(x.ValAsInt()) * 0.3)
                        else 0
                    else 0

            let isAsciiIdStart c =
                isAsciiLetter c || c = '*'
            let isAsciiIdContinue c =
                isAsciiLetter c || isDigit c || c = '*' || c = '+' || c = '-'
            let ws      = spaces
            let pc c    = ws >>. pchar c
            let pval    = ws >>. many1Chars (noneOf ",;=") 
            let pid     = ws >>. identifier (IdentifierOptions(isAsciiIdStart = isAsciiIdStart, 
                                                        isAsciiIdContinue = isAsciiIdContinue))
            let t_q     = pstringCI "q"        >>. pc '=' >>. pval  |>> fun v -> ("Q", v)
            let t_chars = pstringCI "charset"  >>. pc '=' >>. pval  |>> fun v -> ("C", v)
            let t_level = pstringCI "level"    >>. pc '=' >>. pval  |>> fun v -> ("L", v)
            let t_arb   = pid                 .>>  pc '=' .>>. pval |>> fun (param, v) -> (param, v)
            let token   = ws >>. choice [ t_q; t_chars; t_level ; t_arb ]
            let media   = pid .>> pc '/' .>>. pid |>> fun m -> (fst m, snd m)
            let tokens  = pchar ';' >>. sepBy token (pchar ';') 
            let term    = 
                // todo: some sort of soft caching
                media .>>. opt ( tokens ) .>> eof 
                |>> fun ((m,s),l) -> let level = ref 0
                                     let cs : Ref<string> = ref null
                                     let quality = ref 1.0f
                                     if l.IsSome then
                                         l.Value |> List.iter (fun (id,v) -> 
                                                            match id with 
                                                            | "Q" -> quality := Single.Parse(v)
                                                            | "L" -> level := Int32.Parse(v)
                                                            | "C" -> cs := v
                                                            | _   -> ()
                                                            // | _ -> tokens.Add (id, v) 
                                                        )
                                     { media = m; sub = s; quality = !quality; level = !level; 
                                       charset = !cs; (*tokens = tokens :> _ seq;*) valueCache = 0 }

            let parse (accept:string []) = 
                let sort (l:AcceptHeaderInfo) (r:AcceptHeaderInfo) = 
                    r.ValAsInt() - l.ValAsInt()
                let values = 
                    accept |> Array.map (fun ac -> 
                                match run term ac with 
                                | Success(result, _, _) -> result 
                                | Failure(errorMsg, _, _) -> (raise(ArgumentException(errorMsg))))
                values |> Array.sortInPlaceWith sort
                values
        end

    //
    // http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html
    //
    [<System.ComponentModel.Composition.Export;AllowNullLiteral>]
    type ContentNegotiator() = 
        
        (*
        let normalize (acceptHeader:string []) = 
            if acceptHeader = null || acceptHeader.Length = 0 then
                MediaTypes.Html
            else
                let app, text  = 
                    acceptHeader
                    |> Seq.map (fun (h:string) -> let parts = h.Split([|'/';';'|])
                                                  (parts.[0], parts.[1]) )
                    |> Seq.toList
                    |> List.partition (fun (t1:string,t2:string) -> t1 = "application")

                if not (List.isEmpty app) then
                    let _, firstapp = app.Head 
                    match firstapp with 
                    | "json" -> MediaTypes.JSon
                    | "xml" -> MediaTypes.Xml
                    | "atom+xml" -> MediaTypes.Atom
                    | "rss+xml" -> MediaTypes.Rss
                    | "javascript" | "js" -> MediaTypes.Js
                    | "soap+xml" -> MediaTypes.Soap
                    | "xhtml+xml" -> MediaTypes.XHtml
                    | "x-www-form-urlencoded" -> MediaTypes.FormUrlEncoded
                    // | "soap+xml" -> Js
                    | _ -> null
                elif not (List.isEmpty text) then
                    match text.Head with 
                    | _,"xml" -> MediaTypes.Xml
                    | _,"html" -> MediaTypes.Html
                    | _,"javascript" -> MediaTypes.Js
                    | "multipart","form-data" -> MediaTypes.FormUrlEncoded // http://www.w3.org/Protocols/rfc1341/7_2_Multipart.html
                    | _ -> null
                    // csv
                else MediaTypes.Html
        *)

        member x.ResolveBestContentType (accept:string[], supportedMediaTypes:string[]) = 
            if supportedMediaTypes = null || supportedMediaTypes.Length = 0 
            then raise(ArgumentException("Must specify at least one supported media type", "supportedMediaTypes"))
            // if not accept is sent, it's assumed it's */*
            if accept = null || accept.Length = 0 then Seq.head supportedMediaTypes
            else 
                let supportedReversed = supportedMediaTypes |> Array.rev
                let find_best_compatible (accepts:AcceptHeaderParser.AcceptHeaderInfo[]) = 
                    let table = 
                        accepts 
                        |> Array.collect (fun a -> supportedReversed |> Array.map (fun s -> a.Rate s, s)) 
                        |> Array.filter (fun tup -> fst tup > 0)
                    if Array.isEmpty table 
                    then null
                    else
                        table |> Array.sortInPlaceBy (fun i -> -fst i)
                        snd (Array.get table 0)

                let parsedAccepts = AcceptHeaderParser.parse accept
                find_best_compatible parsedAccepts

        member x.ResolveBestContentType (overridingFormat:string, accept:string[], supportedMediaTypes:string[]) = 
            // let r, format = route.RouteParams.TryGetValue "format"

            let effectiveAccept : string[] = 
                if not <| String.IsNullOrEmpty overridingFormat then 
                    // todo: this should delegate to a service provided by the app, so it can be extended. 
                    let acc = 
                        match overridingFormat.ToLowerInvariant() with
                        | "html" -> MediaTypes.Html
                        | "json" -> MediaTypes.JSon
                        | "rss"  -> MediaTypes.Rss
                        | "js"   -> MediaTypes.Js
                        | "atom" -> MediaTypes.Atom
                        | "xml"  -> MediaTypes.Xml
                        | _ -> failwithf "Unknown overriding format %s" overridingFormat
                    [|acc|]
                else accept

            x.ResolveBestContentType (effectiveAccept, supportedMediaTypes)

        
        member x.NormalizeRequestContentType (contentType:string) = 
            if String.IsNullOrEmpty contentType then raise (ArgumentNullException("contentType"))
            
            let parsedAccepts = AcceptHeaderParser.parse [|contentType|]

            parsedAccepts.[0].MediaType



        (*
        member x.ResolveRequestedMediaType (route:RouteMatch) (request:HttpRequestBase) = 
            let r, format = route.RouteParams.TryGetValue "format"
            if r then 
                // todo: this should delegate to a service provided by the app, so it can be extended. 
                match format with
                | "html" -> MediaTypes.Html
                | "json" -> MediaTypes.JSon
                | "rss"  -> MediaTypes.Rss
                | "js"   -> MediaTypes.Js
                | "atom" -> MediaTypes.Atom
                | "xml"  -> MediaTypes.Xml
                | _ -> failwithf "Unknown format %s" format
            else
                let accept_header = request.AcceptTypes
                let media = acceptheader_to_mediatype accept_header 
                if media = null 
                then failwith "Unknown format in accept header"  
                else media

        *)
