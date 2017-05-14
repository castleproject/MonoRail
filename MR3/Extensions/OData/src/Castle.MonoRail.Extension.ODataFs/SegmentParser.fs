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

namespace Castle.MonoRail.Extension.OData

open System
open System.Collections
open System.Collections.Specialized
open System.Collections.Generic
open System.Data.OData
open System.Data.Services.Providers
open System.Linq
open System.Linq.Expressions
open System.Web
open Castle.MonoRail

type EntityAccessInfo = {
    RawPathSegment : string;
    Uri : Uri;
    mutable ManyResult : IQueryable;
    mutable SingleResult : obj;
    ResSet : ResourceSet;
    ResourceType : ResourceType;
    Name : string; 
    Key : string;
}

type PropertyAccessInfo = {
    RawPathSegment : string;
    Uri : Uri;
    mutable ManyResult : IEnumerable;
    mutable SingleResult : obj;
    ResourceType : ResourceType; 
    Property : ResourceProperty;
    Key : string
}

type MetaSegment = 
    | Nothing
    | Metadata 
    | Count
    | Value
    // | Links // of UriSegment[]
    | Batch

and MetaQuerySegment = 
    | Nothing
    | Format of string
    | Skip of int
    | Top of int
    | OrderBy of string
    | Expand of string
    | Select of string
    | InlineCount of string
    | Filter of string
 
and UriSegment = 
    | Nothing
    // | Links
    | ServiceDirectory 
    | EntitySet of EntityAccessInfo
    | EntityType of EntityAccessInfo
    | ComplexType of PropertyAccessInfo
    | PropertyAccessSingle of PropertyAccessInfo
    | PropertyAccessCollection of PropertyAccessInfo
    | RootServiceOperation 
    | ActionOperation of ControllerActionOperation


(*
    Uri Syntax spec. See ms-odata.pdf, section 2.2.3.1
    ==================================================
 
serviceRoot = *( "/" segment-nz ) ; section 3.3 of [RFC3986]
                                  ; segment-nz = the non empty sequence of characters
                                  ; outside the set of URI reserved
                                  ; characters as specified in [RFC3986]

pathPrefix = *( "/" segment-nz )  ; zero or more URI path segments

resourcePath = "/"
                ( ([entityContainer "."] entitySet)
                  / serviceOperation-collEt
                    [ paren ] [ navPath ] [ count ]
                )
                / serviceOperation

paren = "()"

serviceOperation = serviceOperation-et
                   / serviceOperation-collCt
                   / serviceOperation-ct
                   / serviceOperation-collPrim
                   / serviceOperation-prim [ value ]

count = "/$count"

navPath         = "("keyPredicate")" [navPath-options]

navPath-options = [ navPath-np / propertyPath / propertyPath-ct / value ]

navPath-np = "/"
             ( ("$links" / entityNavProperty )
               / (entityNavProperty-es [ paren ] [ navPath ])
               / (entityNavProperty-et [ navPath-options ])
             )

entityNavProperty = (entityNavProperty-es [ paren ])
                    / entityNavProperty-et

propertyPath =      "/" entityProperty [ value ]
propertyPath-ct =   1 * ("/" entityComplexProperty) [ propertyPath ]

keyPredicate = keyPredicate-single
               / keyPredicate-cmplx

keyPredicate-single = 1*DIGIT ; section B.1 of [RFC5234]
                      / ([1*unreserved] "’" 1*unreserved "’") ; section 2.3 of [RFC3986]
                      / 1*(HEXDIG HEXDIG)) ; section B.1 of [RFC5234]

keyPredicate-cmplx = entityProperty "=" keyPredicate-single
                     ["," keyPredicate-cmplx]

value = "/$value"

queryOptions = sysQueryOption ; see section 2.2.3.6.1
                / customQueryOption ; section 2.2.3.6.2
                / serviceOpParam ; see section 2.2.3.6.3
                *("&"(sysQueryOption / serviceOpParam / customQueryOption))

sysQueryOption = expandQueryOp
                / filterQueryOp
                / orderbyQueryOp
                / skipQueryOp
                / topQueryOp
                / formatQueryOp
                / countQueryOp
                / selectQueryOp
                / skiptokenQueryOp

customQueryOption = *pchar ; section 3.3 of [RFC3986]
expandQueryOp = ; see section 2.2.3.6.1.3
filterQueryOp = ; see section 2.2.3.6.1.4
orderbyQueryOp = ; see section 2.2.3.6.1.6
skipQueryOp = ; see section 2.2.3.6.1.7
serviceOpParam = ; see section 2.2.3.6.3
topQueryOp = ; see section 2.2.3.6.1.8
formatQueryOp = ; see section 2.2.3.6.1.5
countQueryOp = ; see section 2.2.3.6.1.10
selectQueryOp = ; see section 2.2.3.6.1.11
skiptokenQueryOp = ; see section 2.2.3.6.1.9

