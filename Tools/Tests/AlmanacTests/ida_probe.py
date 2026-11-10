"""Read-only IDA evidence probe for the local executable's almanac."""
import json
import os
import traceback
import ida_auto
import ida_bytes
import ida_funcs
import ida_hexrays
import ida_pro
import idautils
import idc

out = os.path.dirname(os.path.abspath(__file__))

def function(ea):
    f = ida_funcs.get_func(ea)
    return None if f is None else hex(f.start_ea)

def main():
    ida_auto.auto_wait()
    ida_hexrays.init_hexrays_plugin()
    terms = ["SUBURBAN_ALMANAC_INDEX", "SUBURBAN_ALMANAC_PLANTS", "SUBURBAN_ALMANAC_ZOMBIES",
             "VIEW_PLANTS", "VIEW_ZOMBIES", "ALMANAC_INDEX", "DESCRIPTION_HEADER",
             "IMAGE_ALMANAC_INDEXBACK", "IMAGE_ALMANAC_PLANTBACK", "IMAGE_ALMANAC_ZOMBIEBACK",
             "IMAGE_ALMANAC_PLANTCARD", "IMAGE_ALMANAC_ZOMBIECARD", "IMAGE_ALMANAC_ZOMBIEWINDOW"]
    found = []
    targets = {0x4880A0, 0x409260}
    targets.update(ea for ea in idautils.Functions(0x409260, 0x40D1E0))
    for start in idautils.Segments():
        end = idc.get_segm_end(start)
        data = ida_bytes.get_bytes(start, end - start)
        if not data:
            continue
        for term in terms:
            for encoding in ("ascii", "utf-16le"):
                needle = term.encode(encoding)
                offset = data.find(needle)
                while offset >= 0:
                    ea = start + offset
                    refs = []
                    for probe in (ea - 2, ea - 1, ea):
                        for xref in idautils.XrefsTo(probe):
                            f = function(xref.frm)
                            refs.append({"from": hex(xref.frm), "function": f,
                                         "insn": idc.generate_disasm_line(xref.frm, 0)})
                            if f:
                                targets.add(int(f, 16))
                    found.append({"term": term, "encoding": encoding, "address": hex(ea), "refs": refs})
                    offset = data.find(needle, offset + 1)
    evidence = []
    for ea in sorted(targets):
        f = ida_funcs.get_func(ea)
        if not f:
            continue
        item = {"address": hex(f.start_ea), "end": hex(f.end_ea), "callers": [], "calls": [], "assembly": []}
        for ref in idautils.CodeRefsTo(f.start_ea, False):
            item["callers"].append({"at": hex(ref), "function": function(ref)})
        for insn in idautils.FuncItems(f.start_ea):
            item["assembly"].append({"at": hex(insn), "text": idc.generate_disasm_line(insn, 0)})
            if idc.print_insn_mnem(insn).lower() == "call":
                for ref in idautils.CodeRefsFrom(insn, False):
                    item["calls"].append({"at": hex(insn), "target": hex(ref), "function": function(ref)})
        try:
            item["pseudocode"] = str(ida_hexrays.decompile(f.start_ea))
        except Exception as error:
            item["error"] = str(error)
        evidence.append(item)
    with open(os.path.join(out, "ida-evidence.json"), "w", encoding="utf-8") as f:
        json.dump({"matches": found, "functions": evidence}, f, ensure_ascii=False, indent=2)

try:
    main()
except Exception:
    with open(os.path.join(out, "ida-error.txt"), "w", encoding="utf-8") as f:
        traceback.print_exc(file=f)
    ida_pro.qexit(1)
else:
    ida_pro.qexit(0)
