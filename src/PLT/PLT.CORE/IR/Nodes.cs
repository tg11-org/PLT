namespace PLT.CORE.IR;

public abstract record Node;

public record IrProgram(IReadOnlyList<Stmt> Body) : Node;

// Statements
public abstract record Stmt : Node;

public record ExprStmt(Expr Expr, string? LeadingComment = null) : Stmt;

// Expressions
public abstract record Expr : Node;

public record Literal(object? Value) : Expr;

public record Intrinsic(string Name, IReadOnlyList<Expr> Args) : Expr;
