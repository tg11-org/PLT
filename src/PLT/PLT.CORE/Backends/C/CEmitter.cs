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

            case VarAssignment v:
                if (!string.IsNullOrWhiteSpace(v.LeadingComment))
                    sb.AppendLine($"{pad}// {v.LeadingComment}");
                sb.Append(pad);
                sb.Append("int ");  // TODO: infer type
                sb.Append(v.VarName);
                sb.Append(" = ");
                EmitExpr(v.Value, sb);
                sb.AppendLine(";");
                break;

            case IfStmt i:
                if (!string.IsNullOrWhiteSpace(i.LeadingComment))
                    sb.AppendLine($"{pad}// {i.LeadingComment}");
                sb.Append(pad);
                sb.Append("if (");
                EmitExpr(i.Condition, sb);
                sb.AppendLine(") {");
                foreach (var s in i.ThenBody)
                    EmitStmt(s, sb, indent + 1);
                if (i.ElseBody != null)
                {
                    sb.AppendLine($"{pad}}} else {{");
                    foreach (var s in i.ElseBody)
                        EmitStmt(s, sb, indent + 1);
                }
                sb.AppendLine($"{pad}}}");
                break;

            case ForEachStmt f:
                if (!string.IsNullOrWhiteSpace(f.LeadingComment))
                    sb.AppendLine($"{pad}// {f.LeadingComment}");
                // C doesn't have foreach; we'll approximate with a comment
                sb.AppendLine($"{pad}// foreach {f.LoopVar} in ...");
                foreach (var s in f.Body)
                    EmitStmt(s, sb, indent + 1);
                break;

            case WhileStmt w:
                if (!string.IsNullOrWhiteSpace(w.LeadingComment))
                    sb.AppendLine($"{pad}// {w.LeadingComment}");
                sb.Append(pad);
                sb.Append("while (");
                EmitExpr(w.Condition, sb);
                sb.AppendLine(") {");
                foreach (var s in w.Body)
                    EmitStmt(s, sb, indent + 1);
                sb.AppendLine($"{pad}}}");
                break;

            case FunctionDefStmt f:
                if (!string.IsNullOrWhiteSpace(f.LeadingComment))
                    sb.AppendLine($"{pad}// {f.LeadingComment}");
                sb.Append("void ");  // TODO: infer return type
                sb.Append(f.FunctionName);
                sb.Append("(");
                for (int j = 0; j < f.Parameters.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append("int ");  // TODO: infer parameter types
                    sb.Append(f.Parameters[j]);
                }
                sb.AppendLine(") {");
                foreach (var s in f.Body)
                    EmitStmt(s, sb, indent + 1);
                sb.AppendLine("}");
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

            case Variable v:
                sb.Append(v.Name);
                return;

            case ListLiteral l:
                sb.Append("{");
                for (int j = 0; j < l.Elements.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    EmitExpr(l.Elements[j], sb);
                }
                sb.Append("}");
                return;

            case BinaryOp b:
                EmitExpr(b.Left, sb);
                sb.Append(" ");
                sb.Append(b.Op);
                sb.Append(" ");
                EmitExpr(b.Right, sb);
                return;

            case UnaryOp u:
                sb.Append(u.Op);
                sb.Append(" ");
                EmitExpr(u.Operand, sb);
                return;

            case FunctionCall f:
                sb.Append(f.FunctionName);
                sb.Append("(");
                for (int j = 0; j < f.Args.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    EmitExpr(f.Args[j], sb);
                }
                sb.Append(")");
                return;

            case MethodCall m:
                // C doesn't have methods, just function calls
                sb.Append(m.MethodName);
                sb.Append("(");
                EmitExpr(m.Target, sb);
                for (int j = 0; j < m.Args.Count; j++)
                {
                    sb.Append(", ");
                    EmitExpr(m.Args[j], sb);
                }
                sb.Append(")");
                return;

            case StringInterpolation s:
                sb.Append("\"");
                foreach (var part in s.Parts)
                {
                    if (part is StringPartLiteral lit)
                        sb.Append(EscapeCString(lit.Value));
                    else if (part is StringPartVariable var)
                        sb.Append("%s");  // simplified
                }
                sb.Append("\"");
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
