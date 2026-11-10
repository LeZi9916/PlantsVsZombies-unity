005C16D0  push    ebp
005C16D1  mov     ebp, esp
005C16D3  sub     esp, 124h
005C16D9  mov     [ebp+var_124], ecx
005C16DF  mov     eax, [ebp+var_124]
005C16E5  add     eax, 4
005C16E8  mov     [ebp+var_58], eax
005C16EB  mov     ecx, [ebp+var_58]
005C16EE  mov     edx, [ecx+14h]
005C16F1  mov     [ebp+var_54], edx
005C16F4  mov     eax, [ebp+var_58]
005C16F7  push    eax
005C16F8  mov     ecx, [ebp+var_54]
005C16FB  push    ecx
005C16FC  lea     ecx, [ebp+var_10]
005C16FF  call    sub_65DB10
005C1704  lea     edx, [ebp+arg_0]
005C1707  push    edx
005C1708  mov     eax, [ebp+var_C]
005C170B  push    eax
005C170C  mov     ecx, [ebp+var_10]
005C170F  push    ecx
005C1710  lea     edx, [ebp+var_18]
005C1713  push    edx
005C1714  mov     ecx, [ebp+var_124]
005C171A  add     ecx, 4
005C171D  call    sub_62EFA0
005C1722  mov     ecx, [eax+4]
005C1725  push    ecx
005C1726  mov     edx, [eax]
005C1728  push    edx
005C1729  lea     eax, [ebp+var_8]
005C172C  push    eax
005C172D  call    sub_5D48D0
005C1732  add     esp, 18h
005C1735  mov     ecx, [ebp+var_124]
005C173B  add     ecx, 4
005C173E  mov     [ebp+var_98], ecx
005C1744  mov     edx, [ebp+var_98]
005C174A  mov     eax, [edx+14h]
005C174D  mov     [ebp+var_94], eax
005C1753  lea     eax, [ebp+var_20]
005C1756  call    sub_4086A0
005C175B  mov     ecx, [ebp+var_94]
005C1761  mov     [ebp+var_1C], ecx
005C1764  mov     edx, 1
005C1769  test    edx, edx
005C176B  jz      short loc_5C177B
005C176D  cmp     [ebp+var_98], 0
005C1774  jnz     short loc_5C177B
005C1776  call    __invalid_parameter_noinfo
005C177B  mov     eax, [ebp+var_98]
005C1781  lea     edx, [ebp+var_20]
005C1784  call    sub_408680
005C1789  lea     eax, [ebp+var_20]
005C178C  push    eax
005C178D  lea     ecx, [ebp+var_8]
005C1790  call    sub_632700
005C1795  movzx   ecx, al
005C1798  neg     ecx
005C179A  sbb     ecx, ecx
005C179C  add     ecx, 1
005C179F  movzx   edx, cl
005C17A2  test    edx, edx
005C17A4  jz      loc_5C18E6
005C17AA  mov     eax, [ebp+var_124]
005C17B0  add     eax, 2Ch ; ','
005C17B3  push    eax
005C17B4  lea     ecx, [ebp+var_8]
005C17B7  call    sub_632700
005C17BC  movzx   ecx, al
005C17BF  test    ecx, ecx
005C17C1  jz      short loc_5C1810
005C17C3  mov     edx, [ebp+var_124]
005C17C9  add     edx, 2Ch ; ','
005C17CC  mov     [ebp+var_A8], edx
005C17D2  mov     eax, [ebp+var_A8]
005C17D8  mov     ecx, [eax]
005C17DA  mov     edx, [eax+4]
005C17DD  mov     [ebp+var_A0], ecx
005C17E3  mov     [ebp+var_9C], edx
005C17E9  mov     ecx, [ebp+var_A8]
005C17EF  call    sub_5D3980
005C17F4  mov     eax, [ebp+var_A0]
005C17FA  mov     [ebp+var_28], eax
005C17FD  mov     ecx, [ebp+var_9C]
005C1803  mov     [ebp+var_24], ecx
005C1806  mov     edx, [ebp+var_124]
005C180C  mov     byte ptr [edx+28h], 1
005C1810  mov     eax, [ebp+var_8]
005C1813  mov     [ebp+var_30], eax
005C1816  mov     ecx, [ebp+var_4]
005C1819  mov     [ebp+var_2C], ecx
005C181C  mov     edx, [ebp+var_2C]
005C181F  push    edx
005C1820  mov     eax, [ebp+var_30]
005C1823  push    eax
005C1824  lea     ecx, [ebp+var_38]
005C1827  push    ecx
005C1828  mov     ecx, [ebp+var_124]
005C182E  add     ecx, 4
005C1831  call    sub_5D1840
005C1836  mov     edx, [ebp+var_124]
005C183C  add     edx, 4
005C183F  mov     [ebp+var_D0], edx
005C1845  mov     eax, [ebp+var_D0]
005C184B  mov     ecx, [eax+14h]
005C184E  mov     [ebp+var_CC], ecx
005C1854  lea     eax, [ebp+var_40]
005C1857  call    sub_4086A0
005C185C  mov     edx, [ebp+var_CC]
005C1862  mov     [ebp+var_3C], edx
005C1865  mov     eax, 1
005C186A  test    eax, eax
005C186C  jz      short loc_5C187C
005C186E  cmp     [ebp+var_D0], 0
005C1875  jnz     short loc_5C187C
005C1877  call    __invalid_parameter_noinfo
005C187C  mov     eax, [ebp+var_D0]
005C1882  lea     edx, [ebp+var_40]
005C1885  call    sub_408680
005C188A  lea     ecx, [ebp+arg_4]
005C188D  push    ecx
005C188E  mov     edx, [ebp+var_3C]
005C1891  push    edx
005C1892  mov     eax, [ebp+var_40]
005C1895  push    eax
005C1896  lea     ecx, [ebp+var_48]
005C1899  push    ecx
005C189A  mov     ecx, [ebp+var_124]
005C18A0  add     ecx, 4
005C18A3  call    sub_62EFA0
005C18A8  mov     edx, [eax+4]
005C18AB  push    edx
005C18AC  mov     eax, [eax]
005C18AE  push    eax
005C18AF  lea     ecx, [ebp+var_50]
005C18B2  push    ecx
005C18B3  call    sub_5D48D0
005C18B8  add     esp, 18h
005C18BB  mov     edx, [eax]
005C18BD  mov     eax, [eax+4]
005C18C0  mov     [ebp+var_8], edx
005C18C3  mov     [ebp+var_4], eax
005C18C6  mov     ecx, [ebp+arg_0]
005C18C9  push    ecx
005C18CA  lea     edx, [ebp+var_8]
005C18CD  push    edx
005C18CE  mov     ecx, [ebp+var_124]
005C18D4  call    sub_5C0FD0
005C18D9  mov     eax, [ebp+arg_0]
005C18DC  mov     edx, [eax]
005C18DE  mov     ecx, [ebp+arg_0]
005C18E1  mov     eax, [edx+78h]
005C18E4  call    eax
005C18E6  mov     esp, ebp
005C18E8  pop     ebp
005C18E9  retn    8
