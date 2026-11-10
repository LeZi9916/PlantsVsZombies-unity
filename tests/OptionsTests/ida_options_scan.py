"""Inspect the local EXE's IDA database copy; outputs stay beside this script."""
import json
import os
import traceback

import ida_auto
import ida_bytes
import ida_funcs
import ida_hexrays
import ida_lines
import ida_pro
import idautils
import idc

HERE = os.path.dirname(os.path.abspath(__file__))


def run():
    ida_auto.auto_wait()
    ida_hexrays.init_hexrays_plugin()
    records = []
    functions = set()
    for ea in (0x5C5DF0, 0x464490, 0x453310, 0x453910, 0x526FC0, 0x452720):
        functions.add(ea)
    for ea in idautils.Functions(0x46A000, 0x46C000):
        functions.add(ea)
    for lo, hi in ((0x5C5DF0, 0x5C6700), (0x5CE0A0, 0x5CE800),
                   (0x5D1090, 0x5D1800), (0x4533B0, 0x453910)):
        functions.update(idautils.Functions(lo, hi))
    tables = {}
    for base in (0x711630, 0x711760, 0x71177C, 0x711780, 0x70D628, 0x70D500):
        table = []
        for i in range(100):
            slot = base + i * 4
            value = ida_bytes.get_dword(slot)
            func = ida_funcs.get_func(value)
            if i and (not func or func.start_ea != value):
                break
            table.append({"slot": hex(slot), "target": hex(value),
                          "function": hex(func.start_ea) if func else None})
            if func:
                functions.add(func.start_ea)
        tables[hex(base)] = table
    for ea in range(0x7112A0, 0x711600):
        refs = list(idautils.XrefsTo(ea, 0))
        if not refs:
            continue
        for ref in refs:
            func = ida_funcs.get_func(ref.frm)
            records.append({"target": hex(ea), "from": hex(ref.frm),
                            "function": hex(func.start_ea) if func else None,
                            "insn": ida_lines.tag_remove(idc.generate_disasm_line(ref.frm, 0) or "")})
            if func:
                functions.add(func.start_ea)
    globals_refs = {}
    for ea in (0x752CC8, 0x753420, 0x753710, 0x753094, 0x75334C,
               0x7530A0, 0x752C9C, 0x46A350, 0x46AD90):
        globals_refs[hex(ea)] = []
        for ref in idautils.XrefsTo(ea, 0):
            func = ida_funcs.get_func(ref.frm)
            globals_refs[hex(ea)].append({"from": hex(ref.frm),
                "function": hex(func.start_ea) if func else None,
                "insn": ida_lines.tag_remove(idc.generate_disasm_line(ref.frm, 0) or "")})
            if func:
                functions.add(func.start_ea)
    with open(os.path.join(HERE, "ida-options-refs.json"), "w", encoding="utf-8") as f:
        strings = {}
        for ea in (0x708B58, 0x70C874, 0x708B0C, 0x71128C):
            raw = idc.get_strlit_contents(ea, -1, idc.STRTYPE_C_16)
            strings[hex(ea)] = raw.decode("utf-16le", errors="replace") if raw else None
        json.dump({"bytes": ida_bytes.get_bytes(0x7112A0, 0x360).hex(" "),
                   "refs": records, "vtables": tables, "globals": globals_refs, "strings": strings},
                  f, ensure_ascii=False, indent=2)
    out = os.path.join(HERE, "ida-options")
    os.makedirs(out, exist_ok=True)
    for ea in sorted(functions):
        func = ida_funcs.get_func(ea)
        with open(os.path.join(out, "%X.asm" % ea), "w", encoding="utf-8") as f:
            for insn in idautils.FuncItems(ea):
                f.write("%08X  %s\n" % (insn, ida_lines.tag_remove(idc.generate_disasm_line(insn, 0) or "")))
        try:
            code = "omitted huge function" if func.end_ea - ea > 30000 else str(ida_hexrays.decompile(ea))
        except Exception as error:
            code = "decompile failed: " + str(error)
        with open(os.path.join(out, "%X.c" % ea), "w", encoding="utf-8") as f:
            f.write(code + "\n")


try:
    run()
except Exception:
    with open(os.path.join(HERE, "ida-options-error.txt"), "w", encoding="utf-8") as f:
        traceback.print_exc(file=f)
    ida_pro.qexit(1)
else:
    ida_pro.qexit(0)
