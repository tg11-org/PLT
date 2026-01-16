namespace PLT.CORE.IR;

public abstract record Node;

public record IrProgram(IReadOnlyList<Stmt> Body) : Node;

// Statements
public abstract record Stmt : Node;

public record ExprStmt(Expr Expr, string? LeadingComment = null) : Stmt;

public record VarAssignment(string VarName, Expr Value, string? LeadingComment = null) : Stmt;

public record TupleUnpackingAssignment(IReadOnlyList<string> VarNames, Expr Value, string? LeadingComment = null) : Stmt;

public record PassStmt(string? LeadingComment = null) : Stmt;

public record IfStmt(Expr Condition, IReadOnlyList<Stmt> ThenBody, IReadOnlyList<Stmt>? ElseBody = null, string? LeadingComment = null) : Stmt;

public record ForEachStmt(string LoopVar, Expr IterableExpr, IReadOnlyList<Stmt> Body, string? LeadingComment = null) : Stmt;

public record WhileStmt(Expr Condition, IReadOnlyList<Stmt> Body, string? LeadingComment = null) : Stmt;

public record FunctionDefStmt(string FunctionName, IReadOnlyList<string> Parameters, IReadOnlyList<Stmt> Body, string? LeadingComment = null) : Stmt;

public record ClassDefStmt(string ClassName, IReadOnlyList<Stmt> Body, string? BaseClass = null, string? LeadingComment = null) : Stmt;

public record TryStmt(IReadOnlyList<Stmt> TryBody, IReadOnlyList<(string? ExceptionType, string? VarName, IReadOnlyList<Stmt> Body)> ExceptClauses, IReadOnlyList<Stmt>? FinallyBody = null, string? LeadingComment = null) : Stmt;

// Expressions
public abstract record Expr : Node;

public record Literal(object? Value) : Expr;

public record Variable(string Name) : Expr;

public record StringInterpolation(IReadOnlyList<StringPart> Parts) : Expr;

public abstract record StringPart : Node;
public record StringPartLiteral(string Value) : StringPart;
public record StringPartVariable(string VarName) : StringPart;

public record ListLiteral(IReadOnlyList<Expr> Elements) : Expr;

public record DictLiteral(IReadOnlyList<(Expr Key, Expr Value)> Items) : Expr;

public record ListComprehension(Expr Element, string LoopVar, Expr IterableExpr, Expr? FilterCondition = null) : Expr;

public record DictComprehension(Expr KeyExpr, Expr ValueExpr, string LoopVar, Expr IterableExpr, Expr? FilterCondition = null) : Expr;

public record LambdaExpr(IReadOnlyList<string> Parameters, Expr Body) : Expr;

public record BinaryOp(Expr Left, string Op, Expr Right) : Expr;

public record UnaryOp(string Op, Expr Operand) : Expr;

public record FunctionCall(string FunctionName, IReadOnlyList<Expr> Args, bool IsNamespaced = false, string? Namespace = null) : Expr;

public record MethodCall(Expr Target, string MethodName, IReadOnlyList<Expr> Args) : Expr;

public record Intrinsic(string Name, IReadOnlyList<Expr> Args) : Expr;
