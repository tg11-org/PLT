using System.Text.RegularExpressions;
using PLT.CORE.IR;

namespace PLT.CORE.Frontends.Python;

public static class PythonFrontend
{
    public static IrProgram Parse(string source)
    {
        var lexer = new PythonLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new PythonParser(tokens);
        return parser.ParseProgram();
    }
}

internal enum TokenType
{
    EOF,
    NEWLINE,
    INDENT,
    DEDENT,
    IDENTIFIER,
    STRING,
    NUMBER,
    BOOL,
    NONE,
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
    EQEQ,
    NOTEQ,
    LT,
    GT,
    LTEQ,
    GTEQ,
    AND,
    OR,
    NOT,
    KEYWORD,
    COMMENT,
}

internal record Token(TokenType Type, string Value, int Line, int Col);

internal class PythonLexer
{
    private readonly string _source;
    private int _position = 0;
    private int _line = 1;
    private int _col = 1;
    private readonly List<Token> _tokens = new();
    private int _indentLevel = 0;

    public PythonLexer(string source)
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
                HandleIndentation();
                continue;
            }

            if (ch == '#')
            {
                SkipComment();
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                ReadString();
                continue;
            }

            if (char.IsDigit(ch))
            {
                ReadNumber();
                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
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

    private void SkipComment()
    {
        while (_position < _source.Length && _source[_position] != '\n')
            _position++;
    }

    private void HandleIndentation()
    {
        int spaces = 0;
        while (_position < _source.Length && _source[_position] == ' ')
        {
            spaces++;
            _position++;
        }

        int newIndentLevel = spaces / 4;
        while (_indentLevel > newIndentLevel)
        {
            _tokens.Add(new Token(TokenType.DEDENT, "", _line, _col));
            _indentLevel--;
        }
        while (_indentLevel < newIndentLevel)
        {
            _tokens.Add(new Token(TokenType.INDENT, "", _line, _col));
            _indentLevel++;
        }
        _col = spaces + 1;
    }

    private void ReadString()
    {
        var quote = _source[_position];
        _position++;
        var sb = new System.Text.StringBuilder();

        while (_position < _source.Length && _source[_position] != quote)
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
                    '\'' => '\'',
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

        if (_position < _source.Length) _position++; // closing quote
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
            "True" or "False" => TokenType.BOOL,
            "None" => TokenType.NONE,
            "if" or "elif" or "else" or "for" or "while" or "def" or "return" or "import" or "from" or "as" or "class" or "try" or "except" or "finally" or "with" or "pass" or "break" or "continue" or "in" or "and" or "or" or "not" => TokenType.KEYWORD,
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
                "and" => TokenType.AND,
                "or" => TokenType.OR,
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
            '<' => TokenType.LT,
            '>' => TokenType.GT,
            '!' => TokenType.NOT,
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

internal class PythonParser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public PythonParser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public IrProgram ParseProgram()
    {
        var statements = new List<Stmt>();
        SkipNewlines();

        while (!IsAtEnd())
        {
            if (IsAtEnd()) break;
            var stmt = ParseStatement();
            if (stmt != null) statements.Add(stmt);
            SkipNewlines();
        }

        return new IrProgram(statements);
    }

    private Stmt? ParseStatement()
    {
        SkipNewlines();

        if (Check(TokenType.KEYWORD))
        {
            var keyword = Peek().Value;
            return keyword switch
            {
                "if" => ParseIfStatement(),
                "for" => ParseForStatement(),
                "while" => ParseWhileStatement(),
                "def" => ParseFunctionDef(),
                _ => ParseExpressionStatement()
            };
        }

        if (Check(TokenType.IDENTIFIER) && PeekNext()?.Type == TokenType.EQUALS)
        {
            return ParseAssignment();
        }

        return ParseExpressionStatement();
    }

    private Stmt ParseExpressionStatement()
    {
        var expr = ParseExpression();
        SkipNewlines();
        return new ExprStmt(expr);
    }

    private VarAssignment ParseAssignment()
    {
        var varName = Consume(TokenType.IDENTIFIER, "Expected variable name").Value;
        Consume(TokenType.EQUALS, "Expected '='");
        var value = ParseExpression();
        SkipNewlines();
        return new VarAssignment(varName, value);
    }

    private IfStmt ParseIfStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'if'");
        var condition = ParseExpression();
        Consume(TokenType.COLON, "Expected ':'");
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var thenBody = ParseIndentedBlock();
        IReadOnlyList<Stmt>? elseBody = null;

        if (Check(TokenType.DEDENT)) Advance();
        SkipNewlines();

        if (Match(TokenType.KEYWORD) && Previous().Value == "else")
        {
            Consume(TokenType.COLON, "Expected ':'");
            SkipNewlines();
            Consume(TokenType.INDENT, "Expected indented block");
            elseBody = ParseIndentedBlock();
            if (Check(TokenType.DEDENT)) Advance();
        }

        return new IfStmt(condition, thenBody, elseBody);
    }

    private ForEachStmt ParseForStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'for'");
        var loopVar = Consume(TokenType.IDENTIFIER, "Expected loop variable").Value;
        if (!Check(TokenType.KEYWORD) || Peek().Value != "in")
            throw new Exception("Expected 'in' after loop variable");
        Advance();  // consume 'in'
        var iterExpr = ParseExpression();
        Consume(TokenType.COLON, "Expected ':'");
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var body = ParseIndentedBlock();
        if (Check(TokenType.DEDENT)) Advance();

        return new ForEachStmt(loopVar, iterExpr, body);
    }

    private WhileStmt ParseWhileStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'while'");
        var condition = ParseExpression();
        Consume(TokenType.COLON, "Expected ':'");
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var body = ParseIndentedBlock();
        if (Check(TokenType.DEDENT)) Advance();

        return new WhileStmt(condition, body);
    }

    private FunctionDefStmt ParseFunctionDef()
    {
        Consume(TokenType.KEYWORD, "Expected 'def'");
        var funcName = Consume(TokenType.IDENTIFIER, "Expected function name").Value;
        Consume(TokenType.LPAREN, "Expected '('");

        var parameters = new List<string>();
        if (!Check(TokenType.RPAREN))
        {
            do
            {
                parameters.Add(Consume(TokenType.IDENTIFIER, "Expected parameter name").Value);
            } while (Match(TokenType.COMMA));
        }
        Consume(TokenType.RPAREN, "Expected ')'");
        Consume(TokenType.COLON, "Expected ':'");
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var body = ParseIndentedBlock();
        if (Check(TokenType.DEDENT)) Advance();

        return new FunctionDefStmt(funcName, parameters, body);
    }

    private List<Stmt> ParseIndentedBlock()
    {
        var statements = new List<Stmt>();

        while (!Check(TokenType.DEDENT) && !IsAtEnd())
        {
            SkipNewlines();
            if (Check(TokenType.DEDENT)) break;
            var stmt = ParseStatement();
            if (stmt != null) statements.Add(stmt);
        }

        return statements;
    }

    private Expr ParseExpression()
    {
        return ParseOrExpression();
    }

    private Expr ParseOrExpression()
    {
        var expr = ParseAndExpression();

        while (Match(TokenType.OR) || (Check(TokenType.KEYWORD) && Peek().Value == "or"))
        {
            var op = "or";
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
            var op = "and";
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
            var op = "not";
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
                var methodName = Consume(TokenType.IDENTIFIER, "Expected method name").Value;
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
                var index = ParseExpression();
                Consume(TokenType.RBRACKET, "Expected ']'");
                // For now, treat as method call
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
            var value = Previous().Value;
            // Check for f-strings
            return new Literal(value);
        }

        if (Match(TokenType.BOOL))
        {
            return new Literal(Previous().Value == "True");
        }

        if (Match(TokenType.NONE))
        {
            return new Literal(null);
        }

        if (Match(TokenType.IDENTIFIER))
        {
            return new Variable(Previous().Value);
        }

        if (Match(TokenType.LPAREN))
        {
            var expr = ParseExpression();
            Consume(TokenType.RPAREN, "Expected ')'");
            return expr;
        }

        if (Match(TokenType.LBRACKET))
        {
            var elements = new List<Expr>();
            if (!Check(TokenType.RBRACKET))
            {
                do
                {
                    elements.Add(ParseExpression());
                } while (Match(TokenType.COMMA));
            }
            Consume(TokenType.RBRACKET, "Expected ']'");
            return new ListLiteral(elements);
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
                args.Add(ParseExpression());
            } while (Match(TokenType.COMMA));
        }
        return args;
    }

    private void SkipNewlines()
    {
        while (Match(TokenType.NEWLINE)) { }
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
