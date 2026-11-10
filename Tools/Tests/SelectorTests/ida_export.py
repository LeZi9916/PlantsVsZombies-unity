import os
import traceback

import ida_auto
import ida_bytes
import ida_funcs
import ida_hexrays
import ida_pro
import idautils
import idc


HERE = os.path.dirname(os.path.abspath(__file__))

try:
    ida_auto.auto_wait()
    if not ida_hexrays.init_hexrays_plugin():
        raise RuntimeError("Hex-Rays is unavailable")
    targets = [0x4539E0]
    with open(os.path.join(HERE, "ida_targets.txt"), encoding="utf-8") as source:
        targets.extend(int(line.strip(), 0) for line in source if line.strip())
    for address in sorted(set(targets)):
        func = ida_funcs.get_func(address)
        if not func:
            continue
        with open(os.path.join(HERE, "%X.c" % address), "w", encoding="utf-8") as output:
            output.write(str(ida_hexrays.decompile(func.start_ea)))
        with open(os.path.join(HERE, "%X.asm" % address), "w", encoding="utf-8") as output:
            for item in idautils.FuncItems(func.start_ea):
                output.write("%08X %s\n" % (item, idc.generate_disasm_line(item, 0)))
    with open(os.path.join(HERE, "selector_vtables.txt"), "w", encoding="utf-8") as output:
        for table in (0x70DCD0, 0x70DDE8):
            output.write("TABLE %X\n" % table)
            for offset in range(0, 0x120, 4):
                target = ida_bytes.get_dword(table + offset)
                func = ida_funcs.get_func(target)
                output.write("%03X %08X %s\n" % (offset, target, idc.get_func_name(target) if func else ""))
    ida_pro.qexit(0)
except Exception:
    with open(os.path.join(HERE, "ida_error.txt"), "w", encoding="utf-8") as output:
        traceback.print_exc(file=output)
    ida_pro.qexit(1)
