"""Read-only, hash-pinned PC shadow image-coordinate oracle.

Interprets the exact integer/x87 instructions of 0x527D20 up to its matrix
submission at 0x527DEE. The two native image-size getter calls read their real
Image fields; matrix initialization and renderer submission are explicit
boundaries. This is a restricted instruction interpreter, not full game/CPU
execution. Body-origin numbers use the separately verified 0x547FF0 formula.
"""

from pathlib import Path
import hashlib
import json
import re
import struct
import sys


ROOT = Path(__file__).resolve().parents[3]
try:
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    from capstone.x86_const import X86_OP_IMM, X86_OP_MEM, X86_OP_REG
except ImportError:
    sys.path.insert(0, str(ROOT.parent / "analysis/ida/plant_timing/vendor"))
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    from capstone.x86_const import X86_OP_IMM, X86_OP_MEM, X86_OP_REG


EXPECTED = "e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c"


class ImageCoordinateOracle:
    """Interpret only the original image helper's observed instruction subset."""

    def __init__(self):
        self.data = (ROOT.parent / "PlantsVsZombies.exe").read_bytes()
        assert hashlib.sha256(self.data).hexdigest() == EXPECTED
        pe = struct.unpack_from("<I", self.data, 60)[0]
        self.base = struct.unpack_from("<I", self.data, pe + 52)[0]
        self.sections = []
        for index in range(struct.unpack_from("<H", self.data, pe + 6)[0]):
            at = pe + 24 + struct.unpack_from("<H", self.data, pe + 20)[0] + index * 40
            _, virtual, size, source = struct.unpack_from("<IIII", self.data, at + 8)
            self.sections.append((virtual, size, source))
        decoder = Cs(CS_ARCH_X86, CS_MODE_32)
        decoder.detail = True
        self.code = list(decoder.disasm(
            self.data[self.offset(0x527D20):self.offset(0x527DFD)], 0x527D20
        ))
        self.indices = {instruction.address: index for index, instruction in enumerate(self.code)}

    def offset(self, address):
        """Resolve a native virtual address into the pinned PE file."""
        return next(source + address - self.base - virtual
                    for virtual, size, source in self.sections
                    if 0 <= address - self.base - virtual < size)

    def read(self, address, size):
        """Read fixture bytes or immutable original executable constants."""
        result = bytearray()
        for byte_address in range(address, address + size):
            if byte_address in self.memory:
                result.append(self.memory[byte_address])
            else:
                try:
                    result.append(self.data[self.offset(byte_address)])
                except StopIteration:
                    result.append(0)
        return bytes(result)

    def write(self, address, data):
        """Write only the interpreter's isolated fixture memory."""
        for index, value in enumerate(data):
            self.memory[address + index] = value

    def address(self, instruction, operand):
        """Evaluate the native base/index/displacement address expression."""
        memory = operand.mem
        base = self.registers.get(instruction.reg_name(memory.base), 0)
        index = self.registers.get(instruction.reg_name(memory.index), 0)
        return base + index * memory.scale + memory.disp

    def value(self, instruction, operand, floating=False):
        """Read an immediate, register, or memory operand at its native width."""
        if operand.type == X86_OP_IMM:
            return operand.imm
        if operand.type == X86_OP_REG:
            name = instruction.reg_name(operand.reg)
            if name.startswith("st"):
                return self.fpu[self.stack_index(name)]
            return self.registers[name]
        assert operand.type == X86_OP_MEM
        value = self.read(self.address(instruction, operand), operand.size)
        return struct.unpack("<d" if operand.size == 8 else "<f", value)[0] if floating else int.from_bytes(value, "little")

    def assign(self, instruction, operand, value, floating=False):
        """Store a native register or memory operand, including float32 rounding."""
        if operand.type == X86_OP_REG:
            name = instruction.reg_name(operand.reg)
            if name.startswith("st"):
                self.fpu[self.stack_index(name)] = value
            else:
                self.registers[name] = value & 0xFFFFFFFF
        else:
            address = self.address(instruction, operand)
            data = struct.pack("<f", value) if floating else int(value & 0xFFFFFFFF).to_bytes(operand.size, "little")
            self.write(address, data)

    @staticmethod
    def stack_index(name):
        """Decode Capstone's x87 stack register spelling."""
        return int(re.search(r"\d", name)[0])

    def run(self, x, y, scale, trans_x, trans_y, width=86, height=36):
        """Execute the original image helper until it submits its native matrix."""
        self.memory = {}
        self.fpu = []
        self.registers = {name: 0 for name in ("eax", "ebx", "ecx", "edx", "esi", "edi", "ebp")}
        self.registers.update(esp=0x200F000, esi=0x1000000)
        image = 0x1010000
        for address, value in ((image + 0x24, width), (image + 0x28, height),
                               (image + 0x2C, 1), (image + 0x30, 1)):
            self.write(address, struct.pack("<I", value))
        self.write(0x1000008, struct.pack("<f", trans_x))
        self.write(0x100000C, struct.pack("<f", trans_y))
        self.write(0x200F000, struct.pack("<IIffIff", 0, image, x, y, 0, scale, scale))
        cursor = 0
        equal = False
        while cursor < len(self.code):
            instruction = self.code[cursor]
            operands = instruction.operands
            opcode = instruction.mnemonic
            if opcode in ("mov", "lea"):
                value = self.address(instruction, operands[1]) if opcode == "lea" else self.value(instruction, operands[1])
                self.assign(instruction, operands[0], value)
            elif opcode in ("sub", "add", "imul"):
                left = self.value(instruction, operands[0])
                right = self.value(instruction, operands[1])
                value = left - right if opcode == "sub" else left + right if opcode == "add" else left * right
                self.assign(instruction, operands[0], value)
            elif opcode == "push":
                value = self.value(instruction, operands[0])
                self.registers["esp"] -= 4
                self.write(self.registers["esp"], struct.pack("<I", value))
            elif opcode == "cmp":
                equal = self.value(instruction, operands[0]) == self.value(instruction, operands[1])
            elif opcode == "jne":
                if not equal:
                    cursor = self.indices[operands[0].imm]
                    continue
            elif opcode == "call":
                target = operands[0].imm
                if target in (0x595120, 0x595140):
                    offset, divisor = (0x28, 0x2C) if target == 0x595120 else (0x24, 0x30)
                    pointer = self.registers["ecx"]
                    size = int.from_bytes(self.read(pointer + offset, 4), "little")
                    cells = int.from_bytes(self.read(pointer + divisor, 4), "little")
                    self.registers["eax"] = size // cells
                elif target == 0x527BD0:
                    stack = self.registers["esp"]
                    matrix = int.from_bytes(self.read(stack, 4), "little")
                    return list(struct.unpack("<9f", self.read(matrix, 36)))
                else:
                    assert target == 0x672B30, hex(target)
            elif opcode in ("fld", "fild", "fldz", "fld1"):
                value = 0.0 if opcode == "fldz" else 1.0 if opcode == "fld1" else self.value(instruction, operands[0], opcode == "fld")
                self.fpu.insert(0, float(value))
            elif opcode in ("fst", "fstp"):
                self.assign(instruction, operands[0], self.fpu[0], True)
                if opcode == "fstp":
                    self.fpu.pop(0)
            elif opcode == "fxch":
                index = self.stack_index(instruction.reg_name(operands[-1].reg))
                self.fpu[0], self.fpu[index] = self.fpu[index], self.fpu[0]
            elif opcode in ("fadd", "fmul", "fimul", "faddp"):
                if opcode == "faddp":
                    index = self.stack_index(instruction.reg_name(operands[0].reg))
                    self.fpu[index] += self.fpu[0]
                    self.fpu.pop(0)
                elif len(operands) == 2:
                    value = self.value(instruction, operands[0], True) * self.value(instruction, operands[1], True)
                    self.assign(instruction, operands[0], value, True)
                else:
                    value = self.value(instruction, operands[0], opcode != "fimul")
                    self.fpu[0] = self.fpu[0] + value if opcode == "fadd" else self.fpu[0] * value
            else:
                raise ValueError(f"Unsupported instruction at {instruction.address:X}: {opcode}")
            cursor += 1
        raise ValueError("Native helper did not reach its matrix submission.")


