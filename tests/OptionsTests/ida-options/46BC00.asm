0046BC00  mov     eax, [esp+arg_C]
0046BC04  mov     edx, [esp+arg_4]
0046BC08  push    esi
0046BC09  push    edi
0046BC0A  push    eax
0046BC0B  mov     eax, [esp+0Ch+arg_0]
0046BC0F  mov     esi, ecx
0046BC11  mov     ecx, [esp+0Ch+arg_8]
0046BC15  push    ecx
0046BC16  push    edx
0046BC17  push    eax
0046BC18  mov     ecx, esi
0046BC1A  call    sub_464FF0
0046BC1F  mov     edi, [esi+48h]
0046BC22  sub     edi, [esi+13Ch]
0046BC28  mov     eax, [esi+134h]
0046BC2E  mov     ecx, [esi+18Ch]
0046BC34  mov     esi, [esi+4Ch]
0046BC37  mov     edx, [ecx]
0046BC39  sub     edi, eax
0046BC3B  push    1Ch
0046BC3D  sub     edi, 18h
0046BC40  push    edi
0046BC41  sub     esi, 9Bh
0046BC47  add     eax, 0Ch
0046BC4A  push    esi
0046BC4B  push    eax
0046BC4C  mov     eax, [edx+0A0h]
0046BC52  call    eax
0046BC54  pop     edi
0046BC55  pop     esi
0046BC56  retn    10h
