import platform
import sys

# Test platform.system()
win = platform.system() == "Windows"
lin = sys.platform.startswith("linux")

# Test string methods
text = "  hello world  "
upper = text.upper()
lower = text.lower()
stripped = text.strip()
parts = text.split()
joined = "-".join(parts)

# Test endswith
if text.endswith("d"):
    print("ends with d")
