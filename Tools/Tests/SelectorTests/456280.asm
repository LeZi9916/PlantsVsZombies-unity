00456280 push    ebp
00456281 mov     ebp, esp
00456283 and     esp, 0FFFFFFF8h
00456286 sub     esp, 40h
00456289 push    ebx
0045628A push    ebp
0045628B push    esi
0045628C push    edi
0045628D mov     ebp, ecx
0045628F call    sub_5C5350
00456294 mov     eax, [ebp+0]
00456297 mov     edx, [eax+44h]
0045629A mov     ecx, ebp
0045629C call    edx
0045629E push    ebp
0045629F call    sub_4560B0
004562A4 mov     eax, [ebp+0A4h]
004562AA cmp     dword ptr [eax+950h], 0
004562B1 jz      short loc_4562D9
004562B3 cmp     byte_755E34, 0
004562BA jnz     short loc_4562D9
004562BC mov     byte_755E34, 1
004562C3 mov     eax, [ebp+0A4h]
004562C9 mov     eax, [eax+950h]
004562CF test    eax, eax
004562D1 jz      short loc_4562D9
004562D3 push    eax
004562D4 call    sub_4784A0
004562D9 mov     esi, [ebp+15Ch]
004562DF test    esi, esi
004562E1 jle     short loc_456337
004562E3 mov     ecx, [ebp+16Ch]
004562E9 mov     edx, [ebp+164h]
004562EF mov     edi, [ebp+0]
004562F2 push    4
004562F4 push    ecx
004562F5 xor     ecx, ecx
004562F7 push    edx
004562F8 mov     eax, esi
004562FA lea     edx, [ecx+4Bh]
004562FD call    sub_526F20
00456302 add     esp, 0Ch
00456305 push    eax
00456306 mov     eax, [ebp+168h]
0045630C push    ecx
0045630D mov     ecx, [ebp+160h]
00456313 push    4
00456315 push    eax
00456316 push    ecx
00456317 xor     ecx, ecx
00456319 mov     eax, esi
0045631B lea     edx, [ecx+4Bh]
0045631E call    sub_526F20
00456323 mov     edx, [edi+0A4h]
00456329 add     esp, 10h
0045632C push    eax
0045632D mov     ecx, ebp
0045632F call    edx
00456331 dec     dword ptr [ebp+15Ch]
00456337 mov     eax, [ebp+0A4h]
0045633D mov     ecx, [eax+940h]
00456343 push    ecx
00456344 call    sub_5346F0
00456349 mov     edx, [ebp+0A4h]
0045634F mov     ecx, [ebp+0F8h]
00456355 mov     eax, [edx+944h]
0045635B mov     edx, [eax]
0045635D test    ecx, ecx
0045635F jz      short loc_456379
00456361 movzx   eax, cx
00456364 cmp     eax, [edx+8]
00456367 jnb     short loc_456379
00456369 imul    eax, 2Ch ; ','
0045636C add     eax, [edx]
0045636E cmp     [eax+28h], ecx
00456371 jnz     short loc_456379
00456373 push    eax
00456374 call    sub_52C8D0
00456379 cmp     byte ptr [ebp+0E8h], 0
00456380 jz      loc_4564FA
00456386 mov     ebx, 1
0045638B add     [ebp+0ECh], ebx
00456391 mov     eax, [ebp+0ECh]
00456397 cmp     eax, 1C2h
0045639C jle     loc_456486
004563A2 mov     esi, [ebp+0A4h]
004563A8 call    sub_45C590
004563AD mov     esi, [ebp+0A4h]
004563B3 cmp     dword ptr [esi+91Ch], 0
004563BA jnz     short loc_4563F4
004563BC mov     eax, [esi+950h]
004563C2 test    eax, eax
004563C4 jz      short loc_4563CC
004563C6 cmp     dword ptr [eax+54h], 0
004563CA jg      short loc_4563F4
004563CC cmp     [ebp+14Ch], ebx
004563D2 jnz     short loc_4563F4
004563D4 call    sub_45C2E0
004563D9 test    al, al
004563DB jnz     short loc_4563F4
004563DD mov     esi, [ebp+0A4h]
004563E3 push    0
004563E5 push    48h ; 'H'
004563E7 call    sub_45C100
004563EC pop     edi
004563ED pop     esi
004563EE pop     ebp
004563EF pop     ebx
004563F0 mov     esp, ebp
004563F2 pop     ebp
004563F3 retn
004563F4 mov     esi, [ebp+0A4h]
004563FA mov     ecx, [esi+950h]
00456400 cmp     dword ptr [ecx+36Ch], 0
00456407 jz      short loc_456449
00456409 cmp     dword ptr [ebp+14Ch], 23h ; '#'
00456410 jnz     short loc_456449
00456412 mov     edi, esi
00456414 call    sub_45C950
00456419 mov     edi, eax
0045641B push    259h
00456420 call    sub_4A2EB0
00456425 mov     edx, [edi]
00456427 mov     eax, [edx+12Ch]
0045642D push    ebx
0045642E mov     ecx, edi
00456430 call    eax
00456432 mov     esi, [ebp+0A4h]
00456438 push    0
0045643A push    0
0045643C call    sub_45C100
00456441 pop     edi
00456442 pop     esi
00456443 pop     ebp
00456444 pop     ebx
00456445 mov     esp, ebp
00456447 pop     ebp
00456448 retn
00456449 mov     eax, ebp
0045644B call    sub_4578B0
00456450 test    al, al
00456452 jz      short loc_456476
00456454 push    0
00456456 push    2Bh ; '+'
00456458 call    sub_45C100
0045645D mov     ecx, [ebp+0A4h]
00456463 mov     ecx, [ecx+940h]
00456469 call    sub_538080
0045646E pop     edi
0045646F pop     esi
00456470 pop     ebp
00456471 pop     ebx
00456472 mov     esp, ebp
00456474 pop     ebp
00456475 retn
00456476 push    ebx
00456477 push    0
00456479 call    sub_45C100
0045647E pop     edi
0045647F pop     esi
00456480 pop     ebp
00456481 pop     ebx
00456482 mov     esp, ebp
00456484 pop     ebp
00456485 retn
00456486 cdq
00456487 mov     ecx, 14h
0045648C idiv    ecx
0045648E cmp     edx, 0Ah
00456491 jge     short loc_4564C4
00456493 mov     edx, [ebp+0ACh]
00456499 mov     esi, [edx]
0045649B push    80h
004564A0 push    80h
004564A5 push    80h
004564AA lea     ecx, [esp+5Ch+var_3C]
004564AE add     esi, 88h
004564B4 call    sub_55BBB0
004564B9 mov     ecx, [ebp+0ACh]
004564BF push    eax
004564C0 mov     eax, [esi]
004564C2 jmp     short loc_4564D7
004564C4 mov     ecx, [ebp+0ACh]
004564CA mov     edx, [ecx]
004564CC mov     eax, [edx+88h]
004564D2 push    offset dword_7DCA98
004564D7 push    5
004564D9 call    eax
004564DB cmp     dword ptr [ebp+0ECh], 7Dh ; '}'
004564E2 jnz     short loc_4564FA
004564E4 mov     ecx, [ebp+0A4h]
004564EA mov     edx, [ecx]
004564EC mov     eax, dword_752C34
004564F1 mov     edx, [edx+0D8h]
004564F7 push    eax
004564F8 call    edx
004564FA mov     esi, [ebp+0FCh]
00456500 mov     ecx, [ebp+0A4h]
00456506 mov     eax, [ecx+944h]
0045650C mov     edx, [eax+8]
0045650F mov     eax, [ebp+148h]
00456515 and     esi, 0FFFFh
0045651B lea     esi, [esi+esi*4]
0045651E shl     esi, 5
00456521 add     esi, [edx]
00456523 test    eax, eax
00456525 jnz     loc_45674A
0045652B mov     ecx, [ebp+20h]
0045652E test    ecx, ecx
00456530 jz      short loc_456537
00456532 call    sub_5C41E0
00456537 cmp     dword ptr [esi+5Ch], 0
0045653B jle     loc_456796
00456541 push    0FFFFFFFFh; int
00456543 push    offset aSelectorscreen_1; "SelectorScreen_Adventure_button"
00456548 mov     ebx, esi
0045654A call    sub_482EC0
0045654F push    0FFFFFFFFh; int
00456551 push    offset aSelectorscreen_2; "SelectorScreen_StartAdventure_button"
00456556 call    sub_482EC0
0045655B push    0FFFFFFFFh; int
0045655D push    offset aSelectorscreen_3; "SelectorScreen_Survival_button"
00456562 call    sub_482EC0
00456567 push    0FFFFFFFFh; int
00456569 push    offset aSelectorscreen_4; "SelectorScreen_Challenges_button"
0045656E call    sub_482EC0
00456573 push    0FFFFFFFFh; int
00456575 push    offset aSelectorscreen_5; "SelectorScreen_ZenGarden_button"
0045657A call    sub_482EC0
0045657F mov     eax, [ebp+0ACh]
00456585 mov     byte ptr [eax+119h], 0
0045658C mov     ecx, [ebp+0B0h]
00456592 mov     byte ptr [ecx+119h], 0
00456599 mov     edx, [ebp+0B4h]
0045659F mov     byte ptr [edx+119h], 0
004565A6 mov     eax, [ebp+0DCh]
004565AC mov     byte ptr [eax+119h], 0
004565B3 mov     ecx, [ebp+0D8h]
004565B9 mov     byte ptr [ecx+119h], 0
004565C0 mov     edx, [ebp+0C0h]
004565C6 mov     byte ptr [edx+119h], 0
004565CD mov     eax, [ebp+0B8h]
004565D3 mov     byte ptr [eax+119h], 0
004565DA mov     ecx, [ebp+0BCh]
004565E0 mov     byte ptr [ecx+119h], 0
004565E7 mov     edx, [ebp+0C4h]
004565ED mov     byte ptr [edx+119h], 0
004565F4 mov     eax, [ebp+0C8h]
004565FA mov     byte ptr [eax+119h], 0
00456601 mov     ecx, [ebp+0A8h]
00456607 mov     byte ptr [ecx+119h], 0
0045660E mov     edx, [ebp+0A8h]
00456614 mov     ebx, 1
00456619 mov     [edx+65h], bl
0045661C mov     eax, [ebp+0ACh]
00456622 mov     [eax+65h], bl
00456625 mov     ecx, [ebp+0B0h]
0045662B mov     [ecx+65h], bl
0045662E mov     edx, [ebp+0B4h]
00456634 mov     [edx+65h], bl
00456637 mov     eax, [ebp+0DCh]
0045663D mov     [eax+65h], bl
00456640 mov     ecx, [ebp+0D8h]
00456646 mov     [ecx+65h], bl
00456649 mov     edx, [ebp+0C0h]
0045664F mov     [edx+65h], bl
00456652 mov     eax, [ebp+0B8h]
00456658 mov     [eax+65h], bl
0045665B mov     ecx, [ebp+0BCh]
00456661 mov     [ecx+65h], bl
00456664 mov     edx, [ebp+0D0h]
0045666A mov     [edx+65h], bl
0045666D mov     eax, [ebp+0D4h]
00456673 mov     [eax+65h], bl
00456676 mov     ecx, [ebp+0E0h]
0045667C mov     [ecx+65h], bl
0045667F mov     edx, [ebp+0C4h]
00456685 mov     [edx+65h], bl
00456688 mov     eax, [ebp+0C8h]
0045668E mov     [eax+65h], bl
00456691 mov     edi, [ebp+0A4h]
00456697 cmp     dword ptr [edi+950h], 0
0045669E jnz     short loc_4566BC
004566A0 call    sub_45D7E0
004566A5 cmp     byte_755DF5, 0
004566AC jz      short loc_4566B4
004566AE push    ebp
004566AF call    sub_457BC0
004566B4 mov     [ebp+148h], ebx
004566BA jmp     short loc_4566DE
004566BC fld     ds:flt_70830C
004566C2 push    ecx
004566C3 fstp    [esp+54h+var_54]; float
004566C6 push    3; int
004566C8 push    offset aAnimSign; "anim_sign"
004566CD xor     eax, eax
004566CF call    sub_4830B0
004566D4 mov     dword ptr [ebp+148h], 2
004566DE cmp     byte ptr [ebp+158h], 0
004566E5 jz      short loc_4566EE
004566E7 mov     edi, ebp
004566E9 call    sub_4553E0
004566EE mov     ecx, [ebp+0A4h]
004566F4 mov     eax, [ecx+950h]
004566FA test    eax, eax
004566FC jz      loc_456796
00456702 cmp     dword ptr [eax+368h], 0
00456709 jz      loc_456796
0045670F mov     dword ptr [eax+368h], 0
00456719 mov     eax, [ebp+0A4h]
0045671F call    sub_45C0E0
00456724 mov     edx, [ebp+0A4h]
0045672A push    3; int
0045672C push    offset unk_708460; int
00456731 push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
00456736 push    offset off_70DAA8; void *
0045673B push    31h ; '1'; int
0045673D push    edx; int
0045673E mov     ecx, offset off_70DA74; Source
00456743 call    sub_45D250
00456748 jmp     short loc_456796
0045674A cmp     eax, 1
0045674D jnz     short loc_456781
0045674F mov     eax, [ecx]
00456751 mov     edx, [eax+124h]
00456757 push    1Eh
00456759 call    edx
0045675B test    eax, eax
0045675D jnz     short loc_456796
0045675F fld     ds:flt_70830C
00456765 push    ecx
00456766 fstp    [esp+54h+var_54]; float
00456769 push    3; int
0045676B push    offset aAnimSign; "anim_sign"
00456770 call    sub_4830B0
00456775 mov     dword ptr [ebp+148h], 2
0045677F jmp     short loc_456796
00456781 cmp     eax, 2
00456784 jnz     short loc_456796
00456786 cmp     dword ptr [esi+5Ch], 0
0045678A jle     short loc_456796
0045678C mov     dword ptr [ebp+148h], 3
00456796 lea     edi, [ebp+118h]
0045679C mov     ebx, 6
004567A1 mov     eax, [edi-18h]
004567A4 mov     ecx, [ebp+0A4h]
004567AA mov     edx, [ecx+944h]
004567B0 mov     ecx, [edx+8]
004567B3 and     eax, 0FFFFh
004567B8 lea     eax, [eax+eax*4]
004567BB shl     eax, 5
004567BE add     eax, [ecx]
004567C0 mov     ecx, [edi]
004567C2 test    ecx, ecx
004567C4 jle     short loc_4567E4
004567C6 add     ecx, 0FFFFFFFFh
004567C9 mov     [edi], ecx
004567CB jnz     short loc_4567FE
004567CD fldz
004567CF mov     dword ptr [eax+5Ch], 0
004567D6 fstp    dword ptr [eax+4]
004567D9 fld     ds:flt_727270
004567DF fstp    dword ptr [eax+8]
004567E2 jmp     short loc_4567FE
004567E4 cmp     dword ptr [eax+5Ch], 0
004567E8 jle     short loc_4567FE
004567EA push    7D1h
004567EF call    sub_5EECC0
004567F4 add     esp, 4
004567F7 add     eax, 7D0h
004567FC mov     [edi], eax
004567FE add     edi, 4
00456801 sub     ebx, 1
00456804 jnz     short loc_4567A1
00456806 mov     ecx, esi
00456808 call    sub_480F00
0045680D mov     edx, [ebp+0A4h]
00456813 mov     eax, [edx+944h]
00456819 mov     ecx, [eax+8]
0045681C mov     eax, [ebp+13Ch]
00456822 and     eax, 0FFFFh
00456827 lea     edi, [eax+eax*4]
0045682A shl     edi, 5
0045682D add     edi, [ecx]
0045682F mov     ecx, edi
00456831 call    sub_480F00
00456836 push    offset aSelectorscreen; "SelectorScreen_BG_Right"
0045683B mov     ebx, esi
0045683D call    sub_4823E0
00456842 fld     ds:flt_708310
00456848 fst     [esp+50h+var_2C]
0045684C mov     ebx, eax
0045684E fst     [esp+50h+var_28]
00456852 xor     eax, eax
00456854 fst     [esp+50h+var_24]
00456858 mov     [esp+50h+var_8], eax
0045685C fst     [esp+50h+var_20]
00456860 mov     [esp+50h+var_C], eax
00456864 fst     [esp+50h+var_1C]
00456868 push    esi
00456869 fst     [esp+54h+var_18]
0045686D lea     eax, [esp+54h+var_2C]
00456871 fst     [esp+54h+var_14]
00456875 mov     [esp+54h+var_4], offset Class
0045687D fstp    [esp+54h+var_10]
00456881 call    sub_4812C0
00456886 mov     edx, [ebp+40h]
00456889 add     edx, 320h
0045688F mov     [esp+50h+var_40], edx
00456893 fild    [esp+50h+var_40]
00456897 fadd    [esp+50h+var_2C]
0045689B fsub    ds:dbl_727F68
004568A1 fstp    dword ptr [edi+2Ch]
004568A4 fld     [esp+50h+var_28]
004568A8 fsub    ds:dbl_7281E8
004568AE fstp    dword ptr [edi+38h]
004568B1 add     dword ptr [ebp+144h], 0FFFFFFFFh
004568B8 jnz     loc_456951
004568BE fld     ds:flt_7277F8
004568C4 push    ecx
004568C5 fstp    [esp+54h+var_54]; float
004568C8 call    sub_5EECE0
004568CD fadd    ds:dbl_7271D8
004568D3 add     esp, 4
004568D6 push    14h
004568D8 push    edi
004568D9 fstp    [esp+58h+var_40]
004568DD call    sub_4827D0
004568E2 fldz
004568E4 fld     st
004568E6 fld     [esp+50h+var_40]
004568EA fucom   st(1)
004568EC fnstsw  ax
004568EE fstp    st(1)
004568F0 test    ah, 44h
004568F3 jnp     short loc_4568FA
004568F5 fstp    dword ptr [edi+8]
004568F8 jmp     short loc_4568FC
004568FA fstp    st
004568FC fcom    dword ptr [edi+8]
004568FF xor     eax, eax
00456901 mov     [edi+10h], eax
00456904 mov     [edi+5Ch], eax
00456907 fnstsw  ax
00456909 test    ah, 41h
0045690C jnp     short loc_456916
0045690E fstp    st
00456910 fld     ds:flt_727398
00456916 lea     eax, [edi+1Ch]
00456919 fstp    dword ptr [edi+4]
0045691C fld     ds:flt_727228
00456922 push    eax
00456923 lea     ecx, [edi+18h]
00456926 fstp    dword ptr [edi+94h]
0045692C push    ecx
0045692D mov     edx, offset aAnimGrass; "anim_grass"
00456932 mov     eax, edi
00456934 call    sub_482690
00456939 push    0C9h
0045693E call    sub_5EECC0
00456943 add     esp, 4
00456946 add     eax, 0C8h
0045694B mov     [ebp+144h], eax
00456951 lea     edi, [ebp+100h]
00456957 mov     ebx, 6
0045695C lea     esp, [esp+0]
00456960 mov     edx, [ebp+0A4h]
00456966 mov     eax, [edx+944h]
0045696C mov     edx, [eax+8]
0045696F mov     eax, [edi]
00456971 and     eax, 0FFFFh
00456976 lea     ecx, [eax+eax*4]
00456979 shl     ecx, 5
0045697C add     ecx, [edx]
0045697E call    sub_480F00
00456983 add     edi, 4
00456986 sub     ebx, 1
00456989 jnz     short loc_456960
0045698B mov     ecx, [ebp+0A4h]
00456991 mov     ecx, [ecx+944h]
00456997 mov     eax, [ebp+140h]
0045699D test    ecx, ecx
0045699F jz      short loc_4569C5
004569A1 mov     edx, [ecx+8]
004569A4 test    eax, eax
004569A6 jz      short loc_4569C5
004569A8 movzx   ecx, ax
004569AB cmp     ecx, [edx+8]
004569AE jnb     short loc_4569C5
004569B0 lea     ecx, [ecx+ecx*4]
004569B3 shl     ecx, 5
004569B6 add     ecx, [edx]
004569B8 cmp     [ecx+9Ch], eax
004569BE jnz     short loc_4569C5
004569C0 call    sub_480F00
004569C5 fldz
004569C7 mov     edi, [ebp+0ACh]
004569CD sub     esp, 8
004569D0 fstp    [esp+58h+var_54]; float
004569D4 cmp     byte ptr [ebp+0F3h], 0
004569DB fld     ds:flt_727FEC
004569E1 fstp    [esp+58h+var_58]; float
004569E4 mov     eax, ebp
004569E6 jz      short loc_4569EF
004569E8 push    offset aSelectorscreen_2; "SelectorScreen_StartAdventure_button"
004569ED jmp     short loc_4569F4
004569EF push    offset aSelectorscreen_1; "SelectorScreen_Adventure_button"
004569F4 call    sub_456D00
004569F9 fldz
004569FB mov     edi, [ebp+0B0h]
00456A01 sub     esp, 8
00456A04 fstp    [esp+58h+var_54]; float
00456A08 mov     eax, ebp
00456A0A fld     ds:flt_727FEC
00456A10 fstp    [esp+58h+var_58]; float
00456A13 push    offset aSelectorscreen_3; "SelectorScreen_Survival_button"
00456A18 call    sub_456D00
00456A1D fldz
00456A1F mov     edi, [ebp+0DCh]
00456A25 sub     esp, 8
00456A28 fstp    [esp+58h+var_54]; float
00456A2C mov     eax, ebp
00456A2E fld     ds:flt_727FEC
00456A34 fstp    [esp+58h+var_58]; float
00456A37 push    offset aSelectorscreen_5; "SelectorScreen_ZenGarden_button"
00456A3C call    sub_456D00
00456A41 fldz
00456A43 mov     edi, [ebp+0B4h]
00456A49 sub     esp, 8
00456A4C fstp    [esp+58h+var_54]; float
00456A50 mov     eax, ebp
00456A52 fld     ds:flt_727FEC
00456A58 fstp    [esp+58h+var_58]; float
00456A5B push    offset aSelectorscreen_4; "SelectorScreen_Challenges_button"
00456A60 call    sub_456D00
00456A65 fld     ds:flt_7281E0
00456A6B mov     edi, [ebp+0A8h]
00456A71 sub     esp, 8
00456A74 fstp    [esp+58h+var_54]; float
00456A78 mov     eax, ebp
00456A7A fld     ds:flt_7281DC
00456A80 fstp    [esp+58h+var_58]; float
00456A83 push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456A88 call    sub_456D00
00456A8D fld     ds:flt_727940
00456A93 mov     edi, [ebp+0D8h]
00456A99 sub     esp, 8
00456A9C fstp    [esp+58h+var_54]; float
00456AA0 mov     eax, ebp
00456AA2 fld     ds:flt_7281D8
00456AA8 fstp    [esp+58h+var_58]; float
00456AAB push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456AB0 call    sub_456D00
00456AB5 fld     ds:flt_7281D4
00456ABB mov     edi, [ebp+0B8h]
00456AC1 sub     esp, 8
00456AC4 fstp    [esp+58h+var_54]; float
00456AC8 mov     eax, ebp
00456ACA fld     ds:flt_7281D0
00456AD0 fstp    [esp+58h+var_58]; float
00456AD3 push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456AD8 call    sub_456D00
00456ADD fld     ds:flt_7281CC
00456AE3 mov     edi, [ebp+0BCh]
00456AE9 sub     esp, 8
00456AEC fstp    [esp+58h+var_54]; float
00456AF0 mov     eax, ebp
00456AF2 fld     ds:flt_7281C8
00456AF8 fstp    [esp+58h+var_58]; float
00456AFB push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456B00 call    sub_456D00
00456B05 fld     ds:flt_7281C4
00456B0B mov     edi, [ebp+0C0h]
00456B11 sub     esp, 8
00456B14 fstp    [esp+58h+var_54]; float
00456B18 mov     eax, ebp
00456B1A fld     ds:flt_7281C0
00456B20 fstp    [esp+58h+var_58]; float
00456B23 push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456B28 call    sub_456D00
00456B2D fld     ds:flt_728128
00456B33 mov     edi, [ebp+0D4h]
00456B39 sub     esp, 8
00456B3C fstp    [esp+58h+var_54]; float
00456B40 mov     eax, ebp
00456B42 fld     ds:flt_7281BC
00456B48 fstp    [esp+58h+var_58]; float
00456B4B push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456B50 call    sub_456D00
00456B55 fld     ds:flt_7281B8
00456B5B mov     edi, [ebp+0D0h]
00456B61 sub     esp, 8
00456B64 fstp    [esp+58h+var_54]; float
00456B68 mov     eax, ebp
00456B6A fld     ds:flt_7281B4
00456B70 fstp    [esp+58h+var_58]; float
00456B73 push    offset aSelectorscreen; "SelectorScreen_BG_Right"
00456B78 call    sub_456D00
00456B7D fld     ds:flt_727540
00456B83 mov     edi, [ebp+0E0h]
00456B89 sub     esp, 8
00456B8C fstp    [esp+58h+var_54]; float
00456B90 mov     eax, ebp
00456B92 fld     ds:flt_7281B0
00456B98 fstp    [esp+58h+var_58]; float
00456B9B push    offset aWoodsign2; "woodsign2"
00456BA0 call    sub_456D00
00456BA5 fldz
00456BA7 mov     edi, [ebp+0C4h]
00456BAD sub     esp, 8
00456BB0 fstp    [esp+58h+var_54]; float
00456BB4 mov     eax, ebp
00456BB6 fld     ds:flt_727FEC
00456BBC fstp    [esp+58h+var_58]; float
00456BBF push    offset aWoodsign3; "woodsign3"
00456BC4 call    sub_456D00
00456BC9 fld     ds:flt_728020
00456BCF mov     edi, [ebp+0C8h]
00456BD5 sub     esp, 8
00456BD8 fstp    [esp+58h+var_54]; float
00456BDC mov     eax, ebp
00456BDE fld     ds:flt_7281AC
00456BE4 fstp    [esp+58h+var_58]; float
00456BE7 push    offset aSelectorscreen_0; "SelectorScreen_BG_Left"
00456BEC call    sub_456D00
00456BF1 mov     eax, [ebp+0E0h]
00456BF7 cmp     byte ptr [eax+69h], 0
00456BFB jnz     short loc_456C22
00456BFD cmp     byte ptr [eax+68h], 0
00456C01 jnz     short loc_456C22
00456C03 push    offset aWoodsign2; "woodsign2"
00456C08 mov     ebx, esi
00456C0A call    sub_4823E0
00456C0F mov     ecx, [esi+58h]
00456C12 lea     eax, [eax+eax*2]
00456C15 shl     eax, 5
00456C18 mov     dword ptr [eax+ecx+44h], 0
00456C20 jmp     short loc_456C76
00456C22 mov     eax, [esi+0Ch]
00456C25 mov     edx, dword_7532A0
00456C2B xor     edi, edi
00456C2D cmp     [eax+4], edi
00456C30 mov     [esp+50h+var_40], edx
00456C34 jle     short loc_456C63
00456C36 xor     ebx, ebx
00456C38 jmp     short loc_456C40
00456C40 mov     eax, [eax]
00456C42 mov     ecx, [ebx+eax]
00456C45 push    offset aWoodsign2; "woodsign2"
00456C4A push    ecx; String1
00456C4B call    __stricmp
00456C50 add     esp, 8
00456C53 test    eax, eax
00456C55 jz      short loc_456C65
00456C57 mov     eax, [esi+0Ch]
00456C5A inc     edi
00456C5B add     ebx, 0Ch
00456C5E cmp     edi, [eax+4]
00456C61 jl      short loc_456C40
00456C63 xor     edi, edi
00456C65 mov     eax, [esi+58h]
00456C68 mov     ecx, [esp+50h+var_40]
00456C6C lea     edx, [edi+edi*2]
00456C6F shl     edx, 5
00456C72 mov     [edx+eax+44h], ecx
00456C76 mov     ebp, [ebp+0C4h]
00456C7C cmp     byte ptr [ebp+69h], 0
00456C80 jnz     short loc_456CAD
00456C82 cmp     byte ptr [ebp+68h], 0
00456C86 jnz     short loc_456CAD
00456C88 push    offset aWoodsign3; "woodsign3"
00456C8D mov     ebx, esi
00456C8F call    sub_4823E0
00456C94 lea     edx, [eax+eax*2]
00456C97 mov     eax, [esi+58h]
00456C9A shl     edx, 5
00456C9D mov     dword ptr [edx+eax+44h], 0
00456CA5 pop     edi
00456CA6 pop     esi
00456CA7 pop     ebp
00456CA8 pop     ebx
00456CA9 mov     esp, ebp
00456CAB pop     ebp
00456CAC retn
00456CAD mov     eax, [esi+0Ch]
00456CB0 mov     ebp, dword_752F14
00456CB6 xor     edi, edi
00456CB8 cmp     [eax+4], edi
00456CBB jle     short loc_456CE3
00456CBD xor     ebx, ebx
00456CBF nop
00456CC0 mov     ecx, [eax]
00456CC2 mov     edx, [ebx+ecx]
00456CC5 push    offset aWoodsign3; "woodsign3"
00456CCA push    edx; String1
00456CCB call    __stricmp
00456CD0 add     esp, 8
00456CD3 test    eax, eax
00456CD5 jz      short loc_456CE5
00456CD7 mov     eax, [esi+0Ch]
00456CDA inc     edi
00456CDB add     ebx, 0Ch
00456CDE cmp     edi, [eax+4]
00456CE1 jl      short loc_456CC0
00456CE3 xor     edi, edi
00456CE5 mov     ecx, [esi+58h]
00456CE8 lea     eax, [edi+edi*2]
00456CEB shl     eax, 5
00456CEE pop     edi
00456CEF mov     [eax+ecx+44h], ebp
00456CF3 pop     esi
00456CF4 pop     ebp
00456CF5 pop     ebx
00456CF6 mov     esp, ebp
00456CF8 pop     ebp
00456CF9 retn
