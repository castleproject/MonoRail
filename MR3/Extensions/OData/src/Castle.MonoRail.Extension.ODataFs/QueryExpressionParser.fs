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
open System.Globalization
open System.Web
open Castle.MonoRail
open FParsec
open FParsec.Primitives
open FParsec.CharParsers


type BinaryOp = 
    | And   = 0
    | Or    = 1
    | Eq    = 2
    | Neq   = 3
    | Mul   = 4
    | Div   = 5
    | Mod   = 6
    | Add   = 7
    | Sub   = 8
    | LessT = 9
    | GreatT = 10
    | LessET = 11
    | GreatET= 12

type UnaryOp = 
    | Negate = 0
    | Not    = 1
    | Cast   = 2
    // | IsOf

type EdmPrimitives = 
    | Null = 0
    | Binary = 1
    | Boolean = 2
    | Byte = 3
    | DateTime = 4
    | Decimal = 5
    | Double = 6
    | Single = 7
    | Guid = 8
    | Int16 = 9
    | Int32 = 10
    | Int64 = 11
    | SByte = 12
    | SString = 13
    | Time = 14
    | String = 15
    | DateTimeOffset = 16
    

type Exp = 
    | Identifier of string
    | Element 
    | Literal of EdmPrimitives * string * obj
    | MemberAccess of Exp * Exp  // string * (string list) option
    | Unary  of UnaryOp * Exp
    | Binary of Exp * BinaryOp * Exp
    // | MethodCall
    // | Cast
    // | IsOf
    // | FuncCall
    with 
        member x.ToStringTree() = 
            let b = StringBuilder()
            let rec print (n) (level:int) = 
                b.AppendLine() |> ignore
                for x in 1..level do b.Append("  ") |> ignore
                match n with 
                | Identifier f          -> b.Append (sprintf "Id %s" f) |> ignore
                | Element               -> b.Append (sprintf "Element") |> ignore
                | Literal (t,s,v)       -> 
                    if v = null 
                    then b.Append (sprintf "Literal %O [%s]" t s ) |> ignore
                    else
                        if v :? DateTime then
                            let dt = v :?> DateTime
                            b.Append (sprintf "Literal %O [%O]" t (dt.ToString(CultureInfo.InvariantCulture))) |> ignore
                        else b.Append (sprintf "Literal %O [%O]" t v ) |> ignore

                | MemberAccess (ex,n)   -> 
                    b.Append (sprintf "MemberAccess " ) |> ignore
                    print ex (level + 1)
                    print n (level + 1)
                | Unary (op,ex)         -> 
                    b.Append (sprintf "Unary %O " op) |> ignore
                    print ex (level + 1)
                | Binary (ex1, op, ex2) -> 
                    b.Append (sprintf "Binary %O " op) |> ignore
                    print ex1 (level + 1)
                    print ex2 (level + 1)
                | _ -> failwithf "Unsupported node type? Need to update this match, dude"
            print x 1
            b.ToString()


type OrderByExp =
    | Asc of Exp
    | Desc of Exp 


