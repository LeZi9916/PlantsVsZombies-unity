005C22C0  push    ebp
005C22C1  mov     ebp, esp
005C22C3  sub     esp, 30h
005C22C6  mov     [ebp+var_30], ecx
005C22C9  mov     eax, [ebp+arg_0]
005C22CC  movzx   ecx, byte ptr [eax+3Ch]
005C22D0  test    ecx, ecx
005C22D2  jz      short loc_5C22D9
005C22D4  jmp     loc_5C23EF
005C22D9  mov     edx, [ebp+var_30]
005C22DC  mov     eax, [edx]
005C22DE  mov     ecx, [ebp+var_30]
005C22E1  mov     edx, [eax+44h]
005C22E4  call    edx
005C22E6  mov     eax, [ebp+arg_0]
005C22E9  mov     byte ptr [eax+3Ch], 1
005C22ED  mov     ecx, [ebp+var_30]
005C22F0  cmp     dword ptr [ecx+24h], 0
005C22F4  jz      short loc_5C22FB
005C22F6  jmp     loc_5C23EF
005C22FB  mov     edx, [ebp+arg_0]
005C22FE  movzx   eax, byte ptr [edx+50h]
005C2302  test    eax, eax
005C2304  jz      short loc_5C231C
005C2306  mov     ecx, [ebp+arg_0]
005C2309  push    ecx
005C230A  mov     edx, [ebp+var_30]
005C230D  mov     eax, [edx]
005C230F  mov     ecx, [ebp+var_30]
005C2312  mov     edx, [eax+48h]
005C2315  call    edx
005C2317  jmp     loc_5C23EF
005C231C  mov     [ebp+var_1], 0
005C2320  lea     eax, [ebp+var_C]
005C2323  push    eax
005C2324  mov     ecx, [ebp+var_30]
005C2327  add     ecx, 4
005C232A  call    sub_62EFA0
005C232F  mov     ecx, [ebp+var_30]
005C2332  add     ecx, 4
005C2335  mov     [ebp+var_28], ecx
005C2338  mov     edx, [ebp+var_28]
005C233B  mov     eax, [edx+14h]
005C233E  mov     [ebp+var_24], eax
005C2341  lea     eax, [ebp+var_18]
005C2344  call    sub_4086A0
005C2349  mov     ecx, [ebp+var_24]
005C234C  mov     [ebp+var_14], ecx
005C234F  mov     edx, 1
005C2354  test    edx, edx
005C2356  jz      short loc_5C2363
005C2358  cmp     [ebp+var_28], 0
005C235C  jnz     short loc_5C2363
005C235E  call    __invalid_parameter_noinfo
005C2363  mov     eax, [ebp+var_28]
005C2366  lea     edx, [ebp+var_18]
005C2369  call    sub_408680
005C236E  lea     eax, [ebp+var_18]
005C2371  push    eax
005C2372  lea     ecx, [ebp+var_C]
005C2375  call    sub_632700
005C237A  movzx   ecx, al
005C237D  neg     ecx
005C237F  sbb     ecx, ecx
005C2381  add     ecx, 1
005C2384  movzx   edx, cl
005C2387  test    edx, edx
005C2389  jz      short loc_5C23EF
005C238B  lea     ecx, [ebp+var_C]
005C238E  call    sub_5B7240
005C2393  mov     eax, [eax]
005C2395  mov     [ebp+var_10], eax
005C2398  mov     ecx, [ebp+var_10]
005C239B  cmp     ecx, [ebp+arg_0]
005C239E  jnz     short loc_5C23A6
005C23A0  mov     [ebp+var_1], 1
005C23A4  jmp     short loc_5C23E2
005C23A6  movzx   edx, [ebp+var_1]
005C23AA  test    edx, edx
005C23AC  jz      short loc_5C23E2
005C23AE  mov     eax, [ebp+var_10]
005C23B1  movzx   ecx, byte ptr [eax+64h]
005C23B5  test    ecx, ecx
005C23B7  jz      short loc_5C23E2
005C23B9  mov     edx, [ebp+arg_0]
005C23BC  push    edx
005C23BD  mov     eax, [ebp+var_10]
005C23C0  mov     edx, [eax]
005C23C2  mov     ecx, [ebp+var_10]
005C23C5  mov     eax, [edx+8]
005C23C8  call    eax
005C23CA  movzx   ecx, al
005C23CD  test    ecx, ecx
005C23CF  jz      short loc_5C23E2
005C23D1  mov     edx, [ebp+var_10]
005C23D4  push    edx
005C23D5  mov     eax, [ebp+var_30]
005C23D8  mov     edx, [eax]
005C23DA  mov     ecx, [ebp+var_30]
005C23DD  mov     eax, [edx+40h]
005C23E0  call    eax
005C23E2  lea     ecx, [ebp+var_C]
005C23E5  call    sub_5D3980
005C23EA  jmp     loc_5C232F
005C23EF  mov     esp, ebp
005C23F1  pop     ebp
005C23F2  retn    4
