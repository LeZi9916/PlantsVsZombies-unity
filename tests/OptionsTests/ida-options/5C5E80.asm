005C5E80  push    ebp
005C5E81  mov     ebp, esp
005C5E83  push    ecx
005C5E84  mov     [ebp+var_4], ecx
005C5E87  mov     ecx, [ebp+var_4]
005C5E8A  call    sub_5C4A90
005C5E8F  mov     eax, [ebp+arg_0]
005C5E92  and     eax, 1
005C5E95  jz      short loc_5C5EA3
005C5E97  mov     ecx, [ebp+var_4]
005C5E9A  push    ecx; void *
005C5E9B  call    ??3@YAXPAX@Z; operator delete(void *)
005C5EA0  add     esp, 4
005C5EA3  mov     eax, [ebp+var_4]
005C5EA6  mov     esp, ebp
005C5EA8  pop     ebp
005C5EA9  retn    4
