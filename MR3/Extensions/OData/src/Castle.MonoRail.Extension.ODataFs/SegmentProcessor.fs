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
open System.IO
open System.Linq
open System.Linq.Expressions
open System.Collections
open System.Collections.Generic
open System.Collections.Specialized
open System.Data.OData
open System.Data.Services.Providers
open System.ServiceModel.Syndication
open System.Text
open System.Xml
open System.Xml.Linq
open Castle.MonoRail

// http://msdn.microsoft.com/en-us/library/dd233205.aspx

type SegmentOp = 
    | View = 0 
    | Create = 1
    | Update = 3
    | Delete = 4
    // | Merge = 5

type ProcessorCallbacks = {
    authorize : Func<ResourceType, (Type * obj) seq, obj, bool>;
    authorizeMany : Func<ResourceType, (Type * obj) seq, IEnumerable, bool>;
    view   : Func<ResourceType, (Type * obj) seq, obj, bool>;
    viewMany : Func<ResourceType, (Type * obj) seq, IEnumerable, bool>;
    create : Func<ResourceType, (Type * obj) seq, obj, bool>;
    update : Func<ResourceType, (Type * obj) seq, obj, bool>;
    remove : Func<ResourceType, (Type * obj) seq, obj, bool>;
    operation : Action<ResourceType, (Type * obj) seq, string>;
    negotiateContent : Func<bool, string>;
} with
    member x.Auth   (rt, parameters, item) = x.authorize.Invoke(rt, parameters, item) 
    member x.Auth   (rt, parameters, item) = x.authorizeMany.Invoke(rt, parameters, item) 
    member x.View   (rt, parameters, item) = x.view.Invoke(rt, parameters, item) 
    member x.View   (rt, parameters, item) = x.viewMany.Invoke(rt, parameters, item) 
    member x.Create (rt, parameters, item) = x.create.Invoke(rt, parameters, item)
    member x.Update (rt, parameters, item) = x.update.Invoke(rt, parameters, item)
    member x.Remove (rt, parameters, item) = x.remove.Invoke(rt, parameters, item)
    member x.Operation (rt, parameters, action) = x.operation.Invoke(rt, parameters, action)

type RequestParameters = {
    model : ODataModel;
    provider : IDataServiceMetadataProvider;
    wrapper : DataServiceMetadataProviderWrapper;
    contentType: string;
    contentEncoding : Encoding;
    input: Stream;
    baseUri : Uri;
    accept: string[];
}

type ResponseParameters = {
    mutable contentType: string;
    mutable contentEncoding : Encoding;
    writer : TextWriter;
    mutable httpStatus : int;
    mutable httpStatusDesc : string;
    mutable location : string;
}

