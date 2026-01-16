class HeaderV1:
    def __init__(self):
        self.default_method = None
        self.default_level = None
        self.block_exp = None
        self.threads_hint = None
        self.ram_mib_hint = None

def test():
    method = 5
    args_level = 10
    args_block_exp = 15
    args_threads = 20
    args_max_ram_mib = 25
    
    header=HeaderV1(); header.default_method=method; header.default_level=args_level
    header.block_exp=args_block_exp; header.threads_hint=args_threads; header.ram_mib_hint=args_max_ram_mib
    print(header.block_exp, header.threads_hint, header.ram_mib_hint)

test()
