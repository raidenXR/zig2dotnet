open System
open System.Text
open Zig2Dotnet
open Parser


let args = Environment.GetCommandLineArgs()
    
let fs = args[1]
let libname = args[2]
let generator = args[3]

let src = System.IO.File.ReadAllText(fs)
let ctx = Parser.parse src
let sb = StringBuilder(1024 * 8)

match generator with
| "-fs" ->
    sb
    |> FSGenerator.writeTemplate fs libname
    |> FSGenerator.writeParsedContext ctx
    |> ignore
    let a = fs.LastIndexOf("/") + 1
    let path = fs[a..fs.Length - 4] + "fs"
    use output = System.IO.File.CreateText(path)
    output.WriteLine(string sb)
    output.Close()
    printfn "file: %s generated" path

    let html_path = fs[a..fs.Length - 5] + "_doc_fs.html"
    DocGenerator.genDoc html_path ctx FSGenerator.docFns
    printfn "file: %s generated" html_path
| "-cs" ->
    sb
    |> CSGenerator.writeTemplate fs libname
    |> CSGenerator.writeParsedContext ctx
    |> ignore
    let a = fs.LastIndexOf("/") + 1
    let path = fs[a..fs.Length - 4] + "cs"
    use output = System.IO.File.CreateText(path)
    output.WriteLine(string sb)
    output.Close()
    printfn "file: %s generated" path

    let html_path = fs[a..fs.Length - 5] + "_doc_cs.html"
    DocGenerator.genDoc html_path ctx CSGenerator.docFns
    printfn "file: %s generated" html_path
| _ -> printfn "invalid use of args"





