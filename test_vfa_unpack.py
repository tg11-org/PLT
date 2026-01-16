@classmethod
def unpack(cls, bio: io.BufferedReader)->"HeaderV1":
    if bio.read(4)!=MAGIC: raise ValueError("Not a VFA archive")
