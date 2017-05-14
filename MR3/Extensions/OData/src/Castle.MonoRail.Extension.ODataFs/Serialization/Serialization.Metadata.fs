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
open System.Collections.Generic
open System.IO
open System.Data.OData
open System.Data.Services.Providers
open System.Data.Services.Common
open System.Web
open System.Text
open System.Xml
open Castle.MonoRail

// used when ServiceDirectory is accessed
module AtomServiceDocSerializer = 
    begin

        let public serialize (writer:TextWriter, baseUri:Uri, wrapper:DataServiceMetadataProviderWrapper, encoding:Encoding) = 
            let xmlwriter = SerializerCommons.create_xmlwriter writer encoding

            let write_collection (rs:ResourceSetWrapper) = 
                xmlwriter.WriteStartElement ("", "collection", "http://www.w3.org/2007/app")
                xmlwriter.WriteAttributeString ("href", rs.Name)
                xmlwriter.WriteStartElement ("title", "http://www.w3.org/2005/Atom")
                xmlwriter.WriteString (rs.Name)
                xmlwriter.WriteEndElement ()
                xmlwriter.WriteEndElement ()

            xmlwriter.WriteStartElement ("service", "http://www.w3.org/2007/app")
            xmlwriter.WriteAttributeString ("xml", "base", null, baseUri.AbsoluteUri) 
            xmlwriter.WriteAttributeString ("xmlns", "atom", null, "http://www.w3.org/2005/Atom")
            xmlwriter.WriteAttributeString ("xmlns", "app", null, "http://www.w3.org/2007/app")
            xmlwriter.WriteStartElement ("", "workspace", "http://www.w3.org/2007/app")
            xmlwriter.WriteStartElement ("title", "http://www.w3.org/2005/Atom")
            xmlwriter.WriteString ("Default")
            xmlwriter.WriteEndElement ()

            wrapper.ResourceSets |> Seq.iter write_collection

            xmlwriter.WriteEndElement()
            xmlwriter.WriteEndElement()
            xmlwriter.Flush()
    end

