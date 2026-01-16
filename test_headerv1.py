@dataclass
class HeaderV1:
    version: int = 1
    def pack(self)->bytes:
        return b"".join([
            MAGIC,
            b"test"
        ])
    @classmethod
    def unpack(cls, bio: io.BufferedReader)->"HeaderV1":
        if bio.read(4)!=MAGIC: raise ValueError("Not a VFA archive")
