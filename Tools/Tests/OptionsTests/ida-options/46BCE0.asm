0046BCE0  push    esi
0046BCE1  mov     esi, dword ptr [esp+4+C]
0046BCE5  push    esi; C
0046BCE6  call    _iswalnum
0046BCEB  add     esp, 4
0046BCEE  test    eax, eax
0046BCF0  jnz     short loc_46BCFE
0046BCF2  cmp     si, 20h ; ' '
0046BCF6  jz      short loc_46BCFE
0046BCF8  xor     al, al
0046BCFA  pop     esi
0046BCFB  retn    8
0046BCFE  mov     al, 1
0046BD00  pop     esi
0046BD01  retn    8
