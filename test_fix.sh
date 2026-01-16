#!/bin/bash
set -e

cd /workspaces/PLT

echo "=========================================="
echo "Building PLT.CLI project..."
echo "=========================================="
dotnet build src/PLT/PLT.CLI/PLT.CLI.csproj -q

BUILD_STATUS=$?
if [ $BUILD_STATUS -eq 0 ]; then
    echo "✓ Build successful!"
else
    echo "✗ Build failed with status $BUILD_STATUS"
    exit $BUILD_STATUS
fi

echo ""
echo "=========================================="
echo "Testing simple Python to Tcl translation"
echo "=========================================="
cat > /tmp/test_decorator.py << 'EOF'
@dataclass
class Point:
    x: int
    y: int

def add_points(p1, p2):
    return 10 + 20

add_points(1, 2)
EOF

echo "Input Python code:"
cat /tmp/test_decorator.py
echo ""
echo "Translating to Tcl..."
./src/PLT/PLT.CLI/bin/Debug/net8.0/PLT.CLI --from py --to tcl /tmp/test_decorator.py
echo ""
echo "✓ Translation successful!"

echo ""
echo "=========================================="
echo "Testing vfa.py translation to Tcl"
echo "=========================================="
./src/PLT/PLT.CLI/bin/Debug/net8.0/PLT.CLI --from py --to tcl src/examples/vfa.py 2>&1 | head -100
RESULT=$?
if [ $RESULT -eq 0 ]; then
    echo ""
    echo "✓ vfa.py translation completed!"
else
    echo ""
    echo "✗ vfa.py translation failed with status $RESULT"
fi
