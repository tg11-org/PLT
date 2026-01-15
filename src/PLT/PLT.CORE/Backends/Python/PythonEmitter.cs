using System.Text;
using PLT.CORE.IR;

namespace PLT.CORE.Backends.Python;

public sealed class PythonEmitter
{
    public string Emit(IrProgram program)
    {
        var sb = new StringBuilder();
        foreach (var stmt in program.Body)
            EmitStmt(stmt, sb, indent: 0);
        return sb.ToString();
    }

    private static void EmitStmt(Stmt stmt, StringBuilder sb, int indent)
    {
        var pad = new string(' ', indent * 4);

        switch (stmt)
        {
            case ExprStmt s:
                if (!string.IsNullOrWhiteSpace(s.LeadingComment))
                    sb.AppendLine($"{pad}# {s.LeadingComment}");
                sb.Append(pad);
                EmitExpr(s.Expr, sb);
                sb.AppendLine();
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
                sb.Append("print(");
                for (int j = 0; j < i.Args.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    EmitExpr(i.Args[j], sb);
                }
                sb.Append(")");
                return;

            case Literal l:
                sb.Append(FormatLiteral(l.Value));
                return;

            default:
                throw new NotSupportedException($"Unsupported expr: {expr.GetType().Name}");
        }
    }

    private static string FormatLiteral(object? value) =>
        value switch
        {
            null => "None",
            string s => $"\"{EscapeString(s)}\"",
            bool b => b ? "True" : "False",
            int i => i.ToString(),
            long l => l.ToString(),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => $"\"{EscapeString(value.ToString() ?? "")}\""
        };

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
