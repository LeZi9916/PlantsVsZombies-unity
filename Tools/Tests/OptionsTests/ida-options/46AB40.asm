0046AB40  push    0FFFFFFFFh
0046AB42  push    offset SEH_464AB0
0046AB47  mov     eax, large fs:0
0046AB4D  push    eax
0046AB4E  mov     large fs:0, esp
0046AB55  push    ecx
0046AB56  push    esi
0046AB57  mov     esi, ecx
0046AB59  mov     [esp+14h+var_10], esi
0046AB5D  mov     dword ptr [esi], offset off_711630
0046AB63  mov     dword ptr [esi+0A0h], offset off_711760
0046AB6D  mov     dword ptr [esi+168h], offset off_71177C
0046AB77  mov     dword ptr [esi+16Ch], offset off_711780
0046AB81  mov     [esp+14h+var_4], 0
0046AB89  mov     ecx, [esi+174h]
0046AB8F  test    ecx, ecx
0046AB91  jz      short loc_46AB9B
0046AB93  mov     eax, [ecx]
0046AB95  mov     edx, [eax]
0046AB97  push    1
0046AB99  call    edx
0046AB9B  mov     ecx, [esi+178h]
0046ABA1  test    ecx, ecx
0046ABA3  jz      short loc_46ABAD
0046ABA5  mov     eax, [ecx]
0046ABA7  mov     edx, [eax]
0046ABA9  push    1
0046ABAB  call    edx
0046ABAD  mov     ecx, [esi+17Ch]
0046ABB3  test    ecx, ecx
0046ABB5  jz      short loc_46ABBF
0046ABB7  mov     eax, [ecx]
0046ABB9  mov     edx, [eax]
0046ABBB  push    1
0046ABBD  call    edx
0046ABBF  mov     ecx, [esi+180h]
0046ABC5  test    ecx, ecx
0046ABC7  jz      short loc_46ABD1
0046ABC9  mov     eax, [ecx]
0046ABCB  mov     edx, [eax]
0046ABCD  push    1
0046ABCF  call    edx
0046ABD1  mov     ecx, [esi+184h]
0046ABD7  test    ecx, ecx
0046ABD9  jz      short loc_46ABE3
0046ABDB  mov     eax, [ecx]
0046ABDD  mov     edx, [eax]
0046ABDF  push    1
0046ABE1  call    edx
0046ABE3  mov     ecx, [esi+18Ch]
0046ABE9  test    ecx, ecx
0046ABEB  jz      short loc_46ABF5
0046ABED  mov     eax, [ecx]
0046ABEF  mov     edx, [eax]
0046ABF1  push    1
0046ABF3  call    edx
0046ABF5  mov     ecx, [esi+188h]
0046ABFB  test    ecx, ecx
0046ABFD  jz      short loc_46AC07
0046ABFF  mov     eax, [ecx]
0046AC01  mov     edx, [eax]
0046AC03  push    1
0046AC05  call    edx
0046AC07  mov     ecx, [esi+190h]
0046AC0D  test    ecx, ecx
0046AC0F  jz      short loc_46AC19
0046AC11  mov     eax, [ecx]
0046AC13  mov     edx, [eax]
0046AC15  push    1
0046AC17  call    edx
0046AC19  mov     ecx, esi
0046AC1B  mov     [esp+14h+var_4], 0FFFFFFFFh
0046AC23  call    sub_5CFD40
0046AC28  mov     ecx, [esp+14h+var_C]
0046AC2C  pop     esi
0046AC2D  mov     large fs:0, ecx
0046AC34  add     esp, 10h
0046AC37  retn
006DDD80  mov     ecx, [ebp-10h]
006DDD83  jmp     sub_5CFD40
006DDD88  mov     eax, offset stru_72FD8C
006DDD8D  jmp     ___CxxFrameHandler3
