005C2670  push    ebp
005C2671  mov     ebp, esp
005C2673  push    0FFFFFFFFh
005C2675  push    offset SEH_5C2670
005C267A  mov     eax, large fs:0
005C2680  push    eax
005C2681  sub     esp, 48h
005C2684  mov     eax, ___security_cookie
005C2689  xor     eax, ebp
005C268B  push    eax
005C268C  lea     eax, [ebp+var_C]
005C268F  mov     large fs:0, eax
005C2695  mov     [ebp+var_50], ecx
005C2698  mov     eax, [ebp+var_50]
005C269B  add     eax, 54h ; 'T'
005C269E  mov     [ebp+var_34], eax
005C26A1  mov     ecx, [ebp+arg_0]
005C26A4  mov     [ebp+var_18], ecx
005C26A7  mov     edx, [ebp+arg_0]
005C26AA  mov     eax, [edx]
005C26AC  mov     [ebp+var_14], eax
005C26AF  mov     ecx, [ebp+arg_0]
005C26B2  mov     edx, [ecx+4]
005C26B5  mov     [ebp+var_10], edx
005C26B8  mov     eax, [ebp+arg_0]
005C26BB  mov     ecx, [ebp+var_34]
005C26BE  mov     edx, [eax]
005C26C0  or      edx, [ecx]
005C26C2  mov     eax, [ebp+var_34]
005C26C5  mov     ecx, [eax+4]
005C26C8  not     ecx
005C26CA  and     edx, ecx
005C26CC  mov     eax, [ebp+arg_0]
005C26CF  mov     [eax], edx
005C26D1  mov     ecx, [ebp+arg_0]
005C26D4  add     ecx, 4
005C26D7  mov     [ebp+var_30], ecx
005C26DA  mov     edx, [ebp+var_30]
005C26DD  mov     eax, [ebp+var_34]
005C26E0  mov     ecx, [edx]
005C26E2  or      ecx, [eax]
005C26E4  mov     edx, [ebp+var_34]
005C26E7  mov     eax, [edx+4]
005C26EA  not     eax
005C26EC  and     ecx, eax
005C26EE  mov     edx, [ebp+var_30]
005C26F1  mov     [edx], ecx
005C26F3  mov     [ebp+var_4], 0
005C26FA  mov     eax, [ebp+arg_0]
005C26FD  movzx   ecx, byte ptr [eax+8]
005C2701  test    ecx, ecx
005C2703  jz      short loc_5C270F
005C2705  mov     edx, [ebp+arg_0]
005C2708  mov     eax, [edx]
005C270A  mov     [ebp+var_54], eax
005C270D  jmp     short loc_5C2718
005C270F  mov     ecx, [ebp+arg_0]
005C2712  mov     edx, [ecx+4]
005C2715  mov     [ebp+var_54], edx
005C2718  mov     eax, [ebp+var_54]
005C271B  and     eax, 1
005C271E  jz      short loc_5C2734
005C2720  push    ecx
005C2721  fld     [ebp+arg_4]
005C2724  fstp    [esp+5Ch+var_5C]
005C2727  mov     ecx, [ebp+var_50]
005C272A  mov     edx, [ecx]
005C272C  mov     ecx, [ebp+var_50]
005C272F  mov     eax, [edx+60h]
005C2732  call    eax
005C2734  lea     ecx, [ebp+var_24]
005C2737  push    ecx
005C2738  mov     ecx, [ebp+var_50]
005C273B  add     ecx, 4
005C273E  call    sub_62EFA0
005C2743  mov     edx, [eax]
005C2745  mov     eax, [eax+4]
005C2748  mov     ecx, [ebp+var_50]
005C274B  mov     [ecx+2Ch], edx
005C274E  mov     [ecx+30h], eax
005C2751  mov     edx, [ebp+var_50]
005C2754  add     edx, 4
005C2757  mov     [ebp+var_44], edx
005C275A  mov     eax, [ebp+var_44]
005C275D  mov     ecx, [eax+14h]
005C2760  mov     [ebp+var_40], ecx
005C2763  lea     eax, [ebp+var_2C]
005C2766  call    sub_4086A0
005C276B  mov     edx, [ebp+var_40]
005C276E  mov     [ebp+var_28], edx
005C2771  mov     eax, 1
005C2776  test    eax, eax
005C2778  jz      short loc_5C2785
005C277A  cmp     [ebp+var_44], 0
005C277E  jnz     short loc_5C2785
005C2780  call    __invalid_parameter_noinfo
005C2785  mov     eax, [ebp+var_44]
005C2788  lea     edx, [ebp+var_2C]
005C278B  call    sub_408680
005C2790  lea     ecx, [ebp+var_2C]
005C2793  push    ecx
005C2794  mov     ecx, [ebp+var_50]
005C2797  add     ecx, 2Ch ; ','
005C279A  call    sub_632700
005C279F  movzx   edx, al
005C27A2  neg     edx
005C27A4  sbb     edx, edx
005C27A6  add     edx, 1
005C27A9  movzx   eax, dl
005C27AC  test    eax, eax
005C27AE  jz      short loc_5C2818
005C27B0  mov     ecx, [ebp+var_50]
005C27B3  mov     byte ptr [ecx+28h], 0
005C27B7  mov     ecx, [ebp+var_50]
005C27BA  add     ecx, 2Ch ; ','
005C27BD  call    sub_5B7240
005C27C2  mov     edx, [eax]
005C27C4  mov     [ebp+var_1C], edx
005C27C7  mov     eax, [ebp+var_50]
005C27CA  mov     ecx, [eax+20h]
005C27CD  mov     edx, [ebp+var_1C]
005C27D0  cmp     edx, [ecx+0ACh]
005C27D6  jnz     short loc_5C27DF
005C27D8  mov     eax, [ebp+arg_0]
005C27DB  mov     byte ptr [eax+8], 1
005C27DF  push    ecx
005C27E0  fld     [ebp+arg_4]
005C27E3  fstp    [esp+5Ch+var_5C]
005C27E6  mov     ecx, [ebp+arg_0]
005C27E9  push    ecx
005C27EA  mov     edx, [ebp+var_1C]
005C27ED  mov     eax, [edx]
005C27EF  mov     ecx, [ebp+var_1C]
005C27F2  mov     edx, [eax+64h]
005C27F5  call    edx
005C27F7  mov     eax, [ebp+var_50]
005C27FA  movzx   ecx, byte ptr [eax+28h]
005C27FE  test    ecx, ecx
005C2800  jnz     short loc_5C2813
005C2802  mov     edx, [ebp+var_50]
005C2805  add     edx, 2Ch ; ','
005C2808  mov     [ebp+var_4C], edx
005C280B  mov     ecx, [ebp+var_4C]
005C280E  call    sub_5D3980
005C2813  jmp     loc_5C2751
005C2818  mov     eax, [ebp+var_50]
005C281B  mov     byte ptr [eax+28h], 1
005C281F  mov     [ebp+var_4], 0FFFFFFFFh
005C2826  mov     ecx, [ebp+var_18]
005C2829  mov     edx, [ebp+var_14]
005C282C  mov     [ecx], edx
005C282E  mov     eax, [ebp+var_18]
005C2831  mov     ecx, [ebp+var_10]
005C2834  mov     [eax+4], ecx
005C2837  mov     ecx, [ebp+var_C]
005C283A  mov     large fs:0, ecx
005C2841  pop     ecx
005C2842  mov     esp, ebp
005C2844  pop     ebp
005C2845  retn    8
006DD890  lea     ecx, [ebp+var_18]
006DD893  jmp     sub_5C0430
006DD898  mov     edx, [esp-4+arg_4]
006DD89C  lea     eax, [edx+0Ch]
006DD89F  mov     ecx, [edx-4Ch]
006DD8A2  xor     ecx, eax; StackCookie
006DD8A4  call    @__security_check_cookie@4; __security_check_cookie(x)
006DD8A9  mov     eax, offset stru_72F878
006DD8AE  jmp     ___CxxFrameHandler3
