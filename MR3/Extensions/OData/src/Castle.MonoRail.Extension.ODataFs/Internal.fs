module InternalUtils 

    open System
    open System.Reflection
    open System.Collections.Generic


    let getEnumerableElementType (possibleEnumerableType:Type) = 
        let found = possibleEnumerableType.FindInterfaces(TypeFilter(fun t o -> (o :?> Type).IsAssignableFrom(t)), typedefof<IEnumerable<_>>) 
        if found.Length = 0
        then None
        else Some(found.[0].GetGenericArguments().[0])
