0046BE20  mov     edx, [ecx+14h]
0046BE23  mov     eax, [esp+arg_0]
0046BE27  cmp     eax, edx
0046BE29  ja      short loc_46BE3C
0046BE2B  mov     [esp+arg_4], 0FFFFFFFFh
0046BE33  mov     [esp+arg_0], eax
0046BE37  jmp     sub_40D5C0
0046BE3C  push    esi
0046BE3D  mov     esi, [esp+4+arg_4]
0046BE41  push    esi
0046BE42  sub     eax, edx
0046BE44  push    eax
0046BE45  call    sub_468350
0046BE4A  pop     esi
0046BE4B  retn    8
