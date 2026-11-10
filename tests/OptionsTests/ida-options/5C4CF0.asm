005C4CF0  push    ebp
005C4CF1  mov     ebp, esp
005C4CF3  push    ecx
005C4CF4  mov     [ebp+var_4], ecx
005C4CF7  mov     eax, [ebp+var_4]
005C4CFA  movzx   ecx, byte ptr [eax+64h]
005C4CFE  movzx   edx, [ebp+arg_0]
005C4D02  cmp     ecx, edx
005C4D04  jnz     short loc_5C4D08
005C4D06  jmp     short loc_5C4D4C
005C4D08  mov     eax, [ebp+var_4]
005C4D0B  mov     cl, [ebp+arg_0]
005C4D0E  mov     [eax+64h], cl
005C4D11  mov     edx, [ebp+var_4]
005C4D14  movzx   eax, byte ptr [edx+64h]
005C4D18  test    eax, eax
005C4D1A  jz      short loc_5C4D2B
005C4D1C  mov     ecx, [ebp+var_4]
005C4D1F  mov     edx, [ecx]
005C4D21  mov     ecx, [ebp+var_4]
005C4D24  mov     eax, [edx+44h]
005C4D27  call    eax
005C4D29  jmp     short loc_5C4D38
005C4D2B  mov     ecx, [ebp+var_4]
005C4D2E  mov     edx, [ecx]
005C4D30  mov     ecx, [ebp+var_4]
005C4D33  mov     eax, [edx+4Ch]
005C4D36  call    eax
005C4D38  mov     ecx, [ebp+var_4]
005C4D3B  cmp     dword ptr [ecx+20h], 0
005C4D3F  jz      short loc_5C4D4C
005C4D41  mov     edx, [ebp+var_4]
005C4D44  mov     ecx, [edx+20h]
005C4D47  call    sub_5C41E0
005C4D4C  mov     esp, ebp
005C4D4E  pop     ebp
005C4D4F  retn    4
