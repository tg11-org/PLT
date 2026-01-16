#!/bin/bash
cd /workspaces/PLT

echo "Building project..."
dotnet build src/PLT/PLT.CLI/PLT.CLI.csproj -q 2>&1 | grep -E "error|Build"
if [ ${PIPESTATUS[0]} -ne 0 ]; then
    echo "Build had errors!"
else
    echo "Build successful!"
fi

echo ""
echo "Testing bitwise and ternary operators..."
cat > /tmp/test_operators.py << 'EOF'
# Bitwise operators
x = 5 << 2
y = 16 >> 1
z = 12 & 10
w = 12 | 10
v = 12 ^ 10
u = ~5

# Ternary operator
result = "positive" if x > 0 else "non-positive"

# Combined
combined = (x >> 1) & (y << 1) if x > 0 else 0

print(result)
EOF

echo "Input:"
cat /tmp/test_operators.py
echo ""
echo "Testing translation to Tcl..."
cd /workspaces/PLT/src/PLT
dotnet run --project PLT.CLI -- --from py --to tcl /tmp/test_operators.py 2>&1

echo ""
echo "Now testing vfa.py translation (takes a moment)..."
timeout 30 dotnet run --project PLT.CLI -- --from py --to tcl ../examples/vfa.py 2>&1 | head -50 || echo "Translation timed out or failed"
