#!/bin/bash
cd /workspaces/PLT

echo "Building project..."
dotnet build src/PLT/PLT.CLI/PLT.CLI.csproj -q
if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo "Build successful!"
echo ""

echo "Testing ternary operator..."
cat > /tmp/test_ternary.py << 'EOF'
x = 10
y = 5 if x > 0 else 0
print(y)

# Nested ternary
z = "big" if x > 20 else "small" if x > 5 else "tiny"
print(z)
EOF

echo "Input:"
cat /tmp/test_ternary.py
echo ""
echo "Translating to Tcl..."
cd /workspaces/PLT/src/PLT
dotnet run --project PLT.CLI -- --from py --to tcl /tmp/test_ternary.py 2>&1

echo ""
echo "Translating vfa.py (should work now)..."
dotnet run --project PLT.CLI -- --from py --to tcl ../examples/vfa.py 2>&1 | head -50
