00455950 push    esi
00455951 mov     esi, ecx
00455953 mov     ecx, [esi+0A4h]
00455959 mov     eax, [ecx]
0045595B mov     edx, [eax+124h]
00455961 push    4
00455963 call    edx
00455965 test    eax, eax
00455967 jnz     short loc_455990
00455969 mov     ecx, [esi+0A4h]
0045596F mov     eax, [ecx]
00455971 mov     edx, [eax+124h]
00455977 push    3
00455979 call    edx
0045597B test    eax, eax
0045597D jnz     short loc_455990
0045597F push    esi
00455980 mov     esi, [esp+8+arg_0]
00455984 mov     ecx, esi
00455986 call    sub_458090
0045598B call    sub_458450
00455990 pop     esi
00455991 retn    4
