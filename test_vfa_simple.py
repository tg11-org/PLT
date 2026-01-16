# Simple version of vfa.py features without with/yield
# This tests everything we've implemented

from dataclasses import dataclass

# Test decorator
@dataclass
class FileEntry:
    name: str = ""
    size: int = 0
    flags: int = 0

# Test bit operations
def test_bitwise():
    F_ENCRYPTED = 1 << 0
    F_COMPRESSED = 1 << 1
    F_SIGNED = 1 << 2
    
    flags = 0
    flags |= F_ENCRYPTED
    flags |= F_COMPRESSED
    
    encrypted = (flags & F_ENCRYPTED) != 0
    compressed = (flags & F_COMPRESSED) != 0
    
    shifted = (flags >> 1) & 1
    mask = 0xFF00 & flags
    
    power = 2 ** 10  # 1024
    divided = power // 10  # 102
    
    # Ternary operator
    result = "yes" if encrypted else "no"
    return result

# Test augmented assignments in class context  
class Archive:
    def __init__(self):
        self.done_bytes: int = 0
        self.done_files: int = 0
        self.total_bytes: int = 1000
    
    def update_progress(self, bytes_count: int):
        self.done_bytes += bytes_count
        self.done_files += 1
    
    def get_rate(self) -> float:
        elapsed = 10.0
        rate = self.done_bytes / elapsed if elapsed > 0 else 0.0
        return rate

# Test in main code
archive = Archive()
archive.update_progress(100)
archive.update_progress(200)

rate = archive.get_rate()
print(rate)

test_result = test_bitwise()
print(test_result)

entry = FileEntry(name="test.txt", size=512)
print(entry.name)
