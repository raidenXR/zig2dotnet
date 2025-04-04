
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
./zig2dotnet [source-file] [libname] [-fs|cs] [destination]
```
i.e. to generate and add the bindings in the current directory run the following command. 
```
`./zig2dotnet "../src_file.zig" "libname.dll" -fs "./"`  
```

Or run simply as `./zig2dotnet "../src_file.zig"` to generate a `.html` with the zig source file declarations. \
Among the .cs / .fs bindings file, a `.html` documentation file will be generated too.

**P.S.** add the compiled `zig2dotnet` to your path to be more easily accessible.  

TODO: fix the bindings for `[*c]Struct` to generate `Struct[]` and not `Struct*` !!!  
  


## EDIT:

The project has be rewritten to `zig2dotnet_v2.fsproj`. Build with
```
dotnet build zig2dotnet_v2.fsproj
# add to PATH `bin/Debug/net9.0`  to execute easily from terminal zig2dotnet_v2.
```
i.e 
```
# cd to some project directory 
zig2dotnet_v2 "src/rendererFS.zig" "librenderer.so" -fs # to generate F# bindings
zig2dotnet_v2 "src/rendererFS.zig" "librenderer.so" -cs # to generate C# bindings
```

first argument is the path of the source file to generate bindings for.   
second argument is the name of the compiled native library, that the bindings apply for.   
third argument indicate whether to generate bindings for F# or C#.   


#### WARNING:

Edit manually at the generated bindings file, the lines:
```fs
module librenderer.so =
    let [<Literal>] libname = "librenderer.so"

namespace librenderer.so;
```
```cs
public static unsafe class libname
{
    const string libname = "librenderer.so";

```

To match the correct path.   