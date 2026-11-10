00454F30 mov     eax, [esi+0A4h]
00454F36 mov     ecx, [eax+950h]
00454F3C sub     esp, 20h
00454F3F push    ebx
00454F40 push    ebp
00454F41 push    edi
00454F42 test    ecx, ecx
00454F44 jz      short loc_454F52
00454F46 cmp     dword ptr [ecx+54h], 0
00454F4A jg      short loc_454F60
00454F4C cmp     dword ptr [ecx+4Ch], 0Fh
00454F50 jge     short loc_454F60
00454F52 cmp     byte ptr [esi+159h], 0
00454F59 mov     [esp+2Ch+var_1A], 0
00454F5E jz      short loc_454F65
00454F60 mov     [esp+2Ch+var_1A], 1
00454F65 test    ecx, ecx
00454F67 jz      short loc_454F7E
00454F69 cmp     dword ptr [ecx+54h], 0
00454F6D jg      short loc_454F8B
00454F6F cmp     dword ptr [ecx+374h], 0
00454F76 jnz     short loc_454F8B
00454F78 cmp     dword ptr [ecx+4Ch], 19h
00454F7C jge     short loc_454F8B
00454F7E cmp     byte ptr [esi+159h], 0
00454F85 jnz     short loc_454F8B
00454F87 xor     bl, bl
00454F89 jmp     short loc_454F8D
00454F8B mov     bl, 1
00454F8D call    sub_4614B0
00454F92 test    al, al
00454F94 jnz     short loc_454FA2
00454F96 mov     [esp+2Ch+var_19], al
00454F9A cmp     [esi+159h], al
00454FA0 jz      short loc_454FA7
00454FA2 mov     [esp+2Ch+var_19], 1
00454FA7 mov     edx, [esi+0D4h]
00454FAD mov     cl, [esp+2Ch+var_1A]
00454FB1 test    cl, cl
00454FB3 setz    al
00454FB6 mov     [edx+66h], al
00454FB9 mov     eax, [esi+0D4h]
00454FBF mov     [eax+64h], cl
00454FC2 mov     eax, [esi+0D0h]
00454FC8 test    bl, bl
00454FCA setz    dl
00454FCD mov     [eax+66h], dl
00454FD0 mov     edx, [esi+0D0h]
00454FD6 mov     [edx+64h], bl
00454FD9 mov     eax, [esi+0C4h]
00454FDF mov     byte ptr [eax+66h], 0
00454FE3 mov     edx, [esi+0C4h]
00454FE9 mov     byte ptr [edx+64h], 1
00454FED mov     eax, [esi+0A4h]
00454FF3 mov     edx, [eax+944h]
00454FF9 mov     eax, [esi+0FCh]
00454FFF mov     edx, [edx+8]
00455002 and     eax, 0FFFFh
00455007 lea     edi, [eax+eax*4]
0045500A shl     edi, 5
0045500D add     edi, [edx]
0045500F test    cl, cl
00455011 jz      short loc_45505D
00455013 push    0; int
00455015 push    offset aAlmanacKeyShad; "almanac_key_shadow"
0045501A push    edi; int
0045501B call    sub_482F10
00455020 test    bl, bl
00455022 push    offset aAlmanacKeyShad; "almanac_key_shadow"
00455027 mov     ebx, edi
00455029 jz      short loc_455043
0045502B call    sub_4823E0
00455030 mov     ecx, [edi+58h]
00455033 lea     eax, [eax+eax*2]
00455036 shl     eax, 5
00455039 mov     dword ptr [eax+ecx+44h], 0
00455041 jmp     short loc_45509C
00455043 mov     ebp, dword_7536BC
00455049 call    sub_4823E0
0045504E lea     edx, [eax+eax*2]
00455051 mov     eax, [edi+58h]
00455054 shl     edx, 5
00455057 mov     [edx+eax+44h], ebp
0045505B jmp     short loc_45509C
0045505D test    bl, bl
0045505F jz      short loc_45508F
00455061 push    0; int
00455063 push    offset aAlmanacKeyShad; "almanac_key_shadow"
00455068 push    edi; int
00455069 call    sub_482F10
0045506E mov     ebp, dword_752DE8
00455074 push    offset aAlmanacKeyShad; "almanac_key_shadow"
00455079 mov     ebx, edi
0045507B call    sub_4823E0
00455080 mov     edx, [edi+58h]
00455083 lea     ecx, [eax+eax*2]
00455086 shl     ecx, 5
00455089 mov     [ecx+edx+44h], ebp
0045508D jmp     short loc_45509C
0045508F push    0FFFFFFFFh; int
00455091 push    offset aAlmanacKeyShad; "almanac_key_shadow"
00455096 push    edi; int
00455097 call    sub_482F10
0045509C mov     al, [esp+2Ch+var_19]
004550A0 mov     edx, [esi+0D8h]
004550A6 test    al, al
004550A8 setz    cl
004550AB mov     [edx+66h], cl
004550AE mov     ecx, [esi+0D8h]
004550B4 mov     [ecx+64h], al
004550B7 cmp     byte ptr [esi+0F0h], 0
004550BE jz      short loc_455116
004550C0 mov     edx, [esi+0B0h]
004550C6 mov     eax, dword_7531D4
004550CB mov     [edx+0CCh], eax
004550D1 mov     ecx, [esi+0B0h]
004550D7 mov     edx, dword_7531D4
004550DD push    80h
004550E2 mov     [ecx+0D0h], edx
004550E8 mov     eax, [esi+0B0h]
004550EE mov     ebx, [eax]
004550F0 push    80h
004550F5 push    80h
004550FA lea     ecx, [esp+38h+var_14]
004550FE call    sub_55BBB0
00455103 mov     ecx, [esi+0B0h]
00455109 mov     edx, [ebx+88h]
0045510F push    eax
00455110 push    5
00455112 call    edx
00455114 jmp     short loc_455150
00455116 mov     eax, [esi+0B0h]
0045511C mov     ecx, dword_7531D4
00455122 mov     [eax+0CCh], ecx
00455128 mov     edx, [esi+0B0h]
0045512E mov     eax, dword_7531D4
00455133 mov     [edx+0D0h], eax
00455139 mov     ecx, [esi+0B0h]
0045513F mov     edx, [ecx]
00455141 mov     eax, [edx+88h]
00455147 push    offset dword_7DCA98
0045514C push    5
0045514E call    eax
00455150 mov     ecx, [esi+0B4h]
00455156 mov     edx, dword_752B28
0045515C cmp     byte ptr [esi+0F1h], 0
00455163 mov     [ecx+0CCh], edx
00455169 mov     eax, [esi+0B4h]
0045516F mov     ecx, dword_752B28
00455175 mov     [eax+0D0h], ecx
0045517B jz      short loc_4551AC
0045517D mov     edx, [esi+0B4h]
00455183 mov     ebx, [edx]
00455185 push    80h
0045518A push    80h
0045518F push    80h
00455194 lea     ecx, [esp+38h+var_14]
00455198 call    sub_55BBB0
0045519D mov     ecx, [esi+0B4h]
004551A3 push    eax
004551A4 mov     eax, [ebx+88h]
004551AA jmp     short loc_4551BF
004551AC mov     ecx, [esi+0B4h]
004551B2 mov     edx, [ecx]
004551B4 mov     eax, [edx+88h]
004551BA push    offset dword_7DCA98
004551BF push    5
004551C1 call    eax
004551C3 mov     ecx, [esi+0DCh]
004551C9 mov     edx, dword_752CDC
004551CF cmp     byte ptr [esi+0F2h], 0
004551D6 mov     [ecx+0CCh], edx
004551DC mov     eax, [esi+0DCh]
004551E2 mov     ecx, dword_752CDC
004551E8 mov     [eax+0D0h], ecx
004551EE jz      short loc_45521F
004551F0 mov     edx, [esi+0DCh]
004551F6 mov     ebx, [edx]
004551F8 push    80h
004551FD push    80h
00455202 push    80h
00455207 lea     ecx, [esp+38h+var_14]
0045520B call    sub_55BBB0
00455210 mov     ecx, [esi+0DCh]
00455216 push    eax
00455217 mov     eax, [ebx+88h]
0045521D jmp     short loc_455232
0045521F mov     ecx, [esi+0DCh]
00455225 mov     edx, [ecx]
00455227 mov     eax, [edx+88h]
0045522D push    offset dword_7DCA98
00455232 push    5
00455234 call    eax
00455236 cmp     byte ptr [esi+0F4h], 0
0045523D mov     ecx, [esi+0A8h]
00455243 jnz     short loc_455259
00455245 mov     edx, dword_752BB0
0045524B mov     [ecx+0C8h], edx
00455251 mov     ecx, dword_752F20
00455257 jmp     short loc_45526B
00455259 mov     edx, dword_752AB4
0045525F mov     [ecx+0C8h], edx
00455265 mov     ecx, dword_752AB4
0045526B mov     eax, [esi+0A8h]
00455271 mov     [eax+0CCh], ecx
00455277 mov     edx, [esi+0A8h]
0045527D mov     eax, dword_752AB4
00455282 push    offset aSelectorscreen_3; "SelectorScreen_Survival_button"
00455287 mov     ebx, edi
00455289 mov     [edx+0D0h], eax
0045528F call    sub_4823E0
00455294 lea     ebp, [eax+eax*2]
00455297 shl     ebp, 5
0045529A add     ebp, [edi+58h]
0045529D push    offset aSelectorscreen_4; "SelectorScreen_Challenges_button"
004552A2 call    sub_4823E0
004552A7 lea     eax, [eax+eax*2]
004552AA shl     eax, 5
004552AD add     eax, [edi+58h]
004552B0 push    offset aSelectorscreen_5; "SelectorScreen_ZenGarden_button"
004552B5 mov     [esp+30h+var_18], eax
004552B9 call    sub_4823E0
004552BE mov     ecx, [esi+0B0h]
004552C4 mov     edx, [ecx]
004552C6 lea     ebx, [eax+eax*2]
004552C9 mov     eax, [edx+90h]
004552CF shl     ebx, 5
004552D2 add     ebx, [edi+58h]
004552D5 push    5
004552D7 call    eax
004552D9 mov     ecx, [eax]
004552DB mov     [ebp+4Ch], ecx
004552DE mov     edx, [eax+4]
004552E1 mov     [ebp+50h], edx
004552E4 mov     ecx, [eax+8]
004552E7 mov     [ebp+54h], ecx
004552EA mov     edx, [eax+0Ch]
004552ED mov     [ebp+58h], edx
004552F0 mov     ecx, [esi+0B4h]
004552F6 mov     eax, [ecx]
004552F8 mov     edx, [eax+90h]
004552FE push    5
00455300 call    edx
00455302 mov     edx, [eax]
00455304 mov     ecx, [esp+2Ch+var_18]
00455308 mov     [ecx+4Ch], edx
0045530B mov     edx, [eax+4]
0045530E mov     [ecx+50h], edx
00455311 mov     edx, [eax+8]
00455314 mov     [ecx+54h], edx
00455317 mov     eax, [eax+0Ch]
0045531A mov     [ecx+58h], eax
0045531D mov     ecx, [esi+0DCh]
00455323 mov     edx, [ecx]
00455325 mov     eax, [edx+90h]
0045532B push    5
0045532D call    eax
0045532F mov     ecx, [eax]
00455331 mov     [ebx+4Ch], ecx
00455334 mov     edx, [eax+4]
00455337 mov     [ebx+50h], edx
0045533A mov     ecx, [eax+8]
0045533D mov     [ebx+54h], ecx
00455340 mov     edx, [eax+0Ch]
00455343 mov     [ebx+58h], edx
00455346 cmp     byte ptr [esi+0F3h], 0
0045534D push    0FFFFFFFFh; int
0045534F jz      short loc_455398
00455351 push    offset aSelectorscreen_7; "SelectorScreen_Adventure_"
00455356 push    edi; int
00455357 call    sub_482F10
0045535C mov     eax, [esi+0ACh]
00455362 mov     ecx, dword_753820
00455368 mov     [eax+0C8h], ecx
0045536E mov     edx, [esi+0ACh]
00455374 mov     eax, dword_752EB0
00455379 mov     [edx+0CCh], eax
0045537F mov     edx, dword_752EB0
00455385 mov     ecx, [esi+0ACh]
0045538B mov     [ecx+0D0h], edx
00455391 pop     edi
00455392 pop     ebp
00455393 pop     ebx
00455394 add     esp, 20h
00455397 retn
00455398 push    offset aSelectorscreen_8; "SelectorScreen_StartAdventure_"
0045539D push    edi; int
0045539E call    sub_482F10
004553A3 mov     eax, [esi+0ACh]
004553A9 mov     ecx, dword_753774
004553AF mov     [eax+0C8h], ecx
004553B5 mov     edx, [esi+0ACh]
004553BB mov     eax, dword_7536A4
004553C0 mov     [edx+0CCh], eax
004553C6 mov     edx, dword_7536A4
004553CC mov     ecx, [esi+0ACh]
004553D2 pop     edi
004553D3 pop     ebp
004553D4 mov     [ecx+0D0h], edx
004553DA pop     ebx
004553DB add     esp, 20h
004553DE retn
