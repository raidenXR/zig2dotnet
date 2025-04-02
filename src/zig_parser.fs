namespace Zig2Dotnet
open System
open System.Buffers

type StrSlice = ArraySegment<char> 

type Elem =
    | Comment of string
    | HtmlComment of string
    | FieldOfStruct of string * string * option<string>
    | FieldOfEnum of string * option<int>
    | ArgOfFn of string * string


type StructDecl = {
    summary: list<string>
    name: string
    fields: list<Elem>
}

type EnumDecl = {
    summary: list<string>
    name: string
    fields: list<Elem>
}

type FnDecl = {
    summary: list<string>
    name: string
    args: list<Elem>
    ret: string
}

type ParsedContext = {
    enums: list<EnumDecl>
    structs: list<StructDecl>
    fns: list<FnDecl>
}

    

module Parser = 
    open System
    open System.Text
    open System.Linq


    /// a collection of all types present in zig file
    let internal primitive_types = [
        "c_int"
        "c_uint"
        "isize"
        "usize"
        "f32"
        "f64"
        "u16"
        "u32"
        "u64"
        "i16"
        "i32"
        "i64"
        "u8"
        "bool"
    ]

    let internal user_defined_types = ResizeArray<string>()

    let toString (s:StrSlice) =
        String(s.Array.AsSpan(s.Offset, s.Count))
            

    /// return a slice upto '\n'
    let readLine (src:StrSlice) =
        let mutable n = 0
        while src[n] <> '\n' && n < src.Count do
            n <- n + 1
        src.Slice(0, n)

    /// returns the slice of previous line
    let readPrevLine (src:StrSlice) =
        if src.Offset > 3 then 
            let mutable n = src.Offset - 2
            while src.Array[n] <> '\n' && n > 1 do
                n <- n - 1
            StrSlice(src.Array, n + 1, src.Offset - 2 - n)
        else
            StrSlice()

    let readSlice (src:StrSlice) (c:char) =
        let mutable n = 0
        let mutable b = false
        while n < src.Count && (not b) do
            b <- if src[n] = c then true else false
            n <- n + 1
        StrSlice(src.Array, src.Offset, n)

    /// advances the offset of a StrSlice
    let advance (slice:byref<StrSlice>) (offset:int) =
        slice <- StrSlice(slice.Array, slice.Offset + offset, slice.Count - offset)
    
    /// removes trailing and prefix whitespace character
    let trim (slice:StrSlice) =
        let mutable a = 0
        let mutable b = slice.Count - 1
        while slice[a] = ' ' || slice[a] = '\n' && a < b do a <- a + 1
        while slice[b] = ' ' || slice[b] = '\n' && b > 1 do b <- b - 1
        let l = min a b
        let r = max a b
        StrSlice(slice.Array, slice.Offset + l, r - l + 1)

    let trimFromStart (n:int) (slice:StrSlice) =
        slice.Slice(n)

    let trimFromEnd (n:int) (slice:StrSlice) =
        slice.Slice(0, slice.Count - n)


    /// compares the characters of s StringSlice with a sting
    let equal (a:StrSlice) (b:string) =
        let mutable r = if b.Length = 1 then a[0] = b[0] else a.Count = b.Length
        let mutable i = 0
        while i < a.Count && r do
            r <- r && a[i] = b[i]
            i <- i + 1
        r            

    let exists (slice:StrSlice) (collection:seq<string>) =
        let mutable b = false
        for s in collection do
            b <- b || (equal slice s)
        b
    
    [<Obsolete>]
    let getIndex (str:string) (slice:StrSlice) =
        let mutable idx = -1
        let mutable i = 0
        let mutable b = str.Length = slice.Count
        while i + str.Length <= slice.Count && (not b) do
            b <- equal (slice.Slice(i, str.Length)) str 
            idx <- if b then i else idx
            i <- i + 1
        idx        
    
    let indexOf (str:string) (slice:StrSlice) =
        let mutable i = 0
        let mutable b = str.Length = slice.Count
        let mutable idx = if b && (equal slice str) then 0 else -1
        while i + str.Length <= slice.Count && (not b) do
            b <- equal (slice.Slice(i, str.Length)) str
            idx <- if b then i else idx
            i <- i + 1
        if idx >= 0 then ValueSome idx else ValueNone

    
    let contains (str:string) slice =
        match indexOf str slice with
        | ValueSome _ -> true
        | ValueNone -> false

    let startsWith (slice:StrSlice) (str:string) =
        let s = StrSlice(slice.Array, slice.Offset, str.Length)
        equal s str

    /// returns the in between StrSlice of two strings
    let extract (str_a:string) (str_b:string) (slice:StrSlice) =
        match (indexOf str_a slice), (indexOf str_b slice) with
        | ValueSome idx_l, ValueSome idx_r ->
            slice.Slice(idx_l + str_a.Length, idx_r - (idx_l + str_a.Length))
        | ValueNone, _ -> 
            // slice
            failwith $"{str_a} does not exist in {toString slice}"
        | _, ValueNone ->
            // slice
            failwith $"{str_b} does not exist in {toString slice}"        


    /// uses an array pool, so make sure to return the slice back
    let internal split (op:char) (slice:StrSlice) =
        let buffer = ArrayPool<StrSlice>.Shared.Rent(50)
        let mutable i = 0
        let mutable n = 0
        let mutable t = i
        while i < slice.Count do
            if slice[i] = op && i > t then
                buffer[n] <- slice.Slice(t, i - t)
                t <- i + 1
                n <- n + 1
            i <- i + 1
        struct(buffer,n)


    let (|FnDeclaration|EnumDeclaration|StructDeclaration|NoDeclaration|) (line:StrSlice) =
        if line.Count = 0 then NoDeclaration
        elif contains "export fn" line then FnDeclaration
        elif contains "extern struct" line then StructDeclaration
        elif contains "enum(c_int)" line then EnumDeclaration
        else NoDeclaration

    let (|IsPrimitive|IsStruct|IsPointer|) line = 
        if (exists line user_defined_types) then IsStruct
        elif (exists line primitive_types) then IsPrimitive
        elif (startsWith line "*") || (startsWith line "[*]") || (startsWith line "[*c]") then IsPointer
        else failwith "not appropriate case"       
                
    let parseField (s:StrSlice) =
        match indexOf ":" s with
        | ValueSome idx ->
            let trimmed_line = trim s
            let lhs = trimmed_line.Slice(0,idx)
            let rhs = trimmed_line.Slice(idx, trimmed_line.Count - idx - 1)
            (toString lhs, toString rhs)
        | ValueNone -> 
            failwith "failed to parse field, does not contain ':'"
        

    let readSummary (line:StrSlice) =
        let mutable count = 0
        let buffer = ArrayPool<StrSlice>.Shared.Rent(60)
        let mutable prev_line = readPrevLine line
        while contains "///" prev_line do
            buffer[count] <- prev_line
            count <- count + 1
            prev_line <- readPrevLine prev_line
        let summary = if count > 0 then [for i in 0..count - 1 -> toString buffer[i]] |> List.rev else List.empty<string>
        ArrayPool<StrSlice>.Shared.Return(buffer)
        summary
        
    let parse (src:string) =
        let enums = ResizeArray<EnumDecl>()
        let structs = ResizeArray<StructDecl>()
        let fns = ResizeArray<FnDecl>()
        let chars = src.ToCharArray()
        let mutable slice = StrSlice(chars)

        while slice.Offset < src.Length do
        
            while (readLine slice).Count = 0 && slice.Offset < slice.Count do 
                advance &slice 1
                
            let line = readLine slice
            match line with
            | FnDeclaration -> 
                let decl = readSlice slice '{'
                let summary = readSummary line
                let name = extract "fn" "(" decl |> trim |> toString
                let args = extract "(" ")" decl |> toString
                
                if (List.length summary) > 0 then printfn "%A" summary
                printfn "-%s-" name
                printfn "-%s-" args
                
                advance &slice decl.Count
            | StructDeclaration -> 
                let decl = readSlice slice ';'
                let summary = readSummary line
                let name = extract "const" "=" line |> trim |> toString

                let fields = extract "{" "}" decl
                let struct_fields = ResizeArray<Elem>()
                let struct(field_lines,len) = split '\n' fields 
                for i in 0..len - 1 do
                    let fl = field_lines[i] |> trim
                    struct_fields.Add <|
                        match (indexOf ":" fl),(indexOf "," fl) with
                        | ValueSome a, ValueSome b ->
                            let field_name = fl.Slice(0, a) |> trim |> toString
                            let field_type = fl.Slice(a + 1, b - a) |> trim |> toString
                            FieldOfStruct (field_name, field_type, None)
                        | ValueSome a, ValueNone ->
                            let field_name = fl.Slice(0, a) |> trim |> toString
                            let field_type = fl.Slice(a + 1, fl.Count - a - 1) |> trim |> toString
                            FieldOfStruct (field_name, field_type, None)
                        | _, _ -> 
                            if (contains "///" fl) then
                                let str_comment = fl |> trim |> toString
                                let elem = HtmlComment str_comment 
                                HtmlComment (toString fl)
                            else
                                failwith $"all cases failed: -{toString fl}-"                        
                structs.Add({summary = summary; name = name; fields = (List.ofSeq struct_fields)})                   
                ArrayPool<StrSlice>.Shared.Return(field_lines)
                
                // for testing purposes
                if (List.length summary) > 0 then printfn "%A" summary
                printfn "-%s-" name
                for struct_name in structs.Last().fields do printfn "    -%A-" struct_name                    
                
                advance &slice decl.Count
            | EnumDeclaration ->
                let decl = readSlice slice ';'
                let summary = readSummary line
                let name = extract "const" "=" line |> trim |> toString

                let fields = extract "{" "}" decl
                let enum_fields = ResizeArray<Elem>()
                let struct(field_lines,len) = split '\n' fields 
                for i in 0..len - 1 do
                    let fl = field_lines[i] |> trim
                    enum_fields.Add <|
                        match (indexOf "," fl) with
                        | ValueSome a ->
                            let field_name = fl.Slice(0, a) |> trim |> toString
                            FieldOfEnum (field_name, None)
                        | ValueNone -> 
                            if (contains "///" fl) then
                                let str_comment = fl |> trim |> toString
                                let elem = HtmlComment str_comment 
                                HtmlComment (toString fl)
                            else
                                failwith $"all cases failed: -{toString fl}-"                        
                enums.Add({summary = summary; name = name; fields = (List.ofSeq enum_fields)})                   
                ArrayPool<StrSlice>.Shared.Return(field_lines)
                
                // for testing purposes
                if (List.length summary) > 0 then printfn "%A" summary
                printfn "-%s-" name
                for enum_name in enums.Last().fields do printfn "    -%A-" enum_name                    
                
                advance &slice decl.Count
            | NoDeclaration -> 
                advance &slice (line.Count + 1)

        {structs = List.ofSeq structs; enums = List.ofSeq enums; fns = List.ofSeq fns}


