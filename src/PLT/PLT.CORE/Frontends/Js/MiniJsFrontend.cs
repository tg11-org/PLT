using System.Text.RegularExpressions;
using PLT.CORE.IR;

namespace PLT.CORE.Frontends.Js;

public static class MiniJsFrontend
{
    // MVP:
    // - Optionally captures a single-line comment right above the console.log call
    // - Matches: console.log("...") or console.log('...')
    public static IrProgram ParseConsoleLogHelloWorld(string source)
    {
        // Capture an optional leading comment line + console.log string literal
        // Example:
        // // Prints "Hello, world!" to the console
        // console.log("Hello, world!")
        var pattern = @"(?ms)^\s*(?:(//[^\r\n]*)\s*)?console\.log\(\s*(['""])(.*?)\2\s*\)\s*;?\s*$";
        var m = Regex.Match(source, pattern);

        if (!m.Success)
            throw new NotSupportedException("Mini JS frontend currently supports only: console.log(\"...\") (optionally preceded by // comment).");

        var comment = m.Groups[1].Success ? m.Groups[1].Value.TrimStart('/', ' ').Trim() : null;
        var text = m.Groups[3].Value;

        return new IrProgram(new Stmt[]
        {
            new ExprStmt(
                new Intrinsic("print", new Expr[] { new Literal(text) }),
                comment
            )
        });
    }
}
