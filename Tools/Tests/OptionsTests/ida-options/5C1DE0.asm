005C1DE0  push    ebp
005C1DE1  mov     ebp, esp
005C1DE3  push    ecx
005C1DE4  mov     [ebp+var_4], ecx
005C1DE7  mov     eax, [ebp+var_4]
005C1DEA  cmp     dword ptr [eax+24h], 0
005C1DEE  jz      short loc_5C1E09
005C1DF0  mov     ecx, [ebp+var_4]
005C1DF3  push    ecx
005C1DF4  mov     edx, [ebp+var_4]
005C1DF7  mov     eax, [edx+24h]
005C1DFA  mov     ecx, [ebp+var_4]
005C1DFD  mov     ecx, [ecx+24h]
005C1E00  mov     edx, [eax]
005C1E02  mov     eax, [edx+40h]
005C1E05  call    eax
005C1E07  jmp     short loc_5C1E10
005C1E09  mov     ecx, [ebp+var_4]
005C1E0C  mov     byte ptr [ecx+3Ch], 1
005C1E10  mov     esp, ebp
005C1E12  pop     ebp
005C1E13  retn
