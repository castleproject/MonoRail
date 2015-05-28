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
open System.Text
open System.Reflection
open System.Web
open Castle.MonoRail
open FParsec
open FParsec.Primitives
open FParsec.CharParsers


type QueryAst = 
    | Element
    | Null
    | Literal of Type * obj
    | PropertyAccess of QueryAst * PropertyInfo * ResourceType
    | BinaryExp of QueryAst * QueryAst * BinaryOp * ResourceType
    | UnaryExp of QueryAst * UnaryOp * ResourceType
    with 
        member x.GetExpType(root:ResourceType)  = 
            match x with 
            | Element                   -> root.InstanceType
            | Null                      -> typeof<unit>
            | Literal (t,_)             -> t
            | PropertyAccess (_,_,rt)   -> rt.InstanceType
            | UnaryExp (_,_,rt)         -> rt.InstanceType
            | BinaryExp (_,_,_,rt)      -> rt.InstanceType

        // member x.IsDecimal()

        member x.ToStringTree() = 
            let b = StringBuilder()
            let rec print (n) (level:int) = 
                b.AppendLine() |> ignore
                for x in 1..level do b.Append("  ") |> ignore
                match n with 
                | Null                      -> b.Append (sprintf "Null") |> ignore
                | Element                   -> b.Append (sprintf "Element") |> ignore
                | Literal (t,v)             -> b.Append (sprintf "Literal %s [%O]" t.Name v) |> ignore
                | PropertyAccess (ex,pinfo,rt) -> 
                    b.Append (sprintf "PropertyAccess [%s] = %s" pinfo.Name rt.FullName) |> ignore
                    print ex (level + 1)
                
                | UnaryExp (ex,op,rt)       -> 
                    b.Append (sprintf "Unary %O %s" op rt.FullName) |> ignore
                    print ex (level + 1)
                
                | BinaryExp (ex1,ex2,op,rt) -> 
                    b.Append (sprintf "Binary %O %s" op rt.FullName) |> ignore
                    print ex1 (level + 1)
                    print ex2 (level + 1)
                
                | _ -> failwithf "Unsupported node type? Need to update this match, dude"
            print x 1
            b.ToString()

type OrderByAst = 
    | Nothing
    | Asc of QueryAst
    | Desc of QueryAst

