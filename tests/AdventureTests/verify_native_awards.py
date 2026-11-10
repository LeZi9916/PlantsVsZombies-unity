"""Read-only native reward branch verification with a deliberately limited interpreter.

Capstone decodes original EXE bytes. The Python interpreter below implements only
the integer instructions reached at 0x54BD41..0x54BE86 and supplies one external
HasFinishedAdventure=false predicate. It is NOT a full x86 emulator, CPU execution,
or a replay of original victory gameplay. The installed Unicorn Windows DLL faults
in mem_map, so no Unicorn result is claimed. No original files are written.
"""
from pathlib import Path
import hashlib
import json
import struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32, CS_OP_REG, CS_OP_IMM, CS_OP_MEM

HERE = Path(__file__).resolve().parent
EXE = HERE.parents[1].parent / 'PlantsVsZombies.exe'
EXPECTED_HASH = 'e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c'
START, STOP = 0x54BD41, 0x54BE86
pe = EXE.read_bytes()
assert hashlib.sha256(pe).hexdigest() == EXPECTED_HASH, 'Wrong native executable'
h = struct.unpack_from('<I', pe, 0x3C)[0]
count = struct.unpack_from('<H', pe, h + 6)[0]
optional = struct.unpack_from('<H', pe, h + 20)[0]
sections = []
for i in range(count):
    p = h + 24 + optional + 40 * i
    vs, rva, size, offset = struct.unpack_from('<IIII', pe, p + 8)
    sections.append((rva, max(vs, size), offset))

def read_va(address, length):
    rva = address - 0x400000
    for start, size, offset in sections:
        if start <= rva < start + size:
            return pe[offset + rva - start:offset + rva - start + length]
    raise ValueError(hex(address))

md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
instructions = {i.address:i for i in md.disasm(read_va(START, STOP-START), START)}

def evaluate(level):
    registers = {'eax':0x1000000, 'esi':0x2000000, 'edi':0}
    memory = {0x1000000 + 0x5568:level, 0x2000000:0}
    zero, carry, signed_compare = False, False, 0
    pc = START
    def getreg(name):
        return registers['eax'] & 255 if name == 'al' else registers[name]
    def operand(op, ins):
        if op.type == CS_OP_REG:
            return getreg(ins.reg_name(op.reg))
        if op.type == CS_OP_IMM:
            return op.imm & 0xffffffff
        if op.type == CS_OP_MEM:
            address = op.mem.disp
            if op.mem.base: address += getreg(ins.reg_name(op.mem.base))
            if op.mem.index: address += getreg(ins.reg_name(op.mem.index)) * op.mem.scale
            return memory[address]
        raise ValueError('Unsupported operand')
    def assign(op, ins, value):
        assert op.type == CS_OP_REG and op.size == 4
        registers[ins.reg_name(op.reg)] = value & 0xffffffff
    def signed(v): return v if v < 0x80000000 else v - 0x100000000
    for step in range(200):
        if pc == STOP: return registers['edi']
        ins = instructions[pc]
        ops, mnemonic, next_pc = ins.operands, ins.mnemonic, pc + ins.size
        if mnemonic in ('mov', 'movzx'): assign(ops[0],ins,operand(ops[1],ins))
        elif mnemonic == 'call':
            assert pc == 0x54BD73 and operand(ops[0],ins) == 0x461560
            registers['eax'] = 0
        elif mnemonic in ('cmp','test'):
            a,b = operand(ops[0],ins),operand(ops[1],ins)
            zero = (a == b) if mnemonic == 'cmp' else (a & b) == 0
            carry = a < b if mnemonic == 'cmp' else False
            signed_compare = signed(a)-signed(b)
        elif mnemonic in ('je','jne','jg','jmp'):
            take = mnemonic == 'jmp' or mnemonic == 'je' and zero or mnemonic == 'jne' and not zero or mnemonic == 'jg' and signed_compare > 0
            if take: next_pc = operand(ops[0],ins)
        elif mnemonic == 'neg':
            value=operand(ops[0],ins);carry=value!=0;assign(ops[0],ins,-value)
        elif mnemonic in ('and','add','sub','sbb'):
            a,b=operand(ops[0],ins),operand(ops[1],ins)
            value = a & b if mnemonic == 'and' else a+b if mnemonic == 'add' else a-b-(int(carry) if mnemonic == 'sbb' else 0)
            assign(ops[0],ins,value)
        else: raise ValueError((hex(pc),mnemonic))
        pc=next_pc
    raise ValueError('Instruction bound exceeded')

records=[]
for level in range(1,51):
    raw=evaluate(level)
    expected=21 if level==50 else 15 if level%10==9 else {4:9,14:10,24:11,34:14,44:13}.get(level,7)
    assert raw==expected,(level,raw,expected)
    records.append({'level':level,'native_reward_id':raw})
out=HERE/'evidence'
out.mkdir(exist_ok=True)
result={'exe_sha256':EXPECTED_HASH,'entry_va':hex(START),'stop_va':hex(STOP),
        'method':'Capstone-decoded restricted integer instruction interpreter; not CPU/x86 emulator execution',
        'supplied_predicate':'HasFinishedAdventure=false at call 0x54BD73',
        'scope':'isolated reward selection branch; not full original gameplay','levels':records}
(out/'native-awards.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'native-awards.asm.txt').write_text('\n'.join(f'{i.address:08X} {i.bytes.hex():20} {i.mnemonic:7} {i.op_str}' for i in instructions.values())+'\n',encoding='utf-8')
print('PASS: 50 native reward branches decoded/interpreted; read-only EXE; no full x86 execution claimed.')
