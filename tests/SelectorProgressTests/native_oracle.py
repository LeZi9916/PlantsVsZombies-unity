"""Differentially check the C# rules against isolated instructions in the local EXE.

Requires the Python unicorn package and the Release/net9.0 test build. This does
not launch the game, call Windows APIs, read saves, or execute the full UI.
"""

import hashlib
import itertools
import random
import struct
import subprocess
from pathlib import Path

from unicorn import UC_ARCH_X86, UC_HOOK_CODE, UC_MODE_32, Uc
from unicorn.x86_const import (
    UC_X86_REG_EAX, UC_X86_REG_EBX, UC_X86_REG_ECX, UC_X86_REG_EDI,
    UC_X86_REG_EDX, UC_X86_REG_EIP, UC_X86_REG_ESI, UC_X86_REG_ESP,
)


ROOT = Path(__file__).resolve().parents[3]
EXE = ROOT / "PlantsVsZombies.exe"
TEST_DLL = Path(__file__).resolve().parent / "bin/Release/net9.0/SelectorProgressTests.dll"
EXE_SHA256 = "e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c"
SELECTOR = 0x10000000
APP = 0x11000000
PROFILE = 0x12000000
WIDGETS = 0x13000000
STACK = 0x14000000
SERVICE = 0x15000000


def load_image():
    data = EXE.read_bytes()
    if hashlib.sha256(data).hexdigest() != EXE_SHA256:
        raise RuntimeError("EXE SHA-256 does not match the analyzed binary")
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise RuntimeError("Not a PE file")
    machine, count = struct.unpack_from("<HH", data, pe + 4)
    optional_size = struct.unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    if machine != 0x14C or struct.unpack_from("<H", data, optional)[0] != 0x10B:
        raise RuntimeError("Expected a 32-bit x86 PE")
    base = struct.unpack_from("<I", data, optional + 28)[0]
    size = struct.unpack_from("<I", data, optional + 56)[0]
    headers = struct.unpack_from("<I", data, optional + 60)[0]
    if base != 0x400000:
        raise RuntimeError("Unexpected PE image base")
    image = bytearray(size)
    image[:headers] = data[:headers]
    sections = optional + optional_size
    for index in range(count):
        section = sections + index * 40
        virtual_address, raw_size, raw_address = struct.unpack_from("<III", data, section + 12)
        if virtual_address + raw_size > size or raw_address + raw_size > len(data):
            raise RuntimeError("Invalid PE section bounds")
        image[virtual_address:virtual_address + raw_size] = data[raw_address:raw_address + raw_size]
    return base, image


class NativeOracle:
    def __init__(self):
        self.uc = Uc(UC_ARCH_X86, UC_MODE_32)
        base, image = load_image()
        self.uc.mem_map(base, (len(image) + 0xFFF) & ~0xFFF)
        self.uc.mem_write(base, bytes(image))
        for address in (SELECTOR, APP, PROFILE, WIDGETS, STACK, SERVICE):
            self.uc.mem_map(address, 0x10000)
        self.query_results = {}
        self.event = None
        self.uc.hook_add(UC_HOOK_CODE, self.on_instruction)

    def on_instruction(self, uc, address, _size, _user_data):
        if address == 0x651F30:
            if uc.reg_read(UC_X86_REG_ECX) != SERVICE:
                raise AssertionError("Unexpected platform service receiver")
            stack = uc.reg_read(UC_X86_REG_ESP)
            return_address = self.read32(stack)
            if return_address not in self.query_results:
                raise AssertionError("Unexpected platform query call site: %08X" % return_address)
            uc.reg_write(UC_X86_REG_ESP, stack + 4)
            uc.reg_write(UC_X86_REG_EAX, self.query_results[return_address])
            uc.reg_write(UC_X86_REG_EIP, return_address)
        elif address in (0x457988, 0x45D250, 0x457B72):
            self.event = {0x457988: 2, 0x45D250: 1, 0x457B72: 0}[address]
            uc.emu_stop()

    def write32(self, address, value):
        self.uc.mem_write(address, struct.pack("<I", value & 0xFFFFFFFF))

    def read32(self, address):
        return struct.unpack("<I", self.uc.mem_read(address, 4))[0]

    def read_signed32(self, address):
        return struct.unpack("<i", self.uc.mem_read(address, 4))[0]

    def read8(self, address):
        return self.uc.mem_read(address, 1)[0]

    def reset_registers(self):
        for register in (UC_X86_REG_EAX, UC_X86_REG_EBX, UC_X86_REG_ECX,
                         UC_X86_REG_EDX, UC_X86_REG_EDI):
            self.uc.reg_write(register, 0)
        self.uc.reg_write(UC_X86_REG_ESI, SELECTOR)
        self.uc.reg_write(UC_X86_REG_ESP, STACK + 0x8000)

    def run_slice(self, start, end):
        self.reset_registers()
        self.uc.emu_start(start, end, count=1000)
        if self.uc.reg_read(UC_X86_REG_EIP) != end:
            raise AssertionError("Slice %08X did not reach %08X" % (start, end))

    def click(self, mode_id):
        self.reset_registers()
        self.uc.reg_write(UC_X86_REG_ECX, SELECTOR + 0xA0)
        self.write32(STACK + 0x8000, 0)
        self.write32(STACK + 0x8004, mode_id)
        self.event = None
        self.uc.emu_start(0x4578E0, 0, count=100)
        if self.event is None:
            raise AssertionError("Mode callback did not reach a known branch")
        return self.event

    def evaluate(self, row):
        (has_profile, level, completed, mini, puzzle, survival, store, style,
         app_flag, app_value, has_service, mode_query, completion_query,
         garden_query, override, blocker) = row
        for address in (SELECTOR, APP, PROFILE, WIDGETS):
            self.uc.mem_write(address, bytes(0x2000))
        self.write32(SELECTOR + 0xA4, APP)
        self.write32(APP + 0x950, PROFILE if has_profile else 0)
        self.write32(APP + 0x99C, SERVICE if has_service else 0)
        self.write32(APP + 0x9E4, app_value)
        self.uc.mem_write(APP + 0x9E8, bytes([app_flag]))
        for offset, value in ((0x4C, level), (0x54, completed), (0x34C, mini),
                              (0x350, puzzle), (0x364, survival), (0x374, store)):
            self.write32(PROFILE + offset, value)
        self.uc.mem_write(PROFILE + 0x340, bytes([style]))
        self.uc.mem_write(SELECTOR + 0x159, bytes([override]))
        self.write32(SELECTOR + 0x15C, blocker)
        self.query_results = {0x45569D: mode_query, 0x4556F8: completion_query,
                              0x4614D8: garden_query}

        self.run_slice(0x4555C2, 0x455713)
        # The four widget pointers are consulted by the early part of 0x454F30.
        for index, offset in enumerate((0xD4, 0xD0, 0xD8, 0xC4)):
            self.write32(SELECTOR + offset, WIDGETS + index * 0x100)
        self.run_slice(0x454F30, 0x454FED)
        # Skip the shadow/reanimation calls, preserving the original stack local
        # holding the garden predicate for the subsequent widget writes.
        self.uc.emu_start(0x45509C, 0x4550B7, count=100)
        if self.uc.reg_read(UC_X86_REG_EIP) != 0x4550B7:
            raise AssertionError("Garden widget slice did not finish")
        for index in range(3):
            widget = WIDGETS + index * 0x100
            if self.read8(widget + 0x66) != 1 - self.read8(widget + 0x64):
                raise AssertionError("Auxiliary visible/disabled bytes disagree")
        return [
            self.read_signed32(SELECTOR + 0x14C), self.read8(SELECTOR + 0xF3),
            self.read8(SELECTOR + 0xF0), self.read8(SELECTOR + 0xF1),
            self.read8(SELECTOR + 0xF2), self.read8(WIDGETS + 0x64),
            self.read8(WIDGETS + 0x164), self.read8(WIDGETS + 0x264),
            self.read8(SELECTOR + 0x158), self.read8(SELECTOR + 0xF4),
            self.click(101), self.click(102), self.click(110),
        ]


