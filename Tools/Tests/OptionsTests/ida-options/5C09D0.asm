005C09D0  push    ebp
005C09D1  mov     ebp, esp
005C09D3  sub     esp, 0B4h
005C09D9  mov     [ebp+var_B4], ecx
005C09DF  mov     eax, [ebp+var_B4]
005C09E5  add     eax, 4
005C09E8  mov     [ebp+var_44], eax
005C09EB  mov     ecx, [ebp+var_44]
005C09EE  mov     edx, [ecx+14h]
005C09F1  mov     [ebp+var_40], edx
005C09F4  lea     eax, [ebp+var_14]
005C09F7  call    sub_4086A0
005C09FC  mov     eax, [ebp+var_40]
005C09FF  mov     [ebp+var_10], eax
005C0A02  mov     ecx, 1
005C0A07  test    ecx, ecx
005C0A09  jz      short loc_5C0A16
005C0A0B  cmp     [ebp+var_44], 0
005C0A0F  jnz     short loc_5C0A16
005C0A11  call    __invalid_parameter_noinfo
005C0A16  mov     eax, [ebp+var_44]
005C0A19  lea     edx, [ebp+var_14]
005C0A1C  call    sub_408680
005C0A21  lea     edx, [ebp+arg_0]
005C0A24  push    edx
005C0A25  mov     eax, [ebp+var_10]
005C0A28  push    eax
005C0A29  mov     ecx, [ebp+var_14]
005C0A2C  push    ecx
005C0A2D  lea     edx, [ebp+var_1C]
005C0A30  push    edx
005C0A31  mov     ecx, [ebp+var_B4]
005C0A37  add     ecx, 4
005C0A3A  call    sub_62EFA0
005C0A3F  mov     ecx, [eax+4]
005C0A42  push    ecx
005C0A43  mov     edx, [eax]
005C0A45  push    edx
005C0A46  lea     eax, [ebp+var_8]
005C0A49  push    eax
005C0A4A  call    sub_5D48D0
005C0A4F  add     esp, 18h
005C0A52  mov     ecx, [ebp+var_B4]
005C0A58  add     ecx, 4
005C0A5B  mov     [ebp+var_84], ecx
005C0A61  mov     edx, [ebp+var_84]
005C0A67  mov     eax, [edx+14h]
005C0A6A  mov     [ebp+var_80], eax
005C0A6D  lea     eax, [ebp+var_24]
005C0A70  call    sub_4086A0
005C0A75  mov     ecx, [ebp+var_80]
005C0A78  mov     [ebp+var_20], ecx
005C0A7B  mov     edx, 1
005C0A80  test    edx, edx
005C0A82  jz      short loc_5C0A92
005C0A84  cmp     [ebp+var_84], 0
005C0A8B  jnz     short loc_5C0A92
005C0A8D  call    __invalid_parameter_noinfo
005C0A92  mov     eax, [ebp+var_84]
005C0A98  lea     edx, [ebp+var_24]
005C0A9B  call    sub_408680
005C0AA0  lea     eax, [ebp+var_24]
005C0AA3  push    eax
005C0AA4  lea     ecx, [ebp+var_8]
005C0AA7  call    sub_632700
005C0AAC  movzx   ecx, al
005C0AAF  neg     ecx
005C0AB1  sbb     ecx, ecx
005C0AB3  add     ecx, 1
005C0AB6  movzx   edx, cl
005C0AB9  test    edx, edx
005C0ABB  jz      loc_5C0B5E
005C0AC1  mov     ecx, [ebp+arg_0]
005C0AC4  call    sub_5C4B40
005C0AC9  mov     eax, [ebp+arg_0]
005C0ACC  mov     dword ptr [eax+24h], 0
005C0AD3  mov     ecx, [ebp+var_B4]
005C0AD9  add     ecx, 2Ch ; ','
005C0ADC  push    ecx
005C0ADD  lea     ecx, [ebp+var_8]
005C0AE0  call    sub_632700
005C0AE5  mov     [ebp+var_9], al
005C0AE8  mov     edx, [ebp+var_8]
005C0AEB  mov     eax, [ebp+var_4]
005C0AEE  mov     [ebp+var_8C], edx
005C0AF4  mov     [ebp+var_88], eax
005C0AFA  lea     ecx, [ebp+var_8]
005C0AFD  call    sub_5D3980
005C0B02  mov     ecx, [ebp+var_8C]
005C0B08  mov     [ebp+var_34], ecx
005C0B0B  mov     edx, [ebp+var_88]
005C0B11  mov     [ebp+var_30], edx
005C0B14  mov     eax, [ebp+var_34]
005C0B17  mov     ecx, [ebp+var_30]
005C0B1A  mov     [ebp+var_2C], eax
005C0B1D  mov     [ebp+var_28], ecx
005C0B20  mov     edx, [ebp+var_28]
005C0B23  push    edx
005C0B24  mov     eax, [ebp+var_2C]
005C0B27  push    eax
005C0B28  lea     ecx, [ebp+var_3C]
005C0B2B  push    ecx
005C0B2C  mov     ecx, [ebp+var_B4]
005C0B32  add     ecx, 4
005C0B35  call    sub_5D1840
005C0B3A  movzx   edx, [ebp+var_9]
005C0B3E  test    edx, edx
005C0B40  jz      short loc_5C0B5E
005C0B42  mov     eax, [ebp+var_B4]
005C0B48  mov     ecx, [ebp+var_8]
005C0B4B  mov     [eax+2Ch], ecx
005C0B4E  mov     edx, [ebp+var_4]
005C0B51  mov     [eax+30h], edx
005C0B54  mov     eax, [ebp+var_B4]
005C0B5A  mov     byte ptr [eax+28h], 1
005C0B5E  mov     esp, ebp
005C0B60  pop     ebp
005C0B61  retn    4
