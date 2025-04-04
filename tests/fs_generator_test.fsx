#load "../src/zig_parser.fs"
#load "../src/fs_generator.fs"

open System
open System.Text
open Zig2Dotnet
open FSGenerator

let fs = "mkxk.zig"
let libname = "libmkxk.so"
let src = System.IO.File.ReadAllText fs

let ctx = Parser.parse(src)
let sb = StringBuilder(1024 * 8)

sb
|> writeTemplate fs libname
|> writeParsedContext ctx
|> (fun x -> printfn "%s" (string sb))
