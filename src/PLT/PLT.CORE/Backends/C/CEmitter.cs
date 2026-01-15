using System.Text;
using PLT.CORE.IR;

namespace PLT.CORE.Backends.C;

public sealed class CEmitter
{
    public string Emit(IrProgram program)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#include <stdio.h>");
        sb.AppendLine();
        sb.AppendLine("int main(void) {");

        foreach (var stmt in program.Body)
            EmitStmt(stmt, sb, indent: 1);

        sb.AppendLine("    return 0;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitStmt(Stmt stmt, StringBuilder sb, int indent)
    {
        var pad = new string(' ', indent * 4);

        switch (stmt)
        {
            case ExprStmt s:
                if (!string.IsNullOrWhiteSpace(s.LeadingComment))
                    sb.AppendLine($"{pad}// {s.LeadingComment}");
                sb.Append(pad);
                EmitExpr(s.Expr, sb);
                sb.AppendLine(";");
                break;

            default:
                throw new NotSupportedException($"Unsupported stmt: {stmt.GetType().Name}");
        }
    }

    private static void EmitExpr(Expr expr, StringBuilder sb)
    {
        switch (expr)
        {
            case Intrinsic i when i.Name == "print":
                // MVP: map print(x) -> printf("%s", x);
                // For now, only support a single argument.
                if (i.Args.Count != 1)
                    throw new NotSupportedException("C backend currently supports print() with exactly 1 argument.");

                sb.Append("printf(");
                EmitPrintfForSingleArg(i.Args[0], sb);
                sb.Append(")");
                return;

            case Literal l:
                sb.Append(FormatCLiteral(l.Value));
                return;

            default:
                throw new NotSupportedException($"Unsupported expr: {expr.GetType().Name}");
        }
    }

    private static void EmitPrintfForSingleArg(Expr arg, StringBuilder sb)
    {
        // Minimal: only handle string literals nicely.
        // We can expand later (ints, floats, bools).
        if (arg is Literal { Value: string })
        {
            sb.Append("\"%s\\n\", ");
            EmitExpr(arg, sb);
            return;
        }

        if (arg is Literal { Value: int or long })
        {
            sb.Append("\"%lld\\n\", ");
            EmitExpr(arg, sb);
            return;
        }

        if (arg is Literal { Value: float or double })
        {
            sb.Append("\"%f\\n\", ");
            EmitExpr(arg, sb);
            return;
        }

        // Fallback (not great, but honest)
        throw new NotSupportedException("C backend only supports print() of string/number literals for now.");
    }

    private static string FormatCLiteral(object? value) =>
        value switch
        {
            null => "0", // placeholder; C has no null literal for primitives
            string s => $"\"{EscapeCString(s)}\"",
            bool b => b ? "1" : "0",
            int i => i.ToString(),
            long l => l.ToString(),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f",
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => "0"
        };

    private static string EscapeCString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