def main():
    """Print exact native helper coordinates and the verified body-origin relation."""
    header = (ROOT / "Assets/StreamingAssets/images/plantshadow.png").read_bytes()
    assert struct.unpack_from(">II", header, 16) == (86, 36)
    oracle = ImageCoordinateOracle()
    cases = []
    for scale in (1.0, .5):
        for backwards in (False, True):
            native_x, native_y = 200.25, 100.75
            x = native_x - int(native_x) + 20 * scale - 20 + (11 if backwards else 23)
            y = native_y - int(native_y) + 92
            matrix = oracle.run(x, y, scale, int(native_x), int(native_y))
            assert matrix[2] == x + int(native_x) + 43
            assert matrix[5] == y + int(native_y) + 18
            cases.append({
                "scale": scale, "backwards": backwards,
                "native_position": [native_x, native_y],
                "shadow_center_from_native_instructions": [matrix[2], matrix[5]],
                "shadow_bounds": [matrix[2] - 43 * scale, matrix[5] - 18 * scale, 86 * scale, 36 * scale],
                "body_origin_from_verified_547FF0": [native_x + 15 + 30 * (1 - scale) + (90 * scale if backwards else 0),
                                                       native_y - 8 + 120 * (1 - scale)],
            })
    # GetPosYBasedOnRow 0x54D620, GetDrawPos 0x549740, UpdateReanim 0x547FF0, DrawShadow 0x54FF00.
    # Native PC omits the dancer +15/-18 branches found in the mobile reference port.
    species = [
        ("pole", 1, 0, -6, -11, 36, 11, 1),
        ("football", 1, 0, 0, -16, 41, 16, 1),
        ("small-football", .5, 0, 0, -16, 30.5, 16, 1),
        ("newspaper", 1, 0, 0, 0, 29, 4, 1),
        ("imp", 1, 0, 0, 0, 25, 7, .6),
        ("gargantuar", 1, 0, 0, -8, 27, 7, 1.5),
        ("digger-ground", 1, 0, 0, 0, 17, 5, 1),
        ("yeti", 1, 0, 0, -20, 3, 20, 1),
        ("bobsled", 1, 0, 0, -12, 20, 13, 1),
        ("pogo-ground", 1, -16, 0, 16, 23, 0, 1),
        ("balloon-ground", 1, -30, 0, 17, 23, 13, 1),
        ("dancer", 1, 0, 0, 0, 23, 0, 1),
    ]
    species_cases = []
    for name, scale, native_y_offset, body_x, body_y, shadow_x, shadow_y, shadow_scale in species:
        native_x, native_y = 200.25, 150 + native_y_offset
        x = native_x - int(native_x) + body_x + 20 * scale - 20 + shadow_x
        y = native_y - int(native_y) + body_y + shadow_y + 92
        scale_shadow = scale * shadow_scale
        matrix = oracle.run(x, y, scale_shadow, int(native_x), int(native_y))
        football_y = 20 * (1 - scale) if "football" in name else 0
        species_cases.append({
            "species": name,
            "body_origin_from_verified_offsets": [native_x + body_x + 15 + 30 * (1 - scale),
                                                   native_y + body_y - 8 + 120 * (1 - scale) + football_y],
            "shadow_bounds_from_native_image_helper": [matrix[2] - 43 * scale_shadow, matrix[5] - 18 * scale_shadow,
                                                        86 * scale_shadow, 36 * scale_shadow],
        })
    print(json.dumps({"exe_sha256": EXPECTED, "scope": __doc__, "original_image_size": [86, 36],
                      "cases": cases, "verified_species_offsets": species_cases}, indent=2))


if __name__ == "__main__":
    main()
