"""Read-only PC native random fixtures and source recording metadata verification."""

import argparse
import importlib.util
import json
from pathlib import Path
import re
import struct
import sys
import wave
import xml.etree.ElementTree as ET

import capstone
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_EDI, UC_X86_REG_EIP, UC_X86_REG_ESP

APP = 0x4000000
BOSS = APP + 0x2000
BOARD = APP + 0x3000
EFFECT = APP + 0x9000
HOLDER = APP + 0x9100
BASE = APP + 0xA000

def boss_vector(seed=1,health=40000,mode=0,stomp=-1,bungee=-1,head=5000,summon=500,mask=15,row=2):
 u=native.build_machine();u.mem_map(APP,0x10000)
 def w(a,v):u.mem_write(a,struct.pack('<I',v&0xffffffff))
 def r(a):return struct.unpack('<I',u.mem_read(a,4))[0]
 def ret(n=0,value=None):
  sp=u.reg_read(UC_X86_REG_ESP);pc=r(sp);u.reg_write(UC_X86_REG_ESP,sp+4+n);u.reg_write(UC_X86_REG_EIP,pc)
  if value is not None:u.reg_write(UC_X86_REG_EAX,value)
 w(APP+0x944,EFFECT);w(EFFECT+8,HOLDER);w(HOLDER,BASE);w(HOLDER+8,10)
 w(BOSS,APP);w(BOSS+0x24,25);w(BOSS+4,BOARD);w(BOSS+0x118,1);w(BOSS+0x144,2);w(BOSS+0x28,79)
 w(BASE+160+0x9c,1);w(BASE+320+0x9c,2)
 w(BOSS+0xc8,health);w(BOSS+0xcc,40000);w(BOSS+0x12c,mode)
 for off,v in [(0x138,stomp),(0x134,bungee),(0x13c,head),(0x114,summon)]:w(BOSS+off,v)
 u.mem_write(APP+0x9e9,b'\1')
 native.call(u,0x5eed00,seed)
 bounds=[];clips=[]
 def hook(u,addr,size,data):
  sp=u.reg_read(UC_X86_REG_ESP)
  if addr==0x5eecc0:bounds.append(r(sp+4))
  elif addr in [0x551aa0,0x54e0e0,0x54adc0]:ret()
  elif addr==0x419900:ret(4,row)
  elif addr==0x550d90:ret(4,int(bool(mask&(1<<u.reg_read(UC_X86_REG_EDI)))))
  elif addr==0x544580:
   s=bytes(u.mem_read(r(sp+4),80)).split(b'\0')[0].decode('ascii');clips.append(s);ret(16)
 u.hook_add(UC_HOOK_CODE,hook)
 sp=native.STACK+0x8000;w(sp,native.RETURN);u.reg_write(UC_X86_REG_ESP,sp);u.reg_write(UC_X86_REG_EAX,BOSS)
 u.emu_start(0x551ed0,native.RETURN,count=100000)
 def signed(v):return v if v<0x80000000 else v-0x100000000
 return {'input':[seed,health,mode,stomp,bungee,head,summon,mask,row],
  'phase':r(BOSS+0x28),'mode':r(BOSS+0x12c),'wait':r(BOSS+0x68),'summon':r(BOSS+0x114),'bungee':signed(r(BOSS+0x134)),'stomp':signed(r(BOSS+0x138)),'head':r(BOSS+0x13c),'targetRow':r(BOSS+0x130),'targetColumn':r(BOSS+0x80),'bounds':bounds,'clips':clips}

def foley_rows(machine, assets):
    names = {}
    current = None
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    for instruction in decoder.disasm(bytes(machine.mem_read(0x4935B0, 0x495D80 - 0x4935B0)), 0x4935B0):
        if instruction.mnemonic == "push" and instruction.op_str.startswith("0x"):
            address = int(instruction.op_str, 16)
            if 0x400000 <= address < 0x820000:
                value = bytes(machine.mem_read(address, 100)).split(b"\0")[0]
                if value.startswith(b"SOUND_"):
                    current = value.decode("ascii")
        if (instruction.mnemonic == "mov" and instruction.op_str.startswith("dword ptr [0x")
                and instruction.op_str.endswith("], eax") and current):
            address = int(instruction.op_str.split("[")[1].split("]")[0], 16)
            if 0x750000 < address < 0x755000:
                names[address] = current
    resources = ET.parse(assets / "properties/resources.xml").getroot()
    paths = {}
    for group in resources.findall("Resources"):
        prefix = ""
        for element in group:
            if element.tag == "SetDefaults":
                prefix = element.get("idprefix", prefix)
            elif element.tag == "Sound":
                paths[prefix + element.attrib["id"]] = element.attrib["path"]
    result = []
    for identifier in range(104):
        raw = bytes(machine.mem_read(0x74B3B8 + 52 * identifier, 52))
        words = struct.unpack("<13I", raw)
        assert words[0] == identifier
        samples = [paths[names[address]] for address in words[2:12] if address]
        result.append({"id": identifier, "pitch": struct.unpack_from("<f", raw, 4)[0],
                       "flags": words[12], "samples": samples})
    return result


