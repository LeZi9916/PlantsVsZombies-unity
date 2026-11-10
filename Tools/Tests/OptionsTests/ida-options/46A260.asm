0046A260  push    48h ; 'H'; Size
0046A262  call    ??2@YAPAXI@Z; operator new(uint)
0046A267  xor     edx, edx
0046A269  add     esp, 4
0046A26C  cmp     eax, edx
0046A26E  jz      short locret_46A29C
0046A270  mov     ecx, [esp+arg_0]
0046A274  mov     [eax], ecx
0046A276  mov     ecx, [esp+arg_4]
0046A27A  push    esi
0046A27B  mov     esi, [esp+4+arg_C]
0046A27F  mov     [eax+4], ecx
0046A282  mov     ecx, [esp+4+arg_8]
0046A286  push    edi
0046A287  mov     [eax+8], ecx
0046A28A  lea     edi, [eax+10h]
0046A28D  mov     ecx, 0Ch
0046A292  rep movsd
0046A294  pop     edi
0046A295  mov     [eax+40h], dl
0046A298  mov     [eax+41h], dl
0046A29B  pop     esi
0046A29C  retn    10h
