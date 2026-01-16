def unpack(cls, bio):
    if bio.read(4)!=MAGIC: raise ValueError("Not a VFA archive")