module SegmentProcessor = 
    begin
        type ResponseParameters with 
            member x.SetStatus(code:int, desc:string) = 
                x.httpStatus <- code
                x.httpStatusDesc <- desc
                
        let internal emptyResponse = { QItems = null; SingleResult = null; ResType = null; FinalResourceUri=null; ResProp = null }

        let (|HttpGet|HttpPost|HttpPut|HttpDelete|HttpMerge|HttpHead|) (arg:string) = 
            match arg.ToUpperInvariant() with 
            | "POST"  -> HttpPost
            | "PUT"   -> HttpPut
            | "MERGE" -> HttpMerge
            | "HEAD"  -> HttpHead
            | "DELETE"-> HttpDelete
            | "GET"   -> HttpGet
            | _ -> failwithf "Could not understand method %s" arg
            

        let private assert_entitytype_without_entityset op (rt:ResourceType) (model:ODataModel) = 
            if rt.ResourceTypeKind <> ResourceTypeKind.EntityType then 
                failwithf "Unsupported operation %O" op
            match model.GetRelatedResourceSet(rt) with
            | Some rs -> failwithf "Unsupported operation %O" op
            | _ -> ()


        let internal serialize_result (reply:ResponseToSend) (request:RequestParameters) (response:ResponseParameters) (containerUri:Uri) = 
            let s = SerializerFactory.Create(response.contentType) 
            let wrapper = request.wrapper

            s.Serialize(reply, wrapper, request.baseUri, containerUri, response.writer, response.contentEncoding)

        let internal deserialize_input (rt:ResourceType) (request:RequestParameters) = 
            let s = DeserializerFactory.Create(request.contentType)

            s.DeserializeSingle (rt, new StreamReader(request.input), request.contentEncoding)
            
        let internal get_property_value (container:obj) (property:ResourceProperty) = 
            // super weak
            System.Diagnostics.Debug.Assert (container <> null)
            let containerType = container.GetType()
            let getproperty = containerType.GetProperty(property.Name)
            System.Diagnostics.Debug.Assert (getproperty <> null)
            let value = getproperty.GetValue(container, null)
            value


        let internal process_collection_property op container (p:PropertyAccessInfo) (previous:UriSegment) hasMoreSegments 
                                                 (model:ODataModel) (callbacks:ProcessorCallbacks) 
                                                 (request:RequestParameters) (response:ResponseParameters) parameters
                                                 (shouldContinue:Ref<bool>) =  
            System.Diagnostics.Debug.Assert ((match previous with | UriSegment.Nothing -> false | _ -> true), "cannot be root")

            if op = SegmentOp.View || (hasMoreSegments && op = SegmentOp.Update) then
                let value = (get_property_value container p.Property ) :?> IEnumerable
                // if callbacks.accessMany.Invoke(p.ResourceType, value) then 
                p.ManyResult <- value 
                { ResType = p.ResourceType; 
                    QItems = value.AsQueryable(); SingleResult = null; 
                    FinalResourceUri = p.Uri; ResProp = p.Property }
                // else emptyResponse

            else
                match op with 
                | SegmentOp.Create -> 
                    
                    assert_entitytype_without_entityset op p.ResourceType model

                    let input = deserialize_input p.ResourceType request

                    let succ= callbacks.create.Invoke(p.ResourceType, parameters, input)
                    if succ then
                        response.SetStatus(201, "Created")
                        // we dont have enough data to build it
                        // response.location <- Uri(request.baseUri, p.Uri.OriginalString + "(" + key + ")").AbsoluteUri
                        
                        p.SingleResult <- input

                        { ResType = p.ResourceType; 
                          QItems = null; SingleResult = input; 
                          FinalResourceUri = p.Uri; ResProp = null }
                    else 
                        shouldContinue := false
                        emptyResponse

                | _ -> failwithf "Unsupported operation %O" op


        let internal process_item_property op container (p:PropertyAccessInfo) (previous:UriSegment) hasMoreSegments 
                                           (model:ODataModel) (callbacks:ProcessorCallbacks) (shouldContinue:Ref<bool>) 
                                           (requestParams:RequestParameters) (response:ResponseParameters) parameters =   
            System.Diagnostics.Debug.Assert ((match previous with | UriSegment.Nothing -> false | _ -> true), "cannot be root")

            let auth_item (item:obj) = 
                let succ = callbacks.Auth(p.ResourceType, parameters, item) 
                if not succ then shouldContinue := false
                succ

            let get_property_value () = 
                let propValue = get_property_value container p.Property
                let finalVal = 
                    if p.Key <> null then
                        let collAsQueryable = (propValue :?> IEnumerable).AsQueryable()
                        let value = AstLinqTranslator.select_by_key p.ResourceType collAsQueryable p.Key 
                        value
                    else propValue
                if auth_item finalVal 
                then finalVal
                else null

            if op = SegmentOp.View || hasMoreSegments then

                let singleResult = get_property_value ()

                if singleResult <> null then
                    if not hasMoreSegments && not <| callbacks.View(p.ResourceType, parameters, singleResult) then
                        shouldContinue := false
                else
                    shouldContinue := false
                    
                if !shouldContinue then
                    p.SingleResult <- singleResult
                    { ResType = p.ResourceType; 
                        QItems = null; SingleResult = singleResult; 
                        FinalResourceUri = p.Uri; ResProp = p.Property }
                else emptyResponse

            else
                System.Diagnostics.Debug.Assert (not hasMoreSegments)

                match op with
                | SegmentOp.Update -> 

                    if p.Property.IsOfKind(ResourcePropertyKind.Primitive) then 
                        // if primitive... 
                        raise(NotImplementedException("Update for property is not supported yet"))
                    
                    elif p.Property.IsOfKind(ResourcePropertyKind.ResourceSetReference) || 
                         p.Property.IsOfKind(ResourcePropertyKind.ResourceReference) then 
                        
                        // only supported for the case below, otherwise one should use $link instead
                        assert_entitytype_without_entityset op p.ResourceType model 

                        let finalValue = get_property_value ()

                        if callbacks.update.Invoke(p.ResourceType, parameters, finalValue) then 
                            response.SetStatus(204, "No Content")
                        
                        emptyResponse
                    
                    else failwithf "Operation not supported for this entity type"
                    
                | SegmentOp.Delete -> 

                    if p.Property.IsOfKind(ResourcePropertyKind.Primitive) then 
                        failwithf "Cannot delete a primitive value in a property"
                    
                    elif p.Property.IsOfKind(ResourcePropertyKind.ResourceSetReference) || 
                         p.Property.IsOfKind(ResourcePropertyKind.ResourceReference) then 
                        
                        // only supported for the case below, otherwise one should use $link instead
                        assert_entitytype_without_entityset op p.ResourceType model 

                        let finalValue = get_property_value ()

                        if callbacks.remove.Invoke(p.ResourceType, parameters, finalValue) then 
                            response.SetStatus(204, "No Content")

                        emptyResponse
                    
                    else failwithf "Operation not supported for this resource type"

                | _ -> failwithf "Operation not supported for this resource type"


        let internal process_entityset op (d:EntityAccessInfo) (previous:UriSegment) hasMoreSegments 
                                       (model:ODataModel) (callbacks:ProcessorCallbacks) (shouldContinue:Ref<bool>) 
                                       (request:RequestParameters) (response:ResponseParameters) parameters = 
            System.Diagnostics.Debug.Assert ((match previous with | UriSegment.Nothing -> true | _ -> false), "must be root")

            let get_values () = 
                let value = model.GetQueryable (d.ResSet)
                if not <| callbacks.Auth(d.ResourceType, parameters, value) then 
                    shouldContinue := false; null
                else value

            match op with 
            | SegmentOp.View ->
                // acceptable next segments: $count, $orderby, $top, $skip, $format, $inlinecount
                
                let values = get_values ()
                d.ManyResult <- values

                if values <> null then
                    if not hasMoreSegments && not <| callbacks.View( d.ResourceType, parameters, values ) then
                        shouldContinue := false

                // remember: this ! is not NOT, it's a de-ref
                if !shouldContinue then
                    { ResType = d.ResourceType; QItems = values; SingleResult = null; FinalResourceUri = d.Uri; ResProp = null }
                else emptyResponse 


            | SegmentOp.Create -> 
                System.Diagnostics.Debug.Assert (not hasMoreSegments)

                let item = deserialize_input d.ResourceType request

                let succ = callbacks.Create(d.ResourceType, parameters, item)
                if succ then
                    response.SetStatus(201, "Created")
                    // not enough info to build location
                    // response.location <- Uri(request.baseUri, d.Uri.OriginalString + "(" + key + ")").AbsoluteUri

                    { ResType = d.ResourceType; 
                      QItems = null; SingleResult = item; 
                      FinalResourceUri = d.Uri; ResProp = null }
                else 
                    shouldContinue := false
                    emptyResponse

            | _ -> failwithf "Unsupported operation %O" op
            
        
        let internal process_entityset_single op (d:EntityAccessInfo) (previous:UriSegment) hasMoreSegments 
                                              (model:ODataModel) (callbacks:ProcessorCallbacks) (shouldContinue:Ref<bool>) 
                                              (request:RequestParameters) (response:ResponseParameters) parameters = 
            System.Diagnostics.Debug.Assert ((match previous with | UriSegment.Nothing -> true | _ -> false), "must be root")

            let auth_item (item:obj) = 
                let succ = callbacks.Auth(d.ResourceType, parameters, item) 
                if not succ then shouldContinue := false
                succ

            let get_single_result () = 
                let wholeSet = model.GetQueryable (d.ResSet)
                let singleResult = AstLinqTranslator.select_by_key d.ResourceType wholeSet d.Key
                if auth_item singleResult
                then singleResult
                else null

            if op = SegmentOp.View || hasMoreSegments then
                if not hasMoreSegments then Diagnostics.Debug.Assert (not (op = SegmentOp.Delete), "should not be delete")

                let singleResult = get_single_result ()

                d.SingleResult <- singleResult

                if singleResult <> null then
                    if not hasMoreSegments && not <| callbacks.View(d.ResourceType, parameters, singleResult) then
                        shouldContinue := false
                else
                    shouldContinue := false
                    
                if !shouldContinue then
                    { ResType = d.ResourceType; QItems = null; SingleResult = singleResult; FinalResourceUri = d.Uri; ResProp = null }
                else emptyResponse

            else 
                match op with 
                | SegmentOp.Update -> 
                    // runs auth
                    let single = get_single_result()
                    if single <> null then 
                        // todo: shouldn't it deserialize into 'single'?
                        let item = deserialize_input d.ResourceType request
                        let succ = callbacks.Update(d.ResourceType, parameters, item)
                        if succ 
                        then response.SetStatus(204, "No Content")
                        else shouldContinue := false

                | SegmentOp.Delete -> 
                    // http://www.odata.org/developers/protocols/operations#DeletingEntries
                    // Entries are deleted by executing an HTTP DELETE request against a URI that points at the Entry. 
                    // If the operation executed successfully servers should return 200 (OK) with no response body.
                    let single = get_single_result()
                    if single <> null then 
                        if callbacks.Remove(d.ResourceType, parameters, single) then 
                            response.SetStatus(204, "No Content")
                        else shouldContinue := false

                | _ -> failwithf "Unsupported operation %O at this level" op
                emptyResponse
        

        let internal serialize_directory op hasMoreSegments (previous:UriSegment) writer baseUri metadataProviderWrapper (response:ResponseParameters) = 
            System.Diagnostics.Debug.Assert ((match previous with | UriSegment.Nothing -> true | _ -> false), "must be root")
            System.Diagnostics.Debug.Assert (not hasMoreSegments, "needs to be the only segment")
            
            match op with 
            | SegmentOp.View ->
                response.contentType <- "application/xml;charset=utf-8"
                AtomServiceDocSerializer.serialize (writer, baseUri, metadataProviderWrapper, response.contentEncoding)
            | _ -> failwithf "Unsupported operation %O at this level" op


        let internal serialize_metadata op (previous:UriSegment) writer baseUri metadataProviderWrapper (response:ResponseParameters) = 
            System.Diagnostics.Debug.Assert ((match previous with | UriSegment.Nothing -> true | _ -> false), "must be root")

            match op with 
            | SegmentOp.View ->
                response.contentType <- "application/xml;charset=utf-8"
                MetadataSerializer.serialize (writer, metadataProviderWrapper, response.contentEncoding)
            | _ -> failwithf "Unsupported operation %O at this level" op


        let private process_operation_value (previous:UriSegment) (result:ResponseToSend) (response:ResponseParameters) = 
            if result = emptyResponse || result.SingleResult = null 
               || result.ResProp = null 
               || not <| result.ResProp.IsOfKind(ResourcePropertyKind.Primitive) then 
                raise(InvalidOperationException("$value can only operate if a previous segment produced a primitive value"))
            
            // change the response type
            response.contentType <- "text/plain"
            
            // return the exact same result as the previous
            result

        let private apply_filter (response:ResponseToSend) (rawExpression:string) = 
            let ast = QueryExpressionParser.parse_filter rawExpression
            let typedAst = QuerySemanticAnalysis.analyze_and_convert ast response.ResType

            if response.QItems <> null then 
                response.QItems <- AstLinqTranslator.apply_queryable_filter response.ResType response.QItems typedAst :?> IQueryable
            
        let private apply_orderby (response:ResponseToSend) (rawExpression:string) = 
            let exps = QueryExpressionParser.parse_orderby rawExpression
            let typedNodes = QuerySemanticAnalysis.analyze_and_convert_orderby exps response.ResType

            if response.QItems <> null then 
                response.QItems <- AstLinqTranslator.apply_queryable_orderby response.ResType response.QItems typedNodes :?> IQueryable
            

        let public Process (op:SegmentOp) 
                           (segments:UriSegment[]) (meta:MetaSegment) (metaQueries:MetaQuerySegment[])
                           (ordinaryParams:NameValueCollection) 
                           (callbacks:ProcessorCallbacks) 
                           (request:RequestParameters) (response:ResponseParameters) = 
            
            // missing support for operations, value, filters, links, batch, ...

            // binds segments, delegating to SubController if they exist. 
            // for post, put, delete, merge
            //   - deserialize
            //   - process
            // for get operations
            //   - serializes results 
            // in case of exception, serialized error is sent

            let model = request.model
            let baseUri = request.baseUri
            let writer = response.writer
            let parameters = List<Type * obj>()
            let lastSegment = segments.[segments.Length - 1]

            let rec rec_process (index:int) (previous:UriSegment) (result:ResponseToSend) =
                let shouldContinue = ref true

                if index < segments.Length then
                    let container, prevRt, containerUri = 
                        match previous with 
                        | UriSegment.EntityType d -> d.SingleResult, d.ResourceType, d.Uri
                        | UriSegment.ComplexType d 
                        | UriSegment.PropertyAccessSingle d -> d.SingleResult, d.ResourceType, d.Uri
                        | _ -> null, null, null
                    
                    // builds list of contextual parameters. used when calling back controllers
                    if container <> null then parameters.Add (prevRt.InstanceType, container)

                    let hasMoreSegments = index + 1 < segments.Length
                    let segment = segments.[index]

                    let toSerialize = 
                        match segment with 
                        | UriSegment.ServiceDirectory -> 
                            serialize_directory op hasMoreSegments previous writer baseUri request.wrapper response
                            emptyResponse

                        | UriSegment.ActionOperation actionOp -> 
                            callbacks.Operation(actionOp.ResourceType, parameters, actionOp.Name)
                            // it's understood that the action took care of the result
                            emptyResponse
                            // shouldContinue := false

                        | UriSegment.RootServiceOperation -> 
                            emptyResponse

                        | UriSegment.EntitySet d -> 
                            process_entityset op d previous hasMoreSegments model callbacks shouldContinue request response parameters

                        | UriSegment.EntityType d -> 
                            process_entityset_single op d previous hasMoreSegments model callbacks shouldContinue request response parameters

                        | UriSegment.PropertyAccessCollection d -> 
                            process_collection_property op container d previous hasMoreSegments model callbacks request response parameters shouldContinue 

                        | UriSegment.ComplexType d | UriSegment.PropertyAccessSingle d -> 
                            process_item_property op container d previous hasMoreSegments model callbacks shouldContinue request response parameters

                        | _ -> Unchecked.defaultof<ResponseToSend>

                    if !shouldContinue 
                    then rec_process (index+1) segment toSerialize 
                    else result

                else result

            let result = 
                // process segments recursively. 
                let navResult = rec_process 0 UriSegment.Nothing emptyResponse 
            
                match meta with 
                | MetaSegment.Nothing ->  
                    navResult
                | MetaSegment.Metadata -> 
                    serialize_metadata op lastSegment writer baseUri request.wrapper response
                    emptyResponse
                | MetaSegment.Value -> 
                    process_operation_value lastSegment navResult response
                | _ -> failwithf "Unsupported meta instruction %O" meta

            for metaQuery in metaQueries do
                match metaQuery with 
                | MetaQuerySegment.Filter exp ->
                    apply_filter result exp
                | MetaQuerySegment.OrderBy exp ->
                    apply_orderby result exp
                | MetaQuerySegment.Expand exp ->
                    ()
                | MetaQuerySegment.InlineCount cf ->
                    ()
                | MetaQuerySegment.Format fmt ->
                    ()
                | MetaQuerySegment.Select exp ->
                    ()
                | MetaQuerySegment.Skip howMany ->
                    ()
                | MetaQuerySegment.Top count ->
                    ()
                | _ -> failwithf "Unsupported metaQuery instruction %O" metaQuery

            // we ultimately need to serialize a result back
            if result <> emptyResponse then 
                if response.contentType = null then 
                    response.contentType <- callbacks.negotiateContent.Invoke( result.SingleResult <> null ) // segments request.accept
                serialize_result result request response result.FinalResourceUri 

    end

