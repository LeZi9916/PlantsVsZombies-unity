005C51E0  push    ebp
005C51E1  mov     ebp, esp
005C51E3  push    ecx
005C51E4  mov     [ebp+var_4], ecx
005C51E7  mov     eax, [ebp+var_4]
005C51EA  mov     ecx, [eax+4Ch]
005C51ED  push    ecx
005C51EE  mov     edx, [ebp+var_4]
005C51F1  mov     eax, [edx+48h]
005C51F4  push    eax
005C51F5  mov     ecx, [ebp+arg_4]
005C51F8  push    ecx
005C51F9  mov     edx, [ebp+arg_0]
005C51FC  push    edx
005C51FD  mov     eax, [ebp+var_4]
005C5200  mov     edx, [eax]
005C5202  mov     ecx, [ebp+var_4]
005C5205  mov     eax, [edx+0A0h]
005C520B  call    eax
005C520D  mov     esp, ebp
005C520F  pop     ebp
005C5210  retn    8
