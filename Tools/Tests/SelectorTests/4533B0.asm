004533B0 mov     eax, large fs:0
004533B6 push    0FFFFFFFFh
004533B8 push    offset SEH_4533B0
004533BD push    eax
004533BE mov     eax, [esp+0Ch+arg_4]
004533C2 mov     large fs:0, esp
004533C9 push    ebx
004533CA push    esi
004533CB mov     esi, [esp+14h+arg_0]
004533CF push    eax
004533D0 push    ecx
004533D1 xor     ebx, ebx
004533D3 push    ebx
004533D4 mov     ecx, esi
004533D6 call    sub_5D1090
004533DB mov     [esp+14h+var_4], ebx
004533DF fldz
004533E1 mov     dword ptr [esi], offset off_70D500
004533E7 fst     dword ptr [esi+168h]
004533ED fst     dword ptr [esi+16Ch]
004533F3 push    offset dword_7DCA98
004533F8 fst     dword ptr [esi+170h]
004533FE push    5
00453400 fst     dword ptr [esi+174h]
00453406 mov     ecx, esi
00453408 fst     dword ptr [esi+178h]
0045340E fst     dword ptr [esi+17Ch]
00453414 fst     dword ptr [esi+180h]
0045341A fstp    dword ptr [esi+184h]
00453420 mov     [esi+150h], ebx
00453426 mov     [esi+154h], ebx
0045342C mov     [esi+158h], ebx
00453432 mov     [esi+15Ch], ebx
00453438 mov     [esi+160h], ebx
0045343E mov     [esi+164h], bl
00453444 call    sub_5C4F90
00453449 mov     ecx, [esp+14h+var_C]
0045344D mov     eax, esi
0045344F pop     esi
00453450 mov     large fs:0, ecx
00453457 pop     ebx
00453458 add     esp, 0Ch
0045345B retn    8
006E8070 mov     ecx, [ebp+4]
006E8073 jmp     sub_452710
006E8078 mov     eax, offset stru_73AB0C
006E807D jmp     ___CxxFrameHandler3
