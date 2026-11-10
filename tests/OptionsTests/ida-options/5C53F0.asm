005C53F0  push    ebp
005C53F1  mov     ebp, esp
005C53F3  push    ecx
005C53F4  mov     [ebp+var_4], ecx
005C53F7  mov     eax, [ebp+var_4]
005C53FA  cmp     dword ptr [eax+20h], 0
005C53FE  jnz     short loc_5C5402
005C5400  jmp     short loc_5C542C
005C5402  movzx   ecx, [ebp+arg_0]
005C5406  test    ecx, ecx
005C5408  jz      short loc_5C541C
005C540A  push    1
005C540C  mov     edx, [ebp+var_4]
005C540F  mov     eax, [edx+20h]
005C5412  mov     ecx, [eax+6Ch]
005C5415  call    sub_62C810
005C541A  jmp     short loc_5C542C
005C541C  push    0
005C541E  mov     ecx, [ebp+var_4]
005C5421  mov     edx, [ecx+20h]
005C5424  mov     ecx, [edx+6Ch]
005C5427  call    sub_62C810
005C542C  mov     esp, ebp
005C542E  pop     ebp
005C542F  retn    4
