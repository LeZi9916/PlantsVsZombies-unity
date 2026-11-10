005D17A0  push    ebp
005D17A1  mov     ebp, esp
005D17A3  sub     esp, 14h
005D17A6  mov     [ebp+var_14], ecx
005D17A9  mov     ecx, [ebp+var_14]
005D17AC  add     ecx, 1Ch; void *
005D17AF  call    sub_401E90
005D17B4  mov     ecx, [ebp+var_14]; void *
005D17B7  call    sub_401E90
005D17BC  mov     esp, ebp
005D17BE  pop     ebp
005D17BF  retn