// rename to queryparser instead?
module QueryExpressionParser =
    begin
        (*
                        OData/Operator Precedence
        Grouping                (x)                     parenExpression, boolParenExpression
        Primary                 x/m                     memberExpression
        Primary                 x(...)                  methodCallExpression, boolMethodCallExpression
        Unary                   -x                      negateExpression
        Unary                   not x                   notExpression
        Unary                   cast(T), cast(x, T)     castExpression
        Multiplicative          x mul y                 mulExpression
        Multiplicative          x div y                 divExpression
        Multiplicative          x mod y                 modExpression
        Additive                x add y                 addExpression
        Additive                x sub y                 subExpression
        Relational/ttesting     x lt y                  ltExpression
        Relational/ttesting     x gt y                  gtExpression
        Relational/ttesting     x le y                  leExpression
        Relational/ttesting     x ge y                  geExpression
        Relational/ttesting     isof(T), isof(x, T)     isofExpression
        Equality                x eq y                  eqExpression
        Equality                x ne y                  neExpression
        Conditional AND         x and y                 andExpression
        Conditional OR          x or y                  orExpression

        *)

        // Rewrites the tree to be rooted with MemberAccess(Element instead)
        // not ideal, but couldnt get fparsec to produce the desired tree 
        let rec rebuildMemberAccessTree exp = 
            match exp with 
            | MemberAccess (r, arg) -> MemberAccess(rebuildMemberAccessTree r, arg)
            | Identifier i          -> MemberAccess(Exp.Element, exp)
            | _                     -> failwithf "Not supposed to traverse any other node type, but got into %O" exp

        let ws      = spaces
        // let nospace = preturn ()
        let pc c            = ws >>. pchar c
        let pstr s          = ws >>. pstring s
        let pstrCI s        = ws >>. pstringCI s
        let lparen          = pstring "(" >>. ws
        let rparen          = pstring ")" >>. ws
        let opp             = new OperatorPrecedenceParser<_,_,_>()
        let ida             = identifier(IdentifierOptions())
        let entity          = ws >>. ida .>> manyChars (noneOf "/")
        let squote          = pstring "'"
        let shyphen         = pstring "-"
        let scolon          = pstring ":"

        // Address               MemberAccess(element, PriceAddress)
        // Address/Name          MemberAccess(MemberAccess(element, Address), Name)
        // Address/Name/Length   MemberAccess(MemberAccess(MemberAccess(element, Address), Name), Length)
        let combine         = stringReturn "/" (fun x y -> Exp.MemberAccess(x, y))
        let idAsExp         = ida .>> ws |>> (fun id -> Exp.Identifier(id))
        let memberAccessExp = chainl1 (idAsExp) (combine)
        
        let sign            = pstr "-" <|>% ""

        let nullLiteral     = pstr "null" .>> ws |>> fun _ -> Exp.Literal(EdmPrimitives.Null, null, null)

        // guidUriLiteral= "guid" SQUOTE guidLiteral SQUOTE
        // guidLiteral = 8*HEXDIG "-" 4*HEXDIG "-" 4*HEXDIG "-" 12*HEXDIG 
        // 80749f18-d2f1-47e5-b1d0-4169c10125b5
        //                    ^
        let guidLiteral     = pstring "guid" .>> squote >>. 
                                pipe5
                                    (manyMinMaxSatisfy 8 8 isHex .>> shyphen) 
                                    (manyMinMaxSatisfy 4 4 isHex .>> shyphen) 
                                    (manyMinMaxSatisfy 4 4 isHex .>> shyphen) 
                                    (manyMinMaxSatisfy 4 4 isHex .>> shyphen) 
                                    (manyMinMaxSatisfy 12 12 isHex) 
                                    (fun v1 v2 v3 v4 v5 -> sprintf "%s-%s-%s-%s-%s" v1 v2 v3 v4 v5) .>> squote .>> ws
                                |>> fun g -> Exp.Literal(EdmPrimitives.Guid, null, Guid.Parse(g))

        let toInt v         = Int32.Parse(v) 

        //                    year "-" month "-" day 
        let datePart        = pipe3 
                                (manyMinMaxSatisfy 4 4 isDigit .>> shyphen |>> toInt) 
                                (manyMinMaxSatisfy 1 2 isDigit .>> shyphen |>> toInt)
                                (manyMinMaxSatisfy 1 2 isDigit |>> toInt) 
                                (fun year month day -> DateTime(year, month, day))
        
        //                    [":" second ["." nanoSeconds]]
        let secondsNanoPart = opt ( pchar ':' >>. 
                                    (manyMinMaxSatisfy 1 2 isDigit |>> toInt) .>>. 
                                        (opt (pchar '.' >>. manyMinMaxSatisfy 1 7 isDigit |>> toInt)))
                                |>> fun (sec) -> 
                                    match sec with 
                                    | Some (secs, nano) -> 
                                        match nano with | Some n -> (secs, n) | _ -> (secs, 0)
                                    | _ -> (0,0)

        //                    hour ":" minute [":" second ["." nanoSeconds]]
        let timePart        = pipe3 (manyMinMaxSatisfy 1 2 isDigit .>> scolon |>> toInt) 
                                    (manyMinMaxSatisfy 1 2 isDigit |>> toInt) 
                                    secondsNanoPart
                                    ( fun hour minute secs -> DateTime(1999, 1, 1, hour, minute, fst secs, snd secs))
                                                        

        // dateTimeLiteral = year "-" month "-" day "T" hour ":" minute [":" second ["." nanoSeconds]]
        let datetimeLiteral = pstring "datetime" .>> squote >>. 
                                pipe2 (datePart .>> pchar 'T') timePart 
                                    (fun date time -> DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond)) .>> squote .>> ws
                                |>> fun g -> Exp.Literal(EdmPrimitives.DateTime, null, g)

        let signedIntPart   = (sign .>>. many1Chars digit)      |>> fun (s,v) -> Single.Parse(s + v)
        let optDecimalPart  = (pchar '.' >>. many1Chars digit   |>> fun v -> Single.Parse("0." + v)) 
                              <|>% 0.0f
        let decimalPart     = (pchar '.' >>. many1Chars digit   |>> fun v -> Single.Parse("0." + v))

        let intLiteral      = (sign .>>. many1Chars digit) .>> ws 
                                |>> fun (c,v) -> Exp.Literal(EdmPrimitives.Int32, null, Int32.Parse(c + v))
        let int64Literal    = (sign .>>. many1Chars digit .>> pstringCI "l") .>> ws 
                                |>> fun (c,v) -> Exp.Literal(EdmPrimitives.Int64, null, Int64.Parse(c + v))

        // decimalUriLiteral = decimalLiteral ("M"/"m")
        // decimalLiteral = sign 1*29DIGIT ["." 1*29DIGIT]
        let decLiteral      = pipe3 signedIntPart optDecimalPart (pstringCI "m") 
                                (fun i d _ -> (decimal) (i + d)) .>> ws 
                                |>> fun d -> Exp.Literal(EdmPrimitives.Decimal, null, d)

        (* 
        singleLiteral = nonDecimalPoint
                        / nonExp
                        / exp
                        / nan
                        / negativeInfinity
                        / postiveInfinity
        nonDecimalPoint = sign 1*8DIGIT
        nonExpDecimal = sign *DIGIT "." *DIGIT 
        expDecimal = sign 1*DIGIT "." 8DIGIT ("e" / "E") sign 1*2DIGIT        
        *)
        let singleLiteral   = ((attempt(pipe2 signedIntPart decimalPart (fun i d -> (float32) (i + d))))
                                <|> (signedIntPart .>> pstrCI "f" |>> fun i -> (float32) i) ) .>> ws
                              |>> fun d -> Exp.Literal(EdmPrimitives.Single, null, d)
        
        (* 
        doubleLiteral = nonDecimalPoint
                        / nonExp
                        / exp
                        / nan
                        / negativeInfinity
                        / postiveInfinity
                        ("D" / "d")
        nonDecimalPoint = sign 1*17DIGIT
        nonExpDecimal   = sign* DIGIT "." *DIGIT 
        expDecimal      = sign 1*DIGIT "." 16DIGIT ("e" / "E") sign 1*3DIGIT
        *)
        let doubleLiteral   = pipe3 signedIntPart optDecimalPart (pstringCI "d") 
                                (fun i d _ -> (float) (i + d))  .>> ws
                                |>> fun d -> Exp.Literal(EdmPrimitives.Double, null, d)

        let stringLiteral   = between (pc '\'') (pchar '\'') 
                                    (many1Chars (noneOf "'")) .>> ws |>> fun en -> Exp.Literal(EdmPrimitives.SString, en, null)
        
        let boolLiteral     = (pstr "true" <|> pstr "false") .>> ws  |>> fun v  -> Exp.Literal(EdmPrimitives.Boolean, v, null)
        let binaryLiteral   = (pstr "X" <|> pstrCI "binary") >>. squote >>. (many1Chars hex) .>> squote .>> ws 
                                                                     |>> fun v  -> Exp.Literal(EdmPrimitives.Binary, v, null)
        
        let literalExp      = choice [  
                                        nullLiteral
                                        binaryLiteral
                                        datetimeLiteral
                                        guidLiteral
                                        attempt(decLiteral) 
                                        attempt(doubleLiteral) 
                                        attempt(singleLiteral) 
                                        attempt(int64Literal)
                                        intLiteral 
                                        stringLiteral 
                                        boolLiteral
                                     ]

        let tryBetweenParens p = lparen >>? (p .>>? rparen)

        let exp             = opp.ExpressionParser
        let memberAccess    = memberAccessExp |>> rebuildMemberAccessTree
        let units           = literalExp <|> tryBetweenParens exp <|> memberAccess
      
        opp.TermParser <- units

        let term = exp .>> eof



        let orderbyTerm = memberAccess .>>. 
                            ( pstringCI "asc" |>> (fun _ -> "asc") <|> pstringCI "desc" |>> (fun _ -> "desc") <|>% "asc")
                            |>> fun (m,o) -> if o = "asc" then OrderByExp.Asc(m) else OrderByExp.Desc(m)

        let orderByUnit = sepBy1 orderbyTerm (pc ',' .>> ws)

        // ShipCountry , Else [asc|desc]
        // ShipCountry ne 'France' desc
        // "=" [WSP] commonExpression [WSP] [asc / desc] 
        //     *( "," [WSP] commonExpression [WSP] [asc / desc])
        let orderby = orderByUnit .>> eof
        
        opp.AddOperator(InfixOperator("and",  ws, 1 , Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.And, y)))
        opp.AddOperator(InfixOperator("or",   ws, 2 , Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.Or, y)))

        opp.AddOperator(InfixOperator("eq",   ws, 3 , Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.Eq, y)))
        opp.AddOperator(InfixOperator("ne",   ws, 4 , Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.Neq, y)))
        
        opp.AddOperator(InfixOperator("lt" ,  ws, 5, Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.LessT, y)))
        opp.AddOperator(InfixOperator("gt" ,  ws, 6, Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.GreatT, y)))
        opp.AddOperator(InfixOperator("le" ,  ws, 7, Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.LessET, y)))
        opp.AddOperator(InfixOperator("ge" ,  ws, 8, Associativity.Right, fun x y -> Exp.Binary(x, BinaryOp.GreatET, y)))
        // opp.AddOperator(PrefixOperator("isof",nospace, 70, false, fun x -> Exp.Unary(UnaryOp.IsOf, x))

        opp.AddOperator(InfixOperator("add",  ws, 9, Associativity.Left, fun x y -> Exp.Binary(x, BinaryOp.Add, y)))
        opp.AddOperator(InfixOperator("sub",  ws, 10, Associativity.Left, fun x y -> Exp.Binary(x, BinaryOp.Sub, y)))

        opp.AddOperator(InfixOperator("mul",  ws, 11, Associativity.Left, fun x y -> Exp.Binary(x, BinaryOp.Mul, y)))
        opp.AddOperator(InfixOperator("div",  ws, 12, Associativity.Left, fun x y -> Exp.Binary(x, BinaryOp.Div, y)))
        opp.AddOperator(InfixOperator("mod",  ws, 13, Associativity.Left, fun x y -> Exp.Binary(x, BinaryOp.Mod, y)))
        
        opp.AddOperator(PrefixOperator("-",   ws, 14, false, fun x -> Exp.Unary(UnaryOp.Negate, x)))
        opp.AddOperator(PrefixOperator("not", ws, 15, false, fun x -> Exp.Unary(UnaryOp.Not, x)))
        // opp.AddOperator(PrefixOperator("cast", nospace, 100, false, fun x -> Exp.Unary(UnaryOp.Cast, x))

        let parse_orderby (original:string) = 
            let r = 
                match run orderby original with 
                | Success(result, _, _) -> result |> Array.ofList
                | Failure(errorMsg, _, _) -> (raise(ArgumentException(errorMsg)))
            r 

        let parse_filter (original:string) = 
            let r = 
                match run term original with 
                | Success(result, _, _) -> result
                | Failure(errorMsg, _, _) -> (raise(ArgumentException(errorMsg)))
            r

    end

