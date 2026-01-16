m = hashlib.sha256()
m.update(prefix12)
m.update(struct.pack("<Q", index))
m.update(b"vfa-nonce")
