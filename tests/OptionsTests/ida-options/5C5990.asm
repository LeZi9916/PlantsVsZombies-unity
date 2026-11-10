005C5990  push    ebp
005C5991  mov     ebp, esp
005C5993  sub     esp, 8
005C5996  mov     [ebp+var_4], ecx
005C5999  mov     eax, [ebp+var_4]
005C599C  mov     ecx, [ebp+arg_0]
005C599F  cmp     ecx, [eax+40h]
005C59A2  jl      short loc_5C59DA
005C59A4  mov     edx, [ebp+var_4]
005C59A7  mov     eax, [edx+40h]
005C59AA  mov     ecx, [ebp+var_4]
005C59AD  add     eax, [ecx+48h]
005C59B0  cmp     [ebp+arg_0], eax
005C59B3  jge     short loc_5C59DA
005C59B5  mov     edx, [ebp+var_4]
005C59B8  mov     eax, [ebp+arg_4]
005C59BB  cmp     eax, [edx+44h]
005C59BE  jl      short loc_5C59DA
005C59C0  mov     ecx, [ebp+var_4]
005C59C3  mov     edx, [ecx+44h]
005C59C6  mov     eax, [ebp+var_4]
005C59C9  add     edx, [eax+4Ch]
005C59CC  cmp     [ebp+arg_4], edx
005C59CF  jge     short loc_5C59DA
005C59D1  mov     [ebp+var_8], 1
005C59D8  jmp     short loc_5C59E1
005C59DA  mov     [ebp+var_8], 0
005C59E1  mov     al, byte ptr [ebp+var_8]
005C59E4  mov     esp, ebp
005C59E6  pop     ebp
005C59E7  retn    8