def cases():
    levels = (-2147483648, 0, 1, 2, 14, 15, 24, 25, 44, 45, 46, 2147483647)
    # app flag, app value, service pointer, then the three query results
    runtimes = ((0, 0, 0, 0, 0, 0), (0, 1, 0, 1, 1, 1), (0, 1, 1, 0, 1, 0),
                (0, 1, 1, 1, 0, 1), (1, 0, 1, 1, 0, 1), (0, 2, 0, 0, 0, 0))
    index = 0
    for level, completed, store, has_profile, runtime, override in itertools.product(
            levels, (-1, 0, 1), (-1, 0, 1), (0, 1), runtimes, (0, 1)):
        mini = (0, 1, -1)[index % 3]
        puzzle = (1, 0, -1)[(index // 3) % 3]
        survival = (-1, 1, 0)[(index // 9) % 3]
        yield (has_profile, level, completed, mini, puzzle, survival, store, index % 2,
               *runtime, override, (0, 1, -1)[(index // 7) % 3])
        index += 1

    random_source = random.Random(0x455420)
    raw = (-2147483648, -2, -1, 0, 1, 2, 2147483647)
    for _ in range(5000):
        yield (random_source.randrange(2), random_source.choice(levels), random_source.choice(raw),
               random_source.choice(raw), random_source.choice(raw), random_source.choice(raw),
               random_source.choice(raw), random_source.choice((0, 1, 255)),
               random_source.choice((0, 1, 255)), random_source.choice(raw),
               random_source.randrange(2), random_source.randrange(2),
               random_source.randrange(2), random_source.randrange(2),
               random_source.choice((0, 1, 255)), random_source.choice((0, 1, -1)))


def main():
    if not TEST_DLL.is_file():
        raise RuntimeError("Build SelectorProgressTests in Release for net9.0 first")
    rows = list(cases())
    input_text = "".join(",".join(str(value) for value in row) + "\n" for row in rows)
    result = subprocess.run(["dotnet", str(TEST_DLL), "--native-oracle"], input=input_text,
                            capture_output=True, text=True, check=True)
    outputs = result.stdout.splitlines()
    if len(outputs) != len(rows):
        raise AssertionError("Unexpected number of C# oracle responses")
    oracle = NativeOracle()
    for index, (row, output) in enumerate(zip(rows, outputs)):
        actual = [int(value) for value in output.split(",")]
        expected = oracle.evaluate(row)
        if actual != expected:
            raise AssertionError("Case %d: input=%r\nC#=%r\nnative=%r" % (index, row, actual, expected))
    print("PASS: %d native EXE differential cases, including %d mode callbacks." %
          (len(rows), 3 * len(rows)))


if __name__ == "__main__":
    main()
