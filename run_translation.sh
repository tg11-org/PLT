#!/bin/bash
cd /workspaces/PLT/src/PLT
dotnet run --project PLT.CLI -- --from py --to tcl ../examples/vfa.py 2>&1 | head -100
