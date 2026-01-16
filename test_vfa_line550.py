class TOC:
    pass

class Args:
    password = True
    kdf_time = 3
    kdf_mem_kib = 256*1024
    kdf_parallel = 4

def test(args):
    HAVE_ARGON2 = True
    KDF_ARGON2ID = 1
    F_ENCRYPTED = 1
    
    class header:
        kdf_id = 0
        kdf_t = 0
        kdf_m = 0
        kdf_p = 0
        flags = 0
    
    if args.password:
        if HAVE_ARGON2:
            header.kdf_id=KDF_ARGON2ID; header.kdf_t=args.kdf_time or 3; header.kdf_m=args.kdf_mem_kib or (256*1024); header.kdf_p=args.kdf_parallel or 4
        else:
            pass
        header.flags |= F_ENCRYPTED
    
    toc=TOC(); block_index=0
    print(toc, block_index)

args = Args()
test(args)
