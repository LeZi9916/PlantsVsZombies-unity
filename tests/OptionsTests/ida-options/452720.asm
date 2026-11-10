00452720  sub     esp, 10h
00452723  cmp     byte ptr [esp+10h+arg_8], 0
00452728  push    ebx
00452729  push    ebp
0045272A  mov     ebp, dword_752BF4
00452730  push    esi
00452731  mov     ebx, eax
00452733  mov     eax, dword_7534EC
00452738  mov     esi, ecx
0045273A  mov     ecx, dword_752B94
00452740  mov     [esp+1Ch+var_8], esi
00452744  mov     [esp+1Ch+var_4], ebx
00452748  mov     [esp+1Ch+var_10], eax
0045274C  mov     [esp+1Ch+var_C], ecx
00452750  jz      short loc_452777
00452752  mov     edx, dword_7535C4
00452758  mov     eax, dword_753484
0045275D  mov     ebp, dword_752D5C
00452763  inc     esi
00452764  lea     ecx, [ebx+1]
00452767  mov     [esp+1Ch+var_10], edx
0045276B  mov     [esp+1Ch+var_C], eax
0045276F  mov     [esp+1Ch+var_8], esi
00452773  mov     [esp+1Ch+var_4], ecx
00452777  mov     eax, [esp+1Ch+var_C]
0045277B  mov     edx, [esp+1Ch+arg_0]
0045277F  sub     edx, [eax+24h]
00452782  mov     ecx, [esp+1Ch+var_10]
00452786  sub     edx, [ebp+24h]
00452789  mov     [esp+1Ch+arg_8], edx
0045278D  fild    [esp+1Ch+arg_8]
00452791  fidiv   dword ptr [ecx+24h]
00452794  fstp    [esp+1Ch+arg_8]
00452798  fldz
0045279A  fld     [esp+1Ch+arg_8]
0045279E  fcom    st(1)
004527A0  fnstsw  ax
004527A2  fstp    st(1)
004527A4  test    ah, 41h
004527A7  jnz     short loc_4527B1
004527A9  fadd    ds:dbl_7271A8
004527AF  jmp     short loc_4527B7
004527B1  fsub    ds:dbl_7271A8
004527B7  call    __ftol2_sse
004527BC  push    ebx; int
004527BD  push    esi; int
004527BE  push    ebp; Concurrency::details::ExecutionResource *
004527BF  mov     ecx, edi
004527C1  mov     [esp+28h+arg_8], eax
004527C5  call    sub_592F90
004527CA  mov     eax, [esp+1Ch+arg_8]
004527CE  add     esi, [ebp+24h]
004527D1  test    eax, eax
004527D3  jle     short loc_4527F8
004527D5  mov     [esp+1Ch+arg_8], eax
004527D9  lea     esp, [esp+0]
004527E0  mov     ebp, [esp+1Ch+var_10]
004527E4  push    ebx; int
004527E5  push    esi; int
004527E6  push    ebp; Concurrency::details::ExecutionResource *
004527E7  mov     ecx, edi
004527E9  call    sub_592F90
004527EE  add     esi, [ebp+24h]
004527F1  sub     [esp+1Ch+arg_8], 1
004527F6  jnz     short loc_4527E0
004527F8  mov     edx, [esp+1Ch+var_C]
004527FC  push    ebx; int
004527FD  push    esi; int
004527FE  push    edx; Concurrency::details::ExecutionResource *
004527FF  mov     ecx, edi
00452801  call    sub_592F90
00452806  cmp     [esp+1Ch+arg_C], 0
0045280B  jz      short loc_452815
0045280D  mov     eax, dword_752B80
00452812  push    eax
00452813  jmp     short loc_45281C
00452815  mov     ecx, dword_753790
0045281B  push    ecx
0045281C  mov     ecx, edi
0045281E  call    sub_592860
00452823  mov     ecx, dword_753790
00452829  mov     edx, [ecx]
0045282B  mov     ebp, [esp+1Ch+arg_10]
0045282F  mov     eax, [edx+1Ch]
00452832  push    ebp
00452833  call    eax
00452835  mov     ecx, [esp+1Ch+arg_0]
00452839  sub     ecx, eax
0045283B  mov     eax, ecx
0045283D  mov     ecx, [esp+1Ch+var_8]
00452841  cdq
00452842  sub     eax, edx
00452844  sar     eax, 1
00452846  lea     esi, [ecx+eax+1]
0045284A  mov     ecx, dword_753790
00452850  mov     edx, [ecx]
00452852  mov     eax, [edx+4]
00452855  call    eax
00452857  mov     ecx, eax
00452859  mov     eax, 2AAAAAABh
0045285E  imul    ecx
00452860  mov     eax, [esp+1Ch+arg_4]
00452864  mov     ecx, edx
00452866  shr     ecx, 1Fh
00452869  add     ecx, edx
0045286B  sub     eax, ecx
0045286D  mov     ecx, dword_753790
00452873  mov     edx, [ecx]
00452875  lea     ebx, [eax-1]
00452878  mov     eax, [edx+4]
0045287B  call    eax
0045287D  mov     ecx, [esp+1Ch+var_4]
00452881  add     eax, ebx
00452883  cdq
00452884  sub     eax, edx
00452886  sar     eax, 1
00452888  lea     ebx, [ecx+eax-4]
0045288C  push    offset dword_7DCA98
00452891  mov     ecx, edi
00452893  call    sub_592880
00452898  push    ebx
00452899  push    esi
0045289A  push    ebp
0045289B  mov     ecx, edi
0045289D  call    sub_592F40
004528A2  pop     esi
004528A3  pop     ebp
004528A4  pop     ebx
004528A5  add     esp, 10h
004528A8  retn
