class Header:
    pass

header = Header()
args = Header()
args.block_exp = 1
args.threads = 2
args.max_ram_mib = 3

header.block_exp=args.block_exp; header.threads_hint=args.threads; header.ram_mib_hint=args.max_ram_mib
