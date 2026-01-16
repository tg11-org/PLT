#!/bin/bash
set -e

cd /workspaces/PLT

# Build first
echo "Building..."
dotnet build src/PLT/PLT.CLI/PLT.CLI.csproj -q 2>&1 > /tmp/build.log
BUILD_STATUS=$?

if [ $BUILD_STATUS -ne 0 ]; then
    echo "Build failed!"
    cat /tmp/build.log
    exit 1
fi

echo "Build successful!"
echo ""

# Now run the translation
echo "Running translation on vfa.py..."
cd /workspaces/PLT/src/PLT

# Create output directory if needed
mkdir -p /tmp/plt_output

# Run the translation and capture output
dotnet run --project PLT.CLI -- --from py --to tcl ../../src/examples/vfa.py 2>&1 | tee /tmp/plt_translation.log | head -200

echo ""
echo "Translation completed. Check /tmp/plt_translation.log for full output"