;Note: The semantic meaning, relationship to Entity Data Model
; (EDM) constructs and additional URI construction
; constraints for the following grammar rules are further
; defined in (section 2.2.3.4) and (section 2.2.3.5)
; See [MC-CSDL] for further scoping rules regarding the value
; of each of the rules below

entityContainer = *pchar ; section 3.3 of [RFC3986]
                        ; the name of an Entity Container in the EDM model

entitySet = *pchar ; section 3.3 of [RFC3986]
                    ; the name of an Entity Set in the EDM model

entityType = *pchar ; section 3.3 of [RFC3986]
                    ; the name of an Entity Type in the EDM model

entityProperty = *pchar ; section 3.3 of [RFC3986]
                        ; the name of a property (of type EDMSimpleType) on an
                        ; Entity Type in the EDM
                        ; model associated with the data service

entityComplexProperty = *pchar ; section 3.3 of [RFC3986]
                                ; the name of a property (of type ComplexType) on an
                                ; Entity Type in the EDM
                                ; model associated with the data service

entityNavProperty-es= *pchar ; section 3.3 of [RFC3986]
                            ; the name of a Navigation Property on an Entity Type in
                            ; the EDM model associated with the data service. The
                            ; Navigation Property MUST identify an Entity Set.

entityNavProperty-et= *pchar ; section 3.3 of [RFC3986]
                            ; the name of a Navigation Property on an Entity Type
                            ; in the EDM model associated with the data service.
                            ; The Navigation Property MUST identify an entity.

serviceOperation-collEt = *pchar ; section 3.3 of [RFC3986]
                                 ; the name of a Function Import in the EDM model which returns a
                                 ; collection of entities from the same Entity Set

serviceOperation-et = *pchar ; section 3.3 of [RFC3986]
                             ; the name of a Function Import which returns a single Entity
                             ; Type instance

serviceOperation-collCt = *pchar ; section 3.3 of [RFC3986]
                                 ; the name of a Function Import which returns a collection of
                                 ; Complex Type [MC-CSDL] instances. Each member of the
                                 ; collection is of the same type.
