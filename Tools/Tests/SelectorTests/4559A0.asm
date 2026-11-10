004559A0 push    ebp
004559A1 mov     ebp, esp
004559A3 and     esp, 0FFFFFFF8h
004559A6 push    0FFFFFFFFh
004559A8 push    offset SEH_4559A0
004559AD mov     eax, large fs:0
004559B3 push    eax
004559B4 mov     large fs:0, esp
004559BB sub     esp, 68h
004559BE push    ebx
004559BF push    esi
004559C0 mov     esi, [ebp+arg_0]
004559C3 push    edi
004559C4 mov     edi, ecx
004559C6 mov     ebx, 1
004559CB push    ebx
004559CC mov     ecx, esi
004559CE mov     [esp+84h+var_64], edi
004559D2 call    sub_592970
004559D7 mov     eax, [edi+0A4h]
004559DD xor     edx, edx
004559DF cmp     [eax+950h], edx
004559E5 jz      loc_456096
004559EB cmp     [edi+0F3h], dl
004559F1 jnz     loc_455CA9
004559F7 mov     ecx, [edi+0ACh]
004559FD cmp     [ecx+68h], dl
00455A00 jz      short loc_455A11
00455A02 cmp     [ecx+69h], dl
00455A05 jz      short loc_455A11
00455A07 mov     [esp+80h+var_5C], ebx
00455A0B mov     [esp+80h+var_60], ebx
00455A0F jmp     short loc_455A19
00455A11 mov     [esp+80h+var_5C], edx
00455A15 mov     [esp+80h+var_60], edx
00455A19 mov     esi, [edi+0FCh]
00455A1F mov     eax, [eax+944h]
00455A25 mov     eax, [eax+8]
00455A28 and     esi, 0FFFFh
00455A2E lea     esi, [esi+esi*4]
00455A31 shl     esi, 5
00455A34 add     esi, [eax]
00455A36 push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00455A3B mov     ebx, esi
00455A3D call    sub_4823E0
00455A42 fld     ds:flt_708310
00455A48 fst     [esp+80h+var_3C]
00455A4C mov     ebx, eax
00455A4E fst     [esp+80h+var_38]
00455A52 xor     eax, eax
00455A54 fst     [esp+80h+var_34]
00455A58 mov     [esp+80h+var_18], eax
00455A5C fst     [esp+80h+var_30]
00455A60 mov     [esp+80h+var_1C], eax
00455A64 fst     [esp+80h+var_2C]
00455A68 push    esi
00455A69 fst     [esp+84h+var_28]
00455A6D lea     eax, [esp+84h+var_3C]
00455A71 fst     [esp+84h+var_24]
00455A75 mov     [esp+84h+var_14], offset Class
00455A7D fstp    [esp+84h+var_20]
00455A81 call    sub_4812C0
00455A86 fild    [esp+80h+var_5C]
00455A8A mov     ecx, [edi+14Ch]
00455A90 lea     edx, [ecx-1]
00455A93 mov     eax, 66666667h
00455A98 fadd    [esp+80h+var_3C]
00455A9C imul    edx
00455A9E fstp    [esp+80h+var_68]
00455AA2 fild    [esp+80h+var_60]
00455AA6 fadd    [esp+80h+var_38]
00455AAA sar     edx, 2
00455AAD mov     eax, edx
00455AAF fstp    [esp+80h+var_6C]
00455AB3 shr     eax, 1Fh
00455AB6 fld     [esp+80h+var_68]
00455ABA lea     eax, [edx+eax+1]
00455ABE cmp     eax, 1
00455AC1 fstp    [esp+80h+var_70]
00455AC5 fld     [esp+80h+var_6C]
00455AC9 fstp    [esp+80h+var_5C]
00455ACD jg      short loc_455ADA
00455ACF mov     ebx, 1
00455AD4 mov     [esp+80h+var_74], ebx
00455AD8 jmp     short loc_455AF3
00455ADA cmp     eax, 6
00455ADD jl      short loc_455AED
00455ADF mov     [esp+80h+var_74], 6
00455AE7 mov     ebx, [esp+80h+var_74]
00455AEB jmp     short loc_455AF3
00455AED mov     [esp+80h+var_74], eax
00455AF1 mov     ebx, eax
00455AF3 mov     esi, [edi+0A4h]
00455AF9 lea     edx, [ebx+ebx*4]
00455AFC add     edx, edx
00455AFE sub     ecx, edx
00455B00 add     ecx, 0Ah
00455B03 mov     [esp+80h+var_60], ecx
00455B07 call    sub_4631D0
00455B0C test    al, al
00455B0E jz      short loc_455B3A
00455B10 cmp     dword ptr [edi+14Ch], 19h
00455B17 jge     short loc_455B28
00455B19 mov     eax, [edi+0A4h]
00455B1F call    sub_461560
00455B24 test    al, al
00455B26 jz      short loc_455B3A
00455B28 mov     [esp+80h+var_74], 3
00455B30 mov     [esp+80h+var_60], 4
00455B38 jmp     short loc_455B6F
00455B3A fld1
00455B3C cmp     ebx, 1
00455B3F jnz     short loc_455B4D
00455B41 fld     [esp+80h+var_6C]
00455B45 fadd    st, st(1)
00455B47 fstp    [esp+80h+var_6C]
00455B4B jmp     short loc_455B5C
00455B4D cmp     ebx, 4
00455B50 jnz     short loc_455B5C
00455B52 fld     [esp+80h+var_68]
00455B56 fsub    st, st(1)
00455B58 fstp    [esp+80h+var_68]
00455B5C cmp     [esp+80h+var_60], 3
00455B61 jnz     short loc_455B6D
00455B63 fsubr   [esp+80h+var_70]
00455B67 fstp    [esp+80h+var_70]
00455B6B jmp     short loc_455B6F
00455B6D fstp    st
00455B6F mov     ebx, [ebp+arg_0]
00455B72 push    1
00455B74 mov     ecx, ebx
00455B76 call    sub_592910
00455B7B mov     esi, [edi+0ACh]
00455B81 mov     eax, [esi+7Ch]
00455B84 sub     eax, [esi+78h]
00455B87 add     esi, 6Ch ; 'l'
00455B8A sar     eax, 4
00455B8D cmp     eax, 5
00455B90 ja      short loc_455B97
00455B92 call    __invalid_parameter_noinfo
00455B97 mov     ecx, [esi+0Ch]
00455B9A add     ecx, 50h ; 'P'
00455B9D push    ecx
00455B9E mov     ecx, ebx
00455BA0 call    sub_592880
00455BA5 fld     [esp+80h+var_6C]
00455BA9 fadd    ds:dbl_7273F8
00455BAF mov     edx, [esp+80h+var_74]
00455BB3 mov     esi, dword_7537AC
00455BB9 push    edx
00455BBA fstp    [esp+84h+var_6C]
00455BBE sub     esp, 8
00455BC1 fld     [esp+8Ch+var_6C]
00455BC5 fstp    [esp+8Ch+var_88]
00455BC9 fld     [esp+8Ch+var_68]
00455BCD fadd    ds:dbl_727980
00455BD3 fstp    [esp+8Ch+var_68]
00455BD7 fld     [esp+8Ch+var_68]
00455BDB fstp    [esp+8Ch+var_8C]
00455BDE push    ebx
00455BDF call    sub_527AF0
00455BE4 mov     eax, [esp+90h+var_60]
00455BE8 add     esp, 10h
00455BEB cmp     eax, 0Ah
00455BEE jge     short loc_455C1E
00455BF0 fld     [esp+80h+var_5C]
00455BF4 push    eax
00455BF5 fadd    ds:dbl_727218
00455BFB sub     esp, 8
00455BFE fstp    [esp+8Ch+var_5C]
00455C02 fld     [esp+8Ch+var_5C]
00455C06 fstp    [esp+8Ch+var_88]
00455C0A fld     [esp+8Ch+var_70]
00455C0E fadd    ds:dbl_727978
00455C14 fstp    [esp+8Ch+var_5C]
00455C18 fld     [esp+8Ch+var_5C]
00455C1C jmp     short loc_455C8C
00455C1E jnz     short loc_455C9E
00455C20 fld     [esp+80h+var_5C]
00455C24 mov     esi, dword_7537AC
00455C2A fadd    ds:dbl_727218
00455C30 push    1
00455C32 sub     esp, 8
00455C35 fstp    [esp+8Ch+var_60]
00455C39 fld     [esp+8Ch+var_60]
00455C3D fstp    [esp+8Ch+var_88]
00455C41 fld     [esp+8Ch+var_70]
00455C45 fadd    ds:dbl_727978
00455C4B fstp    [esp+8Ch+var_60]
00455C4F fld     [esp+8Ch+var_60]
00455C53 fstp    [esp+8Ch+var_8C]
00455C56 push    ebx
00455C57 call    sub_527AF0
00455C5C fld     [esp+90h+var_5C]
00455C60 fadd    ds:dbl_7278B0
00455C66 add     esp, 10h
00455C69 push    0
00455C6B sub     esp, 8
00455C6E fstp    [esp+8Ch+var_5C]
00455C72 fld     [esp+8Ch+var_5C]
00455C76 fstp    [esp+8Ch+var_88]
00455C7A fld     [esp+8Ch+var_70]
00455C7E fadd    ds:dbl_727970
00455C84 fstp    [esp+8Ch+var_5C]
00455C88 fld     [esp+8Ch+var_5C]
00455C8C mov     esi, dword_7537AC
00455C92 fstp    [esp+8Ch+var_8C]
00455C95 push    ebx
00455C96 call    sub_527AF0
00455C9B add     esp, 10h
00455C9E push    0
00455CA0 mov     ecx, ebx
00455CA2 call    sub_592910
00455CA7 mov     esi, ebx
00455CA9 mov     eax, [edi+0D8h]
00455CAF cmp     byte ptr [eax+64h], 0
00455CB3 jz      short loc_455D0D
00455CB5 mov     ecx, [edi+0A4h]
00455CBB mov     edx, [ecx+940h]
00455CC1 push    edx
00455CC2 call    sub_533FE0
00455CC7 test    al, al
00455CC9 jz      short loc_455D0D
00455CCB mov     eax, [edi+0D8h]
00455CD1 mov     ebx, [eax+40h]
00455CD4 mov     eax, [eax+44h]
00455CD7 add     eax, 24h ; '$'
00455CDA push    eax; int
00455CDB sub     ebx, 2B6h
00455CE1 mov     [esp+84h+var_5C], eax
00455CE5 mov     eax, dword_753754
00455CEA push    ebx; int
00455CEB push    eax; Concurrency::details::ExecutionResource *
00455CEC mov     ecx, esi
00455CEE call    sub_592F90
00455CF3 mov     ecx, [esp+80h+var_5C]
00455CF7 mov     edx, dword_7534FC
00455CFD add     ecx, 9
00455D00 push    ecx; int
00455D01 add     ebx, 11h
00455D04 push    ebx; int
00455D05 push    edx; Concurrency::details::ExecutionResource *
00455D06 mov     ecx, esi
00455D08 call    sub_592F90
00455D0D mov     ecx, [edi+0A4h]
00455D13 mov     ecx, [ecx+944h]
00455D19 mov     eax, [edi+140h]
00455D1F test    ecx, ecx
00455D21 jz      short loc_455D6D
00455D23 mov     ecx, [ecx+8]
00455D26 test    eax, eax
00455D28 jz      short loc_455D6D
00455D2A movzx   edx, ax
00455D2D cmp     edx, [ecx+8]
00455D30 jnb     short loc_455D6D
00455D32 lea     ebx, [edx+edx*4]
00455D35 shl     ebx, 5
00455D38 add     ebx, [ecx]
00455D3A cmp     [ebx+9Ch], eax
00455D40 jnz     short loc_455D6D
00455D42 push    230h
00455D47 push    320h
00455D4C push    0
00455D4E push    0
00455D50 mov     ecx, esi
00455D52 call    sub_593920
00455D57 mov     edi, ebx
00455D59 call    sub_482230
00455D5E mov     ecx, [ebp+arg_0]
00455D61 call    sub_5938D0
00455D66 mov     edi, [esp+80h+var_64]
00455D6A mov     esi, [ebp+arg_0]
00455D6D mov     eax, 0FFFFFCE0h
00455D72 sub     eax, [edi+40h]
00455D75 push    0
00455D77 push    eax
00455D78 mov     ecx, esi
00455D7A call    sub_593AA0
00455D7F mov     ecx, [edi+0A4h]
00455D85 mov     edi, [edi+13Ch]
00455D8B mov     edx, [ecx+944h]
00455D91 mov     eax, [edx+8]
00455D94 and     edi, 0FFFFh
00455D9A lea     edi, [edi+edi*4]
00455D9D shl     edi, 5
00455DA0 add     edi, [eax]
00455DA2 call    sub_482230
00455DA7 mov     ebx, [esp+80h+var_64]
00455DAB mov     eax, [ebx+40h]
00455DAE mov     esi, [ebp+arg_0]
00455DB1 push    0
00455DB3 add     eax, 320h
00455DB8 push    eax
00455DB9 mov     ecx, esi
00455DBB call    sub_593AA0
00455DC0 lea     ecx, [ebx+130h]
00455DC6 mov     [esp+80h+var_5C], ecx
00455DCA mov     [esp+80h+var_60], 3
00455DD2 mov     edx, [ebx+0A4h]
00455DD8 mov     eax, [edx+944h]
00455DDE mov     ecx, [eax+8]
00455DE1 mov     edx, [esp+80h+var_5C]
00455DE5 mov     eax, [edx]
00455DE7 and     eax, 0FFFFh
00455DEC lea     edi, [eax+eax*4]
00455DEF shl     edi, 5
00455DF2 add     edi, [ecx]
00455DF4 call    sub_482230
00455DF9 add     [esp+80h+var_5C], 4
00455DFE sub     [esp+80h+var_60], 1
00455E03 jnz     short loc_455DD2
00455E05 mov     eax, [ebx+0A4h]
00455E0B cmp     byte ptr [eax+11Ch], 0
00455E12 jz      loc_455F19
00455E18 mov     ecx, dword_752B48
00455E1E push    ecx
00455E1F mov     ecx, esi
00455E21 call    sub_592860
00455E26 push    0C8h
00455E2B push    0C8h
00455E30 push    0C8h
00455E35 lea     ecx, [esp+8Ch+var_58]
00455E39 call    sub_55BBB0
00455E3E push    eax
00455E3F mov     ecx, esi
00455E41 call    sub_592880
00455E46 xor     edx, edx
00455E48 xor     edi, edi
00455E4A cmp     byte_755DF5, dl
00455E50 jz      short loc_455EAB
00455E52 push    2Eh ; '.'; int
00455E54 push    offset off_70D8A0; Source
00455E59 lea     ecx, [esp+88h+var_3C]
00455E5D mov     [esp+88h+var_24], 7
00455E65 mov     [esp+88h+var_28], edi
00455E69 mov     word ptr [esp+88h+var_38], dx
00455E6E call    sub_40D1E0
00455E73 push    252h
00455E78 push    1Bh
00455E7A lea     eax, [esp+88h+var_3C]
00455E7E push    eax
00455E7F mov     ecx, esi
00455E81 mov     [esp+8Ch+var_4], edi
00455E88 call    sub_592F40
00455E8D mov     [esp+80h+var_4], 0FFFFFFFFh
00455E95 cmp     [esp+80h+var_24], 8
00455E9A jb      short loc_455F19
00455E9C mov     ecx, [esp+80h+var_38]
00455EA0 push    ecx; void *
00455EA1 call    ??3@YAXPAX@Z; operator delete(void *)
00455EA6 add     esp, 4
00455EA9 jmp     short loc_455F19
00455EAB push    1Dh; int
00455EAD push    offset off_70D900; Source
00455EB2 lea     ecx, [esp+88h+var_58]
00455EB6 mov     [esp+88h+var_40], 7
00455EBE mov     [esp+88h+var_44], edi
00455EC2 mov     word ptr [esp+88h+var_54], dx
00455EC7 call    sub_40D1E0
00455ECC push    252h
00455ED1 push    1Bh
00455ED3 lea     eax, [esp+88h+var_58]
00455ED7 push    eax
00455ED8 mov     ecx, esi
00455EDA mov     [esp+8Ch+var_4], 1
00455EE5 call    sub_592F40
00455EEA mov     [esp+80h+var_4], 0FFFFFFFFh
00455EF2 cmp     [esp+80h+var_40], 8
00455EF7 jb      short loc_455F06
00455EF9 mov     ecx, [esp+80h+var_54]
00455EFD push    ecx; void *
00455EFE call    ??3@YAXPAX@Z; operator delete(void *)
00455F03 add     esp, 4
00455F06 xor     edx, edx
00455F08 mov     [esp+80h+var_40], 7
00455F10 mov     [esp+80h+var_44], edi
00455F14 mov     word ptr [esp+80h+var_54], dx
00455F19 mov     eax, [ebx+0A4h]
00455F1F mov     ebx, [ebx+0FCh]
00455F25 mov     ecx, [eax+944h]
00455F2B mov     edx, [ecx+8]
00455F2E and     ebx, 0FFFFh
00455F34 lea     esi, [ebx+ebx*4]
00455F37 shl     esi, 5
00455F3A add     esi, [edx]
00455F3C push    offset aSelectorscreen_0; "SelectorScreen_BG_Left"
00455F41 mov     ebx, esi
00455F43 call    sub_4823E0
00455F48 fld     ds:flt_708310
00455F4E fst     [esp+80h+var_3C]
00455F52 mov     ebx, eax
00455F54 fst     [esp+80h+var_38]
00455F58 xor     eax, eax
00455F5A fst     [esp+80h+var_34]
00455F5E mov     [esp+80h+var_18], eax
00455F62 fst     [esp+80h+var_30]
00455F66 mov     [esp+80h+var_1C], eax
00455F6A fst     [esp+80h+var_2C]
00455F6E push    esi
00455F6F fst     [esp+84h+var_28]
00455F73 lea     eax, [esp+84h+var_3C]
00455F77 fst     [esp+84h+var_24]
00455F7B mov     [esp+84h+var_14], offset Class
00455F83 fstp    [esp+84h+var_20]
00455F87 call    sub_4812C0
00455F8C mov     edi, [esp+80h+var_64]
00455F90 cmp     byte ptr [edi+158h], 0
00455F97 jz      loc_456087
00455F9D mov     ebx, [edi+0A4h]
00455FA3 mov     eax, [ebx+950h]
00455FA9 test    eax, eax
00455FAB jz      short loc_45600D
00455FAD cmp     dword ptr [eax+54h], 0
00455FB1 jle     short loc_45600D
00455FB3 push    0
00455FB5 call    sub_4630C0
00455FBA mov     esi, 30h ; '0'
00455FBF push    1
00455FC1 sub     esi, eax
00455FC3 call    sub_4630C0
00455FC8 push    3
00455FCA sub     esi, eax
00455FCC call    sub_4630C0
00455FD1 sub     esi, eax
00455FD3 test    esi, esi
00455FD5 jg      short loc_45600D
00455FD7 fld     [esp+80h+var_38]
00455FDB mov     eax, [ebp+arg_0]
00455FDE fadd    ds:dbl_727968
00455FE4 push    1
00455FE6 sub     esp, 8
00455FE9 fstp    [esp+8Ch+var_5C]
00455FED fld     [esp+8Ch+var_5C]
00455FF1 fstp    [esp+8Ch+var_88]
00455FF5 fld     [esp+8Ch+var_3C]
00455FF9 fadd    ds:dbl_7273A0
00455FFF fstp    [esp+8Ch+var_5C]
00456003 fld     [esp+8Ch+var_5C]
00456007 fstp    [esp+8Ch+var_8C]
0045600A push    eax
0045600B jmp     short loc_456041
0045600D fld     [esp+80h+var_38]
00456011 mov     ecx, [ebp+arg_0]
00456014 fadd    ds:dbl_727968
0045601A push    0
0045601C sub     esp, 8
0045601F fstp    [esp+8Ch+var_5C]
00456023 fld     [esp+8Ch+var_5C]
00456027 fstp    [esp+8Ch+var_88]
0045602B fld     [esp+8Ch+var_3C]
0045602F fadd    ds:dbl_7273A0
00456035 fstp    [esp+8Ch+var_5C]
00456039 fld     [esp+8Ch+var_5C]
0045603D fstp    [esp+8Ch+var_8C]
00456040 push    ecx
00456041 mov     esi, dword_752E98
00456047 call    sub_527AF0
0045604C mov     edx, [edi+0A4h]
00456052 mov     ecx, [edi+0F8h]
00456058 mov     eax, [edx+944h]
0045605E mov     edx, [eax]
00456060 add     esp, 10h
00456063 test    ecx, ecx
00456065 jz      short loc_456087
00456067 movzx   eax, cx
0045606A cmp     eax, [edx+8]
0045606D jnb     short loc_456087
0045606F imul    eax, 2Ch ; ','
00456072 add     eax, [edx]
00456074 cmp     [eax+28h], ecx
00456077 jnz     short loc_456087
00456079 mov     ebx, [ebp+arg_0]
0045607C mov     edi, eax
0045607E call    sub_52D8C0
00456083 mov     edi, [esp+80h+var_64]
00456087 mov     ecx, [edi+154h]
0045608D mov     ebx, [ebp+arg_0]
00456090 push    ecx
00456091 call    sub_530180
00456096 mov     ecx, [esp+80h+var_C]
0045609A pop     edi
0045609B pop     esi
0045609C mov     large fs:0, ecx
004560A3 pop     ebx
004560A4 mov     esp, ebp
004560A6 pop     ebp
004560A7 retn    4
006ED3D0 lea     ecx, [ebp+var_3C]; void *
006ED3D3 jmp     sub_401330
006ED3D8 lea     ecx, [ebp+var_58]; void *
006ED3DB jmp     sub_401330
006ED3E0 mov     eax, offset stru_73F488
006ED3E5 jmp     ___CxxFrameHandler3
