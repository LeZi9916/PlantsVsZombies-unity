0046BE50  push    esi
0046BE51  push    edi
0046BE52  mov     edi, [esp+8+arg_0]
0046BE56  mov     esi, ecx
0046BE58  cmp     edi, [esi+14h]
0046BE5B  jbe     short loc_46BE62
0046BE5D  call    __invalid_parameter_noinfo
0046BE62  cmp     dword ptr [esi+18h], 8
0046BE66  jb      short loc_46BE73
0046BE68  mov     esi, [esi+4]
0046BE6B  lea     eax, [esi+edi*2]
0046BE6E  pop     edi
0046BE6F  pop     esi
0046BE70  retn    4
0046BE73  lea     eax, [esi+edi*2+4]
0046BE77  pop     edi
0046BE78  pop     esi
0046BE79  retn    4
