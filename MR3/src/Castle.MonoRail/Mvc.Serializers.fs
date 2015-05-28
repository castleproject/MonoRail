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

// Implementation of default serializer and resolver hidden in this namespace
namespace Castle.MonoRail.Serialization

    open System
    open System.Collections.Generic
    open System.Collections.Specialized
    open System.IO
    open System.Reflection
    open System.Text
    open System.Web
    open System.ComponentModel.Composition
    open Castle.MonoRail
    open Newtonsoft.Json

    type JsonSerializer<'a>(resolver:IModelSerializerResolver) = 
        static let contentType = "application/json"

        let recursiveConverter (metadataProvider) (prefix) (context) = 
            { new JsonConverter() with 
                override x.CanConvert(contract) = 
                    resolver.HasCustomSerializer(contract, MediaTypes.JSon)
                override x.WriteJson(writer, model, serializer) = 
                    let s = resolver.CreateSerializer(model.GetType(), MediaTypes.JSon)
                    use tempWriter = new StringWriter()
                    s.Serialize(model, contentType, tempWriter, metadataProvider)
                    writer.WriteRaw (tempWriter.GetStringBuilder().ToString())
                    
                override x.ReadJson(reader, contract, model, serializer) = 
                    raise(NotImplementedException("ReadJson"))
                    // let s = resolver.CreateSerializer(contract, MediaType.JSon)
                    // let v = reader.Value
                    // s.Deserialize (prefix, contentType, context, metadataProvider)
            }

        let build_serializer (metadataProvider) (prefix) (context) (recursiveResolution:bool) = 
            let settings = JsonSerializerSettings()
            settings.Converters.Add (Converters.IsoDateTimeConverter())
            if recursiveResolution then 
                settings.Converters.Add (recursiveConverter metadataProvider prefix context)
            Newtonsoft.Json.JsonSerializer.Create(settings)

        interface IModelSerializer<'a> with
            member x.Serialize (model:'a, contentType:string, writer:System.IO.TextWriter, metadataProvider) = 
                // very inneficient for large models
                let serializer = build_serializer metadataProvider "" null true
                serializer.Serialize(writer, model)
                // let content = JsonConvert.SerializeObject(model, [|Converters.IsoDateTimeConverter()|])
                // writer.Write content

            member x.Deserialize (prefix, contentType, context, metadataProvider) = 
                let serializer = build_serializer metadataProvider "" context false
                let reader = new StreamReader(context.InputStream)
                serializer.Deserialize(reader, typeof<'a>) :?> 'a
                

    type XmlSerializer<'a>() = 
        interface IModelSerializer<'a> with
            member x.Serialize (model:'a, contentType:string, writer:System.IO.TextWriter, metadataProvider) = 
                let serial = System.Runtime.Serialization.DataContractSerializer(typeof<'a>)
                let memStream = new MemoryStream()
                serial.WriteObject (memStream, model)
                let en = System.Text.UTF8Encoding()
                let content = en.GetString (memStream.GetBuffer(), 0, int(memStream.Length))
                writer.Write content

            member x.Deserialize (prefix, contentType, context, metadataProvider) = 
                let serial = System.Runtime.Serialization.DataContractSerializer(typeof<'a>)
                let graph = serial.ReadObject( context.InputStream )
                graph :?> 'a


    type internal FormBasedSerializerInputEntry = {
            key : string;
            mutable value : string[];
            children : List<FormBasedSerializerInputEntry> // Dictionary<string,FormBasedSerializerInputEntry>
        }
        with 
            static member private regex = System.Text.RegularExpressions.Regex("\[[\w]+\]")

            member internal x.GetNode (name:string) = 
                let node = x.children.Find (fun n -> n.key = name)
                if node != null then 
                    node
                else 
                    let node = { key = name; value = null; children = List() }
                    x.children.Add node
                    node

            member this.Process (key:string) (value:string[]) = 
                let mutable targetNode = this
                let matches = FormBasedSerializerInputEntry.regex.Matches(key)
                for m in matches do
                    let res = m.Value
                    targetNode <- targetNode.GetNode (res.Substring(1, res.Length - 2))
                targetNode.value <- value
                this

    // TODO: Perf analysis 
    type FormBasedSerializer<'a>() = 

        // TODO: Replace reflection by compiled quotations
        //       and cache propertyInfo into these expressions (for get/set)
        // TODO: Even better, start using the ModelMetadataProvider
        let rec deserialize_into (prefix:string) (inst) (targetType:Type) (node:FormBasedSerializerInputEntry) (metadataProvider:ModelMetadataProvider) = 
            for nd in node.children do
                let modelMetadata = metadataProvider.Create(targetType)
                let property = modelMetadata.GetProperty(nd.key)

                if property <> null then
                    if nd.children <> null && nd.children.Count <> 0 then // [node][child] = value
                        process_children property (modelMetadata.GetPropertyMetadata(property)) inst targetType nd metadataProvider
                    else                                                  // [node] = value
                        process_property property (modelMetadata.GetPropertyMetadata(property)) inst targetType nd metadataProvider

        and process_property (property:PropertyInfo) (modelMetadata:ModelMetadata) inst (targetType:Type) (node:FormBasedSerializerInputEntry) (metadataProvider:ModelMetadataProvider) = 
            if property.CanWrite then
                let rawValue = Seq.head node.value
                let succeeded, value = Conversions.convert rawValue (property.PropertyType)
                if succeeded then
                    // property.SetValue(inst, value, null)
                    modelMetadata.SetValue (inst, value)

        and process_children (property:PropertyInfo) (modelMetadata:ModelMetadata) inst (targetType:Type) (node:FormBasedSerializerInputEntry) (metadataProvider:ModelMetadataProvider) = 
            let mutable childInst = null

            let isCollection = 
                property.PropertyType <> typeof<string> && 
                    property.PropertyType.IsGenericType && 
                    typedefof<IEnumerable<_>>.MakeGenericType( property.PropertyType.GetGenericArguments() )
                        .IsAssignableFrom(property.PropertyType) 

            if property.CanRead then
                childInst <- modelMetadata.GetValue(inst) // property.GetValue(inst, null)
                    
            if childInst = null then
                if not isCollection then
                    childInst <- Activator.CreateInstance(property.PropertyType)
                    // property.SetValue(inst, childInst, null)
                    modelMetadata.SetValue (inst, childInst)
                else 
                    // TODO: Support more collection types
                    if typedefof<IList<_>>.MakeGenericType(property.PropertyType.GetGenericArguments()).IsAssignableFrom(property.PropertyType) ||
                       typedefof<IEnumerable<_>>.MakeGenericType(property.PropertyType.GetGenericArguments()).IsAssignableFrom(property.PropertyType) then
                        let targetT = property.PropertyType.GetGenericArguments().[0]
                        let listType = typedefof<List<_>>.MakeGenericType( [|targetT|] )
                        childInst <- Activator.CreateInstance listType
                        // property.SetValue(inst, childInst, null)
                        modelMetadata.SetValue (inst, childInst)
                    else
                        failwithf "Collection type not supported %s" (property.PropertyType.FullName)

            if not isCollection then
                deserialize_into (node.key) childInst (property.PropertyType) node metadataProvider
            else
                let targetT = property.PropertyType.GetGenericArguments().[0]
                let list = childInst :?> System.Collections.IList

                for childNode in node.children do
                    let values = childNode.value

                    for i = 0 to (values.Length - 1) do
                        let value = values.[i]
                        let replNode = { 
                            key = node.key; value = null; 
                            children = List([ { key = childNode.key; value = [|value|]; children = null }  ]) 
                        }

                        if i < list.Count then
                            let collElem = list.[i]    
                            deserialize_into (node.key) collElem targetT replNode metadataProvider
                        else
                            let collElem = Activator.CreateInstance targetT
                            deserialize_into (node.key) collElem targetT replNode metadataProvider
                            list.Add collElem |> ignore


        interface IModelSerializer<'a> with
            
            member x.Serialize (model:'a, contentType:string, writer:System.IO.TextWriter, metadataProvider) = 
                raise(NotImplementedException("Form serialization is not bi-directional. "))
                ()

            member x.Deserialize (prefix, contentType, context, metadataProvider) = 
                let targetType = typeof<'a>
                let form = context.FormValues
                let inst = Activator.CreateInstance typeof<'a> :?> 'a

                let node = 
                    form.AllKeys 
                    |> Array.choose (fun k -> 
                                        if k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then 
                                            Some(k.Substring(prefix.Length)) 
                                        else None
                                    )
                    |> Array.fold 
                        (fun (s:FormBasedSerializerInputEntry) k -> s.Process k (form.GetValues(prefix + k)) ) { key = prefix; value = null; children = List() }

                deserialize_into prefix inst targetType node metadataProvider
                inst


    [<Export(typeof<IModelSerializerResolver>)>]
    type ModelSerializerResolver() as self = 
        let _custom = lazy Dictionary<Type,List<string*Type>>()
        let _defSerializers = lazy 
                                   let dict = Dictionary<string,Type>()
                                   dict.Add (MediaTypes.JSon, typedefof<JsonSerializer<_>>)
                                   dict.Add (MediaTypes.Xml, typedefof<XmlSerializer<_>>)
                                   dict.Add (MediaTypes.FormUrlEncoded, typedefof<FormBasedSerializer<_>>)
                                   dict

        [<System.Security.SecuritySafeCriticalAttribute>]
        let instantiate (serializerType:Type) = 
            // can be optimized by using compiled expressions
            let constructors = serializerType.GetConstructors(BindingFlags.Public ||| BindingFlags.Instance)
            if Seq.isEmpty constructors then 
                Activator.CreateInstance serializerType 
            else
                let firstConstructor = constructors |> Seq.head
                if firstConstructor.GetParameters().Length = 1 then 
                    let selfAsContract = self :> IModelSerializerResolver 
                    // assumes that constructor takes the resolver as parameter (convention)
                    let instance = System.Runtime.Serialization.FormatterServices.GetSafeUninitializedObject( serializerType )
                    firstConstructor.Invoke(instance, [|selfAsContract|]) |> ignore
                    instance 
                else 
                    // doesnt take anything
                    Activator.CreateInstance serializerType // :?> IModelSerializer<'a>


        let resolve_serializer_type (mime) (modelType:Type) = 
            let mutable serializerType : Type = null

            if _custom.IsValueCreated then
                let exists, list = _custom.Force().TryGetValue modelType
                if exists then
                    let found = list |> Seq.tryFind (fun t -> (fst t) = mime)
                    if Option.isSome found then
                       serializerType <- snd found.Value
            if serializerType = null then
                let dict = _defSerializers.Force()
                let exists, tmpType = dict.TryGetValue mime
                if exists then
                    serializerType <- tmpType
            
            if serializerType = null then
                raise (MonoRailException((sprintf "No serializer found for mime type %O for Type %O" mime modelType)))
            else serializerType

        let resolve_serializer (mime) (modelType:Type) = 
            let serializerType = resolve_serializer_type mime modelType

            if serializerType.IsGenericTypeDefinition then
                let instantiatedType = serializerType.MakeGenericType( [|modelType|] )
                instantiate instantiatedType 
            else
                instantiate serializerType

        interface IModelSerializerResolver with

            member x.HasCustomSerializer (model:Type, mediaType:string) = 
                let dict = _custom.Force()
                let res, list = dict.TryGetValue model
                if not res then 
                    false
                else 
                    match list |> Seq.tryFind (fun (m,_) -> m = mediaType) with 
                    | Some _ -> true
                    | _ -> false

            member x.Register<'a>(mediaType:string, serializer:Type) = 
                arg_not_null serializer "serializer"

                let modelType = typeof<'a>
                let dict = _custom.Force()
                let exists,list = dict.TryGetValue modelType
                if not exists then
                    let list = List()
                    list.Add (mediaType, serializer)
                    dict.[modelType] <- list
                else
                    let existing = list |> Seq.tryFindIndex (fun t -> (fst t) === mediaType)
                    if existing.IsSome then
                        list.RemoveAt existing.Value
                    list.Add (mediaType,serializer)

            // todo: memoization would be a good thing here, since serializers should be stateless
            member x.CreateSerializer (modelType:Type, mediaType:string) = 
                arg_not_null modelType "modelType"

                let serializer = resolve_serializer mediaType modelType

                upcast NonGenericSerializerAdapter(serializer, modelType)


            // todo: memoization would be a good thing here, since serializers should be stateless
            member x.CreateSerializer<'a>(mediaType:string) : IModelSerializer<'a> = 
                resolve_serializer mediaType typeof<'a> :?> IModelSerializer<'a>

                
    and NonGenericSerializerAdapter(serializer, modelType) =
        let serializerType = typedefof<IModelSerializer<_>>.MakeGenericType([|modelType|]) 

        let serializeCall   = lazy 
                                 let me = serializerType.GetMethod("Serialize")
                                 fun model content writer metadata -> me.Invoke(serializer, [|model;content;writer;metadata|])
        let deserializeCall = lazy 
                                 let me = serializerType.GetMethod("Deserialize")
                                 fun prefix content request metadata -> me.Invoke(serializer, [|prefix;content;request;metadata|])

        interface IModelSerializer with  
            member x.Serialize (model:obj, contentType:string, writer:System.IO.TextWriter, metadataProvider) = 
                let fn = serializeCall.Force()
                fn model contentType writer metadataProvider |> ignore

            member x.Deserialize (prefix:string, contentType:string, context, metadataProvider) =
                let fn = deserializeCall.Force()
                fn prefix contentType context metadataProvider 
                    
