#!/bin/bash
cd /workspaces/PLT

# Compile the project
echo "Building PLT.CLI..."
dotnet build src/PLT/PLT.CLI/PLT.CLI.csproj -q 2>&1 | head -20

if [ $? -eq 0 ]; then
    echo "Build successful!"
    echo ""
    echo "Testing simple translation (test.py -> Tcl)..."
    ./src/PLT/PLT.CLI/bin/Debug/net8.0/PLT.CLI --from py --to tcl src/examples/test.py 2>&1 | head -50
    
    echo ""
    echo "Testing vfa.py translation to Tcl..."
    ./src/PLT/PLT.CLI/bin/Debug/net8.0/PLT.CLI --from py --to tcl src/examples/vfa.py 2>&1 | head -50
else
    echo "Build failed!"
fi
