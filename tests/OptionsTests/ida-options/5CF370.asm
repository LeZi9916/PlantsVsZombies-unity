005CF370  push    ebp
005CF371  mov     ebp, esp
005CF373  push    ecx
005CF374  mov     [ebp+var_4], ecx
005CF377  mov     eax, [ebp+arg_C]
005CF37A  push    eax
005CF37B  mov     ecx, [ebp+arg_8]
005CF37E  push    ecx
005CF37F  mov     edx, [ebp+arg_4]
005CF382  push    edx
005CF383  mov     eax, [ebp+arg_0]
005CF386  push    eax
005CF387  mov     ecx, [ebp+var_4]
005CF38A  call    sub_5C54C0
005CF38F  mov     ecx, [ebp+arg_C]
005CF392  push    ecx
005CF393  mov     edx, [ebp+var_4]
005CF396  mov     eax, [edx+0A0h]
005CF39C  push    eax
005CF39D  mov     ecx, [ebp+var_4]
005CF3A0  mov     edx, [ecx+11Ch]
005CF3A6  mov     eax, [ebp+var_4]
005CF3A9  mov     ecx, [eax+11Ch]
005CF3AF  mov     edx, [edx]
005CF3B1  mov     eax, [edx]
005CF3B3  call    eax
005CF3B5  mov     ecx, [ebp+var_4]
005CF3B8  mov     edx, [ecx]
005CF3BA  mov     ecx, [ebp+var_4]
005CF3BD  mov     eax, [edx+44h]
005CF3C0  call    eax
005CF3C2  mov     esp, ebp
005CF3C4  pop     ebp
005CF3C5  retn    10h
