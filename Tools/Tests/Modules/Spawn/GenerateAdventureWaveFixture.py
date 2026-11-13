"""Execute the hash-matched PC adventure initializer and full wave constructor.

Requires Unicorn 2.1.4 and the supplied original analysis/ida/waves/pe_oracle.py
PE mapping helper. Only the native bounded-random helper is replaced by ticket 0.
The original directory is read-only; the output belongs to the test fixture folder.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import struct

from unicorn.x86_const import (
    UC_X86_REG_EAX, UC_X86_REG_EBP, UC_X86_REG_EBX, UC_X86_REG_ECX,
    UC_X86_REG_EDI, UC_X86_REG_EDX, UC_X86_REG_EIP, UC_X86_REG_ESI,
    UC_X86_REG_ESP,
)

EXPECTED = "e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--origin", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("adventure-wave-native.json"))
    args = parser.parse_args()
    image = (args.origin / "PlantsVsZombies.exe").read_bytes()
    if hashlib.sha256(image).hexdigest() != EXPECTED:
        raise ValueError("Unexpected original executable SHA-256")
    spec = importlib.util.spec_from_file_location("original_wave_pe_mapper", args.origin / "analysis/ida/waves/pe_oracle.py")
    original = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(original)

    class Native(original.Oracle):
        """Runs original wave instructions while observing zero bounded tickets."""

        def random_hook(self, uc, address, size, unused):
            esp = uc.reg_read(UC_X86_REG_ESP)
            bound = self.read32(esp + 4)
            if bound == 0:
                raise ValueError("Native zero random bound")
            self.bounds.append(bound)
            uc.reg_write(UC_X86_REG_EAX, 0)
            uc.reg_write(UC_X86_REG_EIP, self.read32(esp))
            uc.reg_write(UC_X86_REG_ESP, esp + 4)

        def invoke(self, address, registers):
            for register in (UC_X86_REG_EAX, UC_X86_REG_EBX, UC_X86_REG_ECX, UC_X86_REG_EDX,
                             UC_X86_REG_EDI, UC_X86_REG_ESI, UC_X86_REG_EBP):
                self.uc.reg_write(register, 0)
            esp = original.STACK + 0xF000
            self.uc.mem_write(esp, struct.pack("<I", original.STOP))
            self.uc.reg_write(UC_X86_REG_ESP, esp)
            for register, value in registers.items():
                self.uc.reg_write(register, value)
            self.uc.emu_start(address, original.STOP, count=5000000)
            if self.uc.reg_read(UC_X86_REG_EIP) != original.STOP:
                raise RuntimeError("Native instruction limit reached")

        def generate(self, level, progress):
            self.bounds = []
            self.uc.mem_write(original.BOARD + 0x6CC, bytes(0x4F00))
            self.uc.mem_write(original.APP, bytes(0x1000))
            for address, value in [
                (original.BOARD + 0xA4, original.APP), (0x755E0C, original.APP),
                (original.APP + 0x950, original.PROFILE), (original.APP + 0x868, 1),
                (original.PROFILE + 0x4C, level), (original.PROFILE + 0x54, progress),
                (original.BOARD + 0x5568, level), (original.BOARD + 0x178, original.STATE),
                (original.STATE, original.APP), (original.STATE + 4, original.BOARD),
            ]:
                self.write32(address, value)
            self.invoke(0x416860, {UC_X86_REG_EDX: level, UC_X86_REG_EDI: original.BOARD})
            enabled = [i for i, flag in enumerate(self.uc.mem_read(original.BOARD + 0x54EC, 34)) if flag]
            self.invoke(0x4150C0, {UC_X86_REG_EDI: original.BOARD})
            waves = []
            for wave in range(self.read32(original.BOARD + 0x557C)):
                row = []
                for slot in range(50):
                    identifier = self.read32(original.BOARD + 0x6CC + wave * 200 + slot * 4)
                    if identifier == 0xFFFFFFFF:
                        break
                    row.append(identifier)
                waves.append(row)
            return {"level": level, "progress": progress, "enabled": enabled, "waves": waves, "bounds": self.bounds}

    native = Native(image)
    result = {
        "format": 1, "exe_sha256": EXPECTED,
        "entry": "0x416860 and full 0x4150C0, app+0x868=1, all random tickets zero; not full game startup",
        "cases": [native.generate(level, progress) for level in range(1, 51) for progress in range(3)],
    }
    args.output.write_text(json.dumps(result, separators=(",", ":")) + "\n")
    print("Captured 150 original adventure wave plans")


if __name__ == "__main__":
    main()
