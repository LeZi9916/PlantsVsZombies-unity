0046BB60  push    esi
0046BB61  push    edi
0046BB62  mov     edi, [esp+8+arg_0]
0046BB66  push    edi
0046BB67  mov     esi, ecx
0046BB69  call    sub_5D09C0
0046BB6E  mov     ecx, [esi+170h]
0046BB74  mov     eax, [esi]
0046BB76  mov     edx, [eax+0Ch]
0046BB79  push    ecx
0046BB7A  mov     ecx, esi
0046BB7C  call    edx
0046BB7E  mov     eax, [esi+178h]
0046BB84  test    eax, eax
0046BB86  jz      short loc_46BB92
0046BB88  mov     edx, [esi]
0046BB8A  push    eax
0046BB8B  mov     eax, [edx+0Ch]
0046BB8E  mov     ecx, esi
0046BB90  call    eax
0046BB92  mov     eax, [esi+17Ch]
0046BB98  test    eax, eax
0046BB9A  jz      short loc_46BBA6
0046BB9C  mov     edx, [esi]
0046BB9E  push    eax
0046BB9F  mov     eax, [edx+0Ch]
0046BBA2  mov     ecx, esi
0046BBA4  call    eax
0046BBA6  mov     edx, [esi]
0046BBA8  mov     eax, [esi+18Ch]
0046BBAE  mov     edx, [edx+0Ch]
0046BBB1  push    eax
0046BBB2  mov     ecx, esi
0046BBB4  call    edx
0046BBB6  mov     ecx, [esi+18Ch]
0046BBBC  mov     eax, [edi]
0046BBBE  mov     edx, [eax+20h]
0046BBC1  push    ecx
0046BBC2  mov     ecx, edi
0046BBC4  call    edx
0046BBC6  pop     edi
0046BBC7  pop     esi
0046BBC8  retn    4
