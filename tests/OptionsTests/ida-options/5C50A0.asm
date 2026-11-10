005C50A0  push    ebp
005C50A1  mov     ebp, esp
005C50A3  push    ecx
005C50A4  push    esi
005C50A5  push    edi
005C50A6  mov     [ebp+var_4], ecx
005C50A9  mov     ecx, [ebp+var_4]
005C50AC  add     ecx, 6Ch ; 'l'
005C50AF  call    sub_43B1D0
005C50B4  cmp     [ebp+arg_4], eax
005C50B7  jge     short loc_5C50E5
005C50B9  mov     esi, [ebp+var_4]
005C50BC  add     esi, 6Ch ; 'l'
005C50BF  mov     edi, [ebp+arg_4]
005C50C2  call    sub_43B0D0
005C50C7  mov     ecx, [ebp+arg_0]
005C50CA  mov     edx, [eax]
005C50CC  mov     [ecx], edx
005C50CE  mov     edx, [eax+4]
005C50D1  mov     [ecx+4], edx
005C50D4  mov     edx, [eax+8]
005C50D7  mov     [ecx+8], edx
005C50DA  mov     eax, [eax+0Ch]
005C50DD  mov     [ecx+0Ch], eax
005C50E0  mov     eax, [ebp+arg_0]
005C50E3  jmp     short loc_5C5104
005C50E5  mov     ecx, [ebp+arg_8]
005C50E8  mov     edx, [ebp+arg_0]
005C50EB  mov     eax, [ecx]
005C50ED  mov     [edx], eax
005C50EF  mov     eax, [ecx+4]
005C50F2  mov     [edx+4], eax
005C50F5  mov     eax, [ecx+8]
005C50F8  mov     [edx+8], eax
005C50FB  mov     ecx, [ecx+0Ch]
005C50FE  mov     [edx+0Ch], ecx
005C5101  mov     eax, [ebp+arg_0]
005C5104  pop     edi
005C5105  pop     esi
005C5106  mov     esp, ebp
005C5108  pop     ebp
005C5109  retn    0Ch
