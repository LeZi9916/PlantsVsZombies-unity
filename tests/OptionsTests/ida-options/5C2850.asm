005C2850  push    ebp
005C2851  mov     ebp, esp
005C2853  push    0FFFFFFFFh
005C2855  push    offset SEH_5C2850
005C285A  mov     eax, large fs:0
005C2860  push    eax
005C2861  sub     esp, 0C8h
005C2867  mov     eax, ___security_cookie
005C286C  xor     eax, ebp
005C286E  push    eax
005C286F  lea     eax, [ebp+var_C]
005C2872  mov     large fs:0, eax
005C2878  mov     [ebp+var_C8], ecx
005C287E  mov     eax, [ebp+var_C8]
005C2884  mov     ecx, [eax+20h]
005C2887  mov     edx, [ebp+var_C8]
005C288D  mov     eax, [edx+5Ch]
005C2890  cmp     eax, [ecx+98h]
005C2896  jle     short loc_5C28B0
005C2898  mov     ecx, [ebp+var_C8]
005C289E  mov     edx, [ecx+5Ch]
005C28A1  push    edx
005C28A2  mov     eax, [ebp+var_C8]
005C28A8  mov     ecx, [eax+20h]
005C28AB  call    sub_5C3160
005C28B0  mov     ecx, [ebp+var_C8]
005C28B6  add     ecx, 54h ; 'T'
005C28B9  mov     [ebp+var_AC], ecx
005C28BF  mov     edx, [ebp+arg_0]
005C28C2  mov     [ebp+var_20], edx
005C28C5  mov     eax, [ebp+arg_0]
005C28C8  mov     ecx, [eax]
005C28CA  mov     [ebp+var_1C], ecx
005C28CD  mov     edx, [ebp+arg_0]
005C28D0  mov     eax, [edx+4]
005C28D3  mov     [ebp+var_18], eax
005C28D6  mov     ecx, [ebp+arg_0]
005C28D9  mov     edx, [ebp+var_AC]
005C28DF  mov     eax, [ecx]
005C28E1  or      eax, [edx]
005C28E3  mov     ecx, [ebp+var_AC]
005C28E9  mov     edx, [ecx+4]
005C28EC  not     edx
005C28EE  and     eax, edx
005C28F0  mov     ecx, [ebp+arg_0]
005C28F3  mov     [ecx], eax
005C28F5  mov     edx, [ebp+arg_0]
005C28F8  add     edx, 4
005C28FB  mov     [ebp+var_A8], edx
005C2901  mov     eax, [ebp+var_A8]
005C2907  mov     ecx, [ebp+var_AC]
005C290D  mov     edx, [eax]
005C290F  or      edx, [ecx]
005C2911  mov     eax, [ebp+var_AC]
005C2917  mov     ecx, [eax+4]
005C291A  not     ecx
005C291C  and     edx, ecx
005C291E  mov     eax, [ebp+var_A8]
005C2924  mov     [eax], edx
005C2926  mov     [ebp+var_4], 0
005C292D  mov     ecx, [ebp+var_C8]
005C2933  movzx   edx, byte ptr [ecx+51h]
005C2937  test    edx, edx
005C2939  jz      short loc_5C298A
005C293B  mov     eax, [ebp+arg_0]
005C293E  movzx   ecx, byte ptr [eax+8]
005C2942  test    ecx, ecx
005C2944  jz      short loc_5C2953
005C2946  mov     edx, [ebp+arg_0]
005C2949  mov     eax, [edx]
005C294B  mov     [ebp+var_CC], eax
005C2951  jmp     short loc_5C295F
005C2953  mov     ecx, [ebp+arg_0]
005C2956  mov     edx, [ecx+4]
005C2959  mov     [ebp+var_CC], edx
005C295F  mov     eax, [ebp+var_CC]
005C2965  and     eax, 8
005C2968  jz      short loc_5C298A
005C296A  mov     ecx, [ebp+var_C8]
005C2970  mov     edx, [ecx+4Ch]
005C2973  push    edx
005C2974  mov     eax, [ebp+var_C8]
005C297A  mov     ecx, [eax+48h]
005C297D  push    ecx
005C297E  push    0
005C2980  push    0
005C2982  mov     ecx, [ebp+arg_4]
005C2985  call    sub_5939F0
005C298A  mov     edx, [ebp+var_C8]
005C2990  mov     eax, [edx+1Ch]
005C2993  mov     [ebp+var_B0], eax
005C2999  cmp     [ebp+var_B0], 0
005C29A0  jnz     short loc_5C2A05
005C29A2  mov     ecx, [ebp+arg_0]
005C29A5  movzx   edx, byte ptr [ecx+8]
005C29A9  test    edx, edx
005C29AB  jz      short loc_5C29BA
005C29AD  mov     eax, [ebp+arg_0]
005C29B0  mov     ecx, [eax]
005C29B2  mov     [ebp+var_D0], ecx
005C29B8  jmp     short loc_5C29C6
005C29BA  mov     edx, [ebp+arg_0]
005C29BD  mov     eax, [edx+4]
005C29C0  mov     [ebp+var_D0], eax
005C29C6  mov     ecx, [ebp+var_D0]
005C29CC  and     ecx, 4
005C29CF  jz      short loc_5C29E8
005C29D1  mov     edx, [ebp+arg_4]
005C29D4  push    edx
005C29D5  mov     eax, [ebp+var_C8]
005C29DB  mov     edx, [eax]
005C29DD  mov     ecx, [ebp+var_C8]
005C29E3  mov     eax, [edx+68h]
005C29E6  call    eax
005C29E8  mov     [ebp+var_4], 0FFFFFFFFh
005C29EF  mov     ecx, [ebp+var_20]
005C29F2  mov     edx, [ebp+var_1C]
005C29F5  mov     [ecx], edx
005C29F7  mov     eax, [ebp+var_20]
005C29FA  mov     ecx, [ebp+var_18]
005C29FD  mov     [eax+4], ecx
005C2A00  jmp     loc_5C2BA0
005C2A05  mov     edx, [ebp+arg_0]
005C2A08  movzx   eax, byte ptr [edx+8]
005C2A0C  test    eax, eax
005C2A0E  jz      short loc_5C2A1D
005C2A10  mov     ecx, [ebp+arg_0]
005C2A13  mov     edx, [ecx]
005C2A15  mov     [ebp+var_D4], edx
005C2A1B  jmp     short loc_5C2A29
005C2A1D  mov     eax, [ebp+arg_0]
005C2A20  mov     ecx, [eax+4]
005C2A23  mov     [ebp+var_D4], ecx
005C2A29  mov     edx, [ebp+var_D4]
005C2A2F  and     edx, 4
005C2A32  jz      short loc_5C2A5B
005C2A34  mov     ecx, [ebp+arg_4]
005C2A37  call    sub_592700
005C2A3C  mov     eax, [ebp+arg_4]
005C2A3F  push    eax
005C2A40  mov     ecx, [ebp+var_C8]
005C2A46  mov     edx, [ecx]
005C2A48  mov     ecx, [ebp+var_C8]
005C2A4E  mov     eax, [edx+68h]
005C2A51  call    eax
005C2A53  mov     ecx, [ebp+arg_4]
005C2A56  call    sub_592780
005C2A5B  lea     ecx, [ebp+var_14]
005C2A5E  push    ecx
005C2A5F  mov     ecx, [ebp+var_C8]
005C2A65  add     ecx, 4
005C2A68  call    sub_62EFA0
005C2A6D  mov     edx, [ebp+var_C8]
005C2A73  add     edx, 4
005C2A76  mov     [ebp+var_C0], edx
005C2A7C  mov     eax, [ebp+var_C0]
005C2A82  mov     ecx, [eax+14h]
005C2A85  mov     [ebp+var_BC], ecx
005C2A8B  lea     eax, [ebp+var_A4]
005C2A91  call    sub_4086A0
005C2A96  mov     edx, [ebp+var_BC]
005C2A9C  mov     [ebp+var_A0], edx
005C2AA2  mov     eax, 1
005C2AA7  test    eax, eax
005C2AA9  jz      short loc_5C2AB9
005C2AAB  cmp     [ebp+var_C0], 0
005C2AB2  jnz     short loc_5C2AB9
005C2AB4  call    __invalid_parameter_noinfo
005C2AB9  mov     eax, [ebp+var_C0]
005C2ABF  lea     edx, [ebp+var_A4]
005C2AC5  call    sub_408680
005C2ACA  lea     ecx, [ebp+var_A4]
005C2AD0  push    ecx
005C2AD1  lea     ecx, [ebp+var_14]
005C2AD4  call    sub_632700
005C2AD9  movzx   edx, al
005C2ADC  neg     edx
005C2ADE  sbb     edx, edx
005C2AE0  add     edx, 1
005C2AE3  movzx   eax, dl
005C2AE6  test    eax, eax
005C2AE8  jz      loc_5C2B88
005C2AEE  lea     ecx, [ebp+var_14]
005C2AF1  call    sub_5B7240
005C2AF6  mov     ecx, [eax]
005C2AF8  mov     [ebp+var_24], ecx
005C2AFB  mov     edx, [ebp+var_24]
005C2AFE  movzx   eax, byte ptr [edx+64h]
005C2B02  test    eax, eax
005C2B04  jz      short loc_5C2B7B
005C2B06  mov     ecx, [ebp+var_C8]
005C2B0C  mov     edx, [ecx+20h]
005C2B0F  mov     eax, [ebp+var_24]
005C2B12  cmp     eax, [edx+0ACh]
005C2B18  jnz     short loc_5C2B21
005C2B1A  mov     ecx, [ebp+arg_0]
005C2B1D  mov     byte ptr [ecx+8], 1
005C2B21  mov     edx, [ebp+arg_4]
005C2B24  push    edx
005C2B25  lea     ecx, [ebp+var_9C]
005C2B2B  call    sub_5924D0
005C2B30  mov     byte ptr [ebp+var_4], 1
005C2B34  mov     eax, [ebp+var_24]
005C2B37  mov     ecx, [eax+44h]
005C2B3A  push    ecx
005C2B3B  mov     edx, [ebp+var_24]
005C2B3E  mov     eax, [edx+40h]
005C2B41  push    eax
005C2B42  lea     ecx, [ebp+var_9C]
005C2B48  call    sub_593AA0
005C2B4D  lea     ecx, [ebp+var_9C]
005C2B53  push    ecx
005C2B54  mov     edx, [ebp+arg_0]
005C2B57  push    edx
005C2B58  mov     eax, [ebp+var_24]
005C2B5B  mov     edx, [eax]
005C2B5D  mov     ecx, [ebp+var_24]
005C2B60  mov     eax, [edx+6Ch]
005C2B63  call    eax
005C2B65  mov     ecx, [ebp+var_24]
005C2B68  mov     byte ptr [ecx+3Ch], 0
005C2B6C  mov     byte ptr [ebp+var_4], 0
005C2B70  lea     ecx, [ebp+var_9C]
005C2B76  call    sub_5926D0
005C2B7B  lea     ecx, [ebp+var_14]
005C2B7E  call    sub_5D3980
005C2B83  jmp     loc_5C2A6D
005C2B88  mov     [ebp+var_4], 0FFFFFFFFh
005C2B8F  mov     edx, [ebp+var_20]
005C2B92  mov     eax, [ebp+var_1C]
005C2B95  mov     [edx], eax
005C2B97  mov     ecx, [ebp+var_20]
005C2B9A  mov     edx, [ebp+var_18]
005C2B9D  mov     [ecx+4], edx
005C2BA0  mov     ecx, [ebp+var_C]
005C2BA3  mov     large fs:0, ecx
005C2BAA  pop     ecx
005C2BAB  mov     esp, ebp
005C2BAD  pop     ebp
005C2BAE  retn    8
006E56D0  lea     ecx, [ebp+var_20]
006E56D3  jmp     sub_5C0430
006E56D8  lea     ecx, [ebp+var_9C]
006E56DE  jmp     sub_5926D0
006E56E3  mov     edx, [esp-4+arg_4]
006E56E7  lea     eax, [edx+0Ch]
006E56EA  mov     ecx, [edx-0CCh]
006E56F0  xor     ecx, eax; StackCookie
006E56F2  call    @__security_check_cookie@4; __security_check_cookie(x)
006E56F7  mov     eax, offset stru_738374
006E56FC  jmp     ___CxxFrameHandler3
