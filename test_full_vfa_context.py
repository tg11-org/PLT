class HeaderV1:
    def __init__(self):
        self.default_method = None
        self.default_level = None
        self.block_exp = None
        self.threads_hint = None
        self.ram_mib_hint = None

class Args:
    def __init__(self):
        self.block_exp = 4
        self.method = "test"
        self.level = 5
        self.threads = 2
        self.max_ram_mib = 1024

def cmd_create(args):
    block_size=1<<args.block_exp; method="method_value"
    if method is None: raise SystemExit("Unknown method")
    header=HeaderV1(); header.default_method=method; header.default_level=args.level
    header.block_exp=args.block_exp; header.threads_hint=args.threads; header.ram_mib_hint=args.max_ram_mib
    print(header.block_exp, header.threads_hint, header.ram_mib_hint)

args = Args()
cmd_create(args)
