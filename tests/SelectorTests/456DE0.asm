00456DE0 sub     esp, 8
00456DE3 push    ebx
00456DE4 push    ebp
00456DE5 push    esi
00456DE6 mov     esi, [esp+14h+arg_0]
00456DEA push    edi
00456DEB mov     edi, ecx
00456DED mov     eax, [edi+0A4h]
00456DF3 mov     ecx, [eax+9B4h]
00456DF9 push    esi
00456DFA push    ecx
00456DFB call    sub_531F80
00456E00 test    al, al
00456E02 jz      short loc_456E6A
00456E04 mov     edi, [edi+0A4h]
00456E0A cmp     byte ptr [edi+9E9h], 0
00456E11 jnz     loc_4571D7
00456E17 fldz
00456E19 mov     ecx, dword_755E50
00456E1F mov     edi, [edi+888h]
00456E25 fst     [esp+18h+arg_0]
00456E29 fcomp   dword ptr [ecx+1D8h]
00456E2F fnstsw  ax
00456E31 test    ah, 44h
00456E34 jnp     short loc_456E4C
00456E36 fld     dword ptr [ecx+1D8h]
00456E3C push    ecx
00456E3D fstp    [esp+1Ch+var_1C]; float
00456E40 call    sub_5EECE0
00456E45 fstp    [esp+1Ch+arg_0]
00456E49 add     esp, 4
00456E4C fld     [esp+18h+arg_0]
00456E50 push    ecx
00456E51 mov     eax, 9
00456E56 fstp    [esp+1Ch+var_1C]; float
00456E59 mov     ecx, edi
00456E5B call    sub_52A5A0
00456E60 pop     edi
00456E61 pop     esi
00456E62 pop     ebp
00456E63 pop     ebx
00456E64 add     esp, 8
00456E67 retn    4
00456E6A mov     edx, [edi+0A4h]
00456E70 mov     eax, [edx+9B8h]
00456E76 push    esi
00456E77 push    eax; int
00456E78 call    sub_531F80
00456E7D test    al, al
00456E7F jnz     loc_457135
00456E85 mov     ecx, [edi+0A4h]
00456E8B mov     edx, [ecx+9BCh]
00456E91 push    esi
00456E92 push    edx
00456E93 call    sub_531F80
00456E98 test    al, al
00456E9A jnz     loc_457135
00456EA0 mov     eax, [edi+0A4h]
00456EA6 mov     ecx, [eax+9C0h]
00456EAC push    esi
00456EAD push    ecx
00456EAE call    sub_531F80
00456EB3 test    al, al
00456EB5 jnz     loc_4570BD
00456EBB mov     edx, [edi+0A4h]
00456EC1 mov     eax, [edx+9C4h]
00456EC7 push    esi
00456EC8 push    eax
00456EC9 call    sub_531F80
00456ECE test    al, al
00456ED0 jnz     loc_4570BD
00456ED6 mov     ecx, [edi+0A4h]
00456EDC mov     edx, [ecx+9C8h]
00456EE2 push    esi
00456EE3 push    edx
00456EE4 call    sub_531F80
00456EE9 test    al, al
00456EEB jz      short loc_456F24
00456EED mov     ecx, [edi+0A4h]
00456EF3 mov     eax, [ecx]
00456EF5 mov     edx, dword_752BB8
00456EFB mov     eax, [eax+0D8h]
00456F01 push    edx
00456F02 call    eax
00456F04 mov     edi, [edi+0A4h]
00456F0A cmp     byte ptr [edi+9DEh], 0
00456F11 setz    cl
00456F14 mov     [edi+9DEh], cl
00456F1A pop     edi
00456F1B pop     esi
00456F1C pop     ebp
00456F1D pop     ebx
00456F1E add     esp, 8
00456F21 retn    4
00456F24 mov     edx, [edi+0A4h]
00456F2A mov     eax, [edx+9CCh]
00456F30 push    esi
00456F31 push    eax
00456F32 call    sub_531F80
00456F37 test    al, al
00456F39 jz      short loc_456F92
00456F3B mov     ecx, [edi+0A4h]
00456F41 mov     eax, ecx
00456F43 call    sub_463840
00456F48 test    al, al
00456F4A jz      short loc_456F78
00456F4C mov     esi, 41h ; 'A'
00456F51 mov     eax, ecx
00456F53 call    sub_460A20
00456F58 mov     edi, [edi+0A4h]
00456F5E cmp     byte ptr [edi+9DFh], 0
00456F65 setz    cl
00456F68 mov     [edi+9DFh], cl
00456F6E pop     edi
00456F6F pop     esi
00456F70 pop     ebp
00456F71 pop     ebx
00456F72 add     esp, 8
00456F75 retn    4
00456F78 mov     edx, [ecx]
00456F7A mov     eax, dword_753850
00456F7F pop     edi
00456F80 pop     esi
00456F81 pop     ebp
00456F82 pop     ebx
00456F83 add     esp, 8
00456F86 mov     [esp+arg_0], eax
00456F8A mov     edx, [edx+0D8h]
00456F90 jmp     edx
00456F92 mov     eax, [edi+0A4h]
00456F98 mov     ecx, [eax+9D0h]
00456F9E push    esi
00456F9F push    ecx
00456FA0 call    sub_531F80
00456FA5 mov     ecx, [edi+0A4h]
00456FAB test    al, al
00456FAD jz      short loc_457001
00456FAF mov     eax, ecx
00456FB1 call    sub_463860
00456FB6 test    al, al
00456FB8 jz      short loc_456FE6
00456FBA mov     esi, 64h ; 'd'
00456FBF mov     eax, ecx
00456FC1 call    sub_460A20
00456FC6 mov     edi, [edi+0A4h]
00456FCC cmp     byte ptr [edi+9E0h], 0
00456FD3 setz    dl
00456FD6 mov     [edi+9E0h], dl
00456FDC pop     edi
00456FDD pop     esi
00456FDE pop     ebp
00456FDF pop     ebx
00456FE0 add     esp, 8
00456FE3 retn    4
00456FE6 mov     eax, [ecx]
00456FE8 mov     edx, dword_753850
00456FEE pop     edi
00456FEF pop     esi
00456FF0 pop     ebp
00456FF1 pop     ebx
00456FF2 add     esp, 8
00456FF5 mov     [esp+arg_0], edx
00456FF9 mov     eax, [eax+0D8h]
00456FFF jmp     eax
00457001 mov     edx, [ecx+9D4h]
00457007 push    esi
00457008 push    edx
00457009 call    sub_531F80
0045700E test    al, al
00457010 mov     eax, [edi+0A4h]
00457016 jz      short loc_457072
00457018 call    sub_463880
0045701D mov     ecx, [edi+0A4h]
00457023 test    al, al
00457025 jz      short loc_457058
00457027 mov     eax, [ecx]
00457029 mov     edx, dword_7531C4
0045702F mov     eax, [eax+0D8h]
00457035 push    edx
00457036 call    eax
00457038 mov     edi, [edi+0A4h]
0045703E cmp     byte ptr [edi+9E1h], 0
00457045 setz    cl
00457048 mov     [edi+9E1h], cl
0045704E pop     edi
0045704F pop     esi
00457050 pop     ebp
00457051 pop     ebx
00457052 add     esp, 8
00457055 retn    4
00457058 mov     edx, [ecx]
0045705A mov     eax, dword_753850
0045705F pop     edi
00457060 pop     esi
00457061 pop     ebp
00457062 pop     ebx
00457063 add     esp, 8
00457066 mov     [esp+arg_0], eax
0045706A mov     edx, [edx+0D8h]
00457070 jmp     edx
00457072 mov     ecx, [eax+9D8h]
00457078 push    esi
00457079 push    ecx
0045707A call    sub_531F80
0045707F test    al, al
00457081 jz      loc_4571D7
00457087 mov     ecx, [edi+0A4h]
0045708D mov     edx, [ecx]
0045708F mov     eax, dword_753244
00457094 mov     edx, [edx+0D8h]
0045709A push    eax
0045709B call    edx
0045709D mov     edi, [edi+0A4h]
004570A3 cmp     byte ptr [edi+9E2h], 0
004570AA setz    al
004570AD mov     [edi+9E2h], al
004570B3 pop     edi
004570B4 pop     esi
004570B5 pop     ebp
004570B6 pop     ebx
004570B7 add     esp, 8
004570BA retn    4
004570BD mov     eax, [edi+0A4h]
004570C3 cmp     byte ptr [eax+9E9h], 0
004570CA jnz     short loc_457115
004570CC fldz
004570CE mov     ecx, dword_755E50
004570D4 mov     esi, [eax+888h]
004570DA fst     [esp+18h+arg_0]
004570DE fcomp   dword ptr [ecx+344h]
004570E4 fnstsw  ax
004570E6 test    ah, 44h
004570E9 jnp     short loc_457101
004570EB fld     dword ptr [ecx+344h]
004570F1 push    ecx
004570F2 fstp    [esp+1Ch+var_1C]; float
004570F5 call    sub_5EECE0
004570FA fstp    [esp+1Ch+arg_0]
004570FE add     esp, 4
00457101 fld     [esp+18h+arg_0]
00457105 push    ecx
00457106 mov     eax, 10h
0045710B fstp    [esp+1Ch+var_1C]; float
0045710E mov     ecx, esi
00457110 call    sub_52A5A0
00457115 mov     edi, [edi+0A4h]
0045711B cmp     byte ptr [edi+9DDh], 0
00457122 setz    cl
00457125 mov     [edi+9DDh], cl
0045712B pop     edi
0045712C pop     esi
0045712D pop     ebp
0045712E pop     ebx
0045712F add     esp, 8
00457132 retn    4
00457135 mov     eax, [edi+0A4h]
0045713B cmp     byte ptr [eax+9E9h], 0
00457142 jnz     short loc_45718D
00457144 fldz
00457146 mov     ecx, dword_755E50
0045714C mov     esi, [eax+888h]
00457152 fst     [esp+18h+arg_0]
00457156 fcomp   dword ptr [ecx+0FDCh]
0045715C fnstsw  ax
0045715E test    ah, 44h
00457161 jnp     short loc_457179
00457163 fld     dword ptr [ecx+0FDCh]
00457169 push    ecx
0045716A fstp    [esp+1Ch+var_1C]; float
0045716D call    sub_5EECE0
00457172 fstp    [esp+1Ch+arg_0]
00457176 add     esp, 4
00457179 fld     [esp+18h+arg_0]
0045717D push    ecx
0045717E mov     eax, 4Eh ; 'N'
00457183 fstp    [esp+1Ch+var_1C]; int
00457186 mov     ecx, esi
00457188 call    sub_52A5A0
0045718D mov     eax, [edi+0A4h]
00457193 cmp     byte ptr [eax+9DCh], 0
0045719A mov     byte ptr [esp+18h+arg_0], 0
0045719F setz    dl
004571A2 sub     esp, 1Ch
004571A5 mov     [eax+9DCh], dl
004571AB mov     eax, 13h
004571B0 mov     esi, esp
004571B2 mov     [esp+34h+var_8], eax
004571B6 lea     ebx, [esp+34h+arg_0]
004571BA lea     ebp, [esp+34h+var_8]
004571BE mov     [esp+34h+var_4], esp
004571C2 call    sub_463B60
004571C7 mov     eax, [edi+0A4h]
004571CD push    eax; int
004571CE mov     ecx, ebx
004571D0 mov     edx, ebp
004571D2 call    sub_463910
004571D7 pop     edi
004571D8 pop     esi
004571D9 pop     ebp
004571DA pop     ebx
004571DB add     esp, 8
004571DE retn    4