*)
module SegmentParser =
    begin
        // TODO, change MonoRail to also recognize 
        // "X-HTTP-Method", and gives it the value MERGE, PUT or DELETE.

        let (|Meta|_|) (arg:string) = 
            if arg.StartsWith("$", StringComparison.Ordinal) 
            then match arg.Substring(1).ToLowerInvariant() with 
                 | "metadata" -> Some(MetaSegment.Metadata)
                 | "batch"    -> Some(MetaSegment.Batch)
                 | "count"    -> Some(MetaSegment.Count)
                 | "value"    -> Some(MetaSegment.Value)
                 // | "links"    -> Some(MetaSegment.Links)
                 | _ -> None
            else None

        let (|ResourcePropKind|_|) (arg:ResourcePropertyKind) = 
            if int(arg &&& ResourcePropertyKind.Primitive) <> 0 then 
                Some(ResourcePropertyKind.Primitive)
            elif int(arg &&& ResourcePropertyKind.ComplexType) <> 0 then 
                Some(ResourcePropertyKind.ComplexType)
            elif int(arg &&& ResourcePropertyKind.ResourceReference) <> 0 then 
                Some(ResourcePropertyKind.ResourceReference)
            elif int(arg &&& ResourcePropertyKind.ResourceSetReference) <> 0 then 
                Some(ResourcePropertyKind.ResourceSetReference)
            else None
                 
        let (|SegmentWithKey|_|) (arg:string) = 
            let ``match`` = Constants.SegmentKeyRegex.Match(arg)
            if ``match``.Success && ``match``.Groups.Count = 3 then
                let name = ``match``.Groups.[1].Captures.[0].Value
                let id = ``match``.Groups.[2].Captures.[0].Value.Trim([|'(';')'|])
                Some(name,id)
            else None

        let (|SegmentWithoutKey|_|) (arg:string) = 
            if arg.IndexOfAny([|'$';'('|]) = -1 
            then Some(arg)
            else None

        let (|RootOperationAccess|_|) (model:ODataModel) (arg:string) =  
            None

        let (|OperationAccess|_|) (model:ODataModel) (rt:ResourceType option) (arg:string) =  
            if rt.IsNone then None
            else
                let op = model.GetNestedOperation(rt.Value, arg)
                if op = null 
                then None
                else Some(op)

        let (|PropertyAccess|_|) (rt:ResourceType option) (arg:string) = 
            let name, key = 
                match arg with 
                | SegmentWithKey (name, key) -> (name,key)
                | _ -> arg, null
            match rt with
            | Some r -> 
                match r.Properties |> Seq.tryFind (fun p -> p.Name = name) with
                | Some prop -> Some(prop, key)
                | _ -> None
            | _ -> None

        let (|EntityTypeAccess|_|) (model:ODataModel) (arg:string)  =  
            match arg with 
            | SegmentWithKey (name, key) -> 
                match model.GetResourceSet(name) with 
                | Some rt -> Some(rt, name, key)
                | _ -> None
            | _ -> None
            
        let (|EntitySetAccess|_|) (model:ODataModel) (arg:string)  =  
            match arg with 
            | SegmentWithoutKey name -> 
                match model.GetResourceSet(name) with 
                | Some rs -> Some(rs, name)
                | _ -> None
            | _ -> None

        let internal process_first (firstSeg:string) model (svcUri:Uri) (resourceType:Ref<ResourceType>) (meta:Ref<MetaSegment>) = 
            match firstSeg with 
            | "" -> 
                UriSegment.ServiceDirectory
            | Meta m -> // todo: semantic validation
                Diagnostics.Debug.Assert (!meta = MetaSegment.Nothing)
                meta := m
                UriSegment.Nothing
                
            | RootOperationAccess model o -> 
                UriSegment.RootServiceOperation

            // todo: support for:
            // | OperationAccess within ResourceType
                
            | EntitySetAccess model (rs, name) -> 
                resourceType := rs.ResourceType 
                UriSegment.EntitySet({ Uri=Uri(svcUri, name); RawPathSegment=firstSeg; ResSet = rs; 
                                        ResourceType = !resourceType; Name = name; Key = null; 
                                        SingleResult = null; ManyResult = null; })
                
            | EntityTypeAccess model (rs, name, key) -> 
                resourceType := rs.ResourceType
                UriSegment.EntityType({ Uri=Uri(svcUri, firstSeg); RawPathSegment=firstSeg; ResSet = rs; 
                                        ResourceType = !resourceType; Name = name; Key = key; 
                                        SingleResult = null; ManyResult = null; })
                
            | _ -> raise(HttpException(400, "First segment of uri could not be parsed"))


        let internal build_segment_for_property kind (baseUri:Uri) (rawSegment:string) (prop:ResourceProperty) key = 
            match kind with 
            | ResourcePropertyKind.Primitive -> 
                // todo: assert key is null
                let info = { Uri=Uri(baseUri, rawSegment); RawPathSegment=rawSegment; ResourceType=prop.ResourceType; 
                                Property=prop; Key = null; SingleResult = null; ManyResult = null }
                UriSegment.PropertyAccessSingle(info)

            | ResourcePropertyKind.ComplexType -> 
                // todo: assert key is null
                let info = { Uri=Uri(baseUri, rawSegment); RawPathSegment=rawSegment; ResourceType=prop.ResourceType; 
                                Property=prop; Key = null; SingleResult = null; ManyResult = null }
                UriSegment.ComplexType(info)

            | ResourcePropertyKind.ResourceReference -> 
                let info = { Uri=Uri(baseUri, rawSegment); RawPathSegment=rawSegment; ResourceType=prop.ResourceType; 
                                Property=prop; Key = key; SingleResult = null; ManyResult = null }
                UriSegment.PropertyAccessSingle(info)

            | ResourcePropertyKind.ResourceSetReference -> 
                if key = null then
                    let info = { Uri=Uri(baseUri, rawSegment); RawPathSegment=rawSegment; ResourceType=prop.ResourceType; 
                                    Property=prop; Key = null; SingleResult = null; ManyResult = null }
                    UriSegment.PropertyAccessCollection(info)
                else
                    let info = { Uri=Uri(baseUri, rawSegment); RawPathSegment=rawSegment; ResourceType=prop.ResourceType; 
                                    Property=prop; Key = key; SingleResult = null; ManyResult = null }
                    UriSegment.PropertyAccessSingle(info)

            | _ -> raise(HttpException(500, "Unsupported property kind for segment "))

        let partition_qs_parameters (qs:NameValueCollection) = 
            let odataParams, ordinaryParams = 
                let odata, ordinary =
                    qs.AllKeys 
                    |> List.ofSeq |> List.partition (fun k -> k.StartsWith("$", StringComparison.Ordinal))
                let ordinaryParams = NameValueCollection(StringComparer.OrdinalIgnoreCase)
                ordinary |> List.iter (fun i -> ordinaryParams.[i] <- qs.[i])
                let odataparms = NameValueCollection(StringComparer.OrdinalIgnoreCase)
                odata    |> List.iter (fun i -> odataparms.[i] <- qs.[i])
                odataparms, ordinary
            odataParams, ordinaryParams

        // we also need to parse QS 
        // ex url/Suppliers?$filter=Address/City eq 'Redmond' 
        let parse(path:string, qs:NameValueCollection, model:ODataModel, svcUri:Uri) : UriSegment[] * MetaSegment * MetaQuerySegment[] = 

            let odataParams, ordinaryParams = partition_qs_parameters qs

            // tracks the meta we will discover later
            let meta = ref MetaSegment.Nothing

            // tracks the last segment where (is a collection type = true)
            let lastCollAccessSegment : Ref<UriSegment> = ref UriSegment.Nothing

            // this rec function parses all segments but the first
            let rec parse_segment (all:UriSegment list) (previous:UriSegment) 
                                  (contextRT:ResourceType option) (rawSegments:string[]) (index:int) : UriSegment[] = 
                
                if index < rawSegments.Length && (rawSegments.[index] <> String.Empty && index <= rawSegments.Length - 1) then

                    let rawSegment = rawSegments.[index]
                    let resourceType : Ref<ResourceType> = ref null
                    let baseUri = 
                        match previous with 
                        | UriSegment.EntitySet d 
                        | UriSegment.EntityType d -> Uri(d.Uri.AbsoluteUri + "/")
                        | UriSegment.PropertyAccessCollection d 
                        | UriSegment.PropertyAccessSingle d -> Uri(d.Uri.AbsoluteUri + "/")
                        | _ -> svcUri

                    let newSegment = 
                        match rawSegment with
                        | Meta m -> 
                            // todo: semantic validation 
                            // (e.g. at this point, $metadata is not accepted)
                            Diagnostics.Debug.Assert (!meta = MetaSegment.Nothing)
                            meta := m
                            UriSegment.Nothing
                        
                        | OperationAccess model contextRT o -> 
                            UriSegment.ActionOperation(o)

                        | PropertyAccess contextRT (prop, key) -> 
                            resourceType := prop.ResourceType
                            match prop.Kind with 
                            | ResourcePropKind kind -> 
                                build_segment_for_property kind baseUri rawSegment prop key
                            | _ -> raise(HttpException(500, "Unsupported property kind for segment "))

                        | _ -> raise(HttpException(400, "Segment does not match a property or operation"))
                    
                    match newSegment with 
                    | UriSegment.EntitySet _ 
                    | UriSegment.PropertyAccessCollection _ -> 
                        lastCollAccessSegment := newSegment
                    | _ -> ()                    

                    let rt = if !resourceType <> null then Some(!resourceType) else None

                    let newList = 
                        all @ (if newSegment = UriSegment.Nothing then [] else [newSegment])

                    parse_segment newList newSegment rt rawSegments (index + 1)
                
                else all |> Array.ofList

            let normalizedPath = 
                if path.StartsWith("/", StringComparison.Ordinal) 
                then path.Substring(1)
                else path

            let rawSegments = normalizedPath.Split('/')
            let firstSeg = rawSegments.[0]
            let resourceType : Ref<ResourceType> = ref null

            // first segment is a special situation
            let segment = process_first firstSeg model svcUri resourceType meta

            // calls the parser
            let uriSegments = parse_segment [segment] segment (if !resourceType <> null then Some(!resourceType) else None) rawSegments 1 

            // process odata query parameters, if any
            let metaQuerySegments = 
                let list = List<MetaQuerySegment>()
                for key in odataParams.Keys do
                    let value = odataParams.[key]
                    match key with 
                    | "$filter"     -> MetaQuerySegment.Filter (value)
                    | "$orderby"    -> MetaQuerySegment.OrderBy (value)
                    | "$top"        -> MetaQuerySegment.Top (Int32.Parse(value))
                    | "$skip"       -> MetaQuerySegment.Skip (Int32.Parse(value))
                    | "$expand"     -> MetaQuerySegment.Expand (value)
                    | "$format"     -> MetaQuerySegment.Format (value)
                    | "$inlinecount"-> MetaQuerySegment.InlineCount (value)
                    | "$select"     -> MetaQuerySegment.Select (value)
                    | _ -> failwithf "special query parameter is not supported: %s (note that these parameters are case sensitive)" key
                    |> list.Add 
                list |> Array.ofSeq

            uriSegments, !meta, metaQuerySegments

    end

