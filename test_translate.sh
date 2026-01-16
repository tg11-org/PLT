#!/bin/bash
cd /workspaces/PLT
echo "Testing vfa.py translation to Tcl..."
./src/PLT/PLT.CLI/bin/Debug/net8.0/PLT.CLI --from py --to tcl src/examples/vfa.py -o /tmp/vfa_out.tcl 2>&1
echo "Exit code: $?"
echo ""
echo "Checking if output was created..."
if [ -f /tmp/vfa_out.tcl ]; then
    echo "Output file created!"
    echo "First 50 lines:"
    head -50 /tmp/vfa_out.tcl
else
    echo "No output file created"
fi
