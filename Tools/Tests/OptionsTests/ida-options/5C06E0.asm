005C06E0  push    ebp
005C06E1  mov     ebp, esp
005C06E3  sub     esp, 24h
005C06E6  push    esi
005C06E7  mov     [ebp+var_24], ecx
005C06EA  lea     eax, [ebp+var_20]
005C06ED  push    eax
005C06EE  mov     ecx, [ebp+arg_0]
005C06F1  mov     edx, [ecx]
005C06F3  mov     ecx, [ebp+arg_0]
005C06F6  mov     eax, [edx+4]
005C06F9  call    eax
005C06FB  mov     esi, eax
005C06FD  lea     ecx, [ebp+var_10]
005C0700  push    ecx
005C0701  mov     edx, [ebp+var_24]
005C0704  mov     eax, [edx]
005C0706  mov     ecx, [ebp+var_24]
005C0709  mov     edx, [eax+4]
005C070C  call    edx
005C070E  mov     ecx, eax
005C0710  mov     eax, esi
005C0712  call    sub_47FBE0
005C0717  pop     esi
005C0718  mov     esp, ebp
005C071A  pop     ebp
005C071B  retn    4
