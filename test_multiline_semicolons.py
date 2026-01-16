class Header:
    def __init__(self):
        self.default_method = None
        self.default_level = None
        self.block_exp = None
        self.threads_hint = None
        self.ram_mib_hint = None

def test():
    header = Header()
    header.block_exp=1; header.threads_hint=2; header.ram_mib_hint=3
    print(header.block_exp, header.threads_hint, header.ram_mib_hint)

test()
