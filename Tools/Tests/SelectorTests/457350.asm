00457350 push    ebp
00457351 mov     ebp, esp
00457353 and     esp, 0FFFFFFF8h
00457356 sub     esp, 14h
00457359 push    ebx
0045735A push    esi
0045735B push    edi
0045735C mov     edi, ecx
0045735E mov     ecx, offset unk_708324
00457363 mov     [esp+20h+var_14], ecx
00457367 lea     ebx, [edi+130h]
0045736D lea     ecx, [ecx+0]
00457370 mov     eax, [edi+0A4h]
00457376 fldz
00457378 mov     edx, [eax+944h]
0045737E mov     eax, [ebx]
00457380 mov     edx, [edx+8]
00457383 and     eax, 0FFFFh
00457388 lea     esi, [eax+eax*4]
0045738B shl     esi, 5
0045738E add     esi, [edx]
00457390 fcomp   dword ptr [esi+8]
00457393 fnstsw  ax
00457395 test    ah, 5
00457398 jnp     loc_457458
0045739E fld     dword ptr [ecx-4]
004573A1 fstp    [esp+20h+var_C]
004573A5 fld     dword ptr [ecx]
004573A7 fstp    [esp+20h+var_4]
004573AB fild    [ebp+arg_0]
004573AE fstp    [esp+20h+var_10]
004573B2 fild    [ebp+arg_4]
004573B5 fstp    [esp+20h+var_8]
004573B9 fld     [esp+20h+var_10]
004573BD fsub    [esp+20h+var_C]
004573C1 fld     [esp+20h+var_8]
004573C5 fsub    [esp+20h+var_4]
004573C9 fmul    st, st
004573CB fld     st(1)
004573CD fmulp   st(2), st
004573CF faddp   st(1), st
004573D1 fstp    [esp+20h+var_4]
004573D5 fld     [esp+20h+var_4]
004573D9 call    __CIsqrt
004573DE fstp    [esp+20h+var_4]
004573E2 fld     [esp+20h+var_4]
004573E6 fcomp   ds:flt_727358
004573EC fnstsw  ax
004573EE test    ah, 5
004573F1 jp      short loc_457454
004573F3 fld     ds:flt_727500
004573F9 fstp    dword ptr [esi+8]
004573FC mov     eax, [edi+0A4h]
00457402 cmp     byte ptr [eax+9E9h], 0
00457409 jnz     short loc_457454
0045740B fldz
0045740D mov     ecx, dword_755E50
00457413 mov     esi, [eax+888h]
00457419 fst     [esp+20h+var_10]
0045741D fcomp   dword ptr [ecx+518h]
00457423 fnstsw  ax
00457425 test    ah, 44h
00457428 jnp     short loc_457440
0045742A fld     dword ptr [ecx+518h]
00457430 push    ecx
00457431 fstp    [esp+24h+var_24]; float
00457434 call    sub_5EECE0
00457439 fstp    [esp+24h+var_10]
0045743D add     esp, 4
00457440 fld     [esp+20h+var_10]
00457444 push    ecx
00457445 mov     eax, 19h
0045744A fstp    [esp+24h+var_24]; float
0045744D mov     ecx, esi
0045744F call    sub_52A5A0
00457454 mov     ecx, [esp+20h+var_14]
00457458 add     ecx, 8
0045745B add     ebx, 4
0045745E cmp     ecx, offset flt_70833C
00457464 mov     [esp+20h+var_14], ecx
00457468 jl      loc_457370
0045746E mov     eax, [edi+0A4h]
00457474 cmp     byte ptr [eax+919h], 0
0045747B jz      short loc_457499
0045747D cmp     byte ptr [edi+0E8h], 0
00457484 jz      short loc_457499
00457486 mov     eax, 1C2h
0045748B cmp     [edi+0ECh], eax
00457491 jge     short loc_457499
00457493 mov     [edi+0ECh], eax
00457499 pop     edi
0045749A pop     esi
0045749B pop     ebx
0045749C mov     esp, ebp
0045749E pop     ebp
0045749F retn    0Ch
