#load "../src/zig_parser.fs"
#load "../src/doc_generator.fs"
#load "../src/fs_generator.fs"
#load "../src/cs_generator.fs"

open System
open System.Text
open Zig2Dotnet
open Parser
open DocGenerator


let fs = "mkxk.zig"
let src = System.IO.File.ReadAllText fs

let ctx = Parser.parse(src)

DocGenerator.genDoc "mkxk_doc_fs.html" ctx FSGenerator.docFns
DocGenerator.genDoc "mkxk_doc_cs.html" ctx CSGenerator.docFns
