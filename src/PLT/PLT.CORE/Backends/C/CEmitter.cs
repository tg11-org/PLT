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

            case PassStmt p:
                if (!string.IsNullOrWhiteSpace(p.LeadingComment))
                    sb.AppendLine($"{pad}// {p.LeadingComment}");
                sb.AppendLine($"{pad}// pass");
                break;

            case TupleUnpackingAssignment t:
                if (!string.IsNullOrWhiteSpace(t.LeadingComment))
                    sb.AppendLine($"{pad}// {t.LeadingComment}");
                sb.Append(pad);
                sb.Append("// Tuple unpacking not supported in C: (");
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

            case ClassDefStmt c:
                if (!string.IsNullOrWhiteSpace(c.LeadingComment))
                    sb.AppendLine($"{pad}// {c.LeadingComment}");
                if (!string.IsNullOrWhiteSpace(c.BaseClass))
                    sb.AppendLine($"{pad}// Class {c.ClassName} extends {c.BaseClass}");
                else
                    sb.AppendLine($"{pad}// Class {c.ClassName}");
                foreach (var s in c.Body)
                    EmitStmt(s, sb, indent);
                break;

            case TryStmt t:
                if (!string.IsNullOrWhiteSpace(t.LeadingComment))
                    sb.AppendLine($"{pad}// {t.LeadingComment}");
                sb.AppendLine($"{pad}// Try block");
                foreach (var s in t.TryBody)
                    EmitStmt(s, sb, indent);
                if (t.ExceptClauses.Count > 0)
                {
                    foreach (var (exceptionType, varName, body) in t.ExceptClauses)
                    {
                        if (!string.IsNullOrWhiteSpace(exceptionType))
                            sb.AppendLine($"{pad}// Catch {exceptionType}" + (varName != null ? $" as {varName}" : ""));
                        else
                            sb.AppendLine($"{pad}// Catch all exceptions");
                        foreach (var s in body)
                            EmitStmt(s, sb, indent);
                    }
                }
                if (t.FinallyBody != null)
                {
                    sb.AppendLine($"{pad}// Finally block");
                    foreach (var s in t.FinallyBody)
                        EmitStmt(s, sb, indent);
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
                // MVP: map print(x) -> printf("%s", x);
                // For now, only support a single argument.
                if (i.Args.Count != 1)
                    throw new NotSupportedException("C backend currently supports print() with exactly 1 argument.");

                sb.Append("printf(");
                EmitPrintfForSingleArg(i.Args[0], sb);
                sb.Append(")");
                return;

            case Intrinsic i when i.Name == "ternary":
                // ternary(condition, true_expr, false_expr) => condition ? true_expr : false_expr
                if (i.Args.Count >= 3)
                {
                    EmitExpr(i.Args[0], sb);  // condition
                    sb.Append(" ? ");
                    EmitExpr(i.Args[1], sb);  // true_expr
                    sb.Append(" : ");
                    EmitExpr(i.Args[2], sb);  // false_expr
                }
                return;

            case Intrinsic i when i.Name == "raise":
                // C doesn't have exceptions, emit as comment
                sb.Append("/* raise ");
                if (i.Args.Count > 0)
                {
                    EmitExpr(i.Args[0], sb);
                }
                sb.Append(" */");
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

            case DictLiteral d:
                sb.Append("/* dict: {");
                for (int j = 0; j < d.Items.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    EmitExpr(d.Items[j].Key, sb);
                    sb.Append(": ");
                    EmitExpr(d.Items[j].Value, sb);
                }
                sb.Append("} */");
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
                    sb.Append("/* slice: ");
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
                    sb.Append("] */");
                }
                else if (m.MethodName == "__getitem__")
                {
                    // Array indexing in C
                    EmitExpr(m.Target, sb);
                    sb.Append("[");
                    EmitExpr(m.Args[0], sb);
                    sb.Append("]");
                }
                else
                {
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
                }
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

            case ListComprehension lc:
                // C doesn't have native list comprehensions, emit as comment
                sb.Append("/* list comprehension: [");
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
                sb.Append("] */");
                return;

            case DictComprehension dc:
                // C doesn't have native dict comprehensions, emit as comment
                sb.Append("/* dict comprehension: {");
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
                sb.Append("} */");
                return;

            case LambdaExpr lam:
                // C doesn't have native lambdas, emit as comment
                sb.Append("/* lambda ");
                for (int j = 0; j < lam.Parameters.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(lam.Parameters[j]);
                }
                sb.Append(": ");
                EmitExpr(lam.Body, sb);
                sb.Append(" */");
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
