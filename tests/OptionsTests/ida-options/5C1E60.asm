005C1E60  push    ebp
005C1E61  mov     ebp, esp
005C1E63  sub     esp, 108h
005C1E69  push    esi
005C1E6A  push    edi
005C1E6B  mov     [ebp+var_108], ecx
005C1E71  mov     eax, [ebp+var_108]
005C1E77  mov     edx, [eax]
005C1E79  mov     ecx, [ebp+var_108]
005C1E7F  mov     eax, [edx+4Ch]
005C1E82  call    eax
005C1E84  mov     ecx, [ebp+arg_0]
005C1E87  mov     byte ptr [ecx+3Ch], 1
005C1E8B  mov     edx, [ebp+var_108]
005C1E91  cmp     dword ptr [edx+24h], 0
005C1E95  jz      short loc_5C1E9C
005C1E97  jmp     loc_5C22B8
005C1E9C  mov     eax, [ebp+var_108]
005C1EA2  add     eax, 4
005C1EA5  mov     [ebp+var_88], eax
005C1EAB  mov     ecx, [ebp+var_88]
005C1EB1  mov     edx, [ecx+14h]
005C1EB4  mov     [ebp+var_84], edx
005C1EBA  mov     eax, [ebp+var_88]
005C1EC0  push    eax
005C1EC1  mov     ecx, [ebp+var_84]
005C1EC7  push    ecx
005C1EC8  lea     ecx, [ebp+var_30]
005C1ECB  call    sub_65DB10
005C1ED0  mov     edx, [ebp+var_108]
005C1ED6  add     edx, 4
005C1ED9  mov     [ebp+var_94], edx
005C1EDF  mov     eax, [ebp+var_94]
005C1EE5  mov     ecx, [eax+14h]
005C1EE8  mov     [ebp+var_8C], ecx
005C1EEE  mov     edx, [ebp+var_8C]
005C1EF4  mov     eax, [edx]
005C1EF6  mov     [ebp+var_90], eax
005C1EFC  mov     ecx, [ebp+var_94]
005C1F02  push    ecx
005C1F03  mov     edx, [ebp+var_90]
005C1F09  push    edx
005C1F0A  lea     ecx, [ebp+var_38]
005C1F0D  call    sub_65DB10
005C1F12  mov     eax, [ebp+var_30]
005C1F15  mov     [ebp+var_C4], eax
005C1F1B  mov     ecx, [ebp+var_2C]
005C1F1E  mov     [ebp+var_C0], ecx
005C1F24  mov     edx, [ebp+var_38]
005C1F27  mov     [ebp+var_BC], edx
005C1F2D  mov     eax, [ebp+var_34]
005C1F30  mov     [ebp+var_B8], eax
005C1F36  lea     ecx, [ebp+var_9C]
005C1F3C  push    ecx
005C1F3D  lea     ecx, [ebp+var_C4]
005C1F43  call    sub_613C40
005C1F48  lea     edx, [ebp+var_A4]
005C1F4E  push    edx
005C1F4F  lea     ecx, [ebp+var_BC]
005C1F55  call    sub_613C40
005C1F5A  lea     eax, [ebp+arg_0]
005C1F5D  push    eax
005C1F5E  mov     ecx, [ebp+var_98]
005C1F64  push    ecx
005C1F65  mov     edx, [ebp+var_9C]
005C1F6B  push    edx
005C1F6C  mov     eax, [ebp+var_A0]
005C1F72  push    eax
005C1F73  mov     ecx, [ebp+var_A4]
005C1F79  push    ecx
005C1F7A  lea     edx, [ebp+var_AC]
005C1F80  push    edx
005C1F81  call    sub_5D4C90
005C1F86  add     esp, 18h
005C1F89  mov     [ebp+var_B4], eax
005C1F8F  mov     eax, [ebp+var_B4]
005C1F95  mov     ecx, [eax+4]
005C1F98  push    ecx
005C1F99  mov     edx, [eax]
005C1F9B  push    edx
005C1F9C  lea     ecx, [ebp+var_BC]
005C1FA2  call    sub_5D5240
005C1FA7  mov     eax, [ebp+var_BC]
005C1FAD  mov     [ebp+var_8], eax
005C1FB0  mov     ecx, [ebp+var_B8]
005C1FB6  mov     [ebp+var_4], ecx
005C1FB9  mov     edx, [ebp+var_108]
005C1FBF  add     edx, 4
005C1FC2  mov     [ebp+var_CC], edx
005C1FC8  mov     eax, [ebp+var_CC]
005C1FCE  mov     ecx, [eax+14h]
005C1FD1  mov     [ebp+var_C8], ecx
005C1FD7  mov     edx, [ebp+var_CC]
005C1FDD  push    edx
005C1FDE  mov     eax, [ebp+var_C8]
005C1FE4  push    eax
005C1FE5  lea     ecx, [ebp+var_40]
005C1FE8  call    sub_65DB10
005C1FED  lea     ecx, [ebp+var_40]
005C1FF0  push    ecx
005C1FF1  lea     ecx, [ebp+var_8]
005C1FF4  call    sub_632700
005C1FF9  movzx   edx, al
005C1FFC  test    edx, edx
005C1FFE  jz      short loc_5C2005
005C2000  jmp     loc_5C22B8
005C2005  mov     eax, [ebp+var_8]
005C2008  mov     [ebp+var_10], eax
005C200B  mov     ecx, [ebp+var_4]
005C200E  mov     [ebp+var_C], ecx
005C2011  mov     edx, [ebp+var_108]
005C2017  add     edx, 4
005C201A  mov     [ebp+var_D8], edx
005C2020  mov     eax, [ebp+var_D8]
005C2026  mov     ecx, [eax+14h]
005C2029  mov     [ebp+var_D0], ecx
005C202F  mov     edx, [ebp+var_D0]
005C2035  mov     eax, [edx]
005C2037  mov     [ebp+var_D4], eax
005C203D  mov     ecx, [ebp+var_D8]
005C2043  push    ecx
005C2044  mov     edx, [ebp+var_D4]
005C204A  push    edx
005C204B  lea     ecx, [ebp+var_48]
005C204E  call    sub_65DB10
005C2053  lea     eax, [ebp+var_48]
005C2056  push    eax
005C2057  lea     ecx, [ebp+var_10]
005C205A  call    sub_632700
005C205F  movzx   ecx, al
005C2062  neg     ecx
005C2064  sbb     ecx, ecx
005C2066  add     ecx, 1
005C2069  movzx   edx, cl
005C206C  test    edx, edx
005C206E  jz      loc_5C21E7
005C2074  mov     eax, [ebp+var_10]
005C2077  mov     ecx, [ebp+var_C]
005C207A  mov     [ebp+var_E0], eax
005C2080  mov     [ebp+var_DC], ecx
005C2086  lea     ecx, [ebp+var_10]
005C2089  call    sub_635720
005C208E  mov     edx, [ebp+var_E0]
005C2094  mov     [ebp+var_50], edx
005C2097  mov     eax, [ebp+var_DC]
005C209D  mov     [ebp+var_4C], eax
005C20A0  lea     ecx, [ebp+var_10]
005C20A3  call    sub_5B7240
005C20A8  mov     ecx, [eax]
005C20AA  mov     [ebp+var_14], ecx
005C20AD  mov     edx, [ebp+var_14]
005C20B0  movzx   eax, byte ptr [edx+64h]
005C20B4  test    eax, eax
005C20B6  jz      loc_5C21B6
005C20BC  mov     ecx, [ebp+var_14]
005C20BF  movzx   edx, byte ptr [ecx+6Ah]
005C20C3  test    edx, edx
005C20C5  jnz     loc_5C2187
005C20CB  mov     eax, [ebp+var_14]
005C20CE  movzx   ecx, byte ptr [eax+50h]
005C20D2  test    ecx, ecx
005C20D4  jnz     loc_5C2187
005C20DA  mov     edx, [ebp+var_108]
005C20E0  mov     eax, [edx+4Ch]
005C20E3  push    eax
005C20E4  mov     ecx, [ebp+var_108]
005C20EA  mov     edx, [ecx+48h]
005C20ED  push    edx
005C20EE  xor     edx, edx
005C20F0  xor     ecx, ecx
005C20F2  lea     eax, [ebp+var_70]
005C20F5  call    sub_4091F0
005C20FA  mov     esi, eax
005C20FC  mov     eax, [ebp+arg_0]
005C20FF  mov     ecx, [eax+4Ch]
005C2102  push    ecx
005C2103  mov     edx, [ebp+arg_0]
005C2106  mov     eax, [edx+48h]
005C2109  push    eax
005C210A  mov     ecx, [ebp+arg_0]
005C210D  mov     edx, [ecx+44h]
005C2110  mov     eax, [ebp+arg_0]
005C2113  mov     ecx, [eax+40h]
005C2116  lea     eax, [ebp+var_60]
005C2119  call    sub_4091F0
005C211E  mov     edx, eax
005C2120  mov     ecx, esi
005C2122  lea     eax, [ebp+var_24]
005C2125  mov     edi, edx
005C2127  call    sub_464640
005C212C  mov     eax, [ebp+var_20]
005C212F  push    eax
005C2130  mov     ecx, [ebp+var_24]
005C2133  push    ecx
005C2134  mov     edx, [ebp+var_14]
005C2137  mov     eax, [edx]
005C2139  mov     ecx, [ebp+var_14]
005C213C  mov     edx, [eax+110h]
005C2142  call    edx
005C2144  movzx   eax, al
005C2147  test    eax, eax
005C2149  jz      short loc_5C2187
005C214B  mov     ecx, [ebp+var_18]
005C214E  mov     edx, [ebp+var_20]
005C2151  lea     eax, [edx+ecx-1]
005C2155  push    eax
005C2156  mov     ecx, [ebp+var_1C]
005C2159  mov     edx, [ebp+var_24]
005C215C  lea     eax, [edx+ecx-1]
005C2160  push    eax
005C2161  mov     ecx, [ebp+var_14]
005C2164  mov     edx, [ecx]
005C2166  mov     ecx, [ebp+var_14]
005C2169  mov     eax, [edx+110h]
005C216F  call    eax
005C2171  movzx   ecx, al
005C2174  test    ecx, ecx
005C2176  jz      short loc_5C2187
005C2178  mov     edx, [ebp+var_14]
005C217B  mov     eax, [edx]
005C217D  mov     ecx, [ebp+var_14]
005C2180  mov     edx, [eax+44h]
005C2183  call    edx
005C2185  jmp     short loc_5C21E7
005C2187  mov     eax, [ebp+arg_0]
005C218A  push    eax
005C218B  mov     ecx, [ebp+var_14]
005C218E  mov     edx, [ecx]
005C2190  mov     ecx, [ebp+var_14]
005C2193  mov     eax, [edx+8]
005C2196  call    eax
005C2198  movzx   ecx, al
005C219B  test    ecx, ecx
005C219D  jz      short loc_5C21B6
005C219F  mov     edx, [ebp+var_14]
005C21A2  push    edx
005C21A3  mov     eax, [ebp+var_108]
005C21A9  mov     edx, [eax]
005C21AB  mov     ecx, [ebp+var_108]
005C21B1  mov     eax, [edx+40h]
005C21B4  call    eax
005C21B6  lea     ecx, [ebp+var_78]
005C21B9  push    ecx
005C21BA  mov     ecx, [ebp+var_108]
005C21C0  add     ecx, 4
005C21C3  call    sub_62EFA0
005C21C8  push    eax
005C21C9  lea     ecx, [ebp+var_10]
005C21CC  call    sub_632700
005C21D1  movzx   edx, al
005C21D4  test    edx, edx
005C21D6  jz      short loc_5C21DA
005C21D8  jmp     short loc_5C21E7
005C21DA  lea     ecx, [ebp+var_10]
005C21DD  call    sub_635720
005C21E2  jmp     loc_5C20A0
005C21E7  mov     eax, [ebp+var_8]
005C21EA  mov     [ebp+var_10], eax
005C21ED  mov     ecx, [ebp+var_4]
005C21F0  mov     [ebp+var_C], ecx
005C21F3  mov     edx, [ebp+var_108]
005C21F9  add     edx, 4
005C21FC  mov     [ebp+var_100], edx
005C2202  mov     eax, [ebp+var_100]
005C2208  mov     ecx, [eax+14h]
005C220B  mov     [ebp+var_FC], ecx
005C2211  lea     eax, [ebp+var_80]
005C2214  call    sub_4086A0
005C2219  mov     edx, [ebp+var_FC]
005C221F  mov     [ebp+var_7C], edx
005C2222  mov     eax, 1
005C2227  test    eax, eax
005C2229  jz      short loc_5C2239
005C222B  cmp     [ebp+var_100], 0
005C2232  jnz     short loc_5C2239
005C2234  call    __invalid_parameter_noinfo
005C2239  mov     eax, [ebp+var_100]
005C223F  lea     edx, [ebp+var_80]
005C2242  call    sub_408680
005C2247  lea     ecx, [ebp+var_80]
005C224A  push    ecx
005C224B  lea     ecx, [ebp+var_10]
005C224E  call    sub_632700
005C2253  movzx   edx, al
005C2256  neg     edx
005C2258  sbb     edx, edx
005C225A  add     edx, 1
005C225D  movzx   eax, dl
005C2260  test    eax, eax
005C2262  jz      short loc_5C22B8
005C2264  lea     ecx, [ebp+var_10]
005C2267  call    sub_5B7240
005C226C  mov     ecx, [eax]
005C226E  mov     [ebp+var_28], ecx
005C2271  mov     edx, [ebp+var_28]
005C2274  movzx   eax, byte ptr [edx+64h]
005C2278  test    eax, eax
005C227A  jz      short loc_5C22AB
005C227C  mov     ecx, [ebp+arg_0]
005C227F  push    ecx
005C2280  mov     edx, [ebp+var_28]
005C2283  mov     eax, [edx]
005C2285  mov     ecx, [ebp+var_28]
005C2288  mov     edx, [eax+8]
005C228B  call    edx
005C228D  movzx   eax, al
005C2290  test    eax, eax
005C2292  jz      short loc_5C22AB
005C2294  mov     ecx, [ebp+var_28]
005C2297  push    ecx
005C2298  mov     edx, [ebp+var_108]
005C229E  mov     eax, [edx]
005C22A0  mov     ecx, [ebp+var_108]
005C22A6  mov     edx, [eax+40h]
005C22A9  call    edx
005C22AB  lea     ecx, [ebp+var_10]
005C22AE  call    sub_5D3980
005C22B3  jmp     loc_5C21F3
005C22B8  pop     edi
005C22B9  pop     esi
005C22BA  mov     esp, ebp
005C22BC  pop     ebp
005C22BD  retn    4
