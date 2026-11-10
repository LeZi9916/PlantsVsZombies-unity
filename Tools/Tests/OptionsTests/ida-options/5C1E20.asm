005C1E20  push    ebp
005C1E21  mov     ebp, esp
005C1E23  push    ecx
005C1E24  mov     [ebp+var_4], ecx
005C1E27  mov     eax, [ebp+var_4]
005C1E2A  cmp     dword ptr [eax+24h], 0
005C1E2E  jz      short loc_5C1E49
005C1E30  mov     ecx, [ebp+var_4]
005C1E33  push    ecx
005C1E34  mov     edx, [ebp+var_4]
005C1E37  mov     eax, [edx+24h]
005C1E3A  mov     ecx, [ebp+var_4]
005C1E3D  mov     ecx, [ecx+24h]
005C1E40  mov     edx, [eax]
005C1E42  mov     eax, [edx+48h]
005C1E45  call    eax
005C1E47  jmp     short loc_5C1E50
005C1E49  mov     ecx, [ebp+var_4]
005C1E4C  mov     byte ptr [ecx+3Ch], 1
005C1E50  mov     esp, ebp
005C1E52  pop     ebp
005C1E53  retn
