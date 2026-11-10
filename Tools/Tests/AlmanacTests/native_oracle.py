"""Execute only the native almanac hit-test instructions with explicit availability stubs.

Does not launch the original game, render UI, read saves, or verify unlock rules.
Uses the existing Unicorn installation documented by the other native tests.
"""
import hashlib
import json
import random
import struct
from pathlib import Path

from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_EBX, UC_X86_REG_ECX, UC_X86_REG_EDI, UC_X86_REG_ESI, UC_X86_REG_ESP, UC_X86_REG_EIP

ROOT = Path(__file__).resolve().parents[3]
EXE_SHA256 = 'e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c'
ALMANAC, APP, PROFILE, STACK, STOP = 0x10000000, 0x11000000, 0x12000000, 0x14000000, 0x15000000


class Oracle:
    def __init__(self):
        raw = (ROOT / 'PlantsVsZombies.exe').read_bytes()
        if hashlib.sha256(raw).hexdigest() != EXE_SHA256:
            raise RuntimeError('Local EXE does not match the analyzed sample.')
        pe = struct.unpack_from('<I', raw, 0x3C)[0]
        if raw[pe:pe + 4] != b'PE\0\0':
            raise RuntimeError('Invalid PE.')
        optional = pe + 24
        if struct.unpack_from('<HH', raw, pe + 4)[0] != 0x14C or struct.unpack_from('<H', raw, optional)[0] != 0x10B:
            raise RuntimeError('Expected x86 PE32.')
        base = struct.unpack_from('<I', raw, optional + 28)[0]
        size, headers = struct.unpack_from('<II', raw, optional + 56)
        image = bytearray(size)
        image[:headers] = raw[:headers]
        sections = optional + struct.unpack_from('<H', raw, pe + 20)[0]
        for i in range(struct.unpack_from('<H', raw, pe + 6)[0]):
            virtual, raw_size, raw_address = struct.unpack_from('<III', raw, sections + i * 40 + 12)
            image[virtual:virtual + raw_size] = raw[raw_address:raw_address + raw_size]
        self.uc = Uc(UC_ARCH_X86, UC_MODE_32)
        self.uc.mem_map(base, (size + 0xFFF) & ~0xFFF)
        self.uc.mem_write(base, bytes(image))
        for address in (ALMANAC, APP, PROFILE, STACK, STOP):
            self.uc.mem_map(address, 0x10000)
        self.write32(ALMANAC + 392, APP)
        self.write32(APP + 2384, PROFILE)
        self.uc.mem_write(ALMANAC + 101, b'\x01')
        self.limited = False
        self.uc.hook_add(UC_HOOK_CODE, self.instruction)

    def write32(self, address, value):
        self.uc.mem_write(address, struct.pack('<I', value & 0xFFFFFFFF))

    def instruction(self, uc, address, size, data):
        if address in (0x460F00, 0x40CB70):
            allowed = not self.limited or uc.reg_read(UC_X86_REG_EDI) in (0, 2)
            sp = uc.reg_read(UC_X86_REG_ESP)
            ret = struct.unpack('<I', uc.mem_read(sp, 4))[0]
            uc.reg_write(UC_X86_REG_EAX, int(allowed))
            uc.reg_write(UC_X86_REG_ESP, sp + 4)
            uc.reg_write(UC_X86_REG_EIP, ret)

    def hit(self, category, x, y, limited):
        self.limited = limited
        self.write32(ALMANAC + 416, category)
        self.write32(PROFILE + 488, int(not limited))  # Separate native Gatling Pea ownership check.
        sp = STACK + 0x8000
        self.write32(sp, STOP)
        self.uc.reg_write(UC_X86_REG_ESP, sp)
        if category != 2:
            self.uc.reg_write(UC_X86_REG_EBX, ALMANAC)
            self.write32(sp + 4, x)
            self.write32(sp + 8, y)
            entry = 0x40CAA0
        else:
            self.uc.reg_write(UC_X86_REG_EBX, x)
            self.uc.reg_write(UC_X86_REG_ESI, y)
            self.write32(sp + 4, ALMANAC)
            entry = 0x40CCF0
        self.uc.emu_start(entry, STOP, count=10000)
        if self.uc.reg_read(UC_X86_REG_EIP) != STOP:
            raise RuntimeError('Hit test did not return.')
        value = self.uc.reg_read(UC_X86_REG_EAX)
        return value if value < 0x80000000 else value - 0x100000000


def points(category):
    result = {(-1, -1), (0, 0), (799, 599), (800, 600), (600, 350)}
    rectangles = []
    if category == 1:
        rectangles = [(26 + (i % 8) * 52, 92 + (i // 8) * 78, 50, 70) for i in range(48)] + [(20, 23, 36, 46)]
    elif category == 2:
        rectangles = [(22 + (i % 5) * 85, 86 + (i // 5) * 80, 76, 76) for i in range(25)] + [(192, 486, 76, 76)]
    for x, y, width, height in rectangles:
        for dx in (-1, 0, width // 2, width - 1, width):
            for dy in (-1, 0, height // 2, height - 1, height):
                result.add((x + dx, y + dy))
    generator = random.Random(409260 + category)
    result.update((generator.randrange(800), generator.randrange(600)) for _ in range(200))
    return sorted(result)


def main():
    oracle = Oracle()
    groups = []
    for category, limited in ((0, False), (1, False), (1, True), (2, False), (2, True)):
        groups.append({'category': category, 'limited': limited,
            'vectors': [[x, y, oracle.hit(category, x, y, limited)] for x, y in points(category)]})
    print(json.dumps({'exe_sha256': EXE_SHA256, 'availability_calls_stubbed': ["0x460F00", "0x40CB70"], 'groups': groups}))


if __name__ == '__main__':
    main()
