using System.Text.RegularExpressions;
using PLT.CORE.IR;

namespace PLT.CORE.Frontends.CSharp;

public static class CSharpFrontend
{
    public static IrProgram Parse(string source)
    {
        var lexer = new CSharpLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new CSharpParser(tokens);
        return parser.ParseProgram();
    }
}

internal enum TokenType
{
    EOF,
    NEWLINE,
    IDENTIFIER,
    STRING,
    NUMBER,
    BOOL,
    NULL,
    LPAREN,
    RPAREN,
    LBRACKET,
    RBRACKET,
    LBRACE,
    RBRACE,
    COMMA,
    COLON,
    SEMICOLON,
    DOT,
    EQUALS,
    PLUS,
    MINUS,
    STAR,
    SLASH,
    PERCENT,
    AMPERSAND,
    PIPE,
    CARET,
    EQEQ,
    NOTEQ,
    LT,
    GT,
    LTEQ,
    GTEQ,
    PLUSEQ,
    MINUSEQ,
    STAREQ,
    SLASHEQ,
    AND,
    OR,
    NOT,
    QUESTION,
    ARROW,
    KEYWORD,
    COMMENT,
}

internal record Token(TokenType Type, string Value, int Line, int Col);

internal class CSharpLexer
{
    private readonly string _source;
    private int _position = 0;
    private int _line = 1;
    private int _col = 1;
    private readonly List<Token> _tokens = new();

    public CSharpLexer(string source)
    {
        _source = source;
    }

    public List<Token> Tokenize()
    {
        while (_position < _source.Length)
        {
            SkipWhitespaceExceptNewline();
            if (_position >= _source.Length) break;

            var ch = _source[_position];

            if (ch == '\n')
            {
                _tokens.Add(new Token(TokenType.NEWLINE, "\\n", _line, _col));
                _position++;
                _line++;
                _col = 1;
                continue;
            }

            if (ch == '/' && _position + 1 < _source.Length)
            {
                if (_source[_position + 1] == '/')
                {
                    SkipLineComment();
                    continue;
                }
                else if (_source[_position + 1] == '*')
                {
                    SkipBlockComment();
                    continue;
                }
            }

            if (ch == '"')
            {
                ReadString();
                continue;
            }

            if (char.IsDigit(ch))
            {
                ReadNumber();
                continue;
            }

            if (char.IsLetter(ch) || ch == '_' || ch == '@')
            {
                ReadIdentifierOrKeyword();
                continue;
            }

            if (ReadOperator()) continue;

            _position++;
            _col++;
        }

        _tokens.Add(new Token(TokenType.EOF, "", _line, _col));
        return _tokens;
    }

