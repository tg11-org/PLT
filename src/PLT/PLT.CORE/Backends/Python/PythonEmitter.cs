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

            case VarAssignment v:
                if (!string.IsNullOrWhiteSpace(v.LeadingComment))
                    sb.AppendLine($"{pad}# {v.LeadingComment}");
                sb.Append(pad);
                sb.Append(v.VarName);
                sb.Append(" = ");
                EmitExpr(v.Value, sb);
                sb.AppendLine();
                break;

            case TupleUnpackingAssignment t:
                if (!string.IsNullOrWhiteSpace(t.LeadingComment))
                    sb.AppendLine($"{pad}# {t.LeadingComment}");
                sb.Append(pad);
                sb.Append("(");
                for (int i = 0; i < t.VarNames.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(t.VarNames[i]);
                }
                sb.Append(") = ");
                EmitExpr(t.Value, sb);
                sb.AppendLine();
                break;

            case IfStmt i:
                if (!string.IsNullOrWhiteSpace(i.LeadingComment))
                    sb.AppendLine($"{pad}# {i.LeadingComment}");
                sb.Append(pad);
                sb.Append("if ");
                EmitExpr(i.Condition, sb);
                sb.AppendLine(":");
                foreach (var s in i.ThenBody)
                    EmitStmt(s, sb, indent + 1);
                if (i.ElseBody != null)
                {
                    sb.AppendLine($"{pad}else:");
                    foreach (var s in i.ElseBody)
                        EmitStmt(s, sb, indent + 1);
                }
                break;

            case ForEachStmt f:
                if (!string.IsNullOrWhiteSpace(f.LeadingComment))
                    sb.AppendLine($"{pad}# {f.LeadingComment}");
                sb.Append(pad);
                sb.Append("for ");
                sb.Append(f.LoopVar);
                sb.Append(" in ");
                EmitExpr(f.IterableExpr, sb);
                sb.AppendLine(":");
                foreach (var s in f.Body)
                    EmitStmt(s, sb, indent + 1);
                break;

            case WhileStmt w:
                if (!string.IsNullOrWhiteSpace(w.LeadingComment))
                    sb.AppendLine($"{pad}# {w.LeadingComment}");
                sb.Append(pad);
                sb.Append("while ");
                EmitExpr(w.Condition, sb);
                sb.AppendLine(":");
                foreach (var s in w.Body)
                    EmitStmt(s, sb, indent + 1);
                break;

            case FunctionDefStmt f:
                if (!string.IsNullOrWhiteSpace(f.LeadingComment))
                    sb.AppendLine($"{pad}# {f.LeadingComment}");
                sb.Append(pad);
                sb.Append("def ");
                sb.Append(f.FunctionName);
                sb.Append("(");
                for (int j = 0; j < f.Parameters.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(f.Parameters[j]);
                }
                sb.AppendLine(")");
                foreach (var s in f.Body)
                    EmitStmt(s, sb, indent + 1);
                break;

            case ClassDefStmt c:
                if (!string.IsNullOrWhiteSpace(c.LeadingComment))
                    sb.AppendLine($"{pad}# {c.LeadingComment}");
                sb.Append(pad);
                sb.Append("class ");
                sb.Append(c.ClassName);
                if (!string.IsNullOrWhiteSpace(c.BaseClass))
                {
                    sb.Append("(");
                    sb.Append(c.BaseClass);
                    sb.Append(")");
                }
                sb.AppendLine(":");
                foreach (var s in c.Body)
                    EmitStmt(s, sb, indent + 1);
                break;

            case TryStmt t:
                if (!string.IsNullOrWhiteSpace(t.LeadingComment))
                    sb.AppendLine($"{pad}# {t.LeadingComment}");
                sb.AppendLine($"{pad}try:");
                foreach (var s in t.TryBody)
                    EmitStmt(s, sb, indent + 1);
                foreach (var (exceptionType, varName, body) in t.ExceptClauses)
                {
                    sb.Append($"{pad}except");
                    if (!string.IsNullOrWhiteSpace(exceptionType))
                    {
                        sb.Append(" ");
                        sb.Append(exceptionType);
                        if (!string.IsNullOrWhiteSpace(varName))
                        {
                            sb.Append(" as ");
                            sb.Append(varName);
                        }
                    }
                    sb.AppendLine(":");
                    foreach (var s in body)
                        EmitStmt(s, sb, indent + 1);
                }
                if (t.FinallyBody != null)
                {
                    sb.AppendLine($"{pad}finally:");
                    foreach (var s in t.FinallyBody)
                        EmitStmt(s, sb, indent + 1);
                }
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

            case Intrinsic i when i.Name == "ternary":
                // ternary(condition, true_expr, false_expr) => true_expr if condition else false_expr
                if (i.Args.Count >= 3)
                {
                    EmitExpr(i.Args[1], sb);  // true_expr
                    sb.Append(" if ");
                    EmitExpr(i.Args[0], sb);  // condition
                    sb.Append(" else ");
                    EmitExpr(i.Args[2], sb);  // false_expr
                }
                return;

            case Intrinsic i when i.Name == "raise":
                sb.Append("raise ");
                if (i.Args.Count > 0)
                {
                    EmitExpr(i.Args[0], sb);
                }
                return;

            case Literal l:
                sb.Append(FormatLiteral(l.Value));
                return;

            case Variable v:
                sb.Append(v.Name);
                return;

            case ListLiteral l:
                sb.Append("[");
                for (int j = 0; j < l.Elements.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    EmitExpr(l.Elements[j], sb);
                }
                sb.Append("]");
                return;

            case DictLiteral d:
                sb.Append("{");
                for (int j = 0; j < d.Items.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    EmitExpr(d.Items[j].Key, sb);
                    sb.Append(": ");
                    EmitExpr(d.Items[j].Value, sb);
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
                if (m.MethodName == "__slice__")
                {
                    EmitExpr(m.Target, sb);
                    sb.Append("[");
                    if (m.Args[0] is not Literal { Value: null })
                        EmitExpr(m.Args[0], sb);
                    sb.Append(":");
                    if (m.Args[1] is not Literal { Value: null })
                        EmitExpr(m.Args[1], sb);
                    if (m.Args.Count > 2 && m.Args[2] is not Literal { Value: null })
                    {
                        sb.Append(":");
                        EmitExpr(m.Args[2], sb);
                    }
                    sb.Append("]");
                }
                else if (m.MethodName == "__getitem__")
                {
                    // Regular indexing: arr[index]
                    EmitExpr(m.Target, sb);
                    sb.Append("[");
                    EmitExpr(m.Args[0], sb);
                    sb.Append("]");
                }
                else
                {
                    EmitExpr(m.Target, sb);
                    sb.Append(".");
                    sb.Append(m.MethodName);
                    sb.Append("(");
                    for (int j = 0; j < m.Args.Count; j++)
                    {
                        if (j > 0) sb.Append(", ");
                        EmitExpr(m.Args[j], sb);
                    }
                    sb.Append(")");
                }
                return;

            case StringInterpolation s:
                sb.Append("f\"");
                foreach (var part in s.Parts)
                {
                    if (part is StringPartLiteral lit)
                        sb.Append(EscapeString(lit.Value));
                    else if (part is StringPartVariable var)
                        sb.Append("{").Append(var.VarName).Append("}");
                }
                sb.Append("\"");
                return;

            case ListComprehension lc:
                sb.Append("[");
                EmitExpr(lc.Element, sb);
                sb.Append(" for ");
                sb.Append(lc.LoopVar);
                sb.Append(" in ");
                EmitExpr(lc.IterableExpr, sb);
                if (lc.FilterCondition != null)
                {
                    sb.Append(" if ");
                    EmitExpr(lc.FilterCondition, sb);
                }
                sb.Append("]");
                return;

            case DictComprehension dc:
                sb.Append("{");
                EmitExpr(dc.KeyExpr, sb);
                sb.Append(": ");
                EmitExpr(dc.ValueExpr, sb);
                sb.Append(" for ");
                sb.Append(dc.LoopVar);
                sb.Append(" in ");
                EmitExpr(dc.IterableExpr, sb);
                if (dc.FilterCondition != null)
                {
                    sb.Append(" if ");
                    EmitExpr(dc.FilterCondition, sb);
                }
                sb.Append("}");
                return;

            case LambdaExpr lam:
                sb.Append("lambda ");
                for (int j = 0; j < lam.Parameters.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(lam.Parameters[j]);
                }
                sb.Append(": ");
                EmitExpr(lam.Body, sb);
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
