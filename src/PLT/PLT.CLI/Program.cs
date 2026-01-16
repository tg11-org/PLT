using PLT.CORE.IR;
using PLT.CORE.Backends.Python;
using PLT.CORE.Backends.C;
using PLT.CORE.Backends.Tcl;
using PLT.CORE.Frontends.Js;
using PLT.CORE.Frontends.Python;


static void Usage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  plt --from <js|py> --to <python|c|tcl> <input> [-o out]");
    Console.WriteLine("  --print-ir      Print the IR before emitting output");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run --project .\\PLT.CLI\\ -- --from js --to python examples\\hello.js -o out.py");
    Console.WriteLine("  dotnet run --project .\\PLT.CLI\\ -- --from py --to tcl script.py -o out.tcl");
    Console.WriteLine("  dotnet run --project .\\PLT.CLI\\ -- --from py --to python script.py --print-ir");
}

string? from = null;
string? to = null;
string? inputPath = null;
string? outputPath = null;
bool printIr = false;


for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--print-ir":
            printIr = true;
            break;
        case "--from":
            from = i + 1 < args.Length ? args[++i] : null;
            break;
        case "--to":
            to = i + 1 < args.Length ? args[++i] : null;
            break;
        case "-o":
        case "--out":
            outputPath = i + 1 < args.Length ? args[++i] : null;
            break;
        default:
            if (!args[i].StartsWith("-") && inputPath is null)
                inputPath = args[i];
            else
            {
                Console.WriteLine($"Unknown arg: {args[i]}");
                Usage();
                return;
            }
            break;
    }
}

if (from is null || to is null || inputPath is null)
{
    Usage();
    return;
}

if (!File.Exists(inputPath))
{
    Console.WriteLine($"Input file not found: {inputPath}");
    return;
}

// Parse based on frontend
if (from != "js" && from != "py")
{
    Console.WriteLine($"Unsupported --from {from} (supported: 'js', 'py')");
    return;
}

var source = File.ReadAllText(inputPath);
var ir = from switch
{
    "js" => MiniJsFrontend.ParseConsoleLogHelloWorld(source),
    "py" => PythonFrontend.Parse(source),
    _ => throw new Exception($"Unknown frontend: {from}")
};

if (printIr)
{
    Console.WriteLine("=== IR ===");
    Console.WriteLine(PrettyPrinter.Print(ir));
}


// Emit
string output = to switch
{
    "python" or "py" => new PythonEmitter().Emit(ir),
    "c" => new CEmitter().Emit(ir),
    "tcl" => new TclEmitter().Emit(ir),
    _ => throw new Exception($"Unsupported --to {to}")
};

if (!string.IsNullOrWhiteSpace(outputPath))
{
    File.WriteAllText(outputPath, output);
    Console.WriteLine($"Wrote {to} to: {outputPath}");
}
else
{
    Console.WriteLine(output);
}