    private void SkipWhitespaceExceptNewline()
    {
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position]) && _source[_position] != '\n')
        {
            _position++;
            _col++;
        }
    }

    private void SkipLineComment()
    {
        while (_position < _source.Length && _source[_position] != '\n')
            _position++;
    }

    private void SkipBlockComment()
    {
        _position += 2; // skip /*
        while (_position + 1 < _source.Length)
        {
            if (_source[_position] == '*' && _source[_position + 1] == '/')
            {
                _position += 2;
                break;
            }
            if (_source[_position] == '\n')
            {
                _line++;
                _col = 1;
            }
            else
            {
                _col++;
            }
            _position++;
        }
    }

    private void ReadString()
    {
        _position++; // skip opening "
        var sb = new System.Text.StringBuilder();

        while (_position < _source.Length && _source[_position] != '"')
        {
            if (_source[_position] == '\\' && _position + 1 < _source.Length)
            {
                _position++;
                var escaped = _source[_position];
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '"' => '"',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(_source[_position]);
            }
            _position++;
            _col++;
        }

        if (_position < _source.Length) _position++; // closing "
        _tokens.Add(new Token(TokenType.STRING, sb.ToString(), _line, _col));
    }

    private void ReadNumber()
    {
        var sb = new System.Text.StringBuilder();
        while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.'))
        {
            sb.Append(_source[_position]);
            _position++;
            _col++;
        }
        _tokens.Add(new Token(TokenType.NUMBER, sb.ToString(), _line, _col));
    }

    private void ReadIdentifierOrKeyword()
    {
        var sb = new System.Text.StringBuilder();
        while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
        {
            sb.Append(_source[_position]);
            _position++;
            _col++;
        }

        var text = sb.ToString();
        var type = text switch
        {
            "true" or "false" => TokenType.BOOL,
            "null" => TokenType.NULL,
            "if" or "else" or "for" or "while" or "do" or "switch" or "case" or "default" or "break" or "continue" or "return" or "void" or "int" or "float" or "double" or "bool" or "string" or "char" or "long" or "decimal" or "var" or "const" or "static" or "public" or "private" or "protected" or "class" or "struct" or "namespace" or "using" or "new" or "this" or "base" or "try" or "catch" or "finally" or "throw" or "and" or "or" or "not" => TokenType.KEYWORD,
            _ => TokenType.IDENTIFIER
        };

        _tokens.Add(new Token(type, text, _line, _col));
    }

    private bool ReadOperator()
    {
        if (_position + 1 < _source.Length)
        {
            var twoChar = _source.Substring(_position, 2);
            var type = twoChar switch
            {
                "==" => TokenType.EQEQ,
                "!=" => TokenType.NOTEQ,
                "<=" => TokenType.LTEQ,
                ">=" => TokenType.GTEQ,
                "&&" => TokenType.AND,
                "||" => TokenType.OR,
                "+=" => TokenType.PLUSEQ,
                "-=" => TokenType.MINUSEQ,
                "*=" => TokenType.STAREQ,
                "/=" => TokenType.SLASHEQ,
                "=>" => TokenType.ARROW,
                _ => (TokenType?)null
            };

            if (type.HasValue)
            {
                _tokens.Add(new Token(type.Value, twoChar, _line, _col));
                _position += 2;
                _col += 2;
                return true;
            }
        }

        var oneChar = _source[_position];
        var singleType = oneChar switch
        {
            '(' => TokenType.LPAREN,
            ')' => TokenType.RPAREN,
            '[' => TokenType.LBRACKET,
            ']' => TokenType.RBRACKET,
            '{' => TokenType.LBRACE,
            '}' => TokenType.RBRACE,
            ',' => TokenType.COMMA,
            ':' => TokenType.COLON,
            ';' => TokenType.SEMICOLON,
            '.' => TokenType.DOT,
            '=' => TokenType.EQUALS,
            '+' => TokenType.PLUS,
            '-' => TokenType.MINUS,
            '*' => TokenType.STAR,
            '/' => TokenType.SLASH,
            '%' => TokenType.PERCENT,
            '&' => TokenType.AMPERSAND,
            '|' => TokenType.PIPE,
            '^' => TokenType.CARET,
            '<' => TokenType.LT,
            '>' => TokenType.GT,
            '!' => TokenType.NOT,
            '?' => TokenType.QUESTION,
            _ => (TokenType?)null
        };

        if (singleType.HasValue)
        {
            _tokens.Add(new Token(singleType.Value, oneChar.ToString(), _line, _col));
            _position++;
            _col++;
            return true;
        }

        return false;
    }
}

