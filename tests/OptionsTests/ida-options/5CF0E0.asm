005CF0E0  push    ebp
005CF0E1  mov     ebp, esp
005CF0E3  push    ecx
005CF0E4  mov     [ebp+var_4], ecx
005CF0E7  movzx   eax, [ebp+arg_0]
005CF0EB  push    eax
005CF0EC  mov     ecx, [ebp+var_4]
005CF0EF  call    sub_5C5240
005CF0F4  mov     ecx, [ebp+var_4]
005CF0F7  add     ecx, 108h
005CF0FD  push    ecx
005CF0FE  mov     edx, [ebp+var_4]
005CF101  mov     eax, [edx+0D4h]
005CF107  push    eax
005CF108  mov     ecx, [ebp+var_4]
005CF10B  call    sub_5CE720
005CF110  movzx   ecx, al
005CF113  test    ecx, ecx
005CF115  jz      short loc_5CF124
005CF117  mov     edx, [ebp+var_4]
005CF11A  mov     eax, [edx]
005CF11C  mov     ecx, [ebp+var_4]
005CF11F  mov     edx, [eax+44h]
005CF122  call    edx
005CF124  mov     esp, ebp
005CF126  pop     ebp
005CF127  retn    4
