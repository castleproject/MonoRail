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
    open System.Data.OData
    open System.Data.Services.Providers
    open System.Linq
    open Castle.MonoRail.OData
    open Castle.MonoRail.Extension.OData

    /// Access point to entities to be exposed by a single odata endpoint
    type ODataModel(schemaNamespace, containerName) = 
        class
            let mutable _schemaNs = schemaNamespace
            let mutable _containerName = containerName

            let _entities = List<EntitySetConfig>()
            let _entSeq : EntitySetConfig seq = upcast _entities

            let _resourcetypes = lazy ( let rts = ResourceMetadataBuilder.build(_schemaNs, _entities) 
                                        rts |> Seq.iter (fun rt -> rt.SetReadOnly() )
                                        rts.ToList() |> box :?> ResourceType seq )
            
            let _resourcesets  = lazy ( _resourcetypes.Force() 
                                        |> Seq.filter (fun rt -> rt.ResourceTypeKind = ResourceTypeKind.EntityType && (_entities |> Seq.exists (fun e -> e.EntityName === rt.Name) ) )
                                        |> Seq.map (fun rt -> (let name = (_entSeq |> Seq.find(fun e -> e.TargetType = rt.InstanceType)).EntitySetName 
                                                               let rs = ResourceSet(name, rt)
                                                               rs.SetReadOnly()
                                                               rs ))
                                        |> box :?> ResourceSet seq)
            

            member x.SchemaNamespace with get() = schemaNamespace
            member x.ContainerName   with get() = containerName

            member x.EntitySet<'a>(entitySetName:string, source:IQueryable<'a>) = 
                if _resourcesets.IsValueCreated then raise(InvalidOperationException("Model is frozen since ResourceSets were built"))
                let entityType = typeof<'a>
                let cfg = EntitySetConfigurator(entitySetName, entityType.Name, source)
                _entities.Add cfg
                cfg

            member x.Entities = _entSeq
            member internal x.ResourceSets  = _resourcesets.Force()
            member internal x.ResourceTypes = _resourcetypes.Force()
            member internal x.GetResourceType(name) = 
                x.ResourceTypes 
                |> Seq.tryFind (fun rs -> StringComparer.OrdinalIgnoreCase.Equals( rs.Name, name ) )
            member internal x.GetResourceSet(name) = 
                x.ResourceSets 
                |> Seq.tryFind (fun rs -> StringComparer.OrdinalIgnoreCase.Equals( rs.Name, name ) )
            member internal x.GetQueryable(rs:ResourceSet) = 
                match _entities |> Seq.tryFind (fun e -> StringComparer.OrdinalIgnoreCase.Equals(e.EntitySetName, rs.Name)) with
                | Some e -> e.Source
                | _ -> null
            member internal x.GetRelatedResourceSet(rt:ResourceType) =
                x.ResourceSets 
                |> Seq.tryFind (fun rs -> rs.ResourceType = rt )

            interface IDataServiceMetadataProvider with 
                member x.ContainerNamespace = x.SchemaNamespace
                member x.ContainerName = x.ContainerName
                member x.ResourceSets = x.ResourceSets
                member x.Types = x.ResourceTypes
                member x.ServiceOperations = 
                    // we dont support ops yet
                    Seq.empty
                member x.GetDerivedTypes(resType) = 
                    // we dont support hierarchies yet
                    Seq.empty
                member x.HasDerivedTypes(resType) = 
                    // we dont support hierarchies yet
                    false
                member x.TryResolveResourceSet(name, rtToReturn) = 
                    match x.GetResourceSet(name) with 
                    | Some rt -> rtToReturn <- rt; true
                    | None -> false
                member x.TryResolveResourceType(name, rtToReturn) = 
                    match x.GetResourceType(name) with 
                    | Some rt -> rtToReturn <- rt; true
                    | None -> false 
                member x.TryResolveServiceOperation(name, opToReturn) = 
                    // we dont support ops yet
                    false
                member x.GetResourceAssociationSet(resSet, resType, property) = 
                    let targetResType = property.ResourceType
                    match x.ResourceSets |> Seq.tryFind (fun rs -> targetResType.InstanceType.IsAssignableFrom(rs.ResourceType.InstanceType)) with
                    | Some containerResSet -> 
                        ResourceAssociationSet(resType.Name + "_" + property.Name, 
                                           ResourceAssociationSetEnd(resSet, resType, property), 
                                           ResourceAssociationSetEnd(containerResSet, targetResType, null))
                    | _ -> null

        end

    
        


