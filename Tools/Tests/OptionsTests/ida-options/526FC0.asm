00526FC0  push    0FFFFFFFFh
00526FC2  push    offset SEH_526FC0
00526FC7  mov     eax, large fs:0
00526FCD  push    eax
00526FCE  mov     large fs:0, esp
00526FD5  sub     esp, 98h
00526FDB  mov     eax, [esp+0A4h+arg_4]
00526FE2  push    ebx
00526FE3  push    ebp
00526FE4  push    esi
00526FE5  push    edi
00526FE6  push    eax
00526FE7  lea     edi, [esp+0B8h+var_7C]
00526FEB  call    sub_52EA80
00526FF0  add     esp, 4
00526FF3  xor     eax, eax
00526FF5  mov     [esp+0B4h+var_4], eax
00526FFC  xor     ecx, ecx
00526FFE  push    4; int
00527000  mov     word ptr [esp+0B8h+var_98], cx
00527005  push    offset off_70C1D0; Source
0052700A  lea     ecx, [esp+0BCh+var_9C]
0052700E  mov     [esp+0BCh+var_84], 7
00527016  mov     [esp+0BCh+var_88], eax
0052701A  call    sub_40D1E0
0052701F  lea     edx, [esp+0B4h+var_80]
00527023  push    edx
00527024  mov     byte ptr [esp+0B8h+var_4], 1
0052702C  mov     ebx, 1
00527031  xor     eax, eax
00527033  mov     edx, edi
00527035  mov     [esp+0B8h+var_80], 26h ; '&'
0052703D  call    sub_43B0F0
00527042  mov     ebx, eax
00527044  mov     ebp, 8
00527049  cmp     ebx, 0FFFFFFFFh
0052704C  jz      loc_52719B
00527052  mov     ecx, [esp+0B4h+var_88]
00527056  mov     eax, edi
00527058  push    eax
00527059  mov     edx, ebx
0052705B  lea     esi, [esp+0B8h+var_60]
0052705F  call    sub_43B0A0
00527064  mov     ecx, eax
00527066  mov     byte ptr [esp+0B4h+var_4], 2
0052706E  mov     eax, [esp+0B4h+var_98]
00527072  cmp     [esp+0B4h+var_84], ebp
00527076  jnb     short loc_52707C
00527078  lea     eax, [esp+0B4h+var_98]
0052707C  mov     edi, [esp+0B4h+var_88]
00527080  push    eax
00527081  mov     eax, [ecx+14h]
00527084  call    sub_43B270
00527089  test    eax, eax
0052708B  setz    [esp+0B4h+var_9D]
00527090  mov     byte ptr [esp+0B4h+var_4], 1
00527098  cmp     [esp+0B4h+var_48], ebp
0052709C  jb      short loc_5270AB
0052709E  mov     ecx, [esp+0B4h+var_5C]
005270A2  push    ecx; void *
005270A3  call    ??3@YAXPAX@Z; operator delete(void *)
005270A8  add     esp, 4
005270AB  cmp     [esp+0B4h+var_9D], 0
005270B0  jz      loc_52719B
005270B6  lea     edx, [esp+0B4h+var_7C]
005270BA  push    edx
005270BB  mov     ecx, ebx
005270BD  xor     edx, edx
005270BF  lea     esi, [esp+0B8h+var_60]
005270C3  call    sub_43B0A0
005270C8  mov     byte ptr [esp+0B4h+var_4], 3
005270D0  mov     eax, [esp+0B4h+var_88]
005270D4  lea     ecx, [esp+0B4h+var_7C]
005270D8  push    ecx
005270D9  lea     edx, [eax+ebx]
005270DC  or      ecx, 0FFFFFFFFh
005270DF  lea     esi, [esp+0B8h+var_28]
005270E6  call    sub_43B0A0
005270EB  push    eax
005270EC  lea     ecx, [esp+0B8h+var_60]
005270F0  lea     esi, [esp+0B8h+var_44]
005270F4  mov     byte ptr [esp+0B8h+var_4], 4
005270FC  call    sub_429DF0
00527101  add     esp, 4
00527104  push    0FFFFFFFFh
00527106  push    0
00527108  push    eax
00527109  lea     ecx, [esp+0C0h+var_60]
0052710D  mov     byte ptr [esp+0C0h+var_4], 5
00527115  call    sub_40D0E0
0052711A  cmp     [esp+0B4h+var_2C], ebp
00527121  jb      short loc_527130
00527123  mov     edx, [esp+0B4h+var_40]
00527127  push    edx; void *
00527128  call    ??3@YAXPAX@Z; operator delete(void *)
0052712D  add     esp, 4
00527130  xor     eax, eax
00527132  mov     byte ptr [esp+0B4h+var_4], 3
0052713A  mov     [esp+0B4h+var_2C], 7
00527145  mov     [esp+0B4h+var_30], 0
00527150  mov     word ptr [esp+0B4h+var_40], ax
00527155  cmp     [esp+0B4h+var_10], ebp
0052715C  jb      short loc_52716E
0052715E  mov     ecx, [esp+0B4h+var_24]
00527165  push    ecx; void *
00527166  call    ??3@YAXPAX@Z; operator delete(void *)
0052716B  add     esp, 4
0052716E  push    0FFFFFFFFh
00527170  push    0
00527172  lea     edx, [esp+0BCh+var_60]
00527176  push    edx
00527177  lea     ecx, [esp+0C0h+var_7C]
0052717B  call    sub_40D0E0
00527180  mov     byte ptr [esp+0B4h+var_4], 1
00527188  cmp     [esp+0B4h+var_48], ebp
0052718C  jb      short loc_52719B
0052718E  mov     eax, [esp+0B4h+var_5C]
00527192  push    eax; void *
00527193  call    ??3@YAXPAX@Z; operator delete(void *)
00527198  add     esp, 4
0052719B  mov     ecx, [esp+0B4h+arg_14]
005271A2  mov     esi, [esp+0B4h+arg_8]
005271A9  mov     eax, esi
005271AB  cmp     ecx, 1
005271AE  jz      short loc_5271D1
005271B0  mov     edi, [esp+0B4h+arg_10]
005271B7  cmp     ecx, 2
005271BA  jnz     short loc_5271EF
005271BC  mov     edx, [edi]
005271BE  mov     edx, [edx+1Ch]
005271C1  lea     eax, [esp+0B4h+var_7C]
005271C5  push    eax
005271C6  mov     ecx, edi
005271C8  call    edx
005271CA  cdq
005271CB  sub     eax, edx
005271CD  sar     eax, 1
005271CF  jmp     short loc_5271EB
005271D1  mov     ecx, [esp+0B4h+arg_10]
005271D8  mov     eax, [ecx]
005271DA  mov     eax, [eax+1Ch]
005271DD  lea     edx, [esp+0B4h+var_7C]
005271E1  push    edx
005271E2  call    eax
005271E4  mov     edi, [esp+0B4h+arg_10]
005271EB  sub     esi, eax
005271ED  mov     eax, esi
005271EF  mov     ecx, [esp+0B4h+arg_0]
005271F6  mov     edx, [edi]
005271F8  lea     esi, [ecx+20h]
005271FB  push    esi
005271FC  lea     esi, [esp+0B8h+arg_18]
00527203  push    esi
00527204  lea     esi, [esp+0BCh+var_7C]
00527208  push    esi
00527209  mov     esi, [esp+0C0h+arg_C]
00527210  push    esi
00527211  push    eax
00527212  mov     eax, [edx+28h]
00527215  push    ecx
00527216  mov     ecx, edi
00527218  call    eax
0052721A  cmp     [esp+0B4h+var_84], ebp
0052721E  jb      short loc_52722D
00527220  mov     ecx, [esp+0B4h+var_98]
00527224  push    ecx; void *
00527225  call    ??3@YAXPAX@Z; operator delete(void *)
0052722A  add     esp, 4
0052722D  xor     edx, edx
0052722F  mov     [esp+0B4h+var_84], 7
00527237  mov     [esp+0B4h+var_88], 0
0052723F  mov     word ptr [esp+0B4h+var_98], dx
00527244  cmp     [esp+0B4h+var_64], ebp
00527248  jb      short loc_527257
0052724A  mov     eax, [esp+0B4h+var_78]
0052724E  push    eax; void *
0052724F  call    ??3@YAXPAX@Z; operator delete(void *)
00527254  add     esp, 4
00527257  mov     ecx, [esp+0B4h+var_C]
0052725E  pop     edi
0052725F  pop     esi
00527260  pop     ebp
00527261  mov     large fs:0, ecx
00527268  pop     ebx
00527269  add     esp, 0A4h
0052726F  retn
006DFB20  lea     ecx, [ebp-7Ch]; void *
006DFB23  jmp     sub_401330
006DFB28  lea     ecx, [ebp-9Ch]; void *
006DFB2E  jmp     sub_401330
006DFB33  lea     ecx, [ebp-60h]; void *
006DFB36  jmp     sub_401330
006DFB3B  lea     ecx, [ebp-60h]; void *
006DFB3E  jmp     sub_401330
006DFB43  lea     ecx, [ebp-28h]; void *
006DFB46  jmp     sub_401330
006DFB4B  lea     ecx, [ebp-44h]; void *
006DFB4E  jmp     sub_401330
006DFB53  mov     eax, offset stru_732058
006DFB58  jmp     ___CxxFrameHandler3
