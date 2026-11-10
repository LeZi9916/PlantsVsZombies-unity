0046B530  push    esi
0046B531  mov     esi, ecx
0046B533  mov     eax, [esi+170h]
0046B539  mov     edx, [eax+868h]
0046B53F  push    edi
0046B540  mov     edi, [esp+8+arg_0]
0046B544  test    edx, edx
0046B546  jz      short loc_46B54F
0046B548  mov     ecx, edi
0046B54A  call    sub_426CC0
0046B54F  cmp     edi, 20h ; ' '
0046B552  jz      short loc_46B573
0046B554  cmp     edi, 0Dh
0046B557  jz      short loc_46B573
0046B559  cmp     edi, 1Bh
0046B55C  jnz     short loc_46B583
0046B55E  push    3E9h
0046B563  lea     ecx, [esi+0A0h]
0046B569  call    sub_5D1040
0046B56E  pop     edi
0046B56F  pop     esi
0046B570  retn    4
0046B573  push    3E8h
0046B578  lea     ecx, [esi+0A0h]
0046B57E  call    sub_5D1040
0046B583  pop     edi
0046B584  pop     esi
0046B585  retn    4
