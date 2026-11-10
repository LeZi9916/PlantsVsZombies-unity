005D0CF0  push    ebp
005D0CF1  mov     ebp, esp
005D0CF3  push    ecx
005D0CF4  mov     [ebp+var_4], ecx
005D0CF7  cmp     [ebp+arg_C], 1
005D0CFB  jnz     short loc_5D0D2F
005D0CFD  push    2
005D0CFF  mov     eax, [ebp+var_4]
005D0D02  mov     ecx, [eax+20h]
005D0D05  mov     ecx, [ecx+6Ch]
005D0D08  call    sub_62C810
005D0D0D  mov     edx, [ebp+var_4]
005D0D10  mov     byte ptr [edx+148h], 1
005D0D17  mov     eax, [ebp+var_4]
005D0D1A  mov     ecx, [ebp+arg_0]
005D0D1D  mov     [eax+14Ch], ecx
005D0D23  mov     edx, [ebp+var_4]
005D0D26  mov     eax, [ebp+arg_4]
005D0D29  mov     [edx+150h], eax
005D0D2F  mov     ecx, [ebp+arg_C]
005D0D32  push    ecx
005D0D33  mov     edx, [ebp+arg_8]
005D0D36  push    edx
005D0D37  mov     eax, [ebp+arg_4]
005D0D3A  push    eax
005D0D3B  mov     ecx, [ebp+arg_0]
005D0D3E  push    ecx
005D0D3F  mov     ecx, [ebp+var_4]
005D0D42  call    sub_5C54C0
005D0D47  mov     esp, ebp
005D0D49  pop     ebp
005D0D4A  retn    10h
