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
    PLUSEQ,
    MINUSEQ,
    STAREQ,
    SLASHEQ,
    PLUS,
    MINUS,
    STAR,
    SLASH,
    PERCENT,
    STARSTAR,      // **
    SLASHSLASH,    // //
    EQEQ,
    NOTEQ,
    LT,
    GT,
    LTEQ,
    GTEQ,
    LTLT,          // <<
    GTGT,          // >>
    AMPERSAND,     // &
    PIPE,          // |
    CARET,         // ^
    TILDE,         // ~
    AND,
    OR,
    NOT,
    AT,
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
            "if" or "elif" or "else" or "for" or "while" or "def" or "return" or "import" or "from" or "as" or "class" or "try" or "except" or "finally" or "with" or "pass" or "break" or "continue" or "in" or "and" or "or" or "not" or "lambda" or "raise" => TokenType.KEYWORD,
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
                "+=" => TokenType.PLUSEQ,
                "-=" => TokenType.MINUSEQ,
                "*=" => TokenType.STAREQ,
                "/=" => TokenType.SLASHEQ,
                "**" => TokenType.STARSTAR,
                "//" => TokenType.SLASHSLASH,
                "<<" => TokenType.LTLT,
                ">>" => TokenType.GTGT,
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
            '@' => TokenType.AT,
            '&' => TokenType.AMPERSAND,
            '|' => TokenType.PIPE,
            '^' => TokenType.CARET,
            '~' => TokenType.TILDE,
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
            
            // Handle semicolon-separated statements on same line
            while (Match(TokenType.SEMICOLON))
            {
                if (Check(TokenType.NEWLINE) || IsAtEnd()) break;
                stmt = ParseStatement();
                if (stmt != null) statements.Add(stmt);
            }
            
            SkipNewlines();
        }

        return new IrProgram(statements);
    }

    private Stmt? ParseStatement()
    {
        SkipNewlines();

        // Skip decorators
        if (Check(TokenType.AT))
        {
            SkipDecorator();
            return ParseStatement(); // Parse the next statement (function/class)
        }

        if (Check(TokenType.KEYWORD))
        {
            var keyword = Peek().Value;
            return keyword switch
            {
                "import" => ParseImportStatement(),
                "from" => ParseFromImportStatement(),
                "return" => ParseReturnStatement(),
                "raise" => ParseRaiseStatement(),
                "class" => ParseClassDef(),
                "try" => ParseTryStatement(),
                "if" => ParseIfStatement(),
                "for" => ParseForStatement(),
                "while" => ParseWhileStatement(),
                "def" => ParseFunctionDef(),
                _ => ParseExpressionStatement()
            };
        }

        if (Check(TokenType.IDENTIFIER))
        {
            var next = PeekNext();
            // Check if it's an assignment or augmented assignment or member attribute assignment
            if (next?.Type == TokenType.EQUALS || next?.Type == TokenType.PLUSEQ || 
                next?.Type == TokenType.MINUSEQ || next?.Type == TokenType.STAREQ || next?.Type == TokenType.SLASHEQ)
            {
                return ParseAssignment();
            }
            // For member access (obj.attr), check if it's an assignment or expression
            else if (next?.Type == TokenType.DOT)
            {
                // Look ahead to see if this is obj.attr = value or just obj.attr(...)
                var pos = _current + 1; // position of the DOT
                if (pos + 1 < _tokens.Count && _tokens[pos + 1].Type == TokenType.IDENTIFIER)
                {
                    pos += 2; // position after the attribute name
                    if (pos < _tokens.Count && (_tokens[pos].Type == TokenType.EQUALS || _tokens[pos].Type == TokenType.PLUSEQ || 
                        _tokens[pos].Type == TokenType.MINUSEQ || _tokens[pos].Type == TokenType.STAREQ || _tokens[pos].Type == TokenType.SLASHEQ))
                    {
                        return ParseAssignment(); // obj.attr = value
                    }
                }
                // Otherwise it's an expression (method call or property access)
                return ParseExpressionStatement();
            }
            // Type annotation: var: type = value
            else if (next?.Type == TokenType.COLON)
            {
                return ParseAssignment();
            }
        }
        
        // Check for tuple unpacking: (var1, var2) = expression
        if (Check(TokenType.LPAREN))
        {
            return ParseTupleUnpacking();
        }

        return ParseExpressionStatement();
    }

    private void SkipDecorator()
    {
        // Skip @ and decorator name/call
        Consume(TokenType.AT, "Expected '@'");
        while (!Check(TokenType.NEWLINE) && !IsAtEnd())
            Advance();
        if (Match(TokenType.NEWLINE)) { }
    }

    private Stmt ParseImportStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'import'");
        // Skip the entire import line
        while (!Check(TokenType.NEWLINE) && !IsAtEnd())
            Advance();
        SkipNewlines();
        return null!; // Return empty statement
    }

    private Stmt ParseFromImportStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'from'");
        // Skip the entire from...import line
        while (!Check(TokenType.NEWLINE) && !IsAtEnd())
            Advance();
        SkipNewlines();
        return null!; // Return empty statement
    }

    private Stmt ParseReturnStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'return'");
        
        // Check if there's a value to return
        if (Check(TokenType.NEWLINE) || IsAtEnd())
        {
            SkipNewlines();
            return new ExprStmt(new Literal(null)); // Return None
        }

        var value = ParseExpression();
        SkipNewlines();
        return new ExprStmt(value); // Treat return as expression statement
    }

    private Stmt ParseRaiseStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'raise'");
        
        // Check if there's an exception to raise
        if (Check(TokenType.NEWLINE) || IsAtEnd())
        {
            SkipNewlines();
            // Re-raise current exception
            return new ExprStmt(new Intrinsic("raise", new List<Expr>()));
        }

        var exception = ParseExpression();
        SkipNewlines();
        // Treat raise as an intrinsic function call: raise(exception)
        return new ExprStmt(new Intrinsic("raise", new List<Expr> { exception }));
    }

    private Stmt ParseTupleUnpacking()
    {
        // Parse pattern: (var1, var2, ...) = expression
        Consume(TokenType.LPAREN, "Expected '('");
        
        var varNames = new List<string>();
        
        // Parse comma-separated variable names
        do
        {
            var varName = Consume(TokenType.IDENTIFIER, "Expected variable name").Value;
            varNames.Add(varName);
            
            // Skip trailing comma for single-element tuples: (x,) = ...
            if (!Check(TokenType.COMMA))
                break;
            Advance(); // consume comma
        } while (!Check(TokenType.RPAREN));
        
        Consume(TokenType.RPAREN, "Expected ')'");
        Consume(TokenType.EQUALS, "Expected '='");
        
        var value = ParseExpression();
        SkipNewlines();
        
        return new TupleUnpackingAssignment(varNames, value);
    }

    private Stmt ParseExpressionStatement()
    {
        var expr = ParseExpression();
        SkipNewlines();
        return new ExprStmt(expr);
    }

    private Stmt ParseAssignment()
    {
        var varName = Consume(TokenType.IDENTIFIER, "Expected variable name").Value;
        
        // Check for member attribute access: obj.attr = value or obj.attr += value, etc
        if (Match(TokenType.DOT))
        {
            var attrName = Consume(TokenType.IDENTIFIER, "Expected attribute name").Value;
            
            // Handle augmented assignment for member attributes
            if (Check(TokenType.PLUSEQ) || Check(TokenType.MINUSEQ) || Check(TokenType.STAREQ) || Check(TokenType.SLASHEQ))
            {
                var opToken = Advance();
                var rhs = ParseExpression();
                SkipNewlines();
                
                // obj.attr += value  =>  obj.attr = obj.attr + value
                var op = opToken.Value switch
                {
                    "+=" => "+",
                    "-=" => "-",
                    "*=" => "*",
                    "/=" => "/",
                    _ => "+"
                };
                
                var getAttr = new Intrinsic("getattr", new List<Expr> { 
                    new Variable(varName), 
                    new Literal(attrName) 
                });
                var binaryOp = new BinaryOp(getAttr, op, rhs);
                return new ExprStmt(new Intrinsic("setattr", new List<Expr> { 
                    new Variable(varName), 
                    new Literal(attrName), 
                    binaryOp
                }));
            }
            
            Consume(TokenType.EQUALS, "Expected '='");
            var attrValue = ParseExpression();
            SkipNewlines();
            // Return as expression statement with a special method call pattern
            // This represents: setattr(varName, attrName, attrValue)
            return new ExprStmt(new Intrinsic("setattr", new List<Expr> { 
                new Variable(varName), 
                new Literal(attrName), 
                attrValue 
            }));
        }
        
        // Check for augmented assignment: +=, -=, *=, /=
        if (Check(TokenType.PLUSEQ) || Check(TokenType.MINUSEQ) || Check(TokenType.STAREQ) || Check(TokenType.SLASHEQ))
        {
            var opToken = Advance();
            var assignValue = ParseExpression();
            SkipNewlines();
            
            // Convert augmented assignment to regular assignment with binary operation
            // x += y  =>  x = x + y
            var op = opToken.Value switch
            {
                "+=" => "+",
                "-=" => "-",
                "*=" => "*",
                "/=" => "/",
                _ => "+"
            };
            
            var binaryOp = new BinaryOp(new Variable(varName), op, assignValue);
            return new VarAssignment(varName, binaryOp);
        }
        
        // Skip type annotation if present: var: type = value
        if (Match(TokenType.COLON))
        {
            SkipTypeAnnotation();
        }
        
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
        
        // Check for single-line if statement (if condition: statement)
        if (!Check(TokenType.NEWLINE))
        {
            // Parse single or multiple semicolon-separated statements on same line
            var thenBody = new List<Stmt>();
            var stmt = ParseStatement();
            if (stmt != null) thenBody.Add(stmt);
            
            // Handle semicolon-separated statements: if cond: stmt1; stmt2
            while (Match(TokenType.SEMICOLON))
            {
                if (Check(TokenType.NEWLINE) || IsAtEnd()) break;
                stmt = ParseStatement();
                if (stmt != null) thenBody.Add(stmt);
            }
            
            return new IfStmt(condition, thenBody, null);
        }
        
        // Multi-line if statement
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var thenBodyList = ParseIndentedBlock();
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

        return new IfStmt(condition, thenBodyList, elseBody);
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

    private TryStmt ParseTryStatement()
    {
        Consume(TokenType.KEYWORD, "Expected 'try'");
        Consume(TokenType.COLON, "Expected ':'");
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");
        var tryBody = ParseIndentedBlock();
        if (Check(TokenType.DEDENT)) Advance();

        var exceptClauses = new List<(string? ExceptionType, string? VarName, IReadOnlyList<Stmt> Body)>();
        
        while (Check(TokenType.KEYWORD) && Peek().Value == "except")
        {
            Advance(); // consume 'except'
            string? exceptionType = null;
            string? varName = null;
            
            // Parse exception type if present
            if (!Check(TokenType.COLON))
            {
                if (Check(TokenType.IDENTIFIER))
                    exceptionType = Advance().Value;
                
                // Parse 'as varname' if present
                if (Check(TokenType.KEYWORD) && Peek().Value == "as")
                {
                    Advance(); // consume 'as'
                    varName = Consume(TokenType.IDENTIFIER, "Expected variable name").Value;
                }
            }
            
            Consume(TokenType.COLON, "Expected ':'");
            SkipNewlines();
            Consume(TokenType.INDENT, "Expected indented block");
            var exceptBody = ParseIndentedBlock();
            if (Check(TokenType.DEDENT)) Advance();
            
            exceptClauses.Add((exceptionType, varName, exceptBody));
        }

        IReadOnlyList<Stmt>? finallyBody = null;
        if (Check(TokenType.KEYWORD) && Peek().Value == "finally")
        {
            Advance(); // consume 'finally'
            Consume(TokenType.COLON, "Expected ':'");
            SkipNewlines();
            Consume(TokenType.INDENT, "Expected indented block");
            finallyBody = ParseIndentedBlock();
            if (Check(TokenType.DEDENT)) Advance();
        }

        return new TryStmt(tryBody, exceptClauses, finallyBody);
    }

    private ClassDefStmt ParseClassDef()
    {
        Consume(TokenType.KEYWORD, "Expected 'class'");
        var className = Consume(TokenType.IDENTIFIER, "Expected class name").Value;
        
        // Parse optional base classes
        string? baseClass = null;
        if (Match(TokenType.LPAREN))
        {
            if (!Check(TokenType.RPAREN))
            {
                // Get the first base class
                if (Check(TokenType.IDENTIFIER))
                {
                    baseClass = Advance().Value;
                }
                
                // Skip any additional base classes or arguments
                int parenDepth = 1;
                while (parenDepth > 0 && !IsAtEnd())
                {
                    if (Check(TokenType.LPAREN)) parenDepth++;
                    else if (Check(TokenType.RPAREN)) parenDepth--;
                    else if (parenDepth == 1 && Check(TokenType.COMMA)) { } // comma between base classes
                    Advance();
                }
            }
        }
        
        Consume(TokenType.COLON, "Expected ':'");
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var body = ParseIndentedBlock();
        if (Check(TokenType.DEDENT)) Advance();

        return new ClassDefStmt(className, body, baseClass);
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
                // Skip type annotation if present: name: type
                if (Match(TokenType.COLON))
                {
                    SkipTypeAnnotation();
                }
                // Skip default value if present: = value
                if (Match(TokenType.EQUALS))
                {
                    SkipDefaultValue();
                }
            } while (Match(TokenType.COMMA));
        }
        Consume(TokenType.RPAREN, "Expected ')'");
        
        // Skip return type annotation if present: -> type
        if (Check(TokenType.MINUS))
        {
            var nextIdx = _current + 1;
            if (nextIdx < _tokens.Count && _tokens[nextIdx].Type == TokenType.GT)
            {
                Advance(); // consume MINUS
                Advance(); // consume GT
                SkipTypeAnnotation();
            }
        }
        Consume(TokenType.COLON, "Expected ':'");
        
        // Check for single-line function definition (def foo(): statement)
        if (!Check(TokenType.NEWLINE))
        {
            // Parse single or multiple semicolon-separated statements on same line
            var body = new List<Stmt>();
            var stmt = ParseStatement();
            if (stmt != null) body.Add(stmt);
            
            // Handle semicolon-separated statements: def foo(): stmt1; stmt2; stmt3
            while (Match(TokenType.SEMICOLON))
            {
                if (Check(TokenType.NEWLINE) || IsAtEnd()) break;
                stmt = ParseStatement();
                if (stmt != null) body.Add(stmt);
            }
            
            return new FunctionDefStmt(funcName, parameters, body);
        }
        
        // Multi-line function definition
        SkipNewlines();
        Consume(TokenType.INDENT, "Expected indented block");

        var bodyList = ParseIndentedBlock();
        if (Check(TokenType.DEDENT)) Advance();

        return new FunctionDefStmt(funcName, parameters, bodyList);
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
            
            // Handle semicolon-separated statements on same line
            while (Match(TokenType.SEMICOLON))
            {
                if (Check(TokenType.NEWLINE) || Check(TokenType.DEDENT) || IsAtEnd()) break;
                stmt = ParseStatement();
                if (stmt != null) statements.Add(stmt);
            }
        }

        return statements;
    }
    
    private void SkipTypeAnnotation()
    {
        // Skip over type annotations like: int, str, List[str], Optional[Dict[str, int]], etc.
        int depth = 0;
        while (!IsAtEnd())
        {
            if (Check(TokenType.LBRACKET)) { depth++; Advance(); }
            else if (Check(TokenType.RBRACKET)) { depth--; Advance(); if (depth <= 0) break; }
            else if (depth == 0 && (Check(TokenType.COMMA) || Check(TokenType.RPAREN) || Check(TokenType.COLON) || Check(TokenType.EQUALS) || Check(TokenType.PLUSEQ) || Check(TokenType.MINUSEQ) || Check(TokenType.STAREQ) || Check(TokenType.SLASHEQ) || Check(TokenType.NEWLINE))) break;
            else Advance();
        }
    }

    private void SkipDefaultValue()
    {
        // Skip over default parameter values like: 5, "string", [], {}, etc.
        int depth = 0;
        while (!IsAtEnd())
        {
            if (Check(TokenType.LPAREN) || Check(TokenType.LBRACKET) || Check(TokenType.LBRACE)) { depth++; Advance(); }
            else if (Check(TokenType.RPAREN) && depth == 0) break;
            else if (Check(TokenType.RPAREN) || Check(TokenType.RBRACKET) || Check(TokenType.RBRACE)) { depth--; Advance(); if (depth <= 0) break; }
            else if (Check(TokenType.COMMA) && depth == 0) break;
            else if (Check(TokenType.NEWLINE) && depth == 0) break;
            else Advance();
        }
    }

    private Expr ParseExpression()
    {
        // Check for lambda expressions
        if (Check(TokenType.KEYWORD) && Peek().Value == "lambda")
        {
            return ParseLambda();
        }
        
        var expr = ParseTernary();
        
        // Check for tuple expression (comma-separated values)
        // e.g., "a, b, c" or "return x, y, z"
        if (Check(TokenType.COMMA))
        {
            var elements = new List<Expr> { expr };
            while (Match(TokenType.COMMA))
            {
                // Don't parse comma as part of tuple if we hit newline, RPAREN, etc.
                if (Check(TokenType.NEWLINE) || Check(TokenType.RPAREN) || 
                    Check(TokenType.RBRACKET) || Check(TokenType.RBRACE) || IsAtEnd())
                    break;
                elements.Add(ParseTernary());
            }
            // Return as a list literal (tuple in Python is essentially immutable list)
            return new ListLiteral(elements);
        }
        
        return expr;
    }

    private Expr ParseTernary()
    {
        var expr = ParseOrExpression();
        
        // Check for ternary conditional: expr if condition else expr
        if (Check(TokenType.KEYWORD) && Peek().Value == "if")
        {
            Advance(); // consume 'if'
            var condition = ParseOrExpression();
            
            if (!Check(TokenType.KEYWORD) || Peek().Value != "else")
                throw new Exception("Expected 'else' in ternary expression");
            Advance(); // consume 'else'
            
            var falseExpr = ParseTernary(); // Right-associative
            
            // For now, represent ternary as a function call to a special intrinsic
            // ternary(condition, true_value, false_value)
            return new Intrinsic("ternary", new List<Expr> { condition, expr, falseExpr });
        }
        
        return expr;
    }

    private Expr ParseLambda()
    {
        Consume(TokenType.KEYWORD, "Expected 'lambda'");
        
        var parameters = new List<string>();
        if (!Check(TokenType.COLON))
        {
            do
            {
                if (Check(TokenType.IDENTIFIER))
                    parameters.Add(Advance().Value);
            } while (Match(TokenType.COMMA));
        }
        
        Consume(TokenType.COLON, "Expected ':'");
        var body = ParseOrExpression();
        
        return new LambdaExpr(parameters, body);
    }

    private Expr ParseOrExpression()
    {
        var expr = ParseAndExpression();

        while (Match(TokenType.OR) || (Check(TokenType.KEYWORD) && Peek().Value == "or"))
        {
            // If we matched the keyword "or", consume it
            if (Check(TokenType.KEYWORD) && Peek().Value == "or")
            {
                Advance();
            }
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
            // If we matched the keyword "and", consume it
            if (Check(TokenType.KEYWORD) && Peek().Value == "and")
            {
                Advance();
            }
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
        var expr = ParseBitwiseOrExpression();

        while (Match(TokenType.STAR, TokenType.SLASH, TokenType.PERCENT, TokenType.SLASHSLASH, TokenType.STARSTAR))
        {
            var op = Previous().Value;
            var right = ParseBitwiseOrExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseBitwiseOrExpression()
    {
        var expr = ParseBitwiseXorExpression();

        while (Match(TokenType.PIPE))
        {
            var op = "|";
            var right = ParseBitwiseXorExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseBitwiseXorExpression()
    {
        var expr = ParseBitwiseAndExpression();

        while (Match(TokenType.CARET))
        {
            var op = "^";
            var right = ParseBitwiseAndExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseBitwiseAndExpression()
    {
        var expr = ParseShiftExpression();

        while (Match(TokenType.AMPERSAND))
        {
            var op = "&";
            var right = ParseShiftExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseShiftExpression()
    {
        var expr = ParseUnaryExpression();

        while (Match(TokenType.LTLT, TokenType.GTGT))
        {
            var op = Previous().Value;
            var right = ParseUnaryExpression();
            expr = new BinaryOp(expr, op, right);
        }

        return expr;
    }

    private Expr ParseUnaryExpression()
    {
        if (Match(TokenType.TILDE))
        {
            var expr = ParseUnaryExpression();
            return new UnaryOp("~", expr);
        }

        if (Match(TokenType.NOT))
        {
            var expr = ParseUnaryExpression();
            return new UnaryOp("not", expr);
        }
        
        // Handle 'not' as keyword
        if (Check(TokenType.KEYWORD) && Peek().Value == "not")
        {
            Advance(); // consume the 'not' keyword
            var expr = ParseUnaryExpression();
            return new UnaryOp("not", expr);
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
                // Handle both indexing [expr] and slicing [start:end:step]
                Expr? start = null, end = null, step = null;
                
                if (!Check(TokenType.COLON))
                {
                    start = ParseExpression();
                }
                
                if (Match(TokenType.COLON))
                {
                    // Slice notation: [start:end:step] or [start:] or [:end] or [:] etc.
                    if (!Check(TokenType.RBRACKET) && !Check(TokenType.COLON))
                    {
                        end = ParseExpression();
                    }
                    
                    if (Match(TokenType.COLON))
                    {
                        if (!Check(TokenType.RBRACKET))
                        {
                            step = ParseExpression();
                        }
                    }
                    
                    Consume(TokenType.RBRACKET, "Expected ']'");
                    // For slicing, treat as __slice__ method call
                    expr = new MethodCall(expr, "__slice__", new List<Expr> { 
                        start ?? new Literal(null), 
                        end ?? new Literal(null),
                        step ?? new Literal(null)
                    });
                }
                else
                {
                    // Simple indexing
                    Consume(TokenType.RBRACKET, "Expected ']'");
                    expr = new MethodCall(expr, "__getitem__", new List<Expr> { start! });
                }
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
            if (Check(TokenType.RBRACKET))
            {
                Consume(TokenType.RBRACKET, "Expected ']'");
                return new ListLiteral(new List<Expr>());
            }
            
            var firstExpr = ParseTernary();  // Use ParseTernary to avoid tuple parsing
            
            // Check for list comprehension
            if (Check(TokenType.KEYWORD) && Peek().Value == "for")
            {
                Advance(); // consume 'for'
                if (!Check(TokenType.IDENTIFIER))
                    throw new Exception("Expected variable name after 'for' in list comprehension");
                var loopVar = Advance().Value;
                
                if (!Check(TokenType.KEYWORD) || Peek().Value != "in")
                    throw new Exception("Expected 'in' after variable in list comprehension");
                Advance(); // consume 'in'
                
                var iterableExpr = ParseOrExpression();
                
                // Check for optional filter condition
                Expr? filterCondition = null;
                if (Check(TokenType.KEYWORD) && Peek().Value == "if")
                {
                    Advance(); // consume 'if'
                    filterCondition = ParseOrExpression();
                }
                
                Consume(TokenType.RBRACKET, "Expected ']'");
                return new ListComprehension(firstExpr, loopVar, iterableExpr, filterCondition);
            }
            
            // Regular list literal
            var elements = new List<Expr> { firstExpr };
            while (Match(TokenType.COMMA))
            {
                SkipNewlinesAndIndentation();
                if (Check(TokenType.RBRACKET))
                    break;
                elements.Add(ParseTernary());  // Use ParseTernary to avoid tuple parsing
            }
            SkipNewlinesAndIndentation();
            Consume(TokenType.RBRACKET, "Expected ']'");
            return new ListLiteral(elements);
        }

        if (Match(TokenType.LBRACE))
        {
            SkipNewlinesAndIndentation();
            
            // Empty dict
            if (Check(TokenType.RBRACE))
            {
                Consume(TokenType.RBRACE, "Expected '}'");
                return new DictLiteral(new List<(Expr, Expr)>());
            }
            
            // Parse first key
            var firstKey = ParseTernary();
            SkipNewlinesAndIndentation();
            Consume(TokenType.COLON, "Expected ':' in dictionary");
            SkipNewlinesAndIndentation();
            var firstValue = ParseTernary();
            SkipNewlinesAndIndentation();
            
            // Check for dictionary comprehension: {k:v for k,v in ...}
            if (Check(TokenType.KEYWORD) && Peek().Value == "for")
            {
                Advance(); // consume 'for'
                
                // Parse loop variable(s) - could be single or tuple unpacking like "k,v"
                var loopVars = new List<string>();
                if (!Check(TokenType.IDENTIFIER))
                    throw new Exception("Expected variable name after 'for' in dict comprehension");
                loopVars.Add(Advance().Value);
                
                // Check for tuple unpacking: for k,v in ...
                while (Match(TokenType.COMMA))
                {
                    if (!Check(TokenType.IDENTIFIER))
                        throw new Exception("Expected variable name after ',' in dict comprehension");
                    loopVars.Add(Advance().Value);
                }
                
                if (!Check(TokenType.KEYWORD) || Peek().Value != "in")
                    throw new Exception("Expected 'in' after variable in dict comprehension");
                Advance(); // consume 'in'
                
                var iterableExpr = ParseOrExpression();
                
                // Check for optional filter condition
                Expr? filterCondition = null;
                if (Check(TokenType.KEYWORD) && Peek().Value == "if")
                {
                    Advance(); // consume 'if'
                    filterCondition = ParseOrExpression();
                }
                
                SkipNewlinesAndIndentation();
                Consume(TokenType.RBRACE, "Expected '}'");
                
                // Use comma-joined loop vars as the loop variable name
                var loopVar = string.Join(",", loopVars);
                return new DictComprehension(firstKey, firstValue, loopVar, iterableExpr, filterCondition);
            }
            
            // Regular dictionary literal
            var items = new List<(Expr, Expr)> { (firstKey, firstValue) };
            while (Match(TokenType.COMMA))
            {
                SkipNewlinesAndIndentation();
                if (Check(TokenType.RBRACE))
                    break;
                var key = ParseTernary();
                SkipNewlinesAndIndentation();
                Consume(TokenType.COLON, "Expected ':' in dictionary literal");
                SkipNewlinesAndIndentation();
                var value = ParseTernary();
                SkipNewlinesAndIndentation();
                items.Add((key, value));
            }
            SkipNewlinesAndIndentation();
            Consume(TokenType.RBRACE, "Expected '}'");
            return new DictLiteral(items);
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
                args.Add(ParseTernary());  // Use ParseTernary to avoid tuple parsing
            } while (Match(TokenType.COMMA));
        }
        return args;
    }

    private void SkipNewlines()
    {
        while (Match(TokenType.NEWLINE)) { }
    }

    private void SkipNewlinesAndIndentation()
    {
        // Skip newlines and indentation tokens (used inside brackets/braces/parens)
        while (Match(TokenType.NEWLINE, TokenType.INDENT, TokenType.DEDENT)) { }
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
