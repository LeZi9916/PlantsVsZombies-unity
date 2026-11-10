0046B1D0  push    ebp
0046B1D1  mov     ebp, esp
0046B1D3  and     esp, 0FFFFFFC0h
0046B1D6  mov     eax, [ebp+arg_0]
0046B1D9  sub     esp, 3Ch
0046B1DC  sub     eax, 4
0046B1DF  push    esi
0046B1E0  mov     esi, ecx
0046B1E2  jz      short loc_46B239
0046B1E4  sub     eax, 1
0046B1E7  jnz     short loc_46B25D
0046B1E9  mov     ecx, [esi+8]
0046B1EC  fld     [ebp+arg_4]
0046B1EF  mov     eax, [ecx]
0046B1F1  fmul    ds:dbl_727360
0046B1F7  mov     edx, [eax+0F4h]
0046B1FD  sub     esp, 8
0046B200  fstp    [esp+48h+var_48]
0046B203  call    edx
0046B205  mov     eax, [esi+8]
0046B208  mov     eax, [eax+888h]
0046B20E  call    sub_52AA10
0046B213  mov     ecx, [esi+10h]
0046B216  cmp     byte ptr [ecx+0BCh], 0
0046B21D  jnz     short loc_46B25D
0046B21F  mov     ecx, [esi+8]
0046B222  mov     edx, [ecx]
0046B224  mov     eax, dword_753038
0046B229  mov     edx, [edx+0D8h]
0046B22F  push    eax
0046B230  call    edx
0046B232  pop     esi
0046B233  mov     esp, ebp
0046B235  pop     ebp
0046B236  retn    0Ch
0046B239  mov     ecx, [esi+8]
0046B23C  fld     [ebp+arg_4]
0046B23F  mov     eax, [ecx]
0046B241  mov     edx, [eax+0F0h]
0046B247  sub     esp, 8
0046B24A  fstp    [esp+48h+var_48]
0046B24D  call    edx
0046B24F  mov     eax, [esi+8]
0046B252  mov     eax, [eax+888h]
0046B258  call    sub_52AA10
0046B25D  pop     esi
0046B25E  mov     esp, ebp
0046B260  pop     ebp
0046B261  retn    0Ch
