005D1150  push    ebp
005D1151  mov     ebp, esp
005D1153  push    ecx
005D1154  mov     [ebp+var_4], ecx
005D1157  mov     ecx, [ebp+var_4]
005D115A  call    sub_452710
005D115F  mov     eax, [ebp+arg_0]
005D1162  and     eax, 1
005D1165  jz      short loc_5D1173
005D1167  mov     ecx, [ebp+var_4]
005D116A  push    ecx; void *
005D116B  call    ??3@YAXPAX@Z; operator delete(void *)
005D1170  add     esp, 4
005D1173  mov     eax, [ebp+var_4]
005D1176  mov     esp, ebp
005D1178  pop     ebp
005D1179  retn    4
