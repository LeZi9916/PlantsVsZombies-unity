005CFFA0  push    ebp
005CFFA1  mov     ebp, esp
005CFFA3  push    ecx
005CFFA4  mov     [ebp+var_4], ecx
005CFFA7  mov     eax, [ebp+var_4]
005CFFAA  cmp     dword ptr [eax+0ACh], 0
005CFFB1  jz      short loc_5CFFD3
005CFFB3  mov     ecx, [ebp+arg_0]
005CFFB6  push    ecx
005CFFB7  mov     edx, [ebp+var_4]
005CFFBA  mov     eax, [edx+0ACh]
005CFFC0  mov     ecx, [ebp+var_4]
005CFFC3  mov     ecx, [ecx+0ACh]
005CFFC9  mov     edx, [eax]
005CFFCB  mov     eax, [edx+11Ch]
005CFFD1  call    eax
005CFFD3  mov     ecx, [ebp+var_4]
005CFFD6  cmp     dword ptr [ecx+0B0h], 0
005CFFDD  jz      short loc_5D0001
005CFFDF  mov     edx, [ebp+arg_0]
005CFFE2  push    edx
005CFFE3  mov     eax, [ebp+var_4]
005CFFE6  mov     ecx, [eax+0B0h]
005CFFEC  mov     edx, [ebp+var_4]
005CFFEF  mov     eax, [edx+0B0h]
005CFFF5  mov     edx, [ecx]
005CFFF7  mov     ecx, eax
005CFFF9  mov     eax, [edx+11Ch]
005CFFFF  call    eax
005D0001  mov     esp, ebp
005D0003  pop     ebp
005D0004  retn    4
