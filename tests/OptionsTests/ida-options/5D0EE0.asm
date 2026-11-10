005D0EE0  push    ebp
005D0EE1  mov     ebp, esp
005D0EE3  push    ecx
005D0EE4  mov     [ebp+var_4], ecx
005D0EE7  mov     eax, [ebp+var_4]
005D0EEA  movzx   ecx, byte ptr [eax+148h]
005D0EF1  test    ecx, ecx
005D0EF3  jz      short loc_5D0F0F
005D0EF5  push    0
005D0EF7  mov     edx, [ebp+var_4]
005D0EFA  mov     eax, [edx+20h]
005D0EFD  mov     ecx, [eax+6Ch]
005D0F00  call    sub_62C810
005D0F05  mov     ecx, [ebp+var_4]
005D0F08  mov     byte ptr [ecx+148h], 0
005D0F0F  mov     edx, [ebp+arg_C]
005D0F12  push    edx
005D0F13  mov     eax, [ebp+arg_8]
005D0F16  push    eax
005D0F17  mov     ecx, [ebp+arg_4]
005D0F1A  push    ecx
005D0F1B  mov     edx, [ebp+arg_0]
005D0F1E  push    edx
005D0F1F  mov     ecx, [ebp+var_4]
005D0F22  call    sub_5C54C0
005D0F27  mov     esp, ebp
005D0F29  pop     ebp
005D0F2A  retn    10h
