namespace Zig2Dotnet
open System
open System.Text

module CSGenerator =

    let primitive_types = Map [
        "bool", "bool"
        "u8", "byte"
        "u16", "ushort"
        "u32", "int"
        "u64", "long"
        "i32", "int"
        "i64", "long"
        "f32", "float"
        "f64", "double"
        "c_int", "int"
        "isize", "nint"
        "usize", "nint"       
        "[]const u8", "string"
        "[*c][*c]const u8", "string[]"
        "[*c]const [*c]const u8", "string[]"
        "[*c]const u8", "string"
        "[*]const u8", "string"
        "[*c]u8", "byte*"
        "[*]u8", "byte*"
        "u8", "byte"
        "*anyopaque", "nint"
        "void", "void"        
    ]    

    let keywords = [
        "abstract"
        "as"
        "base"
        "bool"
        "break"
        "byte"
        "case"
        "catch"
        "char"
        "checked"
        "class"
        "const"
        "continue"
        "decimal"
        "default"
        "delegate"
        "do"
        "double"
        "else"
        "enum"
        "event"
        "explicit"
        "extern"
        "false"
        "finally"
        "fixed"
        "float"
        "for"
        "foreach"
        "goto"
        "if"
        "implicit"
        "in"
        "int"
        "interface"
        "internal"
        "is"
        "lock"
        "long"
        "namespace"
        "new"
        "null"
        "object"
        "operator"
        "out"
        "override"
        "params"
        "private"
        "protected"
        "public"
        "readonly"
        "ref"
        "return"
        "sbyte"
        "sealed"
        "short"
        "sizeof"
        "stackalloc"
        "static"
        "string"
        "struct"
        "switch"
        "this"
        "throw"
        "true"
        "try"
        "typeof"
        "uint"
        "ulong"
        "unchecked"
        "unsafe"
        "ushort"
        "using"
        "virtual"
        "void"
        "volatile"
        "while"
        "add"
        "and"
        "alias"
        "ascending"
        "args"
        "async"
        "await"
        "by"
        "descending"
        "dynamic"
        "equals"
        "file"
        "from"
        "get"
        "global"
        "group"
        "init"
        "into"
        "join"
        "let"
        "managed"
        "nameof"
        "nint"
        "not"
        "notnull"
        "nuint"
        "on"
        "or"
        "orderby"
        "partial"
        "partial"
        "record"
        "remove"
        "required"
        "scoped"
        "select"
        "set"
        "unmanaged"
        "unmanaged"
        "value"
        "var"
        "when"
        "where"
        "where"
        "with"
        "yield"
    ]
    
    let private (|IsPrimitive|IsPointer|IsPointerToMany|IsUserDefined|) (str:string) =
        if primitive_types.Keys.Contains(str) then IsPrimitive
        elif str.Contains("[*]") || str.Contains("[*c]") then IsPointerToMany
        elif str.Contains("*") then IsPointer
        else IsUserDefined

    
    let private (|>>) (sb:StringBuilder) (str:string) =
        sb.AppendLine(str)


    let writeTemplate (src_path:string) (libname:string) (sb:StringBuilder) =
        // let a = src_path.LastIndexOf("/")
        // let a = 0
        // let b = src_path.Length
        // let libname = src_path[a..b - 5]
        sb
        |>> "using System;"
        |>> "using System.Runtime.InteropServices;"
        |>> "using System.Diagnostics;"
        |>> ""
        |>> "namespace " + libname + ";"
        |>> ""
        |>> "public static unsafe class " + libname
        |>> "{"
        // |>> """    \\fix manually libname to have the correct name as of .so, .dll, -native binary of lib"""
        |>> "    const string libname = " + "\"" + libname + "\"" + ";"
        |>> ""
        

    let [<Literal>] attr = "    [StructLayout(LayoutKind.Sequential)]"
        
    /// transforms the name if nesseccary to avoid conflict with keywords
    let transformName (lhs:string) =
        if (List.contains lhs keywords) then "_" + lhs else lhs

    /// transform the Zig-type to appropriate F# type
    let transformType (rhs:string) =
        match rhs with
        | IsPrimitive -> primitive_types[rhs]
        | IsPointer -> "nint"
        | IsPointerToMany -> 
            let a = rhs.IndexOf("]") + 1
            let s = rhs[a..]
            s + "[]"
        | IsUserDefined -> rhs


    let writeParsedContext (ctx:ParsedContext) (sb:StringBuilder) =
        // gen all structs
        for _struct in ctx.structs do
            if _struct.summary.Length > 0 then
                ignore (sb.AppendLine("    ///<summary>"))
                for s in _struct.summary do
                    ignore (sb.AppendLine("    ///" + s))
                ignore (sb.AppendLine("    ///</summary>"))

            sb
            |>> attr
            |>> "    public struct " + _struct.name
            |>> "    {"
            |> ignore

            for field in _struct.fields do
                match field with
                | HtmlComment c ->
                    ignore (sb.Append("    ///<summary>").Append(c).AppendLine("</summary>"))
                | FieldOfStruct (lhs,rhs,op) ->
                    let n = transformName lhs
                    let t = transformType rhs
                    ignore (sb.Append("        public ").Append(t).Append(" ").Append(n).AppendLine(","))
                | _ -> ()

            sb
            |>> "    }"
            |>> ""
            |> ignore


        // gen all enums
        for _enum in ctx.enums do
            if _enum.summary.Length > 0 then
                ignore (sb.AppendLine("    ///<summary>"))
                for s in _enum.summary do
                    ignore (sb.AppendLine("    ///" + s))
                ignore (sb.AppendLine("    ///</summary>"))
            
            sb
            |>> attr
            |>> "    public enum " + _enum.name
            |>> "    {"
            |> ignore
            
            for field in _enum.fields do
                match field with
                | HtmlComment c ->
                    ignore (sb.Append("    ///<summary>").Append(c).AppendLine("</summary>"))
                | FieldOfEnum (lhs,op) ->
                    let n = transformName lhs
                    match op with 
                    | Some v -> 
                        ignore (sb.Append("        " + n + " = ").Append(string v).AppendLine(","))
                    | None ->
                        ignore (sb.Append("        " + n + ","))
                | _ -> ()

            sb
            |>> "    }"
            |>> ""
            |> ignore

        // gen all fns
        for _fn in ctx.fns do
            if _fn.summary.Length > 0 then
                ignore (sb.AppendLine("    ///<summary>"))
                for s in _fn.summary do
                    ignore (sb.AppendLine("    ///" + s))
                ignore (sb.AppendLine("    ///</summary>"))

            ignore (sb.AppendLine("    [DllImport(libname)]"))

            let r = transformType _fn.ret
            ignore (sb.Append("    public static extern unsafe " + r + " " + _fn.name).Append(" ("))

            for arg in _fn.args do
                match arg with
                | ArgOfFn (lhs,rhs) ->
                    let n = transformName lhs
                    let t = transformType rhs
                    ignore (sb.Append(t).Append(" ").Append(n).Append(", "))
                | _ -> ()

            if _fn.args.Length > 0 then ignore (sb.Remove(sb.Length - 2, 2))
            ignore (sb.AppendLine(");").AppendLine(""))
            
        ignore (sb.AppendLine("}"))
        sb

    /// docFn function declaration for FS-autogen
    let docFns (ctx:ParsedContext) (sb:StringBuilder) =
        ignore (sb.AppendLine("  <h3>Functions</h3>").AppendLine("  <dl>"))
        for _fn in ctx.fns do
            ignore (sb.Append("    <dt><code>"))            
            let r = transformType _fn.ret
            ignore (sb.Append("    public static extern unsafe " + r + " ").Append("</code><fnName>").Append(_fn.name).Append("</fnName> ("))

            for arg in _fn.args do            
                match arg with
                | ArgOfFn (lhs,rhs) ->
                    let n = transformName lhs
                    let t = transformType rhs
                    ignore (sb.Append("<code>").Append(t).Append("</code>").Append(" ").Append(n).Append(", "))
                | _ -> ()

            if _fn.args.Length > 0 then ignore (sb.Remove(sb.Length - 2, 2))
            ignore (sb.Append(");").Append(""))
            ignore (sb.AppendLine(" <dt>"))

            // write the summary of the fn below the fn definition
            ignore (sb.Append("    <dd>  "))
            for s in _fn.summary do
                ignore (sb.Append("  ").Append(s).AppendLine(" </br>"))
            ignore (sb.AppendLine("    </dd>"))
                
        ignore (sb.AppendLine("</dl>"))
        sb
