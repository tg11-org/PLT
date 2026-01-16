#!/bin/bash
cd /workspaces/PLT

echo "Building..."
dotnet build src/PLT/PLT.CLI/PLT.CLI.csproj -q
BUILD_RESULT=$?

if [ $BUILD_RESULT -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo "Build successful!"
echo ""
echo "Running vfa.py translation..."
cd src/PLT
dotnet run --project PLT.CLI -- --from py --to tcl ../examples/vfa.py --print-ir 2>&1 | head -200