def sample_lengths(rows, assets):
    result = {}
    for sample in sorted({sample for row in rows for sample in row["samples"]}):
        path = assets / "sounds" / (sample + ".ogg")
        if path.exists():
            data = path.read_bytes()
            header = data.index(b"\x01vorbis")
            rate = struct.unpack_from("<I", data, header + 12)[0]
            offset = 0
            frames = 0
            while offset < len(data):
                assert data[offset:offset + 4] == b"OggS"
                granule = struct.unpack_from("<Q", data, offset + 6)[0]
                if granule < 2 ** 63:
                    frames = max(frames, granule)
                segments = data[offset + 26]
                payload = sum(data[offset + 27:offset + 27 + segments])
                offset += 27 + segments + payload
            result[sample] = (frames, rate)
        else:
            with wave.open(str(path.with_suffix(".wav")), "rb") as source:
                result[sample] = (source.getnframes(), source.getframerate())
    return result


def check_sources(root, rows, lengths, vectors):
    source = (root / "Assets/Scripts/Adventure/AdventureZombieFoleyData.cs").read_text()
    definitions = {}
    pattern = r'\{\s*(\d+),\s*new AdventureZombieFoleyDefinition\(([-\d.]+)f, new\[\]\s*\{\s*([^}]*)\s*\}(?:, (\d+))?\)'
    for match in re.finditer(pattern, source):
        definitions[int(match[1])] = (float(match[2]), re.findall(r'"([^"]+)"', match[3]), int(match[4] or 0))
    assert len(definitions) == 104
    for row in rows:
        assert definitions[row["id"]] == (row["pitch"], row["samples"], row["flags"]), row["id"]
    source = (root / "Assets/Scripts/Adventure/AdventureFoleySampleData.cs").read_text()
    actual = {name: (int(samples), int(rate)) for name, samples, rate in
              re.findall(r'\{ "([^"]+)", \((\d+), (\d+)\) \}', source)}
    assert actual == lengths
    source = (root / "Assets/Tests/EditMode/AdventureNativeRandomTests.cs").read_text()
    for vector in vectors:
        original = vector["input"][:8]
        expected = [vector[key] for key in ("phase", "mode", "wait", "summon", "bungee", "stomp", "head", "targetRow", "targetColumn")]
        value = ",".join(map(str, original)) + "|" + ",".join(map(str, expected)) + "|" + ",".join(map(str, vector["bounds"]))
        assert '"' + value + '"' in source, value


def main():
    global native
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--origin", type=Path, required=True, help="Read-only supplied original analysis root.")
    parser.add_argument("--assets", type=Path, help="Imported original runtime resources; defaults to Assets/StreamingAssets.")
    parser.add_argument("--check", action="store_true", help="Verify committed C# fixture facts instead of exporting JSON.")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[4]
    path = args.origin / "analysis/ida/random/machine_oracle.py"
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location("pvz_original_machine", path)
    native = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(native)
    native.EXE = args.origin / "PlantsVsZombies.exe"
    assets = args.assets or root / "Assets/StreamingAssets"
    rows = foley_rows(native.build_machine(), assets)
    lengths = sample_lengths(rows, assets)
    vectors = []
    for seed in (1, 4357, 7529):
        for fields in ({}, {"health": 31999}, {"health": 19999},
                       {"mode": 1, "health": 30000, "stomp": 1, "mask": 6},
                       {"stomp": 1, "mask": 0}, {"bungee": 1}, {"head": 1}, {"summon": 1}):
            vectors.append(boss_vector(seed=seed, **fields))
    if args.check:
        check_sources(root, rows, lengths, vectors)
        print(json.dumps({"sha256": native.EXPECTED, "bossVectors": len(vectors),
                          "foleyRows": len(rows), "recordings": len(lengths), "result": "Passed"}))
    else:
        print(json.dumps({"sha256": native.EXPECTED, "bossVectors": vectors,
                          "foleyRows": rows, "sampleLengths": lengths}, indent=2))


if __name__ == "__main__":
    main()
