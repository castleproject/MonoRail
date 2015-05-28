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

module Conversions

    #nowarn "0042"

    open System
    open System.Globalization
    open System.Reflection

    let rec convert (value:obj) (desiredType:Type) : bool * obj = 

        if desiredType.IsArray then
            let elemType = desiredType.GetElementType()
            if value == null then
                false, Array.CreateInstance(elemType, 0) |> box
            else
                let items = value.ToString().Split([|','|], StringSplitOptions.None)
                let array = Array.CreateInstance(desiredType.GetElementType(), items.Length)
                let index = ref 0
                for item in items do
                    let converted, convertionResult = convert item elemType
                    if converted then
                        array.SetValue(convertionResult, !index)
                    index := !index + 1
                true, array |> box
        else
            let mutable tmp = null

            match desiredType with
            | ptype when ptype = typeof<bool> ->
                // what shall we check for? '0'/'false' ?
                if value <> null then
                    tmp <- Convert.ToBoolean(value.ToString()) :> obj
                    true, tmp
                else 
                    false, null
            | ptype when ptype.IsEnum ->
                if value <> null then 
                    tmp <- Enum.Parse(desiredType, value.ToString())
                    true, tmp
                else 
                    false, null
            | ptype when ptype = typeof<Decimal> ->
                if value <> null && value.ToString() = String.Empty then
                    true, box(0m)
                else
                    let parsed, rval = Decimal.TryParse(value.ToString(), NumberStyles.Any, System.Threading.Thread.CurrentThread.CurrentCulture)

                    if not parsed then
                        if value.ToString().EndsWith("%") then
                            tmp <- (Decimal.Parse(value.ToString().Replace("%", "")) / 100m)
                            true, tmp
                        else failwithf "Unsupported convertion type %O" desiredType
                    else
                        tmp <- rval
                        true, tmp
            | ptype when ptype = typeof<string> || typeof<IConvertible>.IsAssignableFrom(ptype) -> 
            
                if value != null && value.ToString() = String.Empty then
                  match desiredType with
                  | tp when tp = typeof<string> -> 
                     true, null
                  | tp when tp = typeof<int> -> 
                     true, box(0)
                  | _ -> 
                     failwithf "Unsupported type %O" desiredType
                else
                    tmp <- Convert.ChangeType(value, desiredType)
                    true, tmp
            | ptype when ptype = typeof<Guid> ->
                if value <> null then 
                    tmp <- Guid.Parse(value.ToString())
                    true, tmp
                else 
                    false, null
            | ptype when ptype.IsGenericType && ptype.GetGenericTypeDefinition() = typedefof<Nullable<_>> ->
                if value = null || value.ToString() = String.Empty then
                    true, null
                else
                    convert value (ptype.GetGenericArguments().[0])
            | _ -> 
                false, null
