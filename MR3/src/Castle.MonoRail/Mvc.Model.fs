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
    open System.ComponentModel
    open System.ComponentModel.DataAnnotations
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Web
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.ExprShape

    //
    // this whole thing needs to be reviewed
    // 

    [<System.ComponentModel.Composition.Export(typeof<ModelMetadataProvider>)>]
    type DataAnnotationsModelMetadataProvider() = 
        inherit ModelMetadataProvider()
        let _type2CachedMetadata = Dictionary<Type, ModelMetadata>()

        let inspect_property (typ:Type, prop:PropertyInfo) = 
            let propMeta = ModelMetadata(typ, prop)
            // propMeta.DisplayFormat <- read_att prop
            // propMeta.DisplayAtt    <- read_att prop
            // propMeta.Editable      <- read_att prop
            // propMeta.UIHint        <- read_att_filter prop (fun f -> f.PresentationLayer = "MVC")
            propMeta.Required      <- read_att prop
            let defVal = 
                let att : DefaultValueAttribute = read_att prop
                if att <> null then att.Value else null
            propMeta.DefaultValue  <- defVal
            propMeta

        override x.Create(typ) =
            // TODO: replace by ReadWriteLockerSlim
            lock(_type2CachedMetadata) 
                (fun _ -> 
                    let res, meta = _type2CachedMetadata.TryGetValue typ
                    if res then 
                        meta
                    else
                        // TODO: Support for MetadataTypeAttribute
                        let dict = Dictionary() 
            
                        typ.GetProperties( BindingFlags.Public ||| BindingFlags.Instance ) 
                            |> Seq.map  (fun p -> (p, (inspect_property (typ, p))) ) 
                            |> Seq.iter (fun p -> dict.[fst p] <- snd p) 
                            |> ignore

                        let meta = ModelMetadata(typ, null, dict)
                        _type2CachedMetadata.[typ] <- meta
                        meta
                )

    


    

