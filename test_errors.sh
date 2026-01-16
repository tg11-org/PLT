#!/bin/bash
cd /workspaces/PLT/src/PLT

echo "Building..."
dotnet build -q 2>&1 | tail -20

echo ""
echo "Testing simple assignment..."
cat > /tmp/simple_test.py << 'EOF'
x = 10
y = 20
print(x)
EOF

echo "Running translation on simple file..."
dotnet run --project PLT.CLI -- --from py --to tcl /tmp/simple_test.py 2>&1

echo ""
echo "Testing augmented assignment..."
cat > /tmp/aug_test.py << 'EOF'
x = 10
x += 5
print(x)
EOF

echo "Running translation..."
dotnet run --project PLT.CLI -- --from py --to tcl /tmp/aug_test.py 2>&1

echo ""
echo "Testing decorator..."
cat > /tmp/deco_test.py << 'EOF'
@dataclass
class Point:
    x: int

print(1)
EOF

echo "Running translation..."
dotnet run --project PLT.CLI -- --from py --to tcl /tmp/deco_test.py 2>&1
