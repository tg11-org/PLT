from typing import Dict, Tuple

class TOC:
    pass

def test():
    toc=TOC(); block_index=0
    hardlinks: Dict[Tuple[int,int], str] = {}
    print(toc, block_index, hardlinks)

test()
