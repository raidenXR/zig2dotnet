#load "../src/zig_parser.fs"
// #load "../src/fs_generator.fs"

open System
open Zig2Dotnet
open Parser

let [<Literal>] str0 = "   some single line string   "
let slice = StrSlice(str0.ToCharArray())

// printfn "-%s-" (slice |> trim |> toString)
// printfn "-%s-" (slice |> (extract "me" "str") |> toString)


let [<Literal>] some_str = 
    """
    const SomeEnum = enum(c_int) {
        a,
        b,
        c,
        d,    
    };
    
some string to test parser
some other line
some third line
    """

let s0 = StrSlice(some_str.ToCharArray())
// printfn "equals: %A" (equal s0 some_str)

// let struct(lines,len) = split '\n' s0
// for i in 0..len - 1 do 
//     let fl = lines[i] |> trim
//     let idx = indexOf "," fl
//     // let idx = getIndex "," fl
//     let str_idx = if idx.IsValueSome then (string idx.Value) else "no idx"
//     printfn "idx: %s, -%s-" str_idx (toString fl)
//     // printfn "idx:%d, -%s-" idx (toString fl)

// let s = StrSlice(some_str.ToCharArray(), 10, some_str.Length - 10)
// let line = readLine s

// printfn "%s" (toString line)
// printfn "%d" ((indexOf "line" s).Value)

let src = System.IO.File.ReadAllText("mkxk.zig")

let context = Parser.parse src