module QuerySemanticAnalysis =
    begin
        
        // recursively process the raw exp tree transforming it 
        // into a QueryAst bound to types and RTs
        let rec private r_analyze e (rt:ResourceType) = 
            match e with 
            | Exp.Element          -> QueryAst.Element, rt
            | Exp.Literal (edm, v, o) ->
                let literal = 
                    match edm with
                    | EdmPrimitives.Null      -> QueryAst.Null 
                    | EdmPrimitives.SString   -> QueryAst.Literal (typeof<string>, v)
                    | EdmPrimitives.Int16     -> QueryAst.Literal (typeof<int16>, o)
                    | EdmPrimitives.Int32     -> QueryAst.Literal (typeof<int32>, o)
                    | EdmPrimitives.Int64     -> QueryAst.Literal (typeof<int64>, o)
                    | EdmPrimitives.Single    -> QueryAst.Literal (typeof<float32>, o)
                    | EdmPrimitives.Decimal   -> QueryAst.Literal (typeof<decimal>, o)
                    | EdmPrimitives.Double    -> QueryAst.Literal (typeof<double>, o)
                    | EdmPrimitives.DateTime  -> QueryAst.Literal (typeof<DateTime>, o)
                    | EdmPrimitives.Boolean   -> QueryAst.Literal (typeof<bool>, Convert.ToBoolean(v))
                    | EdmPrimitives.Guid      -> QueryAst.Literal (typeof<Guid>, o)
                    | _ -> failwithf "Unsupported edm primitive type %O" edm
                    
                match literal with 
                | QueryAst.Literal (t,v)      -> literal, ResourceType.GetPrimitiveResourceType(t)
                | QueryAst.Null               -> literal, null
                | _ -> failwith "What kind of literal is that?!"

            | Exp.MemberAccess (ex, id) ->
                let name = 
                    match id with 
                    | Identifier i -> i
                    | _ -> failwith "Only Identifier nodes are supported as the rhs of a MemberAccess node"

                let get_prop (name:string) (rt:ResourceType) = 
                    match rt.Properties |> Seq.tryFind (fun p -> p.Name === name) with
                    | Some p -> p
                    | _ -> failwith "Property not found?"

                let root, nestedRt = r_analyze ex rt

                // rt.InstanceType.GetProperty(p.Name, BindingFlags.Public ||| BindingFlags.Instance)
                let prop = get_prop name nestedRt
                let propInfo = nestedRt.InstanceType.GetProperty(prop.Name, BindingFlags.Public ||| BindingFlags.Instance)

                QueryAst.PropertyAccess(root, propInfo, prop.ResourceType), prop.ResourceType


            | Exp.Binary (ex1, op, ex2) ->
                    
                let texp1, t1 = r_analyze ex1 rt
                let texp2, t2 = r_analyze ex2 rt

                // Unary Numeric promotions
                // A data service MAY support unary numeric promotions for the negation operator 
                // (negateExpression common expressions). Unary promotions consist of converting 
                // operands of type Edm.Byte or Edm.Int16 to Edm.Int32 and of type Edm.Single to Edm.Double.

                // Binary Numeric promotions
                // If supported, binary numeric promotion SHOULD implicitly convert both operands to a 
                // common type and, in the case of the nonrelational operators, also become the return type.
                // If supported, a data service SHOULD support binary numeric promotion for the following 
                // Entity Data Model (EDM) primitive types

                let cast_exp (exp:QueryAst) targetType = 
                    // * If binary numeric promotion is supported, a data service SHOULD use a castExpression to 
                    //   promote an operand to the target type.
                    if exp.GetExpType(rt) = targetType 
                    then exp
                    else QueryAst.UnaryExp(exp, UnaryOp.Cast, ResourceType.GetPrimitiveResourceType(targetType))

                let convert_to_bool (e1:QueryAst) (e2:QueryAst) = 
                    if e1.GetExpType(rt) <> typeof<bool> || e2.GetExpType(rt) <> typeof<bool> then
                        let newe1 = 
                            if e1.GetExpType(rt) = typeof<int32> 
                            then QueryAst.UnaryExp(e1, UnaryOp.Cast, ResourceType.GetPrimitiveResourceType(typeof<bool>))
                            else e1
                        let newe2 = 
                            if e2.GetExpType(rt) = typeof<int32> 
                            then QueryAst.UnaryExp(e2, UnaryOp.Cast, ResourceType.GetPrimitiveResourceType(typeof<bool>))
                            else e2
                        newe1, newe2
                    else e1, e2 

                let binary_numeric_promote (e1:QueryAst) (e2:QueryAst) originalRt = 
                    // If supported, binary numeric promotion SHOULD consist of the application of the 
                    // following rules in the order specified:
                    // * If either operand is of type Edm.Decimal, the other operand is converted 
                    //   to Edm.Decimal unless it is of type Edm.Single or Edm.Double.
                    // * Otherwise, if either operand is Edm.Double, the other operand is converted to type Edm.Double.
                    // * Otherwise, if either operand is Edm.Single, the other operand is converted to type Edm.Single.
                    // * Otherwise, if either operand is Edm.Int64, the other operand is converted to type Edm.Int64.
                    // * Otherwise, if either operand is Edm.Int32, the other operand is converted to type Edm.Int32
                    // * Otherwise, if either operand is Edm.Int16, the other operand is converted to type Edm.Int16.

                    if e1.GetExpType(rt) = typeof<decimal> || e2.GetExpType(rt) = typeof<decimal> then
                        cast_exp e1 typeof<decimal>, cast_exp e2 typeof<decimal>, ResourceType.GetPrimitiveResourceType (typeof<decimal>)
                        
                    elif e1.GetExpType(rt) = typeof<float> || e2.GetExpType(rt) = typeof<float> then
                        cast_exp e1 typeof<float>, cast_exp e2 typeof<float>, ResourceType.GetPrimitiveResourceType (typeof<float>)
                        
                    elif e1.GetExpType(rt) = typeof<float32> || e2.GetExpType(rt) = typeof<float32> then
                        cast_exp e1 typeof<float32>, cast_exp e2 typeof<float32>, ResourceType.GetPrimitiveResourceType (typeof<float32>)
                        
                    elif e1.GetExpType(rt) = typeof<int64> || e2.GetExpType(rt) = typeof<int64> then
                        cast_exp e1 typeof<int64>, cast_exp e2 typeof<int64>, ResourceType.GetPrimitiveResourceType (typeof<int64>)
                        
                    elif e1.GetExpType(rt) = typeof<int32> || e2.GetExpType(rt) = typeof<int32> then
                        cast_exp e1 typeof<int32>, cast_exp e2 typeof<int32>, ResourceType.GetPrimitiveResourceType (typeof<int32>)
                        
                    elif e1.GetExpType(rt) = typeof<int16> || e2.GetExpType(rt) = typeof<int16> then
                        cast_exp e1 typeof<int16>, cast_exp e2 typeof<int16>, ResourceType.GetPrimitiveResourceType (typeof<int16>)

                    else e1, e2, originalRt

                let assert_isnumeric (t:Type) = 
                    //t.IsPrimitive && 
                    ()

                let newExp1, newExp2, eqRt = 
                    match op with 
                    | BinaryOp.And
                    | BinaryOp.Or  ->
                        // suports booleans
                        let new1, new2 = convert_to_bool texp1 texp2
                        new1, new2, ResourceType.GetPrimitiveResourceType(typeof<bool>)

                    | BinaryOp.Add
                    | BinaryOp.Mul
                    | BinaryOp.Div
                    | BinaryOp.Mod
                    | BinaryOp.Sub -> 
                        // suports decimal, double single int32 and int64
                        // need to promote members if necessary
                        let new1, new2, newRt = binary_numeric_promote texp1 texp2 t1
                        assert_isnumeric (newRt.InstanceType)
                        assert_isnumeric (new1.GetExpType(rt))
                        assert_isnumeric (new2.GetExpType(rt))
                        new1, new2, newRt

                    | BinaryOp.Neq
                    | BinaryOp.Eq 
                    | BinaryOp.LessT 
                    | BinaryOp.GreatT
                    | BinaryOp.LessET
                    | BinaryOp.GreatET  -> 
                        // suports double single int32 int64 string datetime guid binary
                        let boolRt = ResourceType.GetPrimitiveResourceType(typeof<bool>)
                        let new1, new2, newRt = binary_numeric_promote texp1 texp2 boolRt 
                        new1, new2, boolRt

                    | _ -> failwith "Unknown binary operation"

                QueryAst.BinaryExp(newExp1, newExp2, op, eqRt), eqRt

            | Exp.Unary (op, exp) ->

                let exp1, expRt = r_analyze exp rt

                let newExp, eqRt = 
                    match op with 
                    | UnaryOp.Negate ->
                        // suports decimal, double single int32 and int64
                        // need to promote members if necessary
                        exp1, expRt

                    | UnaryOp.Not -> 
                        // suports bool only
                        exp1, ResourceType.GetPrimitiveResourceType(typeof<bool>)

                    // TODO: isofExpression
                    // TODO: cast
                    | _ -> failwith "Unknown unary operation"

                QueryAst.UnaryExp(newExp, op, eqRt), eqRt

            | _ -> failwithf "Unsupported exp type %O" e


        let analyze_and_convert (exp:Exp) (rt:ResourceType) : QueryAst = 

            let newTree, _ = r_analyze exp rt
            newTree

        let analyze_and_convert_orderby (exps:OrderByExp[]) (rt:ResourceType) : OrderByAst seq = 
            
            let convert exp = 
                match exp with
                | OrderByExp.Asc e  -> let ast, _ = r_analyze e rt in OrderByAst.Asc(ast)
                | OrderByExp.Desc e -> let ast, _ = r_analyze e rt in OrderByAst.Desc(ast)
                | _ -> failwithf "Unsupported OrderByExp type %O" exp

            exps |> Seq.map convert

        let analyze_and_convert_expand (exps:Exp[]) (rt:ResourceType) (properties:HashSet<ResourceProperty>) = 

            let rec resolve_property ast (rt:ResourceType) = 
                match ast with 
                | QueryAst.Element -> 
                    rt
                | QueryAst.PropertyAccess (source, name, res) ->
                    let target = resolve_property source rt 
                    let prop = target.Properties |> Seq.find (fun p -> p.Name = name.Name)
                    properties.Add prop |> ignore
                    res
                | _ -> failwithf "Unsupported QueryAst type %O" ast

            let properties = HashSet<ResourceProperty>()
            
            let convert exp = 
                let ast, _ = r_analyze exp rt
                resolve_property ast rt 

            exps |> Seq.iter (fun e -> convert e |> ignore)


    end

