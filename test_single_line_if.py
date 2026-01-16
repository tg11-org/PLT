def test():
    args_solid = True
    F_SOLID = 1
    
    class header:
        flags = 0
    
    if args_solid: header.flags |= F_SOLID
    key = None
    print(key)

test()
