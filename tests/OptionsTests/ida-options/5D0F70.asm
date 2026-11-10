005D0F70  push    ebp
005D0F71  mov     ebp, esp
005D0F73  push    ecx
005D0F74  mov     [ebp+var_4], ecx
005D0F77  push    0
005D0F79  mov     eax, dword_755F28
005D0F7E  mov     edx, [eax]
005D0F80  mov     ecx, dword_755F28
005D0F86  mov     eax, [edx+17Ch]
005D0F8C  call    eax
005D0F8E  movzx   ecx, al
005D0F91  test    ecx, ecx
005D0F93  jz      short loc_5D0FAF
005D0F95  mov     edx, [ebp+var_4]
005D0F98  cmp     dword ptr [edx+20h], 0
005D0F9C  jz      short loc_5D0FAF
005D0F9E  mov     eax, [ebp+var_4]
005D0FA1  cmp     dword ptr [eax+15Ch], 7FFFFFFFh
005D0FAB  jnz     short loc_5D0FAF
005D0FAD  jmp     short loc_5D0F77
005D0FAF  movzx   ecx, [ebp+arg_0]
005D0FB3  test    ecx, ecx
005D0FB5  jz      short loc_5D0FD7
005D0FB7  mov     edx, [ebp+var_4]
005D0FBA  mov     eax, [edx+154h]
005D0FC0  push    eax
005D0FC1  mov     ecx, dword_755F28
005D0FC7  mov     edx, [ecx]
005D0FC9  mov     ecx, dword_755F28
005D0FCF  mov     eax, [edx+134h]
005D0FD5  call    eax
005D0FD7  mov     ecx, [ebp+var_4]
005D0FDA  mov     eax, [ecx+15Ch]
005D0FE0  mov     esp, ebp
005D0FE2  pop     ebp
005D0FE3  retn    4
