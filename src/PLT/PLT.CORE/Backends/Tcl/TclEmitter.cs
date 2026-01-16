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

    private enum ExprContext
    {
        Normal,       // Variables need $prefix
        InsideExpr    // Variables don't need $prefix (inside [expr {...}])
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
                EmitExpr(s.Expr, sb, ExprContext.Normal);
                sb.AppendLine();
                break;

            case VarAssignment v:
                if (!string.IsNullOrWhiteSpace(v.LeadingComment))
                    sb.AppendLine($"{pad}# {v.LeadingComment}");
                sb.Append(pad);
                sb.Append("set ");
                sb.Append(v.VarName);
                sb.Append(" ");
                EmitExpr(v.Value, sb, ExprContext.Normal);
                sb.AppendLine();
                break;

            case PassStmt p:
                if (!string.IsNullOrWhiteSpace(p.LeadingComment))
                    sb.AppendLine($"{pad}# {p.LeadingComment}");
                sb.AppendLine($"{pad}# pass");
                break;

            case TupleUnpackingAssignment t:
                if (!string.IsNullOrWhiteSpace(t.LeadingComment))
                    sb.AppendLine($"{pad}# {t.LeadingComment}");
                // Emit: set varlist [expr_value]
                // Then: lassign $varlist var1 var2 ...
                sb.Append(pad);
                sb.Append("set _tuple ");
                EmitExpr(t.Value, sb, ExprContext.Normal);
                sb.AppendLine();
                sb.Append(pad);
                sb.Append("lassign $_tuple");
                foreach (var varName in t.VarNames)
                {
                    sb.Append(" ");
                    sb.Append(varName);
                }
                sb.AppendLine();
                break;

            case IfStmt i:
                if (!string.IsNullOrWhiteSpace(i.LeadingComment))
                    sb.AppendLine($"{pad}# {i.LeadingComment}");
                sb.Append(pad);
                sb.Append("if {");
                EmitExpr(i.Condition, sb, ExprContext.Normal);
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
                EmitExpr(f.IterableExpr, sb, ExprContext.Normal);
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
                EmitExpr(w.Condition, sb, ExprContext.Normal);
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

            case TryStmt t:
                if (!string.IsNullOrWhiteSpace(t.LeadingComment))
                    sb.AppendLine($"{pad}# {t.LeadingComment}");
                sb.AppendLine($"{pad}# Try block");
                foreach (var s in t.TryBody)
                    EmitStmt(s, sb, indent);
                if (t.ExceptClauses.Count > 0)
                {
                    foreach (var (exceptionType, varName, body) in t.ExceptClauses)
                    {
                        if (!string.IsNullOrWhiteSpace(exceptionType))
                            sb.AppendLine($"{pad}# Catch {exceptionType}" + (varName != null ? $" as {varName}" : ""));
                        else
                            sb.AppendLine($"{pad}# Catch all exceptions");
                        foreach (var s in body)
                            EmitStmt(s, sb, indent);
                    }
                }
                if (t.FinallyBody != null)
                {
                    sb.AppendLine($"{pad}# Finally block");
                    foreach (var s in t.FinallyBody)
                        EmitStmt(s, sb, indent);
                }
                break;

            default:
                throw new NotSupportedException($"Unsupported stmt: {stmt.GetType().Name}");
        }
    }

    private static void EmitExpr(Expr expr, StringBuilder sb, ExprContext context = ExprContext.Normal)
    {
        switch (expr)
        {
            case Intrinsic i when i.Name == "print":
                sb.Append("puts ");
                if (i.Args.Count == 1)
                {
                    EmitExpr(i.Args[0], sb, ExprContext.Normal);
                }
                else if (i.Args.Count > 1)
                {
                    sb.Append("[concat");
                    foreach (var arg in i.Args)
                    {
                        sb.Append(" ");
                        EmitExpr(arg, sb, ExprContext.Normal);
                    }
                    sb.Append("]");
                }
                return;

            case Intrinsic i when i.Name == "ternary":
                // ternary(condition, true_expr, false_expr) => condition ? true_expr : false_expr
                // In Tcl: expr {condition ? true_value : false_value}
                sb.Append("[expr {");
                if (i.Args.Count >= 3)
                {
                    EmitExpr(i.Args[0], sb, ExprContext.InsideExpr);  // condition
                    sb.Append(" ? ");
                    EmitExpr(i.Args[1], sb, ExprContext.InsideExpr);  // true_expr
                    sb.Append(" : ");
                    EmitExpr(i.Args[2], sb, ExprContext.InsideExpr);  // false_expr
                }
                sb.Append("}]");
                return;

            case Intrinsic i when i.Name == "raise":
                // raise(exception) => error "exception"
                sb.Append("error ");
                if (i.Args.Count > 0)
                {
                    EmitExpr(i.Args[0], sb, ExprContext.Normal);
                }
                else
                {
                    sb.Append("\"\"");  // Re-raise with empty message
                }
                return;

            case Literal l:
                sb.Append(FormatLiteral(l.Value));
                return;

            case Variable v:
                if (context == ExprContext.InsideExpr)
                    sb.Append(v.Name);  // No $ inside expr blocks
                else
                    sb.Append("$").Append(v.Name);  // Need $ outside expr blocks
                return;

            case ListLiteral l:
                sb.Append("[list");
                for (int j = 0; j < l.Elements.Count; j++)
                {
                    sb.Append(" ");
                    EmitExpr(l.Elements[j], sb, ExprContext.Normal);
                }
                sb.Append("]");
                return;

            case DictLiteral d:
                sb.Append("[dict create");
                for (int j = 0; j < d.Items.Count; j++)
                {
                    sb.Append(" ");
                    EmitExpr(d.Items[j].Key, sb, ExprContext.Normal);
                    sb.Append(" ");
                    EmitExpr(d.Items[j].Value, sb, ExprContext.Normal);
                }
                sb.Append("]");
                return;

            case BinaryOp b:
                // Tcl uses expr for math/logic
                sb.Append("[expr {");
                EmitExpr(b.Left, sb, ExprContext.InsideExpr);
                sb.Append(" ");
                sb.Append(b.Op);
                sb.Append(" ");
                EmitExpr(b.Right, sb, ExprContext.InsideExpr);
                sb.Append("}]");
                return;

            case UnaryOp u:
                sb.Append("[expr {");
                sb.Append(u.Op);
                sb.Append(" ");
                EmitExpr(u.Operand, sb, ExprContext.InsideExpr);
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
                        EmitExpr(f.Args[j], sb, ExprContext.Normal);
                    }
                }
                else
                {
                    sb.Append(f.FunctionName);
                    for (int j = 0; j < f.Args.Count; j++)
                    {
                        sb.Append(" ");
                        EmitExpr(f.Args[j], sb, ExprContext.Normal);
                    }
                }
                return;

            case MethodCall m:
                if (m.MethodName == "__slice__")
                {
                    sb.Append("[string range ");
                    EmitExpr(m.Target, sb, ExprContext.Normal);
                    sb.Append(" ");
                    if (m.Args[0] is not Literal { Value: null })
                        EmitExpr(m.Args[0], sb, ExprContext.Normal);
                    else
                        sb.Append("0");
                    sb.Append(" ");
                    if (m.Args[1] is not Literal { Value: null })
                        EmitExpr(m.Args[1], sb, ExprContext.Normal);
                    else
                        sb.Append("end");
                    sb.Append("]");
                }
                else if (m.MethodName == "__getitem__")
                {
                    // Array/string indexing: array[index] -> lindex $array $index or string index
                    sb.Append("[lindex ");
                    EmitExpr(m.Target, sb, ExprContext.Normal);
                    sb.Append(" ");
                    EmitExpr(m.Args[0], sb, ExprContext.Normal);
                    sb.Append("]");
                }
                else
                {
                    // Treat as namespace call
                    sb.Append("::");
                    sb.Append(m.MethodName);
                    sb.Append(" ");
                    EmitExpr(m.Target, sb, ExprContext.Normal);
                    for (int j = 0; j < m.Args.Count; j++)
                    {
                        sb.Append(" ");
                        EmitExpr(m.Args[j], sb, ExprContext.Normal);
                    }
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

            case ListComprehension lc:
                sb.Append("[list");
                sb.Append(" ");
                sb.Append("[foreach ");
                sb.Append(lc.LoopVar);
                sb.Append(" ");
                EmitExpr(lc.IterableExpr, sb, ExprContext.Normal);
                sb.Append(" {");
                if (lc.FilterCondition != null)
                {
                    sb.Append("if {");
                    EmitExpr(lc.FilterCondition, sb, ExprContext.Normal);
                    sb.Append("} {");
                }
                sb.Append("lappend _result ");
                EmitExpr(lc.Element, sb, ExprContext.Normal);
                if (lc.FilterCondition != null)
                    sb.Append("}");
                sb.Append("}]");
                return;

            case DictComprehension dc:
                // Tcl dict comprehension: dict create with foreach
                sb.Append("[dict create ");
                sb.Append("[foreach {");
                // Handle tuple unpacking for loop vars like "k,v"
                sb.Append(dc.LoopVar.Replace(",", " "));
                sb.Append("} ");
                EmitExpr(dc.IterableExpr, sb, ExprContext.Normal);
                sb.Append(" {");
                if (dc.FilterCondition != null)
                {
                    sb.Append("if {");
                    EmitExpr(dc.FilterCondition, sb, ExprContext.Normal);
                    sb.Append("} {");
                }
                sb.Append("dict set _result ");
                EmitExpr(dc.KeyExpr, sb, ExprContext.Normal);
                sb.Append(" ");
                EmitExpr(dc.ValueExpr, sb, ExprContext.Normal);
                if (dc.FilterCondition != null)
                    sb.Append("}");
                sb.Append("}]]");
                return;

            case LambdaExpr lam:
                sb.Append("lambda");
                foreach (var param in lam.Parameters)
                {
                    sb.Append(" ").Append(param);
                }
                sb.Append(" {");
                EmitExpr(lam.Body, sb, ExprContext.Normal);
                sb.Append("}");
                return;

            case Intrinsic intrinsic:
                // Handle intrinsic operations like getattr/setattr
                sb.Append(intrinsic.Name);
                sb.Append(" ");
                for (int j = 0; j < intrinsic.Args.Count; j++)
                {
                    if (j > 0) sb.Append(" ");
                    EmitExpr(intrinsic.Args[j], sb, ExprContext.Normal);
                }
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
