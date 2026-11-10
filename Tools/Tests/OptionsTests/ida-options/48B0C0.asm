0048B0C0  push    ebp
0048B0C1  mov     ebp, esp
0048B0C3  push    0FFFFFFFFh
0048B0C5  push    offset SEH_48B0C0
0048B0CA  mov     eax, large fs:0
0048B0D0  push    eax
0048B0D1  mov     large fs:0, esp
0048B0D8  sub     esp, 48h
0048B0DB  push    ebx
0048B0DC  push    esi
0048B0DD  push    edi
0048B0DE  mov     [ebp+var_10], esp
0048B0E1  xor     ebx, ebx
0048B0E3  mov     [ebp+var_4], ebx
0048B0E6  push    11h; SourceSize
0048B0E8  mov     esi, ecx
0048B0EA  push    offset aFontBriannetod_2; "FONT_BRIANNETOD12"
0048B0EF  lea     ecx, [ebp+var_38]
0048B0F2  mov     byte_755E36, 1
0048B0F9  mov     [ebp+var_20], 0Fh
0048B100  mov     [ebp+var_24], ebx
0048B103  mov     byte ptr [ebp+var_34], bl
0048B106  call    sub_4032C0
0048B10B  mov     byte ptr [ebp+var_4], 1
0048B10F  mov     eax, [esi]
0048B111  mov     edx, [eax+48h]
0048B114  lea     ecx, [ebp+var_38]
0048B117  push    ecx
0048B118  mov     ecx, esi
0048B11A  call    edx
0048B11C  mov     edi, 10h
0048B121  mov     byte ptr [ebp+var_4], bl
0048B124  mov     dword_75339C, eax
0048B129  cmp     [ebp+var_20], edi
0048B12C  jb      short loc_48B13A
0048B12E  mov     eax, [ebp+var_34]
0048B131  push    eax; void *
0048B132  call    ??3@YAXPAX@Z; operator delete(void *)
0048B137  add     esp, 4
0048B13A  push    14h; SourceSize
0048B13C  push    offset aFontContinuumb; "FONT_CONTINUUMBOLD14"
0048B141  lea     ecx, [ebp+var_38]
0048B144  mov     [ebp+var_20], 0Fh
0048B14B  mov     [ebp+var_24], ebx
0048B14E  mov     byte ptr [ebp+var_34], bl
0048B151  call    sub_4032C0
0048B156  mov     byte ptr [ebp+var_4], 2
0048B15A  mov     edx, [esi]
0048B15C  mov     edx, [edx+48h]
0048B15F  lea     eax, [ebp+var_38]
0048B162  push    eax
0048B163  mov     ecx, esi
0048B165  call    edx
0048B167  mov     byte ptr [ebp+var_4], bl
0048B16A  mov     dword_752D30, eax
0048B16F  cmp     [ebp+var_20], edi
0048B172  jb      short loc_48B180
0048B174  mov     eax, [ebp+var_34]
0048B177  push    eax; void *
0048B178  call    ??3@YAXPAX@Z; operator delete(void *)
0048B17D  add     esp, 4
0048B180  push    1Bh; SourceSize
0048B182  push    offset aFontContinuumb_0; "FONT_CONTINUUMBOLD14OUTLINE"
0048B187  lea     ecx, [ebp+var_38]
0048B18A  mov     [ebp+var_20], 0Fh
0048B191  mov     [ebp+var_24], ebx
0048B194  mov     byte ptr [ebp+var_34], bl
0048B197  call    sub_4032C0
0048B19C  mov     byte ptr [ebp+var_4], 3
0048B1A0  mov     edx, [esi]
0048B1A2  mov     edx, [edx+48h]
0048B1A5  lea     eax, [ebp+var_38]
0048B1A8  push    eax
0048B1A9  mov     ecx, esi
0048B1AB  call    edx
0048B1AD  mov     byte ptr [ebp+var_4], bl
0048B1B0  mov     dword_75326C, eax
0048B1B5  cmp     [ebp+var_20], edi
0048B1B8  jb      short loc_48B1C6
0048B1BA  mov     eax, [ebp+var_34]
0048B1BD  push    eax; void *
0048B1BE  call    ??3@YAXPAX@Z; operator delete(void *)
0048B1C3  add     esp, 4
0048B1C6  push    16h; SourceSize
0048B1C8  push    offset aFontDwarventod; "FONT_DWARVENTODCRAFT12"
0048B1CD  lea     ecx, [ebp+var_38]
0048B1D0  mov     [ebp+var_20], 0Fh
0048B1D7  mov     [ebp+var_24], ebx
0048B1DA  mov     byte ptr [ebp+var_34], bl
0048B1DD  call    sub_4032C0
0048B1E2  mov     byte ptr [ebp+var_4], 4
0048B1E6  mov     edx, [esi]
0048B1E8  mov     edx, [edx+48h]
0048B1EB  lea     eax, [ebp+var_38]
0048B1EE  push    eax
0048B1EF  mov     ecx, esi
0048B1F1  call    edx
0048B1F3  mov     byte ptr [ebp+var_4], bl
0048B1F6  mov     dword_753118, eax
0048B1FB  cmp     [ebp+var_20], edi
0048B1FE  jb      short loc_48B20C
0048B200  mov     eax, [ebp+var_34]
0048B203  push    eax; void *
0048B204  call    ??3@YAXPAX@Z; operator delete(void *)
0048B209  add     esp, 4
0048B20C  push    16h; SourceSize
0048B20E  push    offset aFontDwarventod_0; "FONT_DWARVENTODCRAFT15"
0048B213  lea     ecx, [ebp+var_38]
0048B216  mov     [ebp+var_20], 0Fh
0048B21D  mov     [ebp+var_24], ebx
0048B220  mov     byte ptr [ebp+var_34], bl
0048B223  call    sub_4032C0
0048B228  mov     byte ptr [ebp+var_4], 5
0048B22C  mov     edx, [esi]
0048B22E  mov     edx, [edx+48h]
0048B231  lea     eax, [ebp+var_38]
0048B234  push    eax
0048B235  mov     ecx, esi
0048B237  call    edx
0048B239  mov     byte ptr [ebp+var_4], bl
0048B23C  mov     dword_752C54, eax
0048B241  cmp     [ebp+var_20], edi
0048B244  jb      short loc_48B252
0048B246  mov     eax, [ebp+var_34]
0048B249  push    eax; void *
0048B24A  call    ??3@YAXPAX@Z; operator delete(void *)
0048B24F  add     esp, 4
0048B252  push    16h; SourceSize
0048B254  push    offset aFontDwarventod_1; "FONT_DWARVENTODCRAFT18"
0048B259  lea     ecx, [ebp+var_38]
0048B25C  mov     [ebp+var_20], 0Fh
0048B263  mov     [ebp+var_24], ebx
0048B266  mov     byte ptr [ebp+var_34], bl
0048B269  call    sub_4032C0
0048B26E  mov     byte ptr [ebp+var_4], 6
0048B272  mov     edx, [esi]
0048B274  mov     edx, [edx+48h]
0048B277  lea     eax, [ebp+var_38]
0048B27A  push    eax
0048B27B  mov     ecx, esi
0048B27D  call    edx
0048B27F  mov     byte ptr [ebp+var_4], bl
0048B282  mov     dword_752C9C, eax
0048B287  cmp     [ebp+var_20], edi
0048B28A  jb      short loc_48B298
0048B28C  mov     eax, [ebp+var_34]
0048B28F  push    eax; void *
0048B290  call    ??3@YAXPAX@Z; operator delete(void *)
0048B295  add     esp, 4
0048B298  push    26h ; '&'; SourceSize
0048B29A  push    offset aFontDwarventod_2; "FONT_DWARVENTODCRAFT18BRIGHTGREENINSET"
0048B29F  lea     ecx, [ebp+var_38]
0048B2A2  mov     [ebp+var_20], 0Fh
0048B2A9  mov     [ebp+var_24], ebx
0048B2AC  mov     byte ptr [ebp+var_34], bl
0048B2AF  call    sub_4032C0
0048B2B4  mov     byte ptr [ebp+var_4], 7
0048B2B8  mov     edx, [esi]
0048B2BA  mov     edx, [edx+48h]
0048B2BD  lea     eax, [ebp+var_38]
0048B2C0  push    eax
0048B2C1  mov     ecx, esi
0048B2C3  call    edx
0048B2C5  mov     byte ptr [ebp+var_4], bl
0048B2C8  mov     dword_752B80, eax
0048B2CD  cmp     [ebp+var_20], edi
0048B2D0  jb      short loc_48B2DE
0048B2D2  mov     eax, [ebp+var_34]
0048B2D5  push    eax; void *
0048B2D6  call    ??3@YAXPAX@Z; operator delete(void *)
0048B2DB  add     esp, 4
0048B2DE  push    20h ; ' '; SourceSize
0048B2E0  push    offset aFontDwarventod_3; "FONT_DWARVENTODCRAFT18GREENINSET"
0048B2E5  lea     ecx, [ebp+var_38]
0048B2E8  mov     [ebp+var_20], 0Fh
0048B2EF  mov     [ebp+var_24], ebx
0048B2F2  mov     byte ptr [ebp+var_34], bl
0048B2F5  call    sub_4032C0
0048B2FA  mov     byte ptr [ebp+var_4], 8
0048B2FE  mov     edx, [esi]
0048B300  mov     edx, [edx+48h]
0048B303  lea     eax, [ebp+var_38]
0048B306  push    eax
0048B307  mov     ecx, esi
0048B309  call    edx
0048B30B  mov     byte ptr [ebp+var_4], bl
0048B30E  mov     dword_753790, eax
0048B313  cmp     [ebp+var_20], edi
0048B316  jb      short loc_48B324
0048B318  mov     eax, [ebp+var_34]
0048B31B  push    eax; void *
0048B31C  call    ??3@YAXPAX@Z; operator delete(void *)
0048B321  add     esp, 4
0048B324  push    1Ch; SourceSize
0048B326  push    offset aFontDwarventod_4; "FONT_DWARVENTODCRAFT18YELLOW"
0048B32B  lea     ecx, [ebp+var_38]
0048B32E  mov     [ebp+var_20], 0Fh
0048B335  mov     [ebp+var_24], ebx
0048B338  mov     byte ptr [ebp+var_34], bl
0048B33B  call    sub_4032C0
0048B340  mov     byte ptr [ebp+var_4], 9
0048B344  mov     edx, [esi]
0048B346  mov     edx, [edx+48h]
0048B349  lea     eax, [ebp+var_38]
0048B34C  push    eax
0048B34D  mov     ecx, esi
0048B34F  call    edx
0048B351  mov     byte ptr [ebp+var_4], bl
0048B354  mov     dword_75365C, eax
0048B359  cmp     [ebp+var_20], edi
0048B35C  jb      short loc_48B36A
0048B35E  mov     eax, [ebp+var_34]
0048B361  push    eax; void *
0048B362  call    ??3@YAXPAX@Z; operator delete(void *)
0048B367  add     esp, 4
0048B36A  push    16h; SourceSize
0048B36C  push    offset aFontDwarventod_5; "FONT_DWARVENTODCRAFT24"
0048B371  lea     ecx, [ebp+var_38]
0048B374  mov     [ebp+var_20], 0Fh
0048B37B  mov     [ebp+var_24], ebx
0048B37E  mov     byte ptr [ebp+var_34], bl
0048B381  call    sub_4032C0
0048B386  mov     byte ptr [ebp+var_4], 0Ah
0048B38A  mov     edx, [esi]
0048B38C  mov     edx, [edx+48h]
0048B38F  lea     eax, [ebp+var_38]
0048B392  push    eax
0048B393  mov     ecx, esi
0048B395  call    edx
0048B397  mov     byte ptr [ebp+var_4], bl
0048B39A  mov     dword_7536C0, eax
0048B39F  cmp     [ebp+var_20], edi
0048B3A2  jb      short loc_48B3B0
0048B3A4  mov     eax, [ebp+var_34]
0048B3A7  push    eax; void *
0048B3A8  call    ??3@YAXPAX@Z; operator delete(void *)
0048B3AD  add     esp, 4
0048B3B0  push    26h ; '&'; SourceSize
0048B3B2  push    offset aFontDwarventod_6; "FONT_DWARVENTODCRAFT36BRIGHTGREENINSET"
0048B3B7  lea     ecx, [ebp+var_38]
0048B3BA  mov     [ebp+var_20], 0Fh
0048B3C1  mov     [ebp+var_24], ebx
0048B3C4  mov     byte ptr [ebp+var_34], bl
0048B3C7  call    sub_4032C0
0048B3CC  mov     byte ptr [ebp+var_4], 0Bh
0048B3D0  mov     edx, [esi]
0048B3D2  mov     edx, [edx+48h]
0048B3D5  lea     eax, [ebp+var_38]
0048B3D8  push    eax
0048B3D9  mov     ecx, esi
0048B3DB  call    edx
0048B3DD  mov     byte ptr [ebp+var_4], bl
0048B3E0  mov     dword_7530A0, eax
0048B3E5  cmp     [ebp+var_20], edi
0048B3E8  jb      short loc_48B3F6
0048B3EA  mov     eax, [ebp+var_34]
0048B3ED  push    eax; void *
0048B3EE  call    ??3@YAXPAX@Z; operator delete(void *)
0048B3F3  add     esp, 4
0048B3F6  push    20h ; ' '; SourceSize
0048B3F8  push    offset aFontDwarventod_7; "FONT_DWARVENTODCRAFT36GREENINSET"
0048B3FD  lea     ecx, [ebp+var_38]
0048B400  mov     [ebp+var_20], 0Fh
0048B407  mov     [ebp+var_24], ebx
0048B40A  mov     byte ptr [ebp+var_34], bl
0048B40D  call    sub_4032C0
0048B412  mov     byte ptr [ebp+var_4], 0Ch
0048B416  mov     edx, [esi]
0048B418  mov     edx, [edx+48h]
0048B41B  lea     eax, [ebp+var_38]
0048B41E  push    eax
0048B41F  mov     ecx, esi
0048B421  call    edx
0048B423  mov     byte ptr [ebp+var_4], bl
0048B426  mov     dword_75334C, eax
0048B42B  cmp     [ebp+var_20], edi
0048B42E  jb      short loc_48B43C
0048B430  mov     eax, [ebp+var_34]
0048B433  push    eax; void *
0048B434  call    ??3@YAXPAX@Z; operator delete(void *)
0048B439  add     esp, 4
0048B43C  push    14h; SourceSize
0048B43E  push    offset aFontHouseofter; "FONT_HOUSEOFTERROR16"
0048B443  lea     ecx, [ebp+var_38]
0048B446  mov     [ebp+var_20], 0Fh
0048B44D  mov     [ebp+var_24], ebx
0048B450  mov     byte ptr [ebp+var_34], bl
0048B453  call    sub_4032C0
0048B458  mov     byte ptr [ebp+var_4], 0Dh
0048B45C  mov     edx, [esi]
0048B45E  mov     edx, [edx+48h]
0048B461  lea     eax, [ebp+var_38]
0048B464  push    eax
0048B465  mov     ecx, esi
0048B467  call    edx
0048B469  mov     byte ptr [ebp+var_4], bl
0048B46C  mov     dword_752EE0, eax
0048B471  cmp     [ebp+var_20], edi
0048B474  jb      short loc_48B482
0048B476  mov     eax, [ebp+var_34]
0048B479  push    eax; void *
0048B47A  call    ??3@YAXPAX@Z; operator delete(void *)
0048B47F  add     esp, 4
0048B482  push    14h; SourceSize
0048B484  push    offset aFontHouseofter_0; "FONT_HOUSEOFTERROR20"
0048B489  lea     ecx, [ebp+var_38]
0048B48C  mov     [ebp+var_20], 0Fh
0048B493  mov     [ebp+var_24], ebx
0048B496  mov     byte ptr [ebp+var_34], bl
0048B499  call    sub_4032C0
0048B49E  mov     byte ptr [ebp+var_4], 0Eh
0048B4A2  mov     edx, [esi]
0048B4A4  mov     edx, [edx+48h]
0048B4A7  lea     eax, [ebp+var_38]
0048B4AA  push    eax
0048B4AB  mov     ecx, esi
0048B4AD  call    edx
0048B4AF  mov     byte ptr [ebp+var_4], bl
0048B4B2  mov     dword_753088, eax
0048B4B7  cmp     [ebp+var_20], edi
0048B4BA  jb      short loc_48B4C8
0048B4BC  mov     eax, [ebp+var_34]
0048B4BF  push    eax; void *
0048B4C0  call    ??3@YAXPAX@Z; operator delete(void *)
0048B4C5  add     esp, 4
0048B4C8  push    14h; SourceSize
0048B4CA  push    offset aFontHouseofter_1; "FONT_HOUSEOFTERROR28"
0048B4CF  lea     ecx, [ebp+var_38]
0048B4D2  mov     [ebp+var_20], 0Fh
0048B4D9  mov     [ebp+var_24], ebx
0048B4DC  mov     byte ptr [ebp+var_34], bl
0048B4DF  call    sub_4032C0
0048B4E4  mov     byte ptr [ebp+var_4], 0Fh
0048B4E8  mov     edx, [esi]
0048B4EA  mov     edx, [edx+48h]
0048B4ED  lea     eax, [ebp+var_38]
0048B4F0  push    eax
0048B4F1  mov     ecx, esi
0048B4F3  call    edx
0048B4F5  mov     byte ptr [ebp+var_4], bl
0048B4F8  mov     dword_753240, eax
0048B4FD  cmp     [ebp+var_20], edi
0048B500  jb      short loc_48B50E
0048B502  mov     eax, [ebp+var_34]
0048B505  push    eax; void *
0048B506  call    ??3@YAXPAX@Z; operator delete(void *)
0048B50B  add     esp, 4
0048B50E  push    1Ah; SourceSize
0048B510  push    offset aFontImageHouse; "FONT_IMAGE_HOUSEOFTERROR28"
0048B515  lea     ecx, [ebp+var_38]
0048B518  mov     [ebp+var_20], 0Fh
0048B51F  mov     [ebp+var_24], ebx
0048B522  mov     byte ptr [ebp+var_34], bl
0048B525  call    sub_4032C0
0048B52A  lea     eax, [ebp+var_38]
0048B52D  mov     byte ptr [ebp+var_4], 10h
0048B531  mov     edx, [esi]
0048B533  mov     edx, [edx+40h]
0048B536  push    eax
0048B537  lea     ecx, [ebp+var_1C]
0048B53A  push    ecx
0048B53B  mov     ecx, esi
0048B53D  call    edx
0048B53F  mov     ecx, eax
0048B541  call    sub_5B05A0
0048B546  lea     ecx, [ebp+var_1C]
0048B549  mov     dword_7537F8, eax
0048B54E  call    sub_5B0440
0048B553  mov     byte ptr [ebp+var_4], bl
0048B556  cmp     [ebp+var_20], edi
0048B559  jb      short loc_48B567
0048B55B  mov     eax, [ebp+var_34]
0048B55E  push    eax; void *
0048B55F  call    ??3@YAXPAX@Z; operator delete(void *)
0048B564  add     esp, 4
0048B567  push    0Ch; SourceSize
0048B569  push    offset aFontPico129; "FONT_PICO129"
0048B56E  lea     ecx, [ebp+var_38]
0048B571  mov     [ebp+var_20], 0Fh
0048B578  mov     [ebp+var_24], ebx
0048B57B  mov     byte ptr [ebp+var_34], bl
0048B57E  call    sub_4032C0
0048B583  mov     byte ptr [ebp+var_4], 11h
0048B587  mov     edx, [esi]
0048B589  mov     edx, [edx+48h]
0048B58C  lea     eax, [ebp+var_38]
0048B58F  push    eax
0048B590  mov     ecx, esi
0048B592  call    edx
0048B594  mov     byte ptr [ebp+var_4], bl
0048B597  mov     dword_752EC0, eax
0048B59C  cmp     [ebp+var_20], edi
0048B59F  jb      short loc_48B5AD
0048B5A1  mov     eax, [ebp+var_34]
0048B5A4  push    eax; void *
0048B5A5  call    ??3@YAXPAX@Z; operator delete(void *)
0048B5AA  add     esp, 4
0048B5AD  push    0Dh; SourceSize
0048B5AF  push    offset aFontTinybold; "FONT_TINYBOLD"
0048B5B4  lea     ecx, [ebp+var_54]
0048B5B7  mov     [ebp+var_3C], 0Fh
0048B5BE  mov     [ebp+var_40], ebx
0048B5C1  mov     byte ptr [ebp+var_50], bl
0048B5C4  call    sub_4032C0
0048B5C9  mov     byte ptr [ebp+var_4], 12h
0048B5CD  mov     edx, [esi]
0048B5CF  mov     edx, [edx+48h]
0048B5D2  lea     eax, [ebp+var_54]
0048B5D5  push    eax
0048B5D6  mov     ecx, esi
0048B5D8  call    edx
0048B5DA  mov     dword_752C3C, eax
0048B5DF  cmp     [ebp+var_3C], edi
0048B5E2  jb      short loc_48B5F0
0048B5E4  mov     eax, [ebp+var_50]
0048B5E7  push    eax; void *
0048B5E8  call    ??3@YAXPAX@Z; operator delete(void *)
0048B5ED  add     esp, 4
0048B5F0  mov     al, 1
0048B5F2  mov     ecx, [ebp+var_C]
0048B5F5  mov     large fs:0, ecx
0048B5FC  pop     edi
0048B5FD  pop     esi
0048B5FE  pop     ebx
0048B5FF  mov     esp, ebp
0048B601  pop     ebp
0048B602  retn
0048B603  mov     eax, offset loc_48B609
0048B608  retn
0048B609  mov     ecx, [ebp+var_C]
0048B60C  pop     edi
0048B60D  pop     esi
0048B60E  xor     al, al
0048B610  mov     large fs:0, ecx
0048B617  pop     ebx
0048B618  mov     esp, ebp
0048B61A  pop     ebp
0048B61B  retn
006E2E30  lea     ecx, [ebp+var_38]; void *
006E2E33  jmp     sub_401E90
006E2E38  lea     ecx, [ebp+var_38]; void *
006E2E3B  jmp     sub_401E90
006E2E40  lea     ecx, [ebp+var_38]; void *
006E2E43  jmp     sub_401E90
006E2E48  lea     ecx, [ebp+var_38]; void *
006E2E4B  jmp     sub_401E90
006E2E50  lea     ecx, [ebp+var_38]; void *
006E2E53  jmp     sub_401E90
006E2E58  lea     ecx, [ebp+var_38]; void *
006E2E5B  jmp     sub_401E90
006E2E60  lea     ecx, [ebp+var_38]; void *
006E2E63  jmp     sub_401E90
006E2E68  lea     ecx, [ebp+var_38]; void *
006E2E6B  jmp     sub_401E90
006E2E70  lea     ecx, [ebp+var_38]; void *
006E2E73  jmp     sub_401E90
006E2E78  lea     ecx, [ebp+var_38]; void *
006E2E7B  jmp     sub_401E90
006E2E80  lea     ecx, [ebp+var_38]; void *
006E2E83  jmp     sub_401E90
006E2E88  lea     ecx, [ebp+var_38]; void *
006E2E8B  jmp     sub_401E90
006E2E90  lea     ecx, [ebp+var_38]; void *
006E2E93  jmp     sub_401E90
006E2E98  lea     ecx, [ebp+var_38]; void *
006E2E9B  jmp     sub_401E90
006E2EA0  lea     ecx, [ebp+var_38]; void *
006E2EA3  jmp     sub_401E90
006E2EA8  lea     ecx, [ebp+var_38]; void *
006E2EAB  jmp     sub_401E90
006E2EB0  lea     ecx, [ebp+var_38]; void *
006E2EB3  jmp     sub_401E90
006E2EB8  lea     ecx, [ebp+var_54]; void *
006E2EBB  jmp     sub_401E90
006E2EC0  mov     eax, offset stru_7353CC
006E2EC5  jmp     ___CxxFrameHandler3
