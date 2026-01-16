using PLT.CORE.Frontends.Python;
using PLT.CORE.Backends.Python;
using PLT.CORE.Backends.C;
using PLT.CORE.Backends.Tcl;

namespace PLT.TESTS;

public class UnitTest1
{
    [Fact]
    public void TestDictLiterals()
    {
        // Test parsing and emitting dictionary literals
        var pythonCode = @"
# Dictionary test
empty_dict = {}
person = {""name"": ""Alice"", ""age"": ""30""}
numbers = {1: ""one"", 2: ""two""}
nested = {""outer"": {""inner"": ""value""}}
";

        var ast = PythonFrontend.Parse(pythonCode);
        
        // Test Python backend
        var pythonOutput = new PythonEmitter().Emit(ast);
        Assert.NotNull(pythonOutput);
        Assert.Contains("empty_dict", pythonOutput);
        Assert.Contains("{}", pythonOutput);
        Assert.Contains("{", pythonOutput);
        Assert.Contains("}", pythonOutput);
        
        // Test C backend
        var cOutput = new CEmitter().Emit(ast);
        Assert.NotNull(cOutput);
        Assert.Contains("dict:", cOutput);
        
        // Test Tcl backend
        var tclOutput = new TclEmitter().Emit(ast);
        Assert.NotNull(tclOutput);
        Assert.Contains("[dict create", tclOutput);
    }

    [Fact]
    public void TestSimpleDict()
    {
        var pythonCode = @"config = {""debug"": ""true"", ""port"": ""8080""}";
        
        var ast = PythonFrontend.Parse(pythonCode);
        
        var output = new PythonEmitter().Emit(ast);
        
        Assert.Contains("config", output);
        Assert.Contains("debug", output);
        Assert.Contains("true", output);
    }

    [Fact]
    public void TestNestedDict()
    {
        var pythonCode = @"data = {""user"": {""name"": ""Bob"", ""id"": 123}}";
        
        var ast = PythonFrontend.Parse(pythonCode);
        
        var output = new PythonEmitter().Emit(ast);
        
        Assert.Contains("data", output);
        Assert.Contains("user", output);
        Assert.Contains("name", output);
    }
}