internal class CSharpParser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public CSharpParser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public IrProgram ParseProgram()
    {
        var statements = new List<Stmt>();
        SkipNewlines();

        while (!IsAtEnd())
        {
            // Skip using statements
            if (Check(TokenType.KEYWORD) && Peek().Value == "using")
            {
                SkipUsingOrNamespace();
                SkipNewlines();
                continue;
            }

            // Handle class/struct definitions - extract statements from inside
            if (Check(TokenType.KEYWORD) && (Peek().Value == "class" || Peek().Value == "struct"))
            {
                ExtractStatementsFromClassOrStruct(statements);
                SkipNewlines();
                continue;
            }

            var stmt = ParseStatement();
            if (stmt != null) 
            {
                statements.Add(stmt);
            }
            SkipNewlines();
        }

        return new IrProgram(statements);
    }

    private void ExtractStatementsFromClassOrStruct(List<Stmt> statements)
    {
        // Skip class/struct definition until we find {
        while (!Check(TokenType.LBRACE) && !IsAtEnd())
            Advance();
        
        if (!Match(TokenType.LBRACE))
            return;

        // Now we're inside the class body
        // Look for method definitions and extract their body statements
        int braceDepth = 1;
        while (braceDepth > 0 && !IsAtEnd())
        {
            SkipNewlines();

            if (Check(TokenType.RBRACE))
            {
                Advance();
                braceDepth--;
                continue;
            }

            // Skip modifiers and method signatures
            while (Check(TokenType.KEYWORD) && (Peek().Value == "public" || Peek().Value == "private" || Peek().Value == "static" || Peek().Value == "void"))
            {
                Advance();
            }

            // Skip method name and parameters
            if (Check(TokenType.IDENTIFIER))
            {
                Advance(); // method name
                
                // Skip to opening paren and consume method parameters
                while (!Check(TokenType.LPAREN) && !IsAtEnd())
                    Advance();
                
                if (Match(TokenType.LPAREN))
                {
                    // Skip parameters
                    int parenDepth = 1;
                    while (parenDepth > 0 && !IsAtEnd())
                    {
                        if (Check(TokenType.LPAREN)) parenDepth++;
                        else if (Check(TokenType.RPAREN)) parenDepth--;
                        Advance();
                    }
                }

                SkipNewlines();

                // Now should be at the method body {
                if (Match(TokenType.LBRACE))
                {
                    // Parse the method body
                    int methodBraceDepth = 1;
                    while (methodBraceDepth > 0 && !IsAtEnd())
                    {
                        SkipNewlines();
                        if (Check(TokenType.RBRACE))
                        {
                            Advance();
                            methodBraceDepth--;
                        }
                        else if (Check(TokenType.LBRACE))
                        {
                            Advance();
                            methodBraceDepth++;
                        }
                        else
                        {
                            var stmt = ParseStatement();
                            if (stmt != null) statements.Add(stmt);
                        }
                    }
                }
            }
            else
            {
                Advance(); // skip unknown token
            }
        }
    }

    private void SkipUsingOrNamespace()
    {
        // Skip to either semicolon (for using) or opening brace (for namespace/class)
        while (!Check(TokenType.SEMICOLON) && !Check(TokenType.LBRACE) && !IsAtEnd())
            Advance();
        
        if (Match(TokenType.SEMICOLON)) 
        { 
            return; 
        }
        
        if (Check(TokenType.LBRACE))
        {
            Advance(); // consume {
            int braceCount = 1;
            while (braceCount > 0 && !IsAtEnd())
            {
                if (Check(TokenType.LBRACE)) { Advance(); braceCount++; }
                else if (Check(TokenType.RBRACE)) { Advance(); braceCount--; }
                else Advance();
            }
        }
    }

    private Stmt? ParseStatement()
    {
        SkipNewlines();

        // Check for closing brace - end of block
        if (Check(TokenType.RBRACE))
        {
            return null;
        }

        if (Check(TokenType.KEYWORD))
        {
            var keyword = Peek().Value;
            
            // Skip modifiers, namespaces, classes
            if (keyword == "namespace" || keyword == "class" || keyword == "struct" 
                || keyword == "public" || keyword == "private" || keyword == "protected" 
                || keyword == "static" || keyword == "using")
            {
                SkipUntilSemicolonOrBrace();
                return null;
            }
            
            // Skip control flow statements
            if (keyword == "return" || keyword == "break" || keyword == "continue")
            {
                Advance();
                ConsumeSemicolonIfPresent();
                return null;
            }
            
            return keyword switch
            {
                "if" => ParseIfStatement(),
                "for" => ParseForStatement(),
                "while" => ParseWhileStatement(),
                "do" => ParseDoWhileStatement(),
                "var" or "int" or "float" or "double" or "bool" or "string" or "char" or "long" or "decimal" => ParseTypedAssignment(),
                _ => ParseExpressionStatement()
            };
        }

        return ParseExpressionStatement();
    }

    private void SkipUntilSemicolonOrBrace()
    {
        // Skip everything until we hit a semicolon or opening brace
        int depth = 0;
        while (!IsAtEnd())
        {
            if (Check(TokenType.SEMICOLON) && depth == 0)
            {
                Advance();
                return;
            }
            if (Check(TokenType.LBRACE))
            {
                Advance();
                // Now skip the entire block
                depth = 1;
                while (depth > 0 && !IsAtEnd())
                {
                    if (Check(TokenType.LBRACE)) depth++;
                    else if (Check(TokenType.RBRACE)) depth--;
                    Advance();
                }
                return;
            }
            Advance();
        }
    }

    private void SkipClassMethodDef()
    {
        // Skip modifiers and type info until we hit { or ;
        while (!Check(TokenType.LBRACE) && !Check(TokenType.SEMICOLON) && !IsAtEnd())
            Advance();

        if (Match(TokenType.LBRACE))
        {
            // Skip method body
            int braceCount = 1;
            while (braceCount > 0 && !IsAtEnd())
            {
                if (Check(TokenType.LBRACE)) { Advance(); braceCount++; }
                else if (Check(TokenType.RBRACE)) { Advance(); braceCount--; }
                else Advance();
            }
        }
        else if (Match(TokenType.SEMICOLON))
        {
            // Skip
        }
    }

    private VarAssignment ParseTypedAssignment()
    {
        var type = Advance().Value; // consume type
        var varName = Consume(TokenType.IDENTIFIER, "Expected variable name").Value;
        Consume(TokenType.EQUALS, "Expected '='");
        var value = ParseOrExpression();
        ConsumeSemicolonIfPresent();
        return new VarAssignment(varName, value);
    }



    private IfStmt ParseIfStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'if'");
        Consume(TokenType.LPAREN, "Expected '('");
        var condition = ParseOrExpression();
        Consume(TokenType.RPAREN, "Expected ')'");
        SkipNewlines();

        var thenBody = new List<Stmt>();
        if (Match(TokenType.LBRACE))
        {
            thenBody = ParseBlock();
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null) thenBody.Add(stmt);
        }

        IReadOnlyList<Stmt>? elseBody = null;
        SkipNewlines();

        if (Check(TokenType.KEYWORD) && Peek().Value == "else")
        {
            Advance(); // consume 'else'
            SkipNewlines();
            elseBody = new List<Stmt>();
            if (Match(TokenType.LBRACE))
            {
                elseBody = ParseBlock();
            }
            else
            {
                var stmt = ParseStatement();
                if (stmt != null) ((List<Stmt>)elseBody).Add(stmt);
            }
        }

        return new IfStmt(condition, thenBody, elseBody);
    }

    private ForEachStmt ParseForStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'for'");
        Consume(TokenType.LPAREN, "Expected '('");

        // Try to parse as foreach: for (type var in collection)
        int checkPoint = _current;
        bool isForEach = false;
        string loopVar = "";
        Expr iterExpr = null!;

        try
        {
            // Skip type if present
            if (Check(TokenType.KEYWORD) && IsType(Peek().Value))
                Advance();

            if (Check(TokenType.IDENTIFIER))
            {
                loopVar = Advance().Value;
                if (Check(TokenType.KEYWORD) && Peek().Value == "in")
                {
                    isForEach = true;
                    Advance(); // consume 'in'
                    iterExpr = ParseOrExpression();
                }
            }
        }
        catch { }

        if (!isForEach)
        {
            _current = checkPoint;
            // Parse as traditional C# for loop - simplified: just skip it for now
            while (!Check(TokenType.RPAREN) && !IsAtEnd())
                Advance();
            Consume(TokenType.RPAREN, "Expected ')'");
            var body = new List<Stmt>();
            if (Match(TokenType.LBRACE)) body = ParseBlock();
            return new ForEachStmt("_unused", new Literal(0), body);
        }

        Consume(TokenType.RPAREN, "Expected ')'");
        SkipNewlines();

        var forBody = new List<Stmt>();
        if (Match(TokenType.LBRACE))
        {
            forBody = ParseBlock();
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null) forBody.Add(stmt);
        }

        return new ForEachStmt(loopVar, iterExpr, forBody);
    }

    private WhileStmt ParseWhileStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'while'");
        Consume(TokenType.LPAREN, "Expected '('");
        var condition = ParseOrExpression();
        Consume(TokenType.RPAREN, "Expected ')'");
        SkipNewlines();

        var body = new List<Stmt>();
        if (Match(TokenType.LBRACE))
        {
            body = ParseBlock();
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null) body.Add(stmt);
        }

        return new WhileStmt(condition, body);
    }

    private WhileStmt ParseDoWhileStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'do'");
        var body = new List<Stmt>();
        if (Match(TokenType.LBRACE))
        {
            body = ParseBlock();
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null) body.Add(stmt);
        }

        Consume(TokenType.KEYWORD, "Expected 'while'");
        Consume(TokenType.LPAREN, "Expected '('");
        var condition = ParseOrExpression();
        Consume(TokenType.RPAREN, "Expected ')'");
        ConsumeSemicolonIfPresent();

        return new WhileStmt(condition, body);
    }

    private List<Stmt> ParseBlock()
    {
        var statements = new List<Stmt>();

        while (!Check(TokenType.RBRACE) && !IsAtEnd())
        {
            SkipNewlines();
            if (Check(TokenType.RBRACE)) break;
            var stmt = ParseStatement();
            if (stmt != null) statements.Add(stmt);
        }

        if (Check(TokenType.RBRACE)) Advance();
        return statements;
    }

    private Stmt ParseExpressionStatement()
    {
        // Try to parse as an expression first
        var startPos = _current;
        
        // Try to parse as a simple assignment (var = value)
        if (Check(TokenType.IDENTIFIER))
        {
            var savedPos = _current;
            var varName = Advance().Value;
            
            if (Match(TokenType.EQUALS))
            {
                var value = ParseOrExpression();
                ConsumeSemicolonIfPresent();
                return new VarAssignment(varName, value);
            }
            else
            {
                // Not an assignment, restore position
                _current = savedPos;
            }
        }
        
        // Regular expression statement
        var expr = ParseOrExpression();
        ConsumeSemicolonIfPresent();
        return new ExprStmt(expr);
    }

    private Expr ParseAssignmentExpression()
    {
        return ParseOrExpression();
    }

    private Expr ParseOrExpression()
    {
        var expr = ParseAndExpression();

        while (Match(TokenType.OR) || (Check(TokenType.KEYWORD) && Peek().Value == "or"))
        {
            var op = "||";
            var right = ParseAndExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseAndExpression()
    {
        var expr = ParseComparisonExpression();

        while (Match(TokenType.AND) || (Check(TokenType.KEYWORD) && Peek().Value == "and"))
        {
            var op = "&&";
            var right = ParseComparisonExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseComparisonExpression()
    {
        var expr = ParseAdditiveExpression();

        while (Match(TokenType.EQEQ, TokenType.NOTEQ, TokenType.LT, TokenType.GT, TokenType.LTEQ, TokenType.GTEQ))
        {
            var op = Previous().Value;
            var right = ParseAdditiveExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseAdditiveExpression()
    {
        var expr = ParseMultiplicativeExpression();

        while (Match(TokenType.PLUS, TokenType.MINUS))
        {
            var op = Previous().Value;
            var right = ParseMultiplicativeExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseMultiplicativeExpression()
    {
        var expr = ParseUnaryExpression();

        while (Match(TokenType.STAR, TokenType.SLASH, TokenType.PERCENT))
        {
            var op = Previous().Value;
            var right = ParseUnaryExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseUnaryExpression()
    {
        if (Match(TokenType.NOT) || (Check(TokenType.KEYWORD) && Peek().Value == "not"))
        {
            var op = "!";
            var expr = ParseUnaryExpression();
            return new UnaryOp(op, expr);
        }

        if (Match(TokenType.MINUS))
        {
            var expr = ParseUnaryExpression();
            return new UnaryOp("-", expr);
        }

        return ParsePostfixExpression();
    }

    private Expr ParsePostfixExpression()
    {
        var expr = ParsePrimaryExpression();

        while (true)
        {
            if (Match(TokenType.DOT))
            {
                var methodName = Consume(TokenType.IDENTIFIER, "Expected identifier").Value;
                if (Match(TokenType.LPAREN))
                {
                    var args = ParseArguments();
                    Consume(TokenType.RPAREN, "Expected ')'");
                    expr = new MethodCall(expr, methodName, args);
                }
                else
                {
                    expr = new MethodCall(expr, methodName, new List<Expr>());
                }
            }
            else if (Check(TokenType.LPAREN) && expr is Variable v)
            {
                Advance();
                var args = ParseArguments();
                Consume(TokenType.RPAREN, "Expected ')'");
                expr = new FunctionCall(v.Name, args);
            }
            else if (Match(TokenType.LBRACKET))
            {
                var index = ParseOrExpression();
                Consume(TokenType.RBRACKET, "Expected ']'");
                expr = new MethodCall(expr, "__getitem__", new List<Expr> { index });
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expr ParsePrimaryExpression()
    {
        if (Match(TokenType.NUMBER))
        {
            var value = Previous().Value;
            return new Literal(double.TryParse(value, out var d) ? d : int.Parse(value));
        }

        if (Match(TokenType.STRING))
        {
            return new Literal(Previous().Value);
        }

        if (Match(TokenType.BOOL))
        {
            return new Literal(Previous().Value == "true");
        }

        if (Match(TokenType.NULL))
        {
            return new Literal(null);
        }

        if (Match(TokenType.IDENTIFIER))
        {
            return new Variable(Previous().Value);
        }

        if (Match(TokenType.LPAREN))
        {
            var expr = ParseAssignmentExpression();
            Consume(TokenType.RPAREN, "Expected ')'");
            return expr;
        }

        throw new Exception($"Unexpected token: {Peek()}");
    }

    private List<Expr> ParseArguments()
    {
        var args = new List<Expr>();
        if (!Check(TokenType.RPAREN))
        {
            do
            {
                args.Add(ParseOrExpression());
            } while (Match(TokenType.COMMA));
        }
        return args;
    }

    private bool IsType(string keyword) =>
        keyword is "var" or "int" or "float" or "double" or "bool" or "string" or "char" or "long" or "decimal";

    private void SkipNewlines()
    {
        while (Match(TokenType.NEWLINE)) { }
    }

    private void ConsumeSemicolonIfPresent()
    {
        if (Check(TokenType.SEMICOLON)) Advance();
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.EOF;

    private Token Peek() => _tokens[_current];

    private Token? PeekNext() => _current + 1 < _tokens.Count ? _tokens[_current + 1] : null;

    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new Exception($"{message} at {Peek()}");
    }
}
