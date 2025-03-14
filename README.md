
### zig2dotnet

A parser that reads a `.zig` source file, and creates bindings in   

- C#
- F#

for the `export` functions and `extern` structs and `enums(c_int)` enums that file contains.

It can compile with NativeAoT
`dotnet publish -c Release -r linux-x64`

or simple build with `dotnet build`

P.S
`printfn` and `sprintfn` use reflection, thus are not suitable for AoT compiled bindings.
Avoid those functions and use `Console.WriteLine` and `interpolated-strings` instead.

#### How to use
In terminal run  
```
./Parser [source-file] [libname] [-fs|cs] [destination]
```
i.e.  
```
`./Parser "../src_file.zig" "libname.dll" -fs "../directory/"` 
```

Or run simply as `./Parser "../src_file.zig"` to generate a `.html` with the zig source file declarations. \
Among the .cs / .fs bindings file, a `.html` documentation file will be generated too.

