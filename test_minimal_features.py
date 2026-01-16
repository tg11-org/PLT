#!/usr/bin/env python3
# Test minimal features needed

# Test 1: Single-line if with raise
def test1():
    HAVE_FEATURE = False
    if not HAVE_FEATURE: raise RuntimeError("not installed")

# Test 2: Tuple unpacking
def test2():
    import struct
    bio_data = struct.pack("<I", 42)
    (n,) = struct.unpack("<I", bio_data)
    return n

# Test 3: List comprehension with filter
def test3():
    items = [1, 2, 3, 4, 5]
    result = [x for x in items if x > 2]
    return result

# Test 4: Generator expression
def test4():
    items = [1, 2, 3, 4, 5]
    result = sum(1 for x in items if x > 2)
    return result

print(test3())
print(test4())