// used when /$metadata is accessed with GET
// see http://msdn.microsoft.com/en-us/library/bb399292.aspx
module MetadataSerializer =
    begin

        type private Epm = 
            static member EpmKeepInContent = "FC_KeepInContent"
            static member EpmSourcePath    = "FC_SourcePath"
            static member EpmTargetPath    = "FC_TargetPath"
            static member EpmContentKind   = "FC_ContentKind"
            static member EpmNsPrefix      = "FC_NsPrefix"
            static member EpmNsUri         = "FC_NsUri"

        let private get_associationtype_lookupname (rt:ResourceType) (prop:ResourceProperty) = 
            rt.Name + 
                if prop <> null 
                then "_" + prop.Name 
                else ""

        let private get_associationtype_name (end1:ResourceType) (prop1:ResourceProperty) (end2:ResourceType) (prop2:ResourceProperty) =  
            end1.Name + "_" + prop1.Name

            (*
            let actualend1 = if prop1 <> null then end1 else end2
            let actualend2 = if actualend1 = end1
                             then (if prop2 <> null then end2 else null)
                             else null
            actualend1.Name + "_" + prop1.Name + 
                if actualend2 <> null 
                then "_" + actualend2.Name + "_" + prop2.Name
                else ""
            *)

        (*
        let private get_associationtype_name (association:ResourceAssociationSet) =  
            let end1 = if association.End1.ResourceProperty <> null then association.End1 else association.End2
            let end2 = if end1 = association.End1
                       then (if association.End2.ResourceProperty <> null then association.End2 else null)
                       else null
            end1.ResourceType.Name + "_" + end1.ResourceProperty.Name + 
                if end2 <> null 
                then "_" + end2.ResourceType.Name + "_" + end2.ResourceProperty.Name
                else ""
        *)

        // http://msdn.microsoft.com/en-us/library/dd942559%28v=prot.10%29.aspx
        let write_epm_properties (xmlWriter:XmlWriter) skipSourcePath removePrefix (items:EntityPropertyMappingAttribute seq) = 
            
            let syndication_to_path (property:SyndicationItemProperty) = 
                match property with
                | SyndicationItemProperty.AuthorEmail -> "SyndicationAuthorEmail"
                | SyndicationItemProperty.AuthorName -> "SyndicationAuthorName"
                | SyndicationItemProperty.AuthorUri -> "SyndicationAuthorUri"
                | SyndicationItemProperty.ContributorEmail -> "SyndicationContributorEmail"
                | SyndicationItemProperty.ContributorName -> "SyndicationContributorName"
                | SyndicationItemProperty.ContributorUri -> "SyndicationContributorUri"
                | SyndicationItemProperty.Updated -> "SyndicationUpdated"
                | SyndicationItemProperty.Published -> "SyndicationPublished"
                | SyndicationItemProperty.Rights -> "SyndicationRights"
                | SyndicationItemProperty.Summary -> "SyndicationSummary"
                | _ -> "SyndicationTitle"

            let syndicationtext_to_content (contentKind:SyndicationTextContentKind) = 
                match contentKind with
                | SyndicationTextContentKind.Plaintext -> "text"
                | SyndicationTextContentKind.Html -> "html"
                | _ -> "xhtml"

            // let postfix = ref 0
            
            let write_epm_attributes (att:EntityPropertyMappingAttribute) = 
                // let postAsStr = sprintf "_%d" (!postfix)

                if att.TargetSyndicationItem = SyndicationItemProperty.CustomProperty then
                    xmlWriter.WriteAttributeString(Epm.EpmTargetPath,
                                                   "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                                   att.TargetPath)

                    xmlWriter.WriteAttributeString(Epm.EpmNsUri,
                                                   "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                                   att.TargetNamespaceUri)
                    
                    if not <| String.IsNullOrEmpty(att.TargetNamespacePrefix) then
                        xmlWriter.WriteAttributeString(Epm.EpmNsPrefix,
                                                       "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                                       att.TargetNamespacePrefix)
                else 
                    xmlWriter.WriteAttributeString(Epm.EpmTargetPath,
                                                   "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                                   syndication_to_path(att.TargetSyndicationItem))
                    
                    xmlWriter.WriteAttributeString(Epm.EpmContentKind,
                                                   "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                                   syndicationtext_to_content(att.TargetTextContentKind))

                
                if not skipSourcePath then
                    xmlWriter.WriteAttributeString(Epm.EpmSourcePath,
                                                   "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                                   if removePrefix then att.SourcePath.Substring(att.SourcePath.IndexOf('/') + 1) else att.SourcePath)

                xmlWriter.WriteAttributeString(Epm.EpmKeepInContent,
                                               "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata",
                                               if att.KeepInContent then "true" else "false")
                
                // incr postfix

            items |> Seq.iter write_epm_attributes


        let private write_property (xmlWriter:XmlWriter) (resRt:ResourceType) (property:ResourceProperty) (associationTypesCache:Dictionary<string, ResourceAssociationType>) = 

            let write_facets () = 
                let pType = property.Type
                let value = 
                    if property.IsOfKind(ResourcePropertyKind.Key) || (pType.IsValueType && Nullable.GetUnderlyingType(pType) = null) 
                    then "false" else "true"
                xmlWriter.WriteAttributeString("Nullable", value)
            let write_epm_attributes (removePrefix) = 
                let skipSourcePath = resRt.OwnEpmAttributes |> Seq.exists (fun att -> att.SourcePath = property.Name)
                resRt.OwnEpmAttributes 
                |> Seq.filter (fun epm -> (Array.get (epm.SourcePath.Split([|'/'|])) 0) = property.Name)
                |> write_epm_properties xmlWriter skipSourcePath removePrefix

            if property.ResourceType.ResourceTypeKind = ResourceTypeKind.Primitive then
                xmlWriter.WriteStartElement("Property")
                xmlWriter.WriteAttributeString("Name", property.Name)
                xmlWriter.WriteAttributeString("Type", property.ResourceType.FullName)
                write_facets()

                // if (!string.IsNullOrEmpty(property.MimeType)) then
                //    MetadataSerializer.WriteDataWebMetadata(xmlWriter, "MimeType", property.MimeType);
                
                if (resRt.ResourceTypeKind == ResourceTypeKind.EntityType && resRt.ETagProperties.Contains(property)) then
                    xmlWriter.WriteAttributeString("ConcurrencyMode", "Fixed")

                write_epm_attributes false
            
            elif property.Kind = ResourcePropertyKind.ComplexType then
            
                xmlWriter.WriteStartElement("Property")
                xmlWriter.WriteAttributeString("Name", property.Name)
                xmlWriter.WriteAttributeString("Type", property.ResourceType.FullName)
                write_facets()
                // xmlWriter.WriteAttributeString("Nullable", "true")

                write_epm_attributes true
                
            else
                xmlWriter.WriteStartElement("NavigationProperty")
                xmlWriter.WriteAttributeString("Name", property.Name)

                let lookup = get_associationtype_lookupname resRt property
                let res, assocType = associationTypesCache.TryGetValue lookup
                if res then
                    let typeend   = assocType.GetResourceAssociationTypeEnd (resRt, property)
                    let otherside = assocType.GetRelatedResourceAssociationSetEnd (resRt, property)
                    xmlWriter.WriteAttributeString("Relationship", assocType.FullName)
                    xmlWriter.WriteAttributeString("FromRole", typeend.Name)
                    xmlWriter.WriteAttributeString("ToRole", otherside.Name)
            
            xmlWriter.WriteEndElement()

        let private write_properties (writer:XmlWriter) (resRt:ResourceType) (associationTypesCache:Dictionary<string, ResourceAssociationType>) = 
            resRt.PropertiesDeclaredOnThisType 
            |> Seq.iter (fun p -> write_property writer resRt p associationTypesCache)

        let private write_entity (writer:XmlWriter) (resRt:ResourceType) (associationTypesCache:Dictionary<string, ResourceAssociationType>) = 
            
            let write_key () = 
                writer.WriteStartElement("Key")
                resRt.KeyProperties
                |> Seq.iter (fun kp -> (writer.WriteStartElement("PropertyRef")
                                        writer.WriteAttributeString("Name", kp.Name)
                                        writer.WriteEndElement() ))
                writer.WriteEndElement()

            writer.WriteStartElement("EntityType")
            writer.WriteAttributeString("Name", XmlConvert.EncodeName(resRt.InstanceType.Name))

            if resRt.IsAbstract || resRt.BaseType <> null then raise(NotImplementedException("Abstract/Inheritance for ResourceTypes is not supported"))
            if resRt.IsOpenType then raise(NotImplementedException("OpenTypes aren't supported"))
            if resRt.IsMediaLinkEntry then raise(NotImplementedException("MediaLinkEntry is not supported"))

            // just to force validation of type
            resRt.PropertiesDeclaredOnThisType |> ignore

            // if resRt.HasEntityPropertyMappings then 
            write_epm_properties writer false false resRt.OwnEpmAttributes // resRt.InheritedEpmInfo

            write_key ()
            write_properties writer resRt associationTypesCache
            writer.WriteEndElement()

        let private write_complex (writer:XmlWriter) (resRt:ResourceType) (associationTypesCache:Dictionary<string, ResourceAssociationType>) = 
            writer.WriteStartElement("ComplexType")
            writer.WriteAttributeString("Name", XmlConvert.EncodeName(resRt.InstanceType.Name))
            write_properties writer resRt associationTypesCache
            writer.WriteEndElement()

        let private write_type (writer:XmlWriter) (resRt:ResourceType) (associationTypesCache:Dictionary<string, ResourceAssociationType>) = 
            if resRt.ResourceTypeKind = ResourceTypeKind.EntityType then
                write_entity writer resRt associationTypesCache
            else
                write_complex writer resRt associationTypesCache

        let private prepare (wrapper:DataServiceMetadataProviderWrapper) 
                            (associationSetsCache:Dictionary<string, ResourceAssociationSet>) 
                            (associationTypesCache:Dictionary<string, ResourceAssociationType>) = 
            
            let get_res_association_set (rs:ResourceSetWrapper) (rt:ResourceType) (prop:ResourceProperty) = 
                let key = rs.Name + "_" + rt.FullName + "_" + prop.Name

                let r, association = associationSetsCache.TryGetValue key
                if r then association
                else
                    let association = wrapper.GetResourceAssociationSet(rs, rt, prop)
                    if association <> null then
                        // todo: cache
                        let associationEnd = association.GetRelatedResourceAssociationSetEnd(rs, rt, prop)
                        if associationEnd.ResourceProperty <> null then
                            let associationSet = wrapper.GetResourceAssociationSet(wrapper.ValidateResourceSet(associationEnd.ResourceSet), associationEnd.ResourceType, associationEnd.ResourceProperty)
                            if associationSet = null || association.Name <> associationSet.Name 
                            then raise(InvalidOperationException("Invalid bi-directional assocation"))
                            let key2 = sprintf "%s_%s_%s" associationEnd.ResourceSet.Name associationEnd.ResourceProperty.ResourceType.FullName  associationEnd.ResourceProperty.Name
                            associationSetsCache.Add (key2, association)
                        (*  
                        let key2 = 
                            if associationEnd.ResourceProperty <> null
                            then sprintf "%s_%s_%s" associationEnd.ResourceSet.Name associationEnd.ResourceProperty.ResourceType.FullName  associationEnd.ResourceProperty.Name
                            else sprintf "%s_Null_%s_%s" associationEnd.ResourceSet.Name rt.FullName prop.Name
                        associationSetsCache.Add (key2, association)
                        *)
                        associationSetsCache.Add (key, association)
                    association

            let get_association_type (association:ResourceAssociationSet) (rs:ResourceSetWrapper) 
                                     (rt:ResourceType) (prop:ResourceProperty) = 

                let lookupName = get_associationtype_lookupname rt prop
                if not <| associationTypesCache.ContainsKey lookupName then
                    failwithf "Since RT associations are processed first, it was expected that the association name %s would already be created by now. However, it wasn't." lookupName
                else associationTypesCache.[lookupName]

            let build_association_info (rs:ResourceSetWrapper) (rt:ResourceType) (prop:ResourceProperty) = 
                let association = get_res_association_set rs rt prop
                if association <> null then
                    association.ResourceAssociationType <- get_association_type association rs rt prop 

            let populate_association_set (rs:ResourceSetWrapper) = 
                let rt = rs.ResourceType
                rt.PropertiesDeclaredOnThisType 
                |> Seq.filter (fun p -> p.ResourceType.ResourceTypeKind = ResourceTypeKind.EntityType)
                |> Seq.iter (fun p -> build_association_info rs rt p)

            let ensure_association_type_exists (rt:ResourceType) (prop:ResourceProperty) = 
                let end1 = rt
                let end2 = prop.ResourceType
                let otherProp = 
                    match end2.PropertiesDeclaredOnThisType |> Seq.tryPick (fun p -> if p.ResourceType = end1 then Some(p) else None) with 
                    | Some p -> p
                    | _ -> null
                
                let name = get_associationtype_name end1 prop end2 otherProp
                let lookupName = get_associationtype_lookupname rt prop
                
                if not <| associationTypesCache.ContainsKey lookupName then
                    let bidirectional = otherProp <> null
                    
                    let end1Name, end2Name = 
                        if not bidirectional then
                            if otherProp = null 
                            then rt.Name, prop.Name
                            else prop.Name, rt.Name
                        else 
                            (get_associationtype_lookupname end1 prop), 
                            (get_associationtype_lookupname end2 otherProp)

                    let resourceAssociationType = 
                        ResourceAssociationType(
                            name, 
                            rt.Namespace, 
                            new ResourceAssociationTypeEnd(end1Name, end1, prop, otherProp), 
                            new ResourceAssociationTypeEnd(end2Name, end2, otherProp, prop))
                    associationTypesCache.Add (lookupName, resourceAssociationType)

                    if bidirectional then
                        let name = get_associationtype_lookupname end2 otherProp
                        if not <| associationTypesCache.ContainsKey name then
                            associationTypesCache.Add (name, ResourceAssociationType(name, rt.Namespace, resourceAssociationType.End2, resourceAssociationType.End1))

            let populate_association_types (rt:ResourceType) = 
                rt.PropertiesDeclaredOnThisType 
                |> Seq.filter (fun p -> p.ResourceType.ResourceTypeKind = ResourceTypeKind.EntityType)
                |> Seq.iter (fun p -> ensure_association_type_exists rt p)

            wrapper.ResourceTypes |> Seq.iter populate_association_types
            wrapper.ResourceSets  |> Seq.iter populate_association_set
            


        let public serialize(writer:TextWriter, wrapper:DataServiceMetadataProviderWrapper, enc:Encoding) = 
            let xmlWriter = SerializerCommons.create_xmlwriter writer enc

            let associationSetsCache = Dictionary<string, ResourceAssociationSet>()
            let associationTypesCache =  Dictionary<string, ResourceAssociationType>()

            prepare wrapper associationSetsCache associationTypesCache

            let write_associations (writer:XmlWriter) = 
                
                let write_association_type (association:ResourceAssociationType) = 
                    let write_association_end (``end``:ResourceAssociationTypeEnd) = 
                        writer.WriteStartElement "End" 
                        writer.WriteAttributeString ("Role", ``end``.Name)
                        writer.WriteAttributeString ("Type", ``end``.ResourceType.Namespace + "." + ``end``.ResourceType.InstanceType.Name)
                        writer.WriteAttributeString ("Multiplicity", ``end``.Multiplicity)
                        writer.WriteEndElement()

                    writer.WriteStartElement "Association"
                    writer.WriteAttributeString("Name", association.Name)
                    write_association_end association.End1
                    if association.End2.Name <> association.End1.Name then
                        write_association_end association.End2
                    writer.WriteEndElement ()

                associationTypesCache.Values |> Seq.iter write_association_type
                
            let write_entitycontainer (writer:XmlWriter) (wrapper:DataServiceMetadataProviderWrapper) = 

                let write_entity (rs:ResourceSetWrapper) = 
                    writer.WriteStartElement "EntitySet"
                    writer.WriteAttributeString ("Name", rs.Name)
                    writer.WriteAttributeString ("EntityType", rs.ResourceType.Namespace + "." + rs.ResourceType.InstanceType.Name)
                    writer.WriteEndElement()

                let write_association_set (association:ResourceAssociationSet) = 

                    let write_end (``end``:ResourceAssociationTypeEnd) (aSetEnd:ResourceAssociationSetEnd) = 
                        if ``end`` <> null && aSetEnd.ResourceSet <> null then
                            writer.WriteStartElement "End" 
                            writer.WriteAttributeString ("Role", ``end``.Name)
                            writer.WriteAttributeString ("EntitySet", aSetEnd.ResourceSet.Name)
                            writer.WriteEndElement ()

                    writer.WriteStartElement "AssociationSet"
                    writer.WriteAttributeString ("Name", association.Name)
                    writer.WriteAttributeString("Association", association.ResourceAssociationType.FullName)
                    let associationTypeEnd1 = association.ResourceAssociationType.GetResourceAssociationTypeEnd(association.End1.ResourceType, association.End1.ResourceProperty)
                    let associationTypeEnd2 = association.ResourceAssociationType.GetResourceAssociationTypeEnd(association.End2.ResourceType, association.End2.ResourceProperty)
                    write_end associationTypeEnd1 association.End1
                    write_end associationTypeEnd2 association.End2
                    writer.WriteEndElement ()

                writer.WriteStartElement "EntityContainer"
                writer.WriteAttributeString ("Name", (XmlConvert.EncodeName(wrapper.ContainerName)))
                writer.WriteAttributeString ("m", "IsDefaultEntityContainer", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata", "true")

                wrapper.ResourceSets |> Seq.iter write_entity
                associationSetsCache.Values |> Seq.iter write_association_set
                // write service operations

                writer.WriteEndElement ()

            xmlWriter.WriteStartElement("edmx", "Edmx", "http://schemas.microsoft.com/ado/2007/06/edmx")
            xmlWriter.WriteAttributeString("Version", "1.0")
            xmlWriter.WriteStartElement("edmx", "DataServices", "http://schemas.microsoft.com/ado/2007/06/edmx")
            xmlWriter.WriteAttributeString("xmlns", "m", null, "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")
            xmlWriter.WriteAttributeString("m", "DataServiceVersion", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata", "2.0")
            
            xmlWriter.WriteStartElement("Schema", "http://schemas.microsoft.com/ado/2008/09/edm")
            xmlWriter.WriteAttributeString("Namespace", wrapper.ContainerNamespace)
            xmlWriter.WriteAttributeString("xmlns", "d", null, "http://schemas.microsoft.com/ado/2007/08/dataservices")
            xmlWriter.WriteAttributeString("xmlns", "m", null, "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")

            wrapper.ResourceTypes
            |> Seq.iter (fun rs -> write_type xmlWriter rs associationTypesCache)

            write_associations xmlWriter 

            // travese namespaces + types
            //   write schema
            //           entityType
            //           Complextypes
            //           associations
            //           EntityContainer

            write_entitycontainer xmlWriter wrapper // wrapper.ResourceSets

            xmlWriter.WriteEndElement() // Schema
            xmlWriter.WriteEndElement() // edmx:DataServices
            xmlWriter.WriteEndElement() // edmx:Edmx
            xmlWriter.Flush()

    end