(* 
isofExpression      = "isof" [WSP] "("[[WSP] commonExpression [WSP] ","][WSP]stringLiteral [WSP] ")"
castExpression      = "cast" [WSP] "("[[WSP] commonExpression [WSP] ","][WSP]stringLiteral [WSP] ")"
boolCastExpression  = "cast" [WSP] "("[[WSP] commonExpression [WSP] ","][WSP] "Edm.Boolean" [WSP] ")"

methodCallExpression = boolMethodExpression
                        / indexOfMethodCallExpression
                        / replaceMethodCallExpression
                        / toLowerMethodCallExpression
                        / toUpperMethodCallExpression
                        / trimMethodCallExpression
                        / substringMethodCallExpression
                        / concatMethodCallExpression
                        / lengthMethodCallExpression
                        / yearMethodCallExpression
                        / monthMethodCallExpression
                        / dayMethodCallExpression
                        / hourMethodCallExpression
                        / minuteMethodCallExpression
                        / secondMethodCallExpression
                        / roundMethodCallExpression
                        / floorMethodCallExpression
                        / ceilingMethodCallExpression

boolMethodExpression = endsWithMethodCallExpression
                        / startsWithMethodCallExpression
                        / substringOfMethodCallExpression
                        
endsWithMethodCallExpression = "endswith" [WSP]
                                "(" [WSP] commonexpression [WSP]
                                "," [WSP] commonexpression [WSP] ")"
indexOfMethodCallExpression = "indexof" [WSP]
                            "(" [WSP] commonexpression [WSP]
                            "," [WSP] commonexpression [WSP] ")"
replaceMethodCallExpression = "replace" [WSP]
                            "(" [WSP] commonexpression [WSP]
                            "," [WSP] commonexpression [WSP]
                            "," [WSP] commonexpression [WSP] ")"
startsWithMethodCallExpression = "startswith" [WSP]
                                "(" [WSP] commonexpression [WSP]
                                "," [WSP] commonexpression [WSP] ")"
toLowerMethodCallExpression = "tolower" [WSP]
                                "(" [WSP] commonexpression [WSP] ")"
toUpperMethodCallExpression = "toupper" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
trimMethodCallExpression = "trim" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
substringMethodCallExpression = "substring" [WSP]
                                "(" [WSP] commonexpression [WSP]
                                [ "," [WSP] commonexpression [WSP] ] ")"					
substringOfMethodCallExpression = "substringof" [WSP]
                                "(" [WSP] commonexpression [WSP]
                                [ "," [WSP] commonexpression [WSP] ] ")"
concatMethodCallExpression = "concat" [WSP]
                            "(" [WSP] commonexpression [WSP]
                            [ "," [WSP] commonexpression [WSP] ] ")"
lengthMethodCallExpression = "length" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
yearMethodCallExpression = "year" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
monthMethodCallExpression = "month" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
dayMethodCallExpression = "day" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
hourMethodCallExpression = "hour" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
minuteMethodCallExpression = "minute" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
secondMethodCallExpression = "second" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
roundMethodCallExpression = "round" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
floorMethodCallExpression = "floor" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"
ceilingMethodCallExpression = "ceiling" [WSP]
                            "(" [WSP] commonexpression [WSP] ")"					
*)