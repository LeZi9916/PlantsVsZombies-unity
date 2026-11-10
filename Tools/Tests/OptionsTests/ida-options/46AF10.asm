0046AF10  push    ebp
0046AF11  mov     ebp, esp
0046AF13  and     esp, 0FFFFFFF8h
0046AF16  mov     eax, large fs:0
0046AF1C  push    0FFFFFFFFh
0046AF1E  push    offset SEH_46AF10
0046AF23  push    eax
0046AF24  mov     large fs:0, esp
0046AF2B  mov     eax, dword_752CC8
0046AF30  sub     esp, 50h
0046AF33  push    ebx
0046AF34  push    esi
0046AF35  push    edi
0046AF36  mov     edi, [ebp+arg_0]
0046AF39  xor     ebx, ebx
0046AF3B  push    ebx; int
0046AF3C  push    ebx; int
0046AF3D  mov     esi, ecx
0046AF3F  push    eax; Concurrency::details::ExecutionResource *
0046AF40  mov     ecx, edi
0046AF42  call    sub_592F90
0046AF47  cmp     [esi+194h], bl
0046AF4D  jz      short loc_46AF77
0046AF4F  mov     dword ptr [esi+19Ch], 5
0046AF59  mov     dword ptr [esi+1A0h], 0Ah
0046AF63  mov     dword ptr [esi+1A4h], 0Fh
0046AF6D  mov     dword ptr [esi+1A8h], 14h
0046AF77  push    91h
0046AF7C  push    6Dh ; 'm'
0046AF7E  push    6Bh ; 'k'
0046AF80  lea     ecx, [esp+74h+var_58]
0046AF84  call    sub_55BBB0
0046AF89  xor     ecx, ecx
0046AF8B  push    15h; int
0046AF8D  mov     word ptr [esp+6Ch+var_44], cx
0046AF92  push    offset off_7112EC; Source
0046AF97  lea     ecx, [esp+70h+var_48]
0046AF9B  mov     [esp+70h+var_30], 7
0046AFA3  mov     [esp+70h+var_34], ebx
0046AFA7  call    sub_40D1E0
0046AFAC  mov     [esp+68h+var_4], ebx
0046AFB0  mov     edx, [esp+68h+var_58]
0046AFB4  mov     ecx, [esp+68h+var_54]
0046AFB8  sub     esp, 10h
0046AFBB  mov     eax, esp
0046AFBD  mov     [eax], edx
0046AFBF  mov     edx, [esp+78h+var_50]
0046AFC3  mov     [eax+4], ecx
0046AFC6  mov     ecx, [esp+78h+var_4C]
0046AFCA  mov     [eax+8], edx
0046AFCD  mov     edx, dword_752C9C
0046AFD3  mov     [eax+0Ch], ecx
0046AFD6  mov     eax, [esi+19Ch]
0046AFDC  push    1
0046AFDE  push    edx
0046AFDF  add     eax, 8Ch
0046AFE4  push    eax
0046AFE5  push    0BAh
0046AFEA  lea     ecx, [esp+88h+var_48]
0046AFEE  push    ecx
0046AFEF  push    edi
0046AFF0  call    sub_526FC0
0046AFF5  add     esp, 28h
0046AFF8  mov     [esp+68h+var_4], 0FFFFFFFFh
0046B000  cmp     [esp+68h+var_30], 8
0046B005  jb      short loc_46B014
0046B007  mov     edx, [esp+68h+var_44]
0046B00B  push    edx; void *
0046B00C  call    ??3@YAXPAX@Z; operator delete(void *)
0046B011  add     esp, 4
0046B014  push    11h; int
0046B016  xor     eax, eax
0046B018  push    offset off_711318; Source
0046B01D  lea     ecx, [esp+70h+var_48]
0046B021  mov     [esp+70h+var_30], 7
0046B029  mov     [esp+70h+var_34], ebx
0046B02D  mov     word ptr [esp+70h+var_44], ax
0046B032  call    sub_40D1E0
0046B037  mov     [esp+68h+var_4], 1
0046B03F  mov     ecx, [esp+68h+var_58]
0046B043  mov     edx, [esp+68h+var_54]
0046B047  sub     esp, 10h
0046B04A  mov     eax, esp
0046B04C  mov     [eax], ecx
0046B04E  mov     ecx, [esp+78h+var_50]
0046B052  mov     [eax+4], edx
0046B055  mov     edx, [esp+78h+var_4C]
0046B059  mov     [eax+8], ecx
0046B05C  mov     ecx, [esi+1A0h]
0046B062  push    1
0046B064  mov     [eax+0Ch], edx
0046B067  mov     eax, dword_752C9C
0046B06C  push    eax
0046B06D  add     ecx, 0A7h
0046B073  push    ecx
0046B074  push    0BAh
0046B079  lea     edx, [esp+88h+var_48]
0046B07D  push    edx
0046B07E  push    edi
0046B07F  call    sub_526FC0
0046B084  add     esp, 28h
0046B087  mov     [esp+68h+var_4], 0FFFFFFFFh
0046B08F  cmp     [esp+68h+var_30], 8
0046B094  jb      short loc_46B0A3
0046B096  mov     eax, [esp+68h+var_44]
0046B09A  push    eax; void *
0046B09B  call    ??3@YAXPAX@Z; operator delete(void *)
0046B0A0  add     esp, 4
0046B0A3  xor     ecx, ecx
0046B0A5  push    19h; int
0046B0A7  mov     word ptr [esp+6Ch+var_44], cx
0046B0AC  push    offset off_71133C; Source
0046B0B1  lea     ecx, [esp+70h+var_48]
0046B0B5  mov     [esp+70h+var_30], 7
0046B0BD  mov     [esp+70h+var_34], ebx
0046B0C1  call    sub_40D1E0
0046B0C6  mov     [esp+68h+var_4], 2
0046B0CE  mov     edx, [esp+68h+var_58]
0046B0D2  mov     ecx, [esp+68h+var_54]
0046B0D6  sub     esp, 10h
0046B0D9  mov     eax, esp
0046B0DB  mov     [eax], edx
0046B0DD  mov     edx, [esp+78h+var_50]
0046B0E1  mov     [eax+4], ecx
0046B0E4  mov     ecx, [esp+78h+var_4C]
0046B0E8  mov     [eax+8], edx
0046B0EB  mov     edx, dword_752C9C
0046B0F1  mov     [eax+0Ch], ecx
0046B0F4  mov     eax, [esi+1A4h]
0046B0FA  push    1
0046B0FC  push    edx
0046B0FD  add     eax, 0C5h
0046B102  push    eax
0046B103  push    112h
0046B108  lea     ecx, [esp+88h+var_48]
0046B10C  push    ecx
0046B10D  push    edi
0046B10E  call    sub_526FC0
0046B113  add     esp, 28h
0046B116  mov     [esp+68h+var_4], 0FFFFFFFFh
0046B11E  cmp     [esp+68h+var_30], 8
0046B123  jb      short loc_46B132
0046B125  mov     edx, [esp+68h+var_44]
0046B129  push    edx; void *
0046B12A  call    ??3@YAXPAX@Z; operator delete(void *)
0046B12F  add     esp, 4
0046B132  push    15h; int
0046B134  xor     eax, eax
0046B136  push    offset off_711370; Source
0046B13B  lea     ecx, [esp+70h+var_2C]
0046B13F  mov     [esp+70h+var_14], 7
0046B147  mov     [esp+70h+var_18], ebx
0046B14B  mov     word ptr [esp+70h+var_28], ax
0046B150  call    sub_40D1E0
0046B155  mov     [esp+68h+var_4], 3
0046B15D  mov     ecx, [esp+68h+var_58]
0046B161  mov     edx, [esp+68h+var_54]
0046B165  sub     esp, 10h
0046B168  mov     eax, esp
0046B16A  mov     [eax], ecx
0046B16C  mov     ecx, [esp+78h+var_50]
0046B170  mov     [eax+4], edx
0046B173  mov     edx, [esp+78h+var_4C]
0046B177  mov     [eax+8], ecx
0046B17A  mov     ecx, [esi+1A8h]
0046B180  push    1
0046B182  mov     [eax+0Ch], edx
0046B185  mov     eax, dword_752C9C
0046B18A  push    eax
0046B18B  add     ecx, 0E5h
0046B191  push    ecx
0046B192  push    112h
0046B197  lea     edx, [esp+88h+var_2C]
0046B19B  push    edx
0046B19C  push    edi
0046B19D  call    sub_526FC0
0046B1A2  add     esp, 28h
0046B1A5  cmp     [esp+68h+var_14], 8
0046B1AA  jb      short loc_46B1B9
0046B1AC  mov     eax, [esp+68h+var_28]
0046B1B0  push    eax; void *
0046B1B1  call    ??3@YAXPAX@Z; operator delete(void *)
0046B1B6  add     esp, 4
0046B1B9  mov     ecx, [esp+68h+var_C]
0046B1BD  pop     edi
0046B1BE  pop     esi
0046B1BF  mov     large fs:0, ecx
0046B1C6  pop     ebx
0046B1C7  mov     esp, ebp
0046B1C9  pop     ebp
0046B1CA  retn    4
006DFBB0  lea     ecx, [ebp+var_48]; void *
006DFBB3  jmp     sub_401330
006DFBB8  lea     ecx, [ebp+var_48]; void *
006DFBBB  jmp     sub_401330
006DFBC0  lea     ecx, [ebp+var_48]; void *
006DFBC3  jmp     sub_401330
006DFBC8  lea     ecx, [ebp+var_2C]; void *
006DFBCB  jmp     sub_401330
006DFBD0  mov     eax, offset stru_73210C
006DFBD5  jmp     ___CxxFrameHandler3
