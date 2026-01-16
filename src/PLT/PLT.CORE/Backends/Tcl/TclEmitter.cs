using System.Text;
using PLT.CORE.IR;

namespace PLT.CORE.Backends.Tcl;

public sealed class TclEmitter
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

            case VarAssignment v:
                if (!string.IsNullOrWhiteSpace(v.LeadingComment))
                    sb.AppendLine($"{pad}# {v.LeadingComment}");
                sb.Append(pad);
                sb.Append("set ");
                sb.Append(v.VarName);
                sb.Append(" ");
                EmitExpr(v.Value, sb);
                sb.AppendLine();
                break;

            case IfStmt i:
                if (!string.IsNullOrWhiteSpace(i.LeadingComment))
                    sb.AppendLine($"{pad}# {i.LeadingComment}");
                sb.Append(pad);
                sb.Append("if {");
                EmitExpr(i.Condition, sb);
                sb.AppendLine("} {");
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
                    sb.AppendLine($"{pad}# {f.LeadingComment}");
                sb.Append(pad);
                sb.Append("foreach ");
                sb.Append(f.LoopVar);
                sb.Append(" ");
                EmitExpr(f.IterableExpr, sb);
                sb.AppendLine(" {");
                foreach (var s in f.Body)
                    EmitStmt(s, sb, indent + 1);
                sb.AppendLine($"{pad}}}");
                break;

            case WhileStmt w:
                if (!string.IsNullOrWhiteSpace(w.LeadingComment))
                    sb.AppendLine($"{pad}# {w.LeadingComment}");
                sb.Append(pad);
                sb.Append("while {");
                EmitExpr(w.Condition, sb);
                sb.AppendLine("} {");
                foreach (var s in w.Body)
                    EmitStmt(s, sb, indent + 1);
                sb.AppendLine($"{pad}}}");
                break;

            case FunctionDefStmt f:
                if (!string.IsNullOrWhiteSpace(f.LeadingComment))
                    sb.AppendLine($"{pad}# {f.LeadingComment}");
                sb.Append("proc ");
                sb.Append(f.FunctionName);
                sb.Append(" {");
                for (int j = 0; j < f.Parameters.Count; j++)
                {
                    if (j > 0) sb.Append(" ");
                    sb.Append(f.Parameters[j]);
                }
                sb.AppendLine("} {");
                foreach (var s in f.Body)
                    EmitStmt(s, sb, indent + 1);
                sb.AppendLine("}");
                break;

            case ClassDefStmt c:
                if (!string.IsNullOrWhiteSpace(c.LeadingComment))
                    sb.AppendLine($"{pad}# {c.LeadingComment}");
                if (!string.IsNullOrWhiteSpace(c.BaseClass))
                    sb.AppendLine($"{pad}# Class {c.ClassName} extends {c.BaseClass}");
                else
                    sb.AppendLine($"{pad}# Class {c.ClassName}");
                foreach (var s in c.Body)
                    EmitStmt(s, sb, indent);
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
                sb.Append("puts ");
                if (i.Args.Count == 1)
                {
                    EmitExpr(i.Args[0], sb);
                }
                else if (i.Args.Count > 1)
                {
                    sb.Append("[concat");
                    foreach (var arg in i.Args)
                    {
                        sb.Append(" ");
                        EmitExpr(arg, sb);
                    }
                    sb.Append("]");
                }
                return;

            case Literal l:
                sb.Append(FormatLiteral(l.Value));
                return;

            case Variable v:
                sb.Append("$");
                sb.Append(v.Name);
                return;

            case ListLiteral l:
                sb.Append("[list");
                for (int j = 0; j < l.Elements.Count; j++)
                {
                    sb.Append(" ");
                    EmitExpr(l.Elements[j], sb);
                }
                sb.Append("]");
                return;

            case BinaryOp b:
                // Tcl uses expr for math/logic
                sb.Append("[expr {");
                EmitExpr(b.Left, sb);
                sb.Append(" ");
                sb.Append(b.Op);
                sb.Append(" ");
                EmitExpr(b.Right, sb);
                sb.Append("}]");
                return;

            case UnaryOp u:
                sb.Append("[expr {");
                sb.Append(u.Op);
                sb.Append(" ");
                EmitExpr(u.Operand, sb);
                sb.Append("}]");
                return;

            case FunctionCall f:
                // Map print to puts
                if (f.FunctionName == "print")
                {
                    sb.Append("puts");
                    for (int j = 0; j < f.Args.Count; j++)
                    {
                        sb.Append(" ");
                        EmitExpr(f.Args[j], sb);
                    }
                }
                else
                {
                    sb.Append(f.FunctionName);
                    for (int j = 0; j < f.Args.Count; j++)
                    {
                        sb.Append(" ");
                        EmitExpr(f.Args[j], sb);
                    }
                }
                return;

            case MethodCall m:
                // Treat as namespace call
                sb.Append("::");
                sb.Append(m.MethodName);
                sb.Append(" ");
                EmitExpr(m.Target, sb);
                for (int j = 0; j < m.Args.Count; j++)
                {
                    sb.Append(" ");
                    EmitExpr(m.Args[j], sb);
                }
                return;

            case StringInterpolation s:
                sb.Append("\"");
                foreach (var part in s.Parts)
                {
                    if (part is StringPartLiteral lit)
                        sb.Append(EscapeString(lit.Value));
                    else if (part is StringPartVariable var)
                        sb.Append("$").Append(var.VarName);
                }
                sb.Append("\"");
                return;

            default:
                throw new NotSupportedException($"Unsupported expr: {expr.GetType().Name}");
        }
    }

    private static string FormatLiteral(object? value) =>
        value switch
        {
            null => "\"\"",
            string s => $"\"{EscapeString(s)}\"",
            bool b => b ? "1" : "0",
            int i => i.ToString(),
            long l => l.ToString(),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => $"\"{EscapeString(value.ToString() ?? "")}\""
        };

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$");
}
