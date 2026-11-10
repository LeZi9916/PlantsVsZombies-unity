"""Execute two local EXE wave functions with synthetic 1-1 state in Unicorn.

This is an isolated x86 check, not a game replay. The only intercepted native
function is the weighted chooser, and only when its sole candidate is verified.
The third-party unicorn module must already be importable; nothing is installed.
"""
import hashlib
import json
import struct
from pathlib import Path

from unicorn import Uc, UC_ARCH_X86, UC_HOOK_CODE, UC_HOOK_MEM_WRITE, UC_MODE_32
from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_EDI, UC_X86_REG_EDX, UC_X86_REG_EIP, UC_X86_REG_ESP


ROOT = Path(__file__).resolve().parents[3]
EXE = ROOT / "PlantsVsZombies.exe"
BOARD, APP, PROFILE, CHALLENGE = 0x1000000, 0x1100000, 0x1200000, 0x1300000
STACK, STOP = 0x3000000, 0x4000000
EXPECTED_HASH = "e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c"


def u32(data, offset):
    return struct.unpack_from("<I", data, offset)[0]


def load_image(machine, data):
    pe = u32(data, 0x3C)
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError("not a PE image")
    sections = struct.unpack_from("<H", data, pe + 6)[0]
    optional_size = struct.unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    if struct.unpack_from("<H", data, optional)[0] != 0x10B:
        raise ValueError("expected PE32")
    base, size = u32(data, optional + 28), u32(data, optional + 56)
    if base != 0x400000:
        raise ValueError("unexpected image base")
    machine.mem_map(base, (size + 0xFFF) & ~0xFFF)
    machine.mem_write(base, data[:u32(data, optional + 60)])
    table = optional + optional_size
    for index in range(sections):
        entry = table + index * 40
        rva, raw_size, raw_offset = struct.unpack_from("<III", data, entry + 12)
        if raw_size:
            machine.mem_write(base + rva, data[raw_offset:raw_offset + raw_size])


def main():
    binary = EXE.read_bytes()
    digest = hashlib.sha256(binary).hexdigest()
    if digest != EXPECTED_HASH:
        raise ValueError("unexpected executable hash: " + digest)
    machine = Uc(UC_ARCH_X86, UC_MODE_32)
    load_image(machine, binary)
    for address in (BOARD, APP, PROFILE, CHALLENGE):
        machine.mem_map(address, 0x10000)
    machine.mem_map(STACK, 0x10000)
    machine.mem_map(STOP, 0x1000)

    def write32(address, value):
        machine.mem_write(address, struct.pack("<I", value & 0xFFFFFFFF))

    def read32(address):
        return struct.unpack("<I", machine.mem_read(address, 4))[0]

    write32(BOARD + 0xA4, APP)
    write32(BOARD + 0x178, CHALLENGE)
    write32(BOARD + 0x5568, 1)
    write32(APP + 0x868, BOARD)
    write32(APP + 0x91C, 0)
    write32(APP + 0x950, PROFILE)
    write32(PROFILE + 0x4C, 1)
    write32(PROFILE + 0x54, 0)
    write32(0x755E0C, APP)
    selections = []
    wave_writes = []

    def hook(uc, address, size, unused):
        if address != 0x5267C0:
            return
        stack = uc.reg_read(UC_X86_REG_ESP)
        count = uc.reg_read(UC_X86_REG_EDI)
        entries = read32(stack + 4)
        if count != 1 or (read32(entries), read32(entries + 4)) != (0, 4000):
            raise AssertionError("weighted chooser did not receive only normal zombie ID 0")
        selections.append([read32(entries), read32(entries + 4)])
        uc.reg_write(UC_X86_REG_EAX, entries)
        uc.reg_write(UC_X86_REG_EIP, read32(stack))
        uc.reg_write(UC_X86_REG_ESP, stack + 4)

    machine.hook_add(UC_HOOK_CODE, hook)

    def record_wave_write(uc, access, address, size, value, unused):
        if size == 4 and value != 0xFFFFFFFF:
            offset = address - (BOARD + 0x6CC)
            wave_writes.append({"wave": offset // 200, "slot": (offset % 200) // 4,
                                "id": value, "instruction": "0x%X" % uc.reg_read(UC_X86_REG_EIP)})

    machine.hook_add(UC_HOOK_MEM_WRITE, record_wave_write,
                     begin=BOARD + 0x6CC, end=BOARD + 0x6CC + 4 * 200 - 1)

    def call(address):
        stack = STACK + 0xF000
        write32(stack, STOP)
        machine.reg_write(UC_X86_REG_ESP, stack)
        machine.reg_write(UC_X86_REG_EDI, BOARD)
        machine.reg_write(UC_X86_REG_EDX, 1)
        machine.emu_start(address, STOP, count=2000000)
        if machine.reg_read(UC_X86_REG_EIP) != STOP:
            raise RuntimeError("instruction budget exhausted")

    call(0x416860)
    flags = list(machine.mem_read(BOARD + 0x54EC, 34))
    enabled = [index for index, value in enumerate(flags) if value]
    if enabled != [0]:
        raise AssertionError("unexpected eligible zombie IDs: " + repr(enabled))
    call(0x4150C0)
    wave_count = read32(BOARD + 0x557C)
    waves = []
    for wave in range(wave_count):
        members = []
        for slot in range(50):
            member = read32(BOARD + 0x6CC + wave * 200 + slot * 4)
            if member == 0xFFFFFFFF:
                break
            members.append(member)
        waves.append(members)
    if waves != [[0], [0], [0], [0, 0]]:
        raise AssertionError("unexpected native wave list: " + repr(waves))
    print(json.dumps({
        "exe_sha256": digest,
        "scope": "synthetic first-play 1-1; actual x86 functions 0x416860 and 0x4150C0",
        "eligible_ids": enabled,
        "waves": waves,
        "chooser_calls": len(selections),
        "wave_writes": wave_writes,
        "only_hook": "0x5267C0 returns its single checked ID-0 candidate; RNG is not emulated",
        "limitation": "not a full game, tutorial, timing, or victory replay"
    }, indent=2))


if __name__ == "__main__":
    main()
