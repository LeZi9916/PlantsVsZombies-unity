004539E0 push    0FFFFFFFFh
004539E2 push    offset SEH_4539E0
004539E7 mov     eax, large fs:0
004539ED push    eax
004539EE mov     large fs:0, esp
004539F5 sub     esp, 40h
004539F8 push    ebx
004539F9 push    ebp
004539FA mov     ebp, [esp+54h+arg_0]
004539FE push    esi
004539FF push    edi
00453A00 mov     ecx, ebp
00453A02 call    sub_5C4970
00453A07 xor     ebx, ebx
00453A09 mov     [esp+5Ch+var_4], ebx
00453A0D lea     esi, [ebp+0A0h]
00453A13 push    12h; SourceSize
00453A15 mov     dword ptr [esi], offset ??_7ButtonListener@Sexy@@6B@; const Sexy::ButtonListener::`vftable'
00453A1B push    offset aDelayloadZomba; "DelayLoad_Zombatar"
00453A20 lea     ecx, [esp+64h+var_44]
00453A24 mov     dword ptr [ebp+0], offset off_70DCD0
00453A2B mov     dword ptr [esi], offset off_70DDE8
00453A31 mov     [esp+64h+var_2C], 0Fh
00453A39 mov     [esp+64h+var_30], ebx
00453A3D mov     byte ptr [esp+64h+var_40], bl
00453A41 call    sub_4032C0
00453A46 mov     byte ptr [esp+5Ch+var_4], 1
00453A4B mov     eax, dword_755F28
00453A50 mov     eax, [eax+734h]
00453A56 lea     edi, [esp+5Ch+var_44]
00453A5A call    sub_528720
00453A5F mov     byte ptr [esp+5Ch+var_4], bl
00453A63 cmp     [esp+5Ch+var_2C], 10h
00453A68 jb      short loc_453A77
00453A6A mov     ecx, [esp+5Ch+var_40]
00453A6E push    ecx; void *
00453A6F call    ??3@YAXPAX@Z; operator delete(void *)
00453A74 add     esp, 4
00453A77 mov     edx, [esp+5Ch+arg_4]
00453A7B push    7Ch ; '|'; Size
00453A7D mov     [ebp+0A4h], edx
00453A83 mov     dword ptr [ebp+14Ch], 1
00453A8D mov     [ebp+150h], bl
00453A93 mov     [ebp+158h], bl
00453A99 call    ??2@YAPAXI@Z; operator new(uint)
00453A9E add     esp, 4
00453AA1 cmp     eax, ebx
00453AA3 jz      short loc_453AAC
00453AA5 call    sub_52FAB0
00453AAA jmp     short loc_453AAE
00453AAC xor     eax, eax
00453AAE mov     [ebp+154h], eax
00453AB4 push    ebx; int
00453AB5 xor     eax, eax
00453AB7 push    offset unk_708460; Source
00453ABC lea     ecx, [esp+64h+var_44]
00453AC0 mov     [esp+64h+var_2C], 7
00453AC8 mov     [esp+64h+var_30], ebx
00453ACC mov     word ptr [esp+64h+var_40], ax
00453AD1 call    sub_40D1E0
00453AD6 mov     byte ptr [esp+5Ch+var_4], 2
00453ADB mov     eax, dword_7536A4
00453AE0 mov     edi, dword_753774
00453AE6 push    eax
00453AE7 push    ebx
00453AE8 lea     ecx, [esp+64h+var_44]
00453AEC push    ecx
00453AED push    esi
00453AEE push    64h ; 'd'
00453AF0 mov     ebx, eax
00453AF2 call    sub_453910
00453AF7 xor     ebx, ebx
00453AF9 add     esp, 14h
00453AFC mov     byte ptr [esp+5Ch+var_4], bl
00453B00 cmp     [esp+5Ch+var_2C], 8
00453B05 mov     [ebp+0ACh], eax
00453B0B jb      short loc_453B1A
00453B0D mov     edx, [esp+5Ch+var_40]
00453B11 push    edx; void *
00453B12 call    ??3@YAXPAX@Z; operator delete(void *)
00453B17 add     esp, 4
00453B1A mov     edx, dword_753774
00453B20 mov     edx, [edx+24h]
00453B23 mov     ecx, [ebp+0ACh]
00453B29 mov     eax, [ecx]
00453B2B mov     eax, [eax+0A0h]
00453B31 push    7Dh ; '}'
00453B33 push    edx
00453B34 push    ebx
00453B35 push    320h
00453B3A call    eax
00453B3C fld     ds:flt_727E20
00453B42 mov     ecx, [ebp+0ACh]
00453B48 fstp    [esp+5Ch+var_4C]
00453B4C fld1
00453B4E mov     [ecx+51h], bl
00453B51 mov     edx, [ebp+0ACh]
00453B57 fst     [esp+5Ch+var_48]
00453B5B fld     ds:flt_727E1C
00453B61 mov     cl, 1
00453B63 mov     [edx+119h], cl
00453B69 mov     eax, [ebp+0ACh]
00453B6F mov     edx, [esp+5Ch+var_4C]
00453B73 fstp    [esp+5Ch+var_4C]
00453B77 fld     ds:flt_70830C
00453B7D mov     [eax+65h], bl
00453B80 mov     eax, [ebp+0ACh]
00453B86 mov     [eax+168h], edx
00453B8C mov     edx, [esp+5Ch+var_48]
00453B90 fstp    [esp+5Ch+var_48]
00453B94 fld     ds:flt_7276C8
00453B9A mov     [eax+16Ch], edx
00453BA0 mov     eax, [ebp+0ACh]
00453BA6 mov     edx, [esp+5Ch+var_4C]
00453BAA fstp    [esp+5Ch+var_4C]
00453BAE fld     ds:flt_727E18
00453BB4 mov     [eax+170h], edx
00453BBA mov     edx, [esp+5Ch+var_48]
00453BBE fstp    [esp+5Ch+var_48]
00453BC2 mov     [eax+174h], edx
00453BC8 mov     eax, [ebp+0ACh]
00453BCE mov     edx, [esp+5Ch+var_4C]
00453BD2 fstp    [esp+5Ch+var_4C]
00453BD6 fld     ds:flt_727928
00453BDC mov     [eax+178h], edx
00453BE2 mov     edx, [esp+5Ch+var_48]
00453BE6 fstp    [esp+5Ch+var_48]
00453BEA mov     [eax+17Ch], edx
00453BF0 mov     eax, [ebp+0ACh]
00453BF6 mov     edx, [esp+5Ch+var_4C]
00453BFA mov     [eax+180h], edx
00453C00 mov     edx, [esp+5Ch+var_48]
00453C04 mov     [eax+184h], edx
00453C0A mov     eax, [ebp+0ACh]
00453C10 mov     [eax+164h], cl
00453C16 xor     ecx, ecx
00453C18 push    ebx; int
00453C19 mov     word ptr [esp+60h+var_40], cx
00453C1E push    offset unk_708460; Source
00453C23 lea     ecx, [esp+64h+var_44]
00453C27 mov     [esp+64h+var_2C], 7
00453C2F mov     [esp+64h+var_30], ebx
00453C33 call    sub_40D1E0
00453C38 mov     byte ptr [esp+5Ch+var_4], 3
00453C3D mov     eax, dword_7531D4
00453C42 mov     edi, dword_752F68
00453C48 push    eax
00453C49 push    ebx
00453C4A lea     edx, [esp+64h+var_44]
00453C4E push    edx
00453C4F push    esi
00453C50 push    65h ; 'e'
00453C52 mov     ebx, eax
00453C54 call    sub_453910
00453C59 xor     ebx, ebx
00453C5B add     esp, 14h
00453C5E mov     byte ptr [esp+5Ch+var_4], bl
00453C62 cmp     [esp+5Ch+var_2C], 8
00453C67 mov     [ebp+0B0h], eax
00453C6D jb      short loc_453C7C
00453C6F mov     eax, [esp+5Ch+var_40]
00453C73 push    eax; void *
00453C74 call    ??3@YAXPAX@Z; operator delete(void *)
00453C79 add     esp, 4
00453C7C mov     eax, dword_752F68
00453C81 mov     eax, [eax+24h]
00453C84 mov     ecx, [ebp+0B0h]
00453C8A mov     edx, [ecx]
00453C8C mov     edx, [edx+0A0h]
00453C92 push    82h
00453C97 push    eax
00453C98 push    ebx
00453C99 push    320h
00453C9E call    edx
00453CA0 fld     ds:flt_7277C0
00453CA6 mov     eax, [ebp+0B0h]
00453CAC fstp    [esp+5Ch+var_4C]
00453CB0 fld     ds:flt_727298
00453CB6 mov     [eax+51h], bl
00453CB9 mov     ecx, [ebp+0B0h]
00453CBF fstp    [esp+5Ch+var_48]
00453CC3 fld     ds:flt_727E14
00453CC9 mov     [ecx+119h], bl
00453CCF mov     edx, [ebp+0B0h]
00453CD5 mov     ecx, [esp+5Ch+var_4C]
00453CD9 fstp    [esp+5Ch+var_4C]
00453CDD fld     ds:flt_7278B8
00453CE3 mov     [edx+65h], bl
00453CE6 mov     eax, [ebp+0B0h]
00453CEC mov     edx, [esp+5Ch+var_48]
00453CF0 fstp    [esp+5Ch+var_48]
00453CF4 fld     ds:flt_727E10
00453CFA mov     [eax+168h], ecx
00453D00 mov     ecx, [esp+5Ch+var_4C]
00453D04 fstp    [esp+5Ch+var_4C]
00453D08 fld     ds:flt_7279A8
00453D0E mov     [eax+16Ch], edx
00453D14 mov     eax, [ebp+0B0h]
00453D1A mov     edx, [esp+5Ch+var_48]
00453D1E fstp    [esp+5Ch+var_48]
00453D22 fld     ds:flt_727E20
00453D28 mov     [eax+170h], ecx
00453D2E mov     ecx, [esp+5Ch+var_4C]
00453D32 fstp    [esp+5Ch+var_4C]
00453D36 fld     ds:flt_727DC8
00453D3C mov     [eax+174h], edx
00453D42 mov     eax, [ebp+0B0h]
00453D48 mov     edx, [esp+5Ch+var_48]
00453D4C fstp    [esp+5Ch+var_48]
00453D50 mov     [eax+178h], ecx
00453D56 mov     ecx, [esp+5Ch+var_4C]
00453D5A mov     [eax+17Ch], edx
00453D60 mov     eax, [ebp+0B0h]
00453D66 mov     edx, [esp+5Ch+var_48]
00453D6A mov     [eax+180h], ecx
00453D70 xor     ecx, ecx
00453D72 mov     [eax+184h], edx
00453D78 mov     eax, [ebp+0B0h]
00453D7E push    ebx; int
00453D7F mov     word ptr [esp+60h+var_40], cx
00453D84 push    offset unk_708460; Source
00453D89 lea     ecx, [esp+64h+var_44]
00453D8D mov     byte ptr [eax+164h], 1
00453D94 mov     [esp+64h+var_2C], 7
00453D9C mov     [esp+64h+var_30], ebx
00453DA0 call    sub_40D1E0
00453DA5 mov     byte ptr [esp+5Ch+var_4], 4
00453DAA mov     eax, dword_752B28
00453DAF mov     edi, dword_7537A8
00453DB5 push    eax
00453DB6 push    ebx
00453DB7 lea     edx, [esp+64h+var_44]
00453DBB push    edx
00453DBC push    esi
00453DBD push    66h ; 'f'
00453DBF mov     ebx, eax
00453DC1 call    sub_453910
00453DC6 xor     ebx, ebx
00453DC8 add     esp, 14h
00453DCB mov     byte ptr [esp+5Ch+var_4], bl
00453DCF cmp     [esp+5Ch+var_2C], 8
00453DD4 mov     [ebp+0B4h], eax
00453DDA jb      short loc_453DE9
00453DDC mov     eax, [esp+5Ch+var_40]
00453DE0 push    eax; void *
00453DE1 call    ??3@YAXPAX@Z; operator delete(void *)
00453DE6 add     esp, 4
00453DE9 mov     eax, dword_7537A8
00453DEE mov     eax, [eax+24h]
00453DF1 mov     ecx, [ebp+0B4h]
00453DF7 mov     edx, [ecx]
00453DF9 mov     edx, [edx+0A0h]
00453DFF push    79h ; 'y'
00453E01 push    eax
00453E02 push    ebx
00453E03 push    320h
00453E08 call    edx
00453E0A fld     ds:flt_727298
00453E10 mov     eax, [ebp+0B4h]
00453E16 fstp    [esp+5Ch+var_4C]
00453E1A fldz
00453E1C mov     [eax+51h], bl
00453E1F mov     ecx, [ebp+0B4h]
00453E25 fstp    [esp+5Ch+var_48]
00453E29 fld     ds:flt_727E0C
00453E2F mov     [ecx+119h], bl
00453E35 mov     edx, [ebp+0B4h]
00453E3B mov     ecx, [esp+5Ch+var_4C]
00453E3F fstp    [esp+5Ch+var_4C]
00453E43 fld     ds:flt_727898
00453E49 mov     [edx+65h], bl
00453E4C mov     eax, [ebp+0B4h]
00453E52 mov     edx, [esp+5Ch+var_48]
00453E56 fstp    [esp+5Ch+var_48]
00453E5A fld     ds:flt_727E08
00453E60 mov     [eax+168h], ecx
00453E66 mov     ecx, [esp+5Ch+var_4C]
00453E6A fstp    [esp+5Ch+var_4C]
00453E6E fld     ds:flt_727E04
00453E74 mov     [eax+16Ch], edx
00453E7A mov     eax, [ebp+0B4h]
00453E80 mov     edx, [esp+5Ch+var_48]
00453E84 fstp    [esp+5Ch+var_48]
00453E88 fld     ds:flt_72781C
00453E8E mov     [eax+170h], ecx
00453E94 mov     ecx, [esp+5Ch+var_4C]
00453E98 fstp    [esp+5Ch+var_4C]
00453E9C fld     ds:flt_727944
00453EA2 mov     [eax+174h], edx
00453EA8 mov     eax, [ebp+0B4h]
00453EAE mov     edx, [esp+5Ch+var_48]
00453EB2 fstp    [esp+5Ch+var_48]
00453EB6 mov     [eax+178h], ecx
00453EBC mov     ecx, [esp+5Ch+var_4C]
00453EC0 mov     [eax+17Ch], edx
00453EC6 mov     eax, [ebp+0B4h]
00453ECC mov     edx, [esp+5Ch+var_48]
00453ED0 mov     [eax+180h], ecx
00453ED6 xor     ecx, ecx
00453ED8 mov     [eax+184h], edx
00453EDE mov     eax, [ebp+0B4h]
00453EE4 push    ebx; int
00453EE5 mov     word ptr [esp+60h+var_40], cx
00453EEA push    offset unk_708460; Source
00453EEF lea     ecx, [esp+64h+var_44]
00453EF3 mov     byte ptr [eax+164h], 1
00453EFA mov     [esp+64h+var_2C], 7
00453F02 mov     [esp+64h+var_30], ebx
00453F06 call    sub_40D1E0
00453F0B mov     byte ptr [esp+5Ch+var_4], 5
00453F10 mov     eax, dword_752CDC
00453F15 mov     edi, dword_7535B4
00453F1B push    eax
00453F1C push    ebx
00453F1D lea     edx, [esp+64h+var_44]
00453F21 push    edx
00453F22 push    esi
00453F23 push    6Eh ; 'n'
00453F25 mov     ebx, eax
00453F27 call    sub_453910
00453F2C xor     ebx, ebx
00453F2E add     esp, 14h
00453F31 mov     byte ptr [esp+5Ch+var_4], bl
00453F35 cmp     [esp+5Ch+var_2C], 8
00453F3A mov     [ebp+0DCh], eax
00453F40 jb      short loc_453F4F
00453F42 mov     eax, [esp+5Ch+var_40]
00453F46 push    eax; void *
00453F47 call    ??3@YAXPAX@Z; operator delete(void *)
00453F4C add     esp, 4
00453F4F mov     eax, dword_7535B4
00453F54 mov     eax, [eax+24h]
00453F57 mov     ecx, [ebp+0DCh]
00453F5D mov     edx, [ecx]
00453F5F mov     edx, [edx+0A0h]
00453F65 push    7Ch ; '|'
00453F67 push    eax
00453F68 push    ebx
00453F69 push    320h
00453F6E call    edx
00453F70 fld     ds:flt_727E20
00453F76 mov     eax, [ebp+0DCh]
00453F7C fst     [esp+5Ch+var_4C]
00453F80 fld1
00453F82 mov     [eax+51h], bl
00453F85 mov     ecx, [ebp+0DCh]
00453F8B fstp    [esp+5Ch+var_48]
00453F8F fld     ds:flt_727E00
00453F95 mov     [ecx+119h], bl
00453F9B mov     edx, [ebp+0DCh]
00453FA1 mov     ecx, [esp+5Ch+var_4C]
00453FA5 fstp    [esp+5Ch+var_4C]
00453FA9 fld     ds:flt_727A88
00453FAF mov     [edx+65h], bl
00453FB2 mov     eax, [ebp+0DCh]
00453FB8 mov     edx, [esp+5Ch+var_48]
00453FBC fstp    [esp+5Ch+var_48]
00453FC0 fld     ds:flt_727DFC
00453FC6 mov     [eax+168h], ecx
00453FCC mov     ecx, [esp+5Ch+var_4C]
00453FD0 fstp    [esp+5Ch+var_4C]
00453FD4 fld     ds:flt_727DF8
00453FDA mov     [eax+16Ch], edx
00453FE0 mov     eax, [ebp+0DCh]
00453FE6 mov     edx, [esp+5Ch+var_48]
00453FEA fstp    [esp+5Ch+var_48]
00453FEE mov     [eax+170h], ecx
00453FF4 mov     ecx, [esp+5Ch+var_4C]
00453FF8 fstp    [esp+5Ch+var_4C]
00453FFC fld     ds:flt_727890
00454002 mov     [eax+174h], edx
00454008 mov     eax, [ebp+0DCh]
0045400E mov     edx, [esp+5Ch+var_48]
00454012 fstp    [esp+5Ch+var_48]
00454016 mov     [eax+178h], ecx
0045401C mov     ecx, [esp+5Ch+var_4C]
00454020 mov     [eax+17Ch], edx
00454026 mov     eax, [ebp+0DCh]
0045402C mov     edx, [esp+5Ch+var_48]
00454030 mov     [eax+180h], ecx
00454036 xor     ecx, ecx
00454038 mov     [eax+184h], edx
0045403E mov     eax, [ebp+0DCh]
00454044 push    ebx; int
00454045 mov     word ptr [esp+60h+var_40], cx
0045404A push    offset unk_708460; Source
0045404F lea     ecx, [esp+64h+var_44]
00454053 mov     byte ptr [eax+164h], 1
0045405A mov     [esp+64h+var_2C], 7
00454062 mov     [esp+64h+var_30], ebx
00454066 call    sub_40D1E0
0045406B mov     byte ptr [esp+5Ch+var_4], 6
00454070 mov     edx, dword_752F20
00454076 mov     edi, dword_752BB0
0045407C push    edx
0045407D push    ebx
0045407E mov     ebx, dword_752AB4
00454084 lea     eax, [esp+64h+var_44]
00454088 push    eax
00454089 push    esi
0045408A push    73h ; 's'
0045408C call    sub_453910
00454091 xor     ebx, ebx
00454093 add     esp, 14h
00454096 mov     byte ptr [esp+5Ch+var_4], bl
0045409A cmp     [esp+5Ch+var_2C], 8
0045409F mov     [ebp+0A8h], eax
004540A5 jb      short loc_4540B4
004540A7 mov     ecx, [esp+5Ch+var_40]
004540AB push    ecx; void *
004540AC call    ??3@YAXPAX@Z; operator delete(void *)
004540B1 add     esp, 4
004540B4 mov     edx, [ebp+0A8h]
004540BA mov     edi, [edx]
004540BC mov     ecx, dword_752BB0
004540C2 add     edi, 0A0h
004540C8 call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
004540CD mov     ecx, dword_752BB0; this
004540D3 push    eax
004540D4 call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
004540D9 mov     ecx, [ebp+0A8h]
004540DF push    eax
004540E0 mov     eax, [edi]
004540E2 push    ebx
004540E3 push    320h
004540E8 call    eax
004540EA mov     ecx, [ebp+0A8h]
004540F0 mov     [ecx+51h], bl
004540F3 mov     edx, [ebp+0A8h]
004540F9 xor     ecx, ecx
004540FB mov     byte ptr [edx+119h], 1
00454102 mov     eax, [ebp+0A8h]
00454108 push    ebx; int
00454109 mov     word ptr [esp+60h+var_40], cx
0045410E push    offset unk_708460; Source
00454113 lea     ecx, [esp+64h+var_44]
00454117 mov     [eax+65h], bl
0045411A mov     [esp+64h+var_2C], 7
00454122 mov     [esp+64h+var_30], ebx
00454126 call    sub_40D1E0
0045412B mov     byte ptr [esp+5Ch+var_4], 7
00454130 mov     edi, dword_753350
00454136 push    edi
00454137 push    ebx
00454138 lea     edx, [esp+64h+var_44]
0045413C push    edx
0045413D push    esi
0045413E push    6Fh ; 'o'
00454140 mov     ebx, edi
00454142 call    sub_453910
00454147 xor     ebx, ebx
00454149 add     esp, 14h
0045414C mov     byte ptr [esp+5Ch+var_4], bl
00454150 cmp     [esp+5Ch+var_2C], 8
00454155 mov     [ebp+0C4h], eax
0045415B jb      short loc_45416A
0045415D mov     eax, [esp+5Ch+var_40]
00454161 push    eax; void *
00454162 call    ??3@YAXPAX@Z; operator delete(void *)
00454167 add     esp, 4
0045416A mov     ecx, [ebp+0C4h]
00454170 mov     edi, [ecx]
00454172 mov     ecx, dword_752F14
00454178 add     edi, 0A0h
0045417E call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
00454183 mov     ecx, dword_752F14; this
00454189 push    eax
0045418A call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
0045418F mov     ecx, [ebp+0C4h]
00454195 mov     edx, [edi]
00454197 push    eax
00454198 push    ebx
00454199 push    ebx
0045419A call    edx
0045419C mov     eax, [ebp+0C4h]
004541A2 mov     [eax+51h], bl
004541A5 mov     ecx, [ebp+0C4h]
004541AB mov     byte ptr [ecx+119h], 1
004541B2 mov     edx, [ebp+0C4h]
004541B8 push    ebx; int
004541B9 xor     eax, eax
004541BB push    offset unk_708460; Source
004541C0 lea     ecx, [esp+64h+var_44]
004541C4 mov     [edx+65h], bl
004541C7 mov     [esp+64h+var_2C], 7
004541CF mov     [esp+64h+var_30], ebx
004541D3 mov     word ptr [esp+64h+var_40], ax
004541D8 call    sub_40D1E0
004541DD mov     byte ptr [esp+5Ch+var_4], 8
004541E2 mov     eax, dword_753468
004541E7 mov     edi, dword_752D50
004541ED push    eax
004541EE push    ebx
004541EF lea     ecx, [esp+64h+var_44]
004541F3 push    ecx
004541F4 push    esi
004541F5 push    71h ; 'q'
004541F7 mov     ebx, eax
004541F9 call    sub_453910
004541FE xor     ebx, ebx
00454200 add     esp, 14h
00454203 mov     byte ptr [esp+5Ch+var_4], bl
00454207 cmp     [esp+5Ch+var_2C], 8
0045420C mov     [ebp+0C8h], eax
00454212 jb      short loc_454221
00454214 mov     edx, [esp+5Ch+var_40]
00454218 push    edx; void *
00454219 call    ??3@YAXPAX@Z; operator delete(void *)
0045421E add     esp, 4
00454221 mov     eax, [ebp+0C8h]
00454227 mov     edi, [eax]
00454229 mov     ecx, dword_752D50
0045422F add     edi, 0A0h
00454235 call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
0045423A mov     ecx, dword_752D50; this
00454240 push    eax
00454241 call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
00454246 mov     ecx, dword_752D50
0045424C push    eax
0045424D call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
00454252 mov     ecx, [ebp+0A4h]
00454258 mov     edx, [ecx+0C4h]
0045425E mov     ecx, [ebp+0C8h]
00454264 sub     edx, eax
00454266 mov     eax, [edi]
00454268 sub     edx, 23h ; '#'
0045426B push    edx
0045426C push    334h
00454271 call    eax
00454273 mov     ecx, [ebp+0C8h]
00454279 mov     [ecx+51h], bl
0045427C mov     edx, [ebp+0C8h]
00454282 xor     ecx, ecx
00454284 mov     [edx+119h], bl
0045428A mov     eax, [ebp+0C8h]
00454290 push    ebx; int
00454291 mov     word ptr [esp+60h+var_40], cx
00454296 push    offset unk_708460; Source
0045429B lea     ecx, [esp+64h+var_44]
0045429F mov     [eax+65h], bl
004542A2 mov     [esp+64h+var_2C], 7
004542AA mov     [esp+64h+var_30], ebx
004542AE call    sub_40D1E0
004542B3 mov     byte ptr [esp+5Ch+var_4], 9
004542B8 mov     eax, dword_752FBC
004542BD mov     edi, dword_752E3C
004542C3 push    eax
004542C4 push    ebx
004542C5 lea     edx, [esp+64h+var_44]
004542C9 push    edx
004542CA push    esi
004542CB push    6Dh ; 'm'
004542CD mov     ebx, eax
004542CF call    sub_453910
004542D4 xor     ebx, ebx
004542D6 add     esp, 14h
004542D9 mov     byte ptr [esp+5Ch+var_4], bl
004542DD cmp     [esp+5Ch+var_2C], 8
004542E2 mov     [ebp+0D8h], eax
004542E8 jb      short loc_4542F7
004542EA mov     eax, [esp+5Ch+var_40]
004542EE push    eax; void *
004542EF call    ??3@YAXPAX@Z; operator delete(void *)
004542F4 add     esp, 4
004542F7 mov     ecx, [ebp+0D8h]
004542FD mov     edx, [ecx]
004542FF mov     eax, [edx+0A0h]
00454305 push    82h
0045430A push    82h
0045430F push    ebx
00454310 push    320h
00454315 call    eax
00454317 mov     ecx, [ebp+0D8h]
0045431D mov     [ecx+65h], bl
00454320 mov     edx, [ebp+0D8h]
00454326 push    ebx; int
00454327 xor     eax, eax
00454329 push    offset unk_708460; Source
0045432E lea     ecx, [esp+64h+var_44]
00454332 mov     [edx+51h], bl
00454335 mov     [esp+64h+var_2C], 7
0045433D mov     [esp+64h+var_30], ebx
00454341 mov     word ptr [esp+64h+var_40], ax
00454346 call    sub_40D1E0
0045434B mov     byte ptr [esp+5Ch+var_4], 0Ah
00454350 mov     eax, dword_7530DC
00454355 mov     edi, dword_753034
0045435B push    eax
0045435C push    ebx
0045435D lea     ecx, [esp+64h+var_44]
00454361 push    ecx
00454362 push    esi
00454363 push    67h ; 'g'
00454365 mov     ebx, eax
00454367 call    sub_453910
0045436C xor     ebx, ebx
0045436E add     esp, 14h
00454371 mov     byte ptr [esp+5Ch+var_4], bl
00454375 cmp     [esp+5Ch+var_2C], 8
0045437A mov     [ebp+0B8h], eax
00454380 jb      short loc_45438F
00454382 mov     edx, [esp+5Ch+var_40]
00454386 push    edx; void *
00454387 call    ??3@YAXPAX@Z; operator delete(void *)
0045438C add     esp, 4
0045438F mov     eax, dword_753034
00454394 mov     edi, [eax+28h]
00454397 mov     eax, [eax+24h]
0045439A mov     ecx, [ebp+0B8h]
004543A0 mov     edx, [ecx]
004543A2 mov     edx, [edx+0A0h]
004543A8 add     edi, 17h
004543AB push    edi
004543AC push    eax
004543AD push    ebx
004543AE push    320h
004543B3 call    edx
004543B5 mov     eax, [ebp+0B8h]
004543BB mov     byte ptr [eax+119h], 1
004543C2 mov     ecx, [ebp+0B8h]
004543C8 mov     [ecx+65h], bl
004543CB mov     edx, [ebp+0B8h]
004543D1 push    ebx; int
004543D2 xor     eax, eax
004543D4 push    offset unk_708460; Source
004543D9 lea     ecx, [esp+64h+var_44]
004543DD mov     dword ptr [edx+160h], 0Fh
004543E7 mov     [esp+64h+var_2C], 7
004543EF mov     [esp+64h+var_30], ebx
004543F3 mov     word ptr [esp+64h+var_40], ax
004543F8 call    sub_40D1E0
004543FD mov     byte ptr [esp+5Ch+var_4], 0Bh
00454402 mov     eax, dword_753630
00454407 mov     edi, dword_753104
0045440D push    eax
0045440E push    ebx
0045440F lea     ecx, [esp+64h+var_44]
00454413 push    ecx
00454414 push    esi
00454415 push    68h ; 'h'
00454417 mov     ebx, eax
00454419 call    sub_453910
0045441E xor     ebx, ebx
00454420 add     esp, 14h
00454423 mov     byte ptr [esp+5Ch+var_4], bl
00454427 cmp     [esp+5Ch+var_2C], 8
0045442C mov     [ebp+0C0h], eax
00454432 jb      short loc_454441
00454434 mov     edx, [esp+5Ch+var_40]
00454438 push    edx; void *
00454439 call    ??3@YAXPAX@Z; operator delete(void *)
0045443E add     esp, 4
00454441 mov     eax, dword_753104
00454446 mov     edi, [eax+28h]
00454449 mov     eax, [eax+24h]
0045444C mov     ecx, [ebp+0C0h]
00454452 mov     edx, [ecx]
00454454 mov     edx, [edx+0A0h]
0045445A add     edi, 21h ; '!'
0045445D push    edi
0045445E push    eax
0045445F push    ebx
00454460 push    320h
00454465 call    edx
00454467 mov     eax, [ebp+0C0h]
0045446D mov     byte ptr [eax+119h], 1
00454474 mov     ecx, [ebp+0C0h]
0045447A mov     [ecx+65h], bl
0045447D mov     edx, [ebp+0C0h]
00454483 push    ebx; int
00454484 xor     eax, eax
00454486 push    offset unk_708460; Source
0045448B lea     ecx, [esp+64h+var_44]
0045448F mov     dword ptr [edx+160h], 1Eh
00454499 mov     [esp+64h+var_2C], 7
004544A1 mov     [esp+64h+var_30], ebx
004544A5 mov     word ptr [esp+64h+var_40], ax
004544AA call    sub_40D1E0
004544AF mov     byte ptr [esp+5Ch+var_4], 0Ch
004544B4 mov     eax, dword_752BA4
004544B9 mov     edi, dword_752F80
004544BF push    eax
004544C0 push    ebx
004544C1 lea     ecx, [esp+64h+var_44]
004544C5 push    ecx
004544C6 push    esi
004544C7 push    69h ; 'i'
004544C9 mov     ebx, eax
004544CB call    sub_453910
004544D0 xor     ebx, ebx
004544D2 add     esp, 14h
004544D5 mov     byte ptr [esp+5Ch+var_4], bl
004544D9 cmp     [esp+5Ch+var_2C], 8
004544DE mov     [ebp+0BCh], eax
004544E4 jb      short loc_4544F3
004544E6 mov     edx, [esp+5Ch+var_40]
004544EA push    edx; void *
004544EB call    ??3@YAXPAX@Z; operator delete(void *)
004544F0 add     esp, 4
004544F3 mov     eax, dword_752F80
004544F8 mov     edi, [eax+28h]
004544FB mov     eax, [eax+24h]
004544FE mov     ecx, [ebp+0BCh]
00454504 mov     edx, [ecx]
00454506 mov     edx, [edx+0A0h]
0045450C add     edi, 0Ah
0045450F push    edi
00454510 add     eax, 0Ah
00454513 push    eax
00454514 push    ebx
00454515 push    320h
0045451A call    edx
0045451C mov     eax, [ebp+0BCh]
00454522 mov     byte ptr [eax+119h], 1
00454529 mov     ecx, [ebp+0BCh]
0045452F mov     [ecx+65h], bl
00454532 mov     edx, [ebp+0BCh]
00454538 mov     eax, 5
0045453D mov     [edx+15Ch], eax
00454543 mov     ecx, [ebp+0BCh]
00454549 push    ebx; int
0045454A mov     [ecx+160h], eax
00454550 xor     edx, edx
00454552 push    offset unk_708460; Source
00454557 lea     ecx, [esp+64h+var_44]
0045455B mov     [esp+64h+var_2C], 7
00454563 mov     [esp+64h+var_30], ebx
00454567 mov     word ptr [esp+64h+var_40], dx
0045456C call    sub_40D1E0
00454571 mov     byte ptr [esp+5Ch+var_4], 0Dh
00454576 mov     edi, dword_753350
0045457C push    edi
0045457D push    ebx
0045457E lea     eax, [esp+64h+var_44]
00454582 push    eax
00454583 push    esi
00454584 push    6Ah ; 'j'
00454586 mov     ebx, edi
00454588 call    sub_453910
0045458D xor     ebx, ebx
0045458F add     esp, 14h
00454592 mov     byte ptr [esp+5Ch+var_4], bl
00454596 cmp     [esp+5Ch+var_2C], 8
0045459B mov     [ebp+0E0h], eax
004545A1 jb      short loc_4545B0
004545A3 mov     ecx, [esp+5Ch+var_40]
004545A7 push    ecx; void *
004545A8 call    ??3@YAXPAX@Z; operator delete(void *)
004545AD add     esp, 4
004545B0 mov     ecx, [ebp+0E0h]
004545B6 mov     edx, [ecx]
004545B8 mov     eax, [edx+0A0h]
004545BE push    1Eh
004545C0 push    0FAh
004545C5 push    ebx
004545C6 push    320h
004545CB call    eax
004545CD mov     ecx, [ebp+0E0h]
004545D3 mov     byte ptr [ecx+119h], 1
004545DA mov     edx, [ebp+0E0h]
004545E0 push    0A4h; Size
004545E5 mov     [edx+65h], bl
004545E8 call    ??2@YAPAXI@Z; operator new(uint)
004545ED mov     edi, eax
004545EF add     esp, 4
004545F2 mov     [esp+5Ch+arg_4], edi
004545F6 mov     byte ptr [esp+5Ch+var_4], 0Eh
004545FB cmp     edi, ebx
004545FD jz      short loc_45461D
004545FF mov     ecx, edi
00454601 call    sub_5C4970
00454606 mov     dword ptr [edi], offset off_70CE58
0045460C mov     [edi+0A0h], ebp
00454612 mov     [edi+65h], bl
00454615 mov     byte ptr [edi+50h], 1
00454619 mov     ecx, edi
0045461B jmp     short loc_45461F
0045461D xor     ecx, ecx
0045461F push    258h
00454624 mov     byte ptr [esp+60h+var_4], bl
00454628 push    320h
0045462D mov     [ebp+0E4h], ecx
00454633 mov     eax, [ecx]
00454635 mov     edx, [eax+0A0h]
0045463B push    ebx
0045463C push    320h
00454641 call    edx
00454643 push    ebx; int
00454644 xor     eax, eax
00454646 push    offset unk_708460; Source
0045464B lea     ecx, [esp+64h+var_44]
0045464F mov     [esp+64h+var_2C], 7
00454657 mov     [esp+64h+var_30], ebx
0045465B mov     word ptr [esp+64h+var_40], ax
00454660 call    sub_40D1E0
00454665 mov     byte ptr [esp+5Ch+var_4], 0Fh
0045466A mov     eax, dword_753404
0045466F mov     edi, dword_752C44
00454675 push    eax
00454676 push    ebx
00454677 lea     ecx, [esp+64h+var_44]
0045467B push    ecx
0045467C push    esi
0045467D push    6Bh ; 'k'
0045467F mov     ebx, eax
00454681 call    sub_453910
00454686 xor     ebx, ebx
00454688 add     esp, 14h
0045468B mov     byte ptr [esp+5Ch+var_4], bl
0045468F cmp     [esp+5Ch+var_2C], 8
00454694 mov     [ebp+0D0h], eax
0045469A jb      short loc_4546A9
0045469C mov     edx, [esp+5Ch+var_40]
004546A0 push    edx; void *
004546A1 call    ??3@YAXPAX@Z; operator delete(void *)
004546A6 add     esp, 4
004546A9 mov     eax, dword_752C44
004546AE mov     edi, [eax+28h]
004546B1 mov     eax, [eax+24h]
004546B4 mov     ecx, [ebp+0D0h]
004546BA mov     edx, [ecx]
004546BC mov     edx, [edx+0A0h]
004546C2 push    edi
004546C3 push    eax
004546C4 push    1E4h
004546C9 push    4B5h
004546CE call    edx
004546D0 mov     eax, [ebp+0D0h]
004546D6 xor     ecx, ecx
004546D8 push    ebx; int
004546D9 mov     word ptr [esp+60h+var_40], cx
004546DE push    offset unk_708460; Source
004546E3 lea     ecx, [esp+64h+var_44]
004546E7 mov     [eax+65h], bl
004546EA mov     [esp+64h+var_2C], 7
004546F2 mov     [esp+64h+var_30], ebx
004546F6 call    sub_40D1E0
004546FB mov     byte ptr [esp+5Ch+var_4], 10h
00454700 mov     eax, dword_752BF0
00454705 mov     edi, dword_7533EC
0045470B push    eax
0045470C push    ebx
0045470D lea     edx, [esp+64h+var_44]
00454711 push    edx
00454712 push    esi
00454713 push    6Ch ; 'l'
00454715 mov     ebx, eax
00454717 call    sub_453910
0045471C xor     ebx, ebx
0045471E add     esp, 14h
00454721 mov     byte ptr [esp+5Ch+var_4], bl
00454725 cmp     [esp+5Ch+var_2C], 8
0045472A mov     [ebp+0D4h], eax
00454730 jb      short loc_45473F
00454732 mov     eax, [esp+5Ch+var_40]
00454736 push    eax; void *
00454737 call    ??3@YAXPAX@Z; operator delete(void *)
0045473C add     esp, 4
0045473F mov     eax, dword_7533EC
00454744 mov     edi, [eax+28h]
00454747 mov     eax, [eax+24h]
0045474A mov     ecx, [ebp+0D4h]
00454750 mov     edx, [ecx]
00454752 mov     edx, [edx+0A0h]
00454758 push    edi
00454759 push    eax
0045475A push    1ACh
0045475F push    467h
00454764 call    edx
00454766 mov     eax, [ebp+0D4h]
0045476C xor     ecx, ecx
0045476E push    ebx; int
0045476F mov     word ptr [esp+60h+var_40], cx
00454774 push    offset unk_708460; Source
00454779 lea     ecx, [esp+64h+var_44]
0045477D mov     [eax+65h], bl
00454780 mov     [esp+64h+var_2C], 7
00454788 mov     [esp+64h+var_30], ebx
0045478C call    sub_40D1E0
00454791 mov     byte ptr [esp+5Ch+var_4], 11h
00454796 mov     eax, dword_752E20
0045479B mov     edi, dword_75381C
004547A1 push    eax
004547A2 push    ebx
004547A3 lea     edx, [esp+64h+var_44]
004547A7 push    edx
004547A8 push    esi
004547A9 push    72h ; 'r'
004547AB mov     ebx, eax
004547AD call    sub_453910
004547B2 add     esp, 14h
004547B5 mov     byte ptr [esp+5Ch+var_4], 0
004547BA cmp     [esp+5Ch+var_2C], 8
004547BF mov     [ebp+0CCh], eax
004547C5 jb      short loc_4547D4
004547C7 mov     eax, [esp+5Ch+var_40]
004547CB push    eax; void *
004547CC call    ??3@YAXPAX@Z; operator delete(void *)
004547D1 add     esp, 4
004547D4 mov     eax, dword_75381C
004547D9 mov     esi, [eax+28h]
004547DC mov     eax, [eax+24h]
004547DF mov     ecx, [ebp+0CCh]
004547E5 mov     edx, [ecx]
004547E7 mov     edx, [edx+0A0h]
004547ED push    esi
004547EE push    eax
004547EF mov     eax, [ebp+0A4h]
004547F5 mov     eax, [eax+0C0h]
004547FB push    1C7h
00454800 sub     eax, 96h
00454805 push    eax
00454806 call    edx
00454808 mov     eax, [ebp+0A4h]
0045480E mov     esi, [eax+960h]
00454814 cmp     dword ptr [esi+8], 7
00454818 jz      short loc_45482C
0045481A call    sub_468F30
0045481F or      ecx, 0FFFFFFFFh
00454822 push    0FFFFFFFFh
00454824 lea     eax, [ecx+8]
00454827 call    sub_469140
0045482C mov     eax, [ebp+0A4h]
00454832 xor     esi, esi
00454834 mov     byte ptr [ebp+0E8h], 0
0045483B mov     [ebp+0ECh], esi
00454841 mov     byte ptr [ebp+0F0h], 0
00454848 mov     byte ptr [ebp+0F1h], 0
0045484F mov     byte ptr [ebp+0F2h], 0
00454856 mov     byte ptr [ebp+159h], 0
0045485D mov     [ebp+0F8h], esi
00454863 mov     byte ptr [ebp+0F3h], 0
0045486A mov     byte ptr [ebp+0F4h], 1
00454871 mov     ecx, [eax+944h]
00454877 mov     edi, [ecx+8]
0045487A call    sub_483BC0
0045487F mov     ebx, eax
00454881 push    esi
00454882 push    65h ; 'e'
00454884 mov     [ebx+60h], edi
00454887 mov     [ebx+68h], esi
0045488A call    sub_482AA0
0045488F fld     ds:flt_727270
00454895 mov     eax, dword_755E3C
0045489A fst     [esp+64h+var_60]; float
0045489E fstp    [esp+64h+var_64]; float
004548A1 add     eax, 650h
004548A6 push    ebx; int
004548A7 mov     dword ptr [ebx], 65h ; 'e'
004548AD call    sub_480E40
004548B2 fld     ds:flt_70830C
004548B8 fstp    dword ptr [ebx+8]
004548BB lea     edx, [ebx+1Ch]
004548BE fldz
004548C0 push    edx
004548C1 fstp    dword ptr [ebx+4]
004548C4 lea     eax, [ebx+18h]
004548C7 fld     ds:flt_727228
004548CD push    eax
004548CE mov     edx, offset aAnimOpen; "anim_open"
004548D3 fstp    dword ptr [ebx+94h]
004548D9 mov     eax, ebx
004548DB mov     dword ptr [ebx+10h], 3
004548E2 mov     [ebx+5Ch], esi
004548E5 call    sub_482690
004548EA push    0FFFFFFFFh; int
004548EC push    offset aFlower; "flower"
004548F1 push    ebx; int
004548F2 call    sub_482F10
004548F7 push    0FFFFFFFFh; int
004548F9 push    offset aLeaf; "leaf"
004548FE push    ebx; int
004548FF call    sub_482F10
00454904 push    1; int
00454906 push    offset aSelectorscreen_6; "SelectorScreen_BG"
0045490B call    sub_482EC0
00454910 mov     eax, [ebx+9Ch]
00454916 lea     ecx, [esp+5Ch+var_4C]
0045491A push    ecx
0045491B lea     edx, [esp+60h+arg_4]
0045491F mov     [ebp+0FCh], eax
00454925 push    edx
00454926 mov     edx, offset aAnimSign; "anim_sign"
0045492B mov     eax, ebx
0045492D mov     [ebp+148h], esi
00454933 call    sub_482690
00454938 mov     eax, [esp+5Ch+arg_4]
0045493C mov     ecx, [esp+5Ch+var_4C]
00454940 lea     edx, [eax+ecx-1]
00454944 lea     eax, [ebp+118h]
0045494A mov     [ebx+20h], edx
0045494D mov     [esp+5Ch+arg_4], esi
00454951 mov     [esp+5Ch+var_4C], eax
00454955 jmp     short loc_454960
00454960 mov     eax, [ebp+0A4h]
00454966 mov     ecx, [eax+944h]
0045496C mov     esi, [ecx+8]
0045496F mov     edi, esi
00454971 call    sub_483BC0
00454976 mov     edi, eax
00454978 mov     [edi+60h], esi
0045497B xor     esi, esi
0045497D push    esi
0045497E push    65h ; 'e'
00454980 mov     [edi+68h], esi
00454983 call    sub_482AA0
00454988 fld     ds:flt_727270
0045498E mov     eax, dword_755E3C
00454993 fst     [esp+64h+var_60]; float
00454997 fstp    [esp+64h+var_64]; float
0045499A add     eax, 650h
0045499F push    edi; int
004549A0 mov     dword ptr [edi], 65h ; 'e'
004549A6 call    sub_480E40
004549AB mov     [esp+5Ch+var_2C], 0Fh
004549B3 mov     [esp+5Ch+var_30], esi
004549B7 mov     byte ptr [esp+5Ch+var_40], 0
004549BC mov     byte ptr [esp+5Ch+var_4], 12h
004549C1 mov     eax, [esp+5Ch+arg_4]
004549C5 cmp     eax, 1
004549C8 jg      short loc_4549FE
004549CA inc     eax
004549CB push    eax; int
004549CC lea     edx, [esp+60h+var_28]
004549D0 push    offset aAnimCloudD; "anim_cloud%d"
004549D5 push    edx; int
004549D6 call    sub_5F0BD0
004549DB add     esp, 0Ch
004549DE push    0FFFFFFFFh
004549E0 push    esi
004549E1 push    eax
004549E2 lea     ecx, [esp+68h+var_44]
004549E6 mov     byte ptr [esp+68h+var_4], 13h
004549EB call    sub_401D80
004549F0 cmp     [esp+5Ch+var_10], 10h
004549F5 jb      short loc_454A3A
004549F7 mov     eax, [esp+5Ch+var_24]
004549FB push    eax
004549FC jmp     short loc_454A32
004549FE add     eax, 2
00454A01 push    eax; int
00454A02 lea     ecx, [esp+60h+var_28]
00454A06 push    offset aAnimCloudD; "anim_cloud%d"
00454A0B push    ecx; int
00454A0C call    sub_5F0BD0
00454A11 add     esp, 0Ch
00454A14 push    0FFFFFFFFh
00454A16 push    esi
00454A17 push    eax
00454A18 lea     ecx, [esp+68h+var_44]
00454A1C mov     byte ptr [esp+68h+var_4], 14h
00454A21 call    sub_401D80
00454A26 cmp     [esp+5Ch+var_10], 10h
00454A2B jb      short loc_454A3A
00454A2D mov     edx, [esp+5Ch+var_24]
00454A31 push    edx; void *
00454A32 call    ??3@YAXPAX@Z; operator delete(void *)
00454A37 add     esp, 4
00454A3A cmp     [esp+5Ch+var_2C], 10h
00454A3F mov     edx, [esp+5Ch+var_40]
00454A43 jnb     short loc_454A49
00454A45 lea     edx, [esp+5Ch+var_40]
00454A49 fldz
00454A4B mov     dword ptr [edi+10h], 3
00454A52 fcom    dword ptr [edi+8]
00454A55 mov     [edi+5Ch], esi
00454A58 fnstsw  ax
00454A5A test    ah, 41h
00454A5D jnp     short loc_454A67
00454A5F fstp    st
00454A61 fld     ds:flt_727398
00454A67 lea     eax, [edi+1Ch]
00454A6A fstp    dword ptr [edi+4]
00454A6D fld     ds:flt_727228
00454A73 push    eax
00454A74 lea     ecx, [edi+18h]
00454A77 fstp    dword ptr [edi+94h]
00454A7D push    ecx
00454A7E mov     eax, edi
00454A80 call    sub_482690
00454A85 mov     edx, [edi+9Ch]
00454A8B mov     esi, [esp+5Ch+var_4C]
00454A8F push    1F41h
00454A94 mov     [esi-18h], edx
00454A97 call    sub_5EECC0
00454A9C add     esp, 4
00454A9F add     eax, 0FFFFE890h
00454AA4 mov     [esi], eax
00454AA6 jns     short loc_454ACC
00454AA8 neg     eax
00454AAA mov     [esp+5Ch+var_4C], eax
00454AAE fild    [esp+5Ch+var_4C]
00454AB2 fdiv    ds:dbl_727990
00454AB8 fstp    dword ptr [edi+4]
00454ABB fld     ds:flt_727270
00454AC1 fstp    dword ptr [edi+8]
00454AC4 mov     dword ptr [esi], 0
00454ACA jmp     short loc_454AD1
00454ACC fldz
00454ACE fstp    dword ptr [edi+8]
00454AD1 mov     byte ptr [esp+5Ch+var_4], 0
00454AD6 cmp     [esp+5Ch+var_2C], 10h
00454ADB jb      short loc_454AEA
00454ADD mov     eax, [esp+5Ch+var_40]
00454AE1 push    eax; void *
00454AE2 call    ??3@YAXPAX@Z; operator delete(void *)
00454AE7 add     esp, 4
00454AEA mov     eax, [esp+5Ch+arg_4]
00454AEE inc     eax
00454AEF add     esi, 4
00454AF2 cmp     eax, 6
00454AF5 mov     [esp+5Ch+arg_4], eax
00454AF9 mov     [esp+5Ch+var_4C], esi
00454AFD jl      loc_454960
00454B03 lea     ecx, [ebp+130h]
00454B09 mov     [esp+5Ch+var_4C], 0
00454B11 mov     [esp+5Ch+arg_4], ecx
00454B15 mov     eax, [ebp+0A4h]
00454B1B mov     edx, [eax+944h]
00454B21 mov     edi, [edx+8]
00454B24 call    sub_483BC0
00454B29 mov     esi, eax
00454B2B push    0
00454B2D push    65h ; 'e'
00454B2F mov     [esi+60h], edi
00454B32 mov     dword ptr [esi+68h], 0
00454B39 call    sub_482AA0
00454B3E fld     ds:flt_727270
00454B44 mov     eax, dword_755E3C
00454B49 fst     [esp+64h+var_60]; float
00454B4D fstp    [esp+64h+var_64]; float
00454B50 add     eax, 650h
00454B55 push    esi; int
00454B56 mov     dword ptr [esi], 65h ; 'e'
00454B5C call    sub_480E40
00454B61 mov     edi, [esp+5Ch+var_4C]
00454B65 inc     edi
00454B66 push    edi; int
00454B67 lea     eax, [esp+60h+var_44]
00454B6B push    offset aAnimFlowerD; "anim_flower%d"
00454B70 push    eax; int
00454B71 call    sub_5F0BD0
00454B76 mov     edx, [esp+68h+var_40]
00454B7A add     esp, 0Ch
00454B7D cmp     [esp+5Ch+var_2C], 10h
00454B82 jnb     short loc_454B88
00454B84 lea     edx, [esp+5Ch+var_40]
00454B88 fldz
00454B8A mov     dword ptr [esi+10h], 3
00454B91 fcom    dword ptr [esi+8]
00454B94 mov     dword ptr [esi+5Ch], 0
00454B9B fnstsw  ax
00454B9D test    ah, 41h
00454BA0 jnp     short loc_454BAA
00454BA2 fstp    st
00454BA4 fld     ds:flt_727398
00454BAA lea     ecx, [esi+1Ch]
00454BAD fstp    dword ptr [esi+4]
00454BB0 fld     ds:flt_727228
00454BB6 push    ecx
00454BB7 lea     eax, [esi+18h]
00454BBA fstp    dword ptr [esi+94h]
00454BC0 push    eax
00454BC1 mov     eax, esi
00454BC3 call    sub_482690
00454BC8 fldz
00454BCA mov     edx, offset aSelectorscreen; "SelectorScreen_BG_Right"
00454BCF fstp    dword ptr [esi+8]
00454BD2 mov     eax, ebx
00454BD4 call    sub_482430
00454BD9 mov     ecx, [esi+9Ch]
00454BDF mov     edx, [esp+5Ch+arg_4]
00454BE3 mov     byte ptr [esi+64h], 0
00454BE7 cmp     [esp+5Ch+var_2C], 10h
00454BEC mov     [edx], ecx
00454BEE jb      short loc_454BFD
00454BF0 mov     eax, [esp+5Ch+var_40]
00454BF4 push    eax; void *
00454BF5 call    ??3@YAXPAX@Z; operator delete(void *)
00454BFA add     esp, 4
00454BFD add     [esp+5Ch+arg_4], 4
00454C02 cmp     edi, 3
00454C05 mov     [esp+5Ch+var_4C], edi
00454C09 jl      loc_454B15
00454C0F mov     eax, [ebp+0A4h]
00454C15 mov     ecx, [eax+944h]
00454C1B mov     edi, [ecx+8]
00454C1E call    sub_483BC0
00454C23 mov     esi, eax
00454C25 mov     [esi+60h], edi
00454C28 xor     edi, edi
00454C2A push    edi
00454C2B push    65h ; 'e'
00454C2D mov     [esi+68h], edi
00454C30 call    sub_482AA0
00454C35 fld     ds:flt_727270
00454C3B mov     eax, dword_755E3C
00454C40 fst     [esp+64h+var_60]; float
00454C44 fstp    [esp+64h+var_64]; float
00454C47 add     eax, 650h
00454C4C push    esi; int
00454C4D mov     dword ptr [esi], 65h ; 'e'
00454C53 call    sub_480E40
00454C58 fld     ds:flt_7271C8
00454C5E fstp    dword ptr [esi+8]
00454C61 lea     edx, [esi+1Ch]
00454C64 fldz
00454C66 push    edx
00454C67 fstp    dword ptr [esi+4]
00454C6A lea     eax, [esi+18h]
00454C6D fld     ds:flt_727228
00454C73 push    eax
00454C74 mov     edx, offset aAnimGrass; "anim_grass"
00454C79 fstp    dword ptr [esi+94h]
00454C7F mov     eax, esi
00454C81 mov     [esi+10h], edi
00454C84 mov     [esi+5Ch], edi
00454C87 call    sub_482690
00454C8C fldz
00454C8E fstp    dword ptr [esi+8]
00454C91 push    edi
00454C92 mov     esi, [esi+9Ch]
00454C98 mov     ecx, ebp
00454C9A mov     [ebp+13Ch], esi
00454CA0 mov     dword ptr [ebp+144h], 32h ; '2'
00454CAA call    sub_455420
00454CAF mov     ecx, [ebp+0A4h]
00454CB5 mov     edx, [ecx]
00454CB7 mov     eax, dword_752CBC
00454CBC mov     edx, [edx+0D8h]
00454CC2 push    eax
00454CC3 call    edx
00454CC5 push    198h; Size
00454CCA mov     [ebp+15Ch], edi
00454CD0 mov     [ebp+160h], edi
00454CD6 mov     [ebp+164h], edi
00454CDC mov     [ebp+168h], edi
00454CE2 mov     [ebp+16Ch], edi
00454CE8 call    ??2@YAPAXI@Z; operator new(uint)
00454CED add     esp, 4
00454CF0 mov     [esp+5Ch+arg_4], eax
00454CF4 mov     byte ptr [esp+5Ch+var_4], 15h
00454CF9 cmp     eax, edi
00454CFB jz      short loc_454D0C
00454CFD mov     ecx, [ebp+0A4h]
00454D03 push    ecx
00454D04 push    eax
00454D05 call    sub_53CBF0
00454D0A jmp     short loc_454D0E
00454D0C xor     eax, eax
00454D0E mov     byte ptr [esp+5Ch+var_4], 0
00454D13 mov     ecx, [ebp+0A4h]
00454D19 mov     [ebp+170h], eax
00454D1F mov     esi, [ecx+0C4h]
00454D25 mov     ecx, [ecx+0C0h]
00454D2B mov     edx, [eax]
00454D2D mov     edx, [edx+0A0h]
00454D33 push    esi
00454D34 push    ecx
00454D35 push    edi
00454D36 push    640h
00454D3B mov     ecx, eax
00454D3D call    edx
00454D3F push    0D0h; Size
00454D44 call    ??2@YAPAXI@Z; operator new(uint)
00454D49 add     esp, 4
00454D4C mov     [esp+5Ch+arg_4], eax
00454D50 mov     byte ptr [esp+5Ch+var_4], 16h
00454D55 cmp     eax, edi
00454D57 jz      short loc_454D69
00454D59 mov     ecx, [ebp+0A4h]
00454D5F push    ecx
00454D60 mov     esi, eax
00454D62 call    sub_408830
00454D67 jmp     short loc_454D6B
00454D69 xor     eax, eax
00454D6B mov     byte ptr [esp+5Ch+var_4], 0
00454D70 mov     ecx, [ebp+0A4h]
00454D76 mov     [ebp+174h], eax
00454D7C mov     ecx, [ecx+0C4h]
00454D82 mov     edx, [eax]
00454D84 mov     edx, [edx+0A4h]
00454D8A push    ecx
00454D8B push    320h
00454D90 mov     ecx, eax
00454D92 call    edx
00454D94 mov     eax, [ebp+0ACh]
00454D9A push    eax
00454D9B mov     ecx, ebp
00454D9D call    sub_5C0720
00454DA2 mov     ecx, [ebp+0B0h]
00454DA8 push    ecx
00454DA9 mov     ecx, ebp
00454DAB call    sub_5C0720
00454DB0 mov     edx, [ebp+0B4h]
00454DB6 push    edx
00454DB7 mov     ecx, ebp
00454DB9 call    sub_5C0720
00454DBE mov     eax, [ebp+0B8h]
00454DC4 push    eax
00454DC5 mov     ecx, ebp
00454DC7 call    sub_5C0720
00454DCC mov     ecx, [ebp+0BCh]
00454DD2 push    ecx
00454DD3 mov     ecx, ebp
00454DD5 call    sub_5C0720
00454DDA mov     edx, [ebp+0C0h]
00454DE0 push    edx
00454DE1 mov     ecx, ebp
00454DE3 call    sub_5C0720
00454DE8 mov     eax, [ebp+0D0h]
00454DEE push    eax
00454DEF mov     ecx, ebp
00454DF1 call    sub_5C0720
00454DF6 mov     ecx, [ebp+0D4h]
00454DFC push    ecx
00454DFD mov     ecx, ebp
00454DFF call    sub_5C0720
00454E04 mov     edx, [ebp+0DCh]
00454E0A push    edx
00454E0B mov     ecx, ebp
00454E0D call    sub_5C0720
00454E12 mov     eax, [ebp+0D8h]
00454E18 push    eax
00454E19 mov     ecx, ebp
00454E1B call    sub_5C0720
00454E20 mov     ecx, [ebp+0E0h]
00454E26 push    ecx
00454E27 mov     ecx, ebp
00454E29 call    sub_5C0720
00454E2E mov     edx, [ebp+0C4h]
00454E34 push    edx
00454E35 mov     ecx, ebp
00454E37 call    sub_5C0720
00454E3C mov     eax, [ebp+170h]
00454E42 push    eax
00454E43 mov     ecx, ebp
00454E45 call    sub_5C0720
00454E4A mov     ecx, [ebp+0C8h]
00454E50 push    ecx
00454E51 mov     ecx, ebp
00454E53 call    sub_5C0720
00454E58 mov     edx, [ebp+174h]
00454E5E push    edx
00454E5F mov     ecx, ebp
00454E61 call    sub_5C0720
00454E66 mov     eax, [ebp+0CCh]
00454E6C push    eax
00454E6D mov     ecx, ebp
00454E6F call    sub_5C0720
00454E74 mov     ecx, [ebp+0E4h]
00454E7A push    ecx
00454E7B mov     ecx, ebp
00454E7D call    sub_5C0720
00454E82 mov     edx, [ebp+0A8h]
00454E88 push    edx
00454E89 mov     ecx, ebp
00454E8B call    sub_5C0720
00454E90 mov     ecx, [esp+5Ch+var_C]
00454E94 pop     edi
00454E95 pop     esi
00454E96 mov     eax, ebp
00454E98 pop     ebp
00454E99 mov     large fs:0, ecx
00454EA0 pop     ebx
00454EA1 add     esp, 4Ch
00454EA4 retn    8
006ED670 mov     ecx, [ebp+4]
006ED673 jmp     sub_5C4A90
006ED678 lea     ecx, [ebp-44h]; void *
006ED67B jmp     sub_401E90
006ED680 lea     ecx, [ebp-44h]; void *
006ED683 jmp     sub_401330
006ED688 lea     ecx, [ebp-44h]; void *
006ED68B jmp     sub_401330
006ED690 lea     ecx, [ebp-44h]; void *
006ED693 jmp     sub_401330
006ED698 lea     ecx, [ebp-44h]; void *
006ED69B jmp     sub_401330
006ED6A0 lea     ecx, [ebp-44h]; void *
006ED6A3 jmp     sub_401330
006ED6A8 lea     ecx, [ebp-44h]; void *
006ED6AB jmp     sub_401330
006ED6B0 lea     ecx, [ebp-44h]; void *
006ED6B3 jmp     sub_401330
006ED6B8 lea     ecx, [ebp-44h]; void *
006ED6BB jmp     sub_401330
006ED6C0 lea     ecx, [ebp-44h]; void *
006ED6C3 jmp     sub_401330
006ED6C8 lea     ecx, [ebp-44h]; void *
006ED6CB jmp     sub_401330
006ED6D0 lea     ecx, [ebp-44h]; void *
006ED6D3 jmp     sub_401330
006ED6D8 lea     ecx, [ebp-44h]; void *
006ED6DB jmp     sub_401330
006ED6E0 mov     eax, [ebp+8]
006ED6E3 push    eax; void *
006ED6E4 call    ??3@YAXPAX@Z; operator delete(void *)
006ED6E9 pop     ecx
006ED6EA retn
006ED6EB lea     ecx, [ebp-44h]; void *
006ED6EE jmp     sub_401330
006ED6F3 lea     ecx, [ebp-44h]; void *
006ED6F6 jmp     sub_401330
006ED6FB lea     ecx, [ebp-44h]; void *
006ED6FE jmp     sub_401330
006ED703 lea     ecx, [ebp-44h]; void *
006ED706 jmp     sub_401E90
006ED70B lea     ecx, [ebp-28h]; void *
006ED70E jmp     sub_401E90
006ED713 lea     ecx, [ebp-28h]; void *
006ED716 jmp     sub_401E90
006ED71B mov     eax, [ebp+8]
006ED71E push    eax; void *
006ED71F call    ??3@YAXPAX@Z; operator delete(void *)
006ED724 pop     ecx
006ED725 retn
006ED726 mov     eax, [ebp+8]
006ED729 push    eax; void *
006ED72A call    ??3@YAXPAX@Z; operator delete(void *)
006ED72F pop     ecx
006ED730 retn
006ED731 mov     eax, offset stru_73F850
006ED736 jmp     ___CxxFrameHandler3
