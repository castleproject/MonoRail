
module AssemblyLevelDeclarations

open System.Reflection
open System.Security
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyVersion("0.0.1.0")>]
[<assembly: AssemblyFileVersion("0.0.1.0")>]
// [<assembly: InternalsVisibleToAttribute("Castle.MonoRail.Tests") >]


[<assembly: System.Web.PreApplicationStartMethod(typeof<Castle.MonoRail.ViewEngines.Blade.BladeViewEngine>, "Initialize")>]


do()