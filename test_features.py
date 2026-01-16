#!/usr/bin/env python3
# Test file to verify the new features work

# Test decorators
from dataclasses import dataclass

@dataclass
class Point:
    x: int = 0
    y: int = 0

# Test augmented assignments
x = 10
x += 5
x -= 2
x *= 3
x /= 2

# Test ternary operator
y = x if x > 10 else 0
z = 5 if True else 10

# Test bitwise operators
a = 1 << 2     # left shift
b = 8 >> 1     # right shift
c = 5 & 3      # bitwise AND
d = 5 | 3      # bitwise OR
e = 5 ^ 3      # bitwise XOR
f = ~5         # bitwise NOT
g = 2 ** 3     # power
h = 10 // 3    # floor division

# Test combinations
result = (a << 2) & (b >> 1)
flag = 1 if result > 10 else 0

print(x)
print(y)
print(result)
