namespace Zig2Dotnet

open System
open System.Text
open Parser

module DocGenerator =

    let docEnums (ctx:ParsedContext) (sb:StringBuilder) =
        ignore (sb.AppendLine("<h3>Enums</h3>"))
        ignore (sb.AppendLine("<ul>"))
        for _enum in ctx.enums do
            ignore (sb.Append("  <li>").Append(_enum.name).AppendLine("</li>"))
            for s in _enum.summary do
                ignore (sb.Append("    ").Append(s).AppendLine(" </br>"))
        ignore (sb.AppendLine("</ul>"))
        sb

    let docStructs (ctx:ParsedContext) (sb:StringBuilder) =
        ignore (sb.AppendLine("<h3>Structs</h3>"))
        ignore (sb.AppendLine("<ul>"))
        for _struct in ctx.structs do
            ignore (sb.Append("  <li>").Append(_struct.name).AppendLine("</li>"))
            for s in _struct.summary do
                ignore (sb.Append("    ").Append(s).AppendLine(" </br>"))
        ignore (sb.AppendLine("</ul>"))
        sb


    let [<Literal>] private doctype = 
        """
<!DOCTYPE html>
<html>
  <head>
        """

    let [<Literal>] private closedoc =
        """
  </head>
</html>    
        """
        
    let [<Literal>] private style = 
        """
<style>
  :root {
    font-size: 1em;
    --ui: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji";
    --mono: "Source Code Pro", monospace;
    --tx-color: #141414;
    --bg-color: #ffffff;
    --link-color: #2A6286;
    --sidebar-sh-color: rgba(0, 0, 0, 0.09);
    --sidebar-mod-bg-color: #f1f1f1;
    --sidebar-modlnk-tx-color: #141414;
    --sidebar-modlnk-tx-color-hover: #fff;
    --sidebar-modlnk-tx-color-active: #000;
    --sidebar-modlnk-bg-color: transparent;
    --sidebar-modlnk-bg-color-hover: #555;
    --sidebar-modlnk-bg-color-active: #FFBB4D;
    --search-bg-color: #f3f3f3;
    --search-bg-color-focus: #ffffff;
    --search-sh-color: rgba(0, 0, 0, 0.18);
    --search-other-results-color: rgb(100, 100, 100);
    --modal-sh-color: rgba(0, 0, 0, 0.75);
    --modal-bg-color: #aaa;
    --warning-popover-bg-color: #ff4747;
  }
  body {
    font-size: 1rem;
    font-family: sans-serif;
    margin: 20px;
  }
  ul {
    line-height: 25px;
  }
  code {
    font-family: var(--mono);
    font-weight: bold;
    font-size: 1em;
    <!-- background: whitesmoke;
    color: #000;
    line-height: 10px; -->       
  }
  fnName {
    font-family: var(--mono);
    font-weight: bold;
    font-size: 1em;
    <!-- background: whitesmoke;
    color: #A00000;
    line-height: 10px; -->       
  }
  fnargs {
    font-family: var(--mono);
    font-size: 1em;
    <!-- background: whitesmoke;
    color: #000;
    padding: 10px;
    line-height: 10px; -->       
  }
  span {
    margin: 20px;
    padding: 3px;
    line-height: 45px;
  }
  dl {
    font-family: var(--mono);
    <!-- line-height: 45px; -->
  }
  dt {
    margin-bottom: 5px;  
  }
  dd {
    overflow: auto;
    margin-bottom:  20px;  
  }
</style>
    """


    let genDoc (filepath:string) (ctx:ParsedContext) (docFns:ParsedContext -> StringBuilder -> StringBuilder) =
        let sb = StringBuilder(1024 * 8)
        use fs = System.IO.File.CreateText(filepath)
        fs.WriteLine doctype
        fs.WriteLine style
        fs.WriteLine(string (docStructs ctx sb))
        ignore (sb.Clear())
        fs.WriteLine(string (docEnums ctx sb))
        ignore (sb.Clear())
        fs.WriteLine (string (docFns ctx sb))
        fs.WriteLine closedoc
        fs.Close()
