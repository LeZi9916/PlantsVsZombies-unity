004DFF20  push    ebp
004DFF21  mov     ebp, esp
004DFF23  and     esp, 0FFFFFFF8h
004DFF26  sub     esp, 114h
004DFF2C  cmp     byte_755E37, 0
004DFF33  push    ebx
004DFF34  push    esi
004DFF35  push    edi
004DFF36  jz      short loc_4DFF58
004DFF38  mov     eax, [ebp+arg_10]
004DFF3B  mov     ecx, [ebp+arg_4]
004DFF3E  mov     edx, [ebp+arg_8]
004DFF41  push    eax
004DFF42  mov     eax, [ebp+arg_0]
004DFF45  push    ecx
004DFF46  mov     ecx, [ebp+arg_C]
004DFF49  call    sub_4E2070
004DFF4E  add     esp, 8
004DFF51  pop     edi
004DFF52  pop     esi
004DFF53  pop     ebx
004DFF54  mov     esp, ebp
004DFF56  pop     ebp
004DFF57  retn
004DFF58  mov     eax, [ebp+arg_8]
004DFF5B  shr     eax, 2
004DFF5E  mov     [esp+120h+var_88], eax
004DFF65  mov     eax, [ebp+arg_C]
004DFF68  mov     edx, [eax+10h]
004DFF6B  mov     ecx, [eax+18h]
004DFF6E  mov     esi, [eax]
004DFF70  mov     eax, [ebp+arg_0]
004DFF73  mov     [esp+120h+var_A8], edx
004DFF77  mov     edx, [eax+4]
004DFF7A  cmp     edx, [eax+24h]
004DFF7D  mov     [esp+120h+var_4], ecx
004DFF84  lea     ecx, [eax+20h]
004DFF87  lea     ebx, [eax+40h]
004DFF8A  mov     [esp+120h+var_24], esi
004DFF91  mov     edi, eax
004DFF93  mov     [esp+120h+var_E8], ecx
004DFF97  mov     [esp+120h+var_8C], ebx
004DFF9E  jle     short loc_4DFFA8
004DFFA0  mov     edi, ecx
004DFFA2  mov     ecx, eax
004DFFA4  mov     [esp+120h+var_E8], eax
004DFFA8  mov     eax, [ecx+4]
004DFFAB  cmp     eax, [ebx+4]
004DFFAE  jle     short loc_4DFFC1
004DFFB0  mov     eax, ecx
004DFFB2  mov     ecx, ebx
004DFFB4  mov     [esp+120h+var_E8], ecx
004DFFB8  mov     ebx, eax
004DFFBA  mov     [esp+120h+var_8C], eax
004DFFC1  mov     edx, [edi+4]
004DFFC4  cmp     edx, [ecx+4]
004DFFC7  jle     short loc_4DFFD3
004DFFC9  mov     eax, edi
004DFFCB  mov     edi, ecx
004DFFCD  mov     ecx, eax
004DFFCF  mov     [esp+120h+var_E8], ecx
004DFFD3  mov     eax, [ebp+arg_10]
004DFFD6  mov     edx, [edi+8]
004DFFD9  imul    edx, [eax]
004DFFDC  shr     edx, 8
004DFFDF  mov     [edi+8], edx
004DFFE2  mov     edx, [eax+4]
004DFFE5  imul    edx, [edi+0Ch]
004DFFE9  shr     edx, 8
004DFFEC  mov     [edi+0Ch], edx
004DFFEF  mov     edx, [eax+8]
004DFFF2  imul    edx, [edi+10h]
004DFFF6  shr     edx, 8
004DFFF9  mov     [edi+10h], edx
004DFFFC  mov     edx, [eax+0Ch]
004DFFFF  imul    edx, [edi+14h]
004E0003  shr     edx, 8
004E0006  mov     [edi+14h], edx
004E0009  mov     edx, [ecx+8]
004E000C  imul    edx, [eax]
004E000F  shr     edx, 8
004E0012  mov     [ecx+8], edx
004E0015  mov     edx, [ecx+0Ch]
004E0018  imul    edx, [eax+4]
004E001C  shr     edx, 8
004E001F  mov     [ecx+0Ch], edx
004E0022  mov     edx, [eax+8]
004E0025  imul    edx, [ecx+10h]
004E0029  shr     edx, 8
004E002C  mov     [ecx+10h], edx
004E002F  mov     edx, [eax+0Ch]
004E0032  imul    edx, [ecx+14h]
004E0036  shr     edx, 8
004E0039  mov     [ecx+14h], edx
004E003C  mov     ecx, [ebx+8]
004E003F  imul    ecx, [eax]
004E0042  mov     edx, [ebx+0Ch]
004E0045  shr     ecx, 8
004E0048  mov     [ebx+8], ecx
004E004B  imul    edx, [eax+4]
004E004F  shr     edx, 8
004E0052  mov     [ebx+0Ch], edx
004E0055  mov     ecx, [eax+8]
004E0058  imul    ecx, [ebx+10h]
004E005C  shr     ecx, 8
004E005F  mov     [ebx+10h], ecx
004E0062  mov     eax, [eax+0Ch]
004E0065  imul    eax, [ebx+14h]
004E0069  shr     eax, 8
004E006C  mov     [esp+120h+var_108], eax
004E0070  mov     [ebx+14h], eax
004E0073  mov     eax, [edi+4]
004E0076  lea     ecx, [eax+0FFFFh]
004E007C  mov     [esp+120h+var_100], eax
004E0080  mov     eax, [ebx+4]
004E0083  lea     edx, [eax+0FFFFh]
004E0089  sar     ecx, 10h
004E008C  sar     edx, 10h
004E008F  mov     [esp+120h+var_C0], ecx
004E0093  mov     [esp+120h+var_84], edx
004E009A  cmp     ecx, edx
004E009C  jz      loc_4E2062
004E00A2  mov     edx, [esp+120h+var_E8]
004E00A6  sub     eax, [esp+120h+var_100]
004E00AA  mov     ecx, [edx+4]
004E00AD  cdq
004E00AE  push    edx
004E00AF  push    eax
004E00B0  add     ecx, 0FFFFh
004E00B6  push    10000h
004E00BB  sar     ecx, 10h
004E00BE  push    0
004E00C0  mov     [esp+130h+var_80], ecx
004E00C7  call    __alldiv
004E00CC  mov     ecx, edx
004E00CE  mov     edx, [edi]
004E00D0  mov     dword ptr [esp+120h+var_F8], eax
004E00D4  mov     eax, [ebx]
004E00D6  push    ecx
004E00D7  mov     dword ptr [esp+124h+var_F8+4], ecx
004E00DB  mov     ecx, dword ptr [esp+124h+var_F8]
004E00DF  sub     eax, edx
004E00E1  mov     [esp+124h+var_10C], edx
004E00E5  cdq
004E00E6  push    ecx
004E00E7  push    edx
004E00E8  push    eax
004E00E9  call    __allmul
004E00EE  mov     cl, 20h ; ' '
004E00F0  call    __allshr
004E00F5  mov     ecx, [edi+8]
004E00F8  mov     [esp+120h+var_C], eax
004E00FF  mov     eax, [ebx+8]
004E0102  sub     eax, ecx
004E0104  mov     [esp+120h+var_EC], ecx
004E0108  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E010C  push    ecx
004E010D  mov     ecx, dword ptr [esp+124h+var_F8]
004E0111  cdq
004E0112  push    ecx
004E0113  push    edx
004E0114  push    eax
004E0115  call    __allmul
004E011A  mov     cl, 20h ; ' '
004E011C  call    __allshr
004E0121  mov     ecx, [edi+0Ch]
004E0124  mov     [esp+120h+var_8], eax
004E012B  mov     eax, [ebx+0Ch]
004E012E  sub     eax, ecx
004E0130  mov     [esp+120h+var_D4], ecx
004E0134  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E0138  push    ecx
004E0139  mov     ecx, dword ptr [esp+124h+var_F8]
004E013D  cdq
004E013E  push    ecx
004E013F  push    edx
004E0140  push    eax
004E0141  call    __allmul
004E0146  mov     cl, 20h ; ' '
004E0148  call    __allshr
004E014D  mov     ecx, [edi+10h]
004E0150  mov     [esp+120h+var_18], eax
004E0157  mov     eax, [ebx+10h]
004E015A  sub     eax, ecx
004E015C  mov     [esp+120h+var_FC], ecx
004E0160  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E0164  push    ecx
004E0165  mov     ecx, dword ptr [esp+124h+var_F8]
004E0169  cdq
004E016A  push    ecx
004E016B  push    edx
004E016C  push    eax
004E016D  call    __allmul
004E0172  mov     cl, 20h ; ' '
004E0174  call    __allshr
004E0179  mov     ecx, [edi+14h]
004E017C  mov     [esp+120h+var_10], eax
004E0183  mov     eax, [esp+120h+var_108]
004E0187  sub     eax, ecx
004E0189  mov     [esp+120h+var_DC], ecx
004E018D  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E0191  cdq
004E0192  push    ecx
004E0193  mov     ecx, dword ptr [esp+124h+var_F8]
004E0197  push    ecx
004E0198  push    edx
004E0199  push    eax
004E019A  call    __allmul
004E019F  mov     cl, 20h ; ' '
004E01A1  call    __allshr
004E01A6  mov     ecx, [edi+18h]
004E01A9  mov     [esp+120h+var_1C], eax
004E01B0  mov     eax, [ebx+18h]
004E01B3  sub     eax, ecx
004E01B5  mov     [esp+120h+var_E0], ecx
004E01B9  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E01BD  push    ecx
004E01BE  mov     ecx, dword ptr [esp+124h+var_F8]
004E01C2  cdq
004E01C3  push    ecx
004E01C4  push    edx
004E01C5  push    eax
004E01C6  call    __allmul
004E01CB  mov     cl, 20h ; ' '
004E01CD  call    __allshr
004E01D2  mov     edi, [edi+1Ch]
004E01D5  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E01D9  mov     [esp+120h+var_20], eax
004E01E0  mov     eax, [ebx+1Ch]
004E01E3  push    ecx
004E01E4  mov     ecx, dword ptr [esp+124h+var_F8]
004E01E8  sub     eax, edi
004E01EA  cdq
004E01EB  push    ecx
004E01EC  push    edx
004E01ED  push    eax
004E01EE  mov     [esp+130h+var_C4], edi
004E01F2  call    __allmul
004E01F7  mov     cl, 20h ; ' '
004E01F9  call    __allshr
004E01FE  mov     edx, [esp+120h+var_E8]
004E0202  mov     [esp+120h+var_14], eax
004E0209  mov     eax, [edx+4]
004E020C  sub     eax, [esp+120h+var_100]
004E0210  cdq
004E0211  mov     ecx, eax
004E0213  mov     eax, [esp+120h+var_C]
004E021A  mov     ebx, edx
004E021C  push    ebx
004E021D  cdq
004E021E  push    ecx
004E021F  mov     edi, eax
004E0221  push    edx
004E0222  push    edi
004E0223  mov     dword ptr [esp+130h+var_D0], ecx
004E0227  mov     dword ptr [esp+130h+var_D0+4], ebx
004E022B  mov     dword ptr [esp+130h+var_78+4], edx
004E0232  call    __allmul
004E0237  mov     cl, 10h
004E0239  call    __allshr
004E023E  add     eax, [esp+120h+var_10C]
004E0242  mov     ecx, [esp+120h+var_E8]
004E0246  mov     ecx, [ecx]
004E0248  mov     [esp+120h+var_E4], eax
004E024C  mov     [esp+120h+var_104], ecx
004E0250  cmp     ecx, eax
004E0252  jz      loc_4E2062
004E0258  mov     eax, [esp+120h+var_C0]
004E025C  shl     eax, 10h
004E025F  sub     eax, [esp+120h+var_100]
004E0263  cdq
004E0264  push    edx
004E0265  mov     ebx, eax
004E0267  mov     dword ptr [esp+124h+var_98+4], edx
004E026E  mov     edx, dword ptr [esp+124h+var_78+4]
004E0275  push    ebx
004E0276  push    edx
004E0277  push    edi
004E0278  mov     dword ptr [esp+130h+var_98], ebx
004E027F  call    __allmul
004E0284  mov     cl, 10h
004E0286  call    __allshr
004E028B  mov     ecx, dword ptr [esp+120h+var_98+4]
004E0292  mov     edi, eax
004E0294  mov     eax, [esp+120h+var_8]
004E029B  add     edi, [esp+120h+var_10C]
004E029F  cdq
004E02A0  push    ecx
004E02A1  push    ebx
004E02A2  push    edx
004E02A3  push    eax
004E02A4  mov     [esp+130h+var_BC], edi
004E02A8  mov     dword ptr [esp+130h+var_78], eax
004E02AF  mov     dword ptr [esp+130h+var_78+4], edx
004E02B6  call    __allmul
004E02BB  mov     cl, 10h
004E02BD  call    __allshr
004E02C2  add     eax, [esp+120h+var_EC]
004E02C6  mov     ecx, dword ptr [esp+120h+var_98+4]
004E02CD  mov     [esp+120h+var_B0], eax
004E02D1  mov     eax, [esp+120h+var_18]
004E02D8  cdq
004E02D9  push    ecx
004E02DA  push    ebx
004E02DB  push    edx
004E02DC  push    eax
004E02DD  mov     dword ptr [esp+130h+var_50], eax
004E02E4  mov     dword ptr [esp+130h+var_50+4], edx
004E02EB  call    __allmul
004E02F0  mov     cl, 10h
004E02F2  call    __allshr
004E02F7  add     eax, [esp+120h+var_D4]
004E02FB  mov     ecx, dword ptr [esp+120h+var_98+4]
004E0302  mov     [esp+120h+var_A4], eax
004E0306  mov     eax, [esp+120h+var_10]
004E030D  cdq
004E030E  push    ecx
004E030F  push    ebx
004E0310  push    edx
004E0311  push    eax
004E0312  mov     dword ptr [esp+130h+var_60], eax
004E0319  mov     dword ptr [esp+130h+var_60+4], edx
004E0320  call    __allmul
004E0325  mov     cl, 10h
004E0327  call    __allshr
004E032C  add     eax, [esp+120h+var_FC]
004E0330  mov     ecx, dword ptr [esp+120h+var_98+4]
004E0337  mov     [esp+120h+var_AC], eax
004E033B  mov     eax, [esp+120h+var_1C]
004E0342  cdq
004E0343  push    ecx
004E0344  push    ebx
004E0345  push    edx
004E0346  push    eax
004E0347  mov     dword ptr [esp+130h+var_68], eax
004E034E  mov     dword ptr [esp+130h+var_68+4], edx
004E0355  call    __allmul
004E035A  mov     cl, 10h
004E035C  call    __allshr
004E0361  add     eax, [esp+120h+var_DC]
004E0365  mov     ecx, dword ptr [esp+120h+var_98+4]
004E036C  mov     [esp+120h+var_B4], eax
004E0370  mov     eax, [esp+120h+var_20]
004E0377  cdq
004E0378  push    ecx
004E0379  mov     dword ptr [esp+124h+var_58], eax
004E0380  mov     dword ptr [esp+124h+var_58+4], edx
004E0387  push    ebx
004E0388  push    edx
004E0389  push    eax
004E038A  call    __allmul
004E038F  mov     cl, 10h
004E0391  call    __allshr
004E0396  add     eax, [esp+120h+var_E0]
004E039A  mov     ecx, dword ptr [esp+120h+var_98+4]
004E03A1  mov     [esp+120h+var_A0], eax
004E03A8  mov     eax, [esp+120h+var_14]
004E03AF  cdq
004E03B0  push    ecx
004E03B1  push    ebx
004E03B2  push    edx
004E03B3  push    eax
004E03B4  mov     dword ptr [esp+130h+var_70], eax
004E03BB  mov     dword ptr [esp+130h+var_70+4], edx
004E03C2  call    __allmul
004E03C7  mov     cl, 10h
004E03C9  call    __allshr
004E03CE  add     eax, [esp+120h+var_C4]
004E03D2  mov     [esp+120h+var_9C], eax
004E03D9  mov     eax, [esp+120h+var_104]
004E03DD  sub     eax, [esp+120h+var_E4]
004E03E1  cdq
004E03E2  push    edx
004E03E3  push    eax
004E03E4  push    10000h
004E03E9  push    0
004E03EB  call    __alldiv
004E03F0  mov     ecx, dword ptr [esp+120h+var_78+4]
004E03F7  mov     dword ptr [esp+120h+var_F8+4], edx
004E03FB  mov     edx, dword ptr [esp+120h+var_D0+4]
004E03FF  push    edx
004E0400  mov     edx, dword ptr [esp+124h+var_78]
004E0407  mov     dword ptr [esp+124h+var_F8], eax
004E040B  mov     eax, dword ptr [esp+124h+var_D0]
004E040F  push    eax
004E0410  push    ecx
004E0411  push    edx
004E0412  call    __allmul
004E0417  mov     cl, 10h
004E0419  call    __allshr
004E041E  mov     ebx, edx
004E0420  mov     edx, [esp+120h+var_E8]
004E0424  mov     ecx, eax
004E0426  mov     eax, [edx+8]
004E0429  cdq
004E042A  sub     eax, ecx
004E042C  sbb     edx, ebx
004E042E  mov     ecx, eax
004E0430  mov     eax, [esp+120h+var_EC]
004E0434  mov     ebx, edx
004E0436  cdq
004E0437  sub     ecx, eax
004E0439  mov     eax, dword ptr [esp+120h+var_F8]
004E043D  sbb     ebx, edx
004E043F  mov     edx, dword ptr [esp+120h+var_F8+4]
004E0443  push    edx
004E0444  push    eax
004E0445  push    ebx
004E0446  push    ecx
004E0447  call    __allmul
004E044C  mov     cl, 20h ; ' '
004E044E  call    __allshr
004E0453  mov     ecx, dword ptr [esp+120h+var_D0+4]
004E0457  mov     edx, dword ptr [esp+120h+var_D0]
004E045B  push    ecx
004E045C  mov     ecx, dword ptr [esp+124h+var_50]
004E0463  mov     [esp+124h+var_34], eax
004E046A  mov     eax, dword ptr [esp+124h+var_50+4]
004E0471  push    edx
004E0472  push    eax
004E0473  push    ecx
004E0474  call    __allmul
004E0479  mov     cl, 10h
004E047B  call    __allshr
004E0480  mov     ebx, edx
004E0482  mov     edx, [esp+120h+var_E8]
004E0486  mov     ecx, eax
004E0488  mov     eax, [edx+0Ch]
004E048B  cdq
004E048C  sub     eax, ecx
004E048E  sbb     edx, ebx
004E0490  mov     ecx, eax
004E0492  mov     eax, [esp+120h+var_D4]
004E0496  mov     ebx, edx
004E0498  cdq
004E0499  sub     ecx, eax
004E049B  mov     eax, dword ptr [esp+120h+var_F8]
004E049F  sbb     ebx, edx
004E04A1  mov     edx, dword ptr [esp+120h+var_F8+4]
004E04A5  push    edx
004E04A6  push    eax
004E04A7  push    ebx
004E04A8  push    ecx
004E04A9  call    __allmul
004E04AE  mov     cl, 20h ; ' '
004E04B0  call    __allshr
004E04B5  mov     ecx, dword ptr [esp+120h+var_D0+4]
004E04B9  mov     edx, dword ptr [esp+120h+var_D0]
004E04BD  push    ecx
004E04BE  mov     ecx, dword ptr [esp+124h+var_60]
004E04C5  mov     [esp+124h+var_38], eax
004E04CC  mov     eax, dword ptr [esp+124h+var_60+4]
004E04D3  push    edx
004E04D4  push    eax
004E04D5  push    ecx
004E04D6  call    __allmul
004E04DB  mov     cl, 10h
004E04DD  call    __allshr
004E04E2  mov     ebx, edx
004E04E4  mov     edx, [esp+120h+var_E8]
004E04E8  mov     ecx, eax
004E04EA  mov     eax, [edx+10h]
004E04ED  cdq
004E04EE  sub     eax, ecx
004E04F0  sbb     edx, ebx
004E04F2  mov     ecx, eax
004E04F4  mov     eax, [esp+120h+var_FC]
004E04F8  mov     ebx, edx
004E04FA  cdq
004E04FB  sub     ecx, eax
004E04FD  mov     eax, dword ptr [esp+120h+var_F8]
004E0501  sbb     ebx, edx
004E0503  mov     edx, dword ptr [esp+120h+var_F8+4]
004E0507  push    edx
004E0508  push    eax
004E0509  push    ebx
004E050A  push    ecx
004E050B  call    __allmul
004E0510  mov     cl, 20h ; ' '
004E0512  call    __allshr
004E0517  mov     ecx, dword ptr [esp+120h+var_D0+4]
004E051B  mov     edx, dword ptr [esp+120h+var_D0]
004E051F  push    ecx
004E0520  mov     ecx, dword ptr [esp+124h+var_68]
004E0527  mov     [esp+124h+var_48], eax
004E052E  mov     eax, dword ptr [esp+124h+var_68+4]
004E0535  push    edx
004E0536  push    eax
004E0537  push    ecx
004E0538  call    __allmul
004E053D  mov     cl, 10h
004E053F  call    __allshr
004E0544  mov     ecx, eax
004E0546  mov     ebx, edx
004E0548  mov     edx, [esp+120h+var_E8]
004E054C  mov     eax, [edx+14h]
004E054F  cdq
004E0550  sub     eax, ecx
004E0552  mov     ecx, eax
004E0554  mov     eax, [esp+120h+var_DC]
004E0558  sbb     edx, ebx
004E055A  mov     ebx, edx
004E055C  cdq
004E055D  sub     ecx, eax
004E055F  mov     eax, dword ptr [esp+120h+var_F8]
004E0563  sbb     ebx, edx
004E0565  mov     edx, dword ptr [esp+120h+var_F8+4]
004E0569  push    edx
004E056A  push    eax
004E056B  push    ebx
004E056C  push    ecx
004E056D  call    __allmul
004E0572  mov     cl, 20h ; ' '
004E0574  call    __allshr
004E0579  mov     ecx, dword ptr [esp+120h+var_D0+4]
004E057D  mov     edx, dword ptr [esp+120h+var_D0]
004E0581  push    ecx
004E0582  mov     ecx, dword ptr [esp+124h+var_58]
004E0589  mov     [esp+124h+var_40], eax
004E0590  mov     eax, dword ptr [esp+124h+var_58+4]
004E0597  push    edx
004E0598  push    eax
004E0599  push    ecx
004E059A  call    __allmul
004E059F  mov     cl, 10h
004E05A1  call    __allshr
004E05A6  mov     ebx, edx
004E05A8  mov     edx, [esp+120h+var_E8]
004E05AC  mov     ecx, eax
004E05AE  mov     eax, [edx+18h]
004E05B1  cdq
004E05B2  sub     eax, ecx
004E05B4  sbb     edx, ebx
004E05B6  mov     ecx, eax
004E05B8  mov     eax, [esp+120h+var_E0]
004E05BC  mov     ebx, edx
004E05BE  cdq
004E05BF  sub     ecx, eax
004E05C1  mov     eax, dword ptr [esp+120h+var_F8]
004E05C5  sbb     ebx, edx
004E05C7  mov     edx, dword ptr [esp+120h+var_F8+4]
004E05CB  push    edx
004E05CC  push    eax
004E05CD  push    ebx
004E05CE  push    ecx
004E05CF  call    __allmul
004E05D4  mov     cl, 20h ; ' '
004E05D6  call    __allshr
004E05DB  mov     ecx, dword ptr [esp+120h+var_D0+4]
004E05DF  mov     edx, dword ptr [esp+120h+var_D0]
004E05E3  push    ecx
004E05E4  mov     ecx, dword ptr [esp+124h+var_70]
004E05EB  mov     [esp+124h+var_44], eax
004E05F2  mov     eax, dword ptr [esp+124h+var_70+4]
004E05F9  push    edx
004E05FA  push    eax
004E05FB  push    ecx
004E05FC  call    __allmul
004E0601  mov     cl, 10h
004E0603  call    __allshr
004E0608  mov     ebx, edx
004E060A  mov     edx, [esp+120h+var_E8]
004E060E  mov     ecx, eax
004E0610  mov     eax, [edx+1Ch]
004E0613  cdq
004E0614  sub     eax, ecx
004E0616  sbb     edx, ebx
004E0618  mov     ecx, eax
004E061A  mov     eax, [esp+120h+var_C4]
004E061E  mov     ebx, edx
004E0620  cdq
004E0621  sub     ecx, eax
004E0623  mov     eax, dword ptr [esp+120h+var_F8]
004E0627  sbb     ebx, edx
004E0629  mov     edx, dword ptr [esp+120h+var_F8+4]
004E062D  push    edx
004E062E  push    eax
004E062F  push    ebx
004E0630  push    ecx
004E0631  call    __allmul
004E0636  mov     cl, 20h ; ' '
004E0638  call    __allshr
004E063D  mov     ecx, [esp+120h+var_C0]
004E0641  mov     edx, ecx
004E0643  imul    edx, [esp+120h+var_88]
004E064B  mov     [esp+120h+var_3C], eax
004E0652  mov     eax, [ebp+arg_4]
004E0655  lea     edx, [eax+edx*4]
004E0658  mov     eax, [esp+120h+var_80]
004E065F  sub     eax, ecx
004E0661  mov     [esp+120h+var_B8], edx
004E0665  mov     [esp+120h+var_D8], eax
004E0669  jz      loc_4E1361
004E066F  mov     eax, [esp+120h+var_104]
004E0673  sub     eax, [esp+120h+var_10C]
004E0677  mov     ecx, dword ptr [esp+120h+var_D0]
004E067B  cdq
004E067C  mov     edi, eax
004E067E  mov     eax, dword ptr [esp+120h+var_D0+4]
004E0682  push    eax
004E0683  push    ecx
004E0684  push    10000h
004E0689  push    0
004E068B  mov     ebx, edx
004E068D  call    __alldiv
004E0692  push    edx
004E0693  push    eax
004E0694  push    ebx
004E0695  push    edi
004E0696  call    __allmul
004E069B  mov     cl, 20h ; ' '
004E069D  call    __allshr
004E06A2  mov     ecx, dword ptr [esp+120h+var_98+4]
004E06A9  push    ecx
004E06AA  mov     ecx, dword ptr [esp+124h+var_98]
004E06B1  mov     [esp+124h+var_108], eax
004E06B5  cdq
004E06B6  push    ecx
004E06B7  push    edx
004E06B8  push    eax
004E06B9  call    __allmul
004E06BE  mov     cl, 10h
004E06C0  call    __allshr
004E06C5  mov     edx, [esp+120h+var_E8]
004E06C9  mov     edi, [esp+120h+var_BC]
004E06CD  mov     ecx, eax
004E06CF  mov     eax, [edx]
004E06D1  mov     edx, [esp+120h+var_E4]
004E06D5  add     ecx, [esp+120h+var_10C]
004E06D9  cmp     edx, eax
004E06DB  jge     loc_4E0D11
004E06E1  cmp     [esp+120h+var_D8], 0
004E06E6  jle     loc_4E1361
004E06EC  mov     eax, [esp+120h+var_34]
004E06F3  cdq
004E06F4  mov     dword ptr [esp+120h+var_58], eax
004E06FB  mov     eax, [esp+120h+var_38]
004E0702  mov     dword ptr [esp+120h+var_58+4], edx
004E0709  cdq
004E070A  mov     dword ptr [esp+120h+var_68], eax
004E0711  mov     eax, [esp+120h+var_48]
004E0718  mov     dword ptr [esp+120h+var_68+4], edx
004E071F  cdq
004E0720  mov     dword ptr [esp+120h+var_60], eax
004E0727  mov     eax, [esp+120h+var_40]
004E072E  mov     dword ptr [esp+120h+var_60+4], edx
004E0735  cdq
004E0736  mov     dword ptr [esp+120h+var_50], eax
004E073D  mov     eax, [esp+120h+var_44]
004E0744  mov     dword ptr [esp+120h+var_50+4], edx
004E074B  cdq
004E074C  mov     dword ptr [esp+120h+var_98], eax
004E0753  mov     eax, [esp+120h+var_3C]
004E075A  mov     dword ptr [esp+120h+var_98+4], edx
004E0761  cdq
004E0762  mov     dword ptr [esp+120h+var_F8], eax
004E0766  add     ecx, 0FFFFh
004E076C  lea     eax, [edi+0FFFFh]
004E0772  mov     dword ptr [esp+120h+var_F8+4], edx
004E0776  mov     [esp+120h+var_FC], ecx
004E077A  mov     [esp+120h+var_DC], eax
004E077E  mov     edi, edi
004E0780  mov     eax, [esp+120h+var_DC]
004E0784  mov     ecx, dword ptr [esp+120h+var_58+4]
004E078B  dec     [esp+120h+var_D8]
004E078F  and     eax, 0FFFF0000h
004E0794  mov     [esp+120h+var_110], eax
004E0798  sub     eax, edi
004E079A  cdq
004E079B  push    edx
004E079C  mov     ebx, eax
004E079E  push    ebx
004E079F  mov     dword ptr [esp+128h+var_D0+4], edx
004E07A3  mov     edx, dword ptr [esp+128h+var_58]
004E07AA  push    ecx
004E07AB  push    edx
004E07AC  call    __allmul
004E07B1  mov     cl, 10h
004E07B3  call    __allshr
004E07B8  add     eax, [esp+120h+var_B0]
004E07BC  mov     ecx, dword ptr [esp+120h+var_68+4]
004E07C3  mov     edx, dword ptr [esp+120h+var_68]
004E07CA  mov     [esp+120h+var_28], eax
004E07D1  mov     eax, dword ptr [esp+120h+var_D0+4]
004E07D5  push    eax
004E07D6  push    ebx
004E07D7  push    ecx
004E07D8  push    edx
004E07D9  call    __allmul
004E07DE  mov     cl, 10h
004E07E0  call    __allshr
004E07E5  add     eax, [esp+120h+var_A4]
004E07E9  mov     ecx, dword ptr [esp+120h+var_60+4]
004E07F0  mov     edx, dword ptr [esp+120h+var_60]
004E07F7  mov     [esp+120h+var_7C], eax
004E07FE  mov     eax, dword ptr [esp+120h+var_D0+4]
004E0802  push    eax
004E0803  push    ebx
004E0804  push    ecx
004E0805  push    edx
004E0806  call    __allmul
004E080B  mov     cl, 10h
004E080D  call    __allshr
004E0812  add     eax, [esp+120h+var_AC]
004E0816  mov     ecx, dword ptr [esp+120h+var_50+4]
004E081D  mov     edx, dword ptr [esp+120h+var_50]
004E0824  mov     [esp+120h+var_C0], eax
004E0828  mov     eax, dword ptr [esp+120h+var_D0+4]
004E082C  push    eax
004E082D  push    ebx
004E082E  push    ecx
004E082F  push    edx
004E0830  call    __allmul
004E0835  mov     cl, 10h
004E0837  call    __allshr
004E083C  add     eax, [esp+120h+var_B4]
004E0840  mov     ecx, dword ptr [esp+120h+var_98+4]
004E0847  mov     edx, dword ptr [esp+120h+var_98]
004E084E  mov     [esp+120h+var_C4], eax
004E0852  mov     eax, dword ptr [esp+120h+var_D0+4]
004E0856  push    eax
004E0857  push    ebx
004E0858  push    ecx
004E0859  push    edx
004E085A  call    __allmul
004E085F  mov     cl, 10h
004E0861  call    __allshr
004E0866  add     eax, [esp+120h+var_A0]
004E086D  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E0871  mov     edx, dword ptr [esp+120h+var_F8]
004E0875  mov     dword ptr [esp+120h+var_70], eax
004E087C  mov     eax, dword ptr [esp+120h+var_D0+4]
004E0880  push    eax
004E0881  push    ebx
004E0882  push    ecx
004E0883  push    edx
004E0884  call    __allmul
004E0889  mov     cl, 10h
004E088B  call    __allshr
004E0890  mov     edx, [esp+120h+var_110]
004E0894  add     eax, [esp+120h+var_9C]
004E089B  mov     ecx, edx
004E089D  sar     ecx, 10h
004E08A0  mov     ebx, [esp+120h+var_B8]
004E08A4  lea     ecx, [ebx+ecx*4]
004E08A7  mov     [esp+120h+var_10C], ecx
004E08AB  mov     ecx, [esp+120h+var_FC]
004E08AF  and     ecx, 0FFFF0000h
004E08B5  sub     ecx, edx
004E08B7  sar     ecx, 10h
004E08BA  test    ecx, ecx
004E08BC  jle     loc_4E0C91
004E08C2  mov     edi, dword ptr [esp+120h+var_70]
004E08C9  lea     edx, [eax-8000h]
004E08CF  add     edi, 0FFFF8000h
004E08D5  mov     [esp+120h+var_104], edx
004E08D9  mov     [esp+120h+var_E0], edi
004E08DD  lea     ecx, [ecx+0]
004E08E0  mov     eax, [esp+120h+var_A8]
004E08E4  dec     ecx
004E08E5  mov     [esp+120h+var_110], ecx
004E08E9  test    edi, edi
004E08EB  jle     short loc_4E090F
004E08ED  and     edi, 0FFFF0000h
004E08F3  mov     edx, edi
004E08F5  sar     edx, 10h
004E08F8  xor     ebx, ebx
004E08FA  lea     ecx, [eax-1]
004E08FD  cmp     edx, ecx
004E08FF  mov     edx, [esp+120h+var_104]
004E0903  setl    bl
004E0906  mov     [esp+120h+var_90], ebx
004E090D  jmp     short loc_4E0918
004E090F  xor     edi, edi
004E0911  mov     [esp+120h+var_90], edi
004E0918  test    edx, edx
004E091A  jle     short loc_4E0943
004E091C  mov     ecx, [esp+120h+var_4]
004E0923  and     edx, 0FFFF0000h
004E0929  mov     eax, edx
004E092B  sar     eax, 10h
004E092E  dec     ecx
004E092F  xor     ebx, ebx
004E0931  cmp     eax, ecx
004E0933  mov     eax, [esp+120h+var_A8]
004E0937  setnl   bl
004E093A  dec     ebx
004E093B  and     ebx, eax
004E093D  mov     [esp+120h+var_EC], ebx
004E0941  jmp     short loc_4E0949
004E0943  xor     edx, edx
004E0945  mov     [esp+120h+var_EC], edx
004E0949  mov     ecx, edx
004E094B  sar     ecx, 10h
004E094E  imul    ecx, eax
004E0951  mov     eax, edi
004E0953  sar     eax, 10h
004E0956  add     ecx, eax
004E0958  mov     ebx, [esi+ecx*4]
004E095B  mov     eax, [esp+120h+var_90]
004E0962  add     eax, ecx
004E0964  mov     eax, [esi+eax*4]
004E0967  mov     [esp+120h+var_100], eax
004E096B  mov     eax, [esp+120h+var_EC]
004E096F  add     ecx, eax
004E0971  mov     eax, [esi+ecx*4]
004E0974  mov     dword ptr [esp+120h+var_70], eax
004E097B  mov     eax, [esp+120h+var_90]
004E0982  add     ecx, eax
004E0984  mov     ecx, [esi+ecx*4]
004E0987  mov     esi, [esp+120h+var_E0]
004E098B  mov     eax, [esp+120h+var_104]
004E098F  sub     eax, edx
004E0991  sub     esi, edi
004E0993  and     eax, 0FFFEh
004E0998  inc     eax
004E0999  and     esi, 0FFFEh
004E099F  inc     esi
004E09A0  mov     [esp+120h+var_EC], esi
004E09A4  mov     edi, 10000h
004E09A9  sub     edi, esi
004E09AB  mov     esi, edi
004E09AD  imul    edi, eax
004E09B0  mov     edx, 10000h
004E09B5  sub     edx, eax
004E09B7  imul    eax, [esp+120h+var_EC]
004E09BC  imul    esi, edx
004E09BF  imul    edx, [esp+120h+var_EC]
004E09C4  shr     edx, 10h
004E09C7  mov     [esp+120h+var_D4], ebx
004E09CB  shr     ebx, 18h
004E09CE  shr     esi, 10h
004E09D1  imul    esi, ebx
004E09D4  mov     ebx, [esp+120h+var_100]
004E09D8  shr     ebx, 18h
004E09DB  imul    edx, ebx
004E09DE  mov     ebx, dword ptr [esp+120h+var_70]
004E09E5  shr     edx, 10h
004E09E8  mov     [esp+120h+var_90], edx
004E09EF  shr     edi, 10h
004E09F2  shr     eax, 10h
004E09F5  mov     edx, ebx
004E09F7  shr     edx, 18h
004E09FA  imul    edi, edx
004E09FD  mov     edx, ecx
004E09FF  shr     edx, 18h
004E0A02  imul    eax, edx
004E0A05  shr     eax, 10h
004E0A08  mov     edx, eax
004E0A0A  mov     [esp+120h+var_30], edx
004E0A11  mov     eax, ecx
004E0A13  and     eax, 0FF0000h
004E0A18  imul    eax, edx
004E0A1B  shr     edi, 10h
004E0A1E  mov     edx, ebx
004E0A20  and     edx, 0FF0000h
004E0A26  imul    edx, edi
004E0A29  add     eax, edx
004E0A2B  mov     edx, [esp+120h+var_100]
004E0A2F  and     edx, 0FF0000h
004E0A35  imul    edx, [esp+120h+var_90]
004E0A3D  add     eax, edx
004E0A3F  mov     edx, [esp+120h+var_D4]
004E0A43  and     edx, 0FF0000h
004E0A49  shr     esi, 10h
004E0A4C  imul    edx, esi
004E0A4F  mov     [esp+120h+var_2C], edi
004E0A56  add     eax, edx
004E0A58  movzx   edi, cl
004E0A5B  imul    edi, [esp+120h+var_30]
004E0A63  movzx   edx, bl
004E0A66  and     eax, 0FF0000FFh
004E0A6B  imul    edx, [esp+120h+var_2C]
004E0A73  add     edi, edx
004E0A75  movzx   edx, byte ptr [esp+120h+var_100]
004E0A7A  imul    edx, [esp+120h+var_90]
004E0A82  add     edi, edx
004E0A84  mov     edx, [esp+120h+var_D4]
004E0A88  movzx   ebx, dl
004E0A8B  imul    ebx, esi
004E0A8E  add     edi, ebx
004E0A90  mov     ebx, [esp+120h+var_90]
004E0A97  and     ecx, 0FF00h
004E0A9D  imul    ecx, [esp+120h+var_30]
004E0AA5  and     edi, 0FF00h
004E0AAB  or      eax, edi
004E0AAD  mov     edi, dword ptr [esp+120h+var_70]
004E0AB4  and     edi, 0FF00h
004E0ABA  imul    edi, [esp+120h+var_2C]
004E0AC2  add     ecx, edi
004E0AC4  mov     edi, [esp+120h+var_100]
004E0AC8  and     edi, 0FF00h
004E0ACE  and     edx, 0FF00h
004E0AD4  imul    edi, ebx
004E0AD7  imul    edx, esi
004E0ADA  add     ecx, edi
004E0ADC  add     ecx, edx
004E0ADE  mov     edx, [esp+120h+var_2C]
004E0AE5  and     ecx, 0FF0000h
004E0AEB  or      eax, ecx
004E0AED  mov     ecx, [esp+120h+var_30]
004E0AF4  add     ecx, edx
004E0AF6  add     ecx, ebx
004E0AF8  add     ecx, esi
004E0AFA  shl     ecx, 18h
004E0AFD  and     ecx, 0FF000000h
004E0B03  shr     eax, 8
004E0B06  or      eax, ecx
004E0B08  mov     ecx, eax
004E0B0A  shr     ecx, 18h
004E0B0D  cmp     ecx, 8
004E0B10  jbe     loc_4E0C25
004E0B16  mov     edi, [ebp+arg_10]
004E0B19  mov     edx, [edi]
004E0B1B  imul    edx, [esp+120h+var_28]
004E0B23  shr     edx, 18h
004E0B26  mov     esi, edx
004E0B28  imul    esi, ecx
004E0B2B  mov     ecx, [edi+4]
004E0B2E  mov     edi, [edi+0Ch]
004E0B31  imul    ecx, [esp+120h+var_7C]
004E0B39  imul    edi, [esp+120h+var_C4]
004E0B3E  mov     ebx, eax
004E0B40  shr     ecx, 18h
004E0B43  shr     edi, 18h
004E0B46  and     ebx, 0FF0000h
004E0B4C  imul    ecx, ebx
004E0B4F  movzx   ebx, al
004E0B52  imul    edi, ebx
004E0B55  and     edi, 0FF00h
004E0B5B  and     ecx, 0FF0000FFh
004E0B61  or      ecx, edi
004E0B63  mov     edi, [ebp+arg_10]
004E0B66  mov     edi, [edi+8]
004E0B69  imul    edi, [esp+120h+var_C0]
004E0B6E  and     eax, 0FF00h
004E0B73  shr     edi, 18h
004E0B76  imul    edi, eax
004E0B79  and     edi, 0FF0000h
004E0B7F  or      ecx, edi
004E0B81  shr     ecx, 8
004E0B84  mov     eax, ecx
004E0B86  and     eax, 0FF0000h
004E0B8B  mov     edi, ecx
004E0B8D  imul    eax, edx
004E0B90  and     edi, 0FF00h
004E0B96  movzx   ecx, cl
004E0B99  imul    edi, edx
004E0B9C  imul    ecx, edx
004E0B9F  and     eax, 0FF0000FFh
004E0BA4  and     edi, 0FF0000h
004E0BAA  or      eax, edi
004E0BAC  and     ecx, 0FF00h
004E0BB2  or      eax, ecx
004E0BB4  shr     esi, 8
004E0BB7  shr     eax, 8
004E0BBA  cmp     esi, 0F0h
004E0BC0  jnb     short loc_4E0C1A
004E0BC2  mov     edx, [esp+120h+var_10C]
004E0BC6  mov     ecx, [edx]
004E0BC8  mov     edx, 0FFh
004E0BCD  sub     edx, esi
004E0BCF  mov     esi, ecx
004E0BD1  and     esi, 0FFFF00FFh
004E0BD7  mov     ebx, ecx
004E0BD9  imul    esi, edx
004E0BDC  and     ebx, 0FF00h
004E0BE2  imul    ebx, edx
004E0BE5  and     ecx, 0FF00FFh
004E0BEB  imul    ecx, edx
004E0BEE  xor     esi, ebx
004E0BF0  mov     edi, eax
004E0BF2  and     esi, 0FF0000h
004E0BF8  and     eax, 0FF00h
004E0BFD  xor     esi, ecx
004E0BFF  and     edi, 0FF00FFh
004E0C05  or      eax, edi
004E0C07  shr     esi, 8
004E0C0A  add     esi, eax
004E0C0C  mov     eax, [esp+120h+var_10C]
004E0C10  or      esi, 0FF000000h
004E0C16  mov     [eax], esi
004E0C18  jmp     short loc_4E0C25
004E0C1A  mov     ecx, [esp+120h+var_10C]
004E0C1E  or      eax, 0FF000000h
004E0C23  mov     [ecx], eax
004E0C25  mov     edx, [esp+120h+var_34]
004E0C2C  add     [esp+120h+var_28], edx
004E0C33  mov     edx, [esp+120h+var_40]
004E0C3A  add     [esp+120h+var_C4], edx
004E0C3E  mov     ecx, [esp+120h+var_48]
004E0C45  mov     edi, [esp+120h+var_E0]
004E0C49  mov     edx, [esp+120h+var_104]
004E0C4D  add     [esp+120h+var_C0], ecx
004E0C51  mov     eax, [esp+120h+var_38]
004E0C58  add     edi, [esp+120h+var_44]
004E0C5F  add     edx, [esp+120h+var_3C]
004E0C66  mov     ecx, [esp+120h+var_110]
004E0C6A  add     [esp+120h+var_10C], 4
004E0C6F  add     [esp+120h+var_7C], eax
004E0C76  mov     esi, [esp+120h+var_24]
004E0C7D  mov     [esp+120h+var_E0], edi
004E0C81  mov     [esp+120h+var_104], edx
004E0C85  test    ecx, ecx
004E0C87  jg      loc_4E08E0
004E0C8D  mov     edi, [esp+120h+var_BC]
004E0C91  mov     eax, [esp+120h+var_C]
004E0C98  add     [esp+120h+var_DC], eax
004E0C9C  mov     ecx, [esp+120h+var_8]
004E0CA3  mov     edx, [esp+120h+var_18]
004E0CAA  add     [esp+120h+var_B0], ecx
004E0CAE  add     [esp+120h+var_A4], edx
004E0CB2  mov     ecx, [esp+120h+var_1C]
004E0CB9  mov     edx, [esp+120h+var_20]
004E0CC0  add     [esp+120h+var_B4], ecx
004E0CC4  add     [esp+120h+var_A0], edx
004E0CCB  add     edi, eax
004E0CCD  mov     eax, [esp+120h+var_108]
004E0CD1  add     [esp+120h+var_FC], eax
004E0CD5  mov     eax, [esp+120h+var_88]
004E0CDC  add     eax, eax
004E0CDE  add     eax, eax
004E0CE0  add     [esp+120h+var_B8], eax
004E0CE4  mov     eax, [esp+120h+var_10]
004E0CEB  add     [esp+120h+var_AC], eax
004E0CEF  mov     eax, [esp+120h+var_14]
004E0CF6  add     [esp+120h+var_9C], eax
004E0CFD  cmp     [esp+120h+var_D8], 0
004E0D02  mov     [esp+120h+var_BC], edi
004E0D06  jg      loc_4E0780
004E0D0C  jmp     loc_4E1361
004E0D11  jle     loc_4E1361
004E0D17  cmp     [esp+120h+var_D8], 0
004E0D1C  jle     loc_4E1361
004E0D22  mov     eax, [esp+120h+var_34]
004E0D29  cdq
004E0D2A  mov     dword ptr [esp+120h+var_58], eax
004E0D31  mov     eax, [esp+120h+var_38]
004E0D38  mov     dword ptr [esp+120h+var_58+4], edx
004E0D3F  cdq
004E0D40  mov     dword ptr [esp+120h+var_68], eax
004E0D47  mov     eax, [esp+120h+var_48]
004E0D4E  mov     dword ptr [esp+120h+var_68+4], edx
004E0D55  cdq
004E0D56  mov     dword ptr [esp+120h+var_60], eax
004E0D5D  mov     eax, [esp+120h+var_40]
004E0D64  mov     dword ptr [esp+120h+var_60+4], edx
004E0D6B  cdq
004E0D6C  mov     dword ptr [esp+120h+var_50], eax
004E0D73  mov     eax, [esp+120h+var_44]
004E0D7A  mov     dword ptr [esp+120h+var_50+4], edx
004E0D81  cdq
004E0D82  mov     dword ptr [esp+120h+var_98], eax
004E0D89  mov     eax, [esp+120h+var_3C]
004E0D90  mov     dword ptr [esp+120h+var_98+4], edx
004E0D97  cdq
004E0D98  mov     dword ptr [esp+120h+var_F8+4], edx
004E0D9C  lea     edx, [edi+0FFFFh]
004E0DA2  add     ecx, 0FFFFh
004E0DA8  mov     dword ptr [esp+120h+var_F8], eax
004E0DAC  mov     [esp+120h+var_D4], edx
004E0DB0  mov     [esp+120h+var_EC], ecx
004E0DB4  jmp     short loc_4E0DC0
004E0DC0  mov     eax, [esp+120h+var_EC]
004E0DC4  mov     ecx, dword ptr [esp+120h+var_58]
004E0DCB  dec     [esp+120h+var_D8]
004E0DCF  and     eax, 0FFFF0000h
004E0DD4  mov     [esp+120h+var_110], eax
004E0DD8  sub     eax, edi
004E0DDA  cdq
004E0DDB  push    edx
004E0DDC  mov     ebx, eax
004E0DDE  mov     eax, dword ptr [esp+124h+var_58+4]
004E0DE5  push    ebx
004E0DE6  push    eax
004E0DE7  push    ecx
004E0DE8  mov     dword ptr [esp+130h+var_78+4], edx
004E0DEF  call    __allmul
004E0DF4  mov     cl, 10h
004E0DF6  call    __allshr
004E0DFB  add     eax, [esp+120h+var_B0]
004E0DFF  mov     edx, dword ptr [esp+120h+var_78+4]
004E0E06  mov     ecx, dword ptr [esp+120h+var_68]
004E0E0D  push    edx
004E0E0E  mov     [esp+124h+var_C0], eax
004E0E12  mov     eax, dword ptr [esp+124h+var_68+4]
004E0E19  push    ebx
004E0E1A  push    eax
004E0E1B  push    ecx
004E0E1C  call    __allmul
004E0E21  mov     cl, 10h
004E0E23  call    __allshr
004E0E28  add     eax, [esp+120h+var_A4]
004E0E2C  mov     edx, dword ptr [esp+120h+var_78+4]
004E0E33  mov     ecx, dword ptr [esp+120h+var_60]
004E0E3A  push    edx
004E0E3B  mov     [esp+124h+var_7C], eax
004E0E42  mov     eax, dword ptr [esp+124h+var_60+4]
004E0E49  push    ebx
004E0E4A  push    eax
004E0E4B  push    ecx
004E0E4C  call    __allmul
004E0E51  mov     cl, 10h
004E0E53  call    __allshr
004E0E58  add     eax, [esp+120h+var_AC]
004E0E5C  mov     edx, dword ptr [esp+120h+var_78+4]
004E0E63  mov     ecx, dword ptr [esp+120h+var_50]
004E0E6A  push    edx
004E0E6B  mov     [esp+124h+var_28], eax
004E0E72  mov     eax, dword ptr [esp+124h+var_50+4]
004E0E79  push    ebx
004E0E7A  push    eax
004E0E7B  push    ecx
004E0E7C  call    __allmul
004E0E81  mov     cl, 10h
004E0E83  call    __allshr
004E0E88  add     eax, [esp+120h+var_B4]
004E0E8C  mov     edx, dword ptr [esp+120h+var_78+4]
004E0E93  mov     ecx, dword ptr [esp+120h+var_98]
004E0E9A  push    edx
004E0E9B  mov     [esp+124h+var_2C], eax
004E0EA2  mov     eax, dword ptr [esp+124h+var_98+4]
004E0EA9  push    ebx
004E0EAA  push    eax
004E0EAB  push    ecx
004E0EAC  call    __allmul
004E0EB1  mov     cl, 10h
004E0EB3  call    __allshr
004E0EB8  add     eax, [esp+120h+var_A0]
004E0EBF  mov     edx, dword ptr [esp+120h+var_78+4]
004E0EC6  mov     ecx, dword ptr [esp+120h+var_F8]
004E0ECA  push    edx
004E0ECB  mov     dword ptr [esp+124h+var_70], eax
004E0ED2  mov     eax, dword ptr [esp+124h+var_F8+4]
004E0ED6  push    ebx
004E0ED7  push    eax
004E0ED8  push    ecx
004E0ED9  call    __allmul
004E0EDE  mov     cl, 10h
004E0EE0  call    __allshr
004E0EE5  mov     edx, [esp+120h+var_110]
004E0EE9  add     eax, [esp+120h+var_9C]
004E0EF0  mov     ecx, edx
004E0EF2  sar     ecx, 10h
004E0EF5  mov     ebx, [esp+120h+var_B8]
004E0EF9  lea     ecx, [ebx+ecx*4]
004E0EFC  mov     [esp+120h+var_10C], ecx
004E0F00  mov     ecx, [esp+120h+var_D4]
004E0F04  and     ecx, 0FFFF0000h
004E0F0A  sub     ecx, edx
004E0F0C  sar     ecx, 10h
004E0F0F  test    ecx, ecx
004E0F11  jle     loc_4E12E6
004E0F17  mov     edi, dword ptr [esp+120h+var_70]
004E0F1E  lea     edx, [eax-8000h]
004E0F24  add     edi, 0FFFF8000h
004E0F2A  mov     [esp+120h+var_90], edx
004E0F31  mov     [esp+120h+var_30], edi
004E0F38  dec     ecx
004E0F39  mov     dword ptr [esp+120h+var_D0], ecx
004E0F3D  test    edi, edi
004E0F3F  jle     short loc_4E0F5E
004E0F41  mov     ecx, [esp+120h+var_A8]
004E0F45  and     edi, 0FFFF0000h
004E0F4B  mov     eax, edi
004E0F4D  xor     ebx, ebx
004E0F4F  sar     eax, 10h
004E0F52  dec     ecx
004E0F53  cmp     eax, ecx
004E0F55  setl    bl
004E0F58  mov     [esp+120h+var_FC], ebx
004E0F5C  jmp     short loc_4E0F64
004E0F5E  xor     edi, edi
004E0F60  mov     [esp+120h+var_FC], edi
004E0F64  test    edx, edx
004E0F66  jle     short loc_4E0F90
004E0F68  mov     ecx, [esp+120h+var_4]
004E0F6F  and     edx, 0FFFF0000h
004E0F75  mov     eax, edx
004E0F77  sar     eax, 10h
004E0F7A  dec     ecx
004E0F7B  xor     ebx, ebx
004E0F7D  cmp     eax, ecx
004E0F7F  setnl   bl
004E0F82  dec     ebx
004E0F83  and     ebx, [esp+120h+var_A8]
004E0F87  mov     dword ptr [esp+120h+var_70], ebx
004E0F8E  jmp     short loc_4E0F99
004E0F90  xor     edx, edx
004E0F92  mov     dword ptr [esp+120h+var_70], edx
004E0F99  mov     ecx, edx
004E0F9B  sar     ecx, 10h
004E0F9E  imul    ecx, [esp+120h+var_A8]
004E0FA3  mov     eax, edi
004E0FA5  sar     eax, 10h
004E0FA8  add     ecx, eax
004E0FAA  mov     ebx, [esi+ecx*4]
004E0FAD  mov     eax, [esp+120h+var_FC]
004E0FB1  add     eax, ecx
004E0FB3  mov     eax, [esi+eax*4]
004E0FB6  mov     [esp+120h+var_104], eax
004E0FBA  mov     eax, dword ptr [esp+120h+var_70]
004E0FC1  add     ecx, eax
004E0FC3  mov     eax, [esi+ecx*4]
004E0FC6  mov     [esp+120h+var_110], eax
004E0FCA  mov     eax, [esp+120h+var_FC]
004E0FCE  add     ecx, eax
004E0FD0  mov     ecx, [esi+ecx*4]
004E0FD3  mov     eax, [esp+120h+var_90]
004E0FDA  sub     eax, edx
004E0FDC  mov     [esp+120h+var_FC], ecx
004E0FE0  mov     ecx, [esp+120h+var_30]
004E0FE7  sub     ecx, edi
004E0FE9  and     eax, 0FFFEh
004E0FEE  inc     eax
004E0FEF  and     ecx, 0FFFEh
004E0FF5  inc     ecx
004E0FF6  mov     dword ptr [esp+120h+var_70], ecx
004E0FFD  mov     edi, 10000h
004E1002  sub     edi, ecx
004E1004  mov     ecx, edi
004E1006  imul    edi, eax
004E1009  mov     edx, 10000h
004E100E  sub     edx, eax
004E1010  imul    eax, dword ptr [esp+120h+var_70]
004E1018  imul    ecx, edx
004E101B  imul    edx, dword ptr [esp+120h+var_70]
004E1023  shr     edx, 10h
004E1026  mov     [esp+120h+var_E0], ebx
004E102A  shr     ebx, 18h
004E102D  shr     ecx, 10h
004E1030  imul    ecx, ebx
004E1033  mov     ebx, [esp+120h+var_104]
004E1037  shr     ebx, 18h
004E103A  imul    edx, ebx
004E103D  mov     ebx, [esp+120h+var_110]
004E1041  shr     edx, 10h
004E1044  mov     [esp+120h+var_DC], edx
004E1048  shr     edi, 10h
004E104B  mov     edx, ebx
004E104D  shr     edx, 18h
004E1050  imul    edi, edx
004E1053  mov     edx, [esp+120h+var_FC]
004E1057  shr     edi, 10h
004E105A  shr     eax, 10h
004E105D  mov     [esp+120h+var_100], edi
004E1061  mov     edi, edx
004E1063  shr     edi, 18h
004E1066  imul    eax, edi
004E1069  shr     eax, 10h
004E106C  mov     edi, eax
004E106E  mov     eax, edx
004E1070  and     eax, 0FF00h
004E1075  imul    eax, edi
004E1078  mov     edx, ebx
004E107A  and     edx, 0FF00h
004E1080  imul    edx, [esp+120h+var_100]
004E1085  add     eax, edx
004E1087  mov     edx, [esp+120h+var_104]
004E108B  and     edx, 0FF00h
004E1091  imul    edx, [esp+120h+var_DC]
004E1096  add     eax, edx
004E1098  mov     edx, [esp+120h+var_E0]
004E109C  and     edx, 0FF00h
004E10A2  shr     ecx, 10h
004E10A5  imul    edx, ecx
004E10A8  add     eax, edx
004E10AA  mov     edx, [esp+120h+var_FC]
004E10AE  and     eax, 0FF0000h
004E10B3  mov     [esp+120h+var_C4], edi
004E10B7  and     edx, 0FF0000h
004E10BD  imul    edx, edi
004E10C0  mov     edi, [esp+120h+var_104]
004E10C4  and     edi, 0FF0000h
004E10CA  imul    edi, [esp+120h+var_DC]
004E10CF  and     ebx, 0FF0000h
004E10D5  imul    ebx, [esp+120h+var_100]
004E10DA  add     edx, ebx
004E10DC  add     edx, edi
004E10DE  mov     edi, [esp+120h+var_E0]
004E10E2  and     edi, 0FF0000h
004E10E8  imul    edi, ecx
004E10EB  add     edx, edi
004E10ED  movzx   edi, byte ptr [esp+120h+var_FC]
004E10F2  imul    edi, [esp+120h+var_C4]
004E10F7  movzx   ebx, byte ptr [esp+120h+var_104]
004E10FC  and     edx, 0FF0000FFh
004E1102  or      eax, edx
004E1104  movzx   edx, byte ptr [esp+120h+var_110]
004E1109  imul    edx, [esp+120h+var_100]
004E110E  add     edi, edx
004E1110  mov     edx, [esp+120h+var_DC]
004E1114  imul    ebx, edx
004E1117  add     edi, ebx
004E1119  movzx   ebx, byte ptr [esp+120h+var_E0]
004E111E  imul    ebx, ecx
004E1121  add     edi, ebx
004E1123  mov     ebx, [esp+120h+var_100]
004E1127  and     edi, 0FF00h
004E112D  or      eax, edi
004E112F  mov     edi, [esp+120h+var_C4]
004E1133  add     edi, ebx
004E1135  add     edi, edx
004E1137  add     edi, ecx
004E1139  shl     edi, 18h
004E113C  shr     eax, 8
004E113F  and     edi, 0FF000000h
004E1145  or      eax, edi
004E1147  mov     ecx, eax
004E1149  shr     ecx, 18h
004E114C  cmp     ecx, 8
004E114F  jbe     loc_4E1272
004E1155  mov     ebx, [ebp+arg_10]
004E1158  mov     edx, [ebx]
004E115A  imul    edx, [esp+120h+var_C0]
004E115F  shr     edx, 18h
004E1162  mov     edi, edx
004E1164  imul    edi, ecx
004E1167  mov     ecx, [ebx+4]
004E116A  imul    ecx, [esp+120h+var_7C]
004E1172  shr     edi, 8
004E1175  mov     [esp+120h+var_110], edi
004E1179  mov     edi, eax
004E117B  and     edi, 0FF0000h
004E1181  shr     ecx, 18h
004E1184  imul    ecx, edi
004E1187  mov     edi, [ebx+0Ch]
004E118A  imul    edi, [esp+120h+var_2C]
004E1192  shr     edi, 18h
004E1195  movzx   ebx, al
004E1198  imul    edi, ebx
004E119B  and     edi, 0FF00h
004E11A1  and     ecx, 0FF0000FFh
004E11A7  or      ecx, edi
004E11A9  mov     edi, [ebp+arg_10]
004E11AC  mov     edi, [edi+8]
004E11AF  imul    edi, [esp+120h+var_28]
004E11B7  and     eax, 0FF00h
004E11BC  shr     edi, 18h
004E11BF  imul    edi, eax
004E11C2  mov     ebx, [esp+120h+var_110]
004E11C6  and     edi, 0FF0000h
004E11CC  or      ecx, edi
004E11CE  shr     ecx, 8
004E11D1  mov     eax, ecx
004E11D3  and     eax, 0FF0000h
004E11D8  mov     edi, ecx
004E11DA  imul    eax, edx
004E11DD  and     edi, 0FF00h
004E11E3  movzx   ecx, cl
004E11E6  imul    edi, edx
004E11E9  imul    ecx, edx
004E11EC  mov     edx, [esp+120h+var_10C]
004E11F0  and     eax, 0FF0000FFh
004E11F5  and     edi, 0FF0000h
004E11FB  or      eax, edi
004E11FD  and     ecx, 0FF00h
004E1203  or      eax, ecx
004E1205  shr     eax, 8
004E1208  cmp     ebx, 0F0h
004E120E  jnb     short loc_4E126B
004E1210  mov     ecx, [edx]
004E1212  mov     edi, eax
004E1214  and     eax, 0FF00h
004E1219  mov     [esp+120h+var_110], eax
004E121D  mov     edx, 0FFh
004E1222  sub     edx, ebx
004E1224  mov     ebx, ecx
004E1226  and     ebx, 0FFFF00FFh
004E122C  mov     eax, ecx
004E122E  imul    ebx, edx
004E1231  and     eax, 0FF00h
004E1236  imul    eax, edx
004E1239  and     ecx, 0FF00FFh
004E123F  imul    ecx, edx
004E1242  xor     ebx, eax
004E1244  mov     eax, [esp+120h+var_110]
004E1248  and     ebx, 0FF0000h
004E124E  xor     ebx, ecx
004E1250  mov     ecx, [esp+120h+var_10C]
004E1254  and     edi, 0FF00FFh
004E125A  shr     ebx, 8
004E125D  or      eax, edi
004E125F  add     ebx, eax
004E1261  or      ebx, 0FF000000h
004E1267  mov     [ecx], ebx
004E1269  jmp     short loc_4E1272
004E126B  or      eax, 0FF000000h
004E1270  mov     [edx], eax
004E1272  mov     edx, [esp+120h+var_48]
004E1279  mov     eax, [esp+120h+var_34]
004E1280  add     [esp+120h+var_28], edx
004E1287  mov     ecx, [esp+120h+var_38]
004E128E  add     [esp+120h+var_C0], eax
004E1292  mov     edi, [esp+120h+var_30]
004E1299  mov     edx, [esp+120h+var_90]
004E12A0  add     [esp+120h+var_7C], ecx
004E12A7  mov     eax, [esp+120h+var_40]
004E12AE  add     edi, [esp+120h+var_44]
004E12B5  add     edx, [esp+120h+var_3C]
004E12BC  mov     ecx, dword ptr [esp+120h+var_D0]
004E12C0  add     [esp+120h+var_10C], 4
004E12C5  add     [esp+120h+var_2C], eax
004E12CC  mov     [esp+120h+var_30], edi
004E12D3  mov     [esp+120h+var_90], edx
004E12DA  test    ecx, ecx
004E12DC  jg      loc_4E0F38
004E12E2  mov     edi, [esp+120h+var_BC]
004E12E6  mov     eax, [esp+120h+var_C]
004E12ED  add     [esp+120h+var_D4], eax
004E12F1  mov     ecx, [esp+120h+var_108]
004E12F5  add     [esp+120h+var_EC], ecx
004E12F9  mov     edx, [esp+120h+var_8]
004E1300  mov     ecx, [esp+120h+var_10]
004E1307  add     [esp+120h+var_B0], edx
004E130B  add     [esp+120h+var_AC], ecx
004E130F  mov     edx, [esp+120h+var_1C]
004E1316  mov     ecx, [esp+120h+var_14]
004E131D  add     [esp+120h+var_B4], edx
004E1321  add     [esp+120h+var_9C], ecx
004E1328  add     edi, eax
004E132A  mov     eax, [esp+120h+var_88]
004E1331  add     eax, eax
004E1333  add     eax, eax
004E1335  add     [esp+120h+var_B8], eax
004E1339  mov     eax, [esp+120h+var_18]
004E1340  add     [esp+120h+var_A4], eax
004E1344  mov     eax, [esp+120h+var_20]
004E134B  add     [esp+120h+var_A0], eax
004E1352  cmp     [esp+120h+var_D8], 0
004E1357  mov     [esp+120h+var_BC], edi
004E135B  jg      loc_4E0DC0
004E1361  mov     eax, [esp+120h+var_84]
004E1368  sub     eax, [esp+120h+var_80]
004E136F  mov     [esp+120h+var_D8], eax
004E1373  jz      loc_4E2062
004E1379  mov     eax, [esp+120h+var_E8]
004E137D  mov     ecx, [eax+4]
004E1380  mov     ebx, [eax]
004E1382  mov     edx, [esp+120h+var_8C]
004E1389  mov     eax, [edx+4]
004E138C  sub     eax, ecx
004E138E  cdq
004E138F  push    edx
004E1390  push    eax
004E1391  push    10000h
004E1396  push    0
004E1398  mov     dword ptr [esp+130h+var_D0], ecx
004E139C  mov     [esp+130h+var_104], ebx
004E13A0  call    __alldiv
004E13A5  mov     ecx, eax
004E13A7  mov     eax, edx
004E13A9  mov     edx, [esp+120h+var_8C]
004E13B0  mov     [esp+120h+var_84], eax
004E13B7  mov     eax, [edx]
004E13B9  sub     eax, ebx
004E13BB  cdq
004E13BC  push    edx
004E13BD  push    eax
004E13BE  mov     eax, [esp+128h+var_84]
004E13C5  push    eax
004E13C6  push    ecx
004E13C7  call    __allmul
004E13CC  mov     cl, 20h ; ' '
004E13CE  call    __allshr
004E13D3  mov     ecx, eax
004E13D5  mov     eax, [esp+120h+var_80]
004E13DC  shl     eax, 10h
004E13DF  sub     eax, dword ptr [esp+120h+var_D0]
004E13E3  mov     [esp+120h+var_C0], ecx
004E13E7  imul    ecx
004E13E9  mov     cl, 10h
004E13EB  call    __allshr
004E13F0  mov     edx, [esp+120h+var_E4]
004E13F4  mov     ecx, eax
004E13F6  mov     eax, ebx
004E13F8  add     ecx, eax
004E13FA  cmp     edx, eax
004E13FC  jge     loc_4E1A2B
004E1402  cmp     [esp+120h+var_D8], 0
004E1407  jle     loc_4E2062
004E140D  mov     eax, [esp+120h+var_34]
004E1414  cdq
004E1415  mov     dword ptr [esp+120h+var_58], eax
004E141C  mov     eax, [esp+120h+var_38]
004E1423  mov     dword ptr [esp+120h+var_58+4], edx
004E142A  cdq
004E142B  mov     dword ptr [esp+120h+var_68], eax
004E1432  mov     eax, [esp+120h+var_48]
004E1439  mov     dword ptr [esp+120h+var_68+4], edx
004E1440  cdq
004E1441  mov     dword ptr [esp+120h+var_60], eax
004E1448  mov     eax, [esp+120h+var_40]
004E144F  mov     dword ptr [esp+120h+var_60+4], edx
004E1456  cdq
004E1457  mov     dword ptr [esp+120h+var_50], eax
004E145E  mov     eax, [esp+120h+var_44]
004E1465  mov     dword ptr [esp+120h+var_50+4], edx
004E146C  cdq
004E146D  mov     dword ptr [esp+120h+var_98], eax
004E1474  mov     eax, [esp+120h+var_3C]
004E147B  mov     dword ptr [esp+120h+var_98+4], edx
004E1482  cdq
004E1483  mov     dword ptr [esp+120h+var_F8], eax
004E1487  mov     eax, [esp+120h+var_88]
004E148E  mov     dword ptr [esp+120h+var_F8+4], edx
004E1492  lea     edx, ds:0[eax*4]
004E1499  add     ecx, 0FFFFh
004E149F  lea     eax, [edi+0FFFFh]
004E14A5  mov     [esp+120h+var_C4], edx
004E14A9  mov     [esp+120h+var_E0], ecx
004E14AD  mov     [esp+120h+var_DC], eax
004E14B1  mov     eax, [esp+120h+var_DC]
004E14B5  mov     ecx, dword ptr [esp+120h+var_58+4]
004E14BC  dec     [esp+120h+var_D8]
004E14C0  and     eax, 0FFFF0000h
004E14C5  mov     [esp+120h+var_84], eax
004E14CC  sub     eax, edi
004E14CE  cdq
004E14CF  push    edx
004E14D0  mov     ebx, eax
004E14D2  push    ebx
004E14D3  mov     dword ptr [esp+128h+var_78+4], edx
004E14DA  mov     edx, dword ptr [esp+128h+var_58]
004E14E1  push    ecx
004E14E2  push    edx
004E14E3  call    __allmul
004E14E8  mov     cl, 10h
004E14EA  call    __allshr
004E14EF  add     eax, [esp+120h+var_B0]
004E14F3  mov     ecx, dword ptr [esp+120h+var_68+4]
004E14FA  mov     edx, dword ptr [esp+120h+var_68]
004E1501  mov     [esp+120h+var_80], eax
004E1508  mov     eax, dword ptr [esp+120h+var_78+4]
004E150F  push    eax
004E1510  push    ebx
004E1511  push    ecx
004E1512  push    edx
004E1513  call    __allmul
004E1518  mov     cl, 10h
004E151A  call    __allshr
004E151F  add     eax, [esp+120h+var_A4]
004E1523  mov     ecx, dword ptr [esp+120h+var_60+4]
004E152A  mov     edx, dword ptr [esp+120h+var_60]
004E1531  mov     [esp+120h+var_8C], eax
004E1538  mov     eax, dword ptr [esp+120h+var_78+4]
004E153F  push    eax
004E1540  push    ebx
004E1541  push    ecx
004E1542  push    edx
004E1543  call    __allmul
004E1548  mov     cl, 10h
004E154A  call    __allshr
004E154F  add     eax, [esp+120h+var_AC]
004E1553  mov     ecx, dword ptr [esp+120h+var_50+4]
004E155A  mov     edx, dword ptr [esp+120h+var_50]
004E1561  mov     [esp+120h+var_EC], eax
004E1565  mov     eax, dword ptr [esp+120h+var_78+4]
004E156C  push    eax
004E156D  push    ebx
004E156E  push    ecx
004E156F  push    edx
004E1570  call    __allmul
004E1575  mov     cl, 10h
004E1577  call    __allshr
004E157C  add     eax, [esp+120h+var_B4]
004E1580  mov     ecx, dword ptr [esp+120h+var_98+4]
004E1587  mov     edx, dword ptr [esp+120h+var_98]
004E158E  mov     [esp+120h+var_D4], eax
004E1592  mov     eax, dword ptr [esp+120h+var_78+4]
004E1599  push    eax
004E159A  push    ebx
004E159B  push    ecx
004E159C  push    edx
004E159D  call    __allmul
004E15A2  mov     cl, 10h
004E15A4  call    __allshr
004E15A9  add     eax, [esp+120h+var_A0]
004E15B0  mov     ecx, dword ptr [esp+120h+var_F8+4]
004E15B4  mov     edx, dword ptr [esp+120h+var_F8]
004E15B8  mov     dword ptr [esp+120h+var_D0], eax
004E15BC  mov     eax, dword ptr [esp+120h+var_78+4]
004E15C3  push    eax
004E15C4  push    ebx
004E15C5  push    ecx
004E15C6  push    edx
004E15C7  call    __allmul
004E15CC  mov     cl, 10h
004E15CE  call    __allshr
004E15D3  mov     edx, [esp+120h+var_84]
004E15DA  add     eax, [esp+120h+var_9C]
004E15E1  mov     ecx, edx
004E15E3  sar     ecx, 10h
004E15E6  mov     ebx, [esp+120h+var_B8]
004E15EA  lea     ecx, [ebx+ecx*4]
004E15ED  mov     [esp+120h+var_10C], ecx
004E15F1  mov     ecx, [esp+120h+var_E0]
004E15F5  and     ecx, 0FFFF0000h
004E15FB  sub     ecx, edx
004E15FD  sar     ecx, 10h
004E1600  test    ecx, ecx
004E1602  jle     loc_4E19B0
004E1608  add     eax, 0FFFF8000h
004E160D  mov     [esp+120h+var_E8], eax
004E1611  mov     eax, dword ptr [esp+120h+var_D0]
004E1615  mov     edi, [esp+120h+var_E8]
004E1619  add     eax, 0FFFF8000h
004E161E  mov     [esp+120h+var_FC], eax
004E1622  mov     edx, [esp+120h+var_A8]
004E1626  dec     ecx
004E1627  mov     [esp+120h+var_7C], ecx
004E162E  test    eax, eax
004E1630  jle     short loc_4E1650
004E1632  and     eax, 0FFFF0000h
004E1637  mov     ecx, eax
004E1639  lea     edi, [edx-1]
004E163C  xor     ebx, ebx
004E163E  sar     ecx, 10h
004E1641  cmp     ecx, edi
004E1643  mov     edi, [esp+120h+var_E8]
004E1647  setl    bl
004E164A  mov     [esp+120h+var_E4], ebx
004E164E  jmp     short loc_4E1656
004E1650  xor     eax, eax
004E1652  mov     [esp+120h+var_E4], eax
004E1656  test    edi, edi
004E1658  jle     short loc_4E1681
004E165A  mov     ecx, [esp+120h+var_4]
004E1661  and     edi, 0FFFF0000h
004E1667  mov     edx, edi
004E1669  sar     edx, 10h
004E166C  dec     ecx
004E166D  xor     ebx, ebx
004E166F  cmp     edx, ecx
004E1671  mov     edx, [esp+120h+var_A8]
004E1675  setnl   bl
004E1678  dec     ebx
004E1679  and     ebx, edx
004E167B  mov     [esp+120h+var_108], ebx
004E167F  jmp     short loc_4E1687
004E1681  xor     edi, edi
004E1683  mov     [esp+120h+var_108], edi
004E1687  mov     ecx, edi
004E1689  sar     ecx, 10h
004E168C  imul    ecx, edx
004E168F  mov     edx, eax
004E1691  sar     edx, 10h
004E1694  add     ecx, edx
004E1696  mov     ebx, [esi+ecx*4]
004E1699  mov     edx, [esp+120h+var_E4]
004E169D  add     edx, ecx
004E169F  mov     edx, [esi+edx*4]
004E16A2  mov     [esp+120h+var_100], edx
004E16A6  mov     edx, [esp+120h+var_108]
004E16AA  add     ecx, edx
004E16AC  mov     edx, [esi+ecx*4]
004E16AF  mov     [esp+120h+var_104], edx
004E16B3  mov     edx, [esp+120h+var_E4]
004E16B7  add     edx, ecx
004E16B9  mov     ecx, [esi+edx*4]
004E16BC  mov     edx, [esp+120h+var_FC]
004E16C0  sub     edx, eax
004E16C2  mov     eax, [esp+120h+var_E8]
004E16C6  sub     eax, edi
004E16C8  and     eax, 0FFFEh
004E16CD  inc     eax
004E16CE  and     edx, 0FFFEh
004E16D4  inc     edx
004E16D5  mov     dword ptr [esp+120h+var_D0], edx
004E16D9  mov     edi, 10000h
004E16DE  sub     edi, eax
004E16E0  mov     esi, 10000h
004E16E5  sub     esi, edx
004E16E7  imul    edx, edi
004E16EA  mov     [esp+120h+var_84], esi
004E16F1  imul    esi, edi
004E16F4  mov     edi, [esp+120h+var_100]
004E16F8  shr     edx, 10h
004E16FB  shr     edi, 18h
004E16FE  imul    edx, edi
004E1701  mov     edi, eax
004E1703  imul    eax, dword ptr [esp+120h+var_D0]
004E1708  imul    edi, [esp+120h+var_84]
004E1710  mov     [esp+120h+var_110], ebx
004E1714  shr     ebx, 18h
004E1717  shr     esi, 10h
004E171A  imul    esi, ebx
004E171D  mov     ebx, [esp+120h+var_104]
004E1721  shr     ebx, 18h
004E1724  shr     eax, 10h
004E1727  shr     edi, 10h
004E172A  imul    edi, ebx
004E172D  mov     ebx, ecx
004E172F  shr     ebx, 18h
004E1732  imul    eax, ebx
004E1735  shr     eax, 10h
004E1738  mov     ebx, eax
004E173A  mov     [esp+120h+var_E4], ebx
004E173E  mov     eax, ecx
004E1740  and     eax, 0FF0000h
004E1745  imul    eax, ebx
004E1748  mov     ebx, [esp+120h+var_104]
004E174C  and     ebx, 0FF0000h
004E1752  shr     edi, 10h
004E1755  imul    ebx, edi
004E1758  add     eax, ebx
004E175A  mov     ebx, [esp+120h+var_100]
004E175E  shr     edx, 10h
004E1761  and     ebx, 0FF0000h
004E1767  imul    ebx, edx
004E176A  mov     [esp+120h+var_108], edx
004E176E  mov     edx, [esp+120h+var_110]
004E1772  and     edx, 0FF0000h
004E1778  shr     esi, 10h
004E177B  imul    edx, esi
004E177E  add     eax, ebx
004E1780  add     eax, edx
004E1782  movzx   edx, byte ptr [esp+120h+var_104]
004E1787  movzx   ebx, cl
004E178A  imul    ebx, [esp+120h+var_E4]
004E178F  and     eax, 0FF0000FFh
004E1794  imul    edx, edi
004E1797  mov     dword ptr [esp+120h+var_70], edi
004E179E  add     ebx, edx
004E17A0  movzx   edx, byte ptr [esp+120h+var_100]
004E17A5  imul    edx, [esp+120h+var_108]
004E17AA  add     ebx, edx
004E17AC  mov     edx, [esp+120h+var_110]
004E17B0  movzx   edi, dl
004E17B3  imul    edi, esi
004E17B6  and     ecx, 0FF00h
004E17BC  imul    ecx, [esp+120h+var_E4]
004E17C1  add     ebx, edi
004E17C3  mov     edi, [esp+120h+var_104]
004E17C7  and     edi, 0FF00h
004E17CD  imul    edi, dword ptr [esp+120h+var_70]
004E17D5  add     ecx, edi
004E17D7  mov     edi, [esp+120h+var_100]
004E17DB  and     ebx, 0FF00h
004E17E1  or      eax, ebx
004E17E3  mov     ebx, [esp+120h+var_108]
004E17E7  and     edi, 0FF00h
004E17ED  and     edx, 0FF00h
004E17F3  imul    edi, ebx
004E17F6  imul    edx, esi
004E17F9  add     ecx, edi
004E17FB  add     ecx, edx
004E17FD  mov     edx, dword ptr [esp+120h+var_70]
004E1804  and     ecx, 0FF0000h
004E180A  or      eax, ecx
004E180C  mov     ecx, [esp+120h+var_E4]
004E1810  add     ecx, edx
004E1812  add     ecx, ebx
004E1814  add     ecx, esi
004E1816  shl     ecx, 18h
004E1819  and     ecx, 0FF000000h
004E181F  shr     eax, 8
004E1822  or      eax, ecx
004E1824  mov     ecx, eax
004E1826  shr     ecx, 18h
004E1829  cmp     ecx, 8
004E182C  jbe     loc_4E1941
004E1832  mov     edi, [ebp+arg_10]
004E1835  mov     edx, [edi]
004E1837  imul    edx, [esp+120h+var_80]
004E183F  shr     edx, 18h
004E1842  mov     esi, edx
004E1844  imul    esi, ecx
004E1847  mov     ecx, [edi+4]
004E184A  mov     edi, [edi+0Ch]
004E184D  imul    ecx, [esp+120h+var_8C]
004E1855  imul    edi, [esp+120h+var_D4]
004E185A  mov     ebx, eax
004E185C  shr     ecx, 18h
004E185F  shr     edi, 18h
004E1862  and     ebx, 0FF0000h
004E1868  imul    ecx, ebx
004E186B  movzx   ebx, al
004E186E  imul    edi, ebx
004E1871  and     edi, 0FF00h
004E1877  and     ecx, 0FF0000FFh
004E187D  or      ecx, edi
004E187F  mov     edi, [ebp+arg_10]
004E1882  mov     edi, [edi+8]
004E1885  imul    edi, [esp+120h+var_EC]
004E188A  and     eax, 0FF00h
004E188F  shr     edi, 18h
004E1892  imul    edi, eax
004E1895  and     edi, 0FF0000h
004E189B  or      ecx, edi
004E189D  shr     ecx, 8
004E18A0  mov     eax, ecx
004E18A2  and     eax, 0FF0000h
004E18A7  mov     edi, ecx
004E18A9  imul    eax, edx
004E18AC  and     edi, 0FF00h
004E18B2  movzx   ecx, cl
004E18B5  imul    edi, edx
004E18B8  imul    ecx, edx
004E18BB  and     eax, 0FF0000FFh
004E18C0  and     edi, 0FF0000h
004E18C6  or      eax, edi
004E18C8  and     ecx, 0FF00h
004E18CE  or      eax, ecx
004E18D0  shr     esi, 8
004E18D3  shr     eax, 8
004E18D6  cmp     esi, 0F0h
004E18DC  jnb     short loc_4E1936
004E18DE  mov     edx, [esp+120h+var_10C]
004E18E2  mov     ecx, [edx]
004E18E4  mov     edx, 0FFh
004E18E9  sub     edx, esi
004E18EB  mov     esi, ecx
004E18ED  and     esi, 0FFFF00FFh
004E18F3  mov     ebx, ecx
004E18F5  imul    esi, edx
004E18F8  and     ebx, 0FF00h
004E18FE  imul    ebx, edx
004E1901  and     ecx, 0FF00FFh
004E1907  imul    ecx, edx
004E190A  xor     esi, ebx
004E190C  mov     edi, eax
004E190E  and     esi, 0FF0000h
004E1914  and     eax, 0FF00h
004E1919  xor     esi, ecx
004E191B  and     edi, 0FF00FFh
004E1921  or      eax, edi
004E1923  shr     esi, 8
004E1926  add     esi, eax
004E1928  mov     eax, [esp+120h+var_10C]
004E192C  or      esi, 0FF000000h
004E1932  mov     [eax], esi
004E1934  jmp     short loc_4E1941
004E1936  mov     ecx, [esp+120h+var_10C]
004E193A  or      eax, 0FF000000h
004E193F  mov     [ecx], eax
004E1941  mov     eax, [esp+120h+var_38]
004E1948  mov     edx, [esp+120h+var_34]
004E194F  add     [esp+120h+var_8C], eax
004E1956  mov     ecx, [esp+120h+var_48]
004E195D  add     [esp+120h+var_80], edx
004E1964  mov     eax, [esp+120h+var_FC]
004E1968  mov     edi, [esp+120h+var_E8]
004E196C  add     [esp+120h+var_EC], ecx
004E1970  mov     edx, [esp+120h+var_40]
004E1977  add     eax, [esp+120h+var_44]
004E197E  add     edi, [esp+120h+var_3C]
004E1985  mov     ecx, [esp+120h+var_7C]
004E198C  add     [esp+120h+var_10C], 4
004E1991  add     [esp+120h+var_D4], edx
004E1995  mov     esi, [esp+120h+var_24]
004E199C  mov     [esp+120h+var_FC], eax
004E19A0  mov     [esp+120h+var_E8], edi
004E19A4  test    ecx, ecx
004E19A6  jg      loc_4E1622
004E19AC  mov     edi, [esp+120h+var_BC]
004E19B0  mov     eax, [esp+120h+var_C]
004E19B7  add     [esp+120h+var_DC], eax
004E19BB  mov     ecx, [esp+120h+var_C4]
004E19BF  add     [esp+120h+var_B8], ecx
004E19C3  mov     edx, [esp+120h+var_8]
004E19CA  mov     ecx, [esp+120h+var_10]
004E19D1  add     [esp+120h+var_B0], edx
004E19D5  add     [esp+120h+var_AC], ecx
004E19D9  mov     edx, [esp+120h+var_1C]
004E19E0  mov     ecx, [esp+120h+var_14]
004E19E7  add     [esp+120h+var_B4], edx
004E19EB  add     [esp+120h+var_9C], ecx
004E19F2  add     edi, eax
004E19F4  mov     eax, [esp+120h+var_C0]
004E19F8  add     [esp+120h+var_E0], eax
004E19FC  mov     eax, [esp+120h+var_18]
004E1A03  add     [esp+120h+var_A4], eax
004E1A07  mov     eax, [esp+120h+var_20]
004E1A0E  add     [esp+120h+var_A0], eax
004E1A15  cmp     [esp+120h+var_D8], 0
004E1A1A  mov     [esp+120h+var_BC], edi
004E1A1E  jg      loc_4E14B1
004E1A24  pop     edi
004E1A25  pop     esi
004E1A26  pop     ebx
004E1A27  mov     esp, ebp
004E1A29  pop     ebp
004E1A2A  retn
004E1A2B  jle     loc_4E2062
004E1A31  cmp     [esp+120h+var_D8], 0
004E1A36  jle     loc_4E2062
004E1A3C  mov     eax, [esp+120h+var_34]
004E1A43  cdq
004E1A44  mov     dword ptr [esp+120h+var_58], eax
004E1A4B  mov     eax, [esp+120h+var_38]
004E1A52  mov     dword ptr [esp+120h+var_58+4], edx
004E1A59  cdq
004E1A5A  mov     dword ptr [esp+120h+var_68], eax
004E1A61  mov     eax, [esp+120h+var_48]
004E1A68  mov     dword ptr [esp+120h+var_68+4], edx
004E1A6F  cdq
004E1A70  mov     dword ptr [esp+120h+var_60], eax
004E1A77  mov     eax, [esp+120h+var_40]
004E1A7E  mov     dword ptr [esp+120h+var_60+4], edx
004E1A85  cdq
004E1A86  mov     dword ptr [esp+120h+var_50], eax
004E1A8D  mov     eax, [esp+120h+var_44]
004E1A94  mov     dword ptr [esp+120h+var_50+4], edx
004E1A9B  cdq
004E1A9C  mov     dword ptr [esp+120h+var_98], eax
004E1AA3  mov     eax, [esp+120h+var_3C]
004E1AAA  mov     dword ptr [esp+120h+var_98+4], edx
004E1AB1  cdq
004E1AB2  mov     dword ptr [esp+120h+var_F8+4], edx
004E1AB6  mov     edx, [esp+120h+var_88]
004E1ABD  mov     dword ptr [esp+120h+var_F8], eax
004E1AC1  lea     eax, ds:0[edx*4]
004E1AC8  lea     edx, [edi+0FFFFh]
004E1ACE  add     ecx, 0FFFFh
004E1AD4  mov     [esp+120h+var_C4], eax
004E1AD8  mov     [esp+120h+var_DC], edx
004E1ADC  mov     [esp+120h+var_E0], ecx
004E1AE0  mov     eax, [esp+120h+var_E0]
004E1AE4  mov     ecx, dword ptr [esp+120h+var_58]
004E1AEB  dec     [esp+120h+var_D8]
004E1AEF  and     eax, 0FFFF0000h
004E1AF4  mov     [esp+120h+var_84], eax
004E1AFB  sub     eax, edi
004E1AFD  cdq
004E1AFE  push    edx
004E1AFF  mov     ebx, eax
004E1B01  mov     eax, dword ptr [esp+124h+var_58+4]
004E1B08  push    ebx
004E1B09  push    eax
004E1B0A  push    ecx
004E1B0B  mov     dword ptr [esp+130h+var_78+4], edx
004E1B12  call    __allmul
004E1B17  mov     cl, 10h
004E1B19  call    __allshr
004E1B1E  add     eax, [esp+120h+var_B0]
004E1B22  mov     edx, dword ptr [esp+120h+var_78+4]
004E1B29  mov     ecx, dword ptr [esp+120h+var_68]
004E1B30  push    edx
004E1B31  mov     [esp+124h+var_80], eax
004E1B38  mov     eax, dword ptr [esp+124h+var_68+4]
004E1B3F  push    ebx
004E1B40  push    eax
004E1B41  push    ecx
004E1B42  call    __allmul
004E1B47  mov     cl, 10h
004E1B49  call    __allshr
004E1B4E  add     eax, [esp+120h+var_A4]
004E1B52  mov     edx, dword ptr [esp+120h+var_78+4]
004E1B59  mov     ecx, dword ptr [esp+120h+var_60]
004E1B60  push    edx
004E1B61  mov     [esp+124h+var_8C], eax
004E1B68  mov     eax, dword ptr [esp+124h+var_60+4]
004E1B6F  push    ebx
004E1B70  push    eax
004E1B71  push    ecx
004E1B72  call    __allmul
004E1B77  mov     cl, 10h
004E1B79  call    __allshr
004E1B7E  add     eax, [esp+120h+var_AC]
004E1B82  mov     edx, dword ptr [esp+120h+var_78+4]
004E1B89  mov     ecx, dword ptr [esp+120h+var_50]
004E1B90  push    edx
004E1B91  mov     [esp+124h+var_EC], eax
004E1B95  mov     eax, dword ptr [esp+124h+var_50+4]
004E1B9C  push    ebx
004E1B9D  push    eax
004E1B9E  push    ecx
004E1B9F  call    __allmul
004E1BA4  mov     cl, 10h
004E1BA6  call    __allshr
004E1BAB  add     eax, [esp+120h+var_B4]
004E1BAF  mov     edx, dword ptr [esp+120h+var_78+4]
004E1BB6  mov     ecx, dword ptr [esp+120h+var_98]
004E1BBD  push    edx
004E1BBE  mov     [esp+124h+var_D4], eax
004E1BC2  mov     eax, dword ptr [esp+124h+var_98+4]
004E1BC9  push    ebx
004E1BCA  push    eax
004E1BCB  push    ecx
004E1BCC  call    __allmul
004E1BD1  mov     cl, 10h
004E1BD3  call    __allshr
004E1BD8  add     eax, [esp+120h+var_A0]
004E1BDF  mov     edx, dword ptr [esp+120h+var_78+4]
004E1BE6  mov     ecx, dword ptr [esp+120h+var_F8]
004E1BEA  push    edx
004E1BEB  mov     dword ptr [esp+124h+var_D0], eax
004E1BEF  mov     eax, dword ptr [esp+124h+var_F8+4]
004E1BF3  push    ebx
004E1BF4  push    eax
004E1BF5  push    ecx
004E1BF6  call    __allmul
004E1BFB  mov     cl, 10h
004E1BFD  call    __allshr
004E1C02  mov     edx, [esp+120h+var_84]
004E1C09  add     eax, [esp+120h+var_9C]
004E1C10  mov     ecx, edx
004E1C12  sar     ecx, 10h
004E1C15  mov     ebx, [esp+120h+var_B8]
004E1C19  lea     ecx, [ebx+ecx*4]
004E1C1C  mov     [esp+120h+var_88], ecx
004E1C23  mov     ecx, [esp+120h+var_DC]
004E1C27  and     ecx, 0FFFF0000h
004E1C2D  sub     ecx, edx
004E1C2F  sar     ecx, 10h
004E1C32  test    ecx, ecx
004E1C34  jle     loc_4E1FEE
004E1C3A  add     eax, 0FFFF8000h
004E1C3F  mov     [esp+120h+var_10C], eax
004E1C43  mov     eax, dword ptr [esp+120h+var_D0]
004E1C47  mov     edi, [esp+120h+var_10C]
004E1C4B  add     eax, 0FFFF8000h
004E1C50  mov     [esp+120h+var_FC], eax
004E1C54  mov     edx, [esp+120h+var_A8]
004E1C58  dec     ecx
004E1C59  mov     [esp+120h+var_7C], ecx
004E1C60  test    eax, eax
004E1C62  jle     short loc_4E1C82
004E1C64  and     eax, 0FFFF0000h
004E1C69  mov     ecx, eax
004E1C6B  lea     edi, [edx-1]
004E1C6E  xor     ebx, ebx
004E1C70  sar     ecx, 10h
004E1C73  cmp     ecx, edi
004E1C75  mov     edi, [esp+120h+var_10C]
004E1C79  setl    bl
004E1C7C  mov     [esp+120h+var_E4], ebx
004E1C80  jmp     short loc_4E1C88
004E1C82  xor     eax, eax
004E1C84  mov     [esp+120h+var_E4], eax
004E1C88  test    edi, edi
004E1C8A  jle     short loc_4E1CB3
004E1C8C  mov     ecx, [esp+120h+var_4]
004E1C93  and     edi, 0FFFF0000h
004E1C99  mov     edx, edi
004E1C9B  sar     edx, 10h
004E1C9E  dec     ecx
004E1C9F  xor     ebx, ebx
004E1CA1  cmp     edx, ecx
004E1CA3  mov     edx, [esp+120h+var_A8]
004E1CA7  setnl   bl
004E1CAA  dec     ebx
004E1CAB  and     ebx, edx
004E1CAD  mov     [esp+120h+var_108], ebx
004E1CB1  jmp     short loc_4E1CB9
004E1CB3  xor     edi, edi
004E1CB5  mov     [esp+120h+var_108], edi
004E1CB9  mov     ecx, edi
004E1CBB  sar     ecx, 10h
004E1CBE  imul    ecx, edx
004E1CC1  mov     edx, eax
004E1CC3  sar     edx, 10h
004E1CC6  add     ecx, edx
004E1CC8  mov     ebx, [esi+ecx*4]
004E1CCB  mov     edx, [esp+120h+var_E4]
004E1CCF  add     edx, ecx
004E1CD1  mov     edx, [esi+edx*4]
004E1CD4  mov     [esp+120h+var_100], edx
004E1CD8  mov     edx, [esp+120h+var_108]
004E1CDC  add     ecx, edx
004E1CDE  mov     edx, [esi+ecx*4]
004E1CE1  mov     [esp+120h+var_104], edx
004E1CE5  mov     edx, [esp+120h+var_E4]
004E1CE9  add     edx, ecx
004E1CEB  mov     ecx, [esi+edx*4]
004E1CEE  mov     edx, [esp+120h+var_FC]
004E1CF2  sub     edx, eax
004E1CF4  mov     eax, [esp+120h+var_10C]
004E1CF8  sub     eax, edi
004E1CFA  and     eax, 0FFFEh
004E1CFF  inc     eax
004E1D00  and     edx, 0FFFEh
004E1D06  inc     edx
004E1D07  mov     dword ptr [esp+120h+var_D0], edx
004E1D0B  mov     edi, 10000h
004E1D10  sub     edi, eax
004E1D12  mov     esi, 10000h
004E1D17  sub     esi, edx
004E1D19  imul    edx, edi
004E1D1C  mov     [esp+120h+var_84], esi
004E1D23  imul    esi, edi
004E1D26  mov     edi, [esp+120h+var_100]
004E1D2A  shr     edx, 10h
004E1D2D  shr     edi, 18h
004E1D30  imul    edx, edi
004E1D33  mov     edi, eax
004E1D35  imul    eax, dword ptr [esp+120h+var_D0]
004E1D3A  imul    edi, [esp+120h+var_84]
004E1D42  mov     [esp+120h+var_110], ebx
004E1D46  shr     ebx, 18h
004E1D49  shr     esi, 10h
004E1D4C  imul    esi, ebx
004E1D4F  mov     ebx, [esp+120h+var_104]
004E1D53  shr     ebx, 18h
004E1D56  shr     eax, 10h
004E1D59  shr     edi, 10h
004E1D5C  imul    edi, ebx
004E1D5F  mov     ebx, ecx
004E1D61  shr     ebx, 18h
004E1D64  imul    eax, ebx
004E1D67  shr     eax, 10h
004E1D6A  mov     ebx, eax
004E1D6C  mov     [esp+120h+var_E4], ebx
004E1D70  mov     eax, ecx
004E1D72  and     eax, 0FF0000h
004E1D77  imul    eax, ebx
004E1D7A  mov     ebx, [esp+120h+var_104]
004E1D7E  and     ebx, 0FF0000h
004E1D84  shr     edi, 10h
004E1D87  imul    ebx, edi
004E1D8A  add     eax, ebx
004E1D8C  mov     ebx, [esp+120h+var_100]
004E1D90  shr     edx, 10h
004E1D93  and     ebx, 0FF0000h
004E1D99  imul    ebx, edx
004E1D9C  mov     [esp+120h+var_108], edx
004E1DA0  mov     edx, [esp+120h+var_110]
004E1DA4  and     edx, 0FF0000h
004E1DAA  shr     esi, 10h
004E1DAD  imul    edx, esi
004E1DB0  add     eax, ebx
004E1DB2  add     eax, edx
004E1DB4  movzx   edx, byte ptr [esp+120h+var_104]
004E1DB9  movzx   ebx, cl
004E1DBC  imul    ebx, [esp+120h+var_E4]
004E1DC1  and     eax, 0FF0000FFh
004E1DC6  imul    edx, edi
004E1DC9  mov     dword ptr [esp+120h+var_70], edi
004E1DD0  add     ebx, edx
004E1DD2  movzx   edx, byte ptr [esp+120h+var_100]
004E1DD7  imul    edx, [esp+120h+var_108]
004E1DDC  add     ebx, edx
004E1DDE  mov     edx, [esp+120h+var_110]
004E1DE2  movzx   edi, dl
004E1DE5  imul    edi, esi
004E1DE8  and     ecx, 0FF00h
004E1DEE  imul    ecx, [esp+120h+var_E4]
004E1DF3  add     ebx, edi
004E1DF5  mov     edi, [esp+120h+var_104]
004E1DF9  and     edi, 0FF00h
004E1DFF  imul    edi, dword ptr [esp+120h+var_70]
004E1E07  add     ecx, edi
004E1E09  mov     edi, [esp+120h+var_100]
004E1E0D  and     ebx, 0FF00h
004E1E13  or      eax, ebx
004E1E15  mov     ebx, [esp+120h+var_108]
004E1E19  and     edi, 0FF00h
004E1E1F  and     edx, 0FF00h
004E1E25  imul    edi, ebx
004E1E28  imul    edx, esi
004E1E2B  add     ecx, edi
004E1E2D  add     ecx, edx
004E1E2F  mov     edx, dword ptr [esp+120h+var_70]
004E1E36  and     ecx, 0FF0000h
004E1E3C  or      eax, ecx
004E1E3E  mov     ecx, [esp+120h+var_E4]
004E1E42  add     ecx, edx
004E1E44  add     ecx, ebx
004E1E46  add     ecx, esi
004E1E48  shl     ecx, 18h
004E1E4B  and     ecx, 0FF000000h
004E1E51  shr     eax, 8
004E1E54  or      eax, ecx
004E1E56  mov     ecx, eax
004E1E58  shr     ecx, 18h
004E1E5B  cmp     ecx, 8
004E1E5E  jbe     loc_4E1F7C
004E1E64  mov     edi, [ebp+arg_10]
004E1E67  mov     edx, [edi]
004E1E69  imul    edx, [esp+120h+var_80]
004E1E71  shr     edx, 18h
004E1E74  mov     esi, edx
004E1E76  imul    esi, ecx
004E1E79  mov     ecx, [edi+4]
004E1E7C  mov     edi, [edi+0Ch]
004E1E7F  imul    ecx, [esp+120h+var_8C]
004E1E87  imul    edi, [esp+120h+var_D4]
004E1E8C  mov     ebx, eax
004E1E8E  shr     ecx, 18h
004E1E91  shr     edi, 18h
004E1E94  and     ebx, 0FF0000h
004E1E9A  imul    ecx, ebx
004E1E9D  movzx   ebx, al
004E1EA0  imul    edi, ebx
004E1EA3  and     edi, 0FF00h
004E1EA9  and     ecx, 0FF0000FFh
004E1EAF  or      ecx, edi
004E1EB1  mov     edi, [ebp+arg_10]
004E1EB4  mov     edi, [edi+8]
004E1EB7  imul    edi, [esp+120h+var_EC]
004E1EBC  and     eax, 0FF00h
004E1EC1  shr     edi, 18h
004E1EC4  imul    edi, eax
004E1EC7  and     edi, 0FF0000h
004E1ECD  or      ecx, edi
004E1ECF  shr     ecx, 8
004E1ED2  mov     eax, ecx
004E1ED4  and     eax, 0FF0000h
004E1ED9  mov     edi, ecx
004E1EDB  imul    eax, edx
004E1EDE  and     edi, 0FF00h
004E1EE4  movzx   ecx, cl
004E1EE7  imul    edi, edx
004E1EEA  imul    ecx, edx
004E1EED  and     eax, 0FF0000FFh
004E1EF2  and     edi, 0FF0000h
004E1EF8  or      eax, edi
004E1EFA  and     ecx, 0FF00h
004E1F00  or      eax, ecx
004E1F02  shr     esi, 8
004E1F05  shr     eax, 8
004E1F08  cmp     esi, 0F0h
004E1F0E  jnb     short loc_4E1F6E
004E1F10  mov     edx, [esp+120h+var_88]
004E1F17  mov     ecx, [edx]
004E1F19  mov     edx, 0FFh
004E1F1E  sub     edx, esi
004E1F20  mov     esi, ecx
004E1F22  and     esi, 0FFFF00FFh
004E1F28  mov     ebx, ecx
004E1F2A  imul    esi, edx
004E1F2D  and     ebx, 0FF00h
004E1F33  imul    ebx, edx
004E1F36  and     ecx, 0FF00FFh
004E1F3C  imul    ecx, edx
004E1F3F  xor     esi, ebx
004E1F41  mov     edi, eax
004E1F43  and     esi, 0FF0000h
004E1F49  and     eax, 0FF00h
004E1F4E  xor     esi, ecx
004E1F50  and     edi, 0FF00FFh
004E1F56  or      eax, edi
004E1F58  shr     esi, 8
004E1F5B  add     esi, eax
004E1F5D  mov     eax, [esp+120h+var_88]
004E1F64  or      esi, 0FF000000h
004E1F6A  mov     [eax], esi
004E1F6C  jmp     short loc_4E1F7C
004E1F6E  mov     ecx, [esp+120h+var_88]
004E1F75  or      eax, 0FF000000h
004E1F7A  mov     [ecx], eax
004E1F7C  mov     eax, [esp+120h+var_38]
004E1F83  mov     edx, [esp+120h+var_34]
004E1F8A  add     [esp+120h+var_8C], eax
004E1F91  mov     ecx, [esp+120h+var_48]
004E1F98  add     [esp+120h+var_80], edx
004E1F9F  mov     eax, [esp+120h+var_FC]
004E1FA3  mov     edi, [esp+120h+var_10C]
004E1FA7  add     [esp+120h+var_EC], ecx
004E1FAB  mov     edx, [esp+120h+var_40]
004E1FB2  add     eax, [esp+120h+var_44]
004E1FB9  add     edi, [esp+120h+var_3C]
004E1FC0  mov     ecx, [esp+120h+var_7C]
004E1FC7  add     [esp+120h+var_88], 4
004E1FCF  add     [esp+120h+var_D4], edx
004E1FD3  mov     esi, [esp+120h+var_24]
004E1FDA  mov     [esp+120h+var_FC], eax
004E1FDE  mov     [esp+120h+var_10C], edi
004E1FE2  test    ecx, ecx
004E1FE4  jg      loc_4E1C54
004E1FEA  mov     edi, [esp+120h+var_BC]
004E1FEE  mov     eax, [esp+120h+var_C]
004E1FF5  add     [esp+120h+var_DC], eax
004E1FF9  mov     ecx, [esp+120h+var_C4]
004E1FFD  add     [esp+120h+var_B8], ecx
004E2001  mov     edx, [esp+120h+var_8]
004E2008  mov     ecx, [esp+120h+var_10]
004E200F  add     [esp+120h+var_B0], edx
004E2013  add     [esp+120h+var_AC], ecx
004E2017  mov     edx, [esp+120h+var_1C]
004E201E  mov     ecx, [esp+120h+var_14]
004E2025  add     [esp+120h+var_B4], edx
004E2029  add     [esp+120h+var_9C], ecx
004E2030  add     edi, eax
004E2032  mov     eax, [esp+120h+var_C0]
004E2036  add     [esp+120h+var_E0], eax
004E203A  mov     eax, [esp+120h+var_18]
004E2041  add     [esp+120h+var_A4], eax
004E2045  mov     eax, [esp+120h+var_20]
004E204C  add     [esp+120h+var_A0], eax
004E2053  cmp     [esp+120h+var_D8], 0
004E2058  mov     [esp+120h+var_BC], edi
004E205C  jg      loc_4E1AE0
004E2062  pop     edi
004E2063  pop     esi
004E2064  pop     ebx
004E2065  mov     esp, ebp
004E2067  pop     ebp
004E2068  retn
