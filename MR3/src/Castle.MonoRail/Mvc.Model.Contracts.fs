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

    (*
    type ModelValidationMetadata() = 
        class
        end

    [<AbstractClass>]
    type ModelValidationMetadataProvider() = 
        abstract member Create : ``type``:Type -> ModelValidationMetadata

    [<System.ComponentModel.Composition.Export(typeof<ModelMetadataProvider>)>]
    type DataAnnotationsModelValidationMetadataProvider() = 
        inherit ModelValidationMetadataProvider()
        override x.Create(typ) =
            ModelValidationMetadata()
    *)


    type ModelMetadata(targetType:Type, prop:PropertyInfo, properties:IDictionary<PropertyInfo, ModelMetadata>) = 
        class
            let mutable _dataType : DataType = DataType.Text
            let mutable _displayFormat : DisplayFormatAttribute = null
            let mutable _display : DisplayAttribute = null
            let mutable _editable : EditableAttribute = null
            let mutable _UIHint  : UIHintAttribute = null
            let mutable _required : RequiredAttribute = null
            let mutable _defvalue : obj = null
            let _valueGetter = 
                lazy (
                        if prop = null then failwith "ModelMetadata does not represet a property of a model, therefore GetValue is not supported" 

                        let objParam = Expression.Parameter(typeof<obj>)
                        let lambdaParams = [|objParam|]
                        let body : Expression = 
                            if prop.PropertyType.IsValueType then
                                upcast Expression.Convert( Expression.Property(Expression.TypeAs(objParam, targetType), prop), typeof<obj>)
                            else 
                                upcast Expression.Property(Expression.TypeAs(objParam, targetType), prop)
                        let lambdaExp = Expression.Lambda<Func<obj,obj>>(body, lambdaParams)
                        lambdaExp.Compile()
                     )
            let _valueSetter = 
                lazy (
                        if prop = null then failwith "ModelMetadata does not represet a property of a model, therefore SetValue is not supported" 

                        let objParam = Expression.Parameter(typeof<obj>)
                        let valParam = Expression.Parameter(typeof<obj>)
                        let lambdaParams = [|objParam;valParam|]
                        let valExp = Expression.Convert(valParam, prop.PropertyType)
                        let body = Expression.Assign( Expression.Property(Expression.TypeAs(objParam, targetType), prop), valExp )
                        let lambdaExp = Expression.Lambda<Action<obj,obj>>(body, lambdaParams)
                        lambdaExp.Compile()
                     )

            new (targetType:Type) = 
                ModelMetadata(targetType, null, Dictionary())
            
            new (targetType:Type, prop:PropertyInfo) = 
                ModelMetadata(targetType, prop, Dictionary())

            member x.ModelType = 
                if prop <> null then prop.PropertyType else targetType

            member x.DataType           with get() = _dataType and set v = _dataType <- v
            // member x.DisplayFormat      with get() = _displayFormat and set v = _displayFormat <- v
            // member x.DisplayAtt         with get() = _display  and set v = _display <- v
            // member x.Editable           with get() = _editable and set v = _editable <- v
            // member x.UIHint             with get() = _UIHint   and set v = _UIHint <- v
            member x.Required           with get() = _required and set v = _required <- v
            member x.DefaultValue       with get() = _defvalue and set v = _defvalue <- v
            member x.DisplayName = 
                if _display <> null 
                then _display.Name
                else prop.Name

            member x.GetProperty(name:string) : PropertyInfo = 
                properties.
                    Where( (fun (k:KeyValuePair<PropertyInfo,_>) -> k.Key.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ).
                    Select( (fun (k:KeyValuePair<PropertyInfo,ModelMetadata>) -> k.Key) ).
                    FirstOrDefault()

            member x.GetValue(modelInstance) : obj =
                _valueGetter.Force().Invoke(modelInstance)
            
            member x.SetValue(modelInstance, value) : unit =
                _valueSetter.Force().Invoke(modelInstance, value) 

            member x.GetPropertyMetadata (propertyInfo:PropertyInfo) = 
                properties.[propertyInfo]

        end


    [<AbstractClass;AllowNullLiteral>]
    type ModelMetadataProvider() = 
        abstract member Create : ``type``:Type -> ModelMetadata

