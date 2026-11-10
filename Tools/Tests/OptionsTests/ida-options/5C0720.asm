005C0720  push    ebp
005C0721  mov     ebp, esp
005C0723  sub     esp, 94h
005C0729  mov     [ebp+var_94], ecx
005C072F  mov     eax, [ebp+var_94]
005C0735  add     eax, 4
005C0738  mov     [ebp+var_30], eax
005C073B  mov     ecx, [ebp+var_30]
005C073E  mov     edx, [ecx+14h]
005C0741  mov     [ebp+var_2C], edx
005C0744  lea     eax, [ebp+var_20]
005C0747  call    sub_4086A0
005C074C  mov     eax, [ebp+var_2C]
005C074F  mov     [ebp+var_1C], eax
005C0752  mov     ecx, 1
005C0757  test    ecx, ecx
005C0759  jz      short loc_5C0766
005C075B  cmp     [ebp+var_30], 0
005C075F  jnz     short loc_5C0766
005C0761  call    __invalid_parameter_noinfo
005C0766  mov     eax, [ebp+var_30]
005C0769  lea     edx, [ebp+var_20]
005C076C  call    sub_408680
005C0771  mov     edx, [ebp+var_94]
005C0777  add     edx, 4
005C077A  mov     [ebp+var_38], edx
005C077D  mov     eax, [ebp+var_38]
005C0780  mov     ecx, [eax+14h]
005C0783  mov     [ebp+var_34], ecx
005C0786  lea     eax, [ebp+var_8]
005C0789  call    sub_4086A0
005C078E  mov     edx, [ebp+var_34]
005C0791  mov     [ebp+var_4], edx
005C0794  mov     eax, 1
005C0799  test    eax, eax
005C079B  jz      short loc_5C07A8
005C079D  cmp     [ebp+var_38], 0
005C07A1  jnz     short loc_5C07A8
005C07A3  call    __invalid_parameter_noinfo
005C07A8  mov     eax, [ebp+var_38]
005C07AB  lea     edx, [ebp+var_8]
005C07AE  call    sub_408680
005C07B3  lea     ecx, [ebp+var_20]
005C07B6  push    ecx
005C07B7  lea     edx, [ebp+arg_0]
005C07BA  push    edx
005C07BB  mov     eax, [ebp+var_4]
005C07BE  push    eax
005C07BF  mov     ecx, [ebp+var_8]
005C07C2  push    ecx
005C07C3  lea     edx, [ebp+var_10]
005C07C6  push    edx
005C07C7  mov     ecx, [ebp+var_94]
005C07CD  add     ecx, 4
005C07D0  call    sub_62EFA0
005C07D5  mov     ecx, [eax+4]
005C07D8  push    ecx
005C07D9  mov     edx, [eax]
005C07DB  push    edx
005C07DC  lea     eax, [ebp+var_18]
005C07DF  push    eax
005C07E0  call    sub_5D48D0
005C07E5  add     esp, 18h
005C07E8  mov     ecx, eax
005C07EA  call    sub_632700
005C07EF  movzx   ecx, al
005C07F2  test    ecx, ecx
005C07F4  jz      loc_5C08CD
005C07FA  mov     edx, [ebp+var_94]
005C0800  add     edx, 4
005C0803  mov     [ebp+var_90], edx
005C0809  mov     eax, [ebp+var_90]
005C080F  mov     ecx, [eax+14h]
005C0812  mov     [ebp+var_8C], ecx
005C0818  lea     eax, [ebp+var_28]
005C081B  call    sub_4086A0
005C0820  mov     edx, [ebp+var_8C]
005C0826  mov     [ebp+var_24], edx
005C0829  mov     eax, 1
005C082E  test    eax, eax
005C0830  jz      short loc_5C0840
005C0832  cmp     [ebp+var_90], 0
005C0839  jnz     short loc_5C0840
005C083B  call    __invalid_parameter_noinfo
005C0840  mov     eax, [ebp+var_90]
005C0846  lea     edx, [ebp+var_28]
005C0849  call    sub_408680
005C084E  mov     ecx, [ebp+arg_0]
005C0851  push    ecx
005C0852  lea     edx, [ebp+var_28]
005C0855  push    edx
005C0856  mov     ecx, [ebp+var_94]
005C085C  call    sub_5C0FD0
005C0861  mov     eax, [ebp+arg_0]
005C0864  mov     ecx, [ebp+var_94]
005C086A  mov     edx, [ecx+20h]
005C086D  mov     [eax+20h], edx
005C0870  mov     eax, [ebp+arg_0]
005C0873  mov     ecx, [ebp+var_94]
005C0879  mov     [eax+24h], ecx
005C087C  mov     edx, [ebp+var_94]
005C0882  cmp     dword ptr [edx+20h], 0
005C0886  jz      short loc_5C08BA
005C0888  mov     eax, [ebp+var_94]
005C088E  mov     ecx, [eax+20h]
005C0891  push    ecx
005C0892  mov     edx, [ebp+arg_0]
005C0895  mov     eax, [edx]
005C0897  mov     ecx, [ebp+arg_0]
005C089A  mov     edx, [eax+50h]
005C089D  call    edx
005C089F  mov     eax, [ebp+arg_0]
005C08A2  mov     edx, [eax]
005C08A4  mov     ecx, [ebp+arg_0]
005C08A7  mov     eax, [edx+4Ch]
005C08AA  call    eax
005C08AC  mov     ecx, [ebp+var_94]
005C08B2  mov     ecx, [ecx+20h]
005C08B5  call    sub_5C41E0
005C08BA  mov     edx, [ebp+var_94]
005C08C0  mov     eax, [edx]
005C08C2  mov     ecx, [ebp+var_94]
005C08C8  mov     edx, [eax+44h]
005C08CB  call    edx
005C08CD  mov     esp, ebp
005C08CF  pop     ebp
005C08D0  retn    4
