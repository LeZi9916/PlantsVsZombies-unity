005C18F0  push    ebp
005C18F1  mov     ebp, esp
005C18F3  sub     esp, 130h
005C18F9  mov     [ebp+var_130], ecx
005C18FF  mov     eax, [ebp+var_130]
005C1905  add     eax, 4
005C1908  mov     [ebp+var_68], eax
005C190B  mov     ecx, [ebp+var_68]
005C190E  mov     edx, [ecx+14h]
005C1911  mov     [ebp+var_64], edx
005C1914  mov     eax, [ebp+var_68]
005C1917  push    eax
005C1918  mov     ecx, [ebp+var_64]
005C191B  push    ecx
005C191C  lea     ecx, [ebp+var_10]
005C191F  call    sub_65DB10
005C1924  mov     edx, [ebp+var_130]
005C192A  add     edx, 4
005C192D  mov     [ebp+var_74], edx
005C1930  mov     eax, [ebp+var_74]
005C1933  mov     ecx, [eax+14h]
005C1936  mov     [ebp+var_6C], ecx
005C1939  mov     edx, [ebp+var_6C]
005C193C  mov     eax, [edx]
005C193E  mov     [ebp+var_70], eax
005C1941  mov     ecx, [ebp+var_74]
005C1944  push    ecx
005C1945  mov     edx, [ebp+var_70]
005C1948  push    edx
005C1949  lea     ecx, [ebp+var_18]
005C194C  call    sub_65DB10
005C1951  lea     eax, [ebp+arg_0]
005C1954  push    eax
005C1955  mov     ecx, [ebp+var_C]
005C1958  push    ecx
005C1959  mov     edx, [ebp+var_10]
005C195C  push    edx
005C195D  mov     eax, [ebp+var_14]
005C1960  push    eax
005C1961  mov     ecx, [ebp+var_18]
005C1964  push    ecx
005C1965  lea     edx, [ebp+var_8]
005C1968  push    edx
005C1969  call    sub_5D48D0
005C196E  add     esp, 18h
005C1971  mov     eax, [ebp+var_130]
005C1977  add     eax, 4
005C197A  mov     [ebp+var_AC], eax
005C1980  mov     ecx, [ebp+var_AC]
005C1986  mov     edx, [ecx+14h]
005C1989  mov     [ebp+var_A8], edx
005C198F  mov     eax, [ebp+var_AC]
005C1995  push    eax
005C1996  mov     ecx, [ebp+var_A8]
005C199C  push    ecx
005C199D  lea     ecx, [ebp+var_20]
005C19A0  call    sub_65DB10
005C19A5  lea     edx, [ebp+var_20]
005C19A8  push    edx
005C19A9  lea     ecx, [ebp+var_8]
005C19AC  call    sub_632700
005C19B1  movzx   eax, al
005C19B4  neg     eax
005C19B6  sbb     eax, eax
005C19B8  add     eax, 1
005C19BB  movzx   ecx, al
005C19BE  test    ecx, ecx
005C19C0  jz      loc_5C1B9F
005C19C6  mov     edx, [ebp+var_130]
005C19CC  add     edx, 2Ch ; ','
005C19CF  push    edx
005C19D0  lea     ecx, [ebp+var_8]
005C19D3  call    sub_632700
005C19D8  movzx   eax, al
005C19DB  test    eax, eax
005C19DD  jz      short loc_5C1A2C
005C19DF  mov     ecx, [ebp+var_130]
005C19E5  add     ecx, 2Ch ; ','
005C19E8  mov     [ebp+var_B8], ecx
005C19EE  mov     edx, [ebp+var_B8]
005C19F4  mov     eax, [edx]
005C19F6  mov     ecx, [edx+4]
005C19F9  mov     [ebp+var_B4], eax
005C19FF  mov     [ebp+var_B0], ecx
005C1A05  mov     ecx, [ebp+var_B8]
005C1A0B  call    sub_5D3980
005C1A10  mov     edx, [ebp+var_B4]
005C1A16  mov     [ebp+var_28], edx
005C1A19  mov     eax, [ebp+var_B0]
005C1A1F  mov     [ebp+var_24], eax
005C1A22  mov     ecx, [ebp+var_130]
005C1A28  mov     byte ptr [ecx+28h], 1
005C1A2C  mov     edx, [ebp+var_8]
005C1A2F  mov     [ebp+var_30], edx
005C1A32  mov     eax, [ebp+var_4]
005C1A35  mov     [ebp+var_2C], eax
005C1A38  mov     ecx, [ebp+var_2C]
005C1A3B  push    ecx
005C1A3C  mov     edx, [ebp+var_30]
005C1A3F  push    edx
005C1A40  lea     eax, [ebp+var_38]
005C1A43  push    eax
005C1A44  mov     ecx, [ebp+var_130]
005C1A4A  add     ecx, 4
005C1A4D  call    sub_5D1840
005C1A52  mov     ecx, [ebp+var_130]
005C1A58  add     ecx, 4
005C1A5B  mov     [ebp+var_E0], ecx
005C1A61  mov     edx, [ebp+var_E0]
005C1A67  mov     eax, [edx+14h]
005C1A6A  mov     [ebp+var_DC], eax
005C1A70  lea     eax, [ebp+var_40]
005C1A73  call    sub_4086A0
005C1A78  mov     ecx, [ebp+var_DC]
005C1A7E  mov     [ebp+var_3C], ecx
005C1A81  mov     edx, 1
005C1A86  test    edx, edx
005C1A88  jz      short loc_5C1A98
005C1A8A  cmp     [ebp+var_E0], 0
005C1A91  jnz     short loc_5C1A98
005C1A93  call    __invalid_parameter_noinfo
005C1A98  mov     eax, [ebp+var_E0]
005C1A9E  lea     edx, [ebp+var_40]
005C1AA1  call    sub_408680
005C1AA6  lea     eax, [ebp+arg_4]
005C1AA9  push    eax
005C1AAA  mov     ecx, [ebp+var_3C]
005C1AAD  push    ecx
005C1AAE  mov     edx, [ebp+var_40]
005C1AB1  push    edx
005C1AB2  lea     eax, [ebp+var_48]
005C1AB5  push    eax
005C1AB6  mov     ecx, [ebp+var_130]
005C1ABC  add     ecx, 4
005C1ABF  call    sub_62EFA0
005C1AC4  mov     ecx, [eax+4]
005C1AC7  push    ecx
005C1AC8  mov     edx, [eax]
005C1ACA  push    edx
005C1ACB  lea     eax, [ebp+var_50]
005C1ACE  push    eax
005C1ACF  call    sub_5D48D0
005C1AD4  add     esp, 18h
005C1AD7  mov     ecx, [eax]
005C1AD9  mov     edx, [eax+4]
005C1ADC  mov     [ebp+var_8], ecx
005C1ADF  mov     [ebp+var_4], edx
005C1AE2  mov     eax, [ebp+var_130]
005C1AE8  add     eax, 4
005C1AEB  mov     [ebp+var_120], eax
005C1AF1  mov     ecx, [ebp+var_120]
005C1AF7  mov     edx, [ecx+14h]
005C1AFA  mov     [ebp+var_11C], edx
005C1B00  lea     eax, [ebp+var_58]
005C1B03  call    sub_4086A0
005C1B08  mov     eax, [ebp+var_11C]
005C1B0E  mov     [ebp+var_54], eax
005C1B11  mov     ecx, 1
005C1B16  test    ecx, ecx
005C1B18  jz      short loc_5C1B28
005C1B1A  cmp     [ebp+var_120], 0
005C1B21  jnz     short loc_5C1B28
005C1B23  call    __invalid_parameter_noinfo
005C1B28  mov     eax, [ebp+var_120]
005C1B2E  lea     edx, [ebp+var_58]
005C1B31  call    sub_408680
005C1B36  lea     edx, [ebp+var_58]
005C1B39  push    edx
005C1B3A  lea     ecx, [ebp+var_8]
005C1B3D  call    sub_632700
005C1B42  movzx   eax, al
005C1B45  neg     eax
005C1B47  sbb     eax, eax
005C1B49  add     eax, 1
005C1B4C  movzx   ecx, al
005C1B4F  test    ecx, ecx
005C1B51  jz      short loc_5C1B7F
005C1B53  mov     edx, [ebp+var_8]
005C1B56  mov     eax, [ebp+var_4]
005C1B59  mov     [ebp+var_128], edx
005C1B5F  mov     [ebp+var_124], eax
005C1B65  lea     ecx, [ebp+var_8]
005C1B68  call    sub_5D3980
005C1B6D  mov     ecx, [ebp+var_128]
005C1B73  mov     [ebp+var_60], ecx
005C1B76  mov     edx, [ebp+var_124]
005C1B7C  mov     [ebp+var_5C], edx
005C1B7F  mov     eax, [ebp+arg_0]
005C1B82  push    eax
005C1B83  lea     ecx, [ebp+var_8]
005C1B86  push    ecx
005C1B87  mov     ecx, [ebp+var_130]
005C1B8D  call    sub_5C0FD0
005C1B92  mov     edx, [ebp+arg_0]
005C1B95  mov     eax, [edx]
005C1B97  mov     ecx, [ebp+arg_0]
005C1B9A  mov     edx, [eax+78h]
005C1B9D  call    edx
005C1B9F  mov     esp, ebp
005C1BA1  pop     ebp
005C1BA2  retn    8
