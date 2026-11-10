004532B0  cmp     byte ptr [ecx+119h], 0
004532B7  jnz     short locret_453303
004532B9  cmp     byte ptr [ecx+68h], 0
004532BD  jz      short loc_4532CF
004532BF  cmp     byte ptr [ecx+69h], 0
004532C3  jz      short loc_4532CF
004532C5  cmp     byte ptr [ecx+66h], 0
004532C9  jnz     short loc_4532CF
004532CB  mov     al, 1
004532CD  jmp     short loc_4532D1
004532CF  xor     al, al
004532D1  push    edi
004532D2  mov     edi, [esp+4+arg_0]
004532D6  lea     edx, [ecx+0A4h]
004532DC  push    edx; int
004532DD  movzx   edx, byte ptr [ecx+69h]
004532E1  push    edx; char
004532E2  mov     dl, [ecx+118h]
004532E8  xor     dl, al
004532EA  movzx   eax, dl
004532ED  mov     edx, [ecx+4Ch]
004532F0  push    eax; int
004532F1  mov     eax, [ecx+48h]
004532F4  push    edx; int
004532F5  push    eax; int
004532F6  xor     eax, eax
004532F8  xor     ecx, ecx; int
004532FA  call    sub_452720
004532FF  add     esp, 14h
00453302  pop     edi
00453303  retn    4
