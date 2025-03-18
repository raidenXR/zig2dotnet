open System
open System.IO
open System.Text
open System.Linq
open Parser
open Core


let parseFile() =
    let args = Environment.GetCommandLineArgs()
    if args.Length = 2 then
        documentation args[1]
        stdout "html documentation created"
        Environment.Exit(0)
    elif args.Length < 5 then
        stdout """invalid arguments\n[filepath] [libname] [type: -fs|-cs] [destination]"""
        Environment.Exit(0)
    
    let src = args[1]
    let libname = args[2]
    let generator = args[3]
    let prefix = args[4]

    Console.WriteLine($"generator: {generator}")

    if(src.Contains(".zig")) then
        let lines = readLines src
        let enums = getEnums lines
        let structs = getStructs lines
        let functions = getFunctions lines
        let enums_c, structs_c, fns_c = (enums.Count(), structs.Count(), functions.Count())
        stdout $"enums: {enums_c}, structs: {structs_c}, functions: {fns_c}"

        let filename = 
            match generator with
            | "-fs" -> src[0..^4] + ".fs"
            | "-cs" -> src[0..^4] + ".cs"
            | _ -> raise (invalidArg "-generator" $"invalid type {generator}")
    
        let sb = new StringBuilder(10 * 1024)
        use fs = File.CreateText(prefix + (suffix filename))
        let pwd = System.IO.Path.GetDirectoryName(".");
        let current_path = System.IO.Path.Combine(pwd, prefix + (suffix filename))
        // Console.WriteLine("generating bindings in directory: " + current_path)
        match generator with
        | "-fs" -> do 
            sb |> FSharp.writeTemplate libname |> string |> fs.Write
            enums |> Seq.map (fun e -> FSharp.transformEnum e sb) |> Seq.iter (fun e -> fs.WriteLine(e))
            structs |> Seq.map (fun s -> FSharp.transformStruct s sb) |> Seq.iter (fun s -> fs.WriteLine(s))
            functions |> Seq.map (fun f -> FSharp.transformFunction f sb) |> Seq.iter (fun f -> fs.WriteLine(f))
            openDoc ".fs" args[1]
            |> documentEnums enums
            |> documentStructs structs
            |> FSharp.documentFunctions functions
            |> closeDoc
            
        | "-cs" -> do
            sb |> CSharp.writeTemplate libname |> string |> fs.WriteLine
            enums |> Seq.map (fun e -> CSharp.transformEnum e sb) |> Seq.iter (fun e -> fs.WriteLine(e))
            structs |> Seq.map (fun s -> CSharp.transformStruct s sb) |> Seq.iter (fun s -> fs.WriteLine(s))
            functions |> Seq.map (fun f -> CSharp.transformFunction f sb) |> Seq.iter (fun f -> fs.WriteLine(f))
            fs.WriteLine("}")           
            openDoc ".cs" args[1]
            |> documentEnums enums
            |> documentStructs structs
            |> CSharp.documentFunctions functions
            |> closeDoc
        | _ -> raise (invalidArg "-generator" $"invalid type {generator}")
        
        fs.Flush()             
    else stdout """invalid arguments\n[filepath] [libname] [type: -fs|-cs] [destination]"""



let printFile () =
    let args = Environment.GetCommandLineArgs()
    let src = args[1]
    let generator = args[3]
    let filename = 
        match generator with
        | "-fs" -> src[0..^4] + ".fs"
        | "-cs" -> src[0..^4] + ".cs"
        | _ -> raise (invalidArg "-generator" $"invalid type {generator}")    
    let read = File.ReadAllText(filename)
    stdout read
    

parseFile()

