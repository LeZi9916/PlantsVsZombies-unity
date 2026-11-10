005C2420  push    ebp
005C2421  mov     ebp, esp
005C2423  push    0FFFFFFFFh
005C2425  push    offset SEH_5C2420
005C242A  mov     eax, large fs:0
005C2430  push    eax
005C2431  sub     esp, 50h
005C2434  mov     eax, ___security_cookie
005C2439  xor     eax, ebp
005C243B  push    eax
005C243C  lea     eax, [ebp+var_C]
005C243F  mov     large fs:0, eax
005C2445  mov     [ebp+var_54], ecx
005C2448  mov     eax, [ebp+var_54]
005C244B  add     eax, 54h ; 'T'
005C244E  mov     [ebp+var_38], eax
005C2451  mov     ecx, [ebp+arg_0]
005C2454  mov     [ebp+var_1C], ecx
005C2457  mov     edx, [ebp+arg_0]
005C245A  mov     eax, [edx]
005C245C  mov     [ebp+var_18], eax
005C245F  mov     ecx, [ebp+arg_0]
005C2462  mov     edx, [ecx+4]
005C2465  mov     [ebp+var_14], edx
005C2468  mov     eax, [ebp+arg_0]
005C246B  mov     ecx, [ebp+var_38]
005C246E  mov     edx, [eax]
005C2470  or      edx, [ecx]
005C2472  mov     eax, [ebp+var_38]
005C2475  mov     ecx, [eax+4]
005C2478  not     ecx
005C247A  and     edx, ecx
005C247C  mov     eax, [ebp+arg_0]
005C247F  mov     [eax], edx
005C2481  mov     ecx, [ebp+arg_0]
005C2484  add     ecx, 4
005C2487  mov     [ebp+var_34], ecx
005C248A  mov     edx, [ebp+var_34]
005C248D  mov     eax, [ebp+var_38]
005C2490  mov     ecx, [edx]
005C2492  or      ecx, [eax]
005C2494  mov     edx, [ebp+var_38]
005C2497  mov     eax, [edx+4]
005C249A  not     eax
005C249C  and     ecx, eax
005C249E  mov     edx, [ebp+var_34]
005C24A1  mov     [edx], ecx
005C24A3  mov     [ebp+var_4], 0
005C24AA  mov     eax, [ebp+arg_0]
005C24AD  movzx   ecx, byte ptr [eax+8]
005C24B1  test    ecx, ecx
005C24B3  jz      short loc_5C24BF
005C24B5  mov     edx, [ebp+arg_0]
005C24B8  mov     eax, [edx]
005C24BA  mov     [ebp+var_58], eax
005C24BD  jmp     short loc_5C24C8
005C24BF  mov     ecx, [ebp+arg_0]
005C24C2  mov     edx, [ecx+4]
005C24C5  mov     [ebp+var_58], edx
005C24C8  mov     eax, [ebp+var_58]
005C24CB  and     eax, 2
005C24CE  jz      short loc_5C24DD
005C24D0  mov     ecx, [ebp+var_54]
005C24D3  mov     edx, [ecx]
005C24D5  mov     ecx, [ebp+var_54]
005C24D8  mov     eax, [edx+44h]
005C24DB  call    eax
005C24DD  mov     ecx, [ebp+var_54]
005C24E0  mov     edx, [ecx+20h]
005C24E3  mov     [ebp+var_10], edx
005C24E6  cmp     [ebp+var_10], 0
005C24EA  jnz     short loc_5C2509
005C24EC  mov     [ebp+var_4], 0FFFFFFFFh
005C24F3  mov     eax, [ebp+var_1C]
005C24F6  mov     ecx, [ebp+var_18]
005C24F9  mov     [eax], ecx
005C24FB  mov     edx, [ebp+var_1C]
005C24FE  mov     eax, [ebp+var_14]
005C2501  mov     [edx+4], eax
005C2504  jmp     loc_5C2655
005C2509  mov     ecx, [ebp+arg_0]
005C250C  movzx   edx, byte ptr [ecx+8]
005C2510  test    edx, edx
005C2512  jz      short loc_5C251E
005C2514  mov     eax, [ebp+arg_0]
005C2517  mov     ecx, [eax]
005C2519  mov     [ebp+var_5C], ecx
005C251C  jmp     short loc_5C2527
005C251E  mov     edx, [ebp+arg_0]
005C2521  mov     eax, [edx+4]
005C2524  mov     [ebp+var_5C], eax
005C2527  mov     ecx, [ebp+var_5C]
005C252A  and     ecx, 1
005C252D  jz      short loc_5C255C
005C252F  mov     edx, [ebp+var_54]
005C2532  mov     eax, [edx+20h]
005C2535  mov     ecx, [ebp+var_54]
005C2538  mov     edx, [ecx+34h]
005C253B  cmp     edx, [eax+38h]
005C253E  jz      short loc_5C255C
005C2540  mov     eax, [ebp+var_54]
005C2543  mov     ecx, [eax+20h]
005C2546  mov     edx, [ebp+var_54]
005C2549  mov     eax, [ecx+38h]
005C254C  mov     [edx+34h], eax
005C254F  mov     ecx, [ebp+var_54]
005C2552  mov     edx, [ecx]
005C2554  mov     ecx, [ebp+var_54]
005C2557  mov     eax, [edx+58h]
005C255A  call    eax
005C255C  lea     ecx, [ebp+var_28]
005C255F  push    ecx
005C2560  mov     ecx, [ebp+var_54]
005C2563  add     ecx, 4
005C2566  call    sub_62EFA0
005C256B  mov     edx, [eax]
005C256D  mov     eax, [eax+4]
005C2570  mov     ecx, [ebp+var_54]
005C2573  mov     [ecx+2Ch], edx
005C2576  mov     [ecx+30h], eax
005C2579  mov     edx, [ebp+var_54]
005C257C  add     edx, 4
005C257F  mov     [ebp+var_48], edx
005C2582  mov     eax, [ebp+var_48]
005C2585  mov     ecx, [eax+14h]
005C2588  mov     [ebp+var_44], ecx
005C258B  lea     eax, [ebp+var_30]
005C258E  call    sub_4086A0
005C2593  mov     edx, [ebp+var_44]
005C2596  mov     [ebp+var_2C], edx
005C2599  mov     eax, 1
005C259E  test    eax, eax
005C25A0  jz      short loc_5C25AD
005C25A2  cmp     [ebp+var_48], 0
005C25A6  jnz     short loc_5C25AD
005C25A8  call    __invalid_parameter_noinfo
005C25AD  mov     eax, [ebp+var_48]
005C25B0  lea     edx, [ebp+var_30]
005C25B3  call    sub_408680
005C25B8  lea     ecx, [ebp+var_30]
005C25BB  push    ecx
005C25BC  mov     ecx, [ebp+var_54]
005C25BF  add     ecx, 2Ch ; ','
005C25C2  call    sub_632700
005C25C7  movzx   edx, al
005C25CA  neg     edx
005C25CC  sbb     edx, edx
005C25CE  add     edx, 1
005C25D1  movzx   eax, dl
005C25D4  test    eax, eax
005C25D6  jz      short loc_5C2636
005C25D8  mov     ecx, [ebp+var_54]
005C25DB  mov     byte ptr [ecx+28h], 0
005C25DF  mov     ecx, [ebp+var_54]
005C25E2  add     ecx, 2Ch ; ','
005C25E5  call    sub_5B7240
005C25EA  mov     edx, [eax]
005C25EC  mov     [ebp+var_20], edx
005C25EF  mov     eax, [ebp+var_10]
005C25F2  mov     ecx, [ebp+var_20]
005C25F5  cmp     ecx, [eax+0ACh]
005C25FB  jnz     short loc_5C2604
005C25FD  mov     edx, [ebp+arg_0]
005C2600  mov     byte ptr [edx+8], 1
005C2604  mov     eax, [ebp+arg_0]
005C2607  push    eax
005C2608  mov     ecx, [ebp+var_20]
005C260B  mov     edx, [ecx]
005C260D  mov     ecx, [ebp+var_20]
005C2610  mov     eax, [edx+5Ch]
005C2613  call    eax
005C2615  mov     ecx, [ebp+var_54]
005C2618  movzx   edx, byte ptr [ecx+28h]
005C261C  test    edx, edx
005C261E  jnz     short loc_5C2631
005C2620  mov     eax, [ebp+var_54]
005C2623  add     eax, 2Ch ; ','
005C2626  mov     [ebp+var_50], eax
005C2629  mov     ecx, [ebp+var_50]
005C262C  call    sub_5D3980
005C2631  jmp     loc_5C2579
005C2636  mov     ecx, [ebp+var_54]
005C2639  mov     byte ptr [ecx+28h], 1
005C263D  mov     [ebp+var_4], 0FFFFFFFFh
005C2644  mov     edx, [ebp+var_1C]
005C2647  mov     eax, [ebp+var_18]
005C264A  mov     [edx], eax
005C264C  mov     ecx, [ebp+var_1C]
005C264F  mov     edx, [ebp+var_14]
005C2652  mov     [ecx+4], edx
005C2655  mov     ecx, [ebp+var_C]
005C2658  mov     large fs:0, ecx
005C265F  pop     ecx
005C2660  mov     esp, ebp
005C2662  pop     ebp
005C2663  retn    4
006DD8C0  lea     ecx, [ebp+var_1C]
006DD8C3  jmp     sub_5C0430
006DD8C8  mov     edx, [esp-4+arg_4]
006DD8CC  lea     eax, [edx+0Ch]
006DD8CF  mov     ecx, [edx-54h]
006DD8D2  xor     ecx, eax; StackCookie
006DD8D4  call    @__security_check_cookie@4; __security_check_cookie(x)
006DD8D9  mov     eax, offset stru_72F8A4
006DD8DE  jmp     ___CxxFrameHandler3
