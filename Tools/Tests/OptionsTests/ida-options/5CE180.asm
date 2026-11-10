005CE180  push    ebp
005CE181  mov     ebp, esp
005CE183  push    ecx
005CE184  mov     [ebp+var_4], ecx
005CE187  mov     eax, [ebp+var_4]
005CE18A  mov     cl, [ebp+arg_0]
005CE18D  mov     [eax+0A8h], cl
005CE193  movzx   edx, [ebp+arg_4]
005CE197  test    edx, edx
005CE199  jz      short loc_5CE1D4
005CE19B  mov     eax, [ebp+var_4]
005CE19E  cmp     dword ptr [eax+0A0h], 0
005CE1A5  jz      short loc_5CE1D4
005CE1A7  mov     ecx, [ebp+var_4]
005CE1AA  movzx   edx, byte ptr [ecx+0A8h]
005CE1B1  push    edx
005CE1B2  mov     eax, [ebp+var_4]
005CE1B5  mov     ecx, [eax+0A4h]
005CE1BB  push    ecx
005CE1BC  mov     edx, [ebp+var_4]
005CE1BF  mov     eax, [edx+0A0h]
005CE1C5  mov     ecx, [ebp+var_4]
005CE1C8  mov     ecx, [ecx+0A0h]
005CE1CE  mov     edx, [eax]
005CE1D0  mov     eax, [edx]
005CE1D2  call    eax
005CE1D4  mov     ecx, [ebp+var_4]
005CE1D7  mov     edx, [ecx]
005CE1D9  mov     ecx, [ebp+var_4]
005CE1DC  mov     eax, [edx+44h]
005CE1DF  call    eax
005CE1E1  mov     esp, ebp
005CE1E3  pop     ebp
005CE1E4  retn    8
