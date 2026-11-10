00500050  sub     esp, 0B0h
00500056  push    ebx
00500057  mov     ebx, [ecx+10h]
0050005A  shr     edx, 1
0050005C  push    ebp
0050005D  push    esi
0050005E  mov     [esp+0BCh+var_1C], edx
00500065  mov     edx, [ecx+18h]
00500068  push    edi
00500069  mov     edi, [ecx]
0050006B  mov     [esp+0C0h+var_4], edx
00500072  mov     edx, [eax+4]
00500075  cmp     edx, [eax+24h]
00500078  lea     ecx, [eax+20h]
0050007B  lea     esi, [eax+40h]
0050007E  mov     [esp+0C0h+var_7C], ebx
00500082  mov     ebp, eax
00500084  mov     [esp+0C0h+var_54], ecx
00500088  mov     [esp+0C0h+var_20], esi
0050008F  jle     short loc_500099
00500091  mov     ebp, ecx
00500093  mov     [esp+0C0h+var_54], eax
00500097  mov     ecx, eax
00500099  mov     eax, [ecx+4]
0050009C  cmp     eax, [esi+4]
0050009F  jle     short loc_5000B5
005000A1  mov     [esp+0C0h+var_20], ecx
005000A8  mov     [esp+0C0h+var_54], esi
005000AC  mov     ecx, esi
005000AE  mov     esi, [esp+0C0h+var_20]
005000B5  mov     edx, [ebp+4]
005000B8  cmp     edx, [ecx+4]
005000BB  jle     short loc_5000C7
005000BD  mov     eax, ebp
005000BF  mov     ebp, ecx
005000C1  mov     [esp+0C0h+var_54], eax
005000C5  mov     ecx, eax
005000C7  mov     eax, [ebp+4]
005000CA  lea     edx, [eax+0FFFFh]
005000D0  mov     eax, [esi+4]
005000D3  lea     esi, [eax+0FFFFh]
005000D9  sar     edx, 10h
005000DC  sar     esi, 10h
005000DF  mov     [esp+0C0h+var_A0], edx
005000E3  mov     [esp+0C0h+var_8C], esi
005000E7  cmp     edx, esi
005000E9  jz      loc_5016FB
005000EF  mov     ecx, [ecx+4]
005000F2  mov     [esp+0C0h+var_A8], ecx
005000F6  add     ecx, 0FFFFh
005000FC  sar     ecx, 10h
005000FF  mov     [esp+0C0h+var_58], ecx
00500103  mov     ecx, [ebp+4]
00500106  sub     eax, ecx
00500108  cdq
00500109  push    edx
0050010A  push    eax
0050010B  push    10000h
00500110  push    0
00500112  call    __alldiv
00500117  mov     esi, eax
00500119  mov     eax, [esp+0C0h+var_20]
00500120  mov     eax, [eax]
00500122  mov     ecx, edx
00500124  mov     edx, [ebp+0]
00500127  push    ecx
00500128  sub     eax, edx
0050012A  mov     [esp+0C4h+var_A4], edx
0050012E  cdq
0050012F  push    esi
00500130  push    edx
00500131  push    eax
00500132  mov     dword ptr [esp+0D0h+var_30+4], ecx
00500139  call    __allmul
0050013E  mov     cl, 20h ; ' '
00500140  call    __allshr
00500145  mov     ecx, [esp+0C0h+var_20]
0050014C  mov     [esp+0C0h+var_14], eax
00500153  mov     eax, [ecx+18h]
00500156  sub     eax, [ebp+18h]
00500159  mov     ecx, dword ptr [esp+0C0h+var_30+4]
00500160  push    ecx
00500161  cdq
00500162  push    esi
00500163  push    edx
00500164  push    eax
00500165  call    __allmul
0050016A  mov     cl, 20h ; ' '
0050016C  call    __allshr
00500171  mov     edx, [esp+0C0h+var_20]
00500178  mov     ecx, dword ptr [esp+0C0h+var_30+4]
0050017F  mov     [esp+0C0h+var_8], eax
00500186  mov     eax, [edx+1Ch]
00500189  sub     eax, [ebp+1Ch]
0050018C  push    ecx
0050018D  cdq
0050018E  push    esi
0050018F  push    edx
00500190  push    eax
00500191  call    __allmul
00500196  mov     cl, 20h ; ' '
00500198  call    __allshr
0050019D  mov     [esp+0C0h+var_18], eax
005001A4  mov     eax, [esp+0C0h+var_A8]
005001A8  sub     eax, [ebp+4]
005001AB  cdq
005001AC  mov     ecx, eax
005001AE  mov     eax, [esp+0C0h+var_14]
005001B5  mov     dword ptr [esp+0C0h+var_68+4], edx
005001B9  cdq
005001BA  mov     esi, eax
005001BC  mov     eax, dword ptr [esp+0C0h+var_68+4]
005001C0  push    eax
005001C1  push    ecx
005001C2  push    edx
005001C3  push    esi
005001C4  mov     dword ptr [esp+0D0h+var_68], ecx
005001C8  mov     dword ptr [esp+0D0h+var_30+4], edx
005001CF  call    __allmul
005001D4  mov     cl, 10h
005001D6  call    __allshr
005001DB  add     eax, [esp+0C0h+var_A4]
005001DF  mov     ecx, [esp+0C0h+var_54]
005001E3  mov     ecx, [ecx]
005001E5  mov     [esp+0C0h+var_5C], eax
005001E9  mov     [esp+0C0h+var_B0], ecx
005001ED  cmp     ecx, eax
005001EF  jz      loc_5016FB
005001F5  mov     eax, [esp+0C0h+var_A0]
005001F9  shl     eax, 10h
005001FC  sub     eax, [ebp+4]
005001FF  cdq
00500200  push    edx
00500201  mov     dword ptr [esp+0C4h+var_50+4], edx
00500205  mov     edx, dword ptr [esp+0C4h+var_30+4]
0050020C  push    eax
0050020D  push    edx
0050020E  push    esi
0050020F  mov     dword ptr [esp+0D0h+var_50], eax
00500216  call    __allmul
0050021B  mov     cl, 10h
0050021D  call    __allshr
00500222  mov     ecx, [ebp+18h]
00500225  mov     [esp+0C0h+var_9C], ecx
00500229  mov     ecx, dword ptr [esp+0C0h+var_50+4]
0050022D  mov     esi, eax
0050022F  mov     eax, [esp+0C0h+var_8]
00500236  add     esi, [esp+0C0h+var_A4]
0050023A  push    ecx
0050023B  mov     ecx, dword ptr [esp+0C4h+var_50]
0050023F  cdq
00500240  push    ecx
00500241  push    edx
00500242  push    eax
00500243  mov     [esp+0D0h+var_44], esi
0050024A  mov     dword ptr [esp+0D0h+var_10], eax
00500251  mov     dword ptr [esp+0D0h+var_10+4], edx
00500258  call    __allmul
0050025D  mov     cl, 10h
0050025F  call    __allshr
00500264  add     eax, [esp+0C0h+var_9C]
00500268  mov     ecx, dword ptr [esp+0C0h+var_50+4]
0050026C  mov     ebp, [ebp+1Ch]
0050026F  push    ecx
00500270  mov     ecx, dword ptr [esp+0C4h+var_50]
00500274  mov     [esp+0C4h+var_74], eax
00500278  mov     eax, [esp+0C4h+var_18]
0050027F  cdq
00500280  push    ecx
00500281  push    edx
00500282  push    eax
00500283  mov     dword ptr [esp+0D0h+var_28], eax
0050028A  mov     dword ptr [esp+0D0h+var_28+4], edx
00500291  mov     [esp+0D0h+var_78], ebp
00500295  call    __allmul
0050029A  mov     cl, 10h
0050029C  call    __allshr
005002A1  add     eax, ebp
005002A3  mov     [esp+0C0h+var_70], eax
005002A7  mov     eax, [esp+0C0h+var_B0]
005002AB  sub     eax, [esp+0C0h+var_5C]
005002AF  cdq
005002B0  push    edx
005002B1  push    eax
005002B2  push    10000h
005002B7  push    0
005002B9  call    __alldiv
005002BE  mov     ecx, dword ptr [esp+0C0h+var_10+4]
005002C5  mov     dword ptr [esp+0C0h+var_40+4], edx
005002CC  mov     edx, dword ptr [esp+0C0h+var_68+4]
005002D0  push    edx
005002D1  mov     edx, dword ptr [esp+0C4h+var_10]
005002D8  mov     dword ptr [esp+0C4h+var_40], eax
005002DF  mov     eax, dword ptr [esp+0C4h+var_68]
005002E3  push    eax
005002E4  push    ecx
005002E5  push    edx
005002E6  call    __allmul
005002EB  mov     cl, 10h
005002ED  call    __allshr
005002F2  mov     ebp, edx
005002F4  mov     edx, [esp+0C0h+var_54]
005002F8  mov     ecx, eax
005002FA  mov     eax, [edx+18h]
005002FD  cdq
005002FE  sub     eax, ecx
00500300  mov     ecx, eax
00500302  mov     eax, [esp+0C0h+var_9C]
00500306  sbb     edx, ebp
00500308  mov     ebp, edx
0050030A  cdq
0050030B  sub     ecx, eax
0050030D  mov     eax, dword ptr [esp+0C0h+var_40]
00500314  sbb     ebp, edx
00500316  mov     edx, dword ptr [esp+0C0h+var_40+4]
0050031D  push    edx
0050031E  push    eax
0050031F  push    ebp
00500320  push    ecx
00500321  call    __allmul
00500326  mov     cl, 20h ; ' '
00500328  call    __allshr
0050032D  mov     ecx, dword ptr [esp+0C0h+var_68+4]
00500331  mov     edx, dword ptr [esp+0C0h+var_68]
00500335  push    ecx
00500336  mov     ecx, dword ptr [esp+0C4h+var_28]
0050033D  mov     [esp+0C4h+var_34], eax
00500344  mov     eax, dword ptr [esp+0C4h+var_28+4]
0050034B  push    edx
0050034C  push    eax
0050034D  push    ecx
0050034E  call    __allmul
00500353  mov     cl, 10h
00500355  call    __allshr
0050035A  mov     ebp, edx
0050035C  mov     edx, [esp+0C0h+var_54]
00500360  mov     ecx, eax
00500362  mov     eax, [edx+1Ch]
00500365  cdq
00500366  sub     eax, ecx
00500368  sbb     edx, ebp
0050036A  mov     ecx, eax
0050036C  mov     eax, [esp+0C0h+var_78]
00500370  mov     ebp, edx
00500372  cdq
00500373  sub     ecx, eax
00500375  mov     eax, dword ptr [esp+0C0h+var_40]
0050037C  sbb     ebp, edx
0050037E  mov     edx, dword ptr [esp+0C0h+var_40+4]
00500385  push    edx
00500386  push    eax
00500387  push    ebp
00500388  push    ecx
00500389  call    __allmul
0050038E  mov     cl, 20h ; ' '
00500390  call    __allshr
00500395  mov     ecx, [esp+0C0h+var_A0]
00500399  mov     edx, ecx
0050039B  imul    edx, [esp+0C0h+var_1C]
005003A3  mov     [esp+0C0h+var_48], eax
005003A7  mov     eax, [esp+0C0h+arg_0]
005003AE  lea     edx, [eax+edx*2]
005003B1  mov     eax, [esp+0C0h+var_58]
005003B5  sub     eax, ecx
005003B7  mov     [esp+0C0h+var_6C], edx
005003BB  mov     [esp+0C0h+var_94], eax
005003BF  jz      loc_500D5B
005003C5  mov     eax, [esp+0C0h+var_B0]
005003C9  sub     eax, [esp+0C0h+var_A4]
005003CD  mov     ecx, dword ptr [esp+0C0h+var_68]
005003D1  cdq
005003D2  mov     ebx, eax
005003D4  mov     eax, dword ptr [esp+0C0h+var_68+4]
005003D8  push    eax
005003D9  push    ecx
005003DA  push    10000h
005003DF  push    0
005003E1  mov     ebp, edx
005003E3  call    __alldiv
005003E8  push    edx
005003E9  push    eax
005003EA  push    ebp
005003EB  push    ebx
005003EC  call    __allmul
005003F1  mov     cl, 20h ; ' '
005003F3  call    __allshr
005003F8  mov     ecx, dword ptr [esp+0C0h+var_50+4]
005003FC  push    ecx
005003FD  mov     ecx, dword ptr [esp+0C4h+var_50]
00500401  mov     [esp+0C4h+var_A8], eax
00500405  cdq
00500406  push    ecx
00500407  push    edx
00500408  push    eax
00500409  call    __allmul
0050040E  mov     cl, 10h
00500410  call    __allshr
00500415  mov     edx, [esp+0C0h+var_54]
00500419  mov     ebx, [esp+0C0h+var_7C]
0050041D  mov     ecx, eax
0050041F  mov     eax, [edx]
00500421  mov     edx, [esp+0C0h+var_5C]
00500425  add     ecx, [esp+0C0h+var_A4]
00500429  cmp     edx, eax
0050042B  jge     loc_5008BB
00500431  cmp     [esp+0C0h+var_94], 0
00500436  jle     loc_500D5B
0050043C  mov     eax, [esp+0C0h+var_34]
00500443  cdq
00500444  mov     dword ptr [esp+0C0h+var_50], eax
00500448  mov     eax, [esp+0C0h+var_48]
0050044C  mov     dword ptr [esp+0C0h+var_50+4], edx
00500450  cdq
00500451  mov     dword ptr [esp+0C0h+var_68], eax
00500455  add     ecx, 0FFFFh
0050045B  lea     eax, [esi+0FFFFh]
00500461  mov     dword ptr [esp+0C0h+var_68+4], edx
00500465  mov     [esp+0C0h+var_9C], ecx
00500469  mov     [esp+0C0h+var_A0], eax
0050046D  mov     ebp, [esp+0C0h+var_A0]
00500471  mov     ecx, dword ptr [esp+0C0h+var_50+4]
00500475  dec     [esp+0C0h+var_94]
00500479  and     ebp, 0FFFF0000h
0050047F  mov     eax, ebp
00500481  sub     eax, esi
00500483  cdq
00500484  push    edx
00500485  push    eax
00500486  mov     dword ptr [esp+0C8h+var_40+4], edx
0050048D  mov     edx, dword ptr [esp+0C8h+var_50]
00500491  push    ecx
00500492  push    edx
00500493  mov     dword ptr [esp+0D0h+var_40], eax
0050049A  call    __allmul
0050049F  mov     cl, 10h
005004A1  call    __allshr
005004A6  add     eax, [esp+0C0h+var_74]
005004AA  mov     ecx, dword ptr [esp+0C0h+var_40]
005004B1  mov     edx, dword ptr [esp+0C0h+var_68+4]
005004B5  mov     [esp+0C0h+var_AC], eax
005004B9  mov     eax, dword ptr [esp+0C0h+var_40+4]
005004C0  push    eax
005004C1  mov     eax, dword ptr [esp+0C4h+var_68]
005004C5  push    ecx
005004C6  push    edx
005004C7  push    eax
005004C8  call    __allmul
005004CD  mov     cl, 10h
005004CF  call    __allshr
005004D4  mov     edx, [esp+0C0h+var_6C]
005004D8  add     eax, [esp+0C0h+var_70]
005004DC  mov     ecx, ebp
005004DE  sar     ecx, 10h
005004E1  lea     ecx, [edx+ecx*2]
005004E4  mov     [esp+0C0h+var_84], ecx
005004E8  mov     ecx, [esp+0C0h+var_9C]
005004EC  and     ecx, 0FFFF0000h
005004F2  sub     ecx, ebp
005004F4  sar     ecx, 10h
005004F7  test    ecx, ecx
005004F9  jle     loc_50086F
005004FF  mov     ebp, [esp+0C0h+var_AC]
00500503  lea     edx, [eax-8000h]
00500509  add     ebp, 0FFFF8000h
0050050F  mov     [esp+0C0h+var_90], edx
00500513  mov     [esp+0C0h+var_88], ebp
00500517  jmp     short loc_500520
00500520  dec     ecx
00500521  mov     dword ptr [esp+0C0h+var_10], ecx
00500528  test    ebp, ebp
0050052A  jle     short loc_500551
0050052C  and     ebp, 0FFFF0000h
00500532  mov     eax, ebp
00500534  mov     [esp+0C0h+var_AC], edx
00500538  xor     edx, edx
0050053A  sar     eax, 10h
0050053D  lea     ecx, [ebx-1]
00500540  cmp     eax, ecx
00500542  setl    dl
00500545  mov     esi, edx
00500547  mov     [esp+0C0h+var_80], edx
0050054B  mov     edx, [esp+0C0h+var_AC]
0050054F  jmp     short loc_500559
00500551  xor     ebp, ebp
00500553  xor     esi, esi
00500555  mov     [esp+0C0h+var_80], esi
00500559  test    edx, edx
0050055B  jle     short loc_50058C
0050055D  mov     ecx, [esp+0C0h+var_4]
00500564  mov     esi, [esp+0C0h+var_80]
00500568  and     edx, 0FFFF0000h
0050056E  mov     eax, edx
00500570  mov     [esp+0C0h+var_AC], edx
00500574  xor     edx, edx
00500576  sar     eax, 10h
00500579  dec     ecx
0050057A  cmp     eax, ecx
0050057C  setnl   dl
0050057F  dec     edx
00500580  and     edx, ebx
00500582  mov     [esp+0C0h+var_98], edx
00500586  mov     edx, [esp+0C0h+var_AC]
0050058A  jmp     short loc_500592
0050058C  xor     edx, edx
0050058E  mov     [esp+0C0h+var_98], edx
00500592  mov     ecx, edx
00500594  sar     ecx, 10h
00500597  imul    ecx, ebx
0050059A  mov     eax, ebp
0050059C  sar     eax, 10h
0050059F  add     ecx, eax
005005A1  mov     ebx, [edi+ecx*4]
005005A4  lea     eax, [ecx+esi]
005005A7  mov     eax, [edi+eax*4]
005005AA  mov     [esp+0C0h+var_A4], eax
005005AE  mov     eax, [esp+0C0h+var_98]
005005B2  add     ecx, eax
005005B4  mov     eax, [edi+ecx*4]
005005B7  mov     [esp+0C0h+var_78], eax
005005BB  mov     eax, [esp+0C0h+var_90]
005005BF  sub     eax, edx
005005C1  add     ecx, esi
005005C3  mov     esi, [esp+0C0h+var_88]
005005C7  sub     esi, ebp
005005C9  and     eax, 0FFFEh
005005CE  inc     eax
005005CF  and     esi, 0FFFEh
005005D5  inc     esi
005005D6  mov     [esp+0C0h+var_98], esi
005005DA  mov     ebp, 10000h
005005DF  sub     ebp, esi
005005E1  mov     esi, ebp
005005E3  imul    ebp, eax
005005E6  mov     edx, 10000h
005005EB  sub     edx, eax
005005ED  imul    eax, [esp+0C0h+var_98]
005005F2  imul    esi, edx
005005F5  imul    edx, [esp+0C0h+var_98]
005005FA  shr     edx, 10h
005005FD  mov     ecx, [edi+ecx*4]
00500600  mov     dword ptr [esp+0C0h+var_28], ebx
00500607  shr     ebx, 18h
0050060A  shr     esi, 10h
0050060D  imul    esi, ebx
00500610  mov     ebx, [esp+0C0h+var_A4]
00500614  shr     ebx, 18h
00500617  imul    edx, ebx
0050061A  mov     ebx, [esp+0C0h+var_78]
0050061E  shr     edx, 10h
00500621  mov     [esp+0C0h+var_B0], edx
00500625  shr     ebp, 10h
00500628  shr     eax, 10h
0050062B  mov     edx, ebx
0050062D  shr     edx, 18h
00500630  imul    ebp, edx
00500633  mov     edx, ecx
00500635  shr     edx, 18h
00500638  imul    eax, edx
0050063B  shr     eax, 10h
0050063E  mov     edx, eax
00500640  mov     [esp+0C0h+var_98], edx
00500644  mov     eax, ecx
00500646  and     eax, 0FF0000h
0050064B  imul    eax, edx
0050064E  shr     ebp, 10h
00500651  mov     edx, ebx
00500653  and     edx, 0FF0000h
00500659  imul    edx, ebp
0050065C  add     eax, edx
0050065E  mov     edx, [esp+0C0h+var_A4]
00500662  and     edx, 0FF0000h
00500668  imul    edx, [esp+0C0h+var_B0]
0050066D  add     eax, edx
0050066F  mov     edx, dword ptr [esp+0C0h+var_28]
00500676  and     edx, 0FF0000h
0050067C  shr     esi, 10h
0050067F  imul    edx, esi
00500682  add     eax, edx
00500684  mov     [esp+0C0h+var_80], ebp
00500688  movzx   ebp, cl
0050068B  imul    ebp, [esp+0C0h+var_98]
00500690  movzx   edx, bl
00500693  imul    edx, [esp+0C0h+var_80]
00500698  and     eax, 0FF0000FFh
0050069D  add     ebp, edx
0050069F  movzx   edx, byte ptr [esp+0C0h+var_A4]
005006A4  imul    edx, [esp+0C0h+var_B0]
005006A9  add     ebp, edx
005006AB  mov     edx, dword ptr [esp+0C0h+var_28]
005006B2  movzx   ebx, dl
005006B5  imul    ebx, esi
005006B8  and     ecx, 0FF00h
005006BE  imul    ecx, [esp+0C0h+var_98]
005006C3  add     ebp, ebx
005006C5  mov     ebx, [esp+0C0h+var_78]
005006C9  and     ebx, 0FF00h
005006CF  imul    ebx, [esp+0C0h+var_80]
005006D4  add     ecx, ebx
005006D6  mov     ebx, [esp+0C0h+var_A4]
005006DA  and     ebp, 0FF00h
005006E0  or      eax, ebp
005006E2  mov     ebp, [esp+0C0h+var_B0]
005006E6  and     ebx, 0FF00h
005006EC  and     edx, 0FF00h
005006F2  imul    ebx, ebp
005006F5  imul    edx, esi
005006F8  add     ecx, ebx
005006FA  add     ecx, edx
005006FC  mov     edx, [esp+0C0h+var_80]
00500700  and     ecx, 0FF0000h
00500706  or      eax, ecx
00500708  mov     ecx, [esp+0C0h+var_98]
0050070C  add     ecx, edx
0050070E  add     ecx, ebp
00500710  add     ecx, esi
00500712  shl     ecx, 18h
00500715  and     ecx, 0FF000000h
0050071B  shr     eax, 8
0050071E  or      eax, ecx
00500720  mov     ecx, eax
00500722  and     ecx, 0FF000000h
00500728  cmp     ecx, 8000000h
0050072E  jbe     loc_500838
00500734  mov     ecx, [esp+0C0h+arg_4]
0050073B  mov     esi, [ecx]
0050073D  mov     edx, eax
0050073F  movzx   ebx, al
00500742  imul    ebx, [ecx+0Ch]
00500746  and     edx, 0FF0000h
0050074C  imul    edx, [ecx+4]
00500750  and     eax, 0FF00h
00500755  imul    eax, [ecx+8]
00500759  and     edx, 0FF0000FFh
0050075F  and     eax, 0FF0000h
00500764  and     ebx, 0FF00h
0050076A  or      edx, ebx
0050076C  or      edx, eax
0050076E  shr     edx, 8
00500771  mov     ecx, edx
00500773  and     ecx, 0FF0000h
00500779  imul    ecx, esi
0050077C  mov     eax, edx
0050077E  and     eax, 0FF00h
00500783  imul    eax, esi
00500786  movzx   edx, dl
00500789  imul    edx, esi
0050078C  mov     esi, [esp+0C0h+var_84]
00500790  and     ecx, 0FF0000FFh
00500796  and     eax, 0FF0000h
0050079B  or      ecx, eax
0050079D  and     edx, 0FF00h
005007A3  or      ecx, edx
005007A5  movzx   edx, word ptr [esi]
005007A8  mov     eax, edx
005007AA  and     eax, 0F800h
005007AF  mov     ebx, edx
005007B1  shr     ecx, 8
005007B4  shl     eax, 5
005007B7  and     ebx, 1Fh
005007BA  or      eax, ebx
005007BC  mov     ebx, ecx
005007BE  add     eax, eax
005007C0  and     ecx, 0FF00h
005007C6  add     eax, eax
005007C8  and     edx, 7E0h
005007CE  add     eax, eax
005007D0  or      edx, 18h
005007D3  and     ebx, 0FF00FFh
005007D9  or      eax, 70007h
005007DE  add     eax, ebx
005007E0  shl     edx, 5
005007E3  add     edx, ecx
005007E5  mov     ecx, eax
005007E7  shr     ecx, 18h
005007EA  and     ecx, 1
005007ED  imul    ecx, 0FF0000h
005007F3  mov     ebx, eax
005007F5  shr     ebx, 8
005007F8  and     ebx, 1
005007FB  imul    ebx, 0FFh
00500801  or      ecx, ebx
00500803  or      eax, ecx
00500805  mov     ecx, edx
00500807  shr     ecx, 10h
0050080A  and     ecx, 1
0050080D  imul    ecx, 0FF00h
00500813  shr     edx, 5
00500816  shr     ecx, 5
00500819  or      ecx, edx
0050081B  mov     edx, eax
0050081D  shr     edx, 3
00500820  and     ecx, 7E0h
00500826  and     edx, 1Fh
00500829  shr     eax, 8
0050082C  or      ecx, edx
0050082E  and     eax, 0F800h
00500833  or      ecx, eax
00500835  mov     [esi], cx
00500838  mov     ebp, [esp+0C0h+var_88]
0050083C  mov     edx, [esp+0C0h+var_90]
00500840  add     ebp, [esp+0C0h+var_34]
00500847  add     edx, [esp+0C0h+var_48]
0050084B  mov     ecx, dword ptr [esp+0C0h+var_10]
00500852  add     [esp+0C0h+var_84], 2
00500857  mov     ebx, [esp+0C0h+var_7C]
0050085B  mov     [esp+0C0h+var_88], ebp
0050085F  mov     [esp+0C0h+var_90], edx
00500863  test    ecx, ecx
00500865  jg      loc_500520
0050086B  mov     esi, [esp+0C0h+var_44]
0050086F  mov     eax, [esp+0C0h+var_14]
00500876  add     [esp+0C0h+var_A0], eax
0050087A  mov     ecx, [esp+0C0h+var_8]
00500881  mov     edx, [esp+0C0h+var_18]
00500888  add     [esp+0C0h+var_74], ecx
0050088C  add     [esp+0C0h+var_70], edx
00500890  add     esi, eax
00500892  mov     eax, [esp+0C0h+var_A8]
00500896  add     [esp+0C0h+var_9C], eax
0050089A  mov     eax, [esp+0C0h+var_1C]
005008A1  add     eax, eax
005008A3  add     [esp+0C0h+var_6C], eax
005008A7  cmp     [esp+0C0h+var_94], 0
005008AC  mov     [esp+0C0h+var_44], esi
005008B0  jg      loc_50046D
005008B6  jmp     loc_500D5B
005008BB  jle     loc_500D5B
005008C1  cmp     [esp+0C0h+var_94], 0
005008C6  jle     loc_500D5B
005008CC  mov     eax, [esp+0C0h+var_34]
005008D3  cdq
005008D4  mov     dword ptr [esp+0C0h+var_50], eax
005008D8  mov     eax, [esp+0C0h+var_48]
005008DC  mov     dword ptr [esp+0C0h+var_50+4], edx
005008E0  cdq
005008E1  mov     dword ptr [esp+0C0h+var_68], eax
005008E5  lea     eax, [esi+0FFFFh]
005008EB  add     ecx, 0FFFFh
005008F1  mov     dword ptr [esp+0C0h+var_68+4], edx
005008F5  mov     [esp+0C0h+var_98], eax
005008F9  mov     [esp+0C0h+var_B0], ecx
005008FD  mov     ebp, [esp+0C0h+var_B0]
00500901  mov     ecx, dword ptr [esp+0C0h+var_50+4]
00500905  dec     [esp+0C0h+var_94]
00500909  and     ebp, 0FFFF0000h
0050090F  mov     eax, ebp
00500911  sub     eax, esi
00500913  cdq
00500914  push    edx
00500915  push    eax
00500916  mov     dword ptr [esp+0C8h+var_30+4], edx
0050091D  mov     edx, dword ptr [esp+0C8h+var_50]
00500921  push    ecx
00500922  push    edx
00500923  mov     dword ptr [esp+0D0h+var_30], eax
0050092A  call    __allmul
0050092F  mov     cl, 10h
00500931  call    __allshr
00500936  add     eax, [esp+0C0h+var_74]
0050093A  mov     ecx, dword ptr [esp+0C0h+var_30]
00500941  mov     edx, dword ptr [esp+0C0h+var_68+4]
00500945  mov     dword ptr [esp+0C0h+var_10], eax
0050094C  mov     eax, dword ptr [esp+0C0h+var_30+4]
00500953  push    eax
00500954  mov     eax, dword ptr [esp+0C4h+var_68]
00500958  push    ecx
00500959  push    edx
0050095A  push    eax
0050095B  call    __allmul
00500960  mov     cl, 10h
00500962  call    __allshr
00500967  mov     edx, [esp+0C0h+var_6C]
0050096B  add     eax, [esp+0C0h+var_70]
0050096F  mov     ecx, ebp
00500971  sar     ecx, 10h
00500974  lea     ecx, [edx+ecx*2]
00500977  mov     [esp+0C0h+var_88], ecx
0050097B  mov     ecx, [esp+0C0h+var_98]
0050097F  and     ecx, 0FFFF0000h
00500985  sub     ecx, ebp
00500987  sar     ecx, 10h
0050098A  test    ecx, ecx
0050098C  jle     loc_500D14
00500992  mov     ebp, dword ptr [esp+0C0h+var_10]
00500999  lea     edx, [eax-8000h]
0050099F  add     ebp, 0FFFF8000h
005009A5  mov     [esp+0C0h+var_80], edx
005009A9  mov     [esp+0C0h+var_84], ebp
005009AD  lea     ecx, [ecx+0]
005009B0  dec     ecx
005009B1  mov     dword ptr [esp+0C0h+var_40], ecx
005009B8  test    ebp, ebp
005009BA  jle     short loc_5009E7
005009BC  and     ebp, 0FFFF0000h
005009C2  mov     eax, ebp
005009C4  mov     dword ptr [esp+0C0h+var_10], edx
005009CB  xor     edx, edx
005009CD  sar     eax, 10h
005009D0  lea     ecx, [ebx-1]
005009D3  cmp     eax, ecx
005009D5  setl    dl
005009D8  mov     esi, edx
005009DA  mov     [esp+0C0h+var_78], edx
005009DE  mov     edx, dword ptr [esp+0C0h+var_10]
005009E5  jmp     short loc_5009EF
005009E7  xor     ebp, ebp
005009E9  xor     esi, esi
005009EB  mov     [esp+0C0h+var_78], esi
005009EF  test    edx, edx
005009F1  jle     short loc_500A2B
005009F3  mov     ecx, [esp+0C0h+var_4]
005009FA  mov     esi, [esp+0C0h+var_78]
005009FE  and     edx, 0FFFF0000h
00500A04  mov     eax, edx
00500A06  mov     dword ptr [esp+0C0h+var_10], edx
00500A0D  xor     edx, edx
00500A0F  sar     eax, 10h
00500A12  dec     ecx
00500A13  cmp     eax, ecx
00500A15  setnl   dl
00500A18  dec     edx
00500A19  and     edx, ebx
00500A1B  mov     dword ptr [esp+0C0h+var_28], edx
00500A22  mov     edx, dword ptr [esp+0C0h+var_10]
00500A29  jmp     short loc_500A34
00500A2B  xor     edx, edx
00500A2D  mov     dword ptr [esp+0C0h+var_28], edx
00500A34  mov     ecx, edx
00500A36  sar     ecx, 10h
00500A39  imul    ecx, ebx
00500A3C  mov     eax, ebp
00500A3E  sar     eax, 10h
00500A41  add     ecx, eax
00500A43  mov     ebx, [edi+ecx*4]
00500A46  lea     eax, [ecx+esi]
00500A49  mov     eax, [edi+eax*4]
00500A4C  mov     [esp+0C0h+var_A4], eax
00500A50  mov     eax, dword ptr [esp+0C0h+var_28]
00500A57  add     ecx, eax
00500A59  mov     eax, [edi+ecx*4]
00500A5C  mov     dword ptr [esp+0C0h+var_28], eax
00500A63  mov     eax, [esp+0C0h+var_80]
00500A67  sub     eax, edx
00500A69  add     ecx, esi
00500A6B  mov     esi, [esp+0C0h+var_84]
00500A6F  sub     esi, ebp
00500A71  and     eax, 0FFFEh
00500A76  inc     eax
00500A77  and     esi, 0FFFEh
00500A7D  inc     esi
00500A7E  mov     [esp+0C0h+var_78], esi
00500A82  mov     ebp, 10000h
00500A87  sub     ebp, esi
00500A89  mov     esi, ebp
00500A8B  imul    ebp, eax
00500A8E  mov     edx, 10000h
00500A93  sub     edx, eax
00500A95  imul    eax, [esp+0C0h+var_78]
00500A9A  imul    esi, edx
00500A9D  imul    edx, [esp+0C0h+var_78]
00500AA2  shr     edx, 10h
00500AA5  mov     ecx, [edi+ecx*4]
00500AA8  mov     [esp+0C0h+var_AC], ebx
00500AAC  shr     ebx, 18h
00500AAF  shr     esi, 10h
00500AB2  imul    esi, ebx
00500AB5  mov     ebx, [esp+0C0h+var_A4]
00500AB9  shr     ebx, 18h
00500ABC  imul    edx, ebx
00500ABF  mov     ebx, dword ptr [esp+0C0h+var_28]
00500AC6  shr     edx, 10h
00500AC9  mov     [esp+0C0h+var_9C], edx
00500ACD  shr     ebp, 10h
00500AD0  shr     eax, 10h
00500AD3  mov     edx, ebx
00500AD5  shr     edx, 18h
00500AD8  imul    ebp, edx
00500ADB  mov     edx, ecx
00500ADD  shr     edx, 18h
00500AE0  imul    eax, edx
00500AE3  shr     eax, 10h
00500AE6  mov     edx, eax
00500AE8  mov     [esp+0C0h+var_A0], edx
00500AEC  mov     eax, ecx
00500AEE  and     eax, 0FF0000h
00500AF3  imul    eax, edx
00500AF6  shr     ebp, 10h
00500AF9  mov     edx, ebx
00500AFB  and     edx, 0FF0000h
00500B01  imul    edx, ebp
00500B04  add     eax, edx
00500B06  mov     edx, [esp+0C0h+var_A4]
00500B0A  and     edx, 0FF0000h
00500B10  imul    edx, [esp+0C0h+var_9C]
00500B15  add     eax, edx
00500B17  mov     edx, [esp+0C0h+var_AC]
00500B1B  and     edx, 0FF0000h
00500B21  shr     esi, 10h
00500B24  imul    edx, esi
00500B27  add     eax, edx
00500B29  mov     [esp+0C0h+var_90], ebp
00500B2D  movzx   ebp, cl
00500B30  imul    ebp, [esp+0C0h+var_A0]
00500B35  movzx   edx, bl
00500B38  imul    edx, [esp+0C0h+var_90]
00500B3D  and     eax, 0FF0000FFh
00500B42  add     ebp, edx
00500B44  movzx   edx, byte ptr [esp+0C0h+var_A4]
00500B49  imul    edx, [esp+0C0h+var_9C]
00500B4E  add     ebp, edx
00500B50  mov     edx, [esp+0C0h+var_AC]
00500B54  movzx   ebx, dl
00500B57  imul    ebx, esi
00500B5A  and     ecx, 0FF00h
00500B60  imul    ecx, [esp+0C0h+var_A0]
00500B65  add     ebp, ebx
00500B67  mov     ebx, dword ptr [esp+0C0h+var_28]
00500B6E  and     ebx, 0FF00h
00500B74  imul    ebx, [esp+0C0h+var_90]
00500B79  add     ecx, ebx
00500B7B  mov     ebx, [esp+0C0h+var_A4]
00500B7F  and     ebp, 0FF00h
00500B85  or      eax, ebp
00500B87  mov     ebp, [esp+0C0h+var_9C]
00500B8B  and     ebx, 0FF00h
00500B91  and     edx, 0FF00h
00500B97  imul    ebx, ebp
00500B9A  imul    edx, esi
00500B9D  add     ecx, ebx
00500B9F  add     ecx, edx
00500BA1  mov     edx, [esp+0C0h+var_90]
00500BA5  and     ecx, 0FF0000h
00500BAB  or      eax, ecx
00500BAD  mov     ecx, [esp+0C0h+var_A0]
00500BB1  add     ecx, edx
00500BB3  add     ecx, ebp
00500BB5  add     ecx, esi
00500BB7  shl     ecx, 18h
00500BBA  and     ecx, 0FF000000h
00500BC0  shr     eax, 8
00500BC3  or      eax, ecx
00500BC5  mov     ecx, eax
00500BC7  and     ecx, 0FF000000h
00500BCD  cmp     ecx, 8000000h
00500BD3  jbe     loc_500CDD
00500BD9  mov     ecx, [esp+0C0h+arg_4]
00500BE0  mov     esi, [ecx]
00500BE2  mov     edx, eax
00500BE4  movzx   ebx, al
00500BE7  imul    ebx, [ecx+0Ch]
00500BEB  and     edx, 0FF0000h
00500BF1  imul    edx, [ecx+4]
00500BF5  and     eax, 0FF00h
00500BFA  imul    eax, [ecx+8]
00500BFE  and     edx, 0FF0000FFh
00500C04  and     eax, 0FF0000h
00500C09  and     ebx, 0FF00h
00500C0F  or      edx, ebx
00500C11  or      edx, eax
00500C13  shr     edx, 8
00500C16  mov     ecx, edx
00500C18  and     ecx, 0FF0000h
00500C1E  imul    ecx, esi
00500C21  mov     eax, edx
00500C23  and     eax, 0FF00h
00500C28  imul    eax, esi
00500C2B  movzx   edx, dl
00500C2E  imul    edx, esi
00500C31  mov     esi, [esp+0C0h+var_88]
00500C35  and     ecx, 0FF0000FFh
00500C3B  and     eax, 0FF0000h
00500C40  or      ecx, eax
00500C42  and     edx, 0FF00h
00500C48  or      ecx, edx
00500C4A  movzx   edx, word ptr [esi]
00500C4D  mov     eax, edx
00500C4F  and     eax, 0F800h
00500C54  mov     ebx, edx
00500C56  shr     ecx, 8
00500C59  shl     eax, 5
00500C5C  and     ebx, 1Fh
00500C5F  or      eax, ebx
00500C61  mov     ebx, ecx
00500C63  add     eax, eax
00500C65  and     ecx, 0FF00h
00500C6B  add     eax, eax
00500C6D  and     edx, 7E0h
00500C73  add     eax, eax
00500C75  or      edx, 18h
00500C78  and     ebx, 0FF00FFh
00500C7E  or      eax, 70007h
00500C83  add     eax, ebx
00500C85  shl     edx, 5
00500C88  add     edx, ecx
00500C8A  mov     ecx, eax
00500C8C  shr     ecx, 18h
00500C8F  and     ecx, 1
00500C92  imul    ecx, 0FF0000h
00500C98  mov     ebx, eax
00500C9A  shr     ebx, 8
00500C9D  and     ebx, 1
00500CA0  imul    ebx, 0FFh
00500CA6  or      ecx, ebx
00500CA8  or      eax, ecx
00500CAA  mov     ecx, edx
00500CAC  shr     ecx, 10h
00500CAF  and     ecx, 1
00500CB2  imul    ecx, 0FF00h
00500CB8  shr     edx, 5
00500CBB  shr     ecx, 5
00500CBE  or      ecx, edx
00500CC0  mov     edx, eax
00500CC2  shr     edx, 3
00500CC5  and     ecx, 7E0h
00500CCB  and     edx, 1Fh
00500CCE  shr     eax, 8
00500CD1  or      ecx, edx
00500CD3  and     eax, 0F800h
00500CD8  or      ecx, eax
00500CDA  mov     [esi], cx
00500CDD  mov     ebp, [esp+0C0h+var_84]
00500CE1  mov     edx, [esp+0C0h+var_80]
00500CE5  add     ebp, [esp+0C0h+var_34]
00500CEC  add     edx, [esp+0C0h+var_48]
00500CF0  mov     ecx, dword ptr [esp+0C0h+var_40]
00500CF7  add     [esp+0C0h+var_88], 2
00500CFC  mov     ebx, [esp+0C0h+var_7C]
00500D00  mov     [esp+0C0h+var_84], ebp
00500D04  mov     [esp+0C0h+var_80], edx
00500D08  test    ecx, ecx
00500D0A  jg      loc_5009B0
00500D10  mov     esi, [esp+0C0h+var_44]
00500D14  mov     eax, [esp+0C0h+var_14]
00500D1B  add     [esp+0C0h+var_98], eax
00500D1F  mov     ecx, [esp+0C0h+var_8]
00500D26  mov     edx, [esp+0C0h+var_18]
00500D2D  add     [esp+0C0h+var_74], ecx
00500D31  add     [esp+0C0h+var_70], edx
00500D35  add     esi, eax
00500D37  mov     eax, [esp+0C0h+var_A8]
00500D3B  add     [esp+0C0h+var_B0], eax
00500D3F  mov     eax, [esp+0C0h+var_1C]
00500D46  add     eax, eax
00500D48  add     [esp+0C0h+var_6C], eax
00500D4C  cmp     [esp+0C0h+var_94], 0
00500D51  mov     [esp+0C0h+var_44], esi
00500D55  jg      loc_5008FD
00500D5B  mov     eax, [esp+0C0h+var_8C]
00500D5F  sub     eax, [esp+0C0h+var_58]
00500D63  mov     [esp+0C0h+var_94], eax
00500D67  jz      loc_5016FB
00500D6D  mov     eax, [esp+0C0h+var_54]
00500D71  mov     ecx, [eax+4]
00500D74  mov     ebp, [eax]
00500D76  mov     eax, [esp+0C0h+var_20]
00500D7D  mov     eax, [eax+4]
00500D80  sub     eax, ecx
00500D82  cdq
00500D83  push    edx
00500D84  push    eax
00500D85  push    10000h
00500D8A  push    0
00500D8C  mov     dword ptr [esp+0D0h+var_40], ecx
00500D93  mov     [esp+0D0h+var_B0], ebp
00500D97  call    __alldiv
00500D9C  mov     ecx, eax
00500D9E  mov     eax, edx
00500DA0  mov     edx, [esp+0C0h+var_20]
00500DA7  mov     [esp+0C0h+var_8C], eax
00500DAB  mov     eax, [edx]
00500DAD  sub     eax, ebp
00500DAF  cdq
00500DB0  push    edx
00500DB1  push    eax
00500DB2  mov     eax, [esp+0C8h+var_8C]
00500DB6  push    eax
00500DB7  push    ecx
00500DB8  call    __allmul
00500DBD  mov     cl, 20h ; ' '
00500DBF  call    __allshr
00500DC4  mov     ecx, eax
00500DC6  mov     eax, [esp+0C0h+var_58]
00500DCA  shl     eax, 10h
00500DCD  sub     eax, dword ptr [esp+0C0h+var_40]
00500DD4  mov     dword ptr [esp+0C0h+var_28], ecx
00500DDB  imul    ecx
00500DDD  mov     cl, 10h
00500DDF  call    __allshr
00500DE4  mov     edx, [esp+0C0h+var_5C]
00500DE8  mov     ecx, eax
00500DEA  mov     eax, ebp
00500DEC  add     ecx, eax
00500DEE  cmp     edx, eax
00500DF0  jge     loc_501276
00500DF6  cmp     [esp+0C0h+var_94], 0
00500DFB  jle     loc_5016FB
00500E01  mov     eax, [esp+0C0h+var_34]
00500E08  cdq
00500E09  mov     dword ptr [esp+0C0h+var_50], eax
00500E0D  mov     eax, [esp+0C0h+var_48]
00500E11  mov     dword ptr [esp+0C0h+var_50+4], edx
00500E15  cdq
00500E16  mov     dword ptr [esp+0C0h+var_68], eax
00500E1A  mov     eax, [esp+0C0h+var_1C]
00500E21  mov     dword ptr [esp+0C0h+var_68+4], edx
00500E25  lea     edx, [eax+eax]
00500E28  add     ecx, 0FFFFh
00500E2E  lea     eax, [esi+0FFFFh]
00500E34  mov     [esp+0C0h+var_98], edx
00500E38  mov     [esp+0C0h+var_84], ecx
00500E3C  mov     [esp+0C0h+var_88], eax
00500E40  mov     ebp, [esp+0C0h+var_88]
00500E44  mov     ecx, dword ptr [esp+0C0h+var_50+4]
00500E48  dec     [esp+0C0h+var_94]
00500E4C  and     ebp, 0FFFF0000h
00500E52  push    ecx
00500E53  mov     ecx, dword ptr [esp+0C4h+var_50]
00500E57  mov     eax, ebp
00500E59  sub     eax, esi
00500E5B  cdq
00500E5C  push    ecx
00500E5D  push    edx
00500E5E  push    eax
00500E5F  mov     dword ptr [esp+0D0h+var_30], eax
00500E66  mov     dword ptr [esp+0D0h+var_30+4], edx
00500E6D  call    __allmul
00500E72  mov     cl, 10h
00500E74  call    __allshr
00500E79  add     eax, [esp+0C0h+var_74]
00500E7D  mov     edx, dword ptr [esp+0C0h+var_68+4]
00500E81  mov     ecx, dword ptr [esp+0C0h+var_30+4]
00500E88  push    edx
00500E89  mov     edx, dword ptr [esp+0C4h+var_30]
00500E90  mov     [esp+0C4h+var_8C], eax
00500E94  mov     eax, dword ptr [esp+0C4h+var_68]
00500E98  push    eax
00500E99  push    ecx
00500E9A  push    edx
00500E9B  call    __allmul
00500EA0  mov     cl, 10h
00500EA2  call    __allshr
00500EA7  mov     edx, [esp+0C0h+var_6C]
00500EAB  add     eax, [esp+0C0h+var_70]
00500EAF  mov     ecx, ebp
00500EB1  sar     ecx, 10h
00500EB4  lea     ecx, [edx+ecx*2]
00500EB7  mov     [esp+0C0h+var_9C], ecx
00500EBB  mov     ecx, [esp+0C0h+var_84]
00500EBF  and     ecx, 0FFFF0000h
00500EC5  sub     ecx, ebp
00500EC7  sar     ecx, 10h
00500ECA  test    ecx, ecx
00500ECC  jle     loc_501226
00500ED2  mov     esi, [esp+0C0h+var_8C]
00500ED6  add     eax, 0FFFF8000h
00500EDB  add     esi, 0FFFF8000h
00500EE1  mov     [esp+0C0h+var_90], eax
00500EE5  mov     [esp+0C0h+var_A0], esi
00500EE9  lea     esp, [esp+0]
00500EF0  dec     ecx
00500EF1  mov     dword ptr [esp+0C0h+var_40], ecx
00500EF8  test    esi, esi
00500EFA  jle     short loc_500F21
00500EFC  and     esi, 0FFFF0000h
00500F02  mov     edx, esi
00500F04  mov     [esp+0C0h+var_8C], eax
00500F08  xor     eax, eax
00500F0A  sar     edx, 10h
00500F0D  lea     ecx, [ebx-1]
00500F10  cmp     edx, ecx
00500F12  setl    al
00500F15  mov     edx, eax
00500F17  mov     [esp+0C0h+var_A8], eax
00500F1B  mov     eax, [esp+0C0h+var_8C]
00500F1F  jmp     short loc_500F29
00500F21  xor     esi, esi
00500F23  xor     edx, edx
00500F25  mov     [esp+0C0h+var_A8], edx
00500F29  xor     ecx, ecx
00500F2B  cmp     eax, ecx
00500F2D  jle     short loc_500F60
00500F2F  mov     ecx, eax
00500F31  mov     eax, [esp+0C0h+var_4]
00500F38  and     ecx, 0FFFF0000h
00500F3E  mov     edx, ecx
00500F40  mov     [esp+0C0h+var_8C], ecx
00500F44  sar     edx, 10h
00500F47  xor     ecx, ecx
00500F49  dec     eax
00500F4A  cmp     edx, eax
00500F4C  mov     edx, [esp+0C0h+var_A8]
00500F50  setnl   cl
00500F53  dec     ecx
00500F54  and     ecx, ebx
00500F56  mov     [esp+0C0h+var_AC], ecx
00500F5A  mov     ecx, [esp+0C0h+var_8C]
00500F5E  jmp     short loc_500F64
00500F60  mov     [esp+0C0h+var_AC], ecx
00500F64  mov     eax, ecx
00500F66  sar     eax, 10h
00500F69  imul    eax, ebx
00500F6C  mov     ebx, esi
00500F6E  sar     ebx, 10h
00500F71  add     eax, ebx
00500F73  mov     ebx, [edi+eax*4]
00500F76  lea     ebp, [eax+edx]
00500F79  mov     ebp, [edi+ebp*4]
00500F7C  mov     [esp+0C0h+var_A4], ebp
00500F80  mov     ebp, [esp+0C0h+var_AC]
00500F84  add     eax, ebp
00500F86  mov     ebp, [edi+eax*4]
00500F89  add     eax, edx
00500F8B  mov     edx, [edi+eax*4]
00500F8E  mov     eax, [esp+0C0h+var_90]
00500F92  sub     eax, ecx
00500F94  mov     [esp+0C0h+var_A8], edx
00500F98  mov     edx, [esp+0C0h+var_A0]
00500F9C  sub     edx, esi
00500F9E  and     eax, 0FFFEh
00500FA3  inc     eax
00500FA4  mov     [esp+0C0h+var_B0], ebp
00500FA8  and     edx, 0FFFEh
00500FAE  inc     edx
00500FAF  mov     [esp+0C0h+var_5C], ebx
00500FB3  shr     ebx, 18h
00500FB6  mov     ebp, 10000h
00500FBB  sub     ebp, edx
00500FBD  mov     ecx, ebp
00500FBF  imul    ebp, eax
00500FC2  mov     esi, 10000h
00500FC7  sub     esi, eax
00500FC9  imul    eax, edx
00500FCC  imul    ecx, esi
00500FCF  imul    esi, edx
00500FD2  mov     edx, [esp+0C0h+var_A8]
00500FD6  shr     ecx, 10h
00500FD9  imul    ecx, ebx
00500FDC  mov     ebx, [esp+0C0h+var_A4]
00500FE0  shr     ebx, 18h
00500FE3  shr     esi, 10h
00500FE6  imul    esi, ebx
00500FE9  mov     ebx, [esp+0C0h+var_B0]
00500FED  shr     ebx, 18h
00500FF0  shr     eax, 10h
00500FF3  shr     ebp, 10h
00500FF6  imul    ebp, ebx
00500FF9  mov     ebx, edx
00500FFB  shr     ebx, 18h
00500FFE  imul    eax, ebx
00501001  shr     eax, 10h
00501004  mov     ebx, eax
00501006  mov     [esp+0C0h+var_58], ebx
0050100A  mov     eax, edx
0050100C  and     eax, 0FF00h
00501011  imul    eax, ebx
00501014  mov     ebx, [esp+0C0h+var_B0]
00501018  and     ebx, 0FF00h
0050101E  shr     ebp, 10h
00501021  imul    ebx, ebp
00501024  add     eax, ebx
00501026  mov     ebx, [esp+0C0h+var_A4]
0050102A  and     ebx, 0FF00h
00501030  shr     esi, 10h
00501033  imul    ebx, esi
00501036  add     eax, ebx
00501038  mov     ebx, [esp+0C0h+var_5C]
0050103C  and     ebx, 0FF00h
00501042  shr     ecx, 10h
00501045  imul    ebx, ecx
00501048  add     eax, ebx
0050104A  mov     ebx, [esp+0C0h+var_B0]
0050104E  and     edx, 0FF0000h
00501054  imul    edx, [esp+0C0h+var_58]
00501059  and     ebx, 0FF0000h
0050105F  imul    ebx, ebp
00501062  add     edx, ebx
00501064  mov     ebx, [esp+0C0h+var_A4]
00501068  and     eax, 0FF0000h
0050106D  and     ebx, 0FF0000h
00501073  imul    ebx, esi
00501076  add     edx, ebx
00501078  mov     ebx, [esp+0C0h+var_5C]
0050107C  and     ebx, 0FF0000h
00501082  imul    ebx, ecx
00501085  add     edx, ebx
00501087  movzx   ebx, byte ptr [esp+0C0h+var_B0]
0050108C  and     edx, 0FF0000FFh
00501092  imul    ebx, ebp
00501095  or      eax, edx
00501097  movzx   edx, byte ptr [esp+0C0h+var_A8]
0050109C  imul    edx, [esp+0C0h+var_58]
005010A1  add     edx, ebx
005010A3  movzx   ebx, byte ptr [esp+0C0h+var_A4]
005010A8  imul    ebx, esi
005010AB  add     edx, ebx
005010AD  movzx   ebx, byte ptr [esp+0C0h+var_5C]
005010B2  imul    ebx, ecx
005010B5  add     edx, ebx
005010B7  and     edx, 0FF00h
005010BD  or      eax, edx
005010BF  mov     edx, [esp+0C0h+var_58]
005010C3  add     edx, ebp
005010C5  add     edx, esi
005010C7  add     edx, ecx
005010C9  shl     edx, 18h
005010CC  shr     eax, 8
005010CF  and     edx, 0FF000000h
005010D5  or      eax, edx
005010D7  mov     ecx, eax
005010D9  and     ecx, 0FF000000h
005010DF  cmp     ecx, 8000000h
005010E5  jbe     loc_5011EF
005010EB  mov     ecx, [esp+0C0h+arg_4]
005010F2  mov     esi, [ecx]
005010F4  mov     edx, eax
005010F6  movzx   ebx, al
005010F9  imul    ebx, [ecx+0Ch]
005010FD  and     edx, 0FF0000h
00501103  imul    edx, [ecx+4]
00501107  and     eax, 0FF00h
0050110C  imul    eax, [ecx+8]
00501110  and     edx, 0FF0000FFh
00501116  and     eax, 0FF0000h
0050111B  and     ebx, 0FF00h
00501121  or      edx, ebx
00501123  or      edx, eax
00501125  shr     edx, 8
00501128  mov     ecx, edx
0050112A  and     ecx, 0FF0000h
00501130  imul    ecx, esi
00501133  mov     eax, edx
00501135  and     eax, 0FF00h
0050113A  imul    eax, esi
0050113D  movzx   edx, dl
00501140  imul    edx, esi
00501143  mov     esi, [esp+0C0h+var_9C]
00501147  and     ecx, 0FF0000FFh
0050114D  and     eax, 0FF0000h
00501152  or      ecx, eax
00501154  and     edx, 0FF00h
0050115A  or      ecx, edx
0050115C  movzx   edx, word ptr [esi]
0050115F  mov     eax, edx
00501161  and     eax, 0F800h
00501166  mov     ebx, edx
00501168  shr     ecx, 8
0050116B  shl     eax, 5
0050116E  and     ebx, 1Fh
00501171  or      eax, ebx
00501173  mov     ebx, ecx
00501175  add     eax, eax
00501177  and     ecx, 0FF00h
0050117D  add     eax, eax
0050117F  and     edx, 7E0h
00501185  add     eax, eax
00501187  or      edx, 18h
0050118A  and     ebx, 0FF00FFh
00501190  or      eax, 70007h
00501195  add     eax, ebx
00501197  shl     edx, 5
0050119A  add     edx, ecx
0050119C  mov     ecx, eax
0050119E  shr     ecx, 18h
005011A1  and     ecx, 1
005011A4  imul    ecx, 0FF0000h
005011AA  mov     ebx, eax
005011AC  shr     ebx, 8
005011AF  and     ebx, 1
005011B2  imul    ebx, 0FFh
005011B8  or      ecx, ebx
005011BA  or      eax, ecx
005011BC  mov     ecx, edx
005011BE  shr     ecx, 10h
005011C1  and     ecx, 1
005011C4  imul    ecx, 0FF00h
005011CA  shr     edx, 5
005011CD  shr     ecx, 5
005011D0  or      ecx, edx
005011D2  mov     edx, eax
005011D4  shr     edx, 3
005011D7  and     ecx, 7E0h
005011DD  and     edx, 1Fh
005011E0  shr     eax, 8
005011E3  or      ecx, edx
005011E5  and     eax, 0F800h
005011EA  or      ecx, eax
005011EC  mov     [esi], cx
005011EF  mov     esi, [esp+0C0h+var_A0]
005011F3  mov     eax, [esp+0C0h+var_90]
005011F7  add     esi, [esp+0C0h+var_34]
005011FE  add     eax, [esp+0C0h+var_48]
00501202  mov     ecx, dword ptr [esp+0C0h+var_40]
00501209  add     [esp+0C0h+var_9C], 2
0050120E  mov     ebx, [esp+0C0h+var_7C]
00501212  mov     [esp+0C0h+var_A0], esi
00501216  mov     [esp+0C0h+var_90], eax
0050121A  test    ecx, ecx
0050121C  jg      loc_500EF0
00501222  mov     esi, [esp+0C0h+var_44]
00501226  mov     eax, [esp+0C0h+var_14]
0050122D  add     [esp+0C0h+var_88], eax
00501231  mov     ecx, [esp+0C0h+var_98]
00501235  mov     edx, [esp+0C0h+var_8]
0050123C  add     [esp+0C0h+var_6C], ecx
00501240  add     [esp+0C0h+var_74], edx
00501244  add     esi, eax
00501246  mov     eax, dword ptr [esp+0C0h+var_28]
0050124D  add     [esp+0C0h+var_84], eax
00501251  mov     eax, [esp+0C0h+var_18]
00501258  add     [esp+0C0h+var_70], eax
0050125C  cmp     [esp+0C0h+var_94], 0
00501261  mov     [esp+0C0h+var_44], esi
00501265  jg      loc_500E40
0050126B  pop     edi
0050126C  pop     esi
0050126D  pop     ebp
0050126E  pop     ebx
0050126F  add     esp, 0B0h
00501275  retn
00501276  jle     loc_5016FB
0050127C  cmp     [esp+0C0h+var_94], 0
00501281  jle     loc_5016FB
00501287  mov     eax, [esp+0C0h+var_34]
0050128E  cdq
0050128F  mov     dword ptr [esp+0C0h+var_50], eax
00501293  mov     eax, [esp+0C0h+var_48]
00501297  mov     dword ptr [esp+0C0h+var_50+4], edx
0050129B  cdq
0050129C  mov     dword ptr [esp+0C0h+var_68+4], edx
005012A0  mov     edx, [esp+0C0h+var_1C]
005012A7  mov     dword ptr [esp+0C0h+var_68], eax
005012AB  lea     eax, [edx+edx]
005012AE  lea     edx, [esi+0FFFFh]
005012B4  add     ecx, 0FFFFh
005012BA  mov     [esp+0C0h+var_98], eax
005012BE  mov     [esp+0C0h+var_84], edx
005012C2  mov     [esp+0C0h+var_80], ecx
005012C6  mov     ebp, [esp+0C0h+var_80]
005012CA  mov     ecx, dword ptr [esp+0C0h+var_50+4]
005012CE  dec     [esp+0C0h+var_94]
005012D2  and     ebp, 0FFFF0000h
005012D8  push    ecx
005012D9  mov     ecx, dword ptr [esp+0C4h+var_50]
005012DD  mov     eax, ebp
005012DF  sub     eax, esi
005012E1  cdq
005012E2  push    ecx
005012E3  push    edx
005012E4  push    eax
005012E5  mov     dword ptr [esp+0D0h+var_30], eax
005012EC  mov     dword ptr [esp+0D0h+var_30+4], edx
005012F3  call    __allmul
005012F8  mov     cl, 10h
005012FA  call    __allshr
005012FF  add     eax, [esp+0C0h+var_74]
00501303  mov     edx, dword ptr [esp+0C0h+var_68+4]
00501307  mov     ecx, dword ptr [esp+0C0h+var_30+4]
0050130E  push    edx
0050130F  mov     edx, dword ptr [esp+0C4h+var_30]
00501316  mov     [esp+0C4h+var_8C], eax
0050131A  mov     eax, dword ptr [esp+0C4h+var_68]
0050131E  push    eax
0050131F  push    ecx
00501320  push    edx
00501321  call    __allmul
00501326  mov     cl, 10h
00501328  call    __allshr
0050132D  mov     edx, [esp+0C0h+var_6C]
00501331  add     eax, [esp+0C0h+var_70]
00501335  mov     ecx, ebp
00501337  sar     ecx, 10h
0050133A  lea     ecx, [edx+ecx*2]
0050133D  mov     [esp+0C0h+var_A0], ecx
00501341  mov     ecx, [esp+0C0h+var_84]
00501345  and     ecx, 0FFFF0000h
0050134B  sub     ecx, ebp
0050134D  sar     ecx, 10h
00501350  test    ecx, ecx
00501352  jle     loc_5016B6
00501358  mov     ebp, [esp+0C0h+var_8C]
0050135C  lea     edx, [eax-8000h]
00501362  add     ebp, 0FFFF8000h
00501368  mov     [esp+0C0h+var_88], edx
0050136C  mov     [esp+0C0h+var_90], ebp
00501370  dec     ecx
00501371  mov     dword ptr [esp+0C0h+var_40], ecx
00501378  test    ebp, ebp
0050137A  jle     short loc_5013A1
0050137C  and     ebp, 0FFFF0000h
00501382  mov     eax, ebp
00501384  mov     [esp+0C0h+var_8C], edx
00501388  xor     edx, edx
0050138A  sar     eax, 10h
0050138D  lea     ecx, [ebx-1]
00501390  cmp     eax, ecx
00501392  setl    dl
00501395  mov     esi, edx
00501397  mov     [esp+0C0h+var_A8], edx
0050139B  mov     edx, [esp+0C0h+var_8C]
0050139F  jmp     short loc_5013A9
005013A1  xor     ebp, ebp
005013A3  xor     esi, esi
005013A5  mov     [esp+0C0h+var_A8], esi
005013A9  test    edx, edx
005013AB  jle     short loc_5013DC
005013AD  mov     ecx, [esp+0C0h+var_4]
005013B4  mov     esi, [esp+0C0h+var_A8]
005013B8  and     edx, 0FFFF0000h
005013BE  mov     eax, edx
005013C0  mov     [esp+0C0h+var_8C], edx
005013C4  xor     edx, edx
005013C6  sar     eax, 10h
005013C9  dec     ecx
005013CA  cmp     eax, ecx
005013CC  setnl   dl
005013CF  dec     edx
005013D0  and     edx, ebx
005013D2  mov     [esp+0C0h+var_AC], edx
005013D6  mov     edx, [esp+0C0h+var_8C]
005013DA  jmp     short loc_5013E2
005013DC  xor     edx, edx
005013DE  mov     [esp+0C0h+var_AC], edx
005013E2  mov     ecx, edx
005013E4  sar     ecx, 10h
005013E7  imul    ecx, ebx
005013EA  mov     eax, ebp
005013EC  sar     eax, 10h
005013EF  add     ecx, eax
005013F1  mov     ebx, [edi+ecx*4]
005013F4  lea     eax, [ecx+esi]
005013F7  mov     eax, [edi+eax*4]
005013FA  mov     [esp+0C0h+var_B0], eax
005013FE  mov     eax, [esp+0C0h+var_AC]
00501402  add     ecx, eax
00501404  mov     eax, [edi+ecx*4]
00501407  mov     [esp+0C0h+var_AC], eax
0050140B  mov     eax, [esp+0C0h+var_88]
0050140F  sub     eax, edx
00501411  add     ecx, esi
00501413  mov     esi, [esp+0C0h+var_90]
00501417  sub     esi, ebp
00501419  and     eax, 0FFFEh
0050141E  inc     eax
0050141F  and     esi, 0FFFEh
00501425  inc     esi
00501426  mov     [esp+0C0h+var_A8], esi
0050142A  mov     ebp, 10000h
0050142F  sub     ebp, esi
00501431  mov     esi, ebp
00501433  imul    ebp, eax
00501436  mov     edx, 10000h
0050143B  sub     edx, eax
0050143D  imul    eax, [esp+0C0h+var_A8]
00501442  imul    esi, edx
00501445  imul    edx, [esp+0C0h+var_A8]
0050144A  shr     edx, 10h
0050144D  mov     ecx, [edi+ecx*4]
00501450  mov     [esp+0C0h+var_78], ebx
00501454  shr     ebx, 18h
00501457  shr     esi, 10h
0050145A  imul    esi, ebx
0050145D  mov     ebx, [esp+0C0h+var_B0]
00501461  shr     ebx, 18h
00501464  imul    edx, ebx
00501467  mov     ebx, [esp+0C0h+var_AC]
0050146B  shr     edx, 10h
0050146E  mov     [esp+0C0h+var_5C], edx
00501472  shr     ebp, 10h
00501475  shr     eax, 10h
00501478  mov     edx, ebx
0050147A  shr     edx, 18h
0050147D  imul    ebp, edx
00501480  mov     edx, ecx
00501482  shr     edx, 18h
00501485  imul    eax, edx
00501488  shr     eax, 10h
0050148B  mov     edx, eax
0050148D  mov     [esp+0C0h+var_58], edx
00501491  mov     eax, ecx
00501493  and     eax, 0FF0000h
00501498  imul    eax, edx
0050149B  shr     ebp, 10h
0050149E  mov     edx, ebx
005014A0  and     edx, 0FF0000h
005014A6  imul    edx, ebp
005014A9  add     eax, edx
005014AB  mov     edx, [esp+0C0h+var_B0]
005014AF  and     edx, 0FF0000h
005014B5  imul    edx, [esp+0C0h+var_5C]
005014BA  add     eax, edx
005014BC  mov     edx, [esp+0C0h+var_78]
005014C0  and     edx, 0FF0000h
005014C6  shr     esi, 10h
005014C9  imul    edx, esi
005014CC  add     eax, edx
005014CE  mov     [esp+0C0h+var_9C], ebp
005014D2  movzx   ebp, cl
005014D5  imul    ebp, [esp+0C0h+var_58]
005014DA  movzx   edx, bl
005014DD  imul    edx, [esp+0C0h+var_9C]
005014E2  and     eax, 0FF0000FFh
005014E7  add     ebp, edx
005014E9  movzx   edx, byte ptr [esp+0C0h+var_B0]
005014EE  imul    edx, [esp+0C0h+var_5C]
005014F3  add     ebp, edx
005014F5  mov     edx, [esp+0C0h+var_78]
005014F9  movzx   ebx, dl
005014FC  imul    ebx, esi
005014FF  and     ecx, 0FF00h
00501505  imul    ecx, [esp+0C0h+var_58]
0050150A  add     ebp, ebx
0050150C  mov     ebx, [esp+0C0h+var_AC]
00501510  and     ebx, 0FF00h
00501516  imul    ebx, [esp+0C0h+var_9C]
0050151B  add     ecx, ebx
0050151D  mov     ebx, [esp+0C0h+var_B0]
00501521  and     ebp, 0FF00h
00501527  or      eax, ebp
00501529  mov     ebp, [esp+0C0h+var_5C]
0050152D  and     ebx, 0FF00h
00501533  and     edx, 0FF00h
00501539  imul    ebx, ebp
0050153C  imul    edx, esi
0050153F  add     ecx, ebx
00501541  add     ecx, edx
00501543  mov     edx, [esp+0C0h+var_9C]
00501547  and     ecx, 0FF0000h
0050154D  or      eax, ecx
0050154F  mov     ecx, [esp+0C0h+var_58]
00501553  add     ecx, edx
00501555  add     ecx, ebp
00501557  add     ecx, esi
00501559  shl     ecx, 18h
0050155C  and     ecx, 0FF000000h
00501562  shr     eax, 8
00501565  or      eax, ecx
00501567  mov     ecx, eax
00501569  and     ecx, 0FF000000h
0050156F  cmp     ecx, 8000000h
00501575  jbe     loc_50167F
0050157B  mov     ecx, [esp+0C0h+arg_4]
00501582  mov     esi, [ecx]
00501584  mov     edx, eax
00501586  movzx   ebx, al
00501589  imul    ebx, [ecx+0Ch]
0050158D  and     edx, 0FF0000h
00501593  imul    edx, [ecx+4]
00501597  and     eax, 0FF00h
0050159C  imul    eax, [ecx+8]
005015A0  and     edx, 0FF0000FFh
005015A6  and     eax, 0FF0000h
005015AB  and     ebx, 0FF00h
005015B1  or      edx, ebx
005015B3  or      edx, eax
005015B5  shr     edx, 8
005015B8  mov     ecx, edx
005015BA  and     ecx, 0FF0000h
005015C0  imul    ecx, esi
005015C3  mov     eax, edx
005015C5  and     eax, 0FF00h
005015CA  imul    eax, esi
005015CD  movzx   edx, dl
005015D0  imul    edx, esi
005015D3  mov     esi, [esp+0C0h+var_A0]
005015D7  and     ecx, 0FF0000FFh
005015DD  and     eax, 0FF0000h
005015E2  or      ecx, eax
005015E4  and     edx, 0FF00h
005015EA  or      ecx, edx
005015EC  movzx   edx, word ptr [esi]
005015EF  mov     eax, edx
005015F1  and     eax, 0F800h
005015F6  mov     ebx, edx
005015F8  shr     ecx, 8
005015FB  shl     eax, 5
005015FE  and     ebx, 1Fh
00501601  or      eax, ebx
00501603  mov     ebx, ecx
00501605  add     eax, eax
00501607  and     ecx, 0FF00h
0050160D  add     eax, eax
0050160F  and     edx, 7E0h
00501615  add     eax, eax
00501617  or      edx, 18h
0050161A  and     ebx, 0FF00FFh
00501620  or      eax, 70007h
00501625  add     eax, ebx
00501627  shl     edx, 5
0050162A  add     edx, ecx
0050162C  mov     ecx, eax
0050162E  shr     ecx, 18h
00501631  and     ecx, 1
00501634  imul    ecx, 0FF0000h
0050163A  mov     ebx, eax
0050163C  shr     ebx, 8
0050163F  and     ebx, 1
00501642  imul    ebx, 0FFh
00501648  or      ecx, ebx
0050164A  or      eax, ecx
0050164C  mov     ecx, edx
0050164E  shr     ecx, 10h
00501651  and     ecx, 1
00501654  imul    ecx, 0FF00h
0050165A  shr     edx, 5
0050165D  shr     ecx, 5
00501660  or      ecx, edx
00501662  mov     edx, eax
00501664  shr     edx, 3
00501667  and     ecx, 7E0h
0050166D  and     edx, 1Fh
00501670  shr     eax, 8
00501673  or      ecx, edx
00501675  and     eax, 0F800h
0050167A  or      ecx, eax
0050167C  mov     [esi], cx
0050167F  mov     ebp, [esp+0C0h+var_90]
00501683  mov     edx, [esp+0C0h+var_88]
00501687  add     ebp, [esp+0C0h+var_34]
0050168E  add     edx, [esp+0C0h+var_48]
00501692  mov     ecx, dword ptr [esp+0C0h+var_40]
00501699  add     [esp+0C0h+var_A0], 2
0050169E  mov     ebx, [esp+0C0h+var_7C]
005016A2  mov     [esp+0C0h+var_90], ebp
005016A6  mov     [esp+0C0h+var_88], edx
005016AA  test    ecx, ecx
005016AC  jg      loc_501370
005016B2  mov     esi, [esp+0C0h+var_44]
005016B6  mov     eax, [esp+0C0h+var_14]
005016BD  add     [esp+0C0h+var_84], eax
005016C1  mov     ecx, [esp+0C0h+var_98]
005016C5  mov     edx, [esp+0C0h+var_8]
005016CC  add     [esp+0C0h+var_6C], ecx
005016D0  add     [esp+0C0h+var_74], edx
005016D4  add     esi, eax
005016D6  mov     eax, dword ptr [esp+0C0h+var_28]
005016DD  add     [esp+0C0h+var_80], eax
005016E1  mov     eax, [esp+0C0h+var_18]
005016E8  add     [esp+0C0h+var_70], eax
005016EC  cmp     [esp+0C0h+var_94], 0
005016F1  mov     [esp+0C0h+var_44], esi
005016F5  jg      loc_5012C6
005016FB  pop     edi
005016FC  pop     esi
005016FD  pop     ebp
005016FE  pop     ebx
005016FF  add     esp, 0B0h
00501705  retn
