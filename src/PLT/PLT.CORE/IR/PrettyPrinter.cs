using System.Text;

namespace PLT.CORE.IR;

public static class PrettyPrinter
{
    public static string Print(Node node)
    {
        var sb = new StringBuilder();
        PrintNode(node, sb, indent: 0);
        return sb.ToString();
    }

    private static void PrintNode(Node node, StringBuilder sb, int indent)
    {
        var pad = new string(' ', indent * 2);

        switch (node)
        {
            case IrProgram p:
                sb.AppendLine($"{pad}IrProgram");
                foreach (var stmt in p.Body)
                    PrintNode(stmt, sb, indent + 1);
                break;

            case ExprStmt s:
                if (!string.IsNullOrWhiteSpace(s.LeadingComment))
                    sb.AppendLine($"{pad}// {s.LeadingComment}");
                sb.AppendLine($"{pad}ExprStmt");
                PrintNode(s.Expr, sb, indent + 1);
                break;

            case Intrinsic i:
                sb.AppendLine($"{pad}Intrinsic \"{i.Name}\"");
                foreach (var arg in i.Args)
                    PrintNode(arg, sb, indent + 1);
                break;

            case Literal l:
                sb.AppendLine($"{pad}Literal {FormatLiteral(l.Value)}");
                break;

            default:
                sb.AppendLine($"{pad}{node.GetType().Name}");
                break;
        }
    }

    private static string FormatLiteral(object? value) =>
        value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? ""
        };
}
