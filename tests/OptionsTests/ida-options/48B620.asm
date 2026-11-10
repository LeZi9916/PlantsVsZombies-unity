0048B620  push    ebp
0048B621  lea     ebp, [esp-70h]
0048B625  sub     esp, 70h
0048B628  push    0FFFFFFFFh
0048B62A  push    offset SEH_48B620
0048B62F  mov     eax, large fs:0
0048B635  push    eax
0048B636  mov     large fs:0, esp
0048B63D  push    ecx
0048B63E  mov     eax, 3960h
0048B643  call    __alloca_probe
0048B648  push    ebx
0048B649  push    esi
0048B64A  push    edi
0048B64B  mov     [ebp+70h+var_80], esp
0048B64E  xor     eax, eax
0048B650  mov     [ebp+70h+var_74], eax
0048B653  push    0Dh; SourceSize
0048B655  mov     esi, ecx
0048B657  mov     ebx, 0Fh
0048B65C  push    offset aImageAlmanac; "IMAGE_ALMANAC"
0048B661  lea     ecx, [ebp+70h+var_38]
0048B664  mov     byte_755E36, 1
0048B66B  mov     [ebp+70h+var_20], ebx
0048B66E  mov     [ebp+70h+var_24], eax
0048B671  mov     byte ptr [ebp+70h+var_34], al
0048B674  call    sub_4032C0
0048B679  mov     byte ptr [ebp+70h+var_74], 1
0048B67D  mov     eax, [esi]
0048B67F  mov     eax, [eax+40h]
0048B682  lea     ecx, [ebp+70h+var_38]
0048B685  push    ecx
0048B686  lea     edx, [ebp+70h+var_1284]
0048B68C  push    edx
0048B68D  mov     ecx, esi
0048B68F  call    eax
0048B691  mov     ecx, eax
0048B693  call    sub_5B05A0
0048B698  lea     ecx, [ebp+70h+var_1284]
0048B69E  mov     dword_75353C, eax
0048B6A3  call    sub_5B0440
0048B6A8  mov     edi, 10h
0048B6AD  mov     byte ptr [ebp+70h+var_74], 0
0048B6B1  cmp     [ebp+70h+var_20], edi
0048B6B4  jb      short loc_48B6C2
0048B6B6  mov     ecx, [ebp+70h+var_34]
0048B6B9  push    ecx; void *
0048B6BA  call    ??3@YAXPAX@Z; operator delete(void *)
0048B6BF  add     esp, 4
0048B6C2  xor     eax, eax
0048B6C4  push    16h; SourceSize
0048B6C6  push    offset aImageAlmanacIm; "IMAGE_ALMANAC_IMITATER"
0048B6CB  lea     ecx, [ebp+70h+var_1C]
0048B6CE  mov     [ebp+70h+var_20], ebx
0048B6D1  mov     [ebp+70h+var_24], eax
0048B6D4  mov     byte ptr [ebp+70h+var_34], al
0048B6D7  mov     [ebp+70h+var_4], ebx
0048B6DA  mov     [ebp+70h+var_8], eax
0048B6DD  mov     byte ptr [ebp+70h+var_18], al
0048B6E0  call    sub_4032C0
0048B6E5  lea     eax, [ebp+70h+var_1C]
0048B6E8  mov     byte ptr [ebp+70h+var_74], 2
0048B6EC  mov     edx, [esi]
0048B6EE  mov     edx, [edx+40h]
0048B6F1  push    eax
0048B6F2  lea     ecx, [ebp+70h+var_129C]
0048B6F8  push    ecx
0048B6F9  mov     ecx, esi
0048B6FB  call    edx
0048B6FD  mov     ecx, eax
0048B6FF  call    sub_5B05A0
0048B704  lea     ecx, [ebp+70h+var_129C]
0048B70A  mov     dword_753180, eax
0048B70F  call    sub_5B0440
0048B714  mov     byte ptr [ebp+70h+var_74], 0
0048B718  cmp     [ebp+70h+var_4], edi
0048B71B  jb      short loc_48B729
0048B71D  mov     eax, [ebp+70h+var_18]
0048B720  push    eax; void *
0048B721  call    ??3@YAXPAX@Z; operator delete(void *)
0048B726  add     esp, 4
0048B729  push    offset aImageAwardpick; "IMAGE_AWARDPICKUPGLOW"
0048B72E  lea     ecx, [ebp+70h+var_438]
0048B734  mov     [ebp+70h+var_4], ebx
0048B737  mov     [ebp+70h+var_8], 0
0048B73E  mov     byte ptr [ebp+70h+var_18], 0
0048B742  call    sub_4033F0
0048B747  lea     eax, [ebp+70h+var_438]
0048B74D  mov     byte ptr [ebp+70h+var_74], 3
0048B751  mov     edx, [esi]
0048B753  mov     edx, [edx+40h]
0048B756  push    eax
0048B757  lea     ecx, [ebp+70h+var_12B4]
0048B75D  push    ecx
0048B75E  mov     ecx, esi
0048B760  call    edx
0048B762  mov     ecx, eax
0048B764  call    sub_5B05A0
0048B769  lea     ecx, [ebp+70h+var_12B4]
0048B76F  mov     dword_752DF0, eax
0048B774  call    sub_5B0440
0048B779  mov     byte ptr [ebp+70h+var_74], 0
0048B77D  cmp     [ebp+70h+var_420], edi
0048B783  jb      short loc_48B794
0048B785  mov     eax, [ebp+70h+var_434]
0048B78B  push    eax; void *
0048B78C  call    ??3@YAXPAX@Z; operator delete(void *)
0048B791  add     esp, 4
0048B794  push    offset aImageBeghouled; "IMAGE_BEGHOULED_TWIST_OVERLAY"
0048B799  lea     ecx, [ebp+70h+var_B8]
0048B79C  mov     [ebp+70h+var_420], ebx
0048B7A2  mov     [ebp+70h+var_424], 0
0048B7AC  mov     byte ptr [ebp+70h+var_434], 0
0048B7B3  call    sub_4033F0
0048B7B8  lea     eax, [ebp+70h+var_B8]
0048B7BB  mov     byte ptr [ebp+70h+var_74], 4
0048B7BF  mov     edx, [esi]
0048B7C1  mov     edx, [edx+40h]
0048B7C4  push    eax
0048B7C5  lea     ecx, [ebp+70h+var_12CC]
0048B7CB  push    ecx
0048B7CC  mov     ecx, esi
0048B7CE  call    edx
0048B7D0  mov     ecx, eax
0048B7D2  call    sub_5B05A0
0048B7D7  lea     ecx, [ebp+70h+var_12CC]
0048B7DD  mov     dword_753670, eax
0048B7E2  call    sub_5B0440
0048B7E7  mov     byte ptr [ebp+70h+var_74], 0
0048B7EB  cmp     [ebp+70h+var_A0], edi
0048B7EE  jb      short loc_48B7FC
0048B7F0  mov     eax, [ebp+70h+var_B4]
0048B7F3  push    eax; void *
0048B7F4  call    ??3@YAXPAX@Z; operator delete(void *)
0048B7F9  add     esp, 4
0048B7FC  push    offset aImageBrain; "IMAGE_BRAIN"
0048B801  lea     ecx, [ebp+70h+var_7B8]
0048B807  mov     [ebp+70h+var_A0], ebx
0048B80A  mov     [ebp+70h+var_A4], 0
0048B811  mov     byte ptr [ebp+70h+var_B4], 0
0048B815  call    sub_4033F0
0048B81A  lea     eax, [ebp+70h+var_7B8]
0048B820  mov     byte ptr [ebp+70h+var_74], 5
0048B824  mov     edx, [esi]
0048B826  mov     edx, [edx+40h]
0048B829  push    eax
0048B82A  lea     ecx, [ebp+70h+var_12E4]
0048B830  push    ecx
0048B831  mov     ecx, esi
0048B833  call    edx
0048B835  mov     ecx, eax
0048B837  call    sub_5B05A0
0048B83C  lea     ecx, [ebp+70h+var_12E4]
0048B842  mov     dword_753440, eax
0048B847  call    sub_5B0440
0048B84C  mov     byte ptr [ebp+70h+var_74], 0
0048B850  cmp     [ebp+70h+var_7A0], edi
0048B856  jb      short loc_48B867
0048B858  mov     eax, [ebp+70h+var_7B4]
0048B85E  push    eax; void *
0048B85F  call    ??3@YAXPAX@Z; operator delete(void *)
0048B864  add     esp, 4
0048B867  push    offset aImageBugSpray; "IMAGE_BUG_SPRAY"
0048B86C  lea     ecx, [ebp+70h+var_F0]
0048B86F  mov     [ebp+70h+var_7A0], ebx
0048B875  mov     [ebp+70h+var_7A4], 0
0048B87F  mov     byte ptr [ebp+70h+var_7B4], 0
0048B886  call    sub_4033F0
0048B88B  lea     eax, [ebp+70h+var_F0]
0048B88E  mov     byte ptr [ebp+70h+var_74], 6
0048B892  mov     edx, [esi]
0048B894  mov     edx, [edx+40h]
0048B897  push    eax
0048B898  lea     ecx, [ebp+70h+var_12FC]
0048B89E  push    ecx
0048B89F  mov     ecx, esi
0048B8A1  call    edx
0048B8A3  mov     ecx, eax
0048B8A5  call    sub_5B05A0
0048B8AA  lea     ecx, [ebp+70h+var_12FC]
0048B8B0  mov     dword_752E6C, eax
0048B8B5  call    sub_5B0440
0048B8BA  mov     byte ptr [ebp+70h+var_74], 0
0048B8BE  cmp     [ebp+70h+var_D8], edi
0048B8C1  jb      short loc_48B8CF
0048B8C3  mov     eax, [ebp+70h+var_EC]
0048B8C6  push    eax; void *
0048B8C7  call    ??3@YAXPAX@Z; operator delete(void *)
0048B8CC  add     esp, 4
0048B8CF  push    offset aImageBungeecor; "IMAGE_BUNGEECORD"
0048B8D4  lea     ecx, [ebp+70h+var_470]
0048B8DA  mov     [ebp+70h+var_D8], ebx
0048B8DD  mov     [ebp+70h+var_DC], 0
0048B8E4  mov     byte ptr [ebp+70h+var_EC], 0
0048B8E8  call    sub_4033F0
0048B8ED  lea     eax, [ebp+70h+var_470]
0048B8F3  mov     byte ptr [ebp+70h+var_74], 7
0048B8F7  mov     edx, [esi]
0048B8F9  mov     edx, [edx+40h]
0048B8FC  push    eax
0048B8FD  lea     ecx, [ebp+70h+var_1314]
0048B903  push    ecx
0048B904  mov     ecx, esi
0048B906  call    edx
0048B908  mov     ecx, eax
0048B90A  call    sub_5B05A0
0048B90F  lea     ecx, [ebp+70h+var_1314]
0048B915  mov     dword_753408, eax
0048B91A  call    sub_5B0440
0048B91F  mov     byte ptr [ebp+70h+var_74], 0
0048B923  cmp     [ebp+70h+var_458], edi
0048B929  jb      short loc_48B93A
0048B92B  mov     eax, [ebp+70h+var_46C]
0048B931  push    eax; void *
0048B932  call    ??3@YAXPAX@Z; operator delete(void *)
0048B937  add     esp, 4
0048B93A  push    offset aImageBungeetar; "IMAGE_BUNGEETARGET"
0048B93F  lea     ecx, [ebp+70h+var_128]
0048B945  mov     [ebp+70h+var_458], ebx
0048B94B  mov     [ebp+70h+var_45C], 0
0048B955  mov     byte ptr [ebp+70h+var_46C], 0
0048B95C  call    sub_4033F0
0048B961  lea     eax, [ebp+70h+var_128]
0048B967  mov     byte ptr [ebp+70h+var_74], 8
0048B96B  mov     edx, [esi]
0048B96D  mov     edx, [edx+40h]
0048B970  push    eax
0048B971  lea     ecx, [ebp+70h+var_132C]
0048B977  push    ecx
0048B978  mov     ecx, esi
0048B97A  call    edx
0048B97C  mov     ecx, eax
0048B97E  call    sub_5B05A0
0048B983  lea     ecx, [ebp+70h+var_132C]
0048B989  mov     dword_753680, eax
0048B98E  call    sub_5B0440
0048B993  mov     byte ptr [ebp+70h+var_74], 0
0048B997  cmp     [ebp+70h+var_110], edi
0048B99D  jb      short loc_48B9AE
0048B99F  mov     eax, [ebp+70h+var_124]
0048B9A5  push    eax; void *
0048B9A6  call    ??3@YAXPAX@Z; operator delete(void *)
0048B9AB  add     esp, 4
0048B9AE  push    offset aImageButtonDow; "IMAGE_BUTTON_DOWN_LEFT"
0048B9B3  lea     ecx, [ebp+70h+var_630]
0048B9B9  mov     [ebp+70h+var_110], ebx
0048B9BF  mov     [ebp+70h+var_114], 0
0048B9C9  mov     byte ptr [ebp+70h+var_124], 0
0048B9D0  call    sub_4033F0
0048B9D5  lea     eax, [ebp+70h+var_630]
0048B9DB  mov     byte ptr [ebp+70h+var_74], 9
0048B9DF  mov     edx, [esi]
0048B9E1  mov     edx, [edx+40h]
0048B9E4  push    eax
0048B9E5  lea     ecx, [ebp+70h+var_1344]
0048B9EB  push    ecx
0048B9EC  mov     ecx, esi
0048B9EE  call    edx
0048B9F0  mov     ecx, eax
0048B9F2  call    sub_5B05A0
0048B9F7  lea     ecx, [ebp+70h+var_1344]
0048B9FD  mov     dword_752D5C, eax
0048BA02  call    sub_5B0440
0048BA07  mov     byte ptr [ebp+70h+var_74], 0
0048BA0B  cmp     [ebp+70h+var_618], edi
0048BA11  jb      short loc_48BA22
0048BA13  mov     eax, [ebp+70h+var_62C]
0048BA19  push    eax; void *
0048BA1A  call    ??3@YAXPAX@Z; operator delete(void *)
0048BA1F  add     esp, 4
0048BA22  push    offset aImageButtonDow_0; "IMAGE_BUTTON_DOWN_MIDDLE"
0048BA27  lea     ecx, [ebp+70h+var_160]
0048BA2D  mov     [ebp+70h+var_618], ebx
0048BA33  mov     [ebp+70h+var_61C], 0
0048BA3D  mov     byte ptr [ebp+70h+var_62C], 0
0048BA44  call    sub_4033F0
0048BA49  lea     eax, [ebp+70h+var_160]
0048BA4F  mov     byte ptr [ebp+70h+var_74], 0Ah
0048BA53  mov     edx, [esi]
0048BA55  mov     edx, [edx+40h]
0048BA58  push    eax
0048BA59  lea     ecx, [ebp+70h+var_135C]
0048BA5F  push    ecx
0048BA60  mov     ecx, esi
0048BA62  call    edx
0048BA64  mov     ecx, eax
0048BA66  call    sub_5B05A0
0048BA6B  lea     ecx, [ebp+70h+var_135C]
0048BA71  mov     dword_7535C4, eax
0048BA76  call    sub_5B0440
0048BA7B  mov     byte ptr [ebp+70h+var_74], 0
0048BA7F  cmp     [ebp+70h+var_148], edi
0048BA85  jb      short loc_48BA96
0048BA87  mov     eax, [ebp+70h+var_15C]
0048BA8D  push    eax; void *
0048BA8E  call    ??3@YAXPAX@Z; operator delete(void *)
0048BA93  add     esp, 4
0048BA96  push    offset aImageButtonDow_1; "IMAGE_BUTTON_DOWN_RIGHT"
0048BA9B  lea     ecx, [ebp+70h+var_4A8]
0048BAA1  mov     [ebp+70h+var_148], ebx
0048BAA7  mov     [ebp+70h+var_14C], 0
0048BAB1  mov     byte ptr [ebp+70h+var_15C], 0
0048BAB8  call    sub_4033F0
0048BABD  lea     eax, [ebp+70h+var_4A8]
0048BAC3  mov     byte ptr [ebp+70h+var_74], 0Bh
0048BAC7  mov     edx, [esi]
0048BAC9  mov     edx, [edx+40h]
0048BACC  push    eax
0048BACD  lea     ecx, [ebp+70h+var_1374]
0048BAD3  push    ecx
0048BAD4  mov     ecx, esi
0048BAD6  call    edx
0048BAD8  mov     ecx, eax
0048BADA  call    sub_5B05A0
0048BADF  lea     ecx, [ebp+70h+var_1374]
0048BAE5  mov     dword_753484, eax
0048BAEA  call    sub_5B0440
0048BAEF  mov     byte ptr [ebp+70h+var_74], 0
0048BAF3  cmp     [ebp+70h+var_490], edi
0048BAF9  jb      short loc_48BB0A
0048BAFB  mov     eax, [ebp+70h+var_4A4]
0048BB01  push    eax; void *
0048BB02  call    ??3@YAXPAX@Z; operator delete(void *)
0048BB07  add     esp, 4
0048BB0A  push    offset aImageButtonLef; "IMAGE_BUTTON_LEFT"
0048BB0F  lea     ecx, [ebp+70h+var_198]
0048BB15  mov     [ebp+70h+var_490], ebx
0048BB1B  mov     [ebp+70h+var_494], 0
0048BB25  mov     byte ptr [ebp+70h+var_4A4], 0
0048BB2C  call    sub_4033F0
0048BB31  lea     eax, [ebp+70h+var_198]
0048BB37  mov     byte ptr [ebp+70h+var_74], 0Ch
0048BB3B  mov     edx, [esi]
0048BB3D  mov     edx, [edx+40h]
0048BB40  push    eax
0048BB41  lea     ecx, [ebp+70h+var_138C]
0048BB47  push    ecx
0048BB48  mov     ecx, esi
0048BB4A  call    edx
0048BB4C  mov     ecx, eax
0048BB4E  call    sub_5B05A0
0048BB53  lea     ecx, [ebp+70h+var_138C]
0048BB59  mov     dword_752BF4, eax
0048BB5E  call    sub_5B0440
0048BB63  mov     byte ptr [ebp+70h+var_74], 0
0048BB67  cmp     [ebp+70h+var_180], edi
0048BB6D  jb      short loc_48BB7E
0048BB6F  mov     eax, [ebp+70h+var_194]
0048BB75  push    eax; void *
0048BB76  call    ??3@YAXPAX@Z; operator delete(void *)
0048BB7B  add     esp, 4
0048BB7E  push    offset aImageButtonMid; "IMAGE_BUTTON_MIDDLE"
0048BB83  lea     ecx, [ebp+70h+var_710]
0048BB89  mov     [ebp+70h+var_180], ebx
0048BB8F  mov     [ebp+70h+var_184], 0
0048BB99  mov     byte ptr [ebp+70h+var_194], 0
0048BBA0  call    sub_4033F0
0048BBA5  lea     eax, [ebp+70h+var_710]
0048BBAB  mov     byte ptr [ebp+70h+var_74], 0Dh
0048BBAF  mov     edx, [esi]
0048BBB1  mov     edx, [edx+40h]
0048BBB4  push    eax
0048BBB5  lea     ecx, [ebp+70h+var_13A4]
0048BBBB  push    ecx
0048BBBC  mov     ecx, esi
0048BBBE  call    edx
0048BBC0  mov     ecx, eax
0048BBC2  call    sub_5B05A0
0048BBC7  lea     ecx, [ebp+70h+var_13A4]
0048BBCD  mov     dword_7534EC, eax
0048BBD2  call    sub_5B0440
0048BBD7  mov     byte ptr [ebp+70h+var_74], 0
0048BBDB  cmp     [ebp+70h+var_6F8], edi
0048BBE1  jb      short loc_48BBF2
0048BBE3  mov     eax, [ebp+70h+var_70C]
0048BBE9  push    eax; void *
0048BBEA  call    ??3@YAXPAX@Z; operator delete(void *)
0048BBEF  add     esp, 4
0048BBF2  push    offset aImageButtonRig; "IMAGE_BUTTON_RIGHT"
0048BBF7  lea     ecx, [ebp+70h+var_1D0]
0048BBFD  mov     [ebp+70h+var_6F8], ebx
0048BC03  mov     [ebp+70h+var_6FC], 0
0048BC0D  mov     byte ptr [ebp+70h+var_70C], 0
0048BC14  call    sub_4033F0
0048BC19  lea     eax, [ebp+70h+var_1D0]
0048BC1F  mov     byte ptr [ebp+70h+var_74], 0Eh
0048BC23  mov     edx, [esi]
0048BC25  mov     edx, [edx+40h]
0048BC28  push    eax
0048BC29  lea     ecx, [ebp+70h+var_13BC]
0048BC2F  push    ecx
0048BC30  mov     ecx, esi
0048BC32  call    edx
0048BC34  mov     ecx, eax
0048BC36  call    sub_5B05A0
0048BC3B  lea     ecx, [ebp+70h+var_13BC]
0048BC41  mov     dword_752B94, eax
0048BC46  call    sub_5B0440
0048BC4B  mov     byte ptr [ebp+70h+var_74], 0
0048BC4F  cmp     [ebp+70h+var_1B8], edi
0048BC55  jb      short loc_48BC66
0048BC57  mov     eax, [ebp+70h+var_1CC]
0048BC5D  push    eax; void *
0048BC5E  call    ??3@YAXPAX@Z; operator delete(void *)
0048BC63  add     esp, 4
0048BC66  push    offset aImageCarkeys; "IMAGE_CARKEYS"
0048BC6B  lea     ecx, [ebp+70h+var_4E0]
0048BC71  mov     [ebp+70h+var_1B8], ebx
0048BC77  mov     [ebp+70h+var_1BC], 0
0048BC81  mov     byte ptr [ebp+70h+var_1CC], 0
0048BC88  call    sub_4033F0
0048BC8D  lea     eax, [ebp+70h+var_4E0]
0048BC93  mov     byte ptr [ebp+70h+var_74], bl
0048BC96  mov     edx, [esi]
0048BC98  mov     edx, [edx+40h]
0048BC9B  push    eax
0048BC9C  lea     ecx, [ebp+70h+var_13D4]
0048BCA2  push    ecx
0048BCA3  mov     ecx, esi
0048BCA5  call    edx
0048BCA7  mov     ecx, eax
0048BCA9  call    sub_5B05A0
0048BCAE  lea     ecx, [ebp+70h+var_13D4]
0048BCB4  mov     dword_75354C, eax
0048BCB9  call    sub_5B0440
0048BCBE  mov     byte ptr [ebp+70h+var_74], 0
0048BCC2  cmp     [ebp+70h+var_4C8], edi
0048BCC8  jb      short loc_48BCD9
0048BCCA  mov     eax, [ebp+70h+var_4DC]
0048BCD0  push    eax; void *
0048BCD1  call    ??3@YAXPAX@Z; operator delete(void *)
0048BCD6  add     esp, 4
0048BCD9  push    offset aImageChocolate; "IMAGE_CHOCOLATE"
0048BCDE  lea     ecx, [ebp+70h+var_208]
0048BCE4  mov     [ebp+70h+var_4C8], ebx
0048BCEA  mov     [ebp+70h+var_4CC], 0
0048BCF4  mov     byte ptr [ebp+70h+var_4DC], 0
0048BCFB  call    sub_4033F0
0048BD00  lea     eax, [ebp+70h+var_208]
0048BD06  mov     byte ptr [ebp+70h+var_74], 10h
0048BD0A  mov     edx, [esi]
0048BD0C  mov     edx, [edx+40h]
0048BD0F  push    eax
0048BD10  lea     ecx, [ebp+70h+var_13EC]
0048BD16  push    ecx
0048BD17  mov     ecx, esi
0048BD19  call    edx
0048BD1B  mov     ecx, eax
0048BD1D  call    sub_5B05A0
0048BD22  lea     ecx, [ebp+70h+var_13EC]
0048BD28  mov     dword_752FB0, eax
0048BD2D  call    sub_5B0440
0048BD32  mov     byte ptr [ebp+70h+var_74], 0
0048BD36  cmp     [ebp+70h+var_1F0], edi
0048BD3C  jb      short loc_48BD4D
0048BD3E  mov     eax, [ebp+70h+var_204]
0048BD44  push    eax; void *
0048BD45  call    ??3@YAXPAX@Z; operator delete(void *)
0048BD4A  add     esp, 4
0048BD4D  push    offset aImageCobcannon; "IMAGE_COBCANNON_POPCORN"
0048BD52  lea     ecx, [ebp+70h+var_668]
0048BD58  mov     [ebp+70h+var_1F0], ebx
0048BD5E  mov     [ebp+70h+var_1F4], 0
0048BD68  mov     byte ptr [ebp+70h+var_204], 0
0048BD6F  call    sub_4033F0
0048BD74  lea     eax, [ebp+70h+var_668]
0048BD7A  mov     byte ptr [ebp+70h+var_74], 11h
0048BD7E  mov     edx, [esi]
0048BD80  mov     edx, [edx+40h]
0048BD83  push    eax
0048BD84  lea     ecx, [ebp+70h+var_1404]
0048BD8A  push    ecx
0048BD8B  mov     ecx, esi
0048BD8D  call    edx
0048BD8F  mov     ecx, eax
0048BD91  call    sub_5B05A0
0048BD96  lea     ecx, [ebp+70h+var_1404]
0048BD9C  mov     dword_752B60, eax
0048BDA1  call    sub_5B0440
0048BDA6  mov     byte ptr [ebp+70h+var_74], 0
0048BDAA  cmp     [ebp+70h+var_650], edi
0048BDB0  jb      short loc_48BDC1
0048BDB2  mov     eax, [ebp+70h+var_664]
0048BDB8  push    eax; void *
0048BDB9  call    ??3@YAXPAX@Z; operator delete(void *)
0048BDBE  add     esp, 4
0048BDC1  push    offset aImageCobcannon_0; "IMAGE_COBCANNON_TARGET"
0048BDC6  lea     ecx, [ebp+70h+var_240]
0048BDCC  mov     [ebp+70h+var_650], ebx
0048BDD2  mov     [ebp+70h+var_654], 0
0048BDDC  mov     byte ptr [ebp+70h+var_664], 0
0048BDE3  call    sub_4033F0
0048BDE8  lea     eax, [ebp+70h+var_240]
0048BDEE  mov     byte ptr [ebp+70h+var_74], 12h
0048BDF2  mov     edx, [esi]
0048BDF4  mov     edx, [edx+40h]
0048BDF7  push    eax
0048BDF8  lea     ecx, [ebp+70h+var_141C]
0048BDFE  push    ecx
0048BDFF  mov     ecx, esi
0048BE01  call    edx
0048BE03  mov     ecx, eax
0048BE05  call    sub_5B05A0
0048BE0A  lea     ecx, [ebp+70h+var_141C]
0048BE10  mov     dword_752B18, eax
0048BE15  call    sub_5B0440
0048BE1A  mov     byte ptr [ebp+70h+var_74], 0
0048BE1E  cmp     [ebp+70h+var_228], edi
0048BE24  jb      short loc_48BE35
0048BE26  mov     eax, [ebp+70h+var_23C]
0048BE2C  push    eax; void *
0048BE2D  call    ??3@YAXPAX@Z; operator delete(void *)
0048BE32  add     esp, 4
0048BE35  push    offset aImageCoinbank; "IMAGE_COINBANK"
0048BE3A  lea     ecx, [ebp+70h+var_518]
0048BE40  mov     [ebp+70h+var_228], ebx
0048BE46  mov     [ebp+70h+var_22C], 0
0048BE50  mov     byte ptr [ebp+70h+var_23C], 0
0048BE57  call    sub_4033F0
0048BE5C  lea     eax, [ebp+70h+var_518]
0048BE62  mov     byte ptr [ebp+70h+var_74], 13h
0048BE66  mov     edx, [esi]
0048BE68  mov     edx, [edx+40h]
0048BE6B  push    eax
0048BE6C  lea     ecx, [ebp+70h+var_1434]
0048BE72  push    ecx
0048BE73  mov     ecx, esi
0048BE75  call    edx
0048BE77  mov     ecx, eax
0048BE79  call    sub_5B05A0
0048BE7E  lea     ecx, [ebp+70h+var_1434]
0048BE84  mov     dword_75366C, eax
0048BE89  call    sub_5B0440
0048BE8E  mov     byte ptr [ebp+70h+var_74], 0
0048BE92  cmp     [ebp+70h+var_500], edi
0048BE98  jb      short loc_48BEA9
0048BE9A  mov     eax, [ebp+70h+var_514]
0048BEA0  push    eax; void *
0048BEA1  call    ??3@YAXPAX@Z; operator delete(void *)
0048BEA6  add     esp, 4
0048BEA9  push    offset aImageConveyorb; "IMAGE_CONVEYORBELT"
0048BEAE  lea     ecx, [ebp+70h+var_278]
0048BEB4  mov     [ebp+70h+var_500], ebx
0048BEBA  mov     [ebp+70h+var_504], 0
0048BEC4  mov     byte ptr [ebp+70h+var_514], 0
0048BECB  call    sub_4033F0
0048BED0  lea     eax, [ebp+70h+var_278]
0048BED6  mov     byte ptr [ebp+70h+var_74], 14h
0048BEDA  mov     edx, [esi]
0048BEDC  mov     edx, [edx+40h]
0048BEDF  push    eax
0048BEE0  lea     ecx, [ebp+70h+var_144C]
0048BEE6  push    ecx
0048BEE7  mov     ecx, esi
0048BEE9  call    edx
0048BEEB  mov     ecx, eax
0048BEED  call    sub_5B05A0
0048BEF2  lea     ecx, [ebp+70h+var_144C]
0048BEF8  mov     dword_752D90, eax
0048BEFD  call    sub_5B0440
0048BF02  mov     byte ptr [ebp+70h+var_74], 0
0048BF06  cmp     [ebp+70h+var_260], edi
0048BF0C  jb      short loc_48BF1D
0048BF0E  mov     eax, [ebp+70h+var_274]
0048BF14  push    eax; void *
0048BF15  call    ??3@YAXPAX@Z; operator delete(void *)
0048BF1A  add     esp, 4
0048BF1D  push    offset aImageConveyorb_0; "IMAGE_CONVEYORBELT_BACKDROP"
0048BF22  lea     ecx, [ebp+70h+var_780]
0048BF28  mov     [ebp+70h+var_260], ebx
0048BF2E  mov     [ebp+70h+var_264], 0
0048BF38  mov     byte ptr [ebp+70h+var_274], 0
0048BF3F  call    sub_4033F0
0048BF44  lea     eax, [ebp+70h+var_780]
0048BF4A  mov     byte ptr [ebp+70h+var_74], 15h
0048BF4E  mov     edx, [esi]
0048BF50  mov     edx, [edx+40h]
0048BF53  push    eax
0048BF54  lea     ecx, [ebp+70h+var_1464]
0048BF5A  push    ecx
0048BF5B  mov     ecx, esi
0048BF5D  call    edx
0048BF5F  mov     ecx, eax
0048BF61  call    sub_5B05A0
0048BF66  lea     ecx, [ebp+70h+var_1464]
0048BF6C  mov     dword_753164, eax
0048BF71  call    sub_5B0440
0048BF76  mov     byte ptr [ebp+70h+var_74], 0
0048BF7A  cmp     [ebp+70h+var_768], edi
0048BF80  jb      short loc_48BF91
0048BF82  mov     eax, [ebp+70h+var_77C]
0048BF88  push    eax; void *
0048BF89  call    ??3@YAXPAX@Z; operator delete(void *)
0048BF8E  add     esp, 4
0048BF91  push    offset aImageCrater; "IMAGE_CRATER"
0048BF96  lea     ecx, [ebp+70h+var_2B0]
0048BF9C  mov     [ebp+70h+var_768], ebx
0048BFA2  mov     [ebp+70h+var_76C], 0
0048BFAC  mov     byte ptr [ebp+70h+var_77C], 0
0048BFB3  call    sub_4033F0
0048BFB8  lea     eax, [ebp+70h+var_2B0]
0048BFBE  mov     byte ptr [ebp+70h+var_74], 16h
0048BFC2  mov     edx, [esi]
0048BFC4  mov     edx, [edx+40h]
0048BFC7  push    eax
0048BFC8  lea     ecx, [ebp+70h+var_147C]
0048BFCE  push    ecx
0048BFCF  mov     ecx, esi
0048BFD1  call    edx
0048BFD3  mov     ecx, eax
0048BFD5  call    sub_5B05A0
0048BFDA  lea     ecx, [ebp+70h+var_147C]
0048BFE0  mov     dword_7537F4, eax
0048BFE5  call    sub_5B0440
0048BFEA  mov     byte ptr [ebp+70h+var_74], 0
0048BFEE  cmp     [ebp+70h+var_298], edi
0048BFF4  jb      short loc_48C005
0048BFF6  mov     eax, [ebp+70h+var_2AC]
0048BFFC  push    eax; void *
0048BFFD  call    ??3@YAXPAX@Z; operator delete(void *)
0048C002  add     esp, 4
0048C005  push    offset aImageCraterFad; "IMAGE_CRATER_FADING"
0048C00A  lea     ecx, [ebp+70h+var_550]
0048C010  mov     [ebp+70h+var_298], ebx
0048C016  mov     [ebp+70h+var_29C], 0
0048C020  mov     byte ptr [ebp+70h+var_2AC], 0
0048C027  call    sub_4033F0
0048C02C  lea     eax, [ebp+70h+var_550]
0048C032  mov     byte ptr [ebp+70h+var_74], 17h
0048C036  mov     edx, [esi]
0048C038  mov     edx, [edx+40h]
0048C03B  push    eax
0048C03C  lea     ecx, [ebp+70h+var_1494]
0048C042  push    ecx
0048C043  mov     ecx, esi
0048C045  call    edx
0048C047  mov     ecx, eax
0048C049  call    sub_5B05A0
0048C04E  lea     ecx, [ebp+70h+var_1494]
0048C054  mov     dword_752B44, eax
0048C059  call    sub_5B0440
0048C05E  mov     byte ptr [ebp+70h+var_74], 0
0048C062  cmp     [ebp+70h+var_538], edi
0048C068  jb      short loc_48C079
0048C06A  mov     eax, [ebp+70h+var_54C]
0048C070  push    eax; void *
0048C071  call    ??3@YAXPAX@Z; operator delete(void *)
0048C076  add     esp, 4
0048C079  push    offset aImageCraterRoo; "IMAGE_CRATER_ROOF_CENTER"
0048C07E  lea     ecx, [ebp+70h+var_2E8]
0048C084  mov     [ebp+70h+var_538], ebx
0048C08A  mov     [ebp+70h+var_53C], 0
0048C094  mov     byte ptr [ebp+70h+var_54C], 0
0048C09B  call    sub_4033F0
0048C0A0  lea     eax, [ebp+70h+var_2E8]
0048C0A6  mov     byte ptr [ebp+70h+var_74], 18h
0048C0AA  mov     edx, [esi]
0048C0AC  mov     edx, [edx+40h]
0048C0AF  push    eax
0048C0B0  lea     ecx, [ebp+70h+var_14AC]
0048C0B6  push    ecx
0048C0B7  mov     ecx, esi
0048C0B9  call    edx
0048C0BB  mov     ecx, eax
0048C0BD  call    sub_5B05A0
0048C0C2  lea     ecx, [ebp+70h+var_14AC]
0048C0C8  mov     dword_7537E8, eax
0048C0CD  call    sub_5B0440
0048C0D2  mov     byte ptr [ebp+70h+var_74], 0
0048C0D6  cmp     [ebp+70h+var_2D0], edi
0048C0DC  jb      short loc_48C0ED
0048C0DE  mov     eax, [ebp+70h+var_2E4]
0048C0E4  push    eax; void *
0048C0E5  call    ??3@YAXPAX@Z; operator delete(void *)
0048C0EA  add     esp, 4
0048C0ED  push    offset aImageCraterRoo_0; "IMAGE_CRATER_ROOF_LEFT"
0048C0F2  lea     ecx, [ebp+70h+var_6A0]
0048C0F8  mov     [ebp+70h+var_2D0], ebx
0048C0FE  mov     [ebp+70h+var_2D4], 0
0048C108  mov     byte ptr [ebp+70h+var_2E4], 0
0048C10F  call    sub_4033F0
0048C114  lea     eax, [ebp+70h+var_6A0]
0048C11A  mov     byte ptr [ebp+70h+var_74], 19h
0048C11E  mov     edx, [esi]
0048C120  mov     edx, [edx+40h]
0048C123  push    eax
0048C124  lea     ecx, [ebp+70h+var_14C4]
0048C12A  push    ecx
0048C12B  mov     ecx, esi
0048C12D  call    edx
0048C12F  mov     ecx, eax
0048C131  call    sub_5B05A0
0048C136  lea     ecx, [ebp+70h+var_14C4]
0048C13C  mov     dword_753348, eax
0048C141  call    sub_5B0440
0048C146  mov     byte ptr [ebp+70h+var_74], 0
0048C14A  cmp     [ebp+70h+var_688], edi
0048C150  jb      short loc_48C161
0048C152  mov     eax, [ebp+70h+var_69C]
0048C158  push    eax; void *
0048C159  call    ??3@YAXPAX@Z; operator delete(void *)
0048C15E  add     esp, 4
0048C161  push    offset aImageCraterWat; "IMAGE_CRATER_WATER_DAY"
0048C166  lea     ecx, [ebp+70h+var_320]
0048C16C  mov     [ebp+70h+var_688], ebx
0048C172  mov     [ebp+70h+var_68C], 0
0048C17C  mov     byte ptr [ebp+70h+var_69C], 0
0048C183  call    sub_4033F0
0048C188  lea     eax, [ebp+70h+var_320]
0048C18E  mov     byte ptr [ebp+70h+var_74], 1Ah
0048C192  mov     edx, [esi]
0048C194  mov     edx, [edx+40h]
0048C197  push    eax
0048C198  lea     ecx, [ebp+70h+var_14DC]
0048C19E  push    ecx
0048C19F  mov     ecx, esi
0048C1A1  call    edx
0048C1A3  mov     ecx, eax
0048C1A5  call    sub_5B05A0
0048C1AA  lea     ecx, [ebp+70h+var_14DC]
0048C1B0  mov     dword_7534D4, eax
0048C1B5  call    sub_5B0440
0048C1BA  mov     byte ptr [ebp+70h+var_74], 0
0048C1BE  cmp     [ebp+70h+var_308], edi
0048C1C4  jb      short loc_48C1D5
0048C1C6  mov     eax, [ebp+70h+var_31C]
0048C1CC  push    eax; void *
0048C1CD  call    ??3@YAXPAX@Z; operator delete(void *)
0048C1D2  add     esp, 4
0048C1D5  push    offset aImageCraterWat_0; "IMAGE_CRATER_WATER_NIGHT"
0048C1DA  lea     ecx, [ebp+70h+var_588]
0048C1E0  mov     [ebp+70h+var_308], ebx
0048C1E6  mov     [ebp+70h+var_30C], 0
0048C1F0  mov     byte ptr [ebp+70h+var_31C], 0
0048C1F7  call    sub_4033F0
0048C1FC  lea     eax, [ebp+70h+var_588]
0048C202  mov     byte ptr [ebp+70h+var_74], 1Bh
0048C206  mov     edx, [esi]
0048C208  mov     edx, [edx+40h]
0048C20B  push    eax
0048C20C  lea     ecx, [ebp+70h+var_14F4]
0048C212  push    ecx
0048C213  mov     ecx, esi
0048C215  call    edx
0048C217  mov     ecx, eax
0048C219  call    sub_5B05A0
0048C21E  lea     ecx, [ebp+70h+var_14F4]
0048C224  mov     dword_753148, eax
0048C229  call    sub_5B0440
0048C22E  mov     byte ptr [ebp+70h+var_74], 0
0048C232  cmp     [ebp+70h+var_570], edi
0048C238  jb      short loc_48C249
0048C23A  mov     eax, [ebp+70h+var_584]
0048C240  push    eax; void *
0048C241  call    ??3@YAXPAX@Z; operator delete(void *)
0048C246  add     esp, 4
0048C249  push    offset aImageDialogBig; "IMAGE_DIALOG_BIGBOTTOMLEFT"
0048C24E  lea     ecx, [ebp+70h+var_358]
0048C254  mov     [ebp+70h+var_570], ebx
0048C25A  mov     [ebp+70h+var_574], 0
0048C264  mov     byte ptr [ebp+70h+var_584], 0
0048C26B  call    sub_4033F0
0048C270  lea     eax, [ebp+70h+var_358]
0048C276  mov     byte ptr [ebp+70h+var_74], 1Ch
0048C27A  mov     edx, [esi]
0048C27C  mov     edx, [edx+40h]
0048C27F  push    eax
0048C280  lea     ecx, [ebp+70h+var_150C]
0048C286  push    ecx
0048C287  mov     ecx, esi
0048C289  call    edx
0048C28B  mov     ecx, eax
0048C28D  call    sub_5B05A0
0048C292  lea     ecx, [ebp+70h+var_150C]
0048C298  mov     dword_752F08, eax
0048C29D  call    sub_5B0440
0048C2A2  mov     byte ptr [ebp+70h+var_74], 0
0048C2A6  cmp     [ebp+70h+var_340], edi
0048C2AC  jb      short loc_48C2BD
0048C2AE  mov     eax, [ebp+70h+var_354]
0048C2B4  push    eax; void *
0048C2B5  call    ??3@YAXPAX@Z; operator delete(void *)
0048C2BA  add     esp, 4
0048C2BD  push    offset aImageDialogBig_0; "IMAGE_DIALOG_BIGBOTTOMMIDDLE"
0048C2C2  lea     ecx, [ebp+70h+var_748]
0048C2C8  mov     [ebp+70h+var_340], ebx
0048C2CE  mov     [ebp+70h+var_344], 0
0048C2D8  mov     byte ptr [ebp+70h+var_354], 0
0048C2DF  call    sub_4033F0
0048C2E4  lea     eax, [ebp+70h+var_748]
0048C2EA  mov     byte ptr [ebp+70h+var_74], 1Dh
0048C2EE  mov     edx, [esi]
0048C2F0  mov     edx, [edx+40h]
0048C2F3  push    eax
0048C2F4  lea     ecx, [ebp+70h+var_1524]
0048C2FA  push    ecx
0048C2FB  mov     ecx, esi
0048C2FD  call    edx
0048C2FF  mov     ecx, eax
0048C301  call    sub_5B05A0
0048C306  lea     ecx, [ebp+70h+var_1524]
0048C30C  mov     dword_752C98, eax
0048C311  call    sub_5B0440
0048C316  mov     byte ptr [ebp+70h+var_74], 0
0048C31A  cmp     [ebp+70h+var_730], edi
0048C320  jb      short loc_48C331
0048C322  mov     eax, [ebp+70h+var_744]
0048C328  push    eax; void *
0048C329  call    ??3@YAXPAX@Z; operator delete(void *)
0048C32E  add     esp, 4
0048C331  push    offset aImageDialogBig_1; "IMAGE_DIALOG_BIGBOTTOMRIGHT"
0048C336  lea     ecx, [ebp+70h+var_390]
0048C33C  mov     [ebp+70h+var_730], ebx
0048C342  mov     [ebp+70h+var_734], 0
0048C34C  mov     byte ptr [ebp+70h+var_744], 0
0048C353  call    sub_4033F0
0048C358  lea     eax, [ebp+70h+var_390]
0048C35E  mov     byte ptr [ebp+70h+var_74], 1Eh
0048C362  mov     edx, [esi]
0048C364  mov     edx, [edx+40h]
0048C367  push    eax
0048C368  lea     ecx, [ebp+70h+var_153C]
0048C36E  push    ecx
0048C36F  mov     ecx, esi
0048C371  call    edx
0048C373  mov     ecx, eax
0048C375  call    sub_5B05A0
0048C37A  lea     ecx, [ebp+70h+var_153C]
0048C380  mov     dword_75363C, eax
0048C385  call    sub_5B0440
0048C38A  mov     byte ptr [ebp+70h+var_74], 0
0048C38E  cmp     [ebp+70h+var_378], edi
0048C394  jb      short loc_48C3A5
0048C396  mov     eax, [ebp+70h+var_38C]
0048C39C  push    eax; void *
0048C39D  call    ??3@YAXPAX@Z; operator delete(void *)
0048C3A2  add     esp, 4
0048C3A5  push    offset aImageDialogBot; "IMAGE_DIALOG_BOTTOMLEFT"
0048C3AA  lea     ecx, [ebp+70h+var_5C0]
0048C3B0  mov     [ebp+70h+var_378], ebx
0048C3B6  mov     [ebp+70h+var_37C], 0
0048C3C0  mov     byte ptr [ebp+70h+var_38C], 0
0048C3C7  call    sub_4033F0
0048C3CC  lea     eax, [ebp+70h+var_5C0]
0048C3D2  mov     byte ptr [ebp+70h+var_74], 1Fh
0048C3D6  mov     edx, [esi]
0048C3D8  mov     edx, [edx+40h]
0048C3DB  push    eax
0048C3DC  lea     ecx, [ebp+70h+var_1554]
0048C3E2  push    ecx
0048C3E3  mov     ecx, esi
0048C3E5  call    edx
0048C3E7  mov     ecx, eax
0048C3E9  call    sub_5B05A0
0048C3EE  lea     ecx, [ebp+70h+var_1554]
0048C3F4  mov     dword_753354, eax
0048C3F9  call    sub_5B0440
0048C3FE  mov     byte ptr [ebp+70h+var_74], 0
0048C402  cmp     [ebp+70h+var_5A8], edi
0048C408  jb      short loc_48C419
0048C40A  mov     eax, [ebp+70h+var_5BC]
0048C410  push    eax; void *
0048C411  call    ??3@YAXPAX@Z; operator delete(void *)
0048C416  add     esp, 4
0048C419  push    offset aImageDialogBot_0; "IMAGE_DIALOG_BOTTOMMIDDLE"
0048C41E  lea     ecx, [ebp+70h+var_3C8]
0048C424  mov     [ebp+70h+var_5A8], ebx
0048C42A  mov     [ebp+70h+var_5AC], 0
0048C434  mov     byte ptr [ebp+70h+var_5BC], 0
0048C43B  call    sub_4033F0
0048C440  lea     eax, [ebp+70h+var_3C8]
0048C446  mov     byte ptr [ebp+70h+var_74], 20h ; ' '
0048C44A  mov     edx, [esi]
0048C44C  mov     edx, [edx+40h]
0048C44F  push    eax
0048C450  lea     ecx, [ebp+70h+var_156C]
0048C456  push    ecx
0048C457  mov     ecx, esi
0048C459  call    edx
0048C45B  mov     ecx, eax
0048C45D  call    sub_5B05A0
0048C462  lea     ecx, [ebp+70h+var_156C]
0048C468  mov     dword_7536A8, eax
0048C46D  call    sub_5B0440
0048C472  mov     byte ptr [ebp+70h+var_74], 0
0048C476  cmp     [ebp+70h+var_3B0], edi
0048C47C  jb      short loc_48C48D
0048C47E  mov     eax, [ebp+70h+var_3C4]
0048C484  push    eax; void *
0048C485  call    ??3@YAXPAX@Z; operator delete(void *)
0048C48A  add     esp, 4
0048C48D  push    offset aImageDialogBot_1; "IMAGE_DIALOG_BOTTOMRIGHT"
0048C492  lea     ecx, [ebp+70h+var_6D8]
0048C498  mov     [ebp+70h+var_3B0], ebx
0048C49E  mov     [ebp+70h+var_3B4], 0
0048C4A8  mov     byte ptr [ebp+70h+var_3C4], 0
0048C4AF  call    sub_4033F0
0048C4B4  lea     eax, [ebp+70h+var_6D8]
0048C4BA  mov     byte ptr [ebp+70h+var_74], 21h ; '!'
0048C4BE  mov     edx, [esi]
0048C4C0  mov     edx, [edx+40h]
0048C4C3  push    eax
0048C4C4  lea     ecx, [ebp+70h+var_1584]
0048C4CA  push    ecx
0048C4CB  mov     ecx, esi
0048C4CD  call    edx
0048C4CF  mov     ecx, eax
0048C4D1  call    sub_5B05A0
0048C4D6  lea     ecx, [ebp+70h+var_1584]
0048C4DC  mov     dword_753214, eax
0048C4E1  call    sub_5B0440
0048C4E6  mov     byte ptr [ebp+70h+var_74], 0
0048C4EA  cmp     [ebp+70h+var_6C0], edi
0048C4F0  jb      short loc_48C501
0048C4F2  mov     eax, [ebp+70h+var_6D4]
0048C4F8  push    eax; void *
0048C4F9  call    ??3@YAXPAX@Z; operator delete(void *)
0048C4FE  add     esp, 4
0048C501  push    offset aImageDialogCen; "IMAGE_DIALOG_CENTERLEFT"
0048C506  lea     ecx, [ebp+70h+var_400]
0048C50C  mov     [ebp+70h+var_6C0], ebx
0048C512  mov     [ebp+70h+var_6C4], 0
0048C51C  mov     byte ptr [ebp+70h+var_6D4], 0
0048C523  call    sub_4033F0
0048C528  lea     eax, [ebp+70h+var_400]
0048C52E  mov     byte ptr [ebp+70h+var_74], 22h ; '"'
0048C532  mov     edx, [esi]
0048C534  mov     edx, [edx+40h]
0048C537  push    eax
0048C538  lea     ecx, [ebp+70h+var_159C]
0048C53E  push    ecx
0048C53F  mov     ecx, esi
0048C541  call    edx
0048C543  mov     ecx, eax
0048C545  call    sub_5B05A0
0048C54A  lea     ecx, [ebp+70h+var_159C]
0048C550  mov     dword_753428, eax
0048C555  call    sub_5B0440
0048C55A  mov     byte ptr [ebp+70h+var_74], 0
0048C55E  cmp     [ebp+70h+var_3E8], edi
0048C564  jb      short loc_48C575
0048C566  mov     eax, [ebp+70h+var_3FC]
0048C56C  push    eax; void *
0048C56D  call    ??3@YAXPAX@Z; operator delete(void *)
0048C572  add     esp, 4
0048C575  push    offset aImageDialogCen_0; "IMAGE_DIALOG_CENTERMIDDLE"
0048C57A  lea     ecx, [ebp+70h+var_54]
0048C57D  mov     [ebp+70h+var_3E8], ebx
0048C583  mov     [ebp+70h+var_3EC], 0
0048C58D  mov     byte ptr [ebp+70h+var_3FC], 0
0048C594  call    sub_4033F0
0048C599  lea     eax, [ebp+70h+var_54]
0048C59C  mov     byte ptr [ebp+70h+var_74], 23h ; '#'
0048C5A0  mov     edx, [esi]
0048C5A2  mov     edx, [edx+40h]
0048C5A5  push    eax
0048C5A6  lea     ecx, [ebp+70h+var_15B4]
0048C5AC  push    ecx
0048C5AD  mov     ecx, esi
0048C5AF  call    edx
0048C5B1  mov     ecx, eax
0048C5B3  call    sub_5B05A0
0048C5B8  lea     ecx, [ebp+70h+var_15B4]
0048C5BE  mov     dword_753330, eax
0048C5C3  call    sub_5B0440
0048C5C8  mov     byte ptr [ebp+70h+var_74], 0
0048C5CC  cmp     [ebp+70h+var_3C], edi
0048C5CF  jb      short loc_48C5DD
0048C5D1  mov     eax, [ebp+70h+var_50]
0048C5D4  push    eax; void *
0048C5D5  call    ??3@YAXPAX@Z; operator delete(void *)
0048C5DA  add     esp, 4
0048C5DD  push    offset aImageDialogCen_1; "IMAGE_DIALOG_CENTERRIGHT"
0048C5E2  lea     ecx, [ebp+70h+var_70]
0048C5E5  mov     [ebp+70h+var_3C], ebx
0048C5E8  mov     [ebp+70h+var_40], 0
0048C5EF  mov     byte ptr [ebp+70h+var_50], 0
0048C5F3  call    sub_4033F0
0048C5F8  lea     eax, [ebp+70h+var_70]
0048C5FB  mov     byte ptr [ebp+70h+var_74], 24h ; '$'
0048C5FF  mov     edx, [esi]
0048C601  mov     edx, [edx+40h]
0048C604  push    eax
0048C605  lea     ecx, [ebp+70h+var_15CC]
0048C60B  push    ecx
0048C60C  mov     ecx, esi
0048C60E  call    edx
0048C610  mov     ecx, eax
0048C612  call    sub_5B05A0
0048C617  lea     ecx, [ebp+70h+var_15CC]
0048C61D  mov     dword_753668, eax
0048C622  call    sub_5B0440
0048C627  mov     byte ptr [ebp+70h+var_74], 0
0048C62B  cmp     [ebp+70h+var_58], edi
0048C62E  jb      short loc_48C63C
0048C630  mov     eax, [ebp+70h+var_6C]
0048C633  push    eax; void *
0048C634  call    ??3@YAXPAX@Z; operator delete(void *)
0048C639  add     esp, 4
0048C63C  push    offset aImageDialogHea; "IMAGE_DIALOG_HEADER"
0048C641  lea     ecx, [ebp+70h+var_9C]
0048C644  mov     [ebp+70h+var_58], ebx
0048C647  mov     [ebp+70h+var_5C], 0
0048C64E  mov     byte ptr [ebp+70h+var_6C], 0
0048C652  call    sub_4033F0
0048C657  lea     eax, [ebp+70h+var_9C]
0048C65A  mov     byte ptr [ebp+70h+var_74], 25h ; '%'
0048C65E  mov     edx, [esi]
0048C660  mov     edx, [edx+40h]
0048C663  push    eax
0048C664  lea     ecx, [ebp+70h+var_7EC]
0048C66A  push    ecx
0048C66B  mov     ecx, esi
0048C66D  call    edx
0048C66F  mov     ecx, eax
0048C671  call    sub_5B05A0
0048C676  lea     ecx, [ebp+70h+var_7EC]
0048C67C  mov     dword_752E24, eax
0048C681  call    sub_5B0440
0048C686  mov     byte ptr [ebp+70h+var_74], 0
0048C68A  cmp     [ebp+70h+var_84], edi
0048C68D  jb      short loc_48C69B
0048C68F  mov     eax, [ebp+70h+var_98]
0048C692  push    eax; void *
0048C693  call    ??3@YAXPAX@Z; operator delete(void *)
0048C698  add     esp, 4
0048C69B  push    offset aImageDialogTop; "IMAGE_DIALOG_TOPLEFT"
0048C6A0  lea     ecx, [ebp+70h+var_D4]
0048C6A3  mov     [ebp+70h+var_84], ebx
0048C6A6  mov     [ebp+70h+var_88], 0
0048C6AD  mov     byte ptr [ebp+70h+var_98], 0
0048C6B1  call    sub_4033F0
0048C6B6  lea     eax, [ebp+70h+var_D4]
0048C6B9  mov     byte ptr [ebp+70h+var_74], 26h ; '&'
0048C6BD  mov     edx, [esi]
0048C6BF  mov     edx, [edx+40h]
0048C6C2  push    eax
0048C6C3  lea     ecx, [ebp+70h+var_7F8]
0048C6C9  push    ecx
0048C6CA  mov     ecx, esi
0048C6CC  call    edx
0048C6CE  mov     ecx, eax
0048C6D0  call    sub_5B05A0
0048C6D5  lea     ecx, [ebp+70h+var_7F8]
0048C6DB  mov     dword_752F04, eax
0048C6E0  call    sub_5B0440
0048C6E5  mov     byte ptr [ebp+70h+var_74], 0
0048C6E9  cmp     [ebp+70h+var_BC], edi
0048C6EC  jb      short loc_48C6FA
0048C6EE  mov     eax, [ebp+70h+var_D0]
0048C6F1  push    eax; void *
0048C6F2  call    ??3@YAXPAX@Z; operator delete(void *)
0048C6F7  add     esp, 4
0048C6FA  push    offset aImageDialogTop_0; "IMAGE_DIALOG_TOPMIDDLE"
0048C6FF  lea     ecx, [ebp+70h+var_10C]
0048C705  mov     [ebp+70h+var_BC], ebx
0048C708  mov     [ebp+70h+var_C0], 0
0048C70F  mov     byte ptr [ebp+70h+var_D0], 0
0048C713  call    sub_4033F0
0048C718  lea     eax, [ebp+70h+var_10C]
0048C71E  mov     byte ptr [ebp+70h+var_74], 27h ; '''
0048C722  mov     edx, [esi]
0048C724  mov     edx, [edx+40h]
0048C727  push    eax
0048C728  lea     ecx, [ebp+70h+var_810]
0048C72E  push    ecx
0048C72F  mov     ecx, esi
0048C731  call    edx
0048C733  mov     ecx, eax
0048C735  call    sub_5B05A0
0048C73A  lea     ecx, [ebp+70h+var_810]
0048C740  mov     dword_753114, eax
0048C745  call    sub_5B0440
0048C74A  mov     byte ptr [ebp+70h+var_74], 0
0048C74E  cmp     [ebp+70h+var_F4], edi
0048C754  jb      short loc_48C765
0048C756  mov     eax, [ebp+70h+var_108]
0048C75C  push    eax; void *
0048C75D  call    ??3@YAXPAX@Z; operator delete(void *)
0048C762  add     esp, 4
0048C765  push    offset aImageDialogTop_1; "IMAGE_DIALOG_TOPRIGHT"
0048C76A  lea     ecx, [ebp+70h+var_144]
0048C770  mov     [ebp+70h+var_F4], ebx
0048C776  mov     [ebp+70h+var_F8], 0
0048C780  mov     byte ptr [ebp+70h+var_108], 0
0048C787  call    sub_4033F0
0048C78C  lea     eax, [ebp+70h+var_144]
0048C792  mov     byte ptr [ebp+70h+var_74], 28h ; '('
0048C796  mov     edx, [esi]
0048C798  mov     edx, [edx+40h]
0048C79B  push    eax
0048C79C  lea     ecx, [ebp+70h+var_828]
0048C7A2  push    ecx
0048C7A3  mov     ecx, esi
0048C7A5  call    edx
0048C7A7  mov     ecx, eax
0048C7A9  call    sub_5B05A0
0048C7AE  lea     ecx, [ebp+70h+var_828]
0048C7B4  mov     dword_752D00, eax
0048C7B9  call    sub_5B0440
0048C7BE  mov     byte ptr [ebp+70h+var_74], 0
0048C7C2  cmp     [ebp+70h+var_12C], edi
0048C7C8  jb      short loc_48C7D9
0048C7CA  mov     eax, [ebp+70h+var_140]
0048C7D0  push    eax; void *
0048C7D1  call    ??3@YAXPAX@Z; operator delete(void *)
0048C7D6  add     esp, 4
0048C7D9  push    offset aImageDirtbig; "IMAGE_DIRTBIG"
0048C7DE  lea     ecx, [ebp+70h+var_17C]
0048C7E4  mov     [ebp+70h+var_12C], ebx
0048C7EA  mov     [ebp+70h+var_130], 0
0048C7F4  mov     byte ptr [ebp+70h+var_140], 0
0048C7FB  call    sub_4033F0
0048C800  lea     eax, [ebp+70h+var_17C]
0048C806  mov     byte ptr [ebp+70h+var_74], 29h ; ')'
0048C80A  mov     edx, [esi]
0048C80C  mov     edx, [edx+40h]
0048C80F  push    eax
0048C810  lea     ecx, [ebp+70h+var_840]
0048C816  push    ecx
0048C817  mov     ecx, esi
0048C819  call    edx
0048C81B  mov     ecx, eax
0048C81D  call    sub_5B05A0
0048C822  lea     ecx, [ebp+70h+var_840]
0048C828  mov     dword_75332C, eax
0048C82D  call    sub_5B0440
0048C832  mov     byte ptr [ebp+70h+var_74], 0
0048C836  cmp     [ebp+70h+var_164], edi
0048C83C  jb      short loc_48C84D
0048C83E  mov     eax, [ebp+70h+var_178]
0048C844  push    eax; void *
0048C845  call    ??3@YAXPAX@Z; operator delete(void *)
0048C84A  add     esp, 4
0048C84D  push    offset aImageDirtsmall; "IMAGE_DIRTSMALL"
0048C852  lea     ecx, [ebp+70h+var_1B4]
0048C858  mov     [ebp+70h+var_164], ebx
0048C85E  mov     [ebp+70h+var_168], 0
0048C868  mov     byte ptr [ebp+70h+var_178], 0
0048C86F  call    sub_4033F0
0048C874  lea     eax, [ebp+70h+var_1B4]
0048C87A  mov     byte ptr [ebp+70h+var_74], 2Ah ; '*'
0048C87E  mov     edx, [esi]
0048C880  mov     edx, [edx+40h]
0048C883  push    eax
0048C884  lea     ecx, [ebp+70h+var_858]
0048C88A  push    ecx
0048C88B  mov     ecx, esi
0048C88D  call    edx
0048C88F  mov     ecx, eax
0048C891  call    sub_5B05A0
0048C896  lea     ecx, [ebp+70h+var_858]
0048C89C  mov     dword_753258, eax
0048C8A1  call    sub_5B0440
0048C8A6  mov     byte ptr [ebp+70h+var_74], 0
0048C8AA  cmp     [ebp+70h+var_19C], edi
0048C8B0  jb      short loc_48C8C1
0048C8B2  mov     eax, [ebp+70h+var_1B0]
0048C8B8  push    eax; void *
0048C8B9  call    ??3@YAXPAX@Z; operator delete(void *)
0048C8BE  add     esp, 4
0048C8C1  push    offset aImageDoomshroo; "IMAGE_DOOMSHROOM_EXPLOSION_BASE"
0048C8C6  lea     ecx, [ebp+70h+var_1EC]
0048C8CC  mov     [ebp+70h+var_19C], ebx
0048C8D2  mov     [ebp+70h+var_1A0], 0
0048C8DC  mov     byte ptr [ebp+70h+var_1B0], 0
0048C8E3  call    sub_4033F0
0048C8E8  lea     eax, [ebp+70h+var_1EC]
0048C8EE  mov     byte ptr [ebp+70h+var_74], 2Bh ; '+'
0048C8F2  mov     edx, [esi]
0048C8F4  mov     edx, [edx+40h]
0048C8F7  push    eax
0048C8F8  lea     ecx, [ebp+70h+var_870]
0048C8FE  push    ecx
0048C8FF  mov     ecx, esi
0048C901  call    edx
0048C903  mov     ecx, eax
0048C905  call    sub_5B05A0
0048C90A  lea     ecx, [ebp+70h+var_870]
0048C910  mov     dword_752AA4, eax
0048C915  call    sub_5B0440
0048C91A  mov     byte ptr [ebp+70h+var_74], 0
0048C91E  cmp     [ebp+70h+var_1D4], edi
0048C924  jb      short loc_48C935
0048C926  mov     eax, [ebp+70h+var_1E8]
0048C92C  push    eax; void *
0048C92D  call    ??3@YAXPAX@Z; operator delete(void *)
0048C932  add     esp, 4
0048C935  push    offset aImageDustPuffs; "IMAGE_DUST_PUFFS"
0048C93A  lea     ecx, [ebp+70h+var_224]
0048C940  mov     [ebp+70h+var_1D4], ebx
0048C946  mov     [ebp+70h+var_1D8], 0
0048C950  mov     byte ptr [ebp+70h+var_1E8], 0
0048C957  call    sub_4033F0
0048C95C  lea     eax, [ebp+70h+var_224]
0048C962  mov     byte ptr [ebp+70h+var_74], 2Ch ; ','
0048C966  mov     edx, [esi]
0048C968  mov     edx, [edx+40h]
0048C96B  push    eax
0048C96C  lea     ecx, [ebp+70h+var_888]
0048C972  push    ecx
0048C973  mov     ecx, esi
0048C975  call    edx
0048C977  mov     ecx, eax
0048C979  call    sub_5B05A0
0048C97E  lea     ecx, [ebp+70h+var_888]
0048C984  mov     dword_7535F0, eax
0048C989  call    sub_5B0440
0048C98E  mov     byte ptr [ebp+70h+var_74], 0
0048C992  cmp     [ebp+70h+var_20C], edi
0048C998  jb      short loc_48C9A9
0048C99A  mov     eax, [ebp+70h+var_220]
0048C9A0  push    eax; void *
0048C9A1  call    ??3@YAXPAX@Z; operator delete(void *)
0048C9A6  add     esp, 4
0048C9A9  push    offset aImageEditbox; "IMAGE_EDITBOX"
0048C9AE  lea     ecx, [ebp+70h+var_25C]
0048C9B4  mov     [ebp+70h+var_20C], ebx
0048C9BA  mov     [ebp+70h+var_210], 0
0048C9C4  mov     byte ptr [ebp+70h+var_220], 0
0048C9CB  call    sub_4033F0
0048C9D0  lea     eax, [ebp+70h+var_25C]
0048C9D6  mov     byte ptr [ebp+70h+var_74], 2Dh ; '-'
0048C9DA  mov     edx, [esi]
0048C9DC  mov     edx, [edx+40h]
0048C9DF  push    eax
0048C9E0  lea     ecx, [ebp+70h+var_8A0]
0048C9E6  push    ecx
0048C9E7  mov     ecx, esi
0048C9E9  call    edx
0048C9EB  mov     ecx, eax
0048C9ED  call    sub_5B05A0
0048C9F2  lea     ecx, [ebp+70h+var_8A0]
0048C9F8  mov     dword_75346C, eax
0048C9FD  call    sub_5B0440
0048CA02  mov     byte ptr [ebp+70h+var_74], 0
0048CA06  cmp     [ebp+70h+var_244], edi
0048CA0C  jb      short loc_48CA1D
0048CA0E  mov     eax, [ebp+70h+var_258]
0048CA14  push    eax; void *
0048CA15  call    ??3@YAXPAX@Z; operator delete(void *)
0048CA1A  add     esp, 4
0048CA1D  push    offset aImageFertilize; "IMAGE_FERTILIZER"
0048CA22  lea     ecx, [ebp+70h+var_294]
0048CA28  mov     [ebp+70h+var_244], ebx
0048CA2E  mov     [ebp+70h+var_248], 0
0048CA38  mov     byte ptr [ebp+70h+var_258], 0
0048CA3F  call    sub_4033F0
0048CA44  lea     eax, [ebp+70h+var_294]
0048CA4A  mov     byte ptr [ebp+70h+var_74], 2Eh ; '.'
0048CA4E  mov     edx, [esi]
0048CA50  mov     edx, [edx+40h]
0048CA53  push    eax
0048CA54  lea     ecx, [ebp+70h+var_8B8]
0048CA5A  push    ecx
0048CA5B  mov     ecx, esi
0048CA5D  call    edx
0048CA5F  mov     ecx, eax
0048CA61  call    sub_5B05A0
0048CA66  lea     ecx, [ebp+70h+var_8B8]
0048CA6C  mov     dword_752AC4, eax
0048CA71  call    sub_5B0440
0048CA76  mov     byte ptr [ebp+70h+var_74], 0
0048CA7A  cmp     [ebp+70h+var_27C], edi
0048CA80  jb      short loc_48CA91
0048CA82  mov     eax, [ebp+70h+var_290]
0048CA88  push    eax; void *
0048CA89  call    ??3@YAXPAX@Z; operator delete(void *)
0048CA8E  add     esp, 4
0048CA91  push    offset aImageFlagmeter; "IMAGE_FLAGMETER"
0048CA96  lea     ecx, [ebp+70h+var_2CC]
0048CA9C  mov     [ebp+70h+var_27C], ebx
0048CAA2  mov     [ebp+70h+var_280], 0
0048CAAC  mov     byte ptr [ebp+70h+var_290], 0
0048CAB3  call    sub_4033F0
0048CAB8  lea     eax, [ebp+70h+var_2CC]
0048CABE  mov     byte ptr [ebp+70h+var_74], 2Fh ; '/'
0048CAC2  mov     edx, [esi]
0048CAC4  mov     edx, [edx+40h]
0048CAC7  push    eax
0048CAC8  lea     ecx, [ebp+70h+var_8D0]
0048CACE  push    ecx
0048CACF  mov     ecx, esi
0048CAD1  call    edx
0048CAD3  mov     ecx, eax
0048CAD5  call    sub_5B05A0
0048CADA  lea     ecx, [ebp+70h+var_8D0]
0048CAE0  mov     dword_752E68, eax
0048CAE5  call    sub_5B0440
0048CAEA  mov     byte ptr [ebp+70h+var_74], 0
0048CAEE  cmp     [ebp+70h+var_2B4], edi
0048CAF4  jb      short loc_48CB05
0048CAF6  mov     eax, [ebp+70h+var_2C8]
0048CAFC  push    eax; void *
0048CAFD  call    ??3@YAXPAX@Z; operator delete(void *)
0048CB02  add     esp, 4
0048CB05  push    offset aImageFlagmeter_0; "IMAGE_FLAGMETERLEVELPROGRESS"
0048CB0A  lea     ecx, [ebp+70h+var_304]
0048CB10  mov     [ebp+70h+var_2B4], ebx
0048CB16  mov     [ebp+70h+var_2B8], 0
0048CB20  mov     byte ptr [ebp+70h+var_2C8], 0
0048CB27  call    sub_4033F0
0048CB2C  lea     eax, [ebp+70h+var_304]
0048CB32  mov     byte ptr [ebp+70h+var_74], 30h ; '0'
0048CB36  mov     edx, [esi]
0048CB38  mov     edx, [edx+40h]
0048CB3B  push    eax
0048CB3C  lea     ecx, [ebp+70h+var_8E8]
0048CB42  push    ecx
0048CB43  mov     ecx, esi
0048CB45  call    edx
0048CB47  mov     ecx, eax
0048CB49  call    sub_5B05A0
0048CB4E  lea     ecx, [ebp+70h+var_8E8]
0048CB54  mov     dword_752EA0, eax
0048CB59  call    sub_5B0440
0048CB5E  mov     byte ptr [ebp+70h+var_74], 0
0048CB62  cmp     [ebp+70h+var_2EC], edi
0048CB68  jb      short loc_48CB79
0048CB6A  mov     eax, [ebp+70h+var_300]
0048CB70  push    eax; void *
0048CB71  call    ??3@YAXPAX@Z; operator delete(void *)
0048CB76  add     esp, 4
0048CB79  push    offset aImageFlagmeter_1; "IMAGE_FLAGMETERPARTS"
0048CB7E  lea     ecx, [ebp+70h+var_33C]
0048CB84  mov     [ebp+70h+var_2EC], ebx
0048CB8A  mov     [ebp+70h+var_2F0], 0
0048CB94  mov     byte ptr [ebp+70h+var_300], 0
0048CB9B  call    sub_4033F0
0048CBA0  lea     eax, [ebp+70h+var_33C]
0048CBA6  mov     byte ptr [ebp+70h+var_74], 31h ; '1'
0048CBAA  mov     edx, [esi]
0048CBAC  mov     edx, [edx+40h]
0048CBAF  push    eax
0048CBB0  lea     ecx, [ebp+70h+var_900]
0048CBB6  push    ecx
0048CBB7  mov     ecx, esi
0048CBB9  call    edx
0048CBBB  mov     ecx, eax
0048CBBD  call    sub_5B05A0
0048CBC2  lea     ecx, [ebp+70h+var_900]
0048CBC8  mov     dword_753078, eax
0048CBCD  call    sub_5B0440
0048CBD2  mov     byte ptr [ebp+70h+var_74], 0
0048CBD6  cmp     [ebp+70h+var_324], edi
0048CBDC  jb      short loc_48CBED
0048CBDE  mov     eax, [ebp+70h+var_338]
0048CBE4  push    eax; void *
0048CBE5  call    ??3@YAXPAX@Z; operator delete(void *)
0048CBEA  add     esp, 4
0048CBED  push    offset aImageIce; "IMAGE_ICE"
0048CBF2  lea     ecx, [ebp+70h+var_374]
0048CBF8  mov     [ebp+70h+var_324], ebx
0048CBFE  mov     [ebp+70h+var_328], 0
0048CC08  mov     byte ptr [ebp+70h+var_338], 0
0048CC0F  call    sub_4033F0
0048CC14  lea     eax, [ebp+70h+var_374]
0048CC1A  mov     byte ptr [ebp+70h+var_74], 32h ; '2'
0048CC1E  mov     edx, [esi]
0048CC20  mov     edx, [edx+40h]
0048CC23  push    eax
0048CC24  lea     ecx, [ebp+70h+var_918]
0048CC2A  push    ecx
0048CC2B  mov     ecx, esi
0048CC2D  call    edx
0048CC2F  mov     ecx, eax
0048CC31  call    sub_5B05A0
0048CC36  lea     ecx, [ebp+70h+var_918]
0048CC3C  mov     dword_7532F8, eax
0048CC41  call    sub_5B0440
0048CC46  mov     byte ptr [ebp+70h+var_74], 0
0048CC4A  cmp     [ebp+70h+var_35C], edi
0048CC50  jb      short loc_48CC61
0048CC52  mov     eax, [ebp+70h+var_370]
0048CC58  push    eax; void *
0048CC59  call    ??3@YAXPAX@Z; operator delete(void *)
0048CC5E  add     esp, 4
0048CC61  push    offset aImageIcetrap; "IMAGE_ICETRAP"
0048CC66  lea     ecx, [ebp+70h+var_3AC]
0048CC6C  mov     [ebp+70h+var_35C], ebx
0048CC72  mov     [ebp+70h+var_360], 0
0048CC7C  mov     byte ptr [ebp+70h+var_370], 0
0048CC83  call    sub_4033F0
0048CC88  lea     eax, [ebp+70h+var_3AC]
0048CC8E  mov     byte ptr [ebp+70h+var_74], 33h ; '3'
0048CC92  mov     edx, [esi]
0048CC94  mov     edx, [edx+40h]
0048CC97  push    eax
0048CC98  lea     ecx, [ebp+70h+var_930]
0048CC9E  push    ecx
0048CC9F  mov     ecx, esi
0048CCA1  call    edx
0048CCA3  mov     ecx, eax
0048CCA5  call    sub_5B05A0
0048CCAA  lea     ecx, [ebp+70h+var_930]
0048CCB0  mov     dword_7536F0, eax
0048CCB5  call    sub_5B0440
0048CCBA  mov     byte ptr [ebp+70h+var_74], 0
0048CCBE  cmp     [ebp+70h+var_394], edi
0048CCC4  jb      short loc_48CCD5
0048CCC6  mov     eax, [ebp+70h+var_3A8]
0048CCCC  push    eax; void *
0048CCCD  call    ??3@YAXPAX@Z; operator delete(void *)
0048CCD2  add     esp, 4
0048CCD5  push    offset aImageIcetrap2; "IMAGE_ICETRAP2"
0048CCDA  lea     ecx, [ebp+70h+var_3E4]
0048CCE0  mov     [ebp+70h+var_394], ebx
0048CCE6  mov     [ebp+70h+var_398], 0
0048CCF0  mov     byte ptr [ebp+70h+var_3A8], 0
0048CCF7  call    sub_4033F0
0048CCFC  lea     eax, [ebp+70h+var_3E4]
0048CD02  mov     byte ptr [ebp+70h+var_74], 34h ; '4'
0048CD06  mov     edx, [esi]
0048CD08  mov     edx, [edx+40h]
0048CD0B  push    eax
0048CD0C  lea     ecx, [ebp+70h+var_948]
0048CD12  push    ecx
0048CD13  mov     ecx, esi
0048CD15  call    edx
0048CD17  mov     ecx, eax
0048CD19  call    sub_5B05A0
0048CD1E  lea     ecx, [ebp+70h+var_948]
0048CD24  mov     dword_7535D0, eax
0048CD29  call    sub_5B0440
0048CD2E  mov     byte ptr [ebp+70h+var_74], 0
0048CD32  cmp     [ebp+70h+var_3CC], edi
0048CD38  jb      short loc_48CD49
0048CD3A  mov     eax, [ebp+70h+var_3E0]
0048CD40  push    eax; void *
0048CD41  call    ??3@YAXPAX@Z; operator delete(void *)
0048CD46  add     esp, 4
0048CD49  push    offset aImageIcetrapPa; "IMAGE_ICETRAP_PARTICLES"
0048CD4E  lea     ecx, [ebp+70h+var_41C]
0048CD54  mov     [ebp+70h+var_3CC], ebx
0048CD5A  mov     [ebp+70h+var_3D0], 0
0048CD64  mov     byte ptr [ebp+70h+var_3E0], 0
0048CD6B  call    sub_4033F0
0048CD70  lea     eax, [ebp+70h+var_41C]
0048CD76  mov     byte ptr [ebp+70h+var_74], 35h ; '5'
0048CD7A  mov     edx, [esi]
0048CD7C  mov     edx, [edx+40h]
0048CD7F  push    eax
0048CD80  lea     ecx, [ebp+70h+var_960]
0048CD86  push    ecx
0048CD87  mov     ecx, esi
0048CD89  call    edx
0048CD8B  mov     ecx, eax
0048CD8D  call    sub_5B05A0
0048CD92  lea     ecx, [ebp+70h+var_960]
0048CD98  mov     dword_7530EC, eax
0048CD9D  call    sub_5B0440
0048CDA2  mov     byte ptr [ebp+70h+var_74], 0
0048CDA6  cmp     [ebp+70h+var_404], edi
0048CDAC  jb      short loc_48CDBD
0048CDAE  mov     eax, [ebp+70h+var_418]
0048CDB4  push    eax; void *
0048CDB5  call    ??3@YAXPAX@Z; operator delete(void *)
0048CDBA  add     esp, 4
0048CDBD  push    offset aImageIceCap; "IMAGE_ICE_CAP"
0048CDC2  lea     ecx, [ebp+70h+var_454]
0048CDC8  mov     [ebp+70h+var_404], ebx
0048CDCE  mov     [ebp+70h+var_408], 0
0048CDD8  mov     byte ptr [ebp+70h+var_418], 0
0048CDDF  call    sub_4033F0
0048CDE4  lea     eax, [ebp+70h+var_454]
0048CDEA  mov     byte ptr [ebp+70h+var_74], 36h ; '6'
0048CDEE  mov     edx, [esi]
0048CDF0  mov     edx, [edx+40h]
0048CDF3  push    eax
0048CDF4  lea     ecx, [ebp+70h+var_978]
0048CDFA  push    ecx
0048CDFB  mov     ecx, esi
0048CDFD  call    edx
0048CDFF  mov     ecx, eax
0048CE01  call    sub_5B05A0
0048CE06  lea     ecx, [ebp+70h+var_978]
0048CE0C  mov     dword_7535EC, eax
0048CE11  call    sub_5B0440
0048CE16  mov     byte ptr [ebp+70h+var_74], 0
0048CE1A  cmp     [ebp+70h+var_43C], edi
0048CE20  jb      short loc_48CE31
0048CE22  mov     eax, [ebp+70h+var_450]
0048CE28  push    eax; void *
0048CE29  call    ??3@YAXPAX@Z; operator delete(void *)
0048CE2E  add     esp, 4
0048CE31  push    offset aImageIceSparkl; "IMAGE_ICE_SPARKLES"
0048CE36  lea     ecx, [ebp+70h+var_48C]
0048CE3C  mov     [ebp+70h+var_43C], ebx
0048CE42  mov     [ebp+70h+var_440], 0
0048CE4C  mov     byte ptr [ebp+70h+var_450], 0
0048CE53  call    sub_4033F0
0048CE58  lea     eax, [ebp+70h+var_48C]
0048CE5E  mov     byte ptr [ebp+70h+var_74], 37h ; '7'
0048CE62  mov     edx, [esi]
0048CE64  mov     edx, [edx+40h]
0048CE67  push    eax
0048CE68  lea     ecx, [ebp+70h+var_990]
0048CE6E  push    ecx
0048CE6F  mov     ecx, esi
0048CE71  call    edx
0048CE73  mov     ecx, eax
0048CE75  call    sub_5B05A0
0048CE7A  lea     ecx, [ebp+70h+var_990]
0048CE80  mov     dword_75313C, eax
0048CE85  call    sub_5B0440
0048CE8A  mov     byte ptr [ebp+70h+var_74], 0
0048CE8E  cmp     [ebp+70h+var_474], edi
0048CE94  jb      short loc_48CEA5
0048CE96  mov     eax, [ebp+70h+var_488]
0048CE9C  push    eax; void *
0048CE9D  call    ??3@YAXPAX@Z; operator delete(void *)
0048CEA2  add     esp, 4
0048CEA5  push    offset aImageIconPoolc; "IMAGE_ICON_POOLCLEANER"
0048CEAA  lea     ecx, [ebp+70h+var_4C4]
0048CEB0  mov     [ebp+70h+var_474], ebx
0048CEB6  mov     [ebp+70h+var_478], 0
0048CEC0  mov     byte ptr [ebp+70h+var_488], 0
0048CEC7  call    sub_4033F0
0048CECC  lea     eax, [ebp+70h+var_4C4]
0048CED2  mov     byte ptr [ebp+70h+var_74], 38h ; '8'
0048CED6  mov     edx, [esi]
0048CED8  mov     edx, [edx+40h]
0048CEDB  push    eax
0048CEDC  lea     ecx, [ebp+70h+var_9A8]
0048CEE2  push    ecx
0048CEE3  mov     ecx, esi
0048CEE5  call    edx
0048CEE7  mov     ecx, eax
0048CEE9  call    sub_5B05A0
0048CEEE  lea     ecx, [ebp+70h+var_9A8]
0048CEF4  mov     dword_752CAC, eax
0048CEF9  call    sub_5B0440
0048CEFE  mov     byte ptr [ebp+70h+var_74], 0
0048CF02  cmp     [ebp+70h+var_4AC], edi
0048CF08  jb      short loc_48CF19
0048CF0A  mov     eax, [ebp+70h+var_4C0]
0048CF10  push    eax; void *
0048CF11  call    ??3@YAXPAX@Z; operator delete(void *)
0048CF16  add     esp, 4
0048CF19  push    offset aImageIconRake; "IMAGE_ICON_RAKE"
0048CF1E  lea     ecx, [ebp+70h+var_4FC]
0048CF24  mov     [ebp+70h+var_4AC], ebx
0048CF2A  mov     [ebp+70h+var_4B0], 0
0048CF34  mov     byte ptr [ebp+70h+var_4C0], 0
0048CF3B  call    sub_4033F0
0048CF40  lea     eax, [ebp+70h+var_4FC]
0048CF46  mov     byte ptr [ebp+70h+var_74], 39h ; '9'
0048CF4A  mov     edx, [esi]
0048CF4C  mov     edx, [edx+40h]
0048CF4F  push    eax
0048CF50  lea     ecx, [ebp+70h+var_9C0]
0048CF56  push    ecx
0048CF57  mov     ecx, esi
0048CF59  call    edx
0048CF5B  mov     ecx, eax
0048CF5D  call    sub_5B05A0
0048CF62  lea     ecx, [ebp+70h+var_9C0]
0048CF68  mov     dword_7533F4, eax
0048CF6D  call    sub_5B0440
0048CF72  mov     byte ptr [ebp+70h+var_74], 0
0048CF76  cmp     [ebp+70h+var_4E4], edi
0048CF7C  jb      short loc_48CF8D
0048CF7E  mov     eax, [ebp+70h+var_4F8]
0048CF84  push    eax; void *
0048CF85  call    ??3@YAXPAX@Z; operator delete(void *)
0048CF8A  add     esp, 4
0048CF8D  push    offset aImageIconRoofc; "IMAGE_ICON_ROOFCLEANER"
0048CF92  lea     ecx, [ebp+70h+var_534]
0048CF98  mov     [ebp+70h+var_4E4], ebx
0048CF9E  mov     [ebp+70h+var_4E8], 0
0048CFA8  mov     byte ptr [ebp+70h+var_4F8], 0
0048CFAF  call    sub_4033F0
0048CFB4  lea     eax, [ebp+70h+var_534]
0048CFBA  mov     byte ptr [ebp+70h+var_74], 3Ah ; ':'
0048CFBE  mov     edx, [esi]
0048CFC0  mov     edx, [edx+40h]
0048CFC3  push    eax
0048CFC4  lea     ecx, [ebp+70h+var_9D8]
0048CFCA  push    ecx
0048CFCB  mov     ecx, esi
0048CFCD  call    edx
0048CFCF  mov     ecx, eax
0048CFD1  call    sub_5B05A0
0048CFD6  lea     ecx, [ebp+70h+var_9D8]
0048CFDC  mov     dword_7535FC, eax
0048CFE1  call    sub_5B0440
0048CFE6  mov     byte ptr [ebp+70h+var_74], 0
0048CFEA  cmp     [ebp+70h+var_51C], edi
0048CFF0  jb      short loc_48D001
0048CFF2  mov     eax, [ebp+70h+var_530]
0048CFF8  push    eax; void *
0048CFF9  call    ??3@YAXPAX@Z; operator delete(void *)
0048CFFE  add     esp, 4
0048D001  push    offset aImageImitaterc; "IMAGE_IMITATERCLOUDS"
0048D006  lea     ecx, [ebp+70h+var_56C]
0048D00C  mov     [ebp+70h+var_51C], ebx
0048D012  mov     [ebp+70h+var_520], 0
0048D01C  mov     byte ptr [ebp+70h+var_530], 0
0048D023  call    sub_4033F0
0048D028  lea     eax, [ebp+70h+var_56C]
0048D02E  mov     byte ptr [ebp+70h+var_74], 3Bh ; ';'
0048D032  mov     edx, [esi]
0048D034  mov     edx, [edx+40h]
0048D037  push    eax
0048D038  lea     ecx, [ebp+70h+var_9F0]
0048D03E  push    ecx
0048D03F  mov     ecx, esi
0048D041  call    edx
0048D043  mov     ecx, eax
0048D045  call    sub_5B05A0
0048D04A  lea     ecx, [ebp+70h+var_9F0]
0048D050  mov     dword_7532A4, eax
0048D055  call    sub_5B0440
0048D05A  mov     byte ptr [ebp+70h+var_74], 0
0048D05E  cmp     [ebp+70h+var_554], edi
0048D064  jb      short loc_48D075
0048D066  mov     eax, [ebp+70h+var_568]
0048D06C  push    eax; void *
0048D06D  call    ??3@YAXPAX@Z; operator delete(void *)
0048D072  add     esp, 4
0048D075  push    offset aImageImitaterp; "IMAGE_IMITATERPUFFS"
0048D07A  lea     ecx, [ebp+70h+var_5A4]
0048D080  mov     [ebp+70h+var_554], ebx
0048D086  mov     [ebp+70h+var_558], 0
0048D090  mov     byte ptr [ebp+70h+var_568], 0
0048D097  call    sub_4033F0
0048D09C  lea     eax, [ebp+70h+var_5A4]
0048D0A2  mov     byte ptr [ebp+70h+var_74], 3Ch ; '<'
0048D0A6  mov     edx, [esi]
0048D0A8  mov     edx, [edx+40h]
0048D0AB  push    eax
0048D0AC  lea     ecx, [ebp+70h+var_A08]
0048D0B2  push    ecx
0048D0B3  mov     ecx, esi
0048D0B5  call    edx
0048D0B7  mov     ecx, eax
0048D0B9  call    sub_5B05A0
0048D0BE  lea     ecx, [ebp+70h+var_A08]
0048D0C4  mov     dword_753110, eax
0048D0C9  call    sub_5B0440
0048D0CE  mov     byte ptr [ebp+70h+var_74], 0
0048D0D2  cmp     [ebp+70h+var_58C], edi
0048D0D8  jb      short loc_48D0E9
0048D0DA  mov     eax, [ebp+70h+var_5A0]
0048D0E0  push    eax; void *
0048D0E1  call    ??3@YAXPAX@Z; operator delete(void *)
0048D0E6  add     esp, 4
0048D0E9  push    offset aImageImitaters; "IMAGE_IMITATERSEED"
0048D0EE  lea     ecx, [ebp+70h+var_5DC]
0048D0F4  mov     [ebp+70h+var_58C], ebx
0048D0FA  mov     [ebp+70h+var_590], 0
0048D104  mov     byte ptr [ebp+70h+var_5A0], 0
0048D10B  call    sub_4033F0
0048D110  lea     eax, [ebp+70h+var_5DC]
0048D116  mov     byte ptr [ebp+70h+var_74], 3Dh ; '='
0048D11A  mov     edx, [esi]
0048D11C  mov     edx, [edx+40h]
0048D11F  push    eax
0048D120  lea     ecx, [ebp+70h+var_A20]
0048D126  push    ecx
0048D127  mov     ecx, esi
0048D129  call    edx
0048D12B  mov     ecx, eax
0048D12D  call    sub_5B05A0
0048D132  lea     ecx, [ebp+70h+var_A20]
0048D138  mov     dword_752D2C, eax
0048D13D  call    sub_5B0440
0048D142  mov     byte ptr [ebp+70h+var_74], 0
0048D146  cmp     [ebp+70h+var_5C4], edi
0048D14C  jb      short loc_48D15D
0048D14E  mov     eax, [ebp+70h+var_5D8]
0048D154  push    eax; void *
0048D155  call    ??3@YAXPAX@Z; operator delete(void *)
0048D15A  add     esp, 4
0048D15D  push    offset aImageImitaters_0; "IMAGE_IMITATERSEEDDISABLED"
0048D162  lea     ecx, [ebp+70h+var_614]
0048D168  mov     [ebp+70h+var_5C4], ebx
0048D16E  mov     [ebp+70h+var_5C8], 0
0048D178  mov     byte ptr [ebp+70h+var_5D8], 0
0048D17F  call    sub_4033F0
0048D184  lea     eax, [ebp+70h+var_614]
0048D18A  mov     byte ptr [ebp+70h+var_74], 3Eh ; '>'
0048D18E  mov     edx, [esi]
0048D190  mov     edx, [edx+40h]
0048D193  push    eax
0048D194  lea     ecx, [ebp+70h+var_A38]
0048D19A  push    ecx
0048D19B  mov     ecx, esi
0048D19D  call    edx
0048D19F  mov     ecx, eax
0048D1A1  call    sub_5B05A0
0048D1A6  lea     ecx, [ebp+70h+var_A38]
0048D1AC  mov     dword_752AD8, eax
0048D1B1  call    sub_5B0440
0048D1B6  mov     byte ptr [ebp+70h+var_74], 0
0048D1BA  cmp     [ebp+70h+var_5FC], edi
0048D1C0  jb      short loc_48D1D1
0048D1C2  mov     eax, [ebp+70h+var_610]
0048D1C8  push    eax; void *
0048D1C9  call    ??3@YAXPAX@Z; operator delete(void *)
0048D1CE  add     esp, 4
0048D1D1  push    offset aImageMelonpult; "IMAGE_MELONPULT_PARTICLES"
0048D1D6  lea     ecx, [ebp+70h+var_64C]
0048D1DC  mov     [ebp+70h+var_5FC], ebx
0048D1E2  mov     [ebp+70h+var_600], 0
0048D1EC  mov     byte ptr [ebp+70h+var_610], 0
0048D1F3  call    sub_4033F0
0048D1F8  lea     eax, [ebp+70h+var_64C]
0048D1FE  mov     byte ptr [ebp+70h+var_74], 3Fh ; '?'
0048D202  mov     edx, [esi]
0048D204  mov     edx, [edx+40h]
0048D207  push    eax
0048D208  lea     ecx, [ebp+70h+var_A50]
0048D20E  push    ecx
0048D20F  mov     ecx, esi
0048D211  call    edx
0048D213  mov     ecx, eax
0048D215  call    sub_5B05A0
0048D21A  lea     ecx, [ebp+70h+var_A50]
0048D220  mov     dword_752D7C, eax
0048D225  call    sub_5B0440
0048D22A  mov     byte ptr [ebp+70h+var_74], 0
0048D22E  cmp     [ebp+70h+var_634], edi
0048D234  jb      short loc_48D245
0048D236  mov     eax, [ebp+70h+var_648]
0048D23C  push    eax; void *
0048D23D  call    ??3@YAXPAX@Z; operator delete(void *)
0048D242  add     esp, 4
0048D245  push    offset aImageMinigameT; "IMAGE_MINIGAME_TROPHY"
0048D24A  lea     ecx, [ebp+70h+var_684]
0048D250  mov     [ebp+70h+var_634], ebx
0048D256  mov     [ebp+70h+var_638], 0
0048D260  mov     byte ptr [ebp+70h+var_648], 0
0048D267  call    sub_4033F0
0048D26C  lea     eax, [ebp+70h+var_684]
0048D272  mov     byte ptr [ebp+70h+var_74], 40h ; '@'
0048D276  mov     edx, [esi]
0048D278  mov     edx, [edx+40h]
0048D27B  push    eax
0048D27C  lea     ecx, [ebp+70h+var_A68]
0048D282  push    ecx
0048D283  mov     ecx, esi
0048D285  call    edx
0048D287  mov     ecx, eax
0048D289  call    sub_5B05A0
0048D28E  lea     ecx, [ebp+70h+var_A68]
0048D294  mov     dword_753424, eax
0048D299  call    sub_5B0440
0048D29E  mov     byte ptr [ebp+70h+var_74], 0
0048D2A2  cmp     [ebp+70h+var_66C], edi
0048D2A8  jb      short loc_48D2B9
0048D2AA  mov     eax, [ebp+70h+var_680]
0048D2B0  push    eax; void *
0048D2B1  call    ??3@YAXPAX@Z; operator delete(void *)
0048D2B6  add     esp, 4
0048D2B9  push    offset aImageMoneybag; "IMAGE_MONEYBAG"
0048D2BE  lea     ecx, [ebp+70h+var_6BC]
0048D2C4  mov     [ebp+70h+var_66C], ebx
0048D2CA  mov     [ebp+70h+var_670], 0
0048D2D4  mov     byte ptr [ebp+70h+var_680], 0
0048D2DB  call    sub_4033F0
0048D2E0  lea     eax, [ebp+70h+var_6BC]
0048D2E6  mov     byte ptr [ebp+70h+var_74], 41h ; 'A'
0048D2EA  mov     edx, [esi]
0048D2EC  mov     edx, [edx+40h]
0048D2EF  push    eax
0048D2F0  lea     ecx, [ebp+70h+var_A80]
0048D2F6  push    ecx
0048D2F7  mov     ecx, esi
0048D2F9  call    edx
0048D2FB  mov     ecx, eax
0048D2FD  call    sub_5B05A0
0048D302  lea     ecx, [ebp+70h+var_A80]
0048D308  mov     dword_753380, eax
0048D30D  call    sub_5B0440
0048D312  mov     byte ptr [ebp+70h+var_74], 0
0048D316  cmp     [ebp+70h+var_6A4], edi
0048D31C  jb      short loc_48D32D
0048D31E  mov     eax, [ebp+70h+var_6B8]
0048D324  push    eax; void *
0048D325  call    ??3@YAXPAX@Z; operator delete(void *)
0048D32A  add     esp, 4
0048D32D  push    offset aImageMoneybagH; "IMAGE_MONEYBAG_HI_RES"
0048D332  lea     ecx, [ebp+70h+var_6F4]
0048D338  mov     [ebp+70h+var_6A4], ebx
0048D33E  mov     [ebp+70h+var_6A8], 0
0048D348  mov     byte ptr [ebp+70h+var_6B8], 0
0048D34F  call    sub_4033F0
0048D354  lea     eax, [ebp+70h+var_6F4]
0048D35A  mov     byte ptr [ebp+70h+var_74], 42h ; 'B'
0048D35E  mov     edx, [esi]
0048D360  mov     edx, [edx+40h]
0048D363  push    eax
0048D364  lea     ecx, [ebp+70h+var_A98]
0048D36A  push    ecx
0048D36B  mov     ecx, esi
0048D36D  call    edx
0048D36F  mov     ecx, eax
0048D371  call    sub_5B05A0
0048D376  lea     ecx, [ebp+70h+var_A98]
0048D37C  mov     dword_7532B8, eax
0048D381  call    sub_5B0440
0048D386  mov     byte ptr [ebp+70h+var_74], 0
0048D38A  cmp     [ebp+70h+var_6DC], edi
0048D390  jb      short loc_48D3A1
0048D392  mov     eax, [ebp+70h+var_6F0]
0048D398  push    eax; void *
0048D399  call    ??3@YAXPAX@Z; operator delete(void *)
0048D39E  add     esp, 4
0048D3A1  push    offset aImageNightGrav; "IMAGE_NIGHT_GRAVE_GRAPHIC"
0048D3A6  lea     ecx, [ebp+70h+var_72C]
0048D3AC  mov     [ebp+70h+var_6DC], ebx
0048D3B2  mov     [ebp+70h+var_6E0], 0
0048D3BC  mov     byte ptr [ebp+70h+var_6F0], 0
0048D3C3  call    sub_4033F0
0048D3C8  lea     eax, [ebp+70h+var_72C]
0048D3CE  mov     byte ptr [ebp+70h+var_74], 43h ; 'C'
0048D3D2  mov     edx, [esi]
0048D3D4  mov     edx, [edx+40h]
0048D3D7  push    eax
0048D3D8  lea     ecx, [ebp+70h+var_AB0]
0048D3DE  push    ecx
0048D3DF  mov     ecx, esi
0048D3E1  call    edx
0048D3E3  mov     ecx, eax
0048D3E5  call    sub_5B05A0
0048D3EA  lea     ecx, [ebp+70h+var_AB0]
0048D3F0  mov     dword_752FC0, eax
0048D3F5  call    sub_5B0440
0048D3FA  mov     byte ptr [ebp+70h+var_74], 0
0048D3FE  cmp     [ebp+70h+var_714], edi
0048D404  jb      short loc_48D415
0048D406  mov     eax, [ebp+70h+var_728]
0048D40C  push    eax; void *
0048D40D  call    ??3@YAXPAX@Z; operator delete(void *)
0048D412  add     esp, 4
0048D415  push    offset aImageOptionsBa; "IMAGE_OPTIONS_BACKTOGAMEBUTTON0"
0048D41A  lea     ecx, [ebp+70h+var_764]
0048D420  mov     [ebp+70h+var_714], ebx
0048D426  mov     [ebp+70h+var_718], 0
0048D430  mov     byte ptr [ebp+70h+var_728], 0
0048D437  call    sub_4033F0
0048D43C  lea     eax, [ebp+70h+var_764]
0048D442  mov     byte ptr [ebp+70h+var_74], 44h ; 'D'
0048D446  mov     edx, [esi]
0048D448  mov     edx, [edx+40h]
0048D44B  push    eax
0048D44C  lea     ecx, [ebp+70h+var_AC8]
0048D452  push    ecx
0048D453  mov     ecx, esi
0048D455  call    edx
0048D457  mov     ecx, eax
0048D459  call    sub_5B05A0
0048D45E  lea     ecx, [ebp+70h+var_AC8]
0048D464  mov     dword_753094, eax
0048D469  call    sub_5B0440
0048D46E  mov     byte ptr [ebp+70h+var_74], 0
0048D472  cmp     [ebp+70h+var_74C], edi
0048D478  jb      short loc_48D489
0048D47A  mov     eax, [ebp+70h+var_760]
0048D480  push    eax; void *
0048D481  call    ??3@YAXPAX@Z; operator delete(void *)
0048D486  add     esp, 4
0048D489  push    offset aImageOptionsBa_0; "IMAGE_OPTIONS_BACKTOGAMEBUTTON2"
0048D48E  lea     ecx, [ebp+70h+var_79C]
0048D494  mov     [ebp+70h+var_74C], ebx
0048D49A  mov     [ebp+70h+var_750], 0
0048D4A4  mov     byte ptr [ebp+70h+var_760], 0
0048D4AB  call    sub_4033F0
0048D4B0  lea     eax, [ebp+70h+var_79C]
0048D4B6  mov     byte ptr [ebp+70h+var_74], 45h ; 'E'
0048D4BA  mov     edx, [esi]
0048D4BC  mov     edx, [edx+40h]
0048D4BF  push    eax
0048D4C0  lea     ecx, [ebp+70h+var_AE0]
0048D4C6  push    ecx
0048D4C7  mov     ecx, esi
0048D4C9  call    edx
0048D4CB  mov     ecx, eax
0048D4CD  call    sub_5B05A0
0048D4D2  lea     ecx, [ebp+70h+var_AE0]
0048D4D8  mov     dword_753454, eax
0048D4DD  call    sub_5B0440
0048D4E2  mov     byte ptr [ebp+70h+var_74], 0
0048D4E6  cmp     [ebp+70h+var_784], edi
0048D4EC  jb      short loc_48D4FD
0048D4EE  mov     eax, [ebp+70h+var_798]
0048D4F4  push    eax; void *
0048D4F5  call    ??3@YAXPAX@Z; operator delete(void *)
0048D4FA  add     esp, 4
0048D4FD  push    offset aImageOptionsCh; "IMAGE_OPTIONS_CHECKBOX0"
0048D502  lea     ecx, [ebp+70h+var_7D4]
0048D508  mov     [ebp+70h+var_784], ebx
0048D50E  mov     [ebp+70h+var_788], 0
0048D518  mov     byte ptr [ebp+70h+var_798], 0
0048D51F  call    sub_4033F0
0048D524  lea     eax, [ebp+70h+var_7D4]
0048D52A  mov     byte ptr [ebp+70h+var_74], 46h ; 'F'
0048D52E  mov     edx, [esi]
0048D530  mov     edx, [edx+40h]
0048D533  push    eax
0048D534  lea     ecx, [ebp+70h+var_AF8]
0048D53A  push    ecx
0048D53B  mov     ecx, esi
0048D53D  call    edx
0048D53F  mov     ecx, eax
0048D541  call    sub_5B05A0
0048D546  lea     ecx, [ebp+70h+var_AF8]
0048D54C  mov     dword_75382C, eax
0048D551  call    sub_5B0440
0048D556  mov     byte ptr [ebp+70h+var_74], 0
0048D55A  cmp     [ebp+70h+var_7BC], edi
0048D560  jb      short loc_48D571
0048D562  mov     eax, [ebp+70h+var_7D0]
0048D568  push    eax; void *
0048D569  call    ??3@YAXPAX@Z; operator delete(void *)
0048D56E  add     esp, 4
0048D571  push    offset aImageOptionsCh_0; "IMAGE_OPTIONS_CHECKBOX1"
0048D576  lea     ecx, [ebp+70h+var_5F8]
0048D57C  mov     [ebp+70h+var_7BC], ebx
0048D582  mov     [ebp+70h+var_7C0], 0
0048D58C  mov     byte ptr [ebp+70h+var_7D0], 0
0048D593  call    sub_4033F0
0048D598  lea     eax, [ebp+70h+var_5F8]
0048D59E  mov     byte ptr [ebp+70h+var_74], 47h ; 'G'
0048D5A2  mov     edx, [esi]
0048D5A4  mov     edx, [edx+40h]
0048D5A7  push    eax
0048D5A8  lea     ecx, [ebp+70h+var_B10]
0048D5AE  push    ecx
0048D5AF  mov     ecx, esi
0048D5B1  call    edx
0048D5B3  mov     ecx, eax
0048D5B5  call    sub_5B05A0
0048D5BA  lea     ecx, [ebp+70h+var_B10]
0048D5C0  mov     dword_752A98, eax
0048D5C5  call    sub_5B0440
0048D5CA  mov     byte ptr [ebp+70h+var_74], 0
0048D5CE  cmp     [ebp+70h+var_5E0], edi
0048D5D4  jb      short loc_48D5E5
0048D5D6  mov     eax, [ebp+70h+var_5F4]
0048D5DC  push    eax; void *
0048D5DD  call    ??3@YAXPAX@Z; operator delete(void *)
0048D5E2  add     esp, 4
0048D5E5  xor     edi, edi
0048D5E7  push    offset aImageOptionsMe; "IMAGE_OPTIONS_MENUBACK"
0048D5EC  lea     ecx, [ebp+70h+var_1948]
0048D5F2  mov     [ebp+70h+var_5E0], ebx
0048D5F8  mov     [ebp+70h+var_5E4], edi
0048D5FE  mov     byte ptr [ebp+70h+var_5F4], 0
0048D605  call    sub_4033F0
0048D60A  lea     eax, [ebp+70h+var_1948]
0048D610  mov     byte ptr [ebp+70h+var_74], 48h ; 'H'
0048D614  mov     edx, [esi]
0048D616  mov     edx, [edx+40h]
0048D619  push    eax
0048D61A  lea     ecx, [ebp+70h+var_B28]
0048D620  push    ecx
0048D621  mov     ecx, esi
0048D623  call    edx
0048D625  mov     ecx, eax
0048D627  call    sub_5B05A0
0048D62C  lea     ecx, [ebp+70h+var_B28]
0048D632  mov     dword_752CC8, eax
0048D637  call    sub_5B0440
0048D63C  lea     ecx, [ebp+70h+var_1948]; void *
0048D642  mov     byte ptr [ebp+70h+var_74], 0
0048D646  call    sub_401E90
0048D64B  push    offset aImageOptionsSl; "IMAGE_OPTIONS_SLIDERKNOB2"
0048D650  lea     ecx, [ebp+70h+var_35D4]
0048D656  call    sub_4033F0
0048D65B  mov     byte ptr [ebp+70h+var_74], 49h ; 'I'
0048D65F  mov     eax, [esi]
0048D661  mov     eax, [eax+40h]
0048D664  lea     ecx, [ebp+70h+var_35D4]
0048D66A  push    ecx
0048D66B  lea     edx, [ebp+70h+var_15E4]
0048D671  push    edx
0048D672  mov     ecx, esi
0048D674  call    eax
0048D676  mov     ecx, eax
0048D678  call    sub_5B05A0
0048D67D  lea     ecx, [ebp+70h+var_15E4]
0048D683  mov     dword_753710, eax
0048D688  call    sub_5B0440
0048D68D  lea     ecx, [ebp+70h+var_35D4]; void *
0048D693  mov     byte ptr [ebp+70h+var_74], 0
0048D697  call    sub_401E90
0048D69C  push    offset aImageOptionsSl_0; "IMAGE_OPTIONS_SLIDERSLOT"
0048D6A1  lea     ecx, [ebp+70h+var_199C]
0048D6A7  call    sub_4033F0
0048D6AC  lea     eax, [ebp+70h+var_199C]
0048D6B2  mov     byte ptr [ebp+70h+var_74], 4Ah ; 'J'
0048D6B6  mov     edx, [esi]
0048D6B8  mov     edx, [edx+40h]
0048D6BB  push    eax
0048D6BC  lea     ecx, [ebp+70h+var_804]
0048D6C2  push    ecx
0048D6C3  mov     ecx, esi
0048D6C5  call    edx
0048D6C7  mov     ecx, eax
0048D6C9  call    sub_5B05A0
0048D6CE  lea     ecx, [ebp+70h+var_804]
0048D6D4  mov     dword_753420, eax
0048D6D9  call    sub_5B0440
0048D6DE  lea     ecx, [ebp+70h+var_199C]; void *
0048D6E4  mov     byte ptr [ebp+70h+var_74], 0
0048D6E8  call    sub_401E90
0048D6ED  push    offset aImagePacketPla; "IMAGE_PACKET_PLANTS"
0048D6F2  lea     ecx, [ebp+70h+var_29CC]
0048D6F8  call    sub_4033F0
0048D6FD  mov     byte ptr [ebp+70h+var_74], 4Bh ; 'K'
0048D701  mov     eax, [esi]
0048D703  mov     eax, [eax+40h]
0048D706  lea     ecx, [ebp+70h+var_29CC]
0048D70C  push    ecx
0048D70D  lea     edx, [ebp+70h+var_15FC]
0048D713  push    edx
0048D714  mov     ecx, esi
0048D716  call    eax
0048D718  mov     ecx, eax
0048D71A  call    sub_5B05A0
0048D71F  lea     ecx, [ebp+70h+var_15FC]
0048D725  mov     dword_752AE4, eax
0048D72A  call    sub_5B0440
0048D72F  lea     ecx, [ebp+70h+var_29CC]; void *
0048D735  mov     byte ptr [ebp+70h+var_74], 0
0048D739  call    sub_401E90
0048D73E  push    offset aImagePeaPartic; "IMAGE_PEA_PARTICLES"
0048D743  lea     ecx, [ebp+70h+var_19D4]
0048D749  call    sub_4033F0
0048D74E  lea     eax, [ebp+70h+var_19D4]
0048D754  mov     byte ptr [ebp+70h+var_74], 4Ch ; 'L'
0048D758  mov     edx, [esi]
0048D75A  mov     edx, [edx+40h]
0048D75D  push    eax
0048D75E  lea     ecx, [ebp+70h+var_81C]
0048D764  push    ecx
0048D765  mov     ecx, esi
0048D767  call    edx
0048D769  mov     ecx, eax
0048D76B  call    sub_5B05A0
0048D770  lea     ecx, [ebp+70h+var_81C]
0048D776  mov     dword_75323C, eax
0048D77B  call    sub_5B0440
0048D780  lea     ecx, [ebp+70h+var_19D4]; void *
0048D786  mov     byte ptr [ebp+70h+var_74], 0
0048D78A  call    sub_401E90
0048D78F  push    offset aImagePeaShadow; "IMAGE_PEA_SHADOWS"
0048D794  lea     ecx, [ebp+70h+var_31E4]
0048D79A  call    sub_4033F0
0048D79F  mov     byte ptr [ebp+70h+var_74], 4Dh ; 'M'
0048D7A3  mov     eax, [esi]
0048D7A5  mov     eax, [eax+40h]
0048D7A8  lea     ecx, [ebp+70h+var_31E4]
0048D7AE  push    ecx
0048D7AF  lea     edx, [ebp+70h+var_1614]
0048D7B5  push    edx
0048D7B6  mov     ecx, esi
0048D7B8  call    eax
0048D7BA  mov     ecx, eax
0048D7BC  call    sub_5B05A0
0048D7C1  lea     ecx, [ebp+70h+var_1614]
0048D7C7  mov     dword_753784, eax
0048D7CC  call    sub_5B0440
0048D7D1  lea     ecx, [ebp+70h+var_31E4]; void *
0048D7D7  mov     byte ptr [ebp+70h+var_74], 0
0048D7DB  call    sub_401E90
0048D7E0  push    offset aImagePeaSplats; "IMAGE_PEA_SPLATS"
0048D7E5  lea     ecx, [ebp+70h+var_1A0C]
0048D7EB  call    sub_4033F0
0048D7F0  lea     eax, [ebp+70h+var_1A0C]
0048D7F6  mov     byte ptr [ebp+70h+var_74], 4Eh ; 'N'
0048D7FA  mov     edx, [esi]
0048D7FC  mov     edx, [edx+40h]
0048D7FF  push    eax
0048D800  lea     ecx, [ebp+70h+var_834]
0048D806  push    ecx
0048D807  mov     ecx, esi
0048D809  call    edx
0048D80B  mov     ecx, eax
0048D80D  call    sub_5B05A0
0048D812  lea     ecx, [ebp+70h+var_834]
0048D818  mov     dword_75380C, eax
0048D81D  call    sub_5B0440
0048D822  lea     ecx, [ebp+70h+var_1A0C]; void *
0048D828  mov     byte ptr [ebp+70h+var_74], 0
0048D82C  call    sub_401E90
0048D831  push    offset aImagePhonograp; "IMAGE_PHONOGRAPH"
0048D836  lea     ecx, [ebp+70h+var_2A04]
0048D83C  call    sub_4033F0
0048D841  lea     ecx, [ebp+70h+var_2A04]
0048D847  push    ecx
0048D848  lea     edx, [ebp+70h+var_162C]
0048D84E  mov     byte ptr [ebp+70h+var_74], 4Fh ; 'O'
0048D852  mov     eax, [esi]
0048D854  push    edx
0048D855  mov     ecx, esi
0048D857  mov     eax, [eax+40h]
0048D85A  call    eax
0048D85C  mov     ecx, eax
0048D85E  call    sub_5B05A0
0048D863  lea     ecx, [ebp+70h+var_162C]
0048D869  mov     dword_752E4C, eax
0048D86E  call    sub_5B0440
0048D873  lea     ecx, [ebp+70h+var_2A04]; void *
0048D879  mov     byte ptr [ebp+70h+var_74], 0
0048D87D  call    sub_401E90
0048D882  push    offset aImagePinata; "IMAGE_PINATA"
0048D887  lea     ecx, [ebp+70h+var_1A44]
0048D88D  call    sub_4033F0
0048D892  lea     eax, [ebp+70h+var_1A44]
0048D898  mov     byte ptr [ebp+70h+var_74], 50h ; 'P'
0048D89C  mov     edx, [esi]
0048D89E  mov     edx, [edx+40h]
0048D8A1  push    eax
0048D8A2  lea     ecx, [ebp+70h+var_84C]
0048D8A8  push    ecx
0048D8A9  mov     ecx, esi
0048D8AB  call    edx
0048D8AD  mov     ecx, eax
0048D8AF  call    sub_5B05A0
0048D8B4  lea     ecx, [ebp+70h+var_84C]
0048D8BA  mov     dword_752C30, eax
0048D8BF  call    sub_5B0440
0048D8C4  lea     ecx, [ebp+70h+var_1A44]; void *
0048D8CA  mov     byte ptr [ebp+70h+var_74], 0
0048D8CE  call    sub_401E90
0048D8D3  push    offset aImagePlantshad; "IMAGE_PLANTSHADOW"
0048D8D8  lea     ecx, [ebp+70h+var_38E4]
0048D8DE  call    sub_4033F0
0048D8E3  mov     byte ptr [ebp+70h+var_74], 51h ; 'Q'
0048D8E7  mov     eax, [esi]
0048D8E9  mov     eax, [eax+40h]
0048D8EC  lea     ecx, [ebp+70h+var_38E4]
0048D8F2  push    ecx
0048D8F3  lea     edx, [ebp+70h+var_1644]
0048D8F9  push    edx
0048D8FA  mov     ecx, esi
0048D8FC  call    eax
0048D8FE  mov     ecx, eax
0048D900  call    sub_5B05A0
0048D905  lea     ecx, [ebp+70h+var_1644]
0048D90B  mov     dword_7534D0, eax
0048D910  call    sub_5B0440
0048D915  lea     ecx, [ebp+70h+var_38E4]; void *
0048D91B  mov     byte ptr [ebp+70h+var_74], 0
0048D91F  call    sub_401E90
0048D924  push    offset aImagePlantshad_0; "IMAGE_PLANTSHADOW2"
0048D929  lea     ecx, [ebp+70h+var_1A7C]
0048D92F  call    sub_4033F0
0048D934  lea     eax, [ebp+70h+var_1A7C]
0048D93A  mov     byte ptr [ebp+70h+var_74], 52h ; 'R'
0048D93E  mov     edx, [esi]
0048D940  mov     edx, [edx+40h]
0048D943  push    eax
0048D944  lea     ecx, [ebp+70h+var_864]
0048D94A  push    ecx
0048D94B  mov     ecx, esi
0048D94D  call    edx
0048D94F  mov     ecx, eax
0048D951  call    sub_5B05A0
0048D956  lea     ecx, [ebp+70h+var_864]
0048D95C  mov     dword_752CFC, eax
0048D961  call    sub_5B0440
0048D966  lea     ecx, [ebp+70h+var_1A7C]; void *
0048D96C  mov     byte ptr [ebp+70h+var_74], 0
0048D970  call    sub_401E90
0048D975  push    offset aImagePlantspee; "IMAGE_PLANTSPEECHBUBBLE"
0048D97A  lea     ecx, [ebp+70h+var_2A3C]
0048D980  call    sub_4033F0
0048D985  lea     ecx, [ebp+70h+var_2A3C]
0048D98B  mov     byte ptr [ebp+70h+var_74], 53h ; 'S'
0048D98F  mov     eax, [esi]
0048D991  push    ecx
0048D992  mov     eax, [eax+40h]
0048D995  lea     edx, [ebp+70h+var_165C]
0048D99B  push    edx
0048D99C  mov     ecx, esi
0048D99E  call    eax
0048D9A0  mov     ecx, eax
0048D9A2  call    sub_5B05A0
0048D9A7  lea     ecx, [ebp+70h+var_165C]
0048D9AD  mov     dword_753754, eax
0048D9B2  call    sub_5B0440
0048D9B7  lea     ecx, [ebp+70h+var_2A3C]; void *
0048D9BD  mov     byte ptr [ebp+70h+var_74], 0
0048D9C1  call    sub_401E90
0048D9C6  push    offset aImagePool; "IMAGE_POOL"
0048D9CB  lea     ecx, [ebp+70h+var_1AB4]
0048D9D1  call    sub_4033F0
0048D9D6  lea     eax, [ebp+70h+var_1AB4]
0048D9DC  mov     byte ptr [ebp+70h+var_74], 54h ; 'T'
0048D9E0  mov     edx, [esi]
0048D9E2  mov     edx, [edx+40h]
0048D9E5  push    eax
0048D9E6  lea     ecx, [ebp+70h+var_87C]
0048D9EC  push    ecx
0048D9ED  mov     ecx, esi
0048D9EF  call    edx
0048D9F1  mov     ecx, eax
0048D9F3  call    sub_5B05A0
0048D9F8  lea     ecx, [ebp+70h+var_87C]
0048D9FE  mov     dword_752A94, eax
0048DA03  call    sub_5B0440
0048DA08  lea     ecx, [ebp+70h+var_1AB4]; void *
0048DA0E  mov     byte ptr [ebp+70h+var_74], 0
0048DA12  call    sub_401E90
0048DA17  push    offset aImagePoolspark; "IMAGE_POOLSPARKLY"
0048DA1C  lea     ecx, [ebp+70h+var_321C]
0048DA22  call    sub_4033F0
0048DA27  mov     byte ptr [ebp+70h+var_74], 55h ; 'U'
0048DA2B  mov     eax, [esi]
0048DA2D  mov     eax, [eax+40h]
0048DA30  lea     ecx, [ebp+70h+var_321C]
0048DA36  push    ecx
0048DA37  lea     edx, [ebp+70h+var_1674]
0048DA3D  push    edx
0048DA3E  mov     ecx, esi
0048DA40  call    eax
0048DA42  mov     ecx, eax
0048DA44  call    sub_5B05A0
0048DA49  lea     ecx, [ebp+70h+var_1674]
0048DA4F  mov     dword_753068, eax
0048DA54  call    sub_5B0440
0048DA59  lea     ecx, [ebp+70h+var_321C]; void *
0048DA5F  mov     byte ptr [ebp+70h+var_74], 0
0048DA63  call    sub_401E90
0048DA68  push    offset aImagePoolBase; "IMAGE_POOL_BASE"
0048DA6D  lea     ecx, [ebp+70h+var_1AEC]
0048DA73  call    sub_4033F0
0048DA78  lea     eax, [ebp+70h+var_1AEC]
0048DA7E  mov     byte ptr [ebp+70h+var_74], 56h ; 'V'
0048DA82  mov     edx, [esi]
0048DA84  mov     edx, [edx+40h]
0048DA87  push    eax
0048DA88  lea     ecx, [ebp+70h+var_894]
0048DA8E  push    ecx
0048DA8F  mov     ecx, esi
0048DA91  call    edx
0048DA93  mov     ecx, eax
0048DA95  call    sub_5B05A0
0048DA9A  lea     ecx, [ebp+70h+var_894]
0048DAA0  mov     dword_75311C, eax
0048DAA5  call    sub_5B0440
0048DAAA  lea     ecx, [ebp+70h+var_1AEC]; void *
0048DAB0  mov     byte ptr [ebp+70h+var_74], 0
0048DAB4  call    sub_401E90
0048DAB9  push    offset aImagePoolBaseN; "IMAGE_POOL_BASE_NIGHT"
0048DABE  lea     ecx, [ebp+70h+var_2A74]
0048DAC4  call    sub_4033F0
0048DAC9  mov     byte ptr [ebp+70h+var_74], 57h ; 'W'
0048DACD  mov     eax, [esi]
0048DACF  mov     eax, [eax+40h]
0048DAD2  lea     ecx, [ebp+70h+var_2A74]
0048DAD8  push    ecx
0048DAD9  lea     edx, [ebp+70h+var_168C]
0048DADF  push    edx
0048DAE0  mov     ecx, esi
0048DAE2  call    eax
0048DAE4  mov     ecx, eax
0048DAE6  call    sub_5B05A0
0048DAEB  lea     ecx, [ebp+70h+var_168C]
0048DAF1  mov     dword_752EB4, eax
0048DAF6  call    sub_5B0440
0048DAFB  lea     ecx, [ebp+70h+var_2A74]; void *
0048DB01  mov     byte ptr [ebp+70h+var_74], 0
0048DB05  call    sub_401E90
0048DB0A  push    offset aImagePoolCaust; "IMAGE_POOL_CAUSTIC_EFFECT"
0048DB0F  lea     ecx, [ebp+70h+var_1B24]
0048DB15  call    sub_4033F0
0048DB1A  lea     eax, [ebp+70h+var_1B24]
0048DB20  mov     byte ptr [ebp+70h+var_74], 58h ; 'X'
0048DB24  mov     edx, [esi]
0048DB26  mov     edx, [edx+40h]
0048DB29  push    eax
0048DB2A  lea     ecx, [ebp+70h+var_8AC]
0048DB30  push    ecx
0048DB31  mov     ecx, esi
0048DB33  call    edx
0048DB35  mov     ecx, eax
0048DB37  call    sub_5B05A0
0048DB3C  lea     ecx, [ebp+70h+var_8AC]
0048DB42  mov     dword_752E18, eax
0048DB47  call    sub_5B0440
0048DB4C  lea     ecx, [ebp+70h+var_1B24]; void *
0048DB52  mov     byte ptr [ebp+70h+var_74], 0
0048DB56  call    sub_401E90
0048DB5B  push    offset aImagePoolNight; "IMAGE_POOL_NIGHT"
0048DB60  lea     ecx, [ebp+70h+var_360C]
0048DB66  call    sub_4033F0
0048DB6B  mov     byte ptr [ebp+70h+var_74], 59h ; 'Y'
0048DB6F  mov     eax, [esi]
0048DB71  mov     eax, [eax+40h]
0048DB74  lea     ecx, [ebp+70h+var_360C]
0048DB7A  push    ecx
0048DB7B  lea     edx, [ebp+70h+var_16A4]
0048DB81  push    edx
0048DB82  mov     ecx, esi
0048DB84  call    eax
0048DB86  mov     ecx, eax
0048DB88  call    sub_5B05A0
0048DB8D  lea     ecx, [ebp+70h+var_16A4]
0048DB93  mov     dword_7530E8, eax
0048DB98  call    sub_5B0440
0048DB9D  lea     ecx, [ebp+70h+var_360C]; void *
0048DBA3  mov     byte ptr [ebp+70h+var_74], 0
0048DBA7  call    sub_401E90
0048DBAC  push    offset aImagePoolShadi; "IMAGE_POOL_SHADING"
0048DBB1  lea     ecx, [ebp+70h+var_1B5C]
0048DBB7  call    sub_4033F0
0048DBBC  lea     eax, [ebp+70h+var_1B5C]
0048DBC2  mov     byte ptr [ebp+70h+var_74], 5Ah ; 'Z'
0048DBC6  mov     edx, [esi]
0048DBC8  mov     edx, [edx+40h]
0048DBCB  push    eax
0048DBCC  lea     ecx, [ebp+70h+var_8C4]
0048DBD2  push    ecx
0048DBD3  mov     ecx, esi
0048DBD5  call    edx
0048DBD7  mov     ecx, eax
0048DBD9  call    sub_5B05A0
0048DBDE  lea     ecx, [ebp+70h+var_8C4]
0048DBE4  mov     dword_753194, eax
0048DBE9  call    sub_5B0440
0048DBEE  lea     ecx, [ebp+70h+var_1B5C]; void *
0048DBF4  mov     byte ptr [ebp+70h+var_74], 0
0048DBF8  call    sub_401E90
0048DBFD  push    offset aImagePoolShadi_0; "IMAGE_POOL_SHADING_NIGHT"
0048DC02  lea     ecx, [ebp+70h+var_2AAC]
0048DC08  call    sub_4033F0
0048DC0D  mov     byte ptr [ebp+70h+var_74], 5Bh ; '['
0048DC11  mov     eax, [esi]
0048DC13  mov     eax, [eax+40h]
0048DC16  lea     ecx, [ebp+70h+var_2AAC]
0048DC1C  push    ecx
0048DC1D  lea     edx, [ebp+70h+var_16BC]
0048DC23  push    edx
0048DC24  mov     ecx, esi
0048DC26  call    eax
0048DC28  mov     ecx, eax
0048DC2A  call    sub_5B05A0
0048DC2F  lea     ecx, [ebp+70h+var_16BC]
0048DC35  mov     dword_753638, eax
0048DC3A  call    sub_5B0440
0048DC3F  lea     ecx, [ebp+70h+var_2AAC]; void *
0048DC45  mov     byte ptr [ebp+70h+var_74], 0
0048DC49  call    sub_401E90
0048DC4E  push    offset aImagePotatomin; "IMAGE_POTATOMINE_PARTICLES"
0048DC53  lea     ecx, [ebp+70h+var_1B94]
0048DC59  call    sub_4033F0
0048DC5E  lea     eax, [ebp+70h+var_1B94]
0048DC64  mov     byte ptr [ebp+70h+var_74], 5Ch ; '\'
0048DC68  mov     edx, [esi]
0048DC6A  mov     edx, [edx+40h]
0048DC6D  push    eax
0048DC6E  lea     ecx, [ebp+70h+var_8DC]
0048DC74  push    ecx
0048DC75  mov     ecx, esi
0048DC77  call    edx
0048DC79  mov     ecx, eax
0048DC7B  call    sub_5B05A0
0048DC80  lea     ecx, [ebp+70h+var_8DC]
0048DC86  mov     dword_752DBC, eax
0048DC8B  call    sub_5B0440
0048DC90  lea     ecx, [ebp+70h+var_1B94]; void *
0048DC96  mov     byte ptr [ebp+70h+var_74], 0
0048DC9A  call    sub_401E90
0048DC9F  push    offset aImagePresent; "IMAGE_PRESENT"
0048DCA4  lea     ecx, [ebp+70h+var_3254]
0048DCAA  call    sub_4033F0
0048DCAF  mov     byte ptr [ebp+70h+var_74], 5Dh ; ']'
0048DCB3  mov     eax, [esi]
0048DCB5  mov     eax, [eax+40h]
0048DCB8  lea     ecx, [ebp+70h+var_3254]
0048DCBE  push    ecx
0048DCBF  lea     edx, [ebp+70h+var_16D4]
0048DCC5  push    edx
0048DCC6  mov     ecx, esi
0048DCC8  call    eax
0048DCCA  mov     ecx, eax
0048DCCC  call    sub_5B05A0
0048DCD1  lea     ecx, [ebp+70h+var_16D4]
0048DCD7  mov     dword_75367C, eax
0048DCDC  call    sub_5B0440
0048DCE1  lea     ecx, [ebp+70h+var_3254]; void *
0048DCE7  mov     byte ptr [ebp+70h+var_74], 0
0048DCEB  call    sub_401E90
0048DCF0  push    offset aImagePresentop; "IMAGE_PRESENTOPEN"
0048DCF5  lea     ecx, [ebp+70h+var_1BCC]
0048DCFB  call    sub_4033F0
0048DD00  lea     eax, [ebp+70h+var_1BCC]
0048DD06  mov     byte ptr [ebp+70h+var_74], 5Eh ; '^'
0048DD0A  mov     edx, [esi]
0048DD0C  mov     edx, [edx+40h]
0048DD0F  push    eax
0048DD10  lea     ecx, [ebp+70h+var_8F4]
0048DD16  push    ecx
0048DD17  mov     ecx, esi
0048DD19  call    edx
0048DD1B  mov     ecx, eax
0048DD1D  call    sub_5B05A0
0048DD22  lea     ecx, [ebp+70h+var_8F4]
0048DD28  mov     dword_753190, eax
0048DD2D  call    sub_5B0440
0048DD32  mov     byte ptr [ebp+70h+var_74], 0
0048DD36  lea     ecx, [ebp+70h+var_1BCC]; void *
0048DD3C  call    sub_401E90
0048DD41  push    offset aImageProjectil; "IMAGE_PROJECTILECACTUS"
0048DD46  lea     ecx, [ebp+70h+var_2AE4]
0048DD4C  call    sub_4033F0
0048DD51  mov     byte ptr [ebp+70h+var_74], 5Fh ; '_'
0048DD55  mov     eax, [esi]
0048DD57  mov     eax, [eax+40h]
0048DD5A  lea     ecx, [ebp+70h+var_2AE4]
0048DD60  push    ecx
0048DD61  lea     edx, [ebp+70h+var_16EC]
0048DD67  push    edx
0048DD68  mov     ecx, esi
0048DD6A  call    eax
0048DD6C  mov     ecx, eax
0048DD6E  call    sub_5B05A0
0048DD73  lea     ecx, [ebp+70h+var_16EC]
0048DD79  mov     dword_7531DC, eax
0048DD7E  call    sub_5B0440
0048DD83  lea     ecx, [ebp+70h+var_2AE4]; void *
0048DD89  mov     byte ptr [ebp+70h+var_74], 0
0048DD8D  call    sub_401E90
0048DD92  push    offset aImageProjectil_0; "IMAGE_PROJECTILEPEA"
0048DD97  lea     ecx, [ebp+70h+var_1C04]
0048DD9D  call    sub_4033F0
0048DDA2  lea     eax, [ebp+70h+var_1C04]
0048DDA8  mov     byte ptr [ebp+70h+var_74], 60h ; '`'
0048DDAC  mov     edx, [esi]
0048DDAE  mov     edx, [edx+40h]
0048DDB1  push    eax
0048DDB2  lea     ecx, [ebp+70h+var_90C]
0048DDB8  push    ecx
0048DDB9  mov     ecx, esi
0048DDBB  call    edx
0048DDBD  mov     ecx, eax
0048DDBF  call    sub_5B05A0
0048DDC4  lea     ecx, [ebp+70h+var_90C]
0048DDCA  mov     dword_7531B0, eax
0048DDCF  call    sub_5B0440
0048DDD4  lea     ecx, [ebp+70h+var_1C04]; void *
0048DDDA  mov     byte ptr [ebp+70h+var_74], 0
0048DDDE  call    sub_401E90
0048DDE3  push    offset aImageProjectil_1; "IMAGE_PROJECTILESNOWPEA"
0048DDE8  lea     ecx, [ebp+70h+var_3804]
0048DDEE  call    sub_4033F0
0048DDF3  mov     byte ptr [ebp+70h+var_74], 61h ; 'a'
0048DDF7  mov     eax, [esi]
0048DDF9  mov     eax, [eax+40h]
0048DDFC  lea     ecx, [ebp+70h+var_3804]
0048DE02  push    ecx
0048DE03  lea     edx, [ebp+70h+var_1704]
0048DE09  push    edx
0048DE0A  mov     ecx, esi
0048DE0C  call    eax
0048DE0E  mov     ecx, eax
0048DE10  call    sub_5B05A0
0048DE15  lea     ecx, [ebp+70h+var_1704]
0048DE1B  mov     dword_752DEC, eax
0048DE20  call    sub_5B0440
0048DE25  lea     ecx, [ebp+70h+var_3804]; void *
0048DE2B  mov     byte ptr [ebp+70h+var_74], 0
0048DE2F  call    sub_401E90
0048DE34  push    offset aImageProjectil_2; "IMAGE_PROJECTILE_STAR"
0048DE39  lea     ecx, [ebp+70h+var_1C3C]
0048DE3F  call    sub_4033F0
0048DE44  lea     eax, [ebp+70h+var_1C3C]
0048DE4A  mov     byte ptr [ebp+70h+var_74], 62h ; 'b'
0048DE4E  mov     edx, [esi]
0048DE50  mov     edx, [edx+40h]
0048DE53  push    eax
0048DE54  lea     ecx, [ebp+70h+var_924]
0048DE5A  push    ecx
0048DE5B  mov     ecx, esi
0048DE5D  call    edx
0048DE5F  mov     ecx, eax
0048DE61  call    sub_5B05A0
0048DE66  mov     dword_752F50, eax
0048DE6B  lea     ecx, [ebp+70h+var_924]
0048DE71  call    sub_5B0440
0048DE76  lea     ecx, [ebp+70h+var_1C3C]; void *
0048DE7C  mov     byte ptr [ebp+70h+var_74], 0
0048DE80  call    sub_401E90
0048DE85  push    offset aImagePuffshroo; "IMAGE_PUFFSHROOM_PUFF1"
0048DE8A  lea     ecx, [ebp+70h+var_2B1C]
0048DE90  call    sub_4033F0
0048DE95  mov     byte ptr [ebp+70h+var_74], 63h ; 'c'
0048DE99  mov     eax, [esi]
0048DE9B  mov     eax, [eax+40h]
0048DE9E  lea     ecx, [ebp+70h+var_2B1C]
0048DEA4  push    ecx
0048DEA5  lea     edx, [ebp+70h+var_171C]
0048DEAB  push    edx
0048DEAC  mov     ecx, esi
0048DEAE  call    eax
0048DEB0  mov     ecx, eax
0048DEB2  call    sub_5B05A0
0048DEB7  lea     ecx, [ebp+70h+var_171C]
0048DEBD  mov     dword_7533E8, eax
0048DEC2  call    sub_5B0440
0048DEC7  lea     ecx, [ebp+70h+var_2B1C]; void *
0048DECD  mov     byte ptr [ebp+70h+var_74], 0
0048DED1  call    sub_401E90
0048DED6  push    offset aImageRain; "IMAGE_RAIN"
0048DEDB  lea     ecx, [ebp+70h+var_1C74]
0048DEE1  call    sub_4033F0
0048DEE6  lea     eax, [ebp+70h+var_1C74]
0048DEEC  mov     byte ptr [ebp+70h+var_74], 64h ; 'd'
0048DEF0  mov     edx, [esi]
0048DEF2  mov     edx, [edx+40h]
0048DEF5  push    eax
0048DEF6  lea     ecx, [ebp+70h+var_93C]
0048DEFC  push    ecx
0048DEFD  mov     ecx, esi
0048DEFF  call    edx
0048DF01  mov     ecx, eax
0048DF03  call    sub_5B05A0
0048DF08  lea     ecx, [ebp+70h+var_93C]
0048DF0E  mov     dword_752F6C, eax
0048DF13  call    sub_5B0440
0048DF18  lea     ecx, [ebp+70h+var_1C74]; void *
0048DF1E  mov     byte ptr [ebp+70h+var_74], 0
0048DF22  call    sub_401E90
0048DF27  push    offset aImageReanimCab; "IMAGE_REANIM_CABBAGEPULT_CABBAGE"
0048DF2C  lea     ecx, [ebp+70h+var_328C]
0048DF32  call    sub_4033F0
0048DF37  mov     byte ptr [ebp+70h+var_74], 65h ; 'e'
0048DF3B  mov     eax, [esi]
0048DF3D  mov     eax, [eax+40h]
0048DF40  lea     ecx, [ebp+70h+var_328C]
0048DF46  push    ecx
0048DF47  lea     edx, [ebp+70h+var_1734]
0048DF4D  push    edx
0048DF4E  mov     ecx, esi
0048DF50  call    eax
0048DF52  mov     ecx, eax
0048DF54  call    sub_5B05A0
0048DF59  lea     ecx, [ebp+70h+var_1734]
0048DF5F  mov     dword_753534, eax
0048DF64  call    sub_5B0440
0048DF69  lea     ecx, [ebp+70h+var_328C]; void *
0048DF6F  mov     byte ptr [ebp+70h+var_74], 0
0048DF73  call    sub_401E90
0048DF78  push    offset aImageReanimCob; "IMAGE_REANIM_COBCANNON_COB"
0048DF7D  lea     ecx, [ebp+70h+var_1CAC]
0048DF83  call    sub_4033F0
0048DF88  lea     eax, [ebp+70h+var_1CAC]
0048DF8E  mov     byte ptr [ebp+70h+var_74], 66h ; 'f'
0048DF92  mov     edx, [esi]
0048DF94  mov     edx, [edx+40h]
0048DF97  push    eax
0048DF98  lea     ecx, [ebp+70h+var_954]
0048DF9E  push    ecx
0048DF9F  mov     ecx, esi
0048DFA1  call    edx
0048DFA3  mov     ecx, eax
0048DFA5  call    sub_5B05A0
0048DFAA  lea     ecx, [ebp+70h+var_954]
0048DFB0  mov     dword_753160, eax
0048DFB5  call    sub_5B0440
0048DFBA  lea     ecx, [ebp+70h+var_1CAC]; void *
0048DFC0  mov     byte ptr [ebp+70h+var_74], 0
0048DFC4  call    sub_401E90
0048DFC9  push    offset aImageReanimCoi; "IMAGE_REANIM_COINGLOW"
0048DFCE  lea     ecx, [ebp+70h+var_2B54]
0048DFD4  call    sub_4033F0
0048DFD9  mov     byte ptr [ebp+70h+var_74], 67h ; 'g'
0048DFDD  mov     eax, [esi]
0048DFDF  mov     eax, [eax+40h]
0048DFE2  lea     ecx, [ebp+70h+var_2B54]
0048DFE8  push    ecx
0048DFE9  lea     edx, [ebp+70h+var_174C]
0048DFEF  push    edx
0048DFF0  mov     ecx, esi
0048DFF2  call    eax
0048DFF4  mov     ecx, eax
0048DFF6  call    sub_5B05A0
0048DFFB  lea     ecx, [ebp+70h+var_174C]
0048E001  mov     dword_7537E4, eax
0048E006  call    sub_5B0440
0048E00B  lea     ecx, [ebp+70h+var_2B54]; void *
0048E011  mov     byte ptr [ebp+70h+var_74], 0
0048E015  call    sub_401E90
0048E01A  push    offset aImageReanimCoi_0; "IMAGE_REANIM_COIN_GOLD_DOLLAR"
0048E01F  lea     ecx, [ebp+70h+var_1CE4]
0048E025  call    sub_4033F0
0048E02A  lea     eax, [ebp+70h+var_1CE4]
0048E030  mov     byte ptr [ebp+70h+var_74], 68h ; 'h'
0048E034  mov     edx, [esi]
0048E036  mov     edx, [edx+40h]
0048E039  push    eax
0048E03A  lea     ecx, [ebp+70h+var_96C]
0048E040  push    ecx
0048E041  mov     ecx, esi
0048E043  call    edx
0048E045  mov     ecx, eax
0048E047  call    sub_5B05A0
0048E04C  lea     ecx, [ebp+70h+var_96C]
0048E052  mov     dword_753524, eax
0048E057  call    sub_5B0440
0048E05C  lea     ecx, [ebp+70h+var_1CE4]; void *
0048E062  mov     byte ptr [ebp+70h+var_74], 0
0048E066  call    sub_401E90
0048E06B  push    offset aImageReanimCoi_1; "IMAGE_REANIM_COIN_SILVER_DOLLAR"
0048E070  lea     ecx, [ebp+70h+var_3644]
0048E076  call    sub_4033F0
0048E07B  mov     byte ptr [ebp+70h+var_74], 69h ; 'i'
0048E07F  mov     eax, [esi]
0048E081  mov     eax, [eax+40h]
0048E084  lea     ecx, [ebp+70h+var_3644]
0048E08A  push    ecx
0048E08B  lea     edx, [ebp+70h+var_1764]
0048E091  push    edx
0048E092  mov     ecx, esi
0048E094  call    eax
0048E096  mov     ecx, eax
0048E098  call    sub_5B05A0
0048E09D  lea     ecx, [ebp+70h+var_1764]
0048E0A3  mov     dword_752C74, eax
0048E0A8  call    sub_5B0440
0048E0AD  lea     ecx, [ebp+70h+var_3644]; void *
0048E0B3  mov     byte ptr [ebp+70h+var_74], 0
0048E0B7  call    sub_401E90
0048E0BC  push    offset aImageReanimCor; "IMAGE_REANIM_CORNPULT_BUTTER"
0048E0C1  lea     ecx, [ebp+70h+var_1D1C]
0048E0C7  call    sub_4033F0
0048E0CC  lea     eax, [ebp+70h+var_1D1C]
0048E0D2  lea     ecx, [ebp+70h+var_984]
0048E0D8  push    eax
0048E0D9  mov     byte ptr [ebp+70h+var_74], 6Ah ; 'j'
0048E0DD  mov     edx, [esi]
0048E0DF  push    ecx
0048E0E0  mov     ecx, esi
0048E0E2  mov     edx, [edx+40h]
0048E0E5  call    edx
0048E0E7  mov     ecx, eax
0048E0E9  call    sub_5B05A0
0048E0EE  lea     ecx, [ebp+70h+var_984]
0048E0F4  mov     dword_752BD0, eax
0048E0F9  call    sub_5B0440
0048E0FE  lea     ecx, [ebp+70h+var_1D1C]; void *
0048E104  mov     byte ptr [ebp+70h+var_74], 0
0048E108  call    sub_401E90
0048E10D  push    offset aImageReanimCor_0; "IMAGE_REANIM_CORNPULT_BUTTER_SPLAT"
0048E112  lea     ecx, [ebp+70h+var_2B8C]
0048E118  call    sub_4033F0
0048E11D  mov     byte ptr [ebp+70h+var_74], 6Bh ; 'k'
0048E121  mov     eax, [esi]
0048E123  mov     eax, [eax+40h]
0048E126  lea     ecx, [ebp+70h+var_2B8C]
0048E12C  push    ecx
0048E12D  lea     edx, [ebp+70h+var_177C]
0048E133  push    edx
0048E134  mov     ecx, esi
0048E136  call    eax
0048E138  mov     ecx, eax
0048E13A  call    sub_5B05A0
0048E13F  lea     ecx, [ebp+70h+var_177C]
0048E145  mov     dword_7534B8, eax
0048E14A  call    sub_5B0440
0048E14F  lea     ecx, [ebp+70h+var_2B8C]; void *
0048E155  mov     byte ptr [ebp+70h+var_74], 0
0048E159  call    sub_401E90
0048E15E  push    offset aImageReanimCor_1; "IMAGE_REANIM_CORNPULT_KERNAL"
0048E163  lea     ecx, [ebp+70h+var_1D54]
0048E169  call    sub_4033F0
0048E16E  lea     eax, [ebp+70h+var_1D54]
0048E174  mov     byte ptr [ebp+70h+var_74], 6Ch ; 'l'
0048E178  mov     edx, [esi]
0048E17A  mov     edx, [edx+40h]
0048E17D  push    eax
0048E17E  lea     ecx, [ebp+70h+var_99C]
0048E184  push    ecx
0048E185  mov     ecx, esi
0048E187  call    edx
0048E189  mov     ecx, eax
0048E18B  call    sub_5B05A0
0048E190  lea     ecx, [ebp+70h+var_99C]
0048E196  mov     dword_753228, eax
0048E19B  call    sub_5B0440
0048E1A0  lea     ecx, [ebp+70h+var_1D54]; void *
0048E1A6  mov     byte ptr [ebp+70h+var_74], 0
0048E1AA  call    sub_401E90
0048E1AF  push    offset aImageReanimCra; "IMAGE_REANIM_CRAZYDAVE_MOUTH1"
0048E1B4  lea     ecx, [ebp+70h+var_32C4]
0048E1BA  call    sub_4033F0
0048E1BF  mov     byte ptr [ebp+70h+var_74], 6Dh ; 'm'
0048E1C3  mov     eax, [esi]
0048E1C5  mov     eax, [eax+40h]
0048E1C8  lea     ecx, [ebp+70h+var_32C4]
0048E1CE  push    ecx
0048E1CF  lea     edx, [ebp+70h+var_1794]
0048E1D5  push    edx
0048E1D6  mov     ecx, esi
0048E1D8  call    eax
0048E1DA  mov     ecx, eax
0048E1DC  call    sub_5B05A0
0048E1E1  lea     ecx, [ebp+70h+var_1794]
0048E1E7  mov     dword_752F60, eax
0048E1EC  call    sub_5B0440
0048E1F1  lea     ecx, [ebp+70h+var_32C4]; void *
0048E1F7  mov     byte ptr [ebp+70h+var_74], 0
0048E1FB  call    sub_401E90
0048E200  push    offset aImageReanimCra_0; "IMAGE_REANIM_CRAZYDAVE_MOUTH4"
0048E205  lea     ecx, [ebp+70h+var_1D8C]
0048E20B  call    sub_4033F0
0048E210  lea     eax, [ebp+70h+var_1D8C]
0048E216  mov     byte ptr [ebp+70h+var_74], 6Eh ; 'n'
0048E21A  mov     edx, [esi]
0048E21C  push    eax
0048E21D  mov     edx, [edx+40h]
0048E220  lea     ecx, [ebp+70h+var_9B4]
0048E226  push    ecx
0048E227  mov     ecx, esi
0048E229  call    edx
0048E22B  mov     ecx, eax
0048E22D  call    sub_5B05A0
0048E232  lea     ecx, [ebp+70h+var_9B4]
0048E238  mov     dword_752B5C, eax
0048E23D  call    sub_5B0440
0048E242  lea     ecx, [ebp+70h+var_1D8C]; void *
0048E248  mov     byte ptr [ebp+70h+var_74], 0
0048E24C  call    sub_401E90
0048E251  push    offset aImageReanimCra_1; "IMAGE_REANIM_CRAZYDAVE_MOUTH5"
0048E256  lea     ecx, [ebp+70h+var_2BC4]
0048E25C  call    sub_4033F0
0048E261  mov     byte ptr [ebp+70h+var_74], 6Fh ; 'o'
0048E265  mov     eax, [esi]
0048E267  mov     eax, [eax+40h]
0048E26A  lea     ecx, [ebp+70h+var_2BC4]
0048E270  push    ecx
0048E271  lea     edx, [ebp+70h+var_17AC]
0048E277  push    edx
0048E278  mov     ecx, esi
0048E27A  call    eax
0048E27C  mov     ecx, eax
0048E27E  call    sub_5B05A0
0048E283  lea     ecx, [ebp+70h+var_17AC]
0048E289  mov     dword_7530C8, eax
0048E28E  call    sub_5B0440
0048E293  lea     ecx, [ebp+70h+var_2BC4]; void *
0048E299  mov     byte ptr [ebp+70h+var_74], 0
0048E29D  call    sub_401E90
0048E2A2  push    offset aImageReanimCra_2; "IMAGE_REANIM_CRAZYDAVE_MOUTH6"
0048E2A7  lea     ecx, [ebp+70h+var_1DC4]
0048E2AD  call    sub_4033F0
0048E2B2  lea     eax, [ebp+70h+var_1DC4]
0048E2B8  mov     byte ptr [ebp+70h+var_74], 70h ; 'p'
0048E2BC  mov     edx, [esi]
0048E2BE  mov     edx, [edx+40h]
0048E2C1  push    eax
0048E2C2  lea     ecx, [ebp+70h+var_9CC]
0048E2C8  push    ecx
0048E2C9  mov     ecx, esi
0048E2CB  call    edx
0048E2CD  mov     ecx, eax
0048E2CF  call    sub_5B05A0
0048E2D4  lea     ecx, [ebp+70h+var_9CC]
0048E2DA  mov     dword_752BE4, eax
0048E2DF  call    sub_5B0440
0048E2E4  lea     ecx, [ebp+70h+var_1DC4]; void *
0048E2EA  mov     byte ptr [ebp+70h+var_74], 0
0048E2EE  call    sub_401E90
0048E2F3  push    offset aImageReanimDia; "IMAGE_REANIM_DIAMOND"
0048E2F8  lea     ecx, [ebp+70h+var_39C4]
0048E2FE  call    sub_4033F0
0048E303  mov     byte ptr [ebp+70h+var_74], 71h ; 'q'
0048E307  mov     eax, [esi]
0048E309  mov     eax, [eax+40h]
0048E30C  lea     ecx, [ebp+70h+var_39C4]
0048E312  push    ecx
0048E313  lea     edx, [ebp+70h+var_17C4]
0048E319  push    edx
0048E31A  mov     ecx, esi
0048E31C  call    eax
0048E31E  mov     ecx, eax
0048E320  call    sub_5B05A0
0048E325  lea     ecx, [ebp+70h+var_17C4]
0048E32B  mov     dword_75318C, eax
0048E330  call    sub_5B0440
0048E335  lea     ecx, [ebp+70h+var_39C4]; void *
0048E33B  mov     byte ptr [ebp+70h+var_74], 0
0048E33F  call    sub_401E90
0048E344  push    offset aImageReanimGar; "IMAGE_REANIM_GARLIC_BODY2"
0048E349  lea     ecx, [ebp+70h+var_1DFC]
0048E34F  call    sub_4033F0
0048E354  mov     byte ptr [ebp+70h+var_74], 72h ; 'r'
0048E358  mov     edx, [esi]
0048E35A  mov     edx, [edx+40h]
0048E35D  lea     eax, [ebp+70h+var_1DFC]
0048E363  push    eax
0048E364  lea     ecx, [ebp+70h+var_9E4]
0048E36A  push    ecx
0048E36B  mov     ecx, esi
0048E36D  call    edx
0048E36F  mov     ecx, eax
0048E371  call    sub_5B05A0
0048E376  lea     ecx, [ebp+70h+var_9E4]
0048E37C  mov     dword_752EEC, eax
0048E381  call    sub_5B0440
0048E386  lea     ecx, [ebp+70h+var_1DFC]; void *
0048E38C  mov     byte ptr [ebp+70h+var_74], 0
0048E390  call    sub_401E90
0048E395  push    offset aImageReanimGar_0; "IMAGE_REANIM_GARLIC_BODY3"
0048E39A  lea     ecx, [ebp+70h+var_2BFC]
0048E3A0  call    sub_4033F0
0048E3A5  mov     byte ptr [ebp+70h+var_74], 73h ; 's'
0048E3A9  mov     eax, [esi]
0048E3AB  mov     eax, [eax+40h]
0048E3AE  lea     ecx, [ebp+70h+var_2BFC]
0048E3B4  push    ecx
0048E3B5  lea     edx, [ebp+70h+var_17DC]
0048E3BB  push    edx
0048E3BC  mov     ecx, esi
0048E3BE  call    eax
0048E3C0  mov     ecx, eax
0048E3C2  call    sub_5B05A0
0048E3C7  lea     ecx, [ebp+70h+var_17DC]
0048E3CD  mov     dword_752D68, eax
0048E3D2  call    sub_5B0440
0048E3D7  lea     ecx, [ebp+70h+var_2BFC]; void *
0048E3DD  mov     byte ptr [ebp+70h+var_74], 0
0048E3E1  call    sub_401E90
0048E3E6  push    offset aImageReanimMel; "IMAGE_REANIM_MELONPULT_MELON"
0048E3EB  lea     ecx, [ebp+70h+var_1E34]
0048E3F1  call    sub_4033F0
0048E3F6  lea     eax, [ebp+70h+var_1E34]
0048E3FC  mov     byte ptr [ebp+70h+var_74], 74h ; 't'
0048E400  mov     edx, [esi]
0048E402  mov     edx, [edx+40h]
0048E405  push    eax
0048E406  lea     ecx, [ebp+70h+var_9FC]
0048E40C  push    ecx
0048E40D  mov     ecx, esi
0048E40F  call    edx
0048E411  mov     ecx, eax
0048E413  call    sub_5B05A0
0048E418  lea     ecx, [ebp+70h+var_9FC]
0048E41E  mov     dword_7534DC, eax
0048E423  call    sub_5B0440
0048E428  lea     ecx, [ebp+70h+var_1E34]; void *
0048E42E  mov     byte ptr [ebp+70h+var_74], 0
0048E432  call    sub_401E90
0048E437  push    offset aImageReanimPot; "IMAGE_REANIM_POT_TOP_DARK"
0048E43C  lea     ecx, [ebp+70h+var_32FC]
0048E442  call    sub_4033F0
0048E447  mov     byte ptr [ebp+70h+var_74], 75h ; 'u'
0048E44B  mov     eax, [esi]
0048E44D  mov     eax, [eax+40h]
0048E450  lea     ecx, [ebp+70h+var_32FC]
0048E456  push    ecx
0048E457  lea     edx, [ebp+70h+var_17F4]
0048E45D  push    edx
0048E45E  mov     ecx, esi
0048E460  call    eax
0048E462  mov     ecx, eax
0048E464  call    sub_5B05A0
0048E469  lea     ecx, [ebp+70h+var_17F4]
0048E46F  mov     dword_753170, eax
0048E474  call    sub_5B0440
0048E479  lea     ecx, [ebp+70h+var_32FC]; void *
0048E47F  mov     byte ptr [ebp+70h+var_74], 0
0048E483  call    sub_401E90
0048E488  push    offset aImageReanimPum; "IMAGE_REANIM_PUMPKIN_DAMAGE1"
0048E48D  lea     ecx, [ebp+70h+var_1E6C]
0048E493  call    sub_4033F0
0048E498  lea     eax, [ebp+70h+var_1E6C]
0048E49E  mov     byte ptr [ebp+70h+var_74], 76h ; 'v'
0048E4A2  mov     edx, [esi]
0048E4A4  mov     edx, [edx+40h]
0048E4A7  push    eax
0048E4A8  lea     ecx, [ebp+70h+var_A14]
0048E4AE  push    ecx
0048E4AF  mov     ecx, esi
0048E4B1  call    edx
0048E4B3  mov     ecx, eax
0048E4B5  call    sub_5B05A0
0048E4BA  lea     ecx, [ebp+70h+var_A14]
0048E4C0  mov     dword_753634, eax
0048E4C5  call    sub_5B0440
0048E4CA  lea     ecx, [ebp+70h+var_1E6C]; void *
0048E4D0  mov     byte ptr [ebp+70h+var_74], 0
0048E4D4  call    sub_401E90
0048E4D9  push    offset aImageReanimPum_0; "IMAGE_REANIM_PUMPKIN_DAMAGE3"
0048E4DE  lea     ecx, [ebp+70h+var_2C34]
0048E4E4  call    sub_4033F0
0048E4E9  mov     byte ptr [ebp+70h+var_74], 77h ; 'w'
0048E4ED  mov     eax, [esi]
0048E4EF  mov     eax, [eax+40h]
0048E4F2  lea     ecx, [ebp+70h+var_2C34]
0048E4F8  push    ecx
0048E4F9  lea     edx, [ebp+70h+var_180C]
0048E4FF  push    edx
0048E500  mov     ecx, esi
0048E502  call    eax
0048E504  mov     ecx, eax
0048E506  call    sub_5B05A0
0048E50B  lea     ecx, [ebp+70h+var_180C]
0048E511  mov     dword_753500, eax
0048E516  call    sub_5B0440
0048E51B  lea     ecx, [ebp+70h+var_2C34]; void *
0048E521  mov     byte ptr [ebp+70h+var_74], 0
0048E525  call    sub_401E90
0048E52A  push    offset aImageReanimSel; "IMAGE_REANIM_SELECTORSCREEN_ADVENTURE_B"...
0048E52F  lea     ecx, [ebp+70h+var_1EA4]
0048E535  call    sub_4033F0
0048E53A  lea     eax, [ebp+70h+var_1EA4]
0048E540  mov     byte ptr [ebp+70h+var_74], 78h ; 'x'
0048E544  mov     edx, [esi]
0048E546  mov     edx, [edx+40h]
0048E549  push    eax
0048E54A  lea     ecx, [ebp+70h+var_A2C]
0048E550  push    ecx
0048E551  mov     ecx, esi
0048E553  call    edx
0048E555  mov     ecx, eax
0048E557  call    sub_5B05A0
0048E55C  lea     ecx, [ebp+70h+var_A2C]
0048E562  mov     dword_753774, eax
0048E567  call    sub_5B0440
0048E56C  lea     ecx, [ebp+70h+var_1EA4]; void *
0048E572  mov     byte ptr [ebp+70h+var_74], 0
0048E576  call    sub_401E90
0048E57B  push    offset aImageReanimSel_0; "IMAGE_REANIM_SELECTORSCREEN_ADVENTURE_H"...
0048E580  lea     ecx, [ebp+70h+var_367C]
0048E586  call    sub_4033F0
0048E58B  mov     byte ptr [ebp+70h+var_74], 79h ; 'y'
0048E58F  mov     eax, [esi]
0048E591  mov     eax, [eax+40h]
0048E594  lea     ecx, [ebp+70h+var_367C]
0048E59A  push    ecx
0048E59B  lea     edx, [ebp+70h+var_1824]
0048E5A1  push    edx
0048E5A2  mov     ecx, esi
0048E5A4  call    eax
0048E5A6  mov     ecx, eax
0048E5A8  call    sub_5B05A0
0048E5AD  lea     ecx, [ebp+70h+var_1824]
0048E5B3  mov     dword_7536A4, eax
0048E5B8  call    sub_5B0440
0048E5BD  mov     byte ptr [ebp+70h+var_74], 0
0048E5C1  lea     ecx, [ebp+70h+var_367C]; void *
0048E5C7  call    sub_401E90
0048E5CC  push    offset aImageReanimSel_1; "IMAGE_REANIM_SELECTORSCREEN_ALMANAC_SHA"...
0048E5D1  lea     ecx, [ebp+70h+var_1EDC]
0048E5D7  call    sub_4033F0
0048E5DC  lea     eax, [ebp+70h+var_1EDC]
0048E5E2  mov     byte ptr [ebp+70h+var_74], 7Ah ; 'z'
0048E5E6  mov     edx, [esi]
0048E5E8  mov     edx, [edx+40h]
0048E5EB  push    eax
0048E5EC  lea     ecx, [ebp+70h+var_A44]
0048E5F2  push    ecx
0048E5F3  mov     ecx, esi
0048E5F5  call    edx
0048E5F7  mov     ecx, eax
0048E5F9  call    sub_5B05A0
0048E5FE  lea     ecx, [ebp+70h+var_A44]
0048E604  mov     dword_7536BC, eax
0048E609  call    sub_5B0440
0048E60E  lea     ecx, [ebp+70h+var_1EDC]; void *
0048E614  mov     byte ptr [ebp+70h+var_74], 0
0048E618  call    sub_401E90
0048E61D  push    offset aImageReanimSel_2; "IMAGE_REANIM_SELECTORSCREEN_BG"
0048E622  lea     ecx, [ebp+70h+var_2C6C]
0048E628  call    sub_4033F0
0048E62D  mov     byte ptr [ebp+70h+var_74], 7Bh ; '{'
0048E631  mov     eax, [esi]
0048E633  mov     eax, [eax+40h]
0048E636  lea     ecx, [ebp+70h+var_2C6C]
0048E63C  push    ecx
0048E63D  lea     edx, [ebp+70h+var_183C]
0048E643  push    edx
0048E644  mov     ecx, esi
0048E646  call    eax
0048E648  mov     ecx, eax
0048E64A  call    sub_5B05A0
0048E64F  lea     ecx, [ebp+70h+var_183C]
0048E655  mov     dword_753744, eax
0048E65A  call    sub_5B0440
0048E65F  lea     ecx, [ebp+70h+var_2C6C]; void *
0048E665  mov     byte ptr [ebp+70h+var_74], 0
0048E669  call    sub_401E90
0048E66E  push    offset aImageReanimSel_3; "IMAGE_REANIM_SELECTORSCREEN_CHALLENGES_"...
0048E673  lea     ecx, [ebp+70h+var_1F14]
0048E679  call    sub_4033F0
0048E67E  lea     eax, [ebp+70h+var_1F14]
0048E684  mov     byte ptr [ebp+70h+var_74], 7Ch ; '|'
0048E688  mov     edx, [esi]
0048E68A  mov     edx, [edx+40h]
0048E68D  push    eax
0048E68E  lea     ecx, [ebp+70h+var_A5C]
0048E694  push    ecx
0048E695  mov     ecx, esi
0048E697  call    edx
0048E699  mov     ecx, eax
0048E69B  call    sub_5B05A0
0048E6A0  lea     ecx, [ebp+70h+var_A5C]
0048E6A6  mov     dword_7537A8, eax
0048E6AB  call    sub_5B0440
0048E6B0  lea     ecx, [ebp+70h+var_1F14]; void *
0048E6B6  mov     byte ptr [ebp+70h+var_74], 0
0048E6BA  call    sub_401E90
0048E6BF  push    offset aImageReanimSel_4; "IMAGE_REANIM_SELECTORSCREEN_CHALLENGES_"...
0048E6C4  lea     ecx, [ebp+70h+var_3334]
0048E6CA  call    sub_4033F0
0048E6CF  mov     byte ptr [ebp+70h+var_74], 7Dh ; '}'
0048E6D3  mov     eax, [esi]
0048E6D5  mov     eax, [eax+40h]
0048E6D8  lea     ecx, [ebp+70h+var_3334]
0048E6DE  push    ecx
0048E6DF  lea     edx, [ebp+70h+var_1854]
0048E6E5  push    edx
0048E6E6  mov     ecx, esi
0048E6E8  call    eax
0048E6EA  mov     ecx, eax
0048E6EC  call    sub_5B05A0
0048E6F1  mov     dword_752B28, eax
0048E6F6  lea     ecx, [ebp+70h+var_1854]
0048E6FC  call    sub_5B0440
0048E701  lea     ecx, [ebp+70h+var_3334]; void *
0048E707  mov     byte ptr [ebp+70h+var_74], 0
0048E70B  call    sub_401E90
0048E710  push    offset aImageReanimSel_5; "IMAGE_REANIM_SELECTORSCREEN_KEY_SHADOW"
0048E715  lea     ecx, [ebp+70h+var_1F4C]
0048E71B  call    sub_4033F0
0048E720  lea     eax, [ebp+70h+var_1F4C]
0048E726  mov     byte ptr [ebp+70h+var_74], 7Eh ; '~'
0048E72A  mov     edx, [esi]
0048E72C  mov     edx, [edx+40h]
0048E72F  push    eax
0048E730  lea     ecx, [ebp+70h+var_A74]
0048E736  push    ecx
0048E737  mov     ecx, esi
0048E739  call    edx
0048E73B  mov     ecx, eax
0048E73D  call    sub_5B05A0
0048E742  lea     ecx, [ebp+70h+var_A74]
0048E748  mov     dword_752DE8, eax
0048E74D  call    sub_5B0440
0048E752  lea     ecx, [ebp+70h+var_1F4C]; void *
0048E758  mov     byte ptr [ebp+70h+var_74], 0
0048E75C  call    sub_401E90
0048E761  push    offset aImageReanimSel_6; "IMAGE_REANIM_SELECTORSCREEN_STARTADVENT"...
0048E766  lea     ecx, [ebp+70h+var_2CA4]
0048E76C  call    sub_4033F0
0048E771  mov     byte ptr [ebp+70h+var_74], 7Fh
0048E775  mov     eax, [esi]
0048E777  mov     eax, [eax+40h]
0048E77A  lea     ecx, [ebp+70h+var_2CA4]
0048E780  push    ecx
0048E781  lea     edx, [ebp+70h+var_186C]
0048E787  push    edx
0048E788  mov     ecx, esi
0048E78A  call    eax
0048E78C  mov     ecx, eax
0048E78E  call    sub_5B05A0
0048E793  lea     ecx, [ebp+70h+var_186C]
0048E799  mov     dword_753820, eax
0048E79E  call    sub_5B0440
0048E7A3  lea     ecx, [ebp+70h+var_2CA4]; void *
0048E7A9  mov     byte ptr [ebp+70h+var_74], 0
0048E7AD  call    sub_401E90
0048E7B2  push    offset aImageReanimSel_7; "IMAGE_REANIM_SELECTORSCREEN_STARTADVENT"...
0048E7B7  lea     ecx, [ebp+70h+var_1F84]
0048E7BD  call    sub_4033F0
0048E7C2  lea     eax, [ebp+70h+var_1F84]
0048E7C8  mov     byte ptr [ebp+70h+var_74], 80h
0048E7CC  mov     edx, [esi]
0048E7CE  mov     edx, [edx+40h]
0048E7D1  push    eax
0048E7D2  lea     ecx, [ebp+70h+var_A8C]
0048E7D8  push    ecx
0048E7D9  mov     ecx, esi
0048E7DB  call    edx
0048E7DD  mov     ecx, eax
0048E7DF  call    sub_5B05A0
0048E7E4  lea     ecx, [ebp+70h+var_A8C]
0048E7EA  mov     dword_752EB0, eax
0048E7EF  call    sub_5B0440
0048E7F4  lea     ecx, [ebp+70h+var_1F84]; void *
0048E7FA  mov     byte ptr [ebp+70h+var_74], 0
0048E7FE  call    sub_401E90
0048E803  push    offset aImageReanimSel_8; "IMAGE_REANIM_SELECTORSCREEN_SURVIVAL_BU"...
0048E808  lea     ecx, [ebp+70h+var_383C]
0048E80E  call    sub_4033F0
0048E813  mov     byte ptr [ebp+70h+var_74], 81h
0048E817  mov     eax, [esi]
0048E819  mov     eax, [eax+40h]
0048E81C  lea     ecx, [ebp+70h+var_383C]
0048E822  push    ecx
0048E823  lea     edx, [ebp+70h+var_1884]
0048E829  push    edx
0048E82A  mov     ecx, esi
0048E82C  call    eax
0048E82E  mov     ecx, eax
0048E830  call    sub_5B05A0
0048E835  lea     ecx, [ebp+70h+var_1884]
0048E83B  mov     dword_752F68, eax
0048E840  call    sub_5B0440
0048E845  lea     ecx, [ebp+70h+var_383C]; void *
0048E84B  mov     byte ptr [ebp+70h+var_74], 0
0048E84F  call    sub_401E90
0048E854  push    offset aImageReanimSel_9; "IMAGE_REANIM_SELECTORSCREEN_SURVIVAL_HI"...
0048E859  lea     ecx, [ebp+70h+var_1FBC]
0048E85F  call    sub_4033F0
0048E864  lea     eax, [ebp+70h+var_1FBC]
0048E86A  mov     byte ptr [ebp+70h+var_74], 82h
0048E86E  mov     edx, [esi]
0048E870  mov     edx, [edx+40h]
0048E873  push    eax
0048E874  lea     ecx, [ebp+70h+var_AA4]
0048E87A  push    ecx
0048E87B  mov     ecx, esi
0048E87D  call    edx
0048E87F  mov     ecx, eax
0048E881  call    sub_5B05A0
0048E886  lea     ecx, [ebp+70h+var_AA4]
0048E88C  mov     dword_7531D4, eax
0048E891  call    sub_5B0440
0048E896  lea     ecx, [ebp+70h+var_1FBC]; void *
0048E89C  mov     byte ptr [ebp+70h+var_74], 0
0048E8A0  call    sub_401E90
0048E8A5  push    offset aImageReanimSel_10; "IMAGE_REANIM_SELECTORSCREEN_VASEBREAKER"...
0048E8AA  lea     ecx, [ebp+70h+var_2CDC]
0048E8B0  call    sub_4033F0
0048E8B5  mov     byte ptr [ebp+70h+var_74], 83h
0048E8B9  mov     eax, [esi]
0048E8BB  mov     eax, [eax+40h]
0048E8BE  lea     ecx, [ebp+70h+var_2CDC]
0048E8C4  push    ecx
0048E8C5  lea     edx, [ebp+70h+var_189C]
0048E8CB  push    edx
0048E8CC  mov     ecx, esi
0048E8CE  call    eax
0048E8D0  mov     ecx, eax
0048E8D2  call    sub_5B05A0
0048E8D7  lea     ecx, [ebp+70h+var_189C]
0048E8DD  mov     dword_7535B4, eax
0048E8E2  call    sub_5B0440
0048E8E7  lea     ecx, [ebp+70h+var_2CDC]; void *
0048E8ED  mov     byte ptr [ebp+70h+var_74], 0
0048E8F1  call    sub_401E90
0048E8F6  push    offset aImageReanimSel_11; "IMAGE_REANIM_SELECTORSCREEN_VASEBREAKER"...
0048E8FB  lea     ecx, [ebp+70h+var_1FF4]
0048E901  call    sub_4033F0
0048E906  lea     eax, [ebp+70h+var_1FF4]
0048E90C  mov     byte ptr [ebp+70h+var_74], 84h
0048E910  mov     edx, [esi]
0048E912  mov     edx, [edx+40h]
0048E915  push    eax
0048E916  lea     ecx, [ebp+70h+var_ABC]
0048E91C  push    ecx
0048E91D  mov     ecx, esi
0048E91F  call    edx
0048E921  mov     ecx, eax
0048E923  call    sub_5B05A0
0048E928  lea     ecx, [ebp+70h+var_ABC]
0048E92E  mov     dword_752CDC, eax
0048E933  call    sub_5B0440
0048E938  lea     ecx, [ebp+70h+var_1FF4]; void *
0048E93E  mov     byte ptr [ebp+70h+var_74], 0
0048E942  call    sub_401E90
0048E947  push    offset aImageReanimSel_12; "IMAGE_REANIM_SELECTORSCREEN_WOODSIGN2_P"...
0048E94C  lea     ecx, [ebp+70h+var_336C]
0048E952  call    sub_4033F0
0048E957  lea     ecx, [ebp+70h+var_336C]
0048E95D  push    ecx
0048E95E  lea     edx, [ebp+70h+var_18B4]
0048E964  mov     byte ptr [ebp+70h+var_74], 85h
0048E968  mov     eax, [esi]
0048E96A  push    edx
0048E96B  mov     ecx, esi
0048E96D  mov     eax, [eax+40h]
0048E970  call    eax
0048E972  mov     ecx, eax
0048E974  call    sub_5B05A0
0048E979  lea     ecx, [ebp+70h+var_18B4]
0048E97F  mov     dword_7532A0, eax
0048E984  call    sub_5B0440
0048E989  lea     ecx, [ebp+70h+var_336C]; void *
0048E98F  mov     byte ptr [ebp+70h+var_74], 0
0048E993  call    sub_401E90
0048E998  push    offset aImageReanimSel_13; "IMAGE_REANIM_SELECTORSCREEN_WOODSIGN3_P"...
0048E99D  lea     ecx, [ebp+70h+var_202C]
0048E9A3  call    sub_4033F0
0048E9A8  lea     eax, [ebp+70h+var_202C]
0048E9AE  mov     byte ptr [ebp+70h+var_74], 86h
0048E9B2  mov     edx, [esi]
0048E9B4  mov     edx, [edx+40h]
0048E9B7  push    eax
0048E9B8  lea     ecx, [ebp+70h+var_AD4]
0048E9BE  push    ecx
0048E9BF  mov     ecx, esi
0048E9C1  call    edx
0048E9C3  mov     ecx, eax
0048E9C5  call    sub_5B05A0
0048E9CA  lea     ecx, [ebp+70h+var_AD4]
0048E9D0  mov     dword_752F14, eax
0048E9D5  call    sub_5B0440
0048E9DA  lea     ecx, [ebp+70h+var_202C]; void *
0048E9E0  mov     byte ptr [ebp+70h+var_74], 0
0048E9E4  call    sub_401E90
0048E9E9  push    offset aImageReanimSti; "IMAGE_REANIM_STINKY_TURN3"
0048E9EE  lea     ecx, [ebp+70h+var_2D14]
0048E9F4  call    sub_4033F0
0048E9F9  mov     byte ptr [ebp+70h+var_74], 87h
0048E9FD  mov     eax, [esi]
0048E9FF  mov     eax, [eax+40h]
0048EA02  lea     ecx, [ebp+70h+var_2D14]
0048EA08  push    ecx
0048EA09  lea     edx, [ebp+70h+var_18CC]
0048EA0F  push    edx
0048EA10  mov     ecx, esi
0048EA12  call    eax
0048EA14  mov     ecx, eax
0048EA16  call    sub_5B05A0
0048EA1B  lea     ecx, [ebp+70h+var_18CC]
0048EA21  mov     dword_752E8C, eax
0048EA26  call    sub_5B0440
0048EA2B  lea     ecx, [ebp+70h+var_2D14]; void *
0048EA31  mov     byte ptr [ebp+70h+var_74], 0
0048EA35  call    sub_401E90
0048EA3A  push    offset aImageReanimTal; "IMAGE_REANIM_TALLNUT_CRACKED1"
0048EA3F  lea     ecx, [ebp+70h+var_2064]
0048EA45  call    sub_4033F0
0048EA4A  lea     eax, [ebp+70h+var_2064]
0048EA50  mov     byte ptr [ebp+70h+var_74], 88h
0048EA54  mov     edx, [esi]
0048EA56  mov     edx, [edx+40h]
0048EA59  push    eax
0048EA5A  lea     ecx, [ebp+70h+var_AEC]
0048EA60  push    ecx
0048EA61  mov     ecx, esi
0048EA63  call    edx
0048EA65  mov     ecx, eax
0048EA67  call    sub_5B05A0
0048EA6C  lea     ecx, [ebp+70h+var_AEC]
0048EA72  mov     dword_752B04, eax
0048EA77  call    sub_5B0440
0048EA7C  lea     ecx, [ebp+70h+var_2064]; void *
0048EA82  mov     byte ptr [ebp+70h+var_74], 0
0048EA86  call    sub_401E90
0048EA8B  push    offset aImageReanimTal_0; "IMAGE_REANIM_TALLNUT_CRACKED2"
0048EA90  lea     ecx, [ebp+70h+var_36B4]
0048EA96  call    sub_4033F0
0048EA9B  lea     ecx, [ebp+70h+var_36B4]
0048EAA1  mov     byte ptr [ebp+70h+var_74], 89h
0048EAA5  mov     eax, [esi]
0048EAA7  push    ecx
0048EAA8  mov     eax, [eax+40h]
0048EAAB  lea     edx, [ebp+70h+var_18E4]
0048EAB1  push    edx
0048EAB2  mov     ecx, esi
0048EAB4  call    eax
0048EAB6  mov     ecx, eax
0048EAB8  call    sub_5B05A0
0048EABD  lea     ecx, [ebp+70h+var_18E4]
0048EAC3  mov     dword_753370, eax
0048EAC8  call    sub_5B0440
0048EACD  lea     ecx, [ebp+70h+var_36B4]; void *
0048EAD3  mov     byte ptr [ebp+70h+var_74], 0
0048EAD7  call    sub_401E90
0048EADC  push    offset aImageReanimTre; "IMAGE_REANIM_TREE_BG"
0048EAE1  lea     ecx, [ebp+70h+var_209C]
0048EAE7  call    sub_4033F0
0048EAEC  lea     eax, [ebp+70h+var_209C]
0048EAF2  mov     byte ptr [ebp+70h+var_74], 8Ah
0048EAF6  mov     edx, [esi]
0048EAF8  mov     edx, [edx+40h]
0048EAFB  push    eax
0048EAFC  lea     ecx, [ebp+70h+var_B04]
0048EB02  push    ecx
0048EB03  mov     ecx, esi
0048EB05  call    edx
0048EB07  mov     ecx, eax
0048EB09  call    sub_5B05A0
0048EB0E  lea     ecx, [ebp+70h+var_B04]
0048EB14  mov     dword_7534C8, eax
0048EB19  call    sub_5B0440
0048EB1E  lea     ecx, [ebp+70h+var_209C]; void *
0048EB24  mov     byte ptr [ebp+70h+var_74], 0
0048EB28  call    sub_401E90
0048EB2D  push    offset aImageReanimWal; "IMAGE_REANIM_WALLNUT_BODY"
0048EB32  lea     ecx, [ebp+70h+var_2D4C]
0048EB38  call    sub_4033F0
0048EB3D  mov     byte ptr [ebp+70h+var_74], 8Bh
0048EB41  mov     eax, [esi]
0048EB43  mov     eax, [eax+40h]
0048EB46  lea     ecx, [ebp+70h+var_2D4C]
0048EB4C  push    ecx
0048EB4D  lea     edx, [ebp+70h+var_18FC]
0048EB53  push    edx
0048EB54  mov     ecx, esi
0048EB56  call    eax
0048EB58  mov     ecx, eax
0048EB5A  call    sub_5B05A0
0048EB5F  lea     ecx, [ebp+70h+var_18FC]
0048EB65  mov     dword_752B70, eax
0048EB6A  call    sub_5B0440
0048EB6F  lea     ecx, [ebp+70h+var_2D4C]; void *
0048EB75  mov     byte ptr [ebp+70h+var_74], 0
0048EB79  call    sub_401E90
0048EB7E  push    offset aImageReanimWal_0; "IMAGE_REANIM_WALLNUT_CRACKED1"
0048EB83  lea     ecx, [ebp+70h+var_20D4]
0048EB89  call    sub_4033F0
0048EB8E  lea     eax, [ebp+70h+var_20D4]
0048EB94  mov     byte ptr [ebp+70h+var_74], 8Ch
0048EB98  mov     edx, [esi]
0048EB9A  mov     edx, [edx+40h]
0048EB9D  push    eax
0048EB9E  lea     ecx, [ebp+70h+var_B1C]
0048EBA4  push    ecx
0048EBA5  mov     ecx, esi
0048EBA7  call    edx
0048EBA9  mov     ecx, eax
0048EBAB  call    sub_5B05A0
0048EBB0  lea     ecx, [ebp+70h+var_B1C]
0048EBB6  mov     dword_752C70, eax
0048EBBB  call    sub_5B0440
0048EBC0  lea     ecx, [ebp+70h+var_20D4]; void *
0048EBC6  mov     byte ptr [ebp+70h+var_74], 0
0048EBCA  call    sub_401E90
0048EBCF  push    offset aImageReanimWal_1; "IMAGE_REANIM_WALLNUT_CRACKED2"
0048EBD4  lea     ecx, [ebp+70h+var_33A4]
0048EBDA  call    sub_4033F0
0048EBDF  mov     byte ptr [ebp+70h+var_74], 8Dh
0048EBE3  mov     eax, [esi]
0048EBE5  mov     eax, [eax+40h]
0048EBE8  lea     ecx, [ebp+70h+var_33A4]
0048EBEE  push    ecx
0048EBEF  lea     edx, [ebp+70h+var_1914]
0048EBF5  push    edx
0048EBF6  mov     ecx, esi
0048EBF8  call    eax
0048EBFA  mov     ecx, eax
0048EBFC  call    sub_5B05A0
0048EC01  lea     ecx, [ebp+70h+var_1914]
0048EC07  mov     dword_752FC8, eax
0048EC0C  call    sub_5B0440
0048EC11  lea     ecx, [ebp+70h+var_33A4]; void *
0048EC17  mov     byte ptr [ebp+70h+var_74], 0
0048EC1B  call    sub_401E90
0048EC20  push    offset aImageReanimWin; "IMAGE_REANIM_WINTERMELON_PROJECTILE"
0048EC25  lea     ecx, [ebp+70h+var_210C]
0048EC2B  call    sub_4033F0
0048EC30  lea     eax, [ebp+70h+var_210C]
0048EC36  mov     byte ptr [ebp+70h+var_74], 8Eh
0048EC3A  mov     edx, [esi]
0048EC3C  mov     edx, [edx+40h]
0048EC3F  push    eax
0048EC40  lea     ecx, [ebp+70h+var_B34]
0048EC46  push    ecx
0048EC47  mov     ecx, esi
0048EC49  call    edx
0048EC4B  mov     ecx, eax
0048EC4D  call    sub_5B05A0
0048EC52  lea     ecx, [ebp+70h+var_B34]
0048EC58  mov     dword_752C88, eax
0048EC5D  call    sub_5B0440
0048EC62  lea     ecx, [ebp+70h+var_210C]; void *
0048EC68  mov     byte ptr [ebp+70h+var_74], 0
0048EC6C  call    sub_401E90
0048EC71  push    offset aImageReanimZen; "IMAGE_REANIM_ZENGARDEN_BUGSPRAY_BOTTLE"
0048EC76  lea     ecx, [ebp+70h+var_2D84]
0048EC7C  call    sub_4033F0
0048EC81  mov     byte ptr [ebp+70h+var_74], 8Fh
0048EC85  mov     eax, [esi]
0048EC87  mov     eax, [eax+40h]
0048EC8A  lea     ecx, [ebp+70h+var_2D84]
0048EC90  push    ecx
0048EC91  lea     edx, [ebp+70h+var_192C]
0048EC97  push    edx
0048EC98  mov     ecx, esi
0048EC9A  call    eax
0048EC9C  mov     ecx, eax
0048EC9E  call    sub_5B05A0
0048ECA3  lea     ecx, [ebp+70h+var_192C]
0048ECA9  mov     dword_753280, eax
0048ECAE  call    sub_5B0440
0048ECB3  lea     ecx, [ebp+70h+var_2D84]; void *
0048ECB9  mov     byte ptr [ebp+70h+var_74], 0
0048ECBD  call    sub_401E90
0048ECC2  push    offset aImageReanimZom; "IMAGE_REANIM_ZOMBIE_BALLOON_OUTERARM_UP"...
0048ECC7  lea     ecx, [ebp+70h+var_2144]
0048ECCD  call    sub_4033F0
0048ECD2  lea     eax, [ebp+70h+var_2144]
0048ECD8  mov     byte ptr [ebp+70h+var_74], 90h
0048ECDC  mov     edx, [esi]
0048ECDE  mov     edx, [edx+40h]
0048ECE1  push    eax
0048ECE2  lea     ecx, [ebp+70h+var_1770]
0048ECE8  push    ecx
0048ECE9  mov     ecx, esi
0048ECEB  call    edx
0048ECED  mov     ecx, eax
0048ECEF  call    sub_5B05A0
0048ECF4  lea     ecx, [ebp+70h+var_1770]
0048ECFA  mov     dword_7536EC, eax
0048ECFF  call    sub_5B0440
0048ED04  lea     ecx, [ebp+70h+var_2144]; void *
0048ED0A  mov     byte ptr [ebp+70h+var_74], 0
0048ED0E  call    sub_401E90
0048ED13  push    offset aImageReanimZom_0; "IMAGE_REANIM_ZOMBIE_BOBSLED_OUTERARM_HA"...
0048ED18  lea     ecx, [ebp+70h+var_391C]
0048ED1E  call    sub_4033F0
0048ED23  mov     byte ptr [ebp+70h+var_74], 91h
0048ED27  mov     eax, [esi]
0048ED29  mov     eax, [eax+40h]
0048ED2C  lea     ecx, [ebp+70h+var_391C]
0048ED32  push    ecx
0048ED33  lea     edx, [ebp+70h+var_B58]
0048ED39  push    edx
0048ED3A  mov     ecx, esi
0048ED3C  call    eax
0048ED3E  mov     ecx, eax
0048ED40  call    sub_5B05A0
0048ED45  lea     ecx, [ebp+70h+var_B58]
0048ED4B  mov     dword_753584, eax
0048ED50  call    sub_5B0440
0048ED55  lea     ecx, [ebp+70h+var_391C]; void *
0048ED5B  mov     byte ptr [ebp+70h+var_74], 0
0048ED5F  call    sub_401E90
0048ED64  push    offset aImageReanimZom_1; "IMAGE_REANIM_ZOMBIE_BOBSLED_OUTERARM_UP"...
0048ED69  lea     ecx, [ebp+70h+var_217C]
0048ED6F  call    sub_4033F0
0048ED74  lea     eax, [ebp+70h+var_217C]
0048ED7A  mov     byte ptr [ebp+70h+var_74], 92h
0048ED7E  mov     edx, [esi]
0048ED80  mov     edx, [edx+40h]
0048ED83  push    eax
0048ED84  lea     ecx, [ebp+70h+var_1248]
0048ED8A  push    ecx
0048ED8B  mov     ecx, esi
0048ED8D  call    edx
0048ED8F  mov     ecx, eax
0048ED91  call    sub_5B05A0
0048ED96  lea     ecx, [ebp+70h+var_1248]
0048ED9C  mov     dword_7536CC, eax
0048EDA1  call    sub_5B0440
0048EDA6  lea     ecx, [ebp+70h+var_217C]; void *
0048EDAC  mov     byte ptr [ebp+70h+var_74], 0
0048EDB0  call    sub_401E90
0048EDB5  push    offset aImageReanimZom_2; "IMAGE_REANIM_ZOMBIE_BOSS_EYEGLOW_BLUE"
0048EDBA  lea     ecx, [ebp+70h+var_2DBC]
0048EDC0  call    sub_4033F0
0048EDC5  mov     byte ptr [ebp+70h+var_74], 93h
0048EDC9  mov     eax, [esi]
0048EDCB  mov     eax, [eax+40h]
0048EDCE  lea     ecx, [ebp+70h+var_2DBC]
0048EDD4  push    ecx
0048EDD5  lea     edx, [ebp+70h+var_B70]
0048EDDB  push    edx
0048EDDC  mov     ecx, esi
0048EDDE  call    eax
0048EDE0  mov     ecx, eax
0048EDE2  call    sub_5B05A0
0048EDE7  lea     ecx, [ebp+70h+var_B70]
0048EDED  mov     dword_75349C, eax
0048EDF2  call    sub_5B0440
0048EDF7  lea     ecx, [ebp+70h+var_2DBC]; void *
0048EDFD  mov     byte ptr [ebp+70h+var_74], 0
0048EE01  call    sub_401E90
0048EE06  push    offset aImageReanimZom_3; "IMAGE_REANIM_ZOMBIE_BOSS_FOOT_DAMAGE1"
0048EE0B  lea     ecx, [ebp+70h+var_21B4]
0048EE11  call    sub_4033F0
0048EE16  lea     eax, [ebp+70h+var_21B4]
0048EE1C  mov     byte ptr [ebp+70h+var_74], 94h
0048EE20  mov     edx, [esi]
0048EE22  mov     edx, [edx+40h]
0048EE25  push    eax
0048EE26  lea     ecx, [ebp+70h+var_15C0]
0048EE2C  push    ecx
0048EE2D  mov     ecx, esi
0048EE2F  call    edx
0048EE31  mov     ecx, eax
0048EE33  call    sub_5B05A0
0048EE38  lea     ecx, [ebp+70h+var_15C0]
0048EE3E  mov     dword_753708, eax
0048EE43  call    sub_5B0440
0048EE48  mov     byte ptr [ebp+70h+var_74], 0
0048EE4C  lea     ecx, [ebp+70h+var_21B4]; void *
0048EE52  call    sub_401E90
0048EE57  push    offset aImageReanimZom_4; "IMAGE_REANIM_ZOMBIE_BOSS_FOOT_DAMAGE2"
0048EE5C  lea     ecx, [ebp+70h+var_33DC]
0048EE62  call    sub_4033F0
0048EE67  mov     byte ptr [ebp+70h+var_74], 95h
0048EE6B  mov     eax, [esi]
0048EE6D  mov     eax, [eax+40h]
0048EE70  lea     ecx, [ebp+70h+var_33DC]
0048EE76  push    ecx
0048EE77  lea     edx, [ebp+70h+var_B88]
0048EE7D  push    edx
0048EE7E  mov     ecx, esi
0048EE80  call    eax
0048EE82  mov     ecx, eax
0048EE84  call    sub_5B05A0
0048EE89  lea     ecx, [ebp+70h+var_B88]
0048EE8F  mov     dword_752E64, eax
0048EE94  call    sub_5B0440
0048EE99  lea     ecx, [ebp+70h+var_33DC]; void *
0048EE9F  mov     byte ptr [ebp+70h+var_74], 0
0048EEA3  call    sub_401E90
0048EEA8  push    offset aImageReanimZom_5; "IMAGE_REANIM_ZOMBIE_BOSS_HEAD_DAMAGE1"
0048EEAD  lea     ecx, [ebp+70h+var_21EC]
0048EEB3  call    sub_4033F0
0048EEB8  lea     eax, [ebp+70h+var_21EC]
0048EEBE  mov     byte ptr [ebp+70h+var_74], 96h
0048EEC2  mov     edx, [esi]
0048EEC4  mov     edx, [edx+40h]
0048EEC7  push    eax
0048EEC8  lea     ecx, [ebp+70h+var_1260]
0048EECE  push    ecx
0048EECF  mov     ecx, esi
0048EED1  call    edx
0048EED3  mov     ecx, eax
0048EED5  call    sub_5B05A0
0048EEDA  lea     ecx, [ebp+70h+var_1260]
0048EEE0  mov     dword_753720, eax
0048EEE5  call    sub_5B0440
0048EEEA  lea     ecx, [ebp+70h+var_21EC]; void *
0048EEF0  mov     byte ptr [ebp+70h+var_74], 0
0048EEF4  call    sub_401E90
0048EEF9  push    offset aImageReanimZom_6; "IMAGE_REANIM_ZOMBIE_BOSS_HEAD_DAMAGE2"
0048EEFE  lea     ecx, [ebp+70h+var_2DF4]
0048EF04  call    sub_4033F0
0048EF09  mov     byte ptr [ebp+70h+var_74], 97h
0048EF0D  mov     eax, [esi]
0048EF0F  mov     eax, [eax+40h]
0048EF12  lea     ecx, [ebp+70h+var_2DF4]
0048EF18  push    ecx
0048EF19  lea     edx, [ebp+70h+var_BA0]
0048EF1F  push    edx
0048EF20  mov     ecx, esi
0048EF22  call    eax
0048EF24  mov     ecx, eax
0048EF26  call    sub_5B05A0
0048EF2B  lea     ecx, [ebp+70h+var_BA0]
0048EF31  mov     dword_752B00, eax
0048EF36  call    sub_5B0440
0048EF3B  lea     ecx, [ebp+70h+var_2DF4]; void *
0048EF41  mov     byte ptr [ebp+70h+var_74], 0
0048EF45  call    sub_401E90
0048EF4A  push    offset aImageReanimZom_7; "IMAGE_REANIM_ZOMBIE_BOSS_ICEBALL"
0048EF4F  lea     ecx, [ebp+70h+var_2224]
0048EF55  call    sub_4033F0
0048EF5A  lea     eax, [ebp+70h+var_2224]
0048EF60  mov     byte ptr [ebp+70h+var_74], 98h
0048EF64  mov     edx, [esi]
0048EF66  mov     edx, [edx+40h]
0048EF69  push    eax
0048EF6A  lea     ecx, [ebp+70h+var_18C0]
0048EF70  push    ecx
0048EF71  mov     ecx, esi
0048EF73  call    edx
0048EF75  mov     ecx, eax
0048EF77  call    sub_5B05A0
0048EF7C  mov     dword_752A90, eax
0048EF81  lea     ecx, [ebp+70h+var_18C0]
0048EF87  call    sub_5B0440
0048EF8C  lea     ecx, [ebp+70h+var_2224]; void *
0048EF92  mov     byte ptr [ebp+70h+var_74], 0
0048EF96  call    sub_401E90
0048EF9B  push    offset aImageReanimZom_8; "IMAGE_REANIM_ZOMBIE_BOSS_JAW_DAMAGE1"
0048EFA0  lea     ecx, [ebp+70h+var_36EC]
0048EFA6  call    sub_4033F0
0048EFAB  mov     byte ptr [ebp+70h+var_74], 99h
0048EFAF  mov     eax, [esi]
0048EFB1  mov     eax, [eax+40h]
0048EFB4  lea     ecx, [ebp+70h+var_36EC]
0048EFBA  push    ecx
0048EFBB  lea     edx, [ebp+70h+var_BB8]
0048EFC1  push    edx
0048EFC2  mov     ecx, esi
0048EFC4  call    eax
0048EFC6  mov     ecx, eax
0048EFC8  call    sub_5B05A0
0048EFCD  lea     ecx, [ebp+70h+var_BB8]
0048EFD3  mov     dword_75341C, eax
0048EFD8  call    sub_5B0440
0048EFDD  lea     ecx, [ebp+70h+var_36EC]; void *
0048EFE3  mov     byte ptr [ebp+70h+var_74], 0
0048EFE7  call    sub_401E90
0048EFEC  push    offset aImageReanimZom_9; "IMAGE_REANIM_ZOMBIE_BOSS_JAW_DAMAGE2"
0048EFF1  lea     ecx, [ebp+70h+var_225C]
0048EFF7  call    sub_4033F0
0048EFFC  lea     eax, [ebp+70h+var_225C]
0048F002  mov     byte ptr [ebp+70h+var_74], 9Ah
0048F006  mov     edx, [esi]
0048F008  mov     edx, [edx+40h]
0048F00B  push    eax
0048F00C  lea     ecx, [ebp+70h+var_1278]
0048F012  push    ecx
0048F013  mov     ecx, esi
0048F015  call    edx
0048F017  mov     ecx, eax
0048F019  call    sub_5B05A0
0048F01E  lea     ecx, [ebp+70h+var_1278]
0048F024  mov     dword_752B24, eax
0048F029  call    sub_5B0440
0048F02E  lea     ecx, [ebp+70h+var_225C]; void *
0048F034  mov     byte ptr [ebp+70h+var_74], 0
0048F038  call    sub_401E90
0048F03D  push    offset aImageReanimZom_10; "IMAGE_REANIM_ZOMBIE_BOSS_MOUTHGLOW_BLUE"
0048F042  lea     ecx, [ebp+70h+var_2E2C]
0048F048  call    sub_4033F0
0048F04D  mov     byte ptr [ebp+70h+var_74], 9Bh
0048F051  mov     eax, [esi]
0048F053  mov     eax, [eax+40h]
0048F056  lea     ecx, [ebp+70h+var_2E2C]
0048F05C  push    ecx
0048F05D  lea     edx, [ebp+70h+var_BD0]
0048F063  push    edx
0048F064  mov     ecx, esi
0048F066  call    eax
0048F068  mov     ecx, eax
0048F06A  call    sub_5B05A0
0048F06F  lea     ecx, [ebp+70h+var_BD0]
0048F075  mov     dword_7532C0, eax
0048F07A  call    sub_5B0440
0048F07F  lea     ecx, [ebp+70h+var_2E2C]; void *
0048F085  mov     byte ptr [ebp+70h+var_74], 0
0048F089  call    sub_401E90
0048F08E  push    offset aImageReanimZom_11; "IMAGE_REANIM_ZOMBIE_BOSS_OUTERARM_HAND_"...
0048F093  lea     ecx, [ebp+70h+var_2294]
0048F099  call    sub_4033F0
0048F09E  lea     eax, [ebp+70h+var_2294]
0048F0A4  mov     byte ptr [ebp+70h+var_74], 9Ch
0048F0A8  mov     edx, [esi]
0048F0AA  mov     edx, [edx+40h]
0048F0AD  push    eax
0048F0AE  lea     ecx, [ebp+70h+var_15D8]
0048F0B4  push    ecx
0048F0B5  mov     ecx, esi
0048F0B7  call    edx
0048F0B9  mov     ecx, eax
0048F0BB  call    sub_5B05A0
0048F0C0  lea     ecx, [ebp+70h+var_15D8]
0048F0C6  mov     dword_752B40, eax
0048F0CB  call    sub_5B0440
0048F0D0  lea     ecx, [ebp+70h+var_2294]; void *
0048F0D6  mov     byte ptr [ebp+70h+var_74], 0
0048F0DA  call    sub_401E90
0048F0DF  push    offset aImageReanimZom_12; "IMAGE_REANIM_ZOMBIE_BOSS_OUTERARM_HAND_"...
0048F0E4  lea     ecx, [ebp+70h+var_3414]
0048F0EA  call    sub_4033F0
0048F0EF  mov     byte ptr [ebp+70h+var_74], 9Dh
0048F0F3  mov     eax, [esi]
0048F0F5  mov     eax, [eax+40h]
0048F0F8  lea     ecx, [ebp+70h+var_3414]
0048F0FE  push    ecx
0048F0FF  lea     edx, [ebp+70h+var_BE8]
0048F105  push    edx
0048F106  mov     ecx, esi
0048F108  call    eax
0048F10A  mov     ecx, eax
0048F10C  call    sub_5B05A0
0048F111  lea     ecx, [ebp+70h+var_BE8]
0048F117  mov     dword_752E2C, eax
0048F11C  call    sub_5B0440
0048F121  lea     ecx, [ebp+70h+var_3414]; void *
0048F127  mov     byte ptr [ebp+70h+var_74], 0
0048F12B  call    sub_401E90
0048F130  push    offset aImageReanimZom_13; "IMAGE_REANIM_ZOMBIE_BOSS_OUTERARM_THUMB"...
0048F135  lea     ecx, [ebp+70h+var_22CC]
0048F13B  call    sub_4033F0
0048F140  lea     eax, [ebp+70h+var_22CC]
0048F146  mov     byte ptr [ebp+70h+var_74], 9Eh
0048F14A  mov     edx, [esi]
0048F14C  mov     edx, [edx+40h]
0048F14F  push    eax
0048F150  lea     ecx, [ebp+70h+var_1290]
0048F156  push    ecx
0048F157  mov     ecx, esi
0048F159  call    edx
0048F15B  mov     ecx, eax
0048F15D  call    sub_5B05A0
0048F162  lea     ecx, [ebp+70h+var_1290]
0048F168  mov     dword_75302C, eax
0048F16D  call    sub_5B0440
0048F172  lea     ecx, [ebp+70h+var_22CC]; void *
0048F178  mov     byte ptr [ebp+70h+var_74], 0
0048F17C  call    sub_401E90
0048F181  push    offset aImageReanimZom_14; "IMAGE_REANIM_ZOMBIE_BOSS_OUTERARM_THUMB"...
0048F186  lea     ecx, [ebp+70h+var_2E64]
0048F18C  call    sub_4033F0
0048F191  mov     byte ptr [ebp+70h+var_74], 9Fh
0048F195  mov     eax, [esi]
0048F197  mov     eax, [eax+40h]
0048F19A  lea     ecx, [ebp+70h+var_2E64]
0048F1A0  push    ecx
0048F1A1  lea     edx, [ebp+70h+var_C00]
0048F1A7  push    edx
0048F1A8  mov     ecx, esi
0048F1AA  call    eax
0048F1AC  mov     ecx, eax
0048F1AE  call    sub_5B05A0
0048F1B3  lea     ecx, [ebp+70h+var_C00]
0048F1B9  mov     dword_75371C, eax
0048F1BE  call    sub_5B0440
0048F1C3  lea     ecx, [ebp+70h+var_2E64]; void *
0048F1C9  mov     byte ptr [ebp+70h+var_74], 0
0048F1CD  call    sub_401E90
0048F1D2  push    offset aImageReanimZom_15; "IMAGE_REANIM_ZOMBIE_BUCKET1"
0048F1D7  lea     ecx, [ebp+70h+var_2304]
0048F1DD  call    sub_4033F0
0048F1E2  lea     eax, [ebp+70h+var_2304]
0048F1E8  lea     ecx, [ebp+70h+var_1788]
0048F1EE  push    eax
0048F1EF  mov     byte ptr [ebp+70h+var_74], 0A0h
0048F1F3  mov     edx, [esi]
0048F1F5  push    ecx
0048F1F6  mov     ecx, esi
0048F1F8  mov     edx, [edx+40h]
0048F1FB  call    edx
0048F1FD  mov     ecx, eax
0048F1FF  call    sub_5B05A0
0048F204  lea     ecx, [ebp+70h+var_1788]
0048F20A  mov     dword_752D78, eax
0048F20F  call    sub_5B0440
0048F214  lea     ecx, [ebp+70h+var_2304]; void *
0048F21A  mov     byte ptr [ebp+70h+var_74], 0
0048F21E  call    sub_401E90
0048F223  push    offset aImageReanimZom_16; "IMAGE_REANIM_ZOMBIE_BUCKET2"
0048F228  lea     ecx, [ebp+70h+var_3874]
0048F22E  call    sub_4033F0
0048F233  mov     byte ptr [ebp+70h+var_74], 0A1h
0048F237  mov     eax, [esi]
0048F239  mov     eax, [eax+40h]
0048F23C  lea     ecx, [ebp+70h+var_3874]
0048F242  push    ecx
0048F243  lea     edx, [ebp+70h+var_C18]
0048F249  push    edx
0048F24A  mov     ecx, esi
0048F24C  call    eax
0048F24E  mov     ecx, eax
0048F250  call    sub_5B05A0
0048F255  lea     ecx, [ebp+70h+var_C18]
0048F25B  mov     dword_7537C0, eax
0048F260  call    sub_5B0440
0048F265  lea     ecx, [ebp+70h+var_3874]; void *
0048F26B  mov     byte ptr [ebp+70h+var_74], 0
0048F26F  call    sub_401E90
0048F274  push    offset aImageReanimZom_17; "IMAGE_REANIM_ZOMBIE_BUCKET3"
0048F279  lea     ecx, [ebp+70h+var_233C]
0048F27F  call    sub_4033F0
0048F284  lea     eax, [ebp+70h+var_233C]
0048F28A  mov     byte ptr [ebp+70h+var_74], 0A2h
0048F28E  mov     edx, [esi]
0048F290  mov     edx, [edx+40h]
0048F293  push    eax
0048F294  lea     ecx, [ebp+70h+var_12A8]
0048F29A  push    ecx
0048F29B  mov     ecx, esi
0048F29D  call    edx
0048F29F  mov     ecx, eax
0048F2A1  call    sub_5B05A0
0048F2A6  lea     ecx, [ebp+70h+var_12A8]
0048F2AC  mov     dword_752CC4, eax
0048F2B1  call    sub_5B0440
0048F2B6  lea     ecx, [ebp+70h+var_233C]; void *
0048F2BC  mov     byte ptr [ebp+70h+var_74], 0
0048F2C0  call    sub_401E90
0048F2C5  push    offset aImageReanimZom_18; "IMAGE_REANIM_ZOMBIE_BUNGI_HEAD_SCARED"
0048F2CA  lea     ecx, [ebp+70h+var_2E9C]
0048F2D0  call    sub_4033F0
0048F2D5  mov     byte ptr [ebp+70h+var_74], 0A3h
0048F2D9  mov     eax, [esi]
0048F2DB  mov     eax, [eax+40h]
0048F2DE  lea     ecx, [ebp+70h+var_2E9C]
0048F2E4  push    ecx
0048F2E5  lea     edx, [ebp+70h+var_C30]
0048F2EB  push    edx
0048F2EC  mov     ecx, esi
0048F2EE  call    eax
0048F2F0  mov     ecx, eax
0048F2F2  call    sub_5B05A0
0048F2F7  lea     ecx, [ebp+70h+var_C30]
0048F2FD  mov     dword_7535E0, eax
0048F302  call    sub_5B0440
0048F307  lea     ecx, [ebp+70h+var_2E9C]; void *
0048F30D  mov     byte ptr [ebp+70h+var_74], 0
0048F311  call    sub_401E90
0048F316  push    offset aImageReanimZom_19; "IMAGE_REANIM_ZOMBIE_CATAPULT_BASKETBALL"
0048F31B  lea     ecx, [ebp+70h+var_2374]
0048F321  call    sub_4033F0
0048F326  lea     eax, [ebp+70h+var_2374]
0048F32C  mov     byte ptr [ebp+70h+var_74], 0A4h
0048F330  mov     edx, [esi]
0048F332  push    eax
0048F333  mov     edx, [edx+40h]
0048F336  lea     ecx, [ebp+70h+var_15F0]
0048F33C  push    ecx
0048F33D  mov     ecx, esi
0048F33F  call    edx
0048F341  mov     ecx, eax
0048F343  call    sub_5B05A0
0048F348  lea     ecx, [ebp+70h+var_15F0]
0048F34E  mov     dword_752C6C, eax
0048F353  call    sub_5B0440
0048F358  lea     ecx, [ebp+70h+var_2374]; void *
0048F35E  mov     byte ptr [ebp+70h+var_74], 0
0048F362  call    sub_401E90
0048F367  push    offset aImageReanimZom_20; "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE"
0048F36C  lea     ecx, [ebp+70h+var_344C]
0048F372  call    sub_4033F0
0048F377  mov     byte ptr [ebp+70h+var_74], 0A5h
0048F37B  mov     eax, [esi]
0048F37D  mov     eax, [eax+40h]
0048F380  lea     ecx, [ebp+70h+var_344C]
0048F386  push    ecx
0048F387  lea     edx, [ebp+70h+var_C48]
0048F38D  push    edx
0048F38E  mov     ecx, esi
0048F390  call    eax
0048F392  mov     ecx, eax
0048F394  call    sub_5B05A0
0048F399  lea     ecx, [ebp+70h+var_C48]
0048F39F  mov     dword_7537CC, eax
0048F3A4  call    sub_5B0440
0048F3A9  lea     ecx, [ebp+70h+var_344C]; void *
0048F3AF  mov     byte ptr [ebp+70h+var_74], 0
0048F3B3  call    sub_401E90
0048F3B8  push    offset aImageReanimZom_21; "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_DAMAG"...
0048F3BD  lea     ecx, [ebp+70h+var_23AC]
0048F3C3  call    sub_4033F0
0048F3C8  lea     eax, [ebp+70h+var_23AC]
0048F3CE  mov     byte ptr [ebp+70h+var_74], 0A6h
0048F3D2  mov     edx, [esi]
0048F3D4  mov     edx, [edx+40h]
0048F3D7  push    eax
0048F3D8  lea     ecx, [ebp+70h+var_12C0]
0048F3DE  push    ecx
0048F3DF  mov     ecx, esi
0048F3E1  call    edx
0048F3E3  mov     ecx, eax
0048F3E5  call    sub_5B05A0
0048F3EA  lea     ecx, [ebp+70h+var_12C0]
0048F3F0  mov     dword_75304C, eax
0048F3F5  call    sub_5B0440
0048F3FA  lea     ecx, [ebp+70h+var_23AC]; void *
0048F400  mov     byte ptr [ebp+70h+var_74], 0
0048F404  call    sub_401E90
0048F409  push    offset aImageReanimZom_22; "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_DAMAG"...
0048F40E  lea     ecx, [ebp+70h+var_2ED4]
0048F414  call    sub_4033F0
0048F419  mov     byte ptr [ebp+70h+var_74], 0A7h
0048F41D  mov     eax, [esi]
0048F41F  mov     eax, [eax+40h]
0048F422  lea     ecx, [ebp+70h+var_2ED4]
0048F428  push    ecx
0048F429  lea     edx, [ebp+70h+var_C60]
0048F42F  push    edx
0048F430  mov     ecx, esi
0048F432  call    eax
0048F434  mov     ecx, eax
0048F436  call    sub_5B05A0
0048F43B  lea     ecx, [ebp+70h+var_C60]
0048F441  mov     dword_752DE4, eax
0048F446  call    sub_5B0440
0048F44B  lea     ecx, [ebp+70h+var_2ED4]; void *
0048F451  mov     byte ptr [ebp+70h+var_74], 0
0048F455  call    sub_401E90
0048F45A  push    offset aImageReanimZom_23; "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_WITHB"...
0048F45F  lea     ecx, [ebp+70h+var_23E4]
0048F465  call    sub_4033F0
0048F46A  mov     byte ptr [ebp+70h+var_74], 0A8h
0048F46E  mov     edx, [esi]
0048F470  mov     edx, [edx+40h]
0048F473  lea     eax, [ebp+70h+var_23E4]
0048F479  push    eax
0048F47A  lea     ecx, [ebp+70h+var_1860]
0048F480  push    ecx
0048F481  mov     ecx, esi
0048F483  call    edx
0048F485  mov     ecx, eax
0048F487  call    sub_5B05A0
0048F48C  lea     ecx, [ebp+70h+var_1860]
0048F492  mov     dword_752B90, eax
0048F497  call    sub_5B0440
0048F49C  lea     ecx, [ebp+70h+var_23E4]; void *
0048F4A2  mov     byte ptr [ebp+70h+var_74], 0
0048F4A6  call    sub_401E90
0048F4AB  push    offset aImageReanimZom_24; "IMAGE_REANIM_ZOMBIE_CATAPULT_SIDING_DAM"...
0048F4B0  lea     ecx, [ebp+70h+var_3724]
0048F4B6  call    sub_4033F0
0048F4BB  mov     byte ptr [ebp+70h+var_74], 0A9h
0048F4BF  mov     eax, [esi]
0048F4C1  mov     eax, [eax+40h]
0048F4C4  lea     ecx, [ebp+70h+var_3724]
0048F4CA  push    ecx
0048F4CB  lea     edx, [ebp+70h+var_C78]
0048F4D1  push    edx
0048F4D2  mov     ecx, esi
0048F4D4  call    eax
0048F4D6  mov     ecx, eax
0048F4D8  call    sub_5B05A0
0048F4DD  lea     ecx, [ebp+70h+var_C78]
0048F4E3  mov     dword_753854, eax
0048F4E8  call    sub_5B0440
0048F4ED  lea     ecx, [ebp+70h+var_3724]; void *
0048F4F3  mov     byte ptr [ebp+70h+var_74], 0
0048F4F7  call    sub_401E90
0048F4FC  push    offset aImageReanimZom_25; "IMAGE_REANIM_ZOMBIE_CONE1"
0048F501  lea     ecx, [ebp+70h+var_241C]
0048F507  call    sub_4033F0
0048F50C  lea     eax, [ebp+70h+var_241C]
0048F512  mov     byte ptr [ebp+70h+var_74], 0AAh
0048F516  mov     edx, [esi]
0048F518  mov     edx, [edx+40h]
0048F51B  push    eax
0048F51C  lea     ecx, [ebp+70h+var_12D8]
0048F522  push    ecx
0048F523  mov     ecx, esi
0048F525  call    edx
0048F527  mov     ecx, eax
0048F529  call    sub_5B05A0
0048F52E  lea     ecx, [ebp+70h+var_12D8]
0048F534  mov     dword_752F5C, eax
0048F539  call    sub_5B0440
0048F53E  lea     ecx, [ebp+70h+var_241C]; void *
0048F544  mov     byte ptr [ebp+70h+var_74], 0
0048F548  call    sub_401E90
0048F54D  push    offset aImageReanimZom_26; "IMAGE_REANIM_ZOMBIE_CONE2"
0048F552  lea     ecx, [ebp+70h+var_2F0C]
0048F558  call    sub_4033F0
0048F55D  mov     byte ptr [ebp+70h+var_74], 0ABh
0048F561  mov     eax, [esi]
0048F563  mov     eax, [eax+40h]
0048F566  lea     ecx, [ebp+70h+var_2F0C]
0048F56C  push    ecx
0048F56D  lea     edx, [ebp+70h+var_C90]
0048F573  push    edx
0048F574  mov     ecx, esi
0048F576  call    eax
0048F578  mov     ecx, eax
0048F57A  call    sub_5B05A0
0048F57F  lea     ecx, [ebp+70h+var_C90]
0048F585  mov     dword_752DB8, eax
0048F58A  call    sub_5B0440
0048F58F  lea     ecx, [ebp+70h+var_2F0C]; void *
0048F595  mov     byte ptr [ebp+70h+var_74], 0
0048F599  call    sub_401E90
0048F59E  push    offset aImageReanimZom_27; "IMAGE_REANIM_ZOMBIE_CONE3"
0048F5A3  lea     ecx, [ebp+70h+var_2454]
0048F5A9  call    sub_4033F0
0048F5AE  lea     eax, [ebp+70h+var_2454]
0048F5B4  mov     byte ptr [ebp+70h+var_74], 0ACh
0048F5B8  mov     edx, [esi]
0048F5BA  mov     edx, [edx+40h]
0048F5BD  push    eax
0048F5BE  lea     ecx, [ebp+70h+var_1608]
0048F5C4  push    ecx
0048F5C5  mov     ecx, esi
0048F5C7  call    edx
0048F5C9  mov     ecx, eax
0048F5CB  call    sub_5B05A0
0048F5D0  lea     ecx, [ebp+70h+var_1608]
0048F5D6  mov     dword_75336C, eax
0048F5DB  call    sub_5B0440
0048F5E0  lea     ecx, [ebp+70h+var_2454]; void *
0048F5E6  mov     byte ptr [ebp+70h+var_74], 0
0048F5EA  call    sub_401E90
0048F5EF  push    offset aImageReanimZom_28; "IMAGE_REANIM_ZOMBIE_BACKUP_INNERARM_HAN"...
0048F5F4  lea     ecx, [ebp+70h+var_3484]
0048F5FA  call    sub_4033F0
0048F5FF  mov     byte ptr [ebp+70h+var_74], 0ADh
0048F603  mov     eax, [esi]
0048F605  mov     eax, [eax+40h]
0048F608  lea     ecx, [ebp+70h+var_3484]
0048F60E  push    ecx
0048F60F  lea     edx, [ebp+70h+var_CA8]
0048F615  push    edx
0048F616  mov     ecx, esi
0048F618  call    eax
0048F61A  mov     ecx, eax
0048F61C  call    sub_5B05A0
0048F621  lea     ecx, [ebp+70h+var_CA8]
0048F627  mov     dword_753304, eax
0048F62C  call    sub_5B0440
0048F631  lea     ecx, [ebp+70h+var_3484]; void *
0048F637  mov     byte ptr [ebp+70h+var_74], 0
0048F63B  call    sub_401E90
0048F640  push    offset aImageReanimZom_29; "IMAGE_REANIM_ZOMBIE_BACKUP_OUTERARM_UPP"...
0048F645  lea     ecx, [ebp+70h+var_248C]
0048F64B  call    sub_4033F0
0048F650  lea     eax, [ebp+70h+var_248C]
0048F656  mov     byte ptr [ebp+70h+var_74], 0AEh
0048F65A  mov     edx, [esi]
0048F65C  mov     edx, [edx+40h]
0048F65F  push    eax
0048F660  lea     ecx, [ebp+70h+var_12F0]
0048F666  push    ecx
0048F667  mov     ecx, esi
0048F669  call    edx
0048F66B  mov     ecx, eax
0048F66D  call    sub_5B05A0
0048F672  lea     ecx, [ebp+70h+var_12F0]
0048F678  mov     dword_753840, eax
0048F67D  call    sub_5B0440
0048F682  lea     ecx, [ebp+70h+var_248C]; void *
0048F688  mov     byte ptr [ebp+70h+var_74], 0
0048F68C  call    sub_401E90
0048F691  push    offset aImageReanimZom_30; "IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT"
0048F696  lea     ecx, [ebp+70h+var_2F44]
0048F69C  call    sub_4033F0
0048F6A1  mov     byte ptr [ebp+70h+var_74], 0AFh
0048F6A5  mov     eax, [esi]
0048F6A7  mov     eax, [eax+40h]
0048F6AA  lea     ecx, [ebp+70h+var_2F44]
0048F6B0  push    ecx
0048F6B1  lea     edx, [ebp+70h+var_CC0]
0048F6B7  push    edx
0048F6B8  mov     ecx, esi
0048F6BA  call    eax
0048F6BC  mov     ecx, eax
0048F6BE  call    sub_5B05A0
0048F6C3  lea     ecx, [ebp+70h+var_CC0]
0048F6C9  mov     dword_75383C, eax
0048F6CE  call    sub_5B0440
0048F6D3  mov     byte ptr [ebp+70h+var_74], 0
0048F6D7  lea     ecx, [ebp+70h+var_2F44]; void *
0048F6DD  call    sub_401E90
0048F6E2  push    offset aImageReanimZom_31; "IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT2"
0048F6E7  lea     ecx, [ebp+70h+var_24C4]
0048F6ED  call    sub_4033F0
0048F6F2  lea     eax, [ebp+70h+var_24C4]
0048F6F8  mov     byte ptr [ebp+70h+var_74], 0B0h
0048F6FC  mov     edx, [esi]
0048F6FE  mov     edx, [edx+40h]
0048F701  push    eax
0048F702  lea     ecx, [ebp+70h+var_17A0]
0048F708  push    ecx
0048F709  mov     ecx, esi
0048F70B  call    edx
0048F70D  mov     ecx, eax
0048F70F  call    sub_5B05A0
0048F714  lea     ecx, [ebp+70h+var_17A0]
0048F71A  mov     dword_752BC8, eax
0048F71F  call    sub_5B0440
0048F724  lea     ecx, [ebp+70h+var_24C4]; void *
0048F72A  mov     byte ptr [ebp+70h+var_74], 0
0048F72E  call    sub_401E90
0048F733  push    offset aImageReanimZom_32; "IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT3"
0048F738  lea     ecx, [ebp+70h+var_398C]
0048F73E  call    sub_4033F0
0048F743  mov     byte ptr [ebp+70h+var_74], 0B1h
0048F747  mov     eax, [esi]
0048F749  mov     eax, [eax+40h]
0048F74C  lea     ecx, [ebp+70h+var_398C]
0048F752  push    ecx
0048F753  lea     edx, [ebp+70h+var_CD8]
0048F759  push    edx
0048F75A  mov     ecx, esi
0048F75C  call    eax
0048F75E  mov     ecx, eax
0048F760  call    sub_5B05A0
0048F765  lea     ecx, [ebp+70h+var_CD8]
0048F76B  mov     dword_752F8C, eax
0048F770  call    sub_5B0440
0048F775  lea     ecx, [ebp+70h+var_398C]; void *
0048F77B  mov     byte ptr [ebp+70h+var_74], 0
0048F77F  call    sub_401E90
0048F784  push    offset aImageReanimZom_33; "IMAGE_REANIM_ZOMBIE_DIGGER_OUTERARM_UPP"...
0048F789  lea     ecx, [ebp+70h+var_24FC]
0048F78F  call    sub_4033F0
0048F794  lea     eax, [ebp+70h+var_24FC]
0048F79A  mov     byte ptr [ebp+70h+var_74], 0B2h
0048F79E  mov     edx, [esi]
0048F7A0  mov     edx, [edx+40h]
0048F7A3  push    eax
0048F7A4  lea     ecx, [ebp+70h+var_1308]
0048F7AA  push    ecx
0048F7AB  mov     ecx, esi
0048F7AD  call    edx
0048F7AF  mov     ecx, eax
0048F7B1  call    sub_5B05A0
0048F7B6  lea     ecx, [ebp+70h+var_1308]
0048F7BC  mov     dword_752F00, eax
0048F7C1  call    sub_5B0440
0048F7C6  lea     ecx, [ebp+70h+var_24FC]; void *
0048F7CC  mov     byte ptr [ebp+70h+var_74], 0
0048F7D0  call    sub_401E90
0048F7D5  push    offset aImageReanimZom_34; "IMAGE_REANIM_ZOMBIE_DIGGER_PICKAXE"
0048F7DA  lea     ecx, [ebp+70h+var_2F7C]
0048F7E0  call    sub_4033F0
0048F7E5  mov     byte ptr [ebp+70h+var_74], 0B3h
0048F7E9  mov     eax, [esi]
0048F7EB  mov     eax, [eax+40h]
0048F7EE  lea     ecx, [ebp+70h+var_2F7C]
0048F7F4  push    ecx
0048F7F5  lea     edx, [ebp+70h+var_CF0]
0048F7FB  push    edx
0048F7FC  mov     ecx, esi
0048F7FE  call    eax
0048F800  mov     ecx, eax
0048F802  call    sub_5B05A0
0048F807  mov     dword_752DC8, eax
0048F80C  lea     ecx, [ebp+70h+var_CF0]
0048F812  call    sub_5B0440
0048F817  lea     ecx, [ebp+70h+var_2F7C]; void *
0048F81D  mov     byte ptr [ebp+70h+var_74], 0
0048F821  call    sub_401E90
0048F826  push    offset aImageReanimZom_35; "IMAGE_REANIM_ZOMBIE_DOLPHINRIDER_OUTERA"...
0048F82B  lea     ecx, [ebp+70h+var_2534]
0048F831  call    sub_4033F0
0048F836  lea     eax, [ebp+70h+var_2534]
0048F83C  mov     byte ptr [ebp+70h+var_74], 0B4h
0048F840  mov     edx, [esi]
0048F842  mov     edx, [edx+40h]
0048F845  push    eax
0048F846  lea     ecx, [ebp+70h+var_1620]
0048F84C  push    ecx
0048F84D  mov     ecx, esi
0048F84F  call    edx
0048F851  mov     ecx, eax
0048F853  call    sub_5B05A0
0048F858  lea     ecx, [ebp+70h+var_1620]
0048F85E  mov     dword_752D74, eax
0048F863  call    sub_5B0440
0048F868  lea     ecx, [ebp+70h+var_2534]; void *
0048F86E  mov     byte ptr [ebp+70h+var_74], 0
0048F872  call    sub_401E90
0048F877  push    offset aImageReanimZom_36; "IMAGE_REANIM_ZOMBIE_FLAG1"
0048F87C  lea     ecx, [ebp+70h+var_34BC]
0048F882  call    sub_4033F0
0048F887  mov     byte ptr [ebp+70h+var_74], 0B5h
0048F88B  mov     eax, [esi]
0048F88D  mov     eax, [eax+40h]
0048F890  lea     ecx, [ebp+70h+var_34BC]
0048F896  push    ecx
0048F897  lea     edx, [ebp+70h+var_D08]
0048F89D  push    edx
0048F89E  mov     ecx, esi
0048F8A0  call    eax
0048F8A2  mov     ecx, eax
0048F8A4  call    sub_5B05A0
0048F8A9  lea     ecx, [ebp+70h+var_D08]
0048F8AF  mov     dword_753664, eax
0048F8B4  call    sub_5B0440
0048F8B9  lea     ecx, [ebp+70h+var_34BC]; void *
0048F8BF  mov     byte ptr [ebp+70h+var_74], 0
0048F8C3  call    sub_401E90
0048F8C8  push    offset aImageReanimZom_37; "IMAGE_REANIM_ZOMBIE_FLAG3"
0048F8CD  lea     ecx, [ebp+70h+var_256C]
0048F8D3  call    sub_4033F0
0048F8D8  lea     eax, [ebp+70h+var_256C]
0048F8DE  mov     byte ptr [ebp+70h+var_74], 0B6h
0048F8E2  mov     edx, [esi]
0048F8E4  mov     edx, [edx+40h]
0048F8E7  push    eax
0048F8E8  lea     ecx, [ebp+70h+var_1320]
0048F8EE  push    ecx
0048F8EF  mov     ecx, esi
0048F8F1  call    edx
0048F8F3  mov     ecx, eax
0048F8F5  call    sub_5B05A0
0048F8FA  lea     ecx, [ebp+70h+var_1320]
0048F900  mov     dword_7533C0, eax
0048F905  call    sub_5B0440
0048F90A  lea     ecx, [ebp+70h+var_256C]; void *
0048F910  mov     byte ptr [ebp+70h+var_74], 0
0048F914  call    sub_401E90
0048F919  push    offset aImageReanimZom_38; "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET"
0048F91E  lea     ecx, [ebp+70h+var_2FB4]
0048F924  call    sub_4033F0
0048F929  mov     byte ptr [ebp+70h+var_74], 0B7h
0048F92D  mov     eax, [esi]
0048F92F  mov     eax, [eax+40h]
0048F932  lea     ecx, [ebp+70h+var_2FB4]
0048F938  push    ecx
0048F939  lea     edx, [ebp+70h+var_D20]
0048F93F  push    edx
0048F940  mov     ecx, esi
0048F942  call    eax
0048F944  mov     ecx, eax
0048F946  call    sub_5B05A0
0048F94B  lea     ecx, [ebp+70h+var_D20]
0048F951  mov     dword_753218, eax
0048F956  call    sub_5B0440
0048F95B  lea     ecx, [ebp+70h+var_2FB4]; void *
0048F961  mov     byte ptr [ebp+70h+var_74], 0
0048F965  call    sub_401E90
0048F96A  push    offset aImageReanimZom_39; "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET2"
0048F96F  lea     ecx, [ebp+70h+var_25A4]
0048F975  call    sub_4033F0
0048F97A  lea     eax, [ebp+70h+var_25A4]
0048F980  mov     byte ptr [ebp+70h+var_74], 0B8h
0048F984  mov     edx, [esi]
0048F986  mov     edx, [edx+40h]
0048F989  push    eax
0048F98A  lea     ecx, [ebp+70h+var_1920]
0048F990  push    ecx
0048F991  mov     ecx, esi
0048F993  call    edx
0048F995  mov     ecx, eax
0048F997  call    sub_5B05A0
0048F99C  lea     ecx, [ebp+70h+var_1920]
0048F9A2  mov     dword_752D28, eax
0048F9A7  call    sub_5B0440
0048F9AC  lea     ecx, [ebp+70h+var_25A4]; void *
0048F9B2  mov     byte ptr [ebp+70h+var_74], 0
0048F9B6  call    sub_401E90
0048F9BB  push    offset aImageReanimZom_40; "IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET3"
0048F9C0  lea     ecx, [ebp+70h+var_375C]
0048F9C6  call    sub_4033F0
0048F9CB  mov     byte ptr [ebp+70h+var_74], 0B9h
0048F9CF  mov     eax, [esi]
0048F9D1  mov     eax, [eax+40h]
0048F9D4  lea     ecx, [ebp+70h+var_375C]
0048F9DA  push    ecx
0048F9DB  lea     edx, [ebp+70h+var_D38]
0048F9E1  push    edx
0048F9E2  mov     ecx, esi
0048F9E4  call    eax
0048F9E6  mov     ecx, eax
0048F9E8  call    sub_5B05A0
0048F9ED  lea     ecx, [ebp+70h+var_D38]
0048F9F3  mov     dword_7536E4, eax
0048F9F8  call    sub_5B0440
0048F9FD  lea     ecx, [ebp+70h+var_375C]; void *
0048FA03  mov     byte ptr [ebp+70h+var_74], 0
0048FA07  call    sub_401E90
0048FA0C  push    offset aImageReanimZom_41; "IMAGE_REANIM_ZOMBIE_FOOTBALL_LEFTARM_HA"...
0048FA11  lea     ecx, [ebp+70h+var_25DC]
0048FA17  call    sub_4033F0
0048FA1C  lea     eax, [ebp+70h+var_25DC]
0048FA22  mov     byte ptr [ebp+70h+var_74], 0BAh
0048FA26  mov     edx, [esi]
0048FA28  mov     edx, [edx+40h]
0048FA2B  push    eax
0048FA2C  lea     ecx, [ebp+70h+var_1338]
0048FA32  push    ecx
0048FA33  mov     ecx, esi
0048FA35  call    edx
0048FA37  mov     ecx, eax
0048FA39  call    sub_5B05A0
0048FA3E  lea     ecx, [ebp+70h+var_1338]
0048FA44  mov     dword_752EBC, eax
0048FA49  call    sub_5B0440
0048FA4E  lea     ecx, [ebp+70h+var_25DC]; void *
0048FA54  mov     byte ptr [ebp+70h+var_74], 0
0048FA58  call    sub_401E90
0048FA5D  push    offset aImageReanimZom_42; "IMAGE_REANIM_ZOMBIE_FOOTBALL_LEFTARM_UP"...
0048FA62  lea     ecx, [ebp+70h+var_2FEC]
0048FA68  call    sub_4033F0
0048FA6D  lea     ecx, [ebp+70h+var_2FEC]
0048FA73  push    ecx
0048FA74  lea     edx, [ebp+70h+var_D50]
0048FA7A  mov     byte ptr [ebp+70h+var_74], 0BBh
0048FA7E  mov     eax, [esi]
0048FA80  push    edx
0048FA81  mov     ecx, esi
0048FA83  mov     eax, [eax+40h]
0048FA86  call    eax
0048FA88  mov     ecx, eax
0048FA8A  call    sub_5B05A0
0048FA8F  lea     ecx, [ebp+70h+var_D50]
0048FA95  mov     dword_7534C4, eax
0048FA9A  call    sub_5B0440
0048FA9F  lea     ecx, [ebp+70h+var_2FEC]; void *
0048FAA5  mov     byte ptr [ebp+70h+var_74], 0
0048FAA9  call    sub_401E90
0048FAAE  push    offset aImageReanimZom_43; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_BODY1_2"
0048FAB3  lea     ecx, [ebp+70h+var_2614]
0048FAB9  call    sub_4033F0
0048FABE  lea     eax, [ebp+70h+var_2614]
0048FAC4  mov     byte ptr [ebp+70h+var_74], 0BCh
0048FAC8  mov     edx, [esi]
0048FACA  mov     edx, [edx+40h]
0048FACD  push    eax
0048FACE  lea     ecx, [ebp+70h+var_1638]
0048FAD4  push    ecx
0048FAD5  mov     ecx, esi
0048FAD7  call    edx
0048FAD9  mov     ecx, eax
0048FADB  call    sub_5B05A0
0048FAE0  lea     ecx, [ebp+70h+var_1638]
0048FAE6  mov     dword_753084, eax
0048FAEB  call    sub_5B0440
0048FAF0  lea     ecx, [ebp+70h+var_2614]; void *
0048FAF6  mov     byte ptr [ebp+70h+var_74], 0
0048FAFA  call    sub_401E90
0048FAFF  push    offset aImageReanimZom_44; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_BODY1_3"
0048FB04  lea     ecx, [ebp+70h+var_34F4]
0048FB0A  call    sub_4033F0
0048FB0F  mov     byte ptr [ebp+70h+var_74], 0BDh
0048FB13  mov     eax, [esi]
0048FB15  mov     eax, [eax+40h]
0048FB18  lea     ecx, [ebp+70h+var_34F4]
0048FB1E  push    ecx
0048FB1F  lea     edx, [ebp+70h+var_D68]
0048FB25  push    edx
0048FB26  mov     ecx, esi
0048FB28  call    eax
0048FB2A  mov     ecx, eax
0048FB2C  call    sub_5B05A0
0048FB31  lea     ecx, [ebp+70h+var_D68]
0048FB37  mov     dword_752D64, eax
0048FB3C  call    sub_5B0440
0048FB41  lea     ecx, [ebp+70h+var_34F4]; void *
0048FB47  mov     byte ptr [ebp+70h+var_74], 0
0048FB4B  call    sub_401E90
0048FB50  push    offset aImageReanimZom_45; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_DUCKXING"
0048FB55  lea     ecx, [ebp+70h+var_264C]
0048FB5B  call    sub_4033F0
0048FB60  lea     eax, [ebp+70h+var_264C]
0048FB66  mov     byte ptr [ebp+70h+var_74], 0BEh
0048FB6A  mov     edx, [esi]
0048FB6C  mov     edx, [edx+40h]
0048FB6F  push    eax
0048FB70  lea     ecx, [ebp+70h+var_1350]
0048FB76  push    ecx
0048FB77  mov     ecx, esi
0048FB79  call    edx
0048FB7B  mov     ecx, eax
0048FB7D  call    sub_5B05A0
0048FB82  lea     ecx, [ebp+70h+var_1350]
0048FB88  mov     dword_752F10, eax
0048FB8D  call    sub_5B0440
0048FB92  lea     ecx, [ebp+70h+var_264C]; void *
0048FB98  mov     byte ptr [ebp+70h+var_74], 0
0048FB9C  call    sub_401E90
0048FBA1  push    offset aImageReanimZom_46; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_FOOT2"
0048FBA6  lea     ecx, [ebp+70h+var_3024]
0048FBAC  call    sub_4033F0
0048FBB1  lea     ecx, [ebp+70h+var_3024]
0048FBB7  mov     byte ptr [ebp+70h+var_74], 0BFh
0048FBBB  mov     eax, [esi]
0048FBBD  push    ecx
0048FBBE  mov     eax, [eax+40h]
0048FBC1  lea     edx, [ebp+70h+var_D80]
0048FBC7  push    edx
0048FBC8  mov     ecx, esi
0048FBCA  call    eax
0048FBCC  mov     ecx, eax
0048FBCE  call    sub_5B05A0
0048FBD3  lea     ecx, [ebp+70h+var_D80]
0048FBD9  mov     dword_7531FC, eax
0048FBDE  call    sub_5B0440
0048FBE3  lea     ecx, [ebp+70h+var_3024]; void *
0048FBE9  mov     byte ptr [ebp+70h+var_74], 0
0048FBED  call    sub_401E90
0048FBF2  push    offset aImageReanimZom_47; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD2"
0048FBF7  lea     ecx, [ebp+70h+var_2684]
0048FBFD  call    sub_4033F0
0048FC02  lea     eax, [ebp+70h+var_2684]
0048FC08  mov     byte ptr [ebp+70h+var_74], 0C0h
0048FC0C  mov     edx, [esi]
0048FC0E  mov     edx, [edx+40h]
0048FC11  push    eax
0048FC12  lea     ecx, [ebp+70h+var_17B8]
0048FC18  push    ecx
0048FC19  mov     ecx, esi
0048FC1B  call    edx
0048FC1D  mov     ecx, eax
0048FC1F  call    sub_5B05A0
0048FC24  lea     ecx, [ebp+70h+var_17B8]
0048FC2A  mov     dword_7533C8, eax
0048FC2F  call    sub_5B0440
0048FC34  lea     ecx, [ebp+70h+var_2684]; void *
0048FC3A  mov     byte ptr [ebp+70h+var_74], 0
0048FC3E  call    sub_401E90
0048FC43  push    offset aImageReanimZom_48; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD2_RE"...
0048FC48  lea     ecx, [ebp+70h+var_38AC]
0048FC4E  call    sub_4033F0
0048FC53  mov     byte ptr [ebp+70h+var_74], 0C1h
0048FC57  mov     eax, [esi]
0048FC59  mov     eax, [eax+40h]
0048FC5C  lea     ecx, [ebp+70h+var_38AC]
0048FC62  push    ecx
0048FC63  lea     edx, [ebp+70h+var_D98]
0048FC69  push    edx
0048FC6A  mov     ecx, esi
0048FC6C  call    eax
0048FC6E  mov     ecx, eax
0048FC70  call    sub_5B05A0
0048FC75  lea     ecx, [ebp+70h+var_D98]
0048FC7B  mov     dword_75319C, eax
0048FC80  call    sub_5B0440
0048FC85  lea     ecx, [ebp+70h+var_38AC]; void *
0048FC8B  mov     byte ptr [ebp+70h+var_74], 0
0048FC8F  call    sub_401E90
0048FC94  push    offset aImageReanimZom_49; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD_RED"...
0048FC99  lea     ecx, [ebp+70h+var_26BC]
0048FC9F  call    sub_4033F0
0048FCA4  lea     eax, [ebp+70h+var_26BC]
0048FCAA  mov     byte ptr [ebp+70h+var_74], 0C2h
0048FCAE  mov     edx, [esi]
0048FCB0  mov     edx, [edx+40h]
0048FCB3  push    eax
0048FCB4  lea     ecx, [ebp+70h+var_1368]
0048FCBA  push    ecx
0048FCBB  mov     ecx, esi
0048FCBD  call    edx
0048FCBF  mov     ecx, eax
0048FCC1  call    sub_5B05A0
0048FCC6  lea     ecx, [ebp+70h+var_1368]
0048FCCC  mov     dword_7532F4, eax
0048FCD1  call    sub_5B0440
0048FCD6  lea     ecx, [ebp+70h+var_26BC]; void *
0048FCDC  mov     byte ptr [ebp+70h+var_74], 0
0048FCE0  call    sub_401E90
0048FCE5  push    offset aImageReanimZom_50; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_OUTERARM"...
0048FCEA  lea     ecx, [ebp+70h+var_305C]
0048FCF0  call    sub_4033F0
0048FCF5  mov     byte ptr [ebp+70h+var_74], 0C3h
0048FCF9  mov     eax, [esi]
0048FCFB  mov     eax, [eax+40h]
0048FCFE  lea     ecx, [ebp+70h+var_305C]
0048FD04  push    ecx
0048FD05  lea     edx, [ebp+70h+var_DB0]
0048FD0B  push    edx
0048FD0C  mov     ecx, esi
0048FD0E  call    eax
0048FD10  mov     ecx, eax
0048FD12  call    sub_5B05A0
0048FD17  lea     ecx, [ebp+70h+var_DB0]
0048FD1D  mov     dword_752EB8, eax
0048FD22  call    sub_5B0440
0048FD27  lea     ecx, [ebp+70h+var_305C]; void *
0048FD2D  mov     byte ptr [ebp+70h+var_74], 0
0048FD31  call    sub_401E90
0048FD36  push    offset aImageReanimZom_51; "IMAGE_REANIM_ZOMBIE_GARGANTUAR_ZOMBIE"
0048FD3B  lea     ecx, [ebp+70h+var_26F4]
0048FD41  call    sub_4033F0
0048FD46  lea     eax, [ebp+70h+var_26F4]
0048FD4C  mov     byte ptr [ebp+70h+var_74], 0C4h
0048FD50  mov     edx, [esi]
0048FD52  mov     edx, [edx+40h]
0048FD55  push    eax
0048FD56  lea     ecx, [ebp+70h+var_1650]
0048FD5C  push    ecx
0048FD5D  mov     ecx, esi
0048FD5F  call    edx
0048FD61  mov     ecx, eax
0048FD63  call    sub_5B05A0
0048FD68  lea     ecx, [ebp+70h+var_1650]
0048FD6E  mov     dword_752F84, eax
0048FD73  call    sub_5B0440
0048FD78  lea     ecx, [ebp+70h+var_26F4]; void *
0048FD7E  mov     byte ptr [ebp+70h+var_74], 0
0048FD82  call    sub_401E90
0048FD87  push    offset aImageReanimZom_52; "IMAGE_REANIM_ZOMBIE_HEAD_GROSSOUT"
0048FD8C  lea     ecx, [ebp+70h+var_352C]
0048FD92  call    sub_4033F0
0048FD97  mov     byte ptr [ebp+70h+var_74], 0C5h
0048FD9B  mov     eax, [esi]
0048FD9D  mov     eax, [eax+40h]
0048FDA0  lea     ecx, [ebp+70h+var_352C]
0048FDA6  push    ecx
0048FDA7  lea     edx, [ebp+70h+var_DC8]
0048FDAD  push    edx
0048FDAE  mov     ecx, esi
0048FDB0  call    eax
0048FDB2  mov     ecx, eax
0048FDB4  call    sub_5B05A0
0048FDB9  lea     ecx, [ebp+70h+var_DC8]
0048FDBF  mov     dword_753580, eax
0048FDC4  call    sub_5B0440
0048FDC9  lea     ecx, [ebp+70h+var_352C]; void *
0048FDCF  mov     byte ptr [ebp+70h+var_74], 0
0048FDD3  call    sub_401E90
0048FDD8  push    offset aImageReanimZom_53; "IMAGE_REANIM_ZOMBIE_HEAD_SUNGLASSES1"
0048FDDD  lea     ecx, [ebp+70h+var_272C]
0048FDE3  call    sub_4033F0
0048FDE8  lea     eax, [ebp+70h+var_272C]
0048FDEE  mov     byte ptr [ebp+70h+var_74], 0C6h
0048FDF2  mov     edx, [esi]
0048FDF4  mov     edx, [edx+40h]
0048FDF7  push    eax
0048FDF8  lea     ecx, [ebp+70h+var_1380]
0048FDFE  push    ecx
0048FDFF  mov     ecx, esi
0048FE01  call    edx
0048FE03  mov     ecx, eax
0048FE05  call    sub_5B05A0
0048FE0A  lea     ecx, [ebp+70h+var_1380]
0048FE10  mov     dword_75316C, eax
0048FE15  call    sub_5B0440
0048FE1A  lea     ecx, [ebp+70h+var_272C]; void *
0048FE20  mov     byte ptr [ebp+70h+var_74], 0
0048FE24  call    sub_401E90
0048FE29  push    offset aImageReanimZom_54; "IMAGE_REANIM_ZOMBIE_HEAD_SUNGLASSES2"
0048FE2E  lea     ecx, [ebp+70h+var_3094]
0048FE34  call    sub_4033F0
0048FE39  mov     byte ptr [ebp+70h+var_74], 0C7h
0048FE3D  mov     eax, [esi]
0048FE3F  mov     eax, [eax+40h]
0048FE42  lea     ecx, [ebp+70h+var_3094]
0048FE48  push    ecx
0048FE49  lea     edx, [ebp+70h+var_DE0]
0048FE4F  push    edx
0048FE50  mov     ecx, esi
0048FE52  call    eax
0048FE54  mov     ecx, eax
0048FE56  call    sub_5B05A0
0048FE5B  lea     ecx, [ebp+70h+var_DE0]
0048FE61  mov     dword_753368, eax
0048FE66  call    sub_5B0440
0048FE6B  lea     ecx, [ebp+70h+var_3094]; void *
0048FE71  mov     byte ptr [ebp+70h+var_74], 0
0048FE75  call    sub_401E90
0048FE7A  push    offset aImageReanimZom_55; "IMAGE_REANIM_ZOMBIE_HEAD_SUNGLASSES3"
0048FE7F  lea     ecx, [ebp+70h+var_2764]
0048FE85  call    sub_4033F0
0048FE8A  lea     eax, [ebp+70h+var_2764]
0048FE90  mov     byte ptr [ebp+70h+var_74], 0C8h
0048FE94  mov     edx, [esi]
0048FE96  mov     edx, [edx+40h]
0048FE99  push    eax
0048FE9A  lea     ecx, [ebp+70h+var_1878]
0048FEA0  push    ecx
0048FEA1  mov     ecx, esi
0048FEA3  call    edx
0048FEA5  mov     ecx, eax
0048FEA7  call    sub_5B05A0
0048FEAC  lea     ecx, [ebp+70h+var_1878]
0048FEB2  mov     dword_7535A0, eax
0048FEB7  call    sub_5B0440
0048FEBC  lea     ecx, [ebp+70h+var_2764]; void *
0048FEC2  mov     byte ptr [ebp+70h+var_74], 0
0048FEC6  call    sub_401E90
0048FECB  push    offset aImageReanimZom_56; "IMAGE_REANIM_ZOMBIE_HEAD_SUNGLASSES4"
0048FED0  lea     ecx, [ebp+70h+var_3794]
0048FED6  call    sub_4033F0
0048FEDB  mov     byte ptr [ebp+70h+var_74], 0C9h
0048FEDF  mov     eax, [esi]
0048FEE1  mov     eax, [eax+40h]
0048FEE4  lea     ecx, [ebp+70h+var_3794]
0048FEEA  push    ecx
0048FEEB  lea     edx, [ebp+70h+var_DF8]
0048FEF1  push    edx
0048FEF2  mov     ecx, esi
0048FEF4  call    eax
0048FEF6  mov     ecx, eax
0048FEF8  call    sub_5B05A0
0048FEFD  lea     ecx, [ebp+70h+var_DF8]
0048FF03  mov     dword_752B34, eax
0048FF08  call    sub_5B0440
0048FF0D  lea     ecx, [ebp+70h+var_3794]; void *
0048FF13  mov     byte ptr [ebp+70h+var_74], 0
0048FF17  call    sub_401E90
0048FF1C  push    offset aImageReanimZom_57; "IMAGE_REANIM_ZOMBIE_IMP_ARM1_BONE"
0048FF21  lea     ecx, [ebp+70h+var_279C]
0048FF27  call    sub_4033F0
0048FF2C  lea     eax, [ebp+70h+var_279C]
0048FF32  mov     byte ptr [ebp+70h+var_74], 0CAh
0048FF36  mov     edx, [esi]
0048FF38  mov     edx, [edx+40h]
0048FF3B  push    eax
0048FF3C  lea     ecx, [ebp+70h+var_1398]
0048FF42  push    ecx
0048FF43  mov     ecx, esi
0048FF45  call    edx
0048FF47  mov     ecx, eax
0048FF49  call    sub_5B05A0
0048FF4E  lea     ecx, [ebp+70h+var_1398]
0048FF54  mov     dword_752AF4, eax
0048FF59  call    sub_5B0440
0048FF5E  mov     byte ptr [ebp+70h+var_74], 0
0048FF62  lea     ecx, [ebp+70h+var_279C]; void *
0048FF68  call    sub_401E90
0048FF6D  push    offset aImageReanimZom_58; "IMAGE_REANIM_ZOMBIE_IMP_ARM2"
0048FF72  lea     ecx, [ebp+70h+var_30CC]
0048FF78  call    sub_4033F0
0048FF7D  mov     byte ptr [ebp+70h+var_74], 0CBh
0048FF81  mov     eax, [esi]
0048FF83  mov     eax, [eax+40h]
0048FF86  lea     ecx, [ebp+70h+var_30CC]
0048FF8C  push    ecx
0048FF8D  lea     edx, [ebp+70h+var_E10]
0048FF93  push    edx
0048FF94  mov     ecx, esi
0048FF96  call    eax
0048FF98  mov     ecx, eax
0048FF9A  call    sub_5B05A0
0048FF9F  lea     ecx, [ebp+70h+var_E10]
0048FFA5  mov     dword_752E7C, eax
0048FFAA  call    sub_5B0440
0048FFAF  lea     ecx, [ebp+70h+var_30CC]; void *
0048FFB5  mov     byte ptr [ebp+70h+var_74], 0
0048FFB9  call    sub_401E90
0048FFBE  push    offset aImageReanimZom_59; "IMAGE_REANIM_ZOMBIE_JACKBOX_BOX"
0048FFC3  lea     ecx, [ebp+70h+var_27D4]
0048FFC9  call    sub_4033F0
0048FFCE  lea     eax, [ebp+70h+var_27D4]
0048FFD4  mov     byte ptr [ebp+70h+var_74], 0CCh
0048FFD8  mov     edx, [esi]
0048FFDA  mov     edx, [edx+40h]
0048FFDD  push    eax
0048FFDE  lea     ecx, [ebp+70h+var_1668]
0048FFE4  push    ecx
0048FFE5  mov     ecx, esi
0048FFE7  call    edx
0048FFE9  mov     ecx, eax
0048FFEB  call    sub_5B05A0
0048FFF0  lea     ecx, [ebp+70h+var_1668]
0048FFF6  mov     dword_7530E4, eax
0048FFFB  call    sub_5B0440
00490000  lea     ecx, [ebp+70h+var_27D4]; void *
00490006  mov     byte ptr [ebp+70h+var_74], 0
0049000A  call    sub_401E90
0049000F  push    offset aImageReanimZom_60; "IMAGE_REANIM_ZOMBIE_JACKBOX_OUTERARM_LO"...
00490014  lea     ecx, [ebp+70h+var_3564]
0049001A  call    sub_4033F0
0049001F  mov     byte ptr [ebp+70h+var_74], 0CDh
00490023  mov     eax, [esi]
00490025  mov     eax, [eax+40h]
00490028  lea     ecx, [ebp+70h+var_3564]
0049002E  push    ecx
0049002F  lea     edx, [ebp+70h+var_E28]
00490035  push    edx
00490036  mov     ecx, esi
00490038  call    eax
0049003A  mov     ecx, eax
0049003C  call    sub_5B05A0
00490041  lea     ecx, [ebp+70h+var_E28]
00490047  mov     dword_752AE0, eax
0049004C  call    sub_5B0440
00490051  lea     ecx, [ebp+70h+var_3564]; void *
00490057  mov     byte ptr [ebp+70h+var_74], 0
0049005B  call    sub_401E90
00490060  push    offset aImageReanimZom_61; "IMAGE_REANIM_ZOMBIE_DISCO_OUTERARM_HAND"
00490065  lea     ecx, [ebp+70h+var_280C]
0049006B  call    sub_4033F0
00490070  lea     eax, [ebp+70h+var_280C]
00490076  mov     byte ptr [ebp+70h+var_74], 0CEh
0049007A  mov     edx, [esi]
0049007C  mov     edx, [edx+40h]
0049007F  push    eax
00490080  lea     ecx, [ebp+70h+var_13B0]
00490086  push    ecx
00490087  mov     ecx, esi
00490089  call    edx
0049008B  mov     ecx, eax
0049008D  call    sub_5B05A0
00490092  mov     dword_752E14, eax
00490097  lea     ecx, [ebp+70h+var_13B0]
0049009D  call    sub_5B0440
004900A2  lea     ecx, [ebp+70h+var_280C]; void *
004900A8  mov     byte ptr [ebp+70h+var_74], 0
004900AC  call    sub_401E90
004900B1  push    offset aImageReanimZom_62; "IMAGE_REANIM_ZOMBIE_DISCO_OUTERARM_UPPE"...
004900B6  lea     ecx, [ebp+70h+var_3104]
004900BC  call    sub_4033F0
004900C1  mov     byte ptr [ebp+70h+var_74], 0CFh
004900C5  mov     eax, [esi]
004900C7  mov     eax, [eax+40h]
004900CA  lea     ecx, [ebp+70h+var_3104]
004900D0  push    ecx
004900D1  lea     edx, [ebp+70h+var_E40]
004900D7  push    edx
004900D8  mov     ecx, esi
004900DA  call    eax
004900DC  mov     ecx, eax
004900DE  call    sub_5B05A0
004900E3  lea     ecx, [ebp+70h+var_E40]
004900E9  mov     dword_7536E8, eax
004900EE  call    sub_5B0440
004900F3  lea     ecx, [ebp+70h+var_3104]; void *
004900F9  mov     byte ptr [ebp+70h+var_74], 0
004900FD  call    sub_401E90
00490102  push    offset aImageReanimZom_63; "IMAGE_REANIM_ZOMBIE_LADDER_1"
00490107  lea     ecx, [ebp+70h+var_2844]
0049010D  call    sub_4033F0
00490112  lea     eax, [ebp+70h+var_2844]
00490118  mov     byte ptr [ebp+70h+var_74], 0D0h
0049011C  mov     edx, [esi]
0049011E  mov     edx, [edx+40h]
00490121  push    eax
00490122  lea     ecx, [ebp+70h+var_17D0]
00490128  push    ecx
00490129  mov     ecx, esi
0049012B  call    edx
0049012D  mov     ecx, eax
0049012F  call    sub_5B05A0
00490134  lea     ecx, [ebp+70h+var_17D0]
0049013A  mov     dword_7531C0, eax
0049013F  call    sub_5B0440
00490144  lea     ecx, [ebp+70h+var_2844]; void *
0049014A  mov     byte ptr [ebp+70h+var_74], 0
0049014E  call    sub_401E90
00490153  push    offset aImageReanimZom_64; "IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE1"
00490158  lea     ecx, [ebp+70h+var_3954]
0049015E  call    sub_4033F0
00490163  mov     byte ptr [ebp+70h+var_74], 0D1h
00490167  mov     eax, [esi]
00490169  mov     eax, [eax+40h]
0049016C  lea     ecx, [ebp+70h+var_3954]
00490172  push    ecx
00490173  lea     edx, [ebp+70h+var_E58]
00490179  push    edx
0049017A  mov     ecx, esi
0049017C  call    eax
0049017E  mov     ecx, eax
00490180  call    sub_5B05A0
00490185  lea     ecx, [ebp+70h+var_E58]
0049018B  mov     dword_75310C, eax
00490190  call    sub_5B0440
00490195  lea     ecx, [ebp+70h+var_3954]; void *
0049019B  mov     byte ptr [ebp+70h+var_74], 0
0049019F  call    sub_401E90
004901A4  push    offset aImageReanimZom_65; "IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE2"
004901A9  lea     ecx, [ebp+70h+var_287C]
004901AF  call    sub_4033F0
004901B4  lea     eax, [ebp+70h+var_287C]
004901BA  mov     byte ptr [ebp+70h+var_74], 0D2h
004901BE  mov     edx, [esi]
004901C0  mov     edx, [edx+40h]
004901C3  push    eax
004901C4  lea     ecx, [ebp+70h+var_13C8]
004901CA  push    ecx
004901CB  mov     ecx, esi
004901CD  call    edx
004901CF  mov     ecx, eax
004901D1  call    sub_5B05A0
004901D6  lea     ecx, [ebp+70h+var_13C8]
004901DC  mov     dword_753364, eax
004901E1  call    sub_5B0440
004901E6  lea     ecx, [ebp+70h+var_287C]; void *
004901EC  mov     byte ptr [ebp+70h+var_74], 0
004901F0  call    sub_401E90
004901F5  push    offset aImageReanimZom_66; "IMAGE_REANIM_ZOMBIE_LADDER_5"
004901FA  lea     ecx, [ebp+70h+var_313C]
00490200  call    sub_4033F0
00490205  mov     byte ptr [ebp+70h+var_74], 0D3h
00490209  mov     eax, [esi]
0049020B  mov     eax, [eax+40h]
0049020E  lea     ecx, [ebp+70h+var_313C]
00490214  push    ecx
00490215  lea     edx, [ebp+70h+var_E70]
0049021B  push    edx
0049021C  mov     ecx, esi
0049021E  call    eax
00490220  mov     ecx, eax
00490222  call    sub_5B05A0
00490227  lea     ecx, [ebp+70h+var_E70]
0049022D  mov     dword_7530C4, eax
00490232  call    sub_5B0440
00490237  lea     ecx, [ebp+70h+var_313C]; void *
0049023D  mov     byte ptr [ebp+70h+var_74], 0
00490241  call    sub_401E90
00490246  push    offset aImageReanimZom_67; "IMAGE_REANIM_ZOMBIE_LADDER_OUTERARM_UPP"...
0049024B  lea     ecx, [ebp+70h+var_28B4]
00490251  call    sub_4033F0
00490256  lea     eax, [ebp+70h+var_28B4]
0049025C  mov     byte ptr [ebp+70h+var_74], 0D4h
00490260  mov     edx, [esi]
00490262  mov     edx, [edx+40h]
00490265  push    eax
00490266  lea     ecx, [ebp+70h+var_1680]
0049026C  push    ecx
0049026D  mov     ecx, esi
0049026F  call    edx
00490271  mov     ecx, eax
00490273  call    sub_5B05A0
00490278  lea     ecx, [ebp+70h+var_1680]
0049027E  mov     dword_753040, eax
00490283  call    sub_5B0440
00490288  lea     ecx, [ebp+70h+var_28B4]; void *
0049028E  mov     byte ptr [ebp+70h+var_74], 0
00490292  call    sub_401E90
00490297  push    offset aImageReanimZom_68; "IMAGE_REANIM_ZOMBIE_MUSTACHE2"
0049029C  lea     ecx, [ebp+70h+var_359C]
004902A2  call    sub_4033F0
004902A7  mov     byte ptr [ebp+70h+var_74], 0D5h
004902AB  mov     eax, [esi]
004902AD  mov     eax, [eax+40h]
004902B0  lea     ecx, [ebp+70h+var_359C]
004902B6  push    ecx
004902B7  lea     edx, [ebp+70h+var_E88]
004902BD  push    edx
004902BE  mov     ecx, esi
004902C0  call    eax
004902C2  mov     ecx, eax
004902C4  call    sub_5B05A0
004902C9  lea     ecx, [ebp+70h+var_E88]
004902CF  mov     dword_7537F0, eax
004902D4  call    sub_5B0440
004902D9  lea     ecx, [ebp+70h+var_359C]; void *
004902DF  mov     byte ptr [ebp+70h+var_74], 0
004902E3  call    sub_401E90
004902E8  push    offset aImageReanimZom_69; "IMAGE_REANIM_ZOMBIE_MUSTACHE3"
004902ED  lea     ecx, [ebp+70h+var_28EC]
004902F3  call    sub_4033F0
004902F8  lea     eax, [ebp+70h+var_28EC]
004902FE  lea     ecx, [ebp+70h+var_13E0]
00490304  push    eax
00490305  mov     byte ptr [ebp+70h+var_74], 0D6h
00490309  mov     edx, [esi]
0049030B  push    ecx
0049030C  mov     ecx, esi
0049030E  mov     edx, [edx+40h]
00490311  call    edx
00490313  mov     ecx, eax
00490315  call    sub_5B05A0
0049031A  lea     ecx, [ebp+70h+var_13E0]
00490320  mov     dword_752E9C, eax
00490325  call    sub_5B0440
0049032A  lea     ecx, [ebp+70h+var_28EC]; void *
00490330  mov     byte ptr [ebp+70h+var_74], 0
00490334  call    sub_401E90
00490339  push    offset aImageReanimZom_70; "IMAGE_REANIM_ZOMBIE_OUTERARM_HAND"
0049033E  lea     ecx, [ebp+70h+var_3174]
00490344  call    sub_4033F0
00490349  mov     byte ptr [ebp+70h+var_74], 0D7h
0049034D  mov     eax, [esi]
0049034F  mov     eax, [eax+40h]
00490352  lea     ecx, [ebp+70h+var_3174]
00490358  push    ecx
00490359  lea     edx, [ebp+70h+var_EA0]
0049035F  push    edx
00490360  mov     ecx, esi
00490362  call    eax
00490364  mov     ecx, eax
00490366  call    sub_5B05A0
0049036B  lea     ecx, [ebp+70h+var_EA0]
00490371  mov     dword_752D70, eax
00490376  call    sub_5B0440
0049037B  lea     ecx, [ebp+70h+var_3174]; void *
00490381  mov     byte ptr [ebp+70h+var_74], 0
00490385  call    sub_401E90
0049038A  push    offset aImageReanimZom_71; "IMAGE_REANIM_ZOMBIE_OUTERARM_UPPER2"
0049038F  lea     ecx, [ebp+70h+var_2924]
00490395  call    sub_4033F0
0049039A  lea     eax, [ebp+70h+var_2924]
004903A0  mov     byte ptr [ebp+70h+var_74], 0D8h
004903A4  mov     edx, [esi]
004903A6  mov     edx, [edx+40h]
004903A9  push    eax
004903AA  lea     ecx, [ebp+70h+var_18D8]
004903B0  push    ecx
004903B1  mov     ecx, esi
004903B3  call    edx
004903B5  mov     ecx, eax
004903B7  call    sub_5B05A0
004903BC  lea     ecx, [ebp+70h+var_18D8]
004903C2  mov     dword_7531F8, eax
004903C7  call    sub_5B0440
004903CC  lea     ecx, [ebp+70h+var_2924]; void *
004903D2  mov     byte ptr [ebp+70h+var_74], 0
004903D6  call    sub_401E90
004903DB  push    offset aImageReanimZom_72; "IMAGE_REANIM_ZOMBIE_PAPER_LEFTARM_LOWER"
004903E0  lea     ecx, [ebp+70h+var_37CC]
004903E6  call    sub_4033F0
004903EB  mov     byte ptr [ebp+70h+var_74], 0D9h
004903EF  mov     eax, [esi]
004903F1  mov     eax, [eax+40h]
004903F4  lea     ecx, [ebp+70h+var_37CC]
004903FA  push    ecx
004903FB  lea     edx, [ebp+70h+var_EB8]
00490401  push    edx
00490402  mov     ecx, esi
00490404  call    eax
00490406  mov     ecx, eax
00490408  call    sub_5B05A0
0049040D  lea     ecx, [ebp+70h+var_EB8]
00490413  mov     dword_753480, eax
00490418  call    sub_5B0440
0049041D  lea     ecx, [ebp+70h+var_37CC]; void *
00490423  mov     byte ptr [ebp+70h+var_74], 0
00490427  call    sub_401E90
0049042C  push    offset aImageReanimZom_73; "IMAGE_REANIM_ZOMBIE_PAPER_LEFTARM_UPPER"...
00490431  lea     ecx, [ebp+70h+var_295C]
00490437  call    sub_4033F0
0049043C  lea     eax, [ebp+70h+var_295C]
00490442  mov     byte ptr [ebp+70h+var_74], 0DAh
00490446  mov     edx, [esi]
00490448  push    eax
00490449  mov     edx, [edx+40h]
0049044C  lea     ecx, [ebp+70h+var_13F8]
00490452  push    ecx
00490453  mov     ecx, esi
00490455  call    edx
00490457  mov     ecx, eax
00490459  call    sub_5B05A0
0049045E  lea     ecx, [ebp+70h+var_13F8]
00490464  mov     dword_752B8C, eax
00490469  call    sub_5B0440
0049046E  lea     ecx, [ebp+70h+var_295C]; void *
00490474  mov     byte ptr [ebp+70h+var_74], 0
00490478  call    sub_401E90
0049047D  push    offset aImageReanimZom_74; "IMAGE_REANIM_ZOMBIE_PAPER_MADHEAD"
00490482  lea     ecx, [ebp+70h+var_31AC]
00490488  call    sub_4033F0
0049048D  mov     byte ptr [ebp+70h+var_74], 0DBh
00490491  mov     eax, [esi]
00490493  mov     eax, [eax+40h]
00490496  lea     ecx, [ebp+70h+var_31AC]
0049049C  push    ecx
0049049D  lea     edx, [ebp+70h+var_ED0]
004904A3  push    edx
004904A4  mov     ecx, esi
004904A6  call    eax
004904A8  mov     ecx, eax
004904AA  call    sub_5B05A0
004904AF  lea     ecx, [ebp+70h+var_ED0]
004904B5  mov     dword_753808, eax
004904BA  call    sub_5B0440
004904BF  lea     ecx, [ebp+70h+var_31AC]; void *
004904C5  mov     byte ptr [ebp+70h+var_74], 0
004904C9  call    sub_401E90
004904CE  push    offset aImageReanimZom_75; "IMAGE_REANIM_ZOMBIE_PAPER_PAPER2"
004904D3  lea     ecx, [ebp+70h+var_2994]
004904D9  call    sub_4033F0
004904DE  lea     eax, [ebp+70h+var_2994]
004904E4  mov     byte ptr [ebp+70h+var_74], 0DCh
004904E8  mov     edx, [esi]
004904EA  mov     edx, [edx+40h]
004904ED  push    eax
004904EE  lea     ecx, [ebp+70h+var_1698]
004904F4  push    ecx
004904F5  mov     ecx, esi
004904F7  call    edx
004904F9  mov     ecx, eax
004904FB  call    sub_5B05A0
00490500  lea     ecx, [ebp+70h+var_1698]
00490506  mov     dword_752BB4, eax
0049050B  call    sub_5B0440
00490510  lea     ecx, [ebp+70h+var_2994]; void *
00490516  mov     byte ptr [ebp+70h+var_74], 0
0049051A  call    sub_401E90
0049051F  push    offset aImageReanimZom_76; "IMAGE_REANIM_ZOMBIE_PAPER_PAPER3"
00490524  lea     ecx, [ebp+70h+var_1964]
0049052A  call    sub_4033F0
0049052F  mov     byte ptr [ebp+70h+var_74], 0DDh
00490533  mov     eax, [esi]
00490535  mov     eax, [eax+40h]
00490538  lea     ecx, [ebp+70h+var_1964]
0049053E  push    ecx
0049053F  lea     edx, [ebp+70h+var_EE8]
00490545  push    edx
00490546  mov     ecx, esi
00490548  call    eax
0049054A  mov     ecx, eax
0049054C  call    sub_5B05A0
00490551  lea     ecx, [ebp+70h+var_EE8]
00490557  mov     dword_752E88, eax
0049055C  call    sub_5B0440
00490561  lea     ecx, [ebp+70h+var_1964]; void *
00490567  mov     byte ptr [ebp+70h+var_74], 0
0049056B  call    sub_401E90
00490570  push    offset aImageReanimZom_77; "IMAGE_REANIM_ZOMBIE_POGO_OUTERARM_UPPER"...
00490575  lea     ecx, [ebp+70h+var_1980]
0049057B  call    sub_4033F0
00490580  mov     byte ptr [ebp+70h+var_74], 0DEh
00490584  mov     edx, [esi]
00490586  mov     edx, [edx+40h]
00490589  lea     eax, [ebp+70h+var_1980]
0049058F  push    eax
00490590  lea     ecx, [ebp+70h+var_1410]
00490596  push    ecx
00490597  mov     ecx, esi
00490599  call    edx
0049059B  mov     ecx, eax
0049059D  call    sub_5B05A0
004905A2  lea     ecx, [ebp+70h+var_1410]
004905A8  mov     dword_752FA0, eax
004905AD  call    sub_5B0440
004905B2  lea     ecx, [ebp+70h+var_1980]; void *
004905B8  mov     byte ptr [ebp+70h+var_74], 0
004905BC  call    sub_401E90
004905C1  push    offset aImageReanimZom_78; "IMAGE_REANIM_ZOMBIE_POGO_STICK2DAMAGE1"
004905C6  lea     ecx, [ebp+70h+var_19B8]
004905CC  call    sub_4033F0
004905D1  mov     byte ptr [ebp+70h+var_74], 0DFh
004905D5  mov     eax, [esi]
004905D7  mov     eax, [eax+40h]
004905DA  lea     ecx, [ebp+70h+var_19B8]
004905E0  push    ecx
004905E1  lea     edx, [ebp+70h+var_F00]
004905E7  push    edx
004905E8  mov     ecx, esi
004905EA  call    eax
004905EC  mov     ecx, eax
004905EE  call    sub_5B05A0
004905F3  lea     ecx, [ebp+70h+var_F00]
004905F9  mov     dword_7533E4, eax
004905FE  call    sub_5B0440
00490603  lea     ecx, [ebp+70h+var_19B8]; void *
00490609  mov     byte ptr [ebp+70h+var_74], 0
0049060D  call    sub_401E90
00490612  push    offset aImageReanimZom_79; "IMAGE_REANIM_ZOMBIE_POGO_STICK2DAMAGE2"
00490617  lea     ecx, [ebp+70h+var_19F0]
0049061D  call    sub_4033F0
00490622  lea     eax, [ebp+70h+var_19F0]
00490628  mov     byte ptr [ebp+70h+var_74], 0E0h
0049062C  mov     edx, [esi]
0049062E  mov     edx, [edx+40h]
00490631  push    eax
00490632  lea     ecx, [ebp+70h+var_17E8]
00490638  push    ecx
00490639  mov     ecx, esi
0049063B  call    edx
0049063D  mov     ecx, eax
0049063F  call    sub_5B05A0
00490644  lea     ecx, [ebp+70h+var_17E8]
0049064A  mov     dword_753108, eax
0049064F  call    sub_5B0440
00490654  lea     ecx, [ebp+70h+var_19F0]; void *
0049065A  mov     byte ptr [ebp+70h+var_74], 0
0049065E  call    sub_401E90
00490663  push    offset aImageReanimZom_80; "IMAGE_REANIM_ZOMBIE_POGO_STICKDAMAGE1"
00490668  lea     ecx, [ebp+70h+var_1A28]
0049066E  call    sub_4033F0
00490673  mov     byte ptr [ebp+70h+var_74], 0E1h
00490677  mov     eax, [esi]
00490679  mov     eax, [eax+40h]
0049067C  lea     ecx, [ebp+70h+var_1A28]
00490682  push    ecx
00490683  lea     edx, [ebp+70h+var_F18]
00490689  push    edx
0049068A  mov     ecx, esi
0049068C  call    eax
0049068E  mov     ecx, eax
00490690  call    sub_5B05A0
00490695  lea     ecx, [ebp+70h+var_F18]
0049069B  mov     dword_7534B4, eax
004906A0  call    sub_5B0440
004906A5  lea     ecx, [ebp+70h+var_1A28]; void *
004906AB  mov     byte ptr [ebp+70h+var_74], 0
004906AF  call    sub_401E90
004906B4  push    offset aImageReanimZom_81; "IMAGE_REANIM_ZOMBIE_POGO_STICKDAMAGE2"
004906B9  lea     ecx, [ebp+70h+var_1A60]
004906BF  call    sub_4033F0
004906C4  lea     eax, [ebp+70h+var_1A60]
004906CA  mov     byte ptr [ebp+70h+var_74], 0E2h
004906CE  mov     edx, [esi]
004906D0  mov     edx, [edx+40h]
004906D3  push    eax
004906D4  lea     ecx, [ebp+70h+var_1428]
004906DA  push    ecx
004906DB  mov     ecx, esi
004906DD  call    edx
004906DF  mov     ecx, eax
004906E1  call    sub_5B05A0
004906E6  lea     ecx, [ebp+70h+var_1428]
004906EC  mov     dword_75343C, eax
004906F1  call    sub_5B0440
004906F6  lea     ecx, [ebp+70h+var_1A60]; void *
004906FC  mov     byte ptr [ebp+70h+var_74], 0
00490700  call    sub_401E90
00490705  push    offset aImageReanimZom_82; "IMAGE_REANIM_ZOMBIE_POGO_STICKHANDS2"
0049070A  lea     ecx, [ebp+70h+var_1A98]
00490710  call    sub_4033F0
00490715  mov     byte ptr [ebp+70h+var_74], 0E3h
00490719  mov     eax, [esi]
0049071B  mov     eax, [eax+40h]
0049071E  lea     ecx, [ebp+70h+var_1A98]
00490724  push    ecx
00490725  lea     edx, [ebp+70h+var_F30]
0049072B  push    edx
0049072C  mov     ecx, esi
0049072E  call    eax
00490730  mov     ecx, eax
00490732  call    sub_5B05A0
00490737  lea     ecx, [ebp+70h+var_F30]
0049073D  mov     dword_752F58, eax
00490742  call    sub_5B0440
00490747  lea     ecx, [ebp+70h+var_1A98]; void *
0049074D  mov     byte ptr [ebp+70h+var_74], 0
00490751  call    sub_401E90
00490756  push    offset aImageReanimZom_83; "IMAGE_REANIM_ZOMBIE_POLEVAULTER_OUTERAR"...
0049075B  lea     ecx, [ebp+70h+var_1AD0]
00490761  call    sub_4033F0
00490766  lea     eax, [ebp+70h+var_1AD0]
0049076C  mov     byte ptr [ebp+70h+var_74], 0E4h
00490770  mov     edx, [esi]
00490772  mov     edx, [edx+40h]
00490775  push    eax
00490776  lea     ecx, [ebp+70h+var_16B0]
0049077C  push    ecx
0049077D  mov     ecx, esi
0049077F  call    edx
00490781  mov     ecx, eax
00490783  call    sub_5B05A0
00490788  lea     ecx, [ebp+70h+var_16B0]
0049078E  mov     dword_752ECC, eax
00490793  call    sub_5B0440
00490798  lea     ecx, [ebp+70h+var_1AD0]; void *
0049079E  mov     byte ptr [ebp+70h+var_74], 0
004907A2  call    sub_401E90
004907A7  push    offset aImageReanimZom_84; "IMAGE_REANIM_ZOMBIE_SCREENDOOR1"
004907AC  lea     ecx, [ebp+70h+var_1B08]
004907B2  call    sub_4033F0
004907B7  mov     byte ptr [ebp+70h+var_74], 0E5h
004907BB  mov     eax, [esi]
004907BD  mov     eax, [eax+40h]
004907C0  lea     ecx, [ebp+70h+var_1B08]
004907C6  push    ecx
004907C7  lea     edx, [ebp+70h+var_F48]
004907CD  push    edx
004907CE  mov     ecx, esi
004907D0  call    eax
004907D2  mov     ecx, eax
004907D4  call    sub_5B05A0
004907D9  lea     ecx, [ebp+70h+var_F48]
004907DF  mov     dword_752D58, eax
004907E4  call    sub_5B0440
004907E9  mov     byte ptr [ebp+70h+var_74], 0
004907ED  lea     ecx, [ebp+70h+var_1B08]; void *
004907F3  call    sub_401E90
004907F8  push    offset aImageReanimZom_85; "IMAGE_REANIM_ZOMBIE_SCREENDOOR2"
004907FD  lea     ecx, [ebp+70h+var_1B40]
00490803  call    sub_4033F0
00490808  lea     eax, [ebp+70h+var_1B40]
0049080E  mov     byte ptr [ebp+70h+var_74], 0E6h
00490812  mov     edx, [esi]
00490814  mov     edx, [edx+40h]
00490817  push    eax
00490818  lea     ecx, [ebp+70h+var_1440]
0049081E  push    ecx
0049081F  mov     ecx, esi
00490821  call    edx
00490823  mov     ecx, eax
00490825  call    sub_5B05A0
0049082A  lea     ecx, [ebp+70h+var_1440]
00490830  mov     dword_753360, eax
00490835  call    sub_5B0440
0049083A  lea     ecx, [ebp+70h+var_1B40]; void *
00490840  mov     byte ptr [ebp+70h+var_74], 0
00490844  call    sub_401E90
00490849  push    offset aImageReanimZom_86; "IMAGE_REANIM_ZOMBIE_SCREENDOOR3"
0049084E  lea     ecx, [ebp+70h+var_1B78]
00490854  call    sub_4033F0
00490859  mov     byte ptr [ebp+70h+var_74], 0E7h
0049085D  mov     eax, [esi]
0049085F  mov     eax, [eax+40h]
00490862  lea     ecx, [ebp+70h+var_1B78]
00490868  push    ecx
00490869  lea     edx, [ebp+70h+var_F60]
0049086F  push    edx
00490870  mov     ecx, esi
00490872  call    eax
00490874  mov     ecx, eax
00490876  call    sub_5B05A0
0049087B  lea     ecx, [ebp+70h+var_F60]
00490881  mov     dword_753198, eax
00490886  call    sub_5B0440
0049088B  lea     ecx, [ebp+70h+var_1B78]; void *
00490891  mov     byte ptr [ebp+70h+var_74], 0
00490895  call    sub_401E90
0049089A  push    offset aImageReanimZom_87; "IMAGE_REANIM_ZOMBIE_SNORKLE_HEAD"
0049089F  lea     ecx, [ebp+70h+var_1BB0]
004908A5  call    sub_4033F0
004908AA  lea     eax, [ebp+70h+var_1BB0]
004908B0  mov     byte ptr [ebp+70h+var_74], 0E8h
004908B4  mov     edx, [esi]
004908B6  mov     edx, [edx+40h]
004908B9  push    eax
004908BA  lea     ecx, [ebp+70h+var_1890]
004908C0  push    ecx
004908C1  mov     ecx, esi
004908C3  call    edx
004908C5  mov     ecx, eax
004908C7  call    sub_5B05A0
004908CC  lea     ecx, [ebp+70h+var_1890]
004908D2  mov     dword_753578, eax
004908D7  call    sub_5B0440
004908DC  lea     ecx, [ebp+70h+var_1BB0]; void *
004908E2  mov     byte ptr [ebp+70h+var_74], 0
004908E6  call    sub_401E90
004908EB  push    offset aImageReanimZom_88; "IMAGE_REANIM_ZOMBIE_SNORKLE_OUTERARM_UP"...
004908F0  lea     ecx, [ebp+70h+var_1BE8]
004908F6  call    sub_4033F0
004908FB  mov     byte ptr [ebp+70h+var_74], 0E9h
004908FF  mov     eax, [esi]
00490901  mov     eax, [eax+40h]
00490904  lea     ecx, [ebp+70h+var_1BE8]
0049090A  push    ecx
0049090B  lea     edx, [ebp+70h+var_F78]
00490911  push    edx
00490912  mov     ecx, esi
00490914  call    eax
00490916  mov     ecx, eax
00490918  call    sub_5B05A0
0049091D  mov     dword_752EF4, eax
00490922  lea     ecx, [ebp+70h+var_F78]
00490928  call    sub_5B0440
0049092D  lea     ecx, [ebp+70h+var_1BE8]; void *
00490933  mov     byte ptr [ebp+70h+var_74], 0
00490937  call    sub_401E90
0049093C  push    offset aImageReanimZom_89; "IMAGE_REANIM_ZOMBIE_YETI_OUTERARM_HAND"
00490941  lea     ecx, [ebp+70h+var_1C20]
00490947  call    sub_4033F0
0049094C  lea     eax, [ebp+70h+var_1C20]
00490952  mov     byte ptr [ebp+70h+var_74], 0EAh
00490956  mov     edx, [esi]
00490958  mov     edx, [edx+40h]
0049095B  push    eax
0049095C  lea     ecx, [ebp+70h+var_1458]
00490962  push    ecx
00490963  mov     ecx, esi
00490965  call    edx
00490967  mov     ecx, eax
00490969  call    sub_5B05A0
0049096E  lea     ecx, [ebp+70h+var_1458]
00490974  mov     dword_753238, eax
00490979  call    sub_5B0440
0049097E  lea     ecx, [ebp+70h+var_1C20]; void *
00490984  mov     byte ptr [ebp+70h+var_74], 0
00490988  call    sub_401E90
0049098D  push    offset aImageReanimZom_90; "IMAGE_REANIM_ZOMBIE_YETI_OUTERARM_UPPER"...
00490992  lea     ecx, [ebp+70h+var_1C58]
00490998  call    sub_4033F0
0049099D  mov     byte ptr [ebp+70h+var_74], 0EBh
004909A1  mov     eax, [esi]
004909A3  mov     eax, [eax+40h]
004909A6  lea     ecx, [ebp+70h+var_1C58]
004909AC  push    ecx
004909AD  lea     edx, [ebp+70h+var_F90]
004909B3  push    edx
004909B4  mov     ecx, esi
004909B6  call    eax
004909B8  mov     ecx, eax
004909BA  call    sub_5B05A0
004909BF  lea     ecx, [ebp+70h+var_F90]
004909C5  mov     dword_752C94, eax
004909CA  call    sub_5B0440
004909CF  lea     ecx, [ebp+70h+var_1C58]; void *
004909D5  mov     byte ptr [ebp+70h+var_74], 0
004909D9  call    sub_401E90
004909DE  push    offset aImageReanimZom_91; "IMAGE_REANIM_ZOMBIE_ZAMBONI_1_DAMAGE1"
004909E3  lea     ecx, [ebp+70h+var_1C90]
004909E9  call    sub_4033F0
004909EE  lea     eax, [ebp+70h+var_1C90]
004909F4  mov     byte ptr [ebp+70h+var_74], 0ECh
004909F8  mov     edx, [esi]
004909FA  mov     edx, [edx+40h]
004909FD  push    eax
004909FE  lea     ecx, [ebp+70h+var_16C8]
00490A04  push    ecx
00490A05  mov     ecx, esi
00490A07  call    edx
00490A09  mov     ecx, eax
00490A0B  call    sub_5B05A0
00490A10  lea     ecx, [ebp+70h+var_16C8]
00490A16  mov     dword_752CF0, eax
00490A1B  call    sub_5B0440
00490A20  lea     ecx, [ebp+70h+var_1C90]; void *
00490A26  mov     byte ptr [ebp+70h+var_74], 0
00490A2A  call    sub_401E90
00490A2F  push    offset aImageReanimZom_92; "IMAGE_REANIM_ZOMBIE_ZAMBONI_1_DAMAGE2"
00490A34  lea     ecx, [ebp+70h+var_1CC8]
00490A3A  call    sub_4033F0
00490A3F  mov     byte ptr [ebp+70h+var_74], 0EDh
00490A43  mov     eax, [esi]
00490A45  mov     eax, [eax+40h]
00490A48  lea     ecx, [ebp+70h+var_1CC8]
00490A4E  push    ecx
00490A4F  lea     edx, [ebp+70h+var_FA8]
00490A55  push    edx
00490A56  mov     ecx, esi
00490A58  call    eax
00490A5A  mov     ecx, eax
00490A5C  call    sub_5B05A0
00490A61  lea     ecx, [ebp+70h+var_FA8]
00490A67  mov     dword_7533F0, eax
00490A6C  call    sub_5B0440
00490A71  lea     ecx, [ebp+70h+var_1CC8]; void *
00490A77  mov     byte ptr [ebp+70h+var_74], 0
00490A7B  call    sub_401E90
00490A80  push    offset aImageReanimZom_93; "IMAGE_REANIM_ZOMBIE_ZAMBONI_2_DAMAGE1"
00490A85  lea     ecx, [ebp+70h+var_1D00]
00490A8B  call    sub_4033F0
00490A90  lea     eax, [ebp+70h+var_1D00]
00490A96  mov     byte ptr [ebp+70h+var_74], 0EEh
00490A9A  mov     edx, [esi]
00490A9C  mov     edx, [edx+40h]
00490A9F  push    eax
00490AA0  lea     ecx, [ebp+70h+var_1470]
00490AA6  push    ecx
00490AA7  mov     ecx, esi
00490AA9  call    edx
00490AAB  mov     ecx, eax
00490AAD  call    sub_5B05A0
00490AB2  lea     ecx, [ebp+70h+var_1470]
00490AB8  mov     dword_752D40, eax
00490ABD  call    sub_5B0440
00490AC2  lea     ecx, [ebp+70h+var_1D00]; void *
00490AC8  mov     byte ptr [ebp+70h+var_74], 0
00490ACC  call    sub_401E90
00490AD1  push    offset aImageReanimZom_94; "IMAGE_REANIM_ZOMBIE_ZAMBONI_2_DAMAGE2"
00490AD6  lea     ecx, [ebp+70h+var_1D38]
00490ADC  call    sub_4033F0
00490AE1  mov     byte ptr [ebp+70h+var_74], 0EFh
00490AE5  mov     eax, [esi]
00490AE7  mov     eax, [eax+40h]
00490AEA  lea     ecx, [ebp+70h+var_1D38]
00490AF0  push    ecx
00490AF1  lea     edx, [ebp+70h+var_FC0]
00490AF7  push    edx
00490AF8  mov     ecx, esi
00490AFA  call    eax
00490AFC  mov     ecx, eax
00490AFE  call    sub_5B05A0
00490B03  lea     ecx, [ebp+70h+var_FC0]
00490B09  mov     dword_752B7C, eax
00490B0E  call    sub_5B0440
00490B13  lea     ecx, [ebp+70h+var_1D38]; void *
00490B19  mov     byte ptr [ebp+70h+var_74], 0
00490B1D  call    sub_401E90
00490B22  push    offset aImageReanimZom_95; "IMAGE_REANIM_ZOMBIE_ZAMBONI_WHEEL_FLAT"
00490B27  lea     ecx, [ebp+70h+var_1D70]
00490B2D  call    sub_4033F0
00490B32  lea     eax, [ebp+70h+var_1D70]
00490B38  mov     byte ptr [ebp+70h+var_74], 0F0h
00490B3C  mov     edx, [esi]
00490B3E  mov     edx, [edx+40h]
00490B41  push    eax
00490B42  lea     ecx, [ebp+70h+var_1800]
00490B48  push    ecx
00490B49  mov     ecx, esi
00490B4B  call    edx
00490B4D  mov     ecx, eax
00490B4F  call    sub_5B05A0
00490B54  lea     ecx, [ebp+70h+var_1800]
00490B5A  mov     dword_7530E0, eax
00490B5F  call    sub_5B0440
00490B64  lea     ecx, [ebp+70h+var_1D70]; void *
00490B6A  mov     byte ptr [ebp+70h+var_74], 0
00490B6E  call    sub_401E90
00490B73  push    offset aImageRocksmall; "IMAGE_ROCKSMALL"
00490B78  lea     ecx, [ebp+70h+var_1DA8]
00490B7E  call    sub_4033F0
00490B83  lea     ecx, [ebp+70h+var_1DA8]
00490B89  push    ecx
00490B8A  lea     edx, [ebp+70h+var_FD8]
00490B90  mov     byte ptr [ebp+70h+var_74], 0F1h
00490B94  mov     eax, [esi]
00490B96  push    edx
00490B97  mov     ecx, esi
00490B99  mov     eax, [eax+40h]
00490B9C  call    eax
00490B9E  mov     ecx, eax
00490BA0  call    sub_5B05A0
00490BA5  lea     ecx, [ebp+70h+var_FD8]
00490BAB  mov     dword_75329C, eax
00490BB0  call    sub_5B0440
00490BB5  lea     ecx, [ebp+70h+var_1DA8]; void *
00490BBB  mov     byte ptr [ebp+70h+var_74], 0
00490BBF  call    sub_401E90
00490BC4  push    offset aImageScaryPot; "IMAGE_SCARY_POT"
00490BC9  lea     ecx, [ebp+70h+var_1DE0]
00490BCF  call    sub_4033F0
00490BD4  lea     eax, [ebp+70h+var_1DE0]
00490BDA  mov     byte ptr [ebp+70h+var_74], 0F2h
00490BDE  mov     edx, [esi]
00490BE0  mov     edx, [edx+40h]
00490BE3  push    eax
00490BE4  lea     ecx, [ebp+70h+var_1488]
00490BEA  push    ecx
00490BEB  mov     ecx, esi
00490BED  call    edx
00490BEF  mov     ecx, eax
00490BF1  call    sub_5B05A0
00490BF6  lea     ecx, [ebp+70h+var_1488]
00490BFC  mov     dword_753770, eax
00490C01  call    sub_5B0440
00490C06  lea     ecx, [ebp+70h+var_1DE0]; void *
00490C0C  mov     byte ptr [ebp+70h+var_74], 0
00490C10  call    sub_401E90
00490C15  push    offset aImageSeedbank; "IMAGE_SEEDBANK"
00490C1A  lea     ecx, [ebp+70h+var_1E18]
00490C20  call    sub_4033F0
00490C25  mov     byte ptr [ebp+70h+var_74], 0F3h
00490C29  mov     eax, [esi]
00490C2B  mov     eax, [eax+40h]
00490C2E  lea     ecx, [ebp+70h+var_1E18]
00490C34  push    ecx
00490C35  lea     edx, [ebp+70h+var_FF0]
00490C3B  push    edx
00490C3C  mov     ecx, esi
00490C3E  call    eax
00490C40  mov     ecx, eax
00490C42  call    sub_5B05A0
00490C47  lea     ecx, [ebp+70h+var_FF0]
00490C4D  mov     dword_753574, eax
00490C52  call    sub_5B0440
00490C57  lea     ecx, [ebp+70h+var_1E18]; void *
00490C5D  mov     byte ptr [ebp+70h+var_74], 0
00490C61  call    sub_401E90
00490C66  push    offset aImageSeedchoos; "IMAGE_SEEDCHOOSER_BACKGROUND"
00490C6B  lea     ecx, [ebp+70h+var_1E50]
00490C71  call    sub_4033F0
00490C76  lea     eax, [ebp+70h+var_1E50]
00490C7C  mov     byte ptr [ebp+70h+var_74], 0F4h
00490C80  mov     edx, [esi]
00490C82  mov     edx, [edx+40h]
00490C85  push    eax
00490C86  lea     ecx, [ebp+70h+var_16E0]
00490C8C  push    ecx
00490C8D  mov     ecx, esi
00490C8F  call    edx
00490C91  mov     ecx, eax
00490C93  call    sub_5B05A0
00490C98  lea     ecx, [ebp+70h+var_16E0]
00490C9E  mov     dword_752E84, eax
00490CA3  call    sub_5B0440
00490CA8  lea     ecx, [ebp+70h+var_1E50]; void *
00490CAE  mov     byte ptr [ebp+70h+var_74], 0
00490CB2  call    sub_401E90
00490CB7  push    offset aImageSeedchoos_0; "IMAGE_SEEDCHOOSER_BUTTON"
00490CBC  lea     ecx, [ebp+70h+var_1E88]
00490CC2  call    sub_4033F0
00490CC7  lea     ecx, [ebp+70h+var_1E88]
00490CCD  mov     byte ptr [ebp+70h+var_74], 0F5h
00490CD1  mov     eax, [esi]
00490CD3  push    ecx
00490CD4  mov     eax, [eax+40h]
00490CD7  lea     edx, [ebp+70h+var_1008]
00490CDD  push    edx
00490CDE  mov     ecx, esi
00490CE0  call    eax
00490CE2  mov     ecx, eax
00490CE4  call    sub_5B05A0
00490CE9  lea     ecx, [ebp+70h+var_1008]
00490CEF  mov     dword_753628, eax
00490CF4  call    sub_5B0440
00490CF9  lea     ecx, [ebp+70h+var_1E88]; void *
00490CFF  mov     byte ptr [ebp+70h+var_74], 0
00490D03  call    sub_401E90
00490D08  push    offset aImageSeedchoos_1; "IMAGE_SEEDCHOOSER_BUTTON2"
00490D0D  lea     ecx, [ebp+70h+var_1EC0]
00490D13  call    sub_4033F0
00490D18  lea     eax, [ebp+70h+var_1EC0]
00490D1E  mov     byte ptr [ebp+70h+var_74], 0F6h
00490D22  mov     edx, [esi]
00490D24  mov     edx, [edx+40h]
00490D27  push    eax
00490D28  lea     ecx, [ebp+70h+var_14A0]
00490D2E  push    ecx
00490D2F  mov     ecx, esi
00490D31  call    edx
00490D33  mov     ecx, eax
00490D35  call    sub_5B05A0
00490D3A  lea     ecx, [ebp+70h+var_14A0]
00490D40  mov     dword_7530B8, eax
00490D45  call    sub_5B0440
00490D4A  lea     ecx, [ebp+70h+var_1EC0]; void *
00490D50  mov     byte ptr [ebp+70h+var_74], 0
00490D54  call    sub_401E90
00490D59  push    offset aImageSeedchoos_2; "IMAGE_SEEDCHOOSER_BUTTON2_GLOW"
00490D5E  lea     ecx, [ebp+70h+var_1EF8]
00490D64  call    sub_4033F0
00490D69  mov     byte ptr [ebp+70h+var_74], 0F7h
00490D6D  mov     eax, [esi]
00490D6F  mov     eax, [eax+40h]
00490D72  lea     ecx, [ebp+70h+var_1EF8]
00490D78  push    ecx
00490D79  lea     edx, [ebp+70h+var_1020]
00490D7F  push    edx
00490D80  mov     ecx, esi
00490D82  call    eax
00490D84  mov     ecx, eax
00490D86  call    sub_5B05A0
00490D8B  lea     ecx, [ebp+70h+var_1020]
00490D91  mov     dword_7532EC, eax
00490D96  call    sub_5B0440
00490D9B  lea     ecx, [ebp+70h+var_1EF8]; void *
00490DA1  mov     byte ptr [ebp+70h+var_74], 0
00490DA5  call    sub_401E90
00490DAA  push    offset aImageSeedchoos_3; "IMAGE_SEEDCHOOSER_BUTTON_DISABLED"
00490DAF  lea     ecx, [ebp+70h+var_1F30]
00490DB5  call    sub_4033F0
00490DBA  lea     eax, [ebp+70h+var_1F30]
00490DC0  mov     byte ptr [ebp+70h+var_74], 0F8h
00490DC4  mov     edx, [esi]
00490DC6  mov     edx, [edx+40h]
00490DC9  push    eax
00490DCA  lea     ecx, [ebp+70h+var_1908]
00490DD0  push    ecx
00490DD1  mov     ecx, esi
00490DD3  call    edx
00490DD5  mov     ecx, eax
00490DD7  call    sub_5B05A0
00490DDC  lea     ecx, [ebp+70h+var_1908]
00490DE2  mov     dword_753224, eax
00490DE7  call    sub_5B0440
00490DEC  lea     ecx, [ebp+70h+var_1F30]; void *
00490DF2  mov     byte ptr [ebp+70h+var_74], 0
00490DF6  call    sub_401E90
00490DFB  push    offset aImageSeedchoos_4; "IMAGE_SEEDCHOOSER_BUTTON_GLOW"
00490E00  lea     ecx, [ebp+70h+var_1F68]
00490E06  call    sub_4033F0
00490E0B  mov     byte ptr [ebp+70h+var_74], 0F9h
00490E0F  mov     eax, [esi]
00490E11  mov     eax, [eax+40h]
00490E14  lea     ecx, [ebp+70h+var_1F68]
00490E1A  push    ecx
00490E1B  lea     edx, [ebp+70h+var_1038]
00490E21  push    edx
00490E22  mov     ecx, esi
00490E24  call    eax
00490E26  mov     ecx, eax
00490E28  call    sub_5B05A0
00490E2D  lea     ecx, [ebp+70h+var_1038]
00490E33  mov     dword_75337C, eax
00490E38  call    sub_5B0440
00490E3D  lea     ecx, [ebp+70h+var_1F68]; void *
00490E43  mov     byte ptr [ebp+70h+var_74], 0
00490E47  call    sub_401E90
00490E4C  push    offset aImageSeedchoos_5; "IMAGE_SEEDCHOOSER_IMITATERADDON"
00490E51  lea     ecx, [ebp+70h+var_1FA0]
00490E57  call    sub_4033F0
00490E5C  lea     eax, [ebp+70h+var_1FA0]
00490E62  mov     byte ptr [ebp+70h+var_74], 0FAh
00490E66  mov     edx, [esi]
00490E68  mov     edx, [edx+40h]
00490E6B  push    eax
00490E6C  lea     ecx, [ebp+70h+var_14B8]
00490E72  push    ecx
00490E73  mov     ecx, esi
00490E75  call    edx
00490E77  mov     ecx, eax
00490E79  call    sub_5B05A0
00490E7E  lea     ecx, [ebp+70h+var_14B8]
00490E84  mov     dword_752F88, eax
00490E89  call    sub_5B0440
00490E8E  lea     ecx, [ebp+70h+var_1FA0]; void *
00490E94  mov     byte ptr [ebp+70h+var_74], 0
00490E98  call    sub_401E90
00490E9D  push    offset aImageSeedpacke; "IMAGE_SEEDPACKETFLASH"
00490EA2  lea     ecx, [ebp+70h+var_1FD8]
00490EA8  call    sub_4033F0
00490EAD  mov     byte ptr [ebp+70h+var_74], 0FBh
00490EB1  mov     eax, [esi]
00490EB3  mov     eax, [eax+40h]
00490EB6  lea     ecx, [ebp+70h+var_1FD8]
00490EBC  push    ecx
00490EBD  lea     edx, [ebp+70h+var_1050]
00490EC3  push    edx
00490EC4  mov     ecx, esi
00490EC6  call    eax
00490EC8  mov     ecx, eax
00490ECA  call    sub_5B05A0
00490ECF  lea     ecx, [ebp+70h+var_1050]
00490ED5  mov     dword_7537C8, eax
00490EDA  call    sub_5B0440
00490EDF  lea     ecx, [ebp+70h+var_1FD8]; void *
00490EE5  mov     byte ptr [ebp+70h+var_74], 0
00490EE9  call    sub_401E90
00490EEE  push    offset aImageSeedpacke_0; "IMAGE_SEEDPACKETSILHOUETTE"
00490EF3  lea     ecx, [ebp+70h+var_2010]
00490EF9  call    sub_4033F0
00490EFE  lea     eax, [ebp+70h+var_2010]
00490F04  mov     byte ptr [ebp+70h+var_74], 0FCh
00490F08  mov     edx, [esi]
00490F0A  mov     edx, [edx+40h]
00490F0D  push    eax
00490F0E  lea     ecx, [ebp+70h+var_16F8]
00490F14  push    ecx
00490F15  mov     ecx, esi
00490F17  call    edx
00490F19  mov     ecx, eax
00490F1B  call    sub_5B05A0
00490F20  lea     ecx, [ebp+70h+var_16F8]
00490F26  mov     dword_752C50, eax
00490F2B  call    sub_5B0440
00490F30  lea     ecx, [ebp+70h+var_2010]; void *
00490F36  mov     byte ptr [ebp+70h+var_74], 0
00490F3A  call    sub_401E90
00490F3F  push    offset aImageSeedpacke_1; "IMAGE_SEEDPACKET_LARGER"
00490F44  lea     ecx, [ebp+70h+var_2048]
00490F4A  call    sub_4033F0
00490F4F  mov     byte ptr [ebp+70h+var_74], 0FDh
00490F53  mov     eax, [esi]
00490F55  mov     eax, [eax+40h]
00490F58  lea     ecx, [ebp+70h+var_2048]
00490F5E  push    ecx
00490F5F  lea     edx, [ebp+70h+var_1068]
00490F65  push    edx
00490F66  mov     ecx, esi
00490F68  call    eax
00490F6A  mov     ecx, eax
00490F6C  call    sub_5B05A0
00490F71  lea     ecx, [ebp+70h+var_1068]
00490F77  mov     dword_752E40, eax
00490F7C  call    sub_5B0440
00490F81  lea     ecx, [ebp+70h+var_2048]; void *
00490F87  mov     byte ptr [ebp+70h+var_74], 0
00490F8B  call    sub_401E90
00490F90  push    offset aImageSeeds; "IMAGE_SEEDS"
00490F95  lea     ecx, [ebp+70h+var_2080]
00490F9B  call    sub_4033F0
00490FA0  lea     eax, [ebp+70h+var_2080]
00490FA6  mov     byte ptr [ebp+70h+var_74], 0FEh
00490FAA  mov     edx, [esi]
00490FAC  mov     edx, [edx+40h]
00490FAF  push    eax
00490FB0  lea     ecx, [ebp+70h+var_14D0]
00490FB6  push    ecx
00490FB7  mov     ecx, esi
00490FB9  call    edx
00490FBB  mov     ecx, eax
00490FBD  call    sub_5B05A0
00490FC2  lea     ecx, [ebp+70h+var_14D0]
00490FC8  mov     dword_752DB4, eax
00490FCD  call    sub_5B0440
00490FD2  lea     ecx, [ebp+70h+var_2080]; void *
00490FD8  mov     byte ptr [ebp+70h+var_74], 0
00490FDC  call    sub_401E90
00490FE1  push    offset aImageSelectors; "IMAGE_SELECTORSCREEN_ALMANAC"
00490FE6  lea     ecx, [ebp+70h+var_20B8]
00490FEC  call    sub_4033F0
00490FF1  mov     byte ptr [ebp+70h+var_74], 0FFh
00490FF5  mov     eax, [esi]
00490FF7  mov     eax, [eax+40h]
00490FFA  lea     ecx, [ebp+70h+var_20B8]
00491000  push    ecx
00491001  lea     edx, [ebp+70h+var_1080]
00491007  push    edx
00491008  mov     ecx, esi
0049100A  call    eax
0049100C  mov     ecx, eax
0049100E  call    sub_5B05A0
00491013  lea     ecx, [ebp+70h+var_1080]
00491019  mov     dword_7533EC, eax
0049101E  call    sub_5B0440
00491023  lea     ecx, [ebp+70h+var_20B8]; void *
00491029  mov     byte ptr [ebp+70h+var_74], 0
0049102D  call    sub_401E90
00491032  push    offset aImageSelectors_0; "IMAGE_SELECTORSCREEN_ALMANACHIGHLIGHT"
00491037  lea     ecx, [ebp+70h+var_20F0]
0049103D  call    sub_4033F0
00491042  lea     eax, [ebp+70h+var_20F0]
00491048  mov     [ebp+70h+var_74], 100h
0049104F  mov     edx, [esi]
00491051  mov     edx, [edx+40h]
00491054  push    eax
00491055  lea     ecx, [ebp+70h+var_1818]
0049105B  push    ecx
0049105C  mov     ecx, esi
0049105E  call    edx
00491060  mov     ecx, eax
00491062  call    sub_5B05A0
00491067  lea     ecx, [ebp+70h+var_1818]
0049106D  mov     dword_752BF0, eax
00491072  call    sub_5B0440
00491077  mov     [ebp+70h+var_74], edi
0049107A  lea     ecx, [ebp+70h+var_20F0]; void *
00491080  call    sub_401E90
00491085  push    offset aImageSelectors_1; "IMAGE_SELECTORSCREEN_HELP1"
0049108A  lea     ecx, [ebp+70h+var_2128]
00491090  call    sub_4033F0
00491095  mov     [ebp+70h+var_74], 101h
0049109C  mov     eax, [esi]
0049109E  mov     eax, [eax+40h]
004910A1  lea     ecx, [ebp+70h+var_2128]
004910A7  push    ecx
004910A8  lea     edx, [ebp+70h+var_1098]
004910AE  push    edx
004910AF  mov     ecx, esi
004910B1  call    eax
004910B3  mov     ecx, eax
004910B5  call    sub_5B05A0
004910BA  lea     ecx, [ebp+70h+var_1098]
004910C0  mov     dword_753104, eax
004910C5  call    sub_5B0440
004910CA  lea     ecx, [ebp+70h+var_2128]; void *
004910D0  mov     [ebp+70h+var_74], edi
004910D3  call    sub_401E90
004910D8  push    offset aImageSelectors_2; "IMAGE_SELECTORSCREEN_HELP2"
004910DD  lea     ecx, [ebp+70h+var_2160]
004910E3  call    sub_4033F0
004910E8  lea     eax, [ebp+70h+var_2160]
004910EE  mov     [ebp+70h+var_74], 102h
004910F5  mov     edx, [esi]
004910F7  mov     edx, [edx+40h]
004910FA  push    eax
004910FB  lea     ecx, [ebp+70h+var_14E8]
00491101  push    ecx
00491102  mov     ecx, esi
00491104  call    edx
00491106  mov     ecx, eax
00491108  call    sub_5B05A0
0049110D  lea     ecx, [ebp+70h+var_14E8]
00491113  mov     dword_753630, eax
00491118  call    sub_5B0440
0049111D  lea     ecx, [ebp+70h+var_2160]; void *
00491123  mov     [ebp+70h+var_74], edi
00491126  call    sub_401E90
0049112B  push    offset aImageSelectors_3; "IMAGE_SELECTORSCREEN_LEVELNUMBERS"
00491130  lea     ecx, [ebp+70h+var_2198]
00491136  call    sub_4033F0
0049113B  mov     [ebp+70h+var_74], 103h
00491142  mov     eax, [esi]
00491144  mov     eax, [eax+40h]
00491147  lea     ecx, [ebp+70h+var_2198]
0049114D  push    ecx
0049114E  lea     edx, [ebp+70h+var_10B0]
00491154  push    edx
00491155  mov     ecx, esi
00491157  call    eax
00491159  mov     ecx, eax
0049115B  call    sub_5B05A0
00491160  lea     ecx, [ebp+70h+var_10B0]
00491166  mov     dword_7537AC, eax
0049116B  call    sub_5B0440
00491170  lea     ecx, [ebp+70h+var_2198]; void *
00491176  mov     [ebp+70h+var_74], edi
00491179  call    sub_401E90
0049117E  push    offset aImageSelectors_4; "IMAGE_SELECTORSCREEN_OPTIONS1"
00491183  lea     ecx, [ebp+70h+var_21D0]
00491189  call    sub_4033F0
0049118E  lea     eax, [ebp+70h+var_21D0]
00491194  mov     [ebp+70h+var_74], 104h
0049119B  mov     edx, [esi]
0049119D  mov     edx, [edx+40h]
004911A0  push    eax
004911A1  lea     ecx, [ebp+70h+var_1710]
004911A7  push    ecx
004911A8  mov     ecx, esi
004911AA  call    edx
004911AC  mov     ecx, eax
004911AE  call    sub_5B05A0
004911B3  mov     dword_753034, eax
004911B8  lea     ecx, [ebp+70h+var_1710]
004911BE  call    sub_5B0440
004911C3  lea     ecx, [ebp+70h+var_21D0]; void *
004911C9  mov     [ebp+70h+var_74], edi
004911CC  call    sub_401E90
004911D1  push    offset aImageSelectors_5; "IMAGE_SELECTORSCREEN_OPTIONS2"
004911D6  lea     ecx, [ebp+70h+var_2208]
004911DC  call    sub_4033F0
004911E1  mov     [ebp+70h+var_74], 105h
004911E8  mov     eax, [esi]
004911EA  mov     eax, [eax+40h]
004911ED  lea     ecx, [ebp+70h+var_2208]
004911F3  push    ecx
004911F4  lea     edx, [ebp+70h+var_10C8]
004911FA  push    edx
004911FB  mov     ecx, esi
004911FD  call    eax
004911FF  mov     ecx, eax
00491201  call    sub_5B05A0
00491206  lea     ecx, [ebp+70h+var_10C8]
0049120C  mov     dword_7530DC, eax
00491211  call    sub_5B0440
00491216  lea     ecx, [ebp+70h+var_2208]; void *
0049121C  mov     [ebp+70h+var_74], edi
0049121F  call    sub_401E90
00491224  push    offset aImageSelectors_6; "IMAGE_SELECTORSCREEN_QUIT1"
00491229  lea     ecx, [ebp+70h+var_2240]
0049122F  call    sub_4033F0
00491234  lea     eax, [ebp+70h+var_2240]
0049123A  mov     [ebp+70h+var_74], 106h
00491241  mov     edx, [esi]
00491243  mov     edx, [edx+40h]
00491246  push    eax
00491247  lea     ecx, [ebp+70h+var_1500]
0049124D  push    ecx
0049124E  mov     ecx, esi
00491250  call    edx
00491252  mov     ecx, eax
00491254  call    sub_5B05A0
00491259  lea     ecx, [ebp+70h+var_1500]
0049125F  mov     dword_752F80, eax
00491264  call    sub_5B0440
00491269  lea     ecx, [ebp+70h+var_2240]; void *
0049126F  mov     [ebp+70h+var_74], edi
00491272  call    sub_401E90
00491277  push    offset aImageSelectors_7; "IMAGE_SELECTORSCREEN_QUIT2"
0049127C  lea     ecx, [ebp+70h+var_2278]
00491282  call    sub_4033F0
00491287  mov     [ebp+70h+var_74], 107h
0049128E  mov     eax, [esi]
00491290  mov     eax, [eax+40h]
00491293  lea     ecx, [ebp+70h+var_2278]
00491299  push    ecx
0049129A  lea     edx, [ebp+70h+var_10E0]
004912A0  push    edx
004912A1  mov     ecx, esi
004912A3  call    eax
004912A5  mov     ecx, eax
004912A7  call    sub_5B05A0
004912AC  lea     ecx, [ebp+70h+var_10E0]
004912B2  mov     dword_752BA4, eax
004912B7  call    sub_5B0440
004912BC  lea     ecx, [ebp+70h+var_2278]; void *
004912C2  mov     [ebp+70h+var_74], edi
004912C5  call    sub_401E90
004912CA  push    offset aImageSelectors_8; "IMAGE_SELECTORSCREEN_STORE"
004912CF  lea     ecx, [ebp+70h+var_22B0]
004912D5  call    sub_4033F0
004912DA  lea     eax, [ebp+70h+var_22B0]
004912E0  mov     [ebp+70h+var_74], 108h
004912E7  mov     edx, [esi]
004912E9  mov     edx, [edx+40h]
004912EC  push    eax
004912ED  lea     ecx, [ebp+70h+var_18A8]
004912F3  push    ecx
004912F4  mov     ecx, esi
004912F6  call    edx
004912F8  mov     ecx, eax
004912FA  call    sub_5B05A0
004912FF  lea     ecx, [ebp+70h+var_18A8]
00491305  mov     dword_752C44, eax
0049130A  call    sub_5B0440
0049130F  lea     ecx, [ebp+70h+var_22B0]; void *
00491315  mov     [ebp+70h+var_74], edi
00491318  call    sub_401E90
0049131D  push    offset aImageSelectors_9; "IMAGE_SELECTORSCREEN_STOREHIGHLIGHT"
00491322  lea     ecx, [ebp+70h+var_22E8]
00491328  call    sub_4033F0
0049132D  mov     [ebp+70h+var_74], 109h
00491334  mov     eax, [esi]
00491336  mov     eax, [eax+40h]
00491339  lea     ecx, [ebp+70h+var_22E8]
0049133F  push    ecx
00491340  lea     edx, [ebp+70h+var_10F8]
00491346  push    edx
00491347  mov     ecx, esi
00491349  call    eax
0049134B  mov     ecx, eax
0049134D  call    sub_5B05A0
00491352  lea     ecx, [ebp+70h+var_10F8]
00491358  mov     dword_753404, eax
0049135D  call    sub_5B0440
00491362  lea     ecx, [ebp+70h+var_22E8]; void *
00491368  mov     [ebp+70h+var_74], edi
0049136B  call    sub_401E90
00491370  push    offset aImageSelectors_10; "IMAGE_SELECTORSCREEN_ZENGARDEN"
00491375  lea     ecx, [ebp+70h+var_2320]
0049137B  call    sub_4033F0
00491380  lea     eax, [ebp+70h+var_2320]
00491386  mov     [ebp+70h+var_74], 10Ah
0049138D  mov     edx, [esi]
0049138F  mov     edx, [edx+40h]
00491392  push    eax
00491393  lea     ecx, [ebp+70h+var_1518]
00491399  push    ecx
0049139A  mov     ecx, esi
0049139C  call    edx
0049139E  mov     ecx, eax
004913A0  call    sub_5B05A0
004913A5  lea     ecx, [ebp+70h+var_1518]
004913AB  mov     dword_752E3C, eax
004913B0  call    sub_5B0440
004913B5  lea     ecx, [ebp+70h+var_2320]; void *
004913BB  mov     [ebp+70h+var_74], edi
004913BE  call    sub_401E90
004913C3  push    offset aImageSelectors_11; "IMAGE_SELECTORSCREEN_ZENGARDENHIGHLIGHT"
004913C8  lea     ecx, [ebp+70h+var_2358]
004913CE  call    sub_4033F0
004913D3  mov     [ebp+70h+var_74], 10Bh
004913DA  mov     eax, [esi]
004913DC  mov     eax, [eax+40h]
004913DF  lea     ecx, [ebp+70h+var_2358]
004913E5  push    ecx
004913E6  lea     edx, [ebp+70h+var_1110]
004913EC  push    edx
004913ED  mov     ecx, esi
004913EF  call    eax
004913F1  mov     ecx, eax
004913F3  call    sub_5B05A0
004913F8  lea     ecx, [ebp+70h+var_1110]
004913FE  mov     dword_752FBC, eax
00491403  call    sub_5B0440
00491408  lea     ecx, [ebp+70h+var_2358]; void *
0049140E  mov     [ebp+70h+var_74], edi
00491411  call    sub_401E90
00491416  push    offset aImageShovel; "IMAGE_SHOVEL"
0049141B  lea     ecx, [ebp+70h+var_2390]
00491421  call    sub_4033F0
00491426  lea     eax, [ebp+70h+var_2390]
0049142C  lea     ecx, [ebp+70h+var_1728]
00491432  push    eax
00491433  mov     [ebp+70h+var_74], 10Ch
0049143A  mov     edx, [esi]
0049143C  push    ecx
0049143D  mov     ecx, esi
0049143F  mov     edx, [edx+40h]
00491442  call    edx
00491444  mov     ecx, eax
00491446  call    sub_5B05A0
0049144B  lea     ecx, [ebp+70h+var_1728]
00491451  mov     dword_753544, eax
00491456  call    sub_5B0440
0049145B  lea     ecx, [ebp+70h+var_2390]; void *
00491461  mov     [ebp+70h+var_74], edi
00491464  call    sub_401E90
00491469  push    offset aImageShovelban; "IMAGE_SHOVELBANK"
0049146E  lea     ecx, [ebp+70h+var_23C8]
00491474  call    sub_4033F0
00491479  mov     [ebp+70h+var_74], 10Dh
00491480  mov     eax, [esi]
00491482  mov     eax, [eax+40h]
00491485  lea     ecx, [ebp+70h+var_23C8]
0049148B  push    ecx
0049148C  lea     edx, [ebp+70h+var_1128]
00491492  push    edx
00491493  mov     ecx, esi
00491495  call    eax
00491497  mov     ecx, eax
00491499  call    sub_5B05A0
0049149E  lea     ecx, [ebp+70h+var_1128]
004914A4  mov     dword_752AC0, eax
004914A9  call    sub_5B0440
004914AE  lea     ecx, [ebp+70h+var_23C8]; void *
004914B4  mov     [ebp+70h+var_74], edi
004914B7  call    sub_401E90
004914BC  push    offset aImageShovelHiR; "IMAGE_SHOVEL_HI_RES"
004914C1  lea     ecx, [ebp+70h+var_2400]
004914C7  call    sub_4033F0
004914CC  lea     eax, [ebp+70h+var_2400]
004914D2  mov     [ebp+70h+var_74], 10Eh
004914D9  mov     edx, [esi]
004914DB  mov     edx, [edx+40h]
004914DE  push    eax
004914DF  lea     ecx, [ebp+70h+var_1530]
004914E5  push    ecx
004914E6  mov     ecx, esi
004914E8  call    edx
004914EA  mov     ecx, eax
004914EC  call    sub_5B05A0
004914F1  lea     ecx, [ebp+70h+var_1530]
004914F7  mov     dword_752D0C, eax
004914FC  call    sub_5B0440
00491501  lea     ecx, [ebp+70h+var_2400]; void *
00491507  mov     [ebp+70h+var_74], edi
0049150A  call    sub_401E90
0049150F  push    offset aImageSlotmachi; "IMAGE_SLOTMACHINE_OVERLAY"
00491514  lea     ecx, [ebp+70h+var_2438]
0049151A  call    sub_4033F0
0049151F  mov     [ebp+70h+var_74], 10Fh
00491526  mov     eax, [esi]
00491528  mov     eax, [eax+40h]
0049152B  lea     ecx, [ebp+70h+var_2438]
00491531  push    ecx
00491532  lea     edx, [ebp+70h+var_1140]
00491538  push    edx
00491539  mov     ecx, esi
0049153B  call    eax
0049153D  mov     ecx, eax
0049153F  call    sub_5B05A0
00491544  lea     ecx, [ebp+70h+var_1140]
0049154A  mov     dword_753450, eax
0049154F  call    sub_5B0440
00491554  lea     ecx, [ebp+70h+var_2438]; void *
0049155A  mov     [ebp+70h+var_74], edi
0049155D  call    sub_401E90
00491562  push    offset aImageSnowflake; "IMAGE_SNOWFLAKES"
00491567  lea     ecx, [ebp+70h+var_2470]
0049156D  call    sub_4033F0
00491572  lea     eax, [ebp+70h+var_2470]
00491578  mov     [ebp+70h+var_74], 110h
0049157F  mov     edx, [esi]
00491581  push    eax
00491582  mov     edx, [edx+40h]
00491585  lea     ecx, [ebp+70h+var_1830]
0049158B  push    ecx
0049158C  mov     ecx, esi
0049158E  call    edx
00491590  mov     ecx, eax
00491592  call    sub_5B05A0
00491597  lea     ecx, [ebp+70h+var_1830]
0049159D  mov     dword_752BBC, eax
004915A2  call    sub_5B0440
004915A7  lea     ecx, [ebp+70h+var_2470]; void *
004915AD  mov     [ebp+70h+var_74], edi
004915B0  call    sub_401E90
004915B5  push    offset aImageSnowpeaPa; "IMAGE_SNOWPEA_PARTICLES"
004915BA  lea     ecx, [ebp+70h+var_24A8]
004915C0  call    sub_4033F0
004915C5  mov     [ebp+70h+var_74], 111h
004915CC  mov     eax, [esi]
004915CE  mov     eax, [eax+40h]
004915D1  lea     ecx, [ebp+70h+var_24A8]
004915D7  push    ecx
004915D8  lea     edx, [ebp+70h+var_1158]
004915DE  push    edx
004915DF  mov     ecx, esi
004915E1  call    eax
004915E3  mov     ecx, eax
004915E5  call    sub_5B05A0
004915EA  lea     ecx, [ebp+70h+var_1158]
004915F0  mov     dword_753804, eax
004915F5  call    sub_5B0440
004915FA  lea     ecx, [ebp+70h+var_24A8]; void *
00491600  mov     [ebp+70h+var_74], edi
00491603  call    sub_401E90
00491608  push    offset aImageSnowpeaPu; "IMAGE_SNOWPEA_PUFF"
0049160D  lea     ecx, [ebp+70h+var_24E0]
00491613  call    sub_4033F0
00491618  lea     eax, [ebp+70h+var_24E0]
0049161E  mov     [ebp+70h+var_74], 112h
00491625  mov     edx, [esi]
00491627  mov     edx, [edx+40h]
0049162A  push    eax
0049162B  lea     ecx, [ebp+70h+var_1548]
00491631  push    ecx
00491632  mov     ecx, esi
00491634  call    edx
00491636  mov     ecx, eax
00491638  call    sub_5B05A0
0049163D  lea     ecx, [ebp+70h+var_1548]
00491643  mov     dword_753138, eax
00491648  call    sub_5B0440
0049164D  lea     ecx, [ebp+70h+var_24E0]; void *
00491653  mov     [ebp+70h+var_74], edi
00491656  call    sub_401E90
0049165B  push    offset aImageSnowpeaSp; "IMAGE_SNOWPEA_SPLATS"
00491660  lea     ecx, [ebp+70h+var_2518]
00491666  call    sub_4033F0
0049166B  mov     [ebp+70h+var_74], 113h
00491672  mov     eax, [esi]
00491674  mov     eax, [eax+40h]
00491677  lea     ecx, [ebp+70h+var_2518]
0049167D  push    ecx
0049167E  lea     edx, [ebp+70h+var_1170]
00491684  push    edx
00491685  mov     ecx, esi
00491687  call    eax
00491689  mov     ecx, eax
0049168B  call    sub_5B05A0
00491690  lea     ecx, [ebp+70h+var_1170]
00491696  mov     dword_75344C, eax
0049169B  call    sub_5B0440
004916A0  lea     ecx, [ebp+70h+var_2518]; void *
004916A6  mov     [ebp+70h+var_74], edi
004916A9  call    sub_401E90
004916AE  push    offset aImageSpotlight; "IMAGE_SPOTLIGHT"
004916B3  lea     ecx, [ebp+70h+var_2550]
004916B9  call    sub_4033F0
004916BE  mov     [ebp+70h+var_74], 114h
004916C5  mov     edx, [esi]
004916C7  mov     edx, [edx+40h]
004916CA  lea     eax, [ebp+70h+var_2550]
004916D0  push    eax
004916D1  lea     ecx, [ebp+70h+var_1740]
004916D7  push    ecx
004916D8  mov     ecx, esi
004916DA  call    edx
004916DC  mov     ecx, eax
004916DE  call    sub_5B05A0
004916E3  lea     ecx, [ebp+70h+var_1740]
004916E9  mov     dword_753644, eax
004916EE  call    sub_5B0440
004916F3  lea     ecx, [ebp+70h+var_2550]; void *
004916F9  mov     [ebp+70h+var_74], edi
004916FC  call    sub_401E90
00491701  push    offset aImageSpotlight_0; "IMAGE_SPOTLIGHT2"
00491706  lea     ecx, [ebp+70h+var_2588]
0049170C  call    sub_4033F0
00491711  mov     [ebp+70h+var_74], 115h
00491718  mov     eax, [esi]
0049171A  mov     eax, [eax+40h]
0049171D  lea     ecx, [ebp+70h+var_2588]
00491723  push    ecx
00491724  lea     edx, [ebp+70h+var_1188]
0049172A  push    edx
0049172B  mov     ecx, esi
0049172D  call    eax
0049172F  mov     ecx, eax
00491731  call    sub_5B05A0
00491736  lea     ecx, [ebp+70h+var_1188]
0049173C  mov     dword_75327C, eax
00491741  call    sub_5B0440
00491746  lea     ecx, [ebp+70h+var_2588]; void *
0049174C  mov     [ebp+70h+var_74], edi
0049174F  call    sub_401E90
00491754  push    offset aImageStarParti; "IMAGE_STAR_PARTICLES"
00491759  lea     ecx, [ebp+70h+var_25C0]
0049175F  call    sub_4033F0
00491764  lea     eax, [ebp+70h+var_25C0]
0049176A  mov     [ebp+70h+var_74], 116h
00491771  mov     edx, [esi]
00491773  mov     edx, [edx+40h]
00491776  push    eax
00491777  lea     ecx, [ebp+70h+var_1560]
0049177D  push    ecx
0049177E  mov     ecx, esi
00491780  call    edx
00491782  mov     ecx, eax
00491784  call    sub_5B05A0
00491789  lea     ecx, [ebp+70h+var_1560]
0049178F  mov     dword_753344, eax
00491794  call    sub_5B0440
00491799  lea     ecx, [ebp+70h+var_25C0]; void *
0049179F  mov     [ebp+70h+var_74], edi
004917A2  call    sub_401E90
004917A7  push    offset aImageStarSplat; "IMAGE_STAR_SPLATS"
004917AC  lea     ecx, [ebp+70h+var_25F8]
004917B2  call    sub_4033F0
004917B7  mov     [ebp+70h+var_74], 117h
004917BE  mov     eax, [esi]
004917C0  mov     eax, [eax+40h]
004917C3  lea     ecx, [ebp+70h+var_25F8]
004917C9  push    ecx
004917CA  lea     edx, [ebp+70h+var_11A0]
004917D0  push    edx
004917D1  mov     ecx, esi
004917D3  call    eax
004917D5  mov     ecx, eax
004917D7  call    sub_5B05A0
004917DC  lea     ecx, [ebp+70h+var_11A0]
004917E2  mov     dword_7531D0, eax
004917E7  call    sub_5B0440
004917EC  lea     ecx, [ebp+70h+var_25F8]; void *
004917F2  mov     [ebp+70h+var_74], edi
004917F5  call    sub_401E90
004917FA  push    offset aImageStoreSpee; "IMAGE_STORE_SPEECHBUBBLE"
004917FF  lea     ecx, [ebp+70h+var_2630]
00491805  call    sub_4033F0
0049180A  lea     eax, [ebp+70h+var_2630]
00491810  mov     [ebp+70h+var_74], 118h
00491817  mov     edx, [esi]
00491819  mov     edx, [edx+40h]
0049181C  push    eax
0049181D  lea     ecx, [ebp+70h+var_18F0]
00491823  push    ecx
00491824  mov     ecx, esi
00491826  call    edx
00491828  mov     ecx, eax
0049182A  call    sub_5B05A0
0049182F  lea     ecx, [ebp+70h+var_18F0]
00491835  mov     dword_752C0C, eax
0049183A  call    sub_5B0440
0049183F  lea     ecx, [ebp+70h+var_2630]; void *
00491845  mov     [ebp+70h+var_74], edi
00491848  call    sub_401E90
0049184D  push    offset aImageStoreSpee_0; "IMAGE_STORE_SPEECHBUBBLE2"
00491852  lea     ecx, [ebp+70h+var_2668]
00491858  call    sub_4033F0
0049185D  mov     [ebp+70h+var_74], 119h
00491864  mov     eax, [esi]
00491866  mov     eax, [eax+40h]
00491869  lea     ecx, [ebp+70h+var_2668]
0049186F  push    ecx
00491870  lea     edx, [ebp+70h+var_11B8]
00491876  push    edx
00491877  mov     ecx, esi
00491879  call    eax
0049187B  mov     ecx, eax
0049187D  call    sub_5B05A0
00491882  lea     ecx, [ebp+70h+var_11B8]
00491888  mov     dword_753650, eax
0049188D  call    sub_5B0440
00491892  lea     ecx, [ebp+70h+var_2668]; void *
00491898  mov     [ebp+70h+var_74], edi
0049189B  call    sub_401E90
004918A0  push    offset aImageSunbank; "IMAGE_SUNBANK"
004918A5  lea     ecx, [ebp+70h+var_26A0]
004918AB  call    sub_4033F0
004918B0  lea     eax, [ebp+70h+var_26A0]
004918B6  mov     [ebp+70h+var_74], 11Ah
004918BD  mov     edx, [esi]
004918BF  mov     edx, [edx+40h]
004918C2  push    eax
004918C3  lea     ecx, [ebp+70h+var_1578]
004918C9  push    ecx
004918CA  mov     ecx, esi
004918CC  call    edx
004918CE  mov     ecx, eax
004918D0  call    sub_5B05A0
004918D5  lea     ecx, [ebp+70h+var_1578]
004918DB  mov     dword_753398, eax
004918E0  call    sub_5B0440
004918E5  lea     ecx, [ebp+70h+var_26A0]; void *
004918EB  mov     [ebp+70h+var_74], edi
004918EE  call    sub_401E90
004918F3  push    offset aImageSunflower; "IMAGE_SUNFLOWER_TROPHY"
004918F8  lea     ecx, [ebp+70h+var_26D8]
004918FE  call    sub_4033F0
00491903  mov     [ebp+70h+var_74], 11Bh
0049190A  mov     eax, [esi]
0049190C  mov     eax, [eax+40h]
0049190F  lea     ecx, [ebp+70h+var_26D8]
00491915  push    ecx
00491916  lea     edx, [ebp+70h+var_11D0]
0049191C  push    edx
0049191D  mov     ecx, esi
0049191F  call    eax
00491921  mov     ecx, eax
00491923  call    sub_5B05A0
00491928  lea     ecx, [ebp+70h+var_11D0]
0049192E  mov     dword_752E98, eax
00491933  call    sub_5B0440
00491938  mov     [ebp+70h+var_74], edi
0049193B  lea     ecx, [ebp+70h+var_26D8]; void *
00491941  call    sub_401E90
00491946  push    offset aImageTaco; "IMAGE_TACO"
0049194B  lea     ecx, [ebp+70h+var_2710]
00491951  call    sub_4033F0
00491956  lea     eax, [ebp+70h+var_2710]
0049195C  mov     [ebp+70h+var_74], 11Ch
00491963  mov     edx, [esi]
00491965  mov     edx, [edx+40h]
00491968  push    eax
00491969  lea     ecx, [ebp+70h+var_1758]
0049196F  push    ecx
00491970  mov     ecx, esi
00491972  call    edx
00491974  mov     ecx, eax
00491976  call    sub_5B05A0
0049197B  lea     ecx, [ebp+70h+var_1758]
00491981  mov     dword_753010, eax
00491986  call    sub_5B0440
0049198B  lea     ecx, [ebp+70h+var_2710]; void *
00491991  mov     [ebp+70h+var_74], edi
00491994  call    sub_401E90
00491999  push    offset aImageTombstone; "IMAGE_TOMBSTONES"
0049199E  lea     ecx, [ebp+70h+var_2748]
004919A4  call    sub_4033F0
004919A9  mov     [ebp+70h+var_74], 11Dh
004919B0  mov     eax, [esi]
004919B2  mov     eax, [eax+40h]
004919B5  lea     ecx, [ebp+70h+var_2748]
004919BB  push    ecx
004919BC  lea     edx, [ebp+70h+var_11E8]
004919C2  push    edx
004919C3  mov     ecx, esi
004919C5  call    eax
004919C7  mov     ecx, eax
004919C9  call    sub_5B05A0
004919CE  lea     ecx, [ebp+70h+var_11E8]
004919D4  mov     dword_752B6C, eax
004919D9  call    sub_5B0440
004919DE  lea     ecx, [ebp+70h+var_2748]; void *
004919E4  mov     [ebp+70h+var_74], edi
004919E7  call    sub_401E90
004919EC  push    offset aImageTombstone_0; "IMAGE_TOMBSTONE_MOUNDS"
004919F1  lea     ecx, [ebp+70h+var_2780]
004919F7  call    sub_4033F0
004919FC  lea     eax, [ebp+70h+var_2780]
00491A02  mov     [ebp+70h+var_74], 11Eh
00491A09  mov     edx, [esi]
00491A0B  mov     edx, [edx+40h]
00491A0E  push    eax
00491A0F  lea     ecx, [ebp+70h+var_1590]
00491A15  push    ecx
00491A16  mov     ecx, esi
00491A18  call    edx
00491A1A  mov     ecx, eax
00491A1C  call    sub_5B05A0
00491A21  lea     ecx, [ebp+70h+var_1590]
00491A27  mov     dword_753124, eax
00491A2C  call    sub_5B0440
00491A31  lea     ecx, [ebp+70h+var_2780]; void *
00491A37  mov     [ebp+70h+var_74], edi
00491A3A  call    sub_401E90
00491A3F  push    offset aImageTreefood; "IMAGE_TREEFOOD"
00491A44  lea     ecx, [ebp+70h+var_27B8]
00491A4A  call    sub_4033F0
00491A4F  mov     [ebp+70h+var_74], 11Fh
00491A56  mov     eax, [esi]
00491A58  mov     eax, [eax+40h]
00491A5B  lea     ecx, [ebp+70h+var_27B8]
00491A61  push    ecx
00491A62  lea     edx, [ebp+70h+var_1200]
00491A68  push    edx
00491A69  mov     ecx, esi
00491A6B  call    eax
00491A6D  mov     ecx, eax
00491A6F  call    sub_5B05A0
00491A74  mov     dword_752D24, eax
00491A79  lea     ecx, [ebp+70h+var_1200]
00491A7F  call    sub_5B0440
00491A84  lea     ecx, [ebp+70h+var_27B8]; void *
00491A8A  mov     [ebp+70h+var_74], edi
00491A8D  call    sub_401E90
00491A92  push    offset aImageTrophy; "IMAGE_TROPHY"
00491A97  lea     ecx, [ebp+70h+var_27F0]
00491A9D  call    sub_4033F0
00491AA2  lea     eax, [ebp+70h+var_27F0]
00491AA8  mov     [ebp+70h+var_74], 120h
00491AAF  mov     edx, [esi]
00491AB1  mov     edx, [edx+40h]
00491AB4  push    eax
00491AB5  lea     ecx, [ebp+70h+var_1848]
00491ABB  push    ecx
00491ABC  mov     ecx, esi
00491ABE  call    edx
00491AC0  mov     ecx, eax
00491AC2  call    sub_5B05A0
00491AC7  lea     ecx, [ebp+70h+var_1848]
00491ACD  mov     dword_752ABC, eax
00491AD2  call    sub_5B0440
00491AD7  lea     ecx, [ebp+70h+var_27F0]; void *
00491ADD  mov     [ebp+70h+var_74], edi
00491AE0  call    sub_401E90
00491AE5  push    offset aImageTrophyHiR; "IMAGE_TROPHY_HI_RES"
00491AEA  lea     ecx, [ebp+70h+var_2828]
00491AF0  call    sub_4033F0
00491AF5  mov     [ebp+70h+var_74], 121h
00491AFC  mov     eax, [esi]
00491AFE  mov     eax, [eax+40h]
00491B01  lea     ecx, [ebp+70h+var_2828]
00491B07  push    ecx
00491B08  lea     edx, [ebp+70h+var_1218]
00491B0E  push    edx
00491B0F  mov     ecx, esi
00491B11  call    eax
00491B13  mov     ecx, eax
00491B15  call    sub_5B05A0
00491B1A  lea     ecx, [ebp+70h+var_1218]
00491B20  mov     dword_752A8C, eax
00491B25  call    sub_5B0440
00491B2A  lea     ecx, [ebp+70h+var_2828]; void *
00491B30  mov     [ebp+70h+var_74], edi
00491B33  call    sub_401E90
00491B38  push    offset aImageVaseChunk; "IMAGE_VASE_CHUNKS"
00491B3D  lea     ecx, [ebp+70h+var_2860]
00491B43  call    sub_4033F0
00491B48  lea     eax, [ebp+70h+var_2860]
00491B4E  mov     [ebp+70h+var_74], 122h
00491B55  mov     edx, [esi]
00491B57  mov     edx, [edx+40h]
00491B5A  push    eax
00491B5B  lea     ecx, [ebp+70h+var_15A8]
00491B61  push    ecx
00491B62  mov     ecx, esi
00491B64  call    edx
00491B66  mov     ecx, eax
00491B68  call    sub_5B05A0
00491B6D  lea     ecx, [ebp+70h+var_15A8]
00491B73  mov     dword_7536C8, eax
00491B78  call    sub_5B0440
00491B7D  lea     ecx, [ebp+70h+var_2860]; void *
00491B83  mov     [ebp+70h+var_74], edi
00491B86  call    sub_401E90
00491B8B  push    offset aImageWallnutpa; "IMAGE_WALLNUTPARTICLESLARGE"
00491B90  lea     ecx, [ebp+70h+var_2898]
00491B96  call    sub_4033F0
00491B9B  mov     [ebp+70h+var_74], 123h
00491BA2  mov     eax, [esi]
00491BA4  mov     eax, [eax+40h]
00491BA7  lea     ecx, [ebp+70h+var_2898]
00491BAD  push    ecx
00491BAE  lea     edx, [ebp+70h+var_1230]
00491BB4  push    edx
00491BB5  mov     ecx, esi
00491BB7  call    eax
00491BB9  mov     ecx, eax
00491BBB  call    sub_5B05A0
00491BC0  lea     ecx, [ebp+70h+var_1230]
00491BC6  mov     dword_752D98, eax
00491BCB  call    sub_5B0440
00491BD0  lea     ecx, [ebp+70h+var_2898]; void *
00491BD6  mov     [ebp+70h+var_74], edi
00491BD9  call    sub_401E90
00491BDE  push    offset aImageWallnutpa_0; "IMAGE_WALLNUTPARTICLESSMALL"
00491BE3  lea     ecx, [ebp+70h+var_28D0]
00491BE9  call    sub_4033F0
00491BEE  lea     eax, [ebp+70h+var_28D0]
00491BF4  mov     [ebp+70h+var_74], 124h
00491BFB  mov     edx, [esi]
00491BFD  mov     edx, [edx+40h]
00491C00  push    eax
00491C01  lea     ecx, [ebp+70h+var_B40]
00491C07  push    ecx
00491C08  mov     ecx, esi
00491C0A  call    edx
00491C0C  mov     ecx, eax
00491C0E  call    sub_5B05A0
00491C13  lea     ecx, [ebp+70h+var_B40]
00491C19  mov     dword_7532D8, eax
00491C1E  call    sub_5B0440
00491C23  lea     ecx, [ebp+70h+var_28D0]; void *
00491C29  mov     [ebp+70h+var_74], edi
00491C2C  call    sub_401E90
00491C31  push    offset aImageWallnutBo; "IMAGE_WALLNUT_BOWLINGSTRIPE"
00491C36  lea     ecx, [ebp+70h+var_2908]
00491C3C  call    sub_4033F0
00491C41  mov     [ebp+70h+var_74], 125h
00491C48  mov     eax, [esi]
00491C4A  mov     eax, [eax+40h]
00491C4D  lea     ecx, [ebp+70h+var_2908]
00491C53  push    ecx
00491C54  lea     edx, [ebp+70h+var_B4C]
00491C5A  push    edx
00491C5B  mov     ecx, esi
00491C5D  call    eax
00491C5F  mov     ecx, eax
00491C61  call    sub_5B05A0
00491C66  lea     ecx, [ebp+70h+var_B4C]
00491C6C  mov     dword_752E0C, eax
00491C71  call    sub_5B0440
00491C76  lea     ecx, [ebp+70h+var_2908]; void *
00491C7C  mov     [ebp+70h+var_74], edi
00491C7F  call    sub_401E90
00491C84  push    offset aImageWaterdrop; "IMAGE_WATERDROP"
00491C89  lea     ecx, [ebp+70h+var_2940]
00491C8F  call    sub_4033F0
00491C94  lea     eax, [ebp+70h+var_2940]
00491C9A  mov     [ebp+70h+var_74], 126h
00491CA1  mov     edx, [esi]
00491CA3  mov     edx, [edx+40h]
00491CA6  push    eax
00491CA7  lea     ecx, [ebp+70h+var_B64]
00491CAD  push    ecx
00491CAE  mov     ecx, esi
00491CB0  call    edx
00491CB2  mov     ecx, eax
00491CB4  call    sub_5B05A0
00491CB9  lea     ecx, [ebp+70h+var_B64]
00491CBF  mov     dword_7534FC, eax
00491CC4  call    sub_5B0440
00491CC9  lea     ecx, [ebp+70h+var_2940]; void *
00491CCF  mov     [ebp+70h+var_74], edi
00491CD2  call    sub_401E90
00491CD7  push    offset aImageWateringc; "IMAGE_WATERINGCAN"
00491CDC  lea     ecx, [ebp+70h+var_2978]
00491CE2  call    sub_4033F0
00491CE7  lea     ecx, [ebp+70h+var_2978]
00491CED  push    ecx
00491CEE  lea     edx, [ebp+70h+var_B7C]
00491CF4  mov     [ebp+70h+var_74], 127h
00491CFB  mov     eax, [esi]
00491CFD  push    edx
00491CFE  mov     ecx, esi
00491D00  mov     eax, [eax+40h]
00491D03  call    eax
00491D05  mov     ecx, eax
00491D07  call    sub_5B05A0
00491D0C  lea     ecx, [ebp+70h+var_B7C]
00491D12  mov     dword_752C24, eax
00491D17  call    sub_5B0440
00491D1C  lea     ecx, [ebp+70h+var_2978]; void *
00491D22  mov     [ebp+70h+var_74], edi
00491D25  call    sub_401E90
00491D2A  push    offset aImageWateringc_0; "IMAGE_WATERINGCANGOLD"
00491D2F  lea     ecx, [ebp+70h+var_29B0]
00491D35  call    sub_4033F0
00491D3A  lea     eax, [ebp+70h+var_29B0]
00491D40  mov     [ebp+70h+var_74], 128h
00491D47  mov     edx, [esi]
00491D49  mov     edx, [edx+40h]
00491D4C  push    eax
00491D4D  lea     ecx, [ebp+70h+var_B94]
00491D53  push    ecx
00491D54  mov     ecx, esi
00491D56  call    edx
00491D58  mov     ecx, eax
00491D5A  call    sub_5B05A0
00491D5F  lea     ecx, [ebp+70h+var_B94]
00491D65  mov     dword_752F28, eax
00491D6A  call    sub_5B0440
00491D6F  lea     ecx, [ebp+70h+var_29B0]; void *
00491D75  mov     [ebp+70h+var_74], edi
00491D78  call    sub_401E90
00491D7D  push    offset aImageWaterpart; "IMAGE_WATERPARTICLE"
00491D82  lea     ecx, [ebp+70h+var_29E8]
00491D88  call    sub_4033F0
00491D8D  mov     [ebp+70h+var_74], 129h
00491D94  mov     eax, [esi]
00491D96  mov     eax, [eax+40h]
00491D99  lea     ecx, [ebp+70h+var_29E8]
00491D9F  push    ecx
00491DA0  lea     edx, [ebp+70h+var_BAC]
00491DA6  push    edx
00491DA7  mov     ecx, esi
00491DA9  call    eax
00491DAB  mov     ecx, eax
00491DAD  call    sub_5B05A0
00491DB2  lea     ecx, [ebp+70h+var_BAC]
00491DB8  mov     dword_753614, eax
00491DBD  call    sub_5B0440
00491DC2  lea     ecx, [ebp+70h+var_29E8]; void *
00491DC8  mov     [ebp+70h+var_74], edi
00491DCB  call    sub_401E90
00491DD0  push    offset aImageWhitepixe; "IMAGE_WHITEPIXEL"
00491DD5  lea     ecx, [ebp+70h+var_2A20]
00491DDB  call    sub_4033F0
00491DE0  lea     eax, [ebp+70h+var_2A20]
00491DE6  mov     [ebp+70h+var_74], 12Ah
00491DED  mov     edx, [esi]
00491DEF  mov     edx, [edx+40h]
00491DF2  push    eax
00491DF3  lea     ecx, [ebp+70h+var_BC4]
00491DF9  push    ecx
00491DFA  mov     ecx, esi
00491DFC  call    edx
00491DFE  mov     ecx, eax
00491E00  call    sub_5B05A0
00491E05  lea     ecx, [ebp+70h+var_BC4]
00491E0B  mov     dword_753438, eax
00491E10  call    sub_5B0440
00491E15  lea     ecx, [ebp+70h+var_2A20]; void *
00491E1B  mov     [ebp+70h+var_74], edi
00491E1E  call    sub_401E90
00491E23  push    offset aImageWhitewate; "IMAGE_WHITEWATER"
00491E28  lea     ecx, [ebp+70h+var_2A58]
00491E2E  call    sub_4033F0
00491E33  lea     ecx, [ebp+70h+var_2A58]
00491E39  mov     [ebp+70h+var_74], 12Bh
00491E40  mov     eax, [esi]
00491E42  push    ecx
00491E43  mov     eax, [eax+40h]
00491E46  lea     edx, [ebp+70h+var_BDC]
00491E4C  push    edx
00491E4D  mov     ecx, esi
00491E4F  call    eax
00491E51  mov     ecx, eax
00491E53  call    sub_5B05A0
00491E58  lea     ecx, [ebp+70h+var_BDC]
00491E5E  mov     dword_75355C, eax
00491E63  call    sub_5B0440
00491E68  lea     ecx, [ebp+70h+var_2A58]; void *
00491E6E  mov     [ebp+70h+var_74], edi
00491E71  call    sub_401E90
00491E76  push    offset aImageWhitewate_0; "IMAGE_WHITEWATER_SHADOW"
00491E7B  lea     ecx, [ebp+70h+var_2A90]
00491E81  call    sub_4033F0
00491E86  lea     eax, [ebp+70h+var_2A90]
00491E8C  mov     [ebp+70h+var_74], 12Ch
00491E93  mov     edx, [esi]
00491E95  mov     edx, [edx+40h]
00491E98  push    eax
00491E99  lea     ecx, [ebp+70h+var_BF4]
00491E9F  push    ecx
00491EA0  mov     ecx, esi
00491EA2  call    edx
00491EA4  mov     ecx, eax
00491EA6  call    sub_5B05A0
00491EAB  lea     ecx, [ebp+70h+var_BF4]
00491EB1  mov     dword_752EAC, eax
00491EB6  call    sub_5B0440
00491EBB  lea     ecx, [ebp+70h+var_2A90]; void *
00491EC1  mov     [ebp+70h+var_74], edi
00491EC4  call    sub_401E90
00491EC9  push    offset aImageWintermel; "IMAGE_WINTERMELON_PARTICLES"
00491ECE  lea     ecx, [ebp+70h+var_2AC8]
00491ED4  call    sub_4033F0
00491ED9  mov     [ebp+70h+var_74], 12Dh
00491EE0  mov     eax, [esi]
00491EE2  mov     eax, [eax+40h]
00491EE5  lea     ecx, [ebp+70h+var_2AC8]
00491EEB  push    ecx
00491EEC  lea     edx, [ebp+70h+var_C0C]
00491EF2  push    edx
00491EF3  mov     ecx, esi
00491EF5  call    eax
00491EF7  mov     ecx, eax
00491EF9  call    sub_5B05A0
00491EFE  lea     ecx, [ebp+70h+var_C0C]
00491F04  mov     dword_753498, eax
00491F09  call    sub_5B0440
00491F0E  lea     ecx, [ebp+70h+var_2AC8]; void *
00491F14  mov     [ebp+70h+var_74], edi
00491F17  call    sub_401E90
00491F1C  push    offset aImageZambonism; "IMAGE_ZAMBONISMOKE"
00491F21  lea     ecx, [ebp+70h+var_2B00]
00491F27  call    sub_4033F0
00491F2C  lea     eax, [ebp+70h+var_2B00]
00491F32  mov     [ebp+70h+var_74], 12Eh
00491F39  mov     edx, [esi]
00491F3B  mov     edx, [edx+40h]
00491F3E  push    eax
00491F3F  lea     ecx, [ebp+70h+var_C24]
00491F45  push    ecx
00491F46  mov     ecx, esi
00491F48  call    edx
00491F4A  mov     ecx, eax
00491F4C  call    sub_5B05A0
00491F51  lea     ecx, [ebp+70h+var_C24]
00491F57  mov     dword_752E38, eax
00491F5C  call    sub_5B0440
00491F61  lea     ecx, [ebp+70h+var_2B00]; void *
00491F67  mov     [ebp+70h+var_74], edi
00491F6A  call    sub_401E90
00491F6F  push    offset aImageZenshopbu; "IMAGE_ZENSHOPBUTTON"
00491F74  lea     ecx, [ebp+70h+var_2B38]
00491F7A  call    sub_4033F0
00491F7F  mov     [ebp+70h+var_74], 12Fh
00491F86  mov     eax, [esi]
00491F88  mov     eax, [eax+40h]
00491F8B  lea     ecx, [ebp+70h+var_2B38]
00491F91  push    ecx
00491F92  lea     edx, [ebp+70h+var_C3C]
00491F98  push    edx
00491F99  mov     ecx, esi
00491F9B  call    eax
00491F9D  mov     ecx, eax
00491F9F  call    sub_5B05A0
00491FA4  lea     ecx, [ebp+70h+var_C3C]
00491FAA  mov     dword_75376C, eax
00491FAF  call    sub_5B0440
00491FB4  lea     ecx, [ebp+70h+var_2B38]; void *
00491FBA  mov     [ebp+70h+var_74], edi
00491FBD  call    sub_401E90
00491FC2  push    offset aImageZenshopbu_0; "IMAGE_ZENSHOPBUTTON_HIGHLIGHT"
00491FC7  lea     ecx, [ebp+70h+var_2B70]
00491FCD  call    sub_4033F0
00491FD2  lea     eax, [ebp+70h+var_2B70]
00491FD8  mov     [ebp+70h+var_74], 130h
00491FDF  mov     edx, [esi]
00491FE1  mov     edx, [edx+40h]
00491FE4  push    eax
00491FE5  lea     ecx, [ebp+70h+var_C54]
00491FEB  push    ecx
00491FEC  mov     ecx, esi
00491FEE  call    edx
00491FF0  mov     ecx, eax
00491FF2  call    sub_5B05A0
00491FF7  lea     ecx, [ebp+70h+var_C54]
00491FFD  mov     dword_7536E0, eax
00492002  call    sub_5B0440
00492007  lea     ecx, [ebp+70h+var_2B70]; void *
0049200D  mov     [ebp+70h+var_74], edi
00492010  call    sub_401E90
00492015  push    offset aImageZenGarden; "IMAGE_ZEN_GARDENGLOVE"
0049201A  lea     ecx, [ebp+70h+var_2BA8]
00492020  call    sub_4033F0
00492025  mov     [ebp+70h+var_74], 131h
0049202C  mov     eax, [esi]
0049202E  mov     eax, [eax+40h]
00492031  lea     ecx, [ebp+70h+var_2BA8]
00492037  push    ecx
00492038  lea     edx, [ebp+70h+var_C6C]
0049203E  push    edx
0049203F  mov     ecx, esi
00492041  call    eax
00492043  mov     ecx, eax
00492045  call    sub_5B05A0
0049204A  lea     ecx, [ebp+70h+var_C6C]
00492050  mov     dword_753090, eax
00492055  call    sub_5B0440
0049205A  lea     ecx, [ebp+70h+var_2BA8]; void *
00492060  mov     [ebp+70h+var_74], edi
00492063  call    sub_401E90
00492068  push    offset aImageZenGoldto; "IMAGE_ZEN_GOLDTOOLRETICLE"
0049206D  lea     ecx, [ebp+70h+var_2BE0]
00492073  call    sub_4033F0
00492078  lea     eax, [ebp+70h+var_2BE0]
0049207E  mov     [ebp+70h+var_74], 132h
00492085  mov     edx, [esi]
00492087  mov     edx, [edx+40h]
0049208A  push    eax
0049208B  lea     ecx, [ebp+70h+var_C84]
00492091  push    ecx
00492092  mov     ecx, esi
00492094  call    edx
00492096  mov     ecx, eax
00492098  call    sub_5B05A0
0049209D  lea     ecx, [ebp+70h+var_C84]
004920A3  mov     dword_752C08, eax
004920A8  call    sub_5B0440
004920AD  lea     ecx, [ebp+70h+var_2BE0]; void *
004920B3  mov     [ebp+70h+var_74], edi
004920B6  call    sub_401E90
004920BB  push    offset aImageZenMoneys; "IMAGE_ZEN_MONEYSIGN"
004920C0  lea     ecx, [ebp+70h+var_2C18]
004920C6  call    sub_4033F0
004920CB  mov     [ebp+70h+var_74], 133h
004920D2  mov     eax, [esi]
004920D4  mov     eax, [eax+40h]
004920D7  lea     ecx, [ebp+70h+var_2C18]
004920DD  push    ecx
004920DE  lea     edx, [ebp+70h+var_C9C]
004920E4  push    edx
004920E5  mov     ecx, esi
004920E7  call    eax
004920E9  mov     ecx, eax
004920EB  call    sub_5B05A0
004920F0  lea     ecx, [ebp+70h+var_C9C]
004920F6  mov     dword_753828, eax
004920FB  call    sub_5B0440
00492100  lea     ecx, [ebp+70h+var_2C18]; void *
00492106  mov     [ebp+70h+var_74], edi
00492109  call    sub_401E90
0049210E  push    offset aImageZenNeedIc; "IMAGE_ZEN_NEED_ICONS"
00492113  lea     ecx, [ebp+70h+var_2C50]
00492119  call    sub_4033F0
0049211E  lea     eax, [ebp+70h+var_2C50]
00492124  mov     [ebp+70h+var_74], 134h
0049212B  mov     edx, [esi]
0049212D  mov     edx, [edx+40h]
00492130  push    eax
00492131  lea     ecx, [ebp+70h+var_CB4]
00492137  push    ecx
00492138  mov     ecx, esi
0049213A  call    edx
0049213C  mov     ecx, eax
0049213E  call    sub_5B05A0
00492143  lea     ecx, [ebp+70h+var_CB4]
00492149  mov     dword_752FAC, eax
0049214E  call    sub_5B0440
00492153  lea     ecx, [ebp+70h+var_2C50]; void *
00492159  mov     [ebp+70h+var_74], edi
0049215C  call    sub_401E90
00492161  push    offset aImageZenNextga; "IMAGE_ZEN_NEXTGARDEN"
00492166  lea     ecx, [ebp+70h+var_2C88]
0049216C  call    sub_4033F0
00492171  mov     [ebp+70h+var_74], 135h
00492178  mov     eax, [esi]
0049217A  mov     eax, [eax+40h]
0049217D  lea     ecx, [ebp+70h+var_2C88]
00492183  push    ecx
00492184  lea     edx, [ebp+70h+var_CCC]
0049218A  push    edx
0049218B  mov     ecx, esi
0049218D  call    eax
0049218F  mov     ecx, eax
00492191  call    sub_5B05A0
00492196  lea     ecx, [ebp+70h+var_CCC]
0049219C  mov     dword_753064, eax
004921A1  call    sub_5B0440
004921A6  lea     ecx, [ebp+70h+var_2C88]; void *
004921AC  mov     [ebp+70h+var_74], edi
004921AF  call    sub_401E90
004921B4  push    offset aImageZenWheelb; "IMAGE_ZEN_WHEELBARROW"
004921B9  lea     ecx, [ebp+70h+var_2CC0]
004921BF  call    sub_4033F0
004921C4  lea     eax, [ebp+70h+var_2CC0]
004921CA  mov     [ebp+70h+var_74], 136h
004921D1  mov     edx, [esi]
004921D3  mov     edx, [edx+40h]
004921D6  push    eax
004921D7  lea     ecx, [ebp+70h+var_CE4]
004921DD  push    ecx
004921DE  mov     ecx, esi
004921E0  call    edx
004921E2  mov     ecx, eax
004921E4  call    sub_5B05A0
004921E9  lea     ecx, [ebp+70h+var_CE4]
004921EF  mov     dword_752FA8, eax
004921F4  call    sub_5B0440
004921F9  mov     [ebp+70h+var_74], edi
004921FC  lea     ecx, [ebp+70h+var_2CC0]; void *
00492202  call    sub_401E90
00492207  push    offset aImageZombiebac; "IMAGE_ZOMBIEBACKUPDANCERHEAD"
0049220C  lea     ecx, [ebp+70h+var_2CF8]
00492212  call    sub_4033F0
00492217  mov     [ebp+70h+var_74], 137h
0049221E  mov     eax, [esi]
00492220  mov     eax, [eax+40h]
00492223  lea     ecx, [ebp+70h+var_2CF8]
00492229  push    ecx
0049222A  lea     edx, [ebp+70h+var_CFC]
00492230  push    edx
00492231  mov     ecx, esi
00492233  call    eax
00492235  mov     ecx, eax
00492237  call    sub_5B05A0
0049223C  lea     ecx, [ebp+70h+var_CFC]
00492242  mov     dword_753394, eax
00492247  call    sub_5B0440
0049224C  lea     ecx, [ebp+70h+var_2CF8]; void *
00492252  mov     [ebp+70h+var_74], edi
00492255  call    sub_401E90
0049225A  push    offset aImageZombiebal; "IMAGE_ZOMBIEBALLOONHEAD"
0049225F  lea     ecx, [ebp+70h+var_2D30]
00492265  call    sub_4033F0
0049226A  lea     eax, [ebp+70h+var_2D30]
00492270  mov     [ebp+70h+var_74], 138h
00492277  mov     edx, [esi]
00492279  mov     edx, [edx+40h]
0049227C  push    eax
0049227D  lea     ecx, [ebp+70h+var_D14]
00492283  push    ecx
00492284  mov     ecx, esi
00492286  call    edx
00492288  mov     ecx, eax
0049228A  call    sub_5B05A0
0049228F  lea     ecx, [ebp+70h+var_D14]
00492295  mov     dword_752BA0, eax
0049229A  call    sub_5B0440
0049229F  lea     ecx, [ebp+70h+var_2D30]; void *
004922A5  mov     [ebp+70h+var_74], edi
004922A8  call    sub_401E90
004922AD  push    offset aImageZombiebob; "IMAGE_ZOMBIEBOBSLEDHEAD"
004922B2  lea     ecx, [ebp+70h+var_2D68]
004922B8  call    sub_4033F0
004922BD  mov     [ebp+70h+var_74], 139h
004922C4  mov     eax, [esi]
004922C6  mov     eax, [eax+40h]
004922C9  lea     ecx, [ebp+70h+var_2D68]
004922CF  push    ecx
004922D0  lea     edx, [ebp+70h+var_D2C]
004922D6  push    edx
004922D7  mov     ecx, esi
004922D9  call    eax
004922DB  mov     ecx, eax
004922DD  call    sub_5B05A0
004922E2  lea     ecx, [ebp+70h+var_D2C]
004922E8  mov     dword_753334, eax
004922ED  call    sub_5B0440
004922F2  lea     ecx, [ebp+70h+var_2D68]; void *
004922F8  mov     [ebp+70h+var_74], edi
004922FB  call    sub_401E90
00492300  push    offset aImageZombiedan; "IMAGE_ZOMBIEDANCERHEAD"
00492305  lea     ecx, [ebp+70h+var_2DA0]
0049230B  call    sub_4033F0
00492310  lea     eax, [ebp+70h+var_2DA0]
00492316  mov     [ebp+70h+var_74], 13Ah
0049231D  mov     edx, [esi]
0049231F  mov     edx, [edx+40h]
00492322  push    eax
00492323  lea     ecx, [ebp+70h+var_D44]
00492329  push    ecx
0049232A  mov     ecx, esi
0049232C  call    edx
0049232E  mov     ecx, eax
00492330  call    sub_5B05A0
00492335  mov     dword_7537EC, eax
0049233A  lea     ecx, [ebp+70h+var_D44]
00492340  call    sub_5B0440
00492345  lea     ecx, [ebp+70h+var_2DA0]; void *
0049234B  mov     [ebp+70h+var_74], edi
0049234E  call    sub_401E90
00492353  push    offset aImageZombiedig; "IMAGE_ZOMBIEDIGGERARM"
00492358  lea     ecx, [ebp+70h+var_2DD8]
0049235E  call    sub_4033F0
00492363  mov     [ebp+70h+var_74], 13Bh
0049236A  mov     eax, [esi]
0049236C  mov     eax, [eax+40h]
0049236F  lea     ecx, [ebp+70h+var_2DD8]
00492375  push    ecx
00492376  lea     edx, [ebp+70h+var_D5C]
0049237C  push    edx
0049237D  mov     ecx, esi
0049237F  call    eax
00492381  mov     ecx, eax
00492383  call    sub_5B05A0
00492388  lea     ecx, [ebp+70h+var_D5C]
0049238E  mov     dword_75350C, eax
00492393  call    sub_5B0440
00492398  lea     ecx, [ebp+70h+var_2DD8]; void *
0049239E  mov     [ebp+70h+var_74], edi
004923A1  call    sub_401E90
004923A6  push    offset aImageZombiedig_0; "IMAGE_ZOMBIEDIGGERHEAD"
004923AB  lea     ecx, [ebp+70h+var_2E10]
004923B1  call    sub_4033F0
004923B6  lea     eax, [ebp+70h+var_2E10]
004923BC  mov     [ebp+70h+var_74], 13Ch
004923C3  mov     edx, [esi]
004923C5  mov     edx, [edx+40h]
004923C8  push    eax
004923C9  lea     ecx, [ebp+70h+var_D74]
004923CF  push    ecx
004923D0  mov     ecx, esi
004923D2  call    edx
004923D4  mov     ecx, eax
004923D6  call    sub_5B05A0
004923DB  lea     ecx, [ebp+70h+var_D74]
004923E1  mov     dword_7537A4, eax
004923E6  call    sub_5B0440
004923EB  lea     ecx, [ebp+70h+var_2E10]; void *
004923F1  mov     [ebp+70h+var_74], edi
004923F4  call    sub_401E90
004923F9  push    offset aImageZombiedol; "IMAGE_ZOMBIEDOLPHINRIDERHEAD"
004923FE  lea     ecx, [ebp+70h+var_2E48]
00492404  call    sub_4033F0
00492409  mov     [ebp+70h+var_74], 13Dh
00492410  mov     eax, [esi]
00492412  mov     eax, [eax+40h]
00492415  lea     ecx, [ebp+70h+var_2E48]
0049241B  push    ecx
0049241C  lea     edx, [ebp+70h+var_D8C]
00492422  push    edx
00492423  mov     ecx, esi
00492425  call    eax
00492427  mov     ecx, eax
00492429  call    sub_5B05A0
0049242E  lea     ecx, [ebp+70h+var_D8C]
00492434  mov     dword_752CA8, eax
00492439  call    sub_5B0440
0049243E  lea     ecx, [ebp+70h+var_2E48]; void *
00492444  mov     [ebp+70h+var_74], edi
00492447  call    sub_401E90
0049244C  push    offset aImageZombiefoo; "IMAGE_ZOMBIEFOOTBALLHEAD"
00492451  lea     ecx, [ebp+70h+var_2E80]
00492457  call    sub_4033F0
0049245C  lea     eax, [ebp+70h+var_2E80]
00492462  mov     [ebp+70h+var_74], 13Eh
00492469  mov     edx, [esi]
0049246B  mov     edx, [edx+40h]
0049246E  push    eax
0049246F  lea     ecx, [ebp+70h+var_DA4]
00492475  push    ecx
00492476  mov     ecx, esi
00492478  call    edx
0049247A  mov     ecx, eax
0049247C  call    sub_5B05A0
00492481  lea     ecx, [ebp+70h+var_DA4]
00492487  mov     dword_752F24, eax
0049248C  call    sub_5B0440
00492491  lea     ecx, [ebp+70h+var_2E80]; void *
00492497  mov     [ebp+70h+var_74], edi
0049249A  call    sub_401E90
0049249F  push    offset aImageZombiefut; "IMAGE_ZOMBIEFUTUREGLASSES"
004924A4  lea     ecx, [ebp+70h+var_2EB8]
004924AA  call    sub_4033F0
004924AF  mov     [ebp+70h+var_74], 13Fh
004924B6  mov     eax, [esi]
004924B8  mov     eax, [eax+40h]
004924BB  lea     ecx, [ebp+70h+var_2EB8]
004924C1  push    ecx
004924C2  lea     edx, [ebp+70h+var_DBC]
004924C8  push    edx
004924C9  mov     ecx, esi
004924CB  call    eax
004924CD  mov     ecx, eax
004924CF  call    sub_5B05A0
004924D4  lea     ecx, [ebp+70h+var_DBC]
004924DA  mov     dword_75347C, eax
004924DF  call    sub_5B0440
004924E4  lea     ecx, [ebp+70h+var_2EB8]; void *
004924EA  mov     [ebp+70h+var_74], edi
004924ED  call    sub_401E90
004924F2  push    offset aImageZombieimp; "IMAGE_ZOMBIEIMPHEAD"
004924F7  lea     ecx, [ebp+70h+var_2EF0]
004924FD  call    sub_4033F0
00492502  lea     eax, [ebp+70h+var_2EF0]
00492508  mov     [ebp+70h+var_74], 140h
0049250F  mov     edx, [esi]
00492511  mov     edx, [edx+40h]
00492514  push    eax
00492515  lea     ecx, [ebp+70h+var_DD4]
0049251B  push    ecx
0049251C  mov     ecx, esi
0049251E  call    edx
00492520  mov     ecx, eax
00492522  call    sub_5B05A0
00492527  lea     ecx, [ebp+70h+var_DD4]
0049252D  mov     dword_752B9C, eax
00492532  call    sub_5B0440
00492537  lea     ecx, [ebp+70h+var_2EF0]; void *
0049253D  mov     [ebp+70h+var_74], edi
00492540  call    sub_401E90
00492545  push    offset aImageZombiejac; "IMAGE_ZOMBIEJACKBOXARM"
0049254A  lea     ecx, [ebp+70h+var_2F28]
00492550  call    sub_4033F0
00492555  mov     [ebp+70h+var_74], 141h
0049255C  mov     eax, [esi]
0049255E  mov     eax, [eax+40h]
00492561  lea     ecx, [ebp+70h+var_2F28]
00492567  push    ecx
00492568  lea     edx, [ebp+70h+var_DEC]
0049256E  push    edx
0049256F  mov     ecx, esi
00492571  call    eax
00492573  mov     ecx, eax
00492575  call    sub_5B05A0
0049257A  lea     ecx, [ebp+70h+var_DEC]
00492580  mov     dword_7534CC, eax
00492585  call    sub_5B0440
0049258A  lea     ecx, [ebp+70h+var_2F28]; void *
00492590  mov     [ebp+70h+var_74], edi
00492593  call    sub_401E90
00492598  push    offset aImageZombielad; "IMAGE_ZOMBIELADDERHEAD"
0049259D  lea     ecx, [ebp+70h+var_2F60]
004925A3  call    sub_4033F0
004925A8  lea     eax, [ebp+70h+var_2F60]
004925AE  lea     ecx, [ebp+70h+var_E04]
004925B4  push    eax
004925B5  mov     [ebp+70h+var_74], 142h
004925BC  mov     edx, [esi]
004925BE  push    ecx
004925BF  mov     ecx, esi
004925C1  mov     edx, [edx+40h]
004925C4  call    edx
004925C6  mov     ecx, eax
004925C8  call    sub_5B05A0
004925CD  lea     ecx, [ebp+70h+var_E04]
004925D3  mov     dword_7533BC, eax
004925D8  call    sub_5B0440
004925DD  lea     ecx, [ebp+70h+var_2F60]; void *
004925E3  mov     [ebp+70h+var_74], edi
004925E6  call    sub_401E90
004925EB  push    offset aImageZombiepog; "IMAGE_ZOMBIEPOGO"
004925F0  lea     ecx, [ebp+70h+var_2F98]
004925F6  call    sub_4033F0
004925FB  mov     [ebp+70h+var_74], 143h
00492602  mov     eax, [esi]
00492604  mov     eax, [eax+40h]
00492607  lea     ecx, [ebp+70h+var_2F98]
0049260D  push    ecx
0049260E  lea     edx, [ebp+70h+var_E1C]
00492614  push    edx
00492615  mov     ecx, esi
00492617  call    eax
00492619  mov     ecx, eax
0049261B  call    sub_5B05A0
00492620  lea     ecx, [ebp+70h+var_E1C]
00492626  mov     dword_7532B4, eax
0049262B  call    sub_5B0440
00492630  lea     ecx, [ebp+70h+var_2F98]; void *
00492636  mov     [ebp+70h+var_74], edi
00492639  call    sub_401E90
0049263E  push    offset aImageZombiepol; "IMAGE_ZOMBIEPOLEVAULTERHEAD"
00492643  lea     ecx, [ebp+70h+var_2FD0]
00492649  call    sub_4033F0
0049264E  lea     eax, [ebp+70h+var_2FD0]
00492654  mov     [ebp+70h+var_74], 144h
0049265B  mov     edx, [esi]
0049265D  mov     edx, [edx+40h]
00492660  push    eax
00492661  lea     ecx, [ebp+70h+var_E34]
00492667  push    ecx
00492668  mov     ecx, esi
0049266A  call    edx
0049266C  mov     ecx, eax
0049266E  call    sub_5B05A0
00492673  lea     ecx, [ebp+70h+var_E34]
00492679  mov     dword_753060, eax
0049267E  call    sub_5B0440
00492683  lea     ecx, [ebp+70h+var_2FD0]; void *
00492689  mov     [ebp+70h+var_74], edi
0049268C  call    sub_401E90
00492691  push    offset aImageZombieyet; "IMAGE_ZOMBIEYETIHEAD"
00492696  lea     ecx, [ebp+70h+var_3008]
0049269C  call    sub_4033F0
004926A1  mov     [ebp+70h+var_74], 145h
004926A8  mov     eax, [esi]
004926AA  mov     eax, [eax+40h]
004926AD  lea     ecx, [ebp+70h+var_3008]
004926B3  push    ecx
004926B4  lea     edx, [ebp+70h+var_E4C]
004926BA  push    edx
004926BB  mov     ecx, esi
004926BD  call    eax
004926BF  mov     ecx, eax
004926C1  call    sub_5B05A0
004926C6  lea     ecx, [ebp+70h+var_E4C]
004926CC  mov     dword_752D54, eax
004926D1  call    sub_5B0440
004926D6  lea     ecx, [ebp+70h+var_3008]; void *
004926DC  mov     [ebp+70h+var_74], edi
004926DF  call    sub_401E90
004926E4  push    offset aImageZombieBob; "IMAGE_ZOMBIE_BOBSLED1"
004926E9  lea     ecx, [ebp+70h+var_3040]
004926EF  call    sub_4033F0
004926F4  lea     eax, [ebp+70h+var_3040]
004926FA  mov     [ebp+70h+var_74], 146h
00492701  mov     edx, [esi]
00492703  push    eax
00492704  mov     edx, [edx+40h]
00492707  lea     ecx, [ebp+70h+var_E64]
0049270D  push    ecx
0049270E  mov     ecx, esi
00492710  call    edx
00492712  mov     ecx, eax
00492714  call    sub_5B05A0
00492719  lea     ecx, [ebp+70h+var_E64]
0049271F  mov     dword_752E28, eax
00492724  call    sub_5B0440
00492729  lea     ecx, [ebp+70h+var_3040]; void *
0049272F  mov     [ebp+70h+var_74], edi
00492732  call    sub_401E90
00492737  push    offset aImageZombieBob_0; "IMAGE_ZOMBIE_BOBSLED2"
0049273C  lea     ecx, [ebp+70h+var_3078]
00492742  call    sub_4033F0
00492747  mov     [ebp+70h+var_74], 147h
0049274E  mov     eax, [esi]
00492750  mov     eax, [eax+40h]
00492753  lea     ecx, [ebp+70h+var_3078]
00492759  push    ecx
0049275A  lea     edx, [ebp+70h+var_E7C]
00492760  push    edx
00492761  mov     ecx, esi
00492763  call    eax
00492765  mov     ecx, eax
00492767  call    sub_5B05A0
0049276C  lea     ecx, [ebp+70h+var_E7C]
00492772  mov     dword_7535D8, eax
00492777  call    sub_5B0440
0049277C  lea     ecx, [ebp+70h+var_3078]; void *
00492782  mov     [ebp+70h+var_74], edi
00492785  call    sub_401E90
0049278A  push    offset aImageZombieBob_1; "IMAGE_ZOMBIE_BOBSLED3"
0049278F  lea     ecx, [ebp+70h+var_30B0]
00492795  call    sub_4033F0
0049279A  lea     eax, [ebp+70h+var_30B0]
004927A0  mov     [ebp+70h+var_74], 148h
004927A7  mov     edx, [esi]
004927A9  mov     edx, [edx+40h]
004927AC  push    eax
004927AD  lea     ecx, [ebp+70h+var_E94]
004927B3  push    ecx
004927B4  mov     ecx, esi
004927B6  call    edx
004927B8  mov     ecx, eax
004927BA  call    sub_5B05A0
004927BF  lea     ecx, [ebp+70h+var_E94]
004927C5  mov     dword_753704, eax
004927CA  call    sub_5B0440
004927CF  lea     ecx, [ebp+70h+var_30B0]; void *
004927D5  mov     [ebp+70h+var_74], edi
004927D8  call    sub_401E90
004927DD  push    offset aImageZombieBob_2; "IMAGE_ZOMBIE_BOBSLED4"
004927E2  lea     ecx, [ebp+70h+var_30E8]
004927E8  call    sub_4033F0
004927ED  mov     [ebp+70h+var_74], 149h
004927F4  mov     eax, [esi]
004927F6  mov     eax, [eax+40h]
004927F9  lea     ecx, [ebp+70h+var_30E8]
004927FF  push    ecx
00492800  lea     edx, [ebp+70h+var_EAC]
00492806  push    edx
00492807  mov     ecx, esi
00492809  call    eax
0049280B  mov     ecx, eax
0049280D  call    sub_5B05A0
00492812  lea     ecx, [ebp+70h+var_EAC]
00492818  mov     dword_753530, eax
0049281D  call    sub_5B0440
00492822  lea     ecx, [ebp+70h+var_30E8]; void *
00492828  mov     [ebp+70h+var_74], edi
0049282B  call    sub_401E90
00492830  push    offset aImageZombieBob_3; "IMAGE_ZOMBIE_BOBSLED_INSIDE"
00492835  lea     ecx, [ebp+70h+var_3120]
0049283B  call    sub_4033F0
00492840  mov     [ebp+70h+var_74], 14Ah
00492847  mov     edx, [esi]
00492849  mov     edx, [edx+40h]
0049284C  lea     eax, [ebp+70h+var_3120]
00492852  push    eax
00492853  lea     ecx, [ebp+70h+var_EC4]
00492859  push    ecx
0049285A  mov     ecx, esi
0049285C  call    edx
0049285E  mov     ecx, eax
00492860  call    sub_5B05A0
00492865  lea     ecx, [ebp+70h+var_EC4]
0049286B  mov     dword_753418, eax
00492870  call    sub_5B0440
00492875  lea     ecx, [ebp+70h+var_3120]; void *
0049287B  mov     [ebp+70h+var_74], edi
0049287E  call    sub_401E90
00492883  push    offset aImageZombieBos; "IMAGE_ZOMBIE_BOSS_FIREBALL_GROUNDPARTIC"...
00492888  lea     ecx, [ebp+70h+var_3158]
0049288E  call    sub_4033F0
00492893  mov     [ebp+70h+var_74], 14Bh
0049289A  mov     eax, [esi]
0049289C  mov     eax, [eax+40h]
0049289F  lea     ecx, [ebp+70h+var_3158]
004928A5  push    ecx
004928A6  lea     edx, [ebp+70h+var_EDC]
004928AC  push    edx
004928AD  mov     ecx, esi
004928AF  call    eax
004928B1  mov     ecx, eax
004928B3  call    sub_5B05A0
004928B8  lea     ecx, [ebp+70h+var_EDC]
004928BE  mov     dword_752D08, eax
004928C3  call    sub_5B0440
004928C8  lea     ecx, [ebp+70h+var_3158]; void *
004928CE  mov     [ebp+70h+var_74], edi
004928D1  call    sub_401E90
004928D6  push    offset aImageZombieBos_0; "IMAGE_ZOMBIE_BOSS_FIREBALL_PARTICLES"
004928DB  lea     ecx, [ebp+70h+var_3190]
004928E1  call    sub_4033F0
004928E6  lea     eax, [ebp+70h+var_3190]
004928EC  mov     [ebp+70h+var_74], 14Ch
004928F3  mov     edx, [esi]
004928F5  mov     edx, [edx+40h]
004928F8  push    eax
004928F9  lea     ecx, [ebp+70h+var_EF4]
004928FF  push    ecx
00492900  mov     ecx, esi
00492902  call    edx
00492904  mov     ecx, eax
00492906  call    sub_5B05A0
0049290B  lea     ecx, [ebp+70h+var_EF4]
00492911  mov     dword_753248, eax
00492916  call    sub_5B0440
0049291B  lea     ecx, [ebp+70h+var_3190]; void *
00492921  mov     [ebp+70h+var_74], edi
00492924  call    sub_401E90
00492929  push    offset aImageZombieBos_1; "IMAGE_ZOMBIE_BOSS_ICEBALL_GROUNDPARTICL"...
0049292E  lea     ecx, [ebp+70h+var_31C8]
00492934  call    sub_4033F0
00492939  mov     [ebp+70h+var_74], 14Dh
00492940  mov     eax, [esi]
00492942  mov     eax, [eax+40h]
00492945  lea     ecx, [ebp+70h+var_31C8]
0049294B  push    ecx
0049294C  lea     edx, [ebp+70h+var_F0C]
00492952  push    edx
00492953  mov     ecx, esi
00492955  call    eax
00492957  mov     ecx, eax
00492959  call    sub_5B05A0
0049295E  lea     ecx, [ebp+70h+var_F0C]
00492964  mov     dword_752C60, eax
00492969  call    sub_5B0440
0049296E  lea     ecx, [ebp+70h+var_31C8]; void *
00492974  mov     [ebp+70h+var_74], edi
00492977  call    sub_401E90
0049297C  push    offset aImageZombieBos_2; "IMAGE_ZOMBIE_BOSS_ICEBALL_PARTICLES"
00492981  lea     ecx, [ebp+70h+var_3200]
00492987  call    sub_4033F0
0049298C  lea     eax, [ebp+70h+var_3200]
00492992  mov     [ebp+70h+var_74], 14Eh
00492999  mov     edx, [esi]
0049299B  mov     edx, [edx+40h]
0049299E  push    eax
0049299F  lea     ecx, [ebp+70h+var_F24]
004929A5  push    ecx
004929A6  mov     ecx, esi
004929A8  call    edx
004929AA  mov     ecx, eax
004929AC  call    sub_5B05A0
004929B1  lea     ecx, [ebp+70h+var_F24]
004929B7  mov     dword_753684, eax
004929BC  call    sub_5B0440
004929C1  lea     ecx, [ebp+70h+var_3200]; void *
004929C7  mov     [ebp+70h+var_74], edi
004929CA  call    sub_401E90
004929CF  push    offset aImageZombieNot_5; "IMAGE_ZOMBIE_NOTE_SMALL"
004929D4  lea     ecx, [ebp+70h+var_3238]
004929DA  call    sub_4033F0
004929DF  mov     [ebp+70h+var_74], 14Fh
004929E6  mov     eax, [esi]
004929E8  mov     eax, [eax+40h]
004929EB  lea     ecx, [ebp+70h+var_3238]
004929F1  push    ecx
004929F2  lea     edx, [ebp+70h+var_F3C]
004929F8  push    edx
004929F9  mov     ecx, esi
004929FB  call    eax
004929FD  mov     ecx, eax
004929FF  call    sub_5B05A0
00492A04  lea     ecx, [ebp+70h+var_F3C]
00492A0A  mov     dword_753298, eax
00492A0F  call    sub_5B0440
00492A14  lea     ecx, [ebp+70h+var_3238]; void *
00492A1A  mov     [ebp+70h+var_74], edi
00492A1D  call    sub_401E90
00492A22  push    offset aImageZombieSea; "IMAGE_ZOMBIE_SEAWEED"
00492A27  lea     ecx, [ebp+70h+var_3270]
00492A2D  call    sub_4033F0
00492A32  lea     eax, [ebp+70h+var_3270]
00492A38  mov     [ebp+70h+var_74], 150h
00492A3F  mov     edx, [esi]
00492A41  mov     edx, [edx+40h]
00492A44  push    eax
00492A45  lea     ecx, [ebp+70h+var_F54]
00492A4B  push    ecx
00492A4C  mov     ecx, esi
00492A4E  call    edx
00492A50  mov     ecx, eax
00492A52  call    sub_5B05A0
00492A57  lea     ecx, [ebp+70h+var_F54]
00492A5D  mov     dword_7531F4, eax
00492A62  call    sub_5B0440
00492A67  lea     ecx, [ebp+70h+var_3270]; void *
00492A6D  mov     [ebp+70h+var_74], edi
00492A70  call    sub_401E90
00492A75  push    offset aImageZombossPa; "IMAGE_ZOMBOSS_PARTICLES"
00492A7A  lea     ecx, [ebp+70h+var_32A8]
00492A80  call    sub_4033F0
00492A85  mov     [ebp+70h+var_74], 151h
00492A8C  mov     eax, [esi]
00492A8E  mov     eax, [eax+40h]
00492A91  lea     ecx, [ebp+70h+var_32A8]
00492A97  push    ecx
00492A98  lea     edx, [ebp+70h+var_F6C]
00492A9E  push    edx
00492A9F  mov     ecx, esi
00492AA1  call    eax
00492AA3  mov     ecx, eax
00492AA5  call    sub_5B05A0
00492AAA  lea     ecx, [ebp+70h+var_F6C]
00492AB0  mov     dword_7531AC, eax
00492AB5  call    sub_5B0440
00492ABA  mov     [ebp+70h+var_74], edi
00492ABD  lea     ecx, [ebp+70h+var_32A8]; void *
00492AC3  call    sub_401E90
00492AC8  push    offset aImageSelectors_12; "IMAGE_SELECTORSCREEN_ACHIEVEMENTS_BG"
00492ACD  lea     ecx, [ebp+70h+var_32E0]
00492AD3  call    sub_4033F0
00492AD8  lea     eax, [ebp+70h+var_32E0]
00492ADE  mov     [ebp+70h+var_74], 152h
00492AE5  mov     edx, [esi]
00492AE7  mov     edx, [edx+40h]
00492AEA  push    eax
00492AEB  lea     ecx, [ebp+70h+var_F84]
00492AF1  push    ecx
00492AF2  mov     ecx, esi
00492AF4  call    edx
00492AF6  mov     ecx, eax
00492AF8  call    sub_5B05A0
00492AFD  lea     ecx, [ebp+70h+var_F84]
00492B03  mov     dword_7530AC, eax
00492B08  call    sub_5B0440
00492B0D  lea     ecx, [ebp+70h+var_32E0]; void *
00492B13  mov     [ebp+70h+var_74], edi
00492B16  call    sub_401E90
00492B1B  push    offset aImageSelectors_13; "IMAGE_SELECTORSCREEN_MOREWAYSTOPLAY_BG"
00492B20  lea     ecx, [ebp+70h+var_3318]
00492B26  call    sub_4033F0
00492B2B  mov     [ebp+70h+var_74], 153h
00492B32  mov     eax, [esi]
00492B34  mov     eax, [eax+40h]
00492B37  lea     ecx, [ebp+70h+var_3318]
00492B3D  push    ecx
00492B3E  lea     edx, [ebp+70h+var_F9C]
00492B44  push    edx
00492B45  mov     ecx, esi
00492B47  call    eax
00492B49  mov     ecx, eax
00492B4B  call    sub_5B05A0
00492B50  lea     ecx, [ebp+70h+var_F9C]
00492B56  mov     dword_7532D4, eax
00492B5B  call    sub_5B0440
00492B60  lea     ecx, [ebp+70h+var_3318]; void *
00492B66  mov     [ebp+70h+var_74], edi
00492B69  call    sub_401E90
00492B6E  push    offset aImageSelectors_14; "IMAGE_SELECTORSCREEN_ACHIEVEMENTS_PEDES"...
00492B73  lea     ecx, [ebp+70h+var_3350]
00492B79  call    sub_4033F0
00492B7E  lea     eax, [ebp+70h+var_3350]
00492B84  mov     [ebp+70h+var_74], 154h
00492B8B  mov     edx, [esi]
00492B8D  mov     edx, [edx+40h]
00492B90  push    eax
00492B91  lea     ecx, [ebp+70h+var_FB4]
00492B97  push    ecx
00492B98  mov     ecx, esi
00492B9A  call    edx
00492B9C  mov     ecx, eax
00492B9E  call    sub_5B05A0
00492BA3  lea     ecx, [ebp+70h+var_FB4]
00492BA9  mov     dword_752D50, eax
00492BAE  call    sub_5B0440
00492BB3  lea     ecx, [ebp+70h+var_3350]; void *
00492BB9  mov     [ebp+70h+var_74], edi
00492BBC  call    sub_401E90
00492BC1  push    offset aImageSelectors_15; "IMAGE_SELECTORSCREEN_ACHIEVEMENTS_PEDES"...
00492BC6  lea     ecx, [ebp+70h+var_3388]
00492BCC  call    sub_4033F0
00492BD1  mov     [ebp+70h+var_74], 155h
00492BD8  mov     eax, [esi]
00492BDA  mov     eax, [eax+40h]
00492BDD  lea     ecx, [ebp+70h+var_3388]
00492BE3  push    ecx
00492BE4  lea     edx, [ebp+70h+var_FCC]
00492BEA  push    edx
00492BEB  mov     ecx, esi
00492BED  call    eax
00492BEF  mov     ecx, eax
00492BF1  call    sub_5B05A0
00492BF6  mov     dword_753468, eax
00492BFB  lea     ecx, [ebp+70h+var_FCC]
00492C01  call    sub_5B0440
00492C06  lea     ecx, [ebp+70h+var_3388]; void *
00492C0C  mov     [ebp+70h+var_74], edi
00492C0F  call    sub_401E90
00492C14  push    offset aImageAcheeseme; "IMAGE_ACHEESEMENTS_BEJEWELED"
00492C19  lea     ecx, [ebp+70h+var_33C0]
00492C1F  call    sub_4033F0
00492C24  lea     eax, [ebp+70h+var_33C0]
00492C2A  mov     [ebp+70h+var_74], 156h
00492C31  mov     edx, [esi]
00492C33  mov     edx, [edx+40h]
00492C36  push    eax
00492C37  lea     ecx, [ebp+70h+var_FE4]
00492C3D  push    ecx
00492C3E  mov     ecx, esi
00492C40  call    edx
00492C42  mov     ecx, eax
00492C44  call    sub_5B05A0
00492C49  lea     ecx, [ebp+70h+var_FE4]
00492C4F  mov     dword_75359C, eax
00492C54  call    sub_5B0440
00492C59  lea     ecx, [ebp+70h+var_33C0]; void *
00492C5F  mov     [ebp+70h+var_74], edi
00492C62  call    sub_401E90
00492C67  push    offset aImageAcheeseme_0; "IMAGE_ACHEESEMENTS_BOOKWORM"
00492C6C  lea     ecx, [ebp+70h+var_33F8]
00492C72  call    sub_4033F0
00492C77  mov     [ebp+70h+var_74], 157h
00492C7E  mov     eax, [esi]
00492C80  mov     eax, [eax+40h]
00492C83  lea     ecx, [ebp+70h+var_33F8]
00492C89  push    ecx
00492C8A  lea     edx, [ebp+70h+var_FFC]
00492C90  push    edx
00492C91  mov     ecx, esi
00492C93  call    eax
00492C95  mov     ecx, eax
00492C97  call    sub_5B05A0
00492C9C  lea     ecx, [ebp+70h+var_FFC]
00492CA2  mov     dword_753464, eax
00492CA7  call    sub_5B0440
00492CAC  lea     ecx, [ebp+70h+var_33F8]; void *
00492CB2  mov     [ebp+70h+var_74], edi
00492CB5  call    sub_401E90
00492CBA  push    offset aImageAcheeseme_1; "IMAGE_ACHEESEMENTS_CHINA"
00492CBF  lea     ecx, [ebp+70h+var_3430]
00492CC5  call    sub_4033F0
00492CCA  lea     eax, [ebp+70h+var_3430]
00492CD0  mov     [ebp+70h+var_74], 158h
00492CD7  mov     edx, [esi]
00492CD9  mov     edx, [edx+40h]
00492CDC  push    eax
00492CDD  lea     ecx, [ebp+70h+var_1014]
00492CE3  push    ecx
00492CE4  mov     ecx, esi
00492CE6  call    edx
00492CE8  mov     ecx, eax
00492CEA  call    sub_5B05A0
00492CEF  lea     ecx, [ebp+70h+var_1014]
00492CF5  mov     dword_753294, eax
00492CFA  call    sub_5B0440
00492CFF  lea     ecx, [ebp+70h+var_3430]; void *
00492D05  mov     [ebp+70h+var_74], edi
00492D08  call    sub_401E90
00492D0D  push    offset aImageAcheeseme_2; "IMAGE_ACHEESEMENTS_CHUZZLE"
00492D12  lea     ecx, [ebp+70h+var_3468]
00492D18  call    sub_4033F0
00492D1D  mov     [ebp+70h+var_74], 159h
00492D24  mov     eax, [esi]
00492D26  mov     eax, [eax+40h]
00492D29  lea     ecx, [ebp+70h+var_3468]
00492D2F  push    ecx
00492D30  lea     edx, [ebp+70h+var_102C]
00492D36  push    edx
00492D37  mov     ecx, esi
00492D39  call    eax
00492D3B  mov     ecx, eax
00492D3D  call    sub_5B05A0
00492D42  lea     ecx, [ebp+70h+var_102C]
00492D48  mov     dword_7534F8, eax
00492D4D  call    sub_5B0440
00492D52  lea     ecx, [ebp+70h+var_3468]; void *
00492D58  mov     [ebp+70h+var_74], edi
00492D5B  call    sub_401E90
00492D60  push    offset aImageAcheeseme_3; "IMAGE_ACHEESEMENTS_HOLE_TILE"
00492D65  lea     ecx, [ebp+70h+var_34A0]
00492D6B  call    sub_4033F0
00492D70  lea     eax, [ebp+70h+var_34A0]
00492D76  mov     [ebp+70h+var_74], 15Ah
00492D7D  mov     edx, [esi]
00492D7F  mov     edx, [edx+40h]
00492D82  push    eax
00492D83  lea     ecx, [ebp+70h+var_1044]
00492D89  push    ecx
00492D8A  mov     ecx, esi
00492D8C  call    edx
00492D8E  mov     ecx, eax
00492D90  call    sub_5B05A0
00492D95  lea     ecx, [ebp+70h+var_1044]
00492D9B  mov     dword_752D8C, eax
00492DA0  call    sub_5B0440
00492DA5  lea     ecx, [ebp+70h+var_34A0]; void *
00492DAB  mov     [ebp+70h+var_74], edi
00492DAE  call    sub_401E90
00492DB3  push    offset aImageAcheeseme_4; "IMAGE_ACHEESEMENTS_PEGGLE"
00492DB8  lea     ecx, [ebp+70h+var_34D8]
00492DBE  call    sub_4033F0
00492DC3  mov     [ebp+70h+var_74], 15Bh
00492DCA  mov     eax, [esi]
00492DCC  mov     eax, [eax+40h]
00492DCF  lea     ecx, [ebp+70h+var_34D8]
00492DD5  push    ecx
00492DD6  lea     edx, [ebp+70h+var_105C]
00492DDC  push    edx
00492DDD  mov     ecx, esi
00492DDF  call    eax
00492DE1  mov     ecx, eax
00492DE3  call    sub_5B05A0
00492DE8  lea     ecx, [ebp+70h+var_105C]
00492DEE  mov     dword_752D44, eax
00492DF3  call    sub_5B0440
00492DF8  lea     ecx, [ebp+70h+var_34D8]; void *
00492DFE  mov     [ebp+70h+var_74], edi
00492E01  call    sub_401E90
00492E06  push    offset aImageAcheeseme_5; "IMAGE_ACHEESEMENTS_PIPE"
00492E0B  lea     ecx, [ebp+70h+var_3510]
00492E11  call    sub_4033F0
00492E16  lea     eax, [ebp+70h+var_3510]
00492E1C  mov     [ebp+70h+var_74], 15Ch
00492E23  mov     edx, [esi]
00492E25  mov     edx, [edx+40h]
00492E28  push    eax
00492E29  lea     ecx, [ebp+70h+var_1074]
00492E2F  push    ecx
00492E30  mov     ecx, esi
00492E32  call    edx
00492E34  mov     ecx, eax
00492E36  call    sub_5B05A0
00492E3B  lea     ecx, [ebp+70h+var_1074]
00492E41  mov     dword_752CD4, eax
00492E46  call    sub_5B0440
00492E4B  lea     ecx, [ebp+70h+var_3510]; void *
00492E51  mov     [ebp+70h+var_74], edi
00492E54  call    sub_401E90
00492E59  push    offset aImageAcheeseme_6; "IMAGE_ACHEESEMENTS_ZUMA"
00492E5E  lea     ecx, [ebp+70h+var_3548]
00492E64  call    sub_4033F0
00492E69  lea     ecx, [ebp+70h+var_3548]
00492E6F  push    ecx
00492E70  lea     edx, [ebp+70h+var_108C]
00492E76  mov     [ebp+70h+var_74], 15Dh
00492E7D  mov     eax, [esi]
00492E7F  push    edx
00492E80  mov     ecx, esi
00492E82  mov     eax, [eax+40h]
00492E85  call    eax
00492E87  mov     ecx, eax
00492E89  call    sub_5B05A0
00492E8E  lea     ecx, [ebp+70h+var_108C]
00492E94  mov     dword_752EE8, eax
00492E99  call    sub_5B0440
00492E9E  lea     ecx, [ebp+70h+var_3548]; void *
00492EA4  mov     [ebp+70h+var_74], edi
00492EA7  call    sub_401E90
00492EAC  push    offset aImageAcheeseme_7; "IMAGE_ACHEESEMENTS_ICONS"
00492EB1  lea     ecx, [ebp+70h+var_3580]
00492EB7  call    sub_4033F0
00492EBC  lea     eax, [ebp+70h+var_3580]
00492EC2  mov     [ebp+70h+var_74], 15Eh
00492EC9  mov     edx, [esi]
00492ECB  mov     edx, [edx+40h]
00492ECE  push    eax
00492ECF  lea     ecx, [ebp+70h+var_10A4]
00492ED5  push    ecx
00492ED6  mov     ecx, esi
00492ED8  call    edx
00492EDA  mov     ecx, eax
00492EDC  call    sub_5B05A0
00492EE1  lea     ecx, [ebp+70h+var_10A4]
00492EE7  mov     dword_753080, eax
00492EEC  call    sub_5B0440
00492EF1  lea     ecx, [ebp+70h+var_3580]; void *
00492EF7  mov     [ebp+70h+var_74], edi
00492EFA  call    sub_401E90
00492EFF  push    offset aImageAcheeseme_8; "IMAGE_ACHEESEMENTS_BACK_HIGHLIGHT"
00492F04  lea     ecx, [ebp+70h+var_35B8]
00492F0A  call    sub_4033F0
00492F0F  mov     [ebp+70h+var_74], 15Fh
00492F16  mov     eax, [esi]
00492F18  mov     eax, [eax+40h]
00492F1B  lea     ecx, [ebp+70h+var_35B8]
00492F21  push    ecx
00492F22  lea     edx, [ebp+70h+var_10BC]
00492F28  push    edx
00492F29  mov     ecx, esi
00492F2B  call    eax
00492F2D  mov     ecx, eax
00492F2F  call    sub_5B05A0
00492F34  lea     ecx, [ebp+70h+var_10BC]
00492F3A  mov     dword_753300, eax
00492F3F  call    sub_5B0440
00492F44  lea     ecx, [ebp+70h+var_35B8]; void *
00492F4A  mov     [ebp+70h+var_74], edi
00492F4D  call    sub_401E90
00492F52  push    offset aImageQuickplay; "IMAGE_QUICKPLAY_MINIGAMES_BUTTON"
00492F57  lea     ecx, [ebp+70h+var_35F0]
00492F5D  call    sub_4033F0
00492F62  lea     eax, [ebp+70h+var_35F0]
00492F68  mov     [ebp+70h+var_74], 160h
00492F6F  mov     edx, [esi]
00492F71  mov     edx, [edx+40h]
00492F74  push    eax
00492F75  lea     ecx, [ebp+70h+var_10D4]
00492F7B  push    ecx
00492F7C  mov     ecx, esi
00492F7E  call    edx
00492F80  mov     ecx, eax
00492F82  call    sub_5B05A0
00492F87  lea     ecx, [ebp+70h+var_10D4]
00492F8D  mov     dword_753610, eax
00492F92  call    sub_5B0440
00492F97  lea     ecx, [ebp+70h+var_35F0]; void *
00492F9D  mov     [ebp+70h+var_74], edi
00492FA0  call    sub_401E90
00492FA5  push    offset aImageQuickplay_0; "IMAGE_QUICKPLAY_MINIGAMES_BUTTON_HIGHLI"...
00492FAA  lea     ecx, [ebp+70h+var_3628]
00492FB0  call    sub_4033F0
00492FB5  lea     ecx, [ebp+70h+var_3628]
00492FBB  mov     [ebp+70h+var_74], 161h
00492FC2  mov     eax, [esi]
00492FC4  push    ecx
00492FC5  mov     eax, [eax+40h]
00492FC8  lea     edx, [ebp+70h+var_10EC]
00492FCE  push    edx
00492FCF  mov     ecx, esi
00492FD1  call    eax
00492FD3  mov     ecx, eax
00492FD5  call    sub_5B05A0
00492FDA  lea     ecx, [ebp+70h+var_10EC]
00492FE0  mov     dword_7534B0, eax
00492FE5  call    sub_5B0440
00492FEA  lea     ecx, [ebp+70h+var_3628]; void *
00492FF0  mov     [ebp+70h+var_74], edi
00492FF3  call    sub_401E90
00492FF8  push    offset aImageQuickplay_1; "IMAGE_QUICKPLAY_MINIGAMES_CLOUD"
00492FFD  lea     ecx, [ebp+70h+var_3660]
00493003  call    sub_4033F0
00493008  lea     eax, [ebp+70h+var_3660]
0049300E  mov     [ebp+70h+var_74], 162h
00493015  mov     edx, [esi]
00493017  mov     edx, [edx+40h]
0049301A  push    eax
0049301B  lea     ecx, [ebp+70h+var_1104]
00493021  push    ecx
00493022  mov     ecx, esi
00493024  call    edx
00493026  mov     ecx, eax
00493028  call    sub_5B05A0
0049302D  lea     ecx, [ebp+70h+var_1104]
00493033  mov     dword_752F9C, eax
00493038  call    sub_5B0440
0049303D  lea     ecx, [ebp+70h+var_3660]; void *
00493043  mov     [ebp+70h+var_74], edi
00493046  call    sub_401E90
0049304B  push    offset aImageQuickplay_2; "IMAGE_QUICKPLAY_PUZZLES_BUTTON"
00493050  lea     ecx, [ebp+70h+var_3698]
00493056  call    sub_4033F0
0049305B  mov     [ebp+70h+var_74], 163h
00493062  mov     eax, [esi]
00493064  mov     eax, [eax+40h]
00493067  lea     ecx, [ebp+70h+var_3698]
0049306D  push    ecx
0049306E  lea     edx, [ebp+70h+var_111C]
00493074  push    edx
00493075  mov     ecx, esi
00493077  call    eax
00493079  mov     ecx, eax
0049307B  call    sub_5B05A0
00493080  lea     ecx, [ebp+70h+var_111C]
00493086  mov     dword_752C20, eax
0049308B  call    sub_5B0440
00493090  lea     ecx, [ebp+70h+var_3698]; void *
00493096  mov     [ebp+70h+var_74], edi
00493099  call    sub_401E90
0049309E  push    offset aImageQuickplay_3; "IMAGE_QUICKPLAY_PUZZLES_BUTTON_HIGHLIGH"...
004930A3  lea     ecx, [ebp+70h+var_36D0]
004930A9  call    sub_4033F0
004930AE  lea     eax, [ebp+70h+var_36D0]
004930B4  mov     [ebp+70h+var_74], 164h
004930BB  mov     edx, [esi]
004930BD  mov     edx, [edx+40h]
004930C0  push    eax
004930C1  lea     ecx, [ebp+70h+var_1134]
004930C7  push    ecx
004930C8  mov     ecx, esi
004930CA  call    edx
004930CC  mov     ecx, eax
004930CE  call    sub_5B05A0
004930D3  lea     ecx, [ebp+70h+var_1134]
004930D9  mov     dword_752CEC, eax
004930DE  call    sub_5B0440
004930E3  lea     ecx, [ebp+70h+var_36D0]; void *
004930E9  mov     [ebp+70h+var_74], edi
004930EC  call    sub_401E90
004930F1  push    offset aImageQuickplay_4; "IMAGE_QUICKPLAY_PUZZLES_CLOUD"
004930F6  lea     ecx, [ebp+70h+var_3708]
004930FC  call    sub_4033F0
00493101  mov     [ebp+70h+var_74], 165h
00493108  mov     eax, [esi]
0049310A  mov     eax, [eax+40h]
0049310D  lea     ecx, [ebp+70h+var_3708]
00493113  push    ecx
00493114  lea     edx, [ebp+70h+var_114C]
0049311A  push    edx
0049311B  mov     ecx, esi
0049311D  call    eax
0049311F  mov     ecx, eax
00493121  call    sub_5B05A0
00493126  lea     ecx, [ebp+70h+var_114C]
0049312C  mov     dword_753414, eax
00493131  call    sub_5B0440
00493136  lea     ecx, [ebp+70h+var_3708]; void *
0049313C  mov     [ebp+70h+var_74], edi
0049313F  call    sub_401E90
00493144  push    offset aImageQuickplay_5; "IMAGE_QUICKPLAY_SURVIVAL_BUTTON"
00493149  lea     ecx, [ebp+70h+var_3740]
0049314F  call    sub_4033F0
00493154  lea     eax, [ebp+70h+var_3740]
0049315A  mov     [ebp+70h+var_74], 166h
00493161  mov     edx, [esi]
00493163  mov     edx, [edx+40h]
00493166  push    eax
00493167  lea     ecx, [ebp+70h+var_1164]
0049316D  push    ecx
0049316E  mov     ecx, esi
00493170  call    edx
00493172  mov     ecx, eax
00493174  call    sub_5B05A0
00493179  lea     ecx, [ebp+70h+var_1164]
0049317F  mov     dword_752E60, eax
00493184  call    sub_5B0440
00493189  lea     ecx, [ebp+70h+var_3740]; void *
0049318F  mov     [ebp+70h+var_74], edi
00493192  call    sub_401E90
00493197  push    offset aImageQuickplay_6; "IMAGE_QUICKPLAY_SURVIVAL_BUTTON_HIGHLIG"...
0049319C  lea     ecx, [ebp+70h+var_3778]
004931A2  call    sub_4033F0
004931A7  mov     [ebp+70h+var_74], 167h
004931AE  mov     eax, [esi]
004931B0  mov     eax, [eax+40h]
004931B3  lea     ecx, [ebp+70h+var_3778]
004931B9  push    ecx
004931BA  lea     edx, [ebp+70h+var_117C]
004931C0  push    edx
004931C1  mov     ecx, esi
004931C3  call    eax
004931C5  mov     ecx, eax
004931C7  call    sub_5B05A0
004931CC  lea     ecx, [ebp+70h+var_117C]
004931D2  mov     dword_752AB8, eax
004931D7  call    sub_5B0440
004931DC  lea     ecx, [ebp+70h+var_3778]; void *
004931E2  mov     [ebp+70h+var_74], edi
004931E5  call    sub_401E90
004931EA  push    offset aImageQuickplay_7; "IMAGE_QUICKPLAY_SURVIVAL_CLOUD"
004931EF  lea     ecx, [ebp+70h+var_37B0]
004931F5  call    sub_4033F0
004931FA  lea     eax, [ebp+70h+var_37B0]
00493200  mov     [ebp+70h+var_74], 168h
00493207  mov     edx, [esi]
00493209  mov     edx, [edx+40h]
0049320C  push    eax
0049320D  lea     ecx, [ebp+70h+var_1194]
00493213  push    ecx
00493214  mov     ecx, esi
00493216  call    edx
00493218  mov     ecx, eax
0049321A  call    sub_5B05A0
0049321F  lea     ecx, [ebp+70h+var_1194]
00493225  mov     dword_7533D8, eax
0049322A  call    sub_5B0440
0049322F  lea     ecx, [ebp+70h+var_37B0]; void *
00493235  mov     [ebp+70h+var_74], edi
00493238  call    sub_401E90
0049323D  push    offset aImageQuickplay_8; "IMAGE_QUICKPLAY_BACK_BUTTON"
00493242  lea     ecx, [ebp+70h+var_37E8]
00493248  call    sub_4033F0
0049324D  mov     [ebp+70h+var_74], 169h
00493254  mov     eax, [esi]
00493256  mov     eax, [eax+40h]
00493259  lea     ecx, [ebp+70h+var_37E8]
0049325F  push    ecx
00493260  lea     edx, [ebp+70h+var_11AC]
00493266  push    edx
00493267  mov     ecx, esi
00493269  call    eax
0049326B  mov     ecx, eax
0049326D  call    sub_5B05A0
00493272  lea     ecx, [ebp+70h+var_11AC]
00493278  mov     dword_75381C, eax
0049327D  call    sub_5B0440
00493282  lea     ecx, [ebp+70h+var_37E8]; void *
00493288  mov     [ebp+70h+var_74], edi
0049328B  call    sub_401E90
00493290  push    offset aImageQuickplay_9; "IMAGE_QUICKPLAY_BACK_BUTTON_HIGHLIGHT"
00493295  lea     ecx, [ebp+70h+var_3820]
0049329B  call    sub_4033F0
004932A0  lea     eax, [ebp+70h+var_3820]
004932A6  mov     [ebp+70h+var_74], 16Ah
004932AD  mov     edx, [esi]
004932AF  mov     edx, [edx+40h]
004932B2  push    eax
004932B3  lea     ecx, [ebp+70h+var_11C4]
004932B9  push    ecx
004932BA  mov     ecx, esi
004932BC  call    edx
004932BE  mov     ecx, eax
004932C0  call    sub_5B05A0
004932C5  lea     ecx, [ebp+70h+var_11C4]
004932CB  mov     dword_752E20, eax
004932D0  call    sub_5B0440
004932D5  lea     ecx, [ebp+70h+var_3820]; void *
004932DB  mov     [ebp+70h+var_74], edi
004932DE  call    sub_401E90
004932E3  push    offset aImageAcheeseme_9; "IMAGE_ACHEESEMENTS_MORE_ROCK"
004932E8  lea     ecx, [ebp+70h+var_3858]
004932EE  call    sub_4033F0
004932F3  mov     [ebp+70h+var_74], 16Bh
004932FA  mov     eax, [esi]
004932FC  mov     eax, [eax+40h]
004932FF  lea     ecx, [ebp+70h+var_3858]
00493305  push    ecx
00493306  lea     edx, [ebp+70h+var_11DC]
0049330C  push    edx
0049330D  mov     ecx, esi
0049330F  call    eax
00493311  mov     ecx, eax
00493313  call    sub_5B05A0
00493318  lea     ecx, [ebp+70h+var_11DC]
0049331E  mov     dword_7536A0, eax
00493323  call    sub_5B0440
00493328  lea     ecx, [ebp+70h+var_3858]; void *
0049332E  mov     [ebp+70h+var_74], edi
00493331  call    sub_401E90
00493336  push    offset aImageAcheeseme_10; "IMAGE_ACHEESEMENTS_MORE_BUTTON"
0049333B  lea     ecx, [ebp+70h+var_3890]
00493341  call    sub_4033F0
00493346  lea     eax, [ebp+70h+var_3890]
0049334C  mov     [ebp+70h+var_74], 16Ch
00493353  mov     edx, [esi]
00493355  mov     edx, [edx+40h]
00493358  push    eax
00493359  lea     ecx, [ebp+70h+var_11F4]
0049335F  push    ecx
00493360  mov     ecx, esi
00493362  call    edx
00493364  mov     ecx, eax
00493366  call    sub_5B05A0
0049336B  lea     ecx, [ebp+70h+var_11F4]
00493371  mov     dword_752E5C, eax
00493376  call    sub_5B0440
0049337B  mov     [ebp+70h+var_74], edi
0049337E  lea     ecx, [ebp+70h+var_3890]; void *
00493384  call    sub_401E90
00493389  push    offset aImageAcheeseme_11; "IMAGE_ACHEESEMENTS_MORE_BUTTON_HIGHLIGH"...
0049338E  lea     ecx, [ebp+70h+var_38C8]
00493394  call    sub_4033F0
00493399  mov     [ebp+70h+var_74], 16Dh
004933A0  mov     eax, [esi]
004933A2  mov     eax, [eax+40h]
004933A5  lea     ecx, [ebp+70h+var_38C8]
004933AB  push    ecx
004933AC  lea     edx, [ebp+70h+var_120C]
004933B2  push    edx
004933B3  mov     ecx, esi
004933B5  call    eax
004933B7  mov     ecx, eax
004933B9  call    sub_5B05A0
004933BE  lea     ecx, [ebp+70h+var_120C]
004933C4  mov     dword_7535E8, eax
004933C9  call    sub_5B0440
004933CE  lea     ecx, [ebp+70h+var_38C8]; void *
004933D4  mov     [ebp+70h+var_74], edi
004933D7  call    sub_401E90
004933DC  push    offset aImageAcheeseme_12; "IMAGE_ACHEESEMENTS_TOP_BUTTON"
004933E1  lea     ecx, [ebp+70h+var_3900]
004933E7  call    sub_4033F0
004933EC  lea     eax, [ebp+70h+var_3900]
004933F2  mov     [ebp+70h+var_74], 16Eh
004933F9  mov     edx, [esi]
004933FB  mov     edx, [edx+40h]
004933FE  push    eax
004933FF  lea     ecx, [ebp+70h+var_1224]
00493405  push    ecx
00493406  mov     ecx, esi
00493408  call    edx
0049340A  mov     ecx, eax
0049340C  call    sub_5B05A0
00493411  lea     ecx, [ebp+70h+var_1224]
00493417  mov     dword_752B88, eax
0049341C  call    sub_5B0440
00493421  lea     ecx, [ebp+70h+var_3900]; void *
00493427  mov     [ebp+70h+var_74], edi
0049342A  call    sub_401E90
0049342F  push    offset aImageAcheeseme_13; "IMAGE_ACHEESEMENTS_TOP_BUTTON_HIGHLIGHT"
00493434  lea     ecx, [ebp+70h+var_3938]
0049343A  call    sub_4033F0
0049343F  mov     [ebp+70h+var_74], 16Fh
00493446  mov     eax, [esi]
00493448  mov     eax, [eax+40h]
0049344B  lea     ecx, [ebp+70h+var_3938]
00493451  push    ecx
00493452  lea     edx, [ebp+70h+var_123C]
00493458  push    edx
00493459  mov     ecx, esi
0049345B  call    eax
0049345D  mov     ecx, eax
0049345F  call    sub_5B05A0
00493464  lea     ecx, [ebp+70h+var_123C]
0049346A  mov     dword_753188, eax
0049346F  call    sub_5B0440
00493474  lea     ecx, [ebp+70h+var_3938]; void *
0049347A  mov     [ebp+70h+var_74], edi
0049347D  call    sub_401E90
00493482  push    offset aImageUnlockNor; "IMAGE_UNLOCK_NORMAL"
00493487  lea     ecx, [ebp+70h+var_3970]
0049348D  call    sub_4033F0
00493492  lea     eax, [ebp+70h+var_3970]
00493498  mov     [ebp+70h+var_74], 170h
0049349F  mov     edx, [esi]
004934A1  mov     edx, [edx+40h]
004934A4  push    eax
004934A5  lea     ecx, [ebp+70h+var_1254]
004934AB  push    ecx
004934AC  mov     ecx, esi
004934AE  call    edx
004934B0  mov     ecx, eax
004934B2  call    sub_5B05A0
004934B7  mov     dword_752BB0, eax
004934BC  lea     ecx, [ebp+70h+var_1254]
004934C2  call    sub_5B0440
004934C7  lea     ecx, [ebp+70h+var_3970]; void *
004934CD  mov     [ebp+70h+var_74], edi
004934D0  call    sub_401E90
004934D5  push    offset aImageUnlockHig; "IMAGE_UNLOCK_HIGHLIGHT"
004934DA  lea     ecx, [ebp+70h+var_39A8]
004934E0  call    sub_4033F0
004934E5  mov     [ebp+70h+var_74], 171h
004934EC  mov     eax, [esi]
004934EE  mov     eax, [eax+40h]
004934F1  lea     ecx, [ebp+70h+var_39A8]
004934F7  push    ecx
004934F8  lea     edx, [ebp+70h+var_126C]
004934FE  push    edx
004934FF  mov     ecx, esi
00493501  call    eax
00493503  mov     ecx, eax
00493505  call    sub_5B05A0
0049350A  lea     ecx, [ebp+70h+var_126C]
00493510  mov     dword_752F20, eax
00493515  call    sub_5B0440
0049351A  lea     ecx, [ebp+70h+var_39A8]; void *
00493520  mov     [ebp+70h+var_74], edi
00493523  call    sub_401E90
00493528  push    offset aImageUnlockPre; "IMAGE_UNLOCK_PRESS"
0049352D  lea     ecx, [ebp+70h+var_39E0]
00493533  call    sub_4033F0
00493538  lea     eax, [ebp+70h+var_39E0]
0049353E  mov     [ebp+70h+var_74], 172h
00493545  mov     edx, [esi]
00493547  mov     edx, [edx+40h]
0049354A  push    eax
0049354B  lea     ecx, [ebp+70h+var_7E0]
00493551  push    ecx
00493552  mov     ecx, esi
00493554  call    edx
00493556  mov     ecx, eax
00493558  call    sub_5B05A0
0049355D  lea     ecx, [ebp+70h+var_7E0]
00493563  mov     dword_752AB4, eax
00493568  call    sub_5B0440
0049356D  lea     ecx, [ebp+70h+var_39E0]; void *
00493573  call    sub_401E90
00493578  mov     al, 1
0049357A  mov     ecx, [ebp+70h+var_7C]
0049357D  mov     large fs:0, ecx
00493584  pop     edi
00493585  pop     esi
00493586  pop     ebx
00493587  add     ebp, 70h ; 'p'
0049358A  mov     esp, ebp
0049358C  pop     ebp
0049358D  retn
0049358E  mov     eax, offset loc_493594
00493593  retn
00493594  mov     ecx, [ebp+70h+var_7C]
00493597  pop     edi
00493598  pop     esi
00493599  xor     al, al
0049359B  mov     large fs:0, ecx
004935A2  pop     ebx
004935A3  add     ebp, 70h ; 'p'
004935A6  mov     esp, ebp
004935A8  pop     ebp
004935A9  retn
006E1E50  lea     ecx, [ebp+70h+var_38]; void *
006E1E53  jmp     sub_401E90
006E1E58  lea     ecx, [ebp+70h+var_1C]; void *
006E1E5B  jmp     sub_401E90
006E1E60  lea     ecx, [ebp+70h+var_438]; void *
006E1E66  jmp     sub_401E90
006E1E6B  lea     ecx, [ebp+70h+var_B8]; void *
006E1E6E  jmp     sub_401E90
006E1E73  lea     ecx, [ebp+70h+var_7B8]; void *
006E1E79  jmp     sub_401E90
006E1E7E  lea     ecx, [ebp+70h+var_F0]; void *
006E1E81  jmp     sub_401E90
006E1E86  lea     ecx, [ebp+70h+var_470]; void *
006E1E8C  jmp     sub_401E90
006E1E91  lea     ecx, [ebp+70h+var_128]; void *
006E1E97  jmp     sub_401E90
006E1E9C  lea     ecx, [ebp+70h+var_630]; void *
006E1EA2  jmp     sub_401E90
006E1EA7  lea     ecx, [ebp+70h+var_160]; void *
006E1EAD  jmp     sub_401E90
006E1EB2  lea     ecx, [ebp+70h+var_4A8]; void *
006E1EB8  jmp     sub_401E90
006E1EBD  lea     ecx, [ebp+70h+var_198]; void *
006E1EC3  jmp     sub_401E90
006E1EC8  lea     ecx, [ebp+70h+var_710]; void *
006E1ECE  jmp     sub_401E90
006E1ED3  lea     ecx, [ebp+70h+var_1D0]; void *
006E1ED9  jmp     sub_401E90
006E1EDE  lea     ecx, [ebp+70h+var_4E0]; void *
006E1EE4  jmp     sub_401E90
006E1EE9  lea     ecx, [ebp+70h+var_208]; void *
006E1EEF  jmp     sub_401E90
006E1EF4  lea     ecx, [ebp+70h+var_668]; void *
006E1EFA  jmp     sub_401E90
006E1EFF  lea     ecx, [ebp+70h+var_240]; void *
006E1F05  jmp     sub_401E90
006E1F0A  lea     ecx, [ebp+70h+var_518]; void *
006E1F10  jmp     sub_401E90
006E1F15  lea     ecx, [ebp+70h+var_278]; void *
006E1F1B  jmp     sub_401E90
006E1F20  lea     ecx, [ebp+70h+var_780]; void *
006E1F26  jmp     sub_401E90
006E1F2B  lea     ecx, [ebp+70h+var_2B0]; void *
006E1F31  jmp     sub_401E90
006E1F36  lea     ecx, [ebp+70h+var_550]; void *
006E1F3C  jmp     sub_401E90
006E1F41  lea     ecx, [ebp+70h+var_2E8]; void *
006E1F47  jmp     sub_401E90
006E1F4C  lea     ecx, [ebp+70h+var_6A0]; void *
006E1F52  jmp     sub_401E90
006E1F57  lea     ecx, [ebp+70h+var_320]; void *
006E1F5D  jmp     sub_401E90
006E1F62  lea     ecx, [ebp+70h+var_588]; void *
006E1F68  jmp     sub_401E90
006E1F6D  lea     ecx, [ebp+70h+var_358]; void *
006E1F73  jmp     sub_401E90
006E1F78  lea     ecx, [ebp+70h+var_748]; void *
006E1F7E  jmp     sub_401E90
006E1F83  lea     ecx, [ebp+70h+var_390]; void *
006E1F89  jmp     sub_401E90
006E1F8E  lea     ecx, [ebp+70h+var_5C0]; void *
006E1F94  jmp     sub_401E90
006E1F99  lea     ecx, [ebp+70h+var_3C8]; void *
006E1F9F  jmp     sub_401E90
006E1FA4  lea     ecx, [ebp+70h+var_6D8]; void *
006E1FAA  jmp     sub_401E90
006E1FAF  lea     ecx, [ebp+70h+var_400]; void *
006E1FB5  jmp     sub_401E90
006E1FBA  lea     ecx, [ebp+70h+var_54]; void *
006E1FBD  jmp     sub_401E90
006E1FC2  lea     ecx, [ebp+70h+var_70]; void *
006E1FC5  jmp     sub_401E90
006E1FCA  lea     ecx, [ebp+70h+var_9C]; void *
006E1FCD  jmp     sub_401E90
006E1FD2  lea     ecx, [ebp+70h+var_D4]; void *
006E1FD5  jmp     sub_401E90
006E1FDA  lea     ecx, [ebp+70h+var_10C]; void *
006E1FE0  jmp     sub_401E90
006E1FE5  lea     ecx, [ebp+70h+var_144]; void *
006E1FEB  jmp     sub_401E90
006E1FF0  lea     ecx, [ebp+70h+var_17C]; void *
006E1FF6  jmp     sub_401E90
006E1FFB  lea     ecx, [ebp+70h+var_1B4]; void *
006E2001  jmp     sub_401E90
006E2006  lea     ecx, [ebp+70h+var_1EC]; void *
006E200C  jmp     sub_401E90
006E2011  lea     ecx, [ebp+70h+var_224]; void *
006E2017  jmp     sub_401E90
006E201C  lea     ecx, [ebp+70h+var_25C]; void *
006E2022  jmp     sub_401E90
006E2027  lea     ecx, [ebp+70h+var_294]; void *
006E202D  jmp     sub_401E90
006E2032  lea     ecx, [ebp+70h+var_2CC]; void *
006E2038  jmp     sub_401E90
006E203D  lea     ecx, [ebp+70h+var_304]; void *
006E2043  jmp     sub_401E90
006E2048  lea     ecx, [ebp+70h+var_33C]; void *
006E204E  jmp     sub_401E90
006E2053  lea     ecx, [ebp+70h+var_374]; void *
006E2059  jmp     sub_401E90
006E205E  lea     ecx, [ebp+70h+var_3AC]; void *
006E2064  jmp     sub_401E90
006E2069  lea     ecx, [ebp+70h+var_3E4]; void *
006E206F  jmp     sub_401E90
006E2074  lea     ecx, [ebp+70h+var_41C]; void *
006E207A  jmp     sub_401E90
006E207F  lea     ecx, [ebp+70h+var_454]; void *
006E2085  jmp     sub_401E90
006E208A  lea     ecx, [ebp+70h+var_48C]; void *
006E2090  jmp     sub_401E90
006E2095  lea     ecx, [ebp+70h+var_4C4]; void *
006E209B  jmp     sub_401E90
006E20A0  lea     ecx, [ebp+70h+var_4FC]; void *
006E20A6  jmp     sub_401E90
006E20AB  lea     ecx, [ebp+70h+var_534]; void *
006E20B1  jmp     sub_401E90
006E20B6  lea     ecx, [ebp+70h+var_56C]; void *
006E20BC  jmp     sub_401E90
006E20C1  lea     ecx, [ebp+70h+var_5A4]; void *
006E20C7  jmp     sub_401E90
006E20CC  lea     ecx, [ebp+70h+var_5DC]; void *
006E20D2  jmp     sub_401E90
006E20D7  lea     ecx, [ebp+70h+var_614]; void *
006E20DD  jmp     sub_401E90
006E20E2  lea     ecx, [ebp+70h+var_64C]; void *
006E20E8  jmp     sub_401E90
006E20ED  lea     ecx, [ebp+70h+var_684]; void *
006E20F3  jmp     sub_401E90
006E20F8  lea     ecx, [ebp+70h+var_6BC]; void *
006E20FE  jmp     sub_401E90
006E2103  lea     ecx, [ebp+70h+var_6F4]; void *
006E2109  jmp     sub_401E90
006E210E  lea     ecx, [ebp+70h+var_72C]; void *
006E2114  jmp     sub_401E90
006E2119  lea     ecx, [ebp+70h+var_764]; void *
006E211F  jmp     sub_401E90
006E2124  lea     ecx, [ebp+70h+var_79C]; void *
006E212A  jmp     sub_401E90
006E212F  lea     ecx, [ebp+70h+var_7D4]; void *
006E2135  jmp     sub_401E90
006E213A  lea     ecx, [ebp+70h+var_5F8]; void *
006E2140  jmp     sub_401E90
006E2145  lea     ecx, [ebp+70h+var_1948]; void *
006E214B  jmp     sub_401E90
006E2150  lea     ecx, [ebp+70h+var_35D4]; void *
006E2156  jmp     sub_401E90
006E215B  lea     ecx, [ebp+70h+var_199C]; void *
006E2161  jmp     sub_401E90
006E2166  lea     ecx, [ebp+70h+var_29CC]; void *
006E216C  jmp     sub_401E90
006E2171  lea     ecx, [ebp+70h+var_19D4]; void *
006E2177  jmp     sub_401E90
006E217C  lea     ecx, [ebp+70h+var_31E4]; void *
006E2182  jmp     sub_401E90
006E2187  lea     ecx, [ebp+70h+var_1A0C]; void *
006E218D  jmp     sub_401E90
006E2192  lea     ecx, [ebp+70h+var_2A04]; void *
006E2198  jmp     sub_401E90
006E219D  lea     ecx, [ebp+70h+var_1A44]; void *
006E21A3  jmp     sub_401E90
006E21A8  lea     ecx, [ebp+70h+var_38E4]; void *
006E21AE  jmp     sub_401E90
006E21B3  lea     ecx, [ebp+70h+var_1A7C]; void *
006E21B9  jmp     sub_401E90
006E21BE  lea     ecx, [ebp+70h+var_2A3C]; void *
006E21C4  jmp     sub_401E90
006E21C9  lea     ecx, [ebp+70h+var_1AB4]; void *
006E21CF  jmp     sub_401E90
006E21D4  lea     ecx, [ebp+70h+var_321C]; void *
006E21DA  jmp     sub_401E90
006E21DF  lea     ecx, [ebp+70h+var_1AEC]; void *
006E21E5  jmp     sub_401E90
006E21EA  lea     ecx, [ebp+70h+var_2A74]; void *
006E21F0  jmp     sub_401E90
006E21F5  lea     ecx, [ebp+70h+var_1B24]; void *
006E21FB  jmp     sub_401E90
006E2200  lea     ecx, [ebp+70h+var_360C]; void *
006E2206  jmp     sub_401E90
006E220B  lea     ecx, [ebp+70h+var_1B5C]; void *
006E2211  jmp     sub_401E90
006E2216  lea     ecx, [ebp+70h+var_2AAC]; void *
006E221C  jmp     sub_401E90
006E2221  lea     ecx, [ebp+70h+var_1B94]; void *
006E2227  jmp     sub_401E90
006E222C  lea     ecx, [ebp+70h+var_3254]; void *
006E2232  jmp     sub_401E90
006E2237  lea     ecx, [ebp+70h+var_1BCC]; void *
006E223D  jmp     sub_401E90
006E2242  lea     ecx, [ebp+70h+var_2AE4]; void *
006E2248  jmp     sub_401E90
006E224D  lea     ecx, [ebp+70h+var_1C04]; void *
006E2253  jmp     sub_401E90
006E2258  lea     ecx, [ebp+70h+var_3804]; void *
006E225E  jmp     sub_401E90
006E2263  lea     ecx, [ebp+70h+var_1C3C]; void *
006E2269  jmp     sub_401E90
006E226E  lea     ecx, [ebp+70h+var_2B1C]; void *
006E2274  jmp     sub_401E90
006E2279  lea     ecx, [ebp+70h+var_1C74]; void *
006E227F  jmp     sub_401E90
006E2284  lea     ecx, [ebp+70h+var_328C]; void *
006E228A  jmp     sub_401E90
006E228F  lea     ecx, [ebp+70h+var_1CAC]; void *
006E2295  jmp     sub_401E90
006E229A  lea     ecx, [ebp+70h+var_2B54]; void *
006E22A0  jmp     sub_401E90
006E22A5  lea     ecx, [ebp+70h+var_1CE4]; void *
006E22AB  jmp     sub_401E90
006E22B0  lea     ecx, [ebp+70h+var_3644]; void *
006E22B6  jmp     sub_401E90
006E22BB  lea     ecx, [ebp+70h+var_1D1C]; void *
006E22C1  jmp     sub_401E90
006E22C6  lea     ecx, [ebp+70h+var_2B8C]; void *
006E22CC  jmp     sub_401E90
006E22D1  lea     ecx, [ebp+70h+var_1D54]; void *
006E22D7  jmp     sub_401E90
006E22DC  lea     ecx, [ebp+70h+var_32C4]; void *
006E22E2  jmp     sub_401E90
006E22E7  lea     ecx, [ebp+70h+var_1D8C]; void *
006E22ED  jmp     sub_401E90
006E22F2  lea     ecx, [ebp+70h+var_2BC4]; void *
006E22F8  jmp     sub_401E90
006E22FD  lea     ecx, [ebp+70h+var_1DC4]; void *
006E2303  jmp     sub_401E90
006E2308  lea     ecx, [ebp+70h+var_39C4]; void *
006E230E  jmp     sub_401E90
006E2313  lea     ecx, [ebp+70h+var_1DFC]; void *
006E2319  jmp     sub_401E90
006E231E  lea     ecx, [ebp+70h+var_2BFC]; void *
006E2324  jmp     sub_401E90
006E2329  lea     ecx, [ebp+70h+var_1E34]; void *
006E232F  jmp     sub_401E90
006E2334  lea     ecx, [ebp+70h+var_32FC]; void *
006E233A  jmp     sub_401E90
006E233F  lea     ecx, [ebp+70h+var_1E6C]; void *
006E2345  jmp     sub_401E90
006E234A  lea     ecx, [ebp+70h+var_2C34]; void *
006E2350  jmp     sub_401E90
006E2355  lea     ecx, [ebp+70h+var_1EA4]; void *
006E235B  jmp     sub_401E90
006E2360  lea     ecx, [ebp+70h+var_367C]; void *
006E2366  jmp     sub_401E90
006E236B  lea     ecx, [ebp+70h+var_1EDC]; void *
006E2371  jmp     sub_401E90
006E2376  lea     ecx, [ebp+70h+var_2C6C]; void *
006E237C  jmp     sub_401E90
006E2381  lea     ecx, [ebp+70h+var_1F14]; void *
006E2387  jmp     sub_401E90
006E238C  lea     ecx, [ebp+70h+var_3334]; void *
006E2392  jmp     sub_401E90
006E2397  lea     ecx, [ebp+70h+var_1F4C]; void *
006E239D  jmp     sub_401E90
006E23A2  lea     ecx, [ebp+70h+var_2CA4]; void *
006E23A8  jmp     sub_401E90
006E23AD  lea     ecx, [ebp+70h+var_1F84]; void *
006E23B3  jmp     sub_401E90
006E23B8  lea     ecx, [ebp+70h+var_383C]; void *
006E23BE  jmp     sub_401E90
006E23C3  lea     ecx, [ebp+70h+var_1FBC]; void *
006E23C9  jmp     sub_401E90
006E23CE  lea     ecx, [ebp+70h+var_2CDC]; void *
006E23D4  jmp     sub_401E90
006E23D9  lea     ecx, [ebp+70h+var_1FF4]; void *
006E23DF  jmp     sub_401E90
006E23E4  lea     ecx, [ebp+70h+var_336C]; void *
006E23EA  jmp     sub_401E90
006E23EF  lea     ecx, [ebp+70h+var_202C]; void *
006E23F5  jmp     sub_401E90
006E23FA  lea     ecx, [ebp+70h+var_2D14]; void *
006E2400  jmp     sub_401E90
006E2405  lea     ecx, [ebp+70h+var_2064]; void *
006E240B  jmp     sub_401E90
006E2410  lea     ecx, [ebp+70h+var_36B4]; void *
006E2416  jmp     sub_401E90
006E241B  lea     ecx, [ebp+70h+var_209C]; void *
006E2421  jmp     sub_401E90
006E2426  lea     ecx, [ebp+70h+var_2D4C]; void *
006E242C  jmp     sub_401E90
006E2431  lea     ecx, [ebp+70h+var_20D4]; void *
006E2437  jmp     sub_401E90
006E243C  lea     ecx, [ebp+70h+var_33A4]; void *
006E2442  jmp     sub_401E90
006E2447  lea     ecx, [ebp+70h+var_210C]; void *
006E244D  jmp     sub_401E90
006E2452  lea     ecx, [ebp+70h+var_2D84]; void *
006E2458  jmp     sub_401E90
006E245D  lea     ecx, [ebp+70h+var_2144]; void *
006E2463  jmp     sub_401E90
006E2468  lea     ecx, [ebp+70h+var_391C]; void *
006E246E  jmp     sub_401E90
006E2473  lea     ecx, [ebp+70h+var_217C]; void *
006E2479  jmp     sub_401E90
006E247E  lea     ecx, [ebp+70h+var_2DBC]; void *
006E2484  jmp     sub_401E90
006E2489  lea     ecx, [ebp+70h+var_21B4]; void *
006E248F  jmp     sub_401E90
006E2494  lea     ecx, [ebp+70h+var_33DC]; void *
006E249A  jmp     sub_401E90
006E249F  lea     ecx, [ebp+70h+var_21EC]; void *
006E24A5  jmp     sub_401E90
006E24AA  lea     ecx, [ebp+70h+var_2DF4]; void *
006E24B0  jmp     sub_401E90
006E24B5  lea     ecx, [ebp+70h+var_2224]; void *
006E24BB  jmp     sub_401E90
006E24C0  lea     ecx, [ebp+70h+var_36EC]; void *
006E24C6  jmp     sub_401E90
006E24CB  lea     ecx, [ebp+70h+var_225C]; void *
006E24D1  jmp     sub_401E90
006E24D6  lea     ecx, [ebp+70h+var_2E2C]; void *
006E24DC  jmp     sub_401E90
006E24E1  lea     ecx, [ebp+70h+var_2294]; void *
006E24E7  jmp     sub_401E90
006E24EC  lea     ecx, [ebp+70h+var_3414]; void *
006E24F2  jmp     sub_401E90
006E24F7  lea     ecx, [ebp+70h+var_22CC]; void *
006E24FD  jmp     sub_401E90
006E2502  lea     ecx, [ebp+70h+var_2E64]; void *
006E2508  jmp     sub_401E90
006E250D  lea     ecx, [ebp+70h+var_2304]; void *
006E2513  jmp     sub_401E90
006E2518  lea     ecx, [ebp+70h+var_3874]; void *
006E251E  jmp     sub_401E90
006E2523  lea     ecx, [ebp+70h+var_233C]; void *
006E2529  jmp     sub_401E90
006E252E  lea     ecx, [ebp+70h+var_2E9C]; void *
006E2534  jmp     sub_401E90
006E2539  lea     ecx, [ebp+70h+var_2374]; void *
006E253F  jmp     sub_401E90
006E2544  lea     ecx, [ebp+70h+var_344C]; void *
006E254A  jmp     sub_401E90
006E254F  lea     ecx, [ebp+70h+var_23AC]; void *
006E2555  jmp     sub_401E90
006E255A  lea     ecx, [ebp+70h+var_2ED4]; void *
006E2560  jmp     sub_401E90
006E2565  lea     ecx, [ebp+70h+var_23E4]; void *
006E256B  jmp     sub_401E90
006E2570  lea     ecx, [ebp+70h+var_3724]; void *
006E2576  jmp     sub_401E90
006E257B  lea     ecx, [ebp+70h+var_241C]; void *
006E2581  jmp     sub_401E90
006E2586  lea     ecx, [ebp+70h+var_2F0C]; void *
006E258C  jmp     sub_401E90
006E2591  lea     ecx, [ebp+70h+var_2454]; void *
006E2597  jmp     sub_401E90
006E259C  lea     ecx, [ebp+70h+var_3484]; void *
006E25A2  jmp     sub_401E90
006E25A7  lea     ecx, [ebp+70h+var_248C]; void *
006E25AD  jmp     sub_401E90
006E25B2  lea     ecx, [ebp+70h+var_2F44]; void *
006E25B8  jmp     sub_401E90
006E25BD  lea     ecx, [ebp+70h+var_24C4]; void *
006E25C3  jmp     sub_401E90
006E25C8  lea     ecx, [ebp+70h+var_398C]; void *
006E25CE  jmp     sub_401E90
006E25D3  lea     ecx, [ebp+70h+var_24FC]; void *
006E25D9  jmp     sub_401E90
006E25DE  lea     ecx, [ebp+70h+var_2F7C]; void *
006E25E4  jmp     sub_401E90
006E25E9  lea     ecx, [ebp+70h+var_2534]; void *
006E25EF  jmp     sub_401E90
006E25F4  lea     ecx, [ebp+70h+var_34BC]; void *
006E25FA  jmp     sub_401E90
006E25FF  lea     ecx, [ebp+70h+var_256C]; void *
006E2605  jmp     sub_401E90
006E260A  lea     ecx, [ebp+70h+var_2FB4]; void *
006E2610  jmp     sub_401E90
006E2615  lea     ecx, [ebp+70h+var_25A4]; void *
006E261B  jmp     sub_401E90
006E2620  lea     ecx, [ebp+70h+var_375C]; void *
006E2626  jmp     sub_401E90
006E262B  lea     ecx, [ebp+70h+var_25DC]; void *
006E2631  jmp     sub_401E90
006E2636  lea     ecx, [ebp+70h+var_2FEC]; void *
006E263C  jmp     sub_401E90
006E2641  lea     ecx, [ebp+70h+var_2614]; void *
006E2647  jmp     sub_401E90
006E264C  lea     ecx, [ebp+70h+var_34F4]; void *
006E2652  jmp     sub_401E90
006E2657  lea     ecx, [ebp+70h+var_264C]; void *
006E265D  jmp     sub_401E90
006E2662  lea     ecx, [ebp+70h+var_3024]; void *
006E2668  jmp     sub_401E90
006E266D  lea     ecx, [ebp+70h+var_2684]; void *
006E2673  jmp     sub_401E90
006E2678  lea     ecx, [ebp+70h+var_38AC]; void *
006E267E  jmp     sub_401E90
006E2683  lea     ecx, [ebp+70h+var_26BC]; void *
006E2689  jmp     sub_401E90
006E268E  lea     ecx, [ebp+70h+var_305C]; void *
006E2694  jmp     sub_401E90
006E2699  lea     ecx, [ebp+70h+var_26F4]; void *
006E269F  jmp     sub_401E90
006E26A4  lea     ecx, [ebp+70h+var_352C]; void *
006E26AA  jmp     sub_401E90
006E26AF  lea     ecx, [ebp+70h+var_272C]; void *
006E26B5  jmp     sub_401E90
006E26BA  lea     ecx, [ebp+70h+var_3094]; void *
006E26C0  jmp     sub_401E90
006E26C5  lea     ecx, [ebp+70h+var_2764]; void *
006E26CB  jmp     sub_401E90
006E26D0  lea     ecx, [ebp+70h+var_3794]; void *
006E26D6  jmp     sub_401E90
006E26DB  lea     ecx, [ebp+70h+var_279C]; void *
006E26E1  jmp     sub_401E90
006E26E6  lea     ecx, [ebp+70h+var_30CC]; void *
006E26EC  jmp     sub_401E90
006E26F1  lea     ecx, [ebp+70h+var_27D4]; void *
006E26F7  jmp     sub_401E90
006E26FC  lea     ecx, [ebp+70h+var_3564]; void *
006E2702  jmp     sub_401E90
006E2707  lea     ecx, [ebp+70h+var_280C]; void *
006E270D  jmp     sub_401E90
006E2712  lea     ecx, [ebp+70h+var_3104]; void *
006E2718  jmp     sub_401E90
006E271D  lea     ecx, [ebp+70h+var_2844]; void *
006E2723  jmp     sub_401E90
006E2728  lea     ecx, [ebp+70h+var_3954]; void *
006E272E  jmp     sub_401E90
006E2733  lea     ecx, [ebp+70h+var_287C]; void *
006E2739  jmp     sub_401E90
006E273E  lea     ecx, [ebp+70h+var_313C]; void *
006E2744  jmp     sub_401E90
006E2749  lea     ecx, [ebp+70h+var_28B4]; void *
006E274F  jmp     sub_401E90
006E2754  lea     ecx, [ebp+70h+var_359C]; void *
006E275A  jmp     sub_401E90
006E275F  lea     ecx, [ebp+70h+var_28EC]; void *
006E2765  jmp     sub_401E90
006E276A  lea     ecx, [ebp+70h+var_3174]; void *
006E2770  jmp     sub_401E90
006E2775  lea     ecx, [ebp+70h+var_2924]; void *
006E277B  jmp     sub_401E90
006E2780  lea     ecx, [ebp+70h+var_37CC]; void *
006E2786  jmp     sub_401E90
006E278B  lea     ecx, [ebp+70h+var_295C]; void *
006E2791  jmp     sub_401E90
006E2796  lea     ecx, [ebp+70h+var_31AC]; void *
006E279C  jmp     sub_401E90
006E27A1  lea     ecx, [ebp+70h+var_2994]; void *
006E27A7  jmp     sub_401E90
006E27AC  lea     ecx, [ebp+70h+var_1964]; void *
006E27B2  jmp     sub_401E90
006E27B7  lea     ecx, [ebp+70h+var_1980]; void *
006E27BD  jmp     sub_401E90
006E27C2  lea     ecx, [ebp+70h+var_19B8]; void *
006E27C8  jmp     sub_401E90
006E27CD  lea     ecx, [ebp+70h+var_19F0]; void *
006E27D3  jmp     sub_401E90
006E27D8  lea     ecx, [ebp+70h+var_1A28]; void *
006E27DE  jmp     sub_401E90
006E27E3  lea     ecx, [ebp+70h+var_1A60]; void *
006E27E9  jmp     sub_401E90
006E27EE  lea     ecx, [ebp+70h+var_1A98]; void *
006E27F4  jmp     sub_401E90
006E27F9  lea     ecx, [ebp+70h+var_1AD0]; void *
006E27FF  jmp     sub_401E90
006E2804  lea     ecx, [ebp+70h+var_1B08]; void *
006E280A  jmp     sub_401E90
006E280F  lea     ecx, [ebp+70h+var_1B40]; void *
006E2815  jmp     sub_401E90
006E281A  lea     ecx, [ebp+70h+var_1B78]; void *
006E2820  jmp     sub_401E90
006E2825  lea     ecx, [ebp+70h+var_1BB0]; void *
006E282B  jmp     sub_401E90
006E2830  lea     ecx, [ebp+70h+var_1BE8]; void *
006E2836  jmp     sub_401E90
006E283B  lea     ecx, [ebp+70h+var_1C20]; void *
006E2841  jmp     sub_401E90
006E2846  lea     ecx, [ebp+70h+var_1C58]; void *
006E284C  jmp     sub_401E90
006E2851  lea     ecx, [ebp+70h+var_1C90]; void *
006E2857  jmp     sub_401E90
006E285C  lea     ecx, [ebp+70h+var_1CC8]; void *
006E2862  jmp     sub_401E90
006E2867  lea     ecx, [ebp+70h+var_1D00]; void *
006E286D  jmp     sub_401E90
006E2872  lea     ecx, [ebp+70h+var_1D38]; void *
006E2878  jmp     sub_401E90
006E287D  lea     ecx, [ebp+70h+var_1D70]; void *
006E2883  jmp     sub_401E90
006E2888  lea     ecx, [ebp+70h+var_1DA8]; void *
006E288E  jmp     sub_401E90
006E2893  lea     ecx, [ebp+70h+var_1DE0]; void *
006E2899  jmp     sub_401E90
006E289E  lea     ecx, [ebp+70h+var_1E18]; void *
006E28A4  jmp     sub_401E90
006E28A9  lea     ecx, [ebp+70h+var_1E50]; void *
006E28AF  jmp     sub_401E90
006E28B4  lea     ecx, [ebp+70h+var_1E88]; void *
006E28BA  jmp     sub_401E90
006E28BF  lea     ecx, [ebp+70h+var_1EC0]; void *
006E28C5  jmp     sub_401E90
006E28CA  lea     ecx, [ebp+70h+var_1EF8]; void *
006E28D0  jmp     sub_401E90
006E28D5  lea     ecx, [ebp+70h+var_1F30]; void *
006E28DB  jmp     sub_401E90
006E28E0  lea     ecx, [ebp+70h+var_1F68]; void *
006E28E6  jmp     sub_401E90
006E28EB  lea     ecx, [ebp+70h+var_1FA0]; void *
006E28F1  jmp     sub_401E90
006E28F6  lea     ecx, [ebp+70h+var_1FD8]; void *
006E28FC  jmp     sub_401E90
006E2901  lea     ecx, [ebp+70h+var_2010]; void *
006E2907  jmp     sub_401E90
006E290C  lea     ecx, [ebp+70h+var_2048]; void *
006E2912  jmp     sub_401E90
006E2917  lea     ecx, [ebp+70h+var_2080]; void *
006E291D  jmp     sub_401E90
006E2922  lea     ecx, [ebp+70h+var_20B8]; void *
006E2928  jmp     sub_401E90
006E292D  lea     ecx, [ebp+70h+var_20F0]; void *
006E2933  jmp     sub_401E90
006E2938  lea     ecx, [ebp+70h+var_2128]; void *
006E293E  jmp     sub_401E90
006E2943  lea     ecx, [ebp+70h+var_2160]; void *
006E2949  jmp     sub_401E90
006E294E  lea     ecx, [ebp+70h+var_2198]; void *
006E2954  jmp     sub_401E90
006E2959  lea     ecx, [ebp+70h+var_21D0]; void *
006E295F  jmp     sub_401E90
006E2964  lea     ecx, [ebp+70h+var_2208]; void *
006E296A  jmp     sub_401E90
006E296F  lea     ecx, [ebp+70h+var_2240]; void *
006E2975  jmp     sub_401E90
006E297A  lea     ecx, [ebp+70h+var_2278]; void *
006E2980  jmp     sub_401E90
006E2985  lea     ecx, [ebp+70h+var_22B0]; void *
006E298B  jmp     sub_401E90
006E2990  lea     ecx, [ebp+70h+var_22E8]; void *
006E2996  jmp     sub_401E90
006E299B  lea     ecx, [ebp+70h+var_2320]; void *
006E29A1  jmp     sub_401E90
006E29A6  lea     ecx, [ebp+70h+var_2358]; void *
006E29AC  jmp     sub_401E90
006E29B1  lea     ecx, [ebp+70h+var_2390]; void *
006E29B7  jmp     sub_401E90
006E29BC  lea     ecx, [ebp+70h+var_23C8]; void *
006E29C2  jmp     sub_401E90
006E29C7  lea     ecx, [ebp+70h+var_2400]; void *
006E29CD  jmp     sub_401E90
006E29D2  lea     ecx, [ebp+70h+var_2438]; void *
006E29D8  jmp     sub_401E90
006E29DD  lea     ecx, [ebp+70h+var_2470]; void *
006E29E3  jmp     sub_401E90
006E29E8  lea     ecx, [ebp+70h+var_24A8]; void *
006E29EE  jmp     sub_401E90
006E29F3  lea     ecx, [ebp+70h+var_24E0]; void *
006E29F9  jmp     sub_401E90
006E29FE  lea     ecx, [ebp+70h+var_2518]; void *
006E2A04  jmp     sub_401E90
006E2A09  lea     ecx, [ebp+70h+var_2550]; void *
006E2A0F  jmp     sub_401E90
006E2A14  lea     ecx, [ebp+70h+var_2588]; void *
006E2A1A  jmp     sub_401E90
006E2A1F  lea     ecx, [ebp+70h+var_25C0]; void *
006E2A25  jmp     sub_401E90
006E2A2A  lea     ecx, [ebp+70h+var_25F8]; void *
006E2A30  jmp     sub_401E90
006E2A35  lea     ecx, [ebp+70h+var_2630]; void *
006E2A3B  jmp     sub_401E90
006E2A40  lea     ecx, [ebp+70h+var_2668]; void *
006E2A46  jmp     sub_401E90
006E2A4B  lea     ecx, [ebp+70h+var_26A0]; void *
006E2A51  jmp     sub_401E90
006E2A56  lea     ecx, [ebp+70h+var_26D8]; void *
006E2A5C  jmp     sub_401E90
006E2A61  lea     ecx, [ebp+70h+var_2710]; void *
006E2A67  jmp     sub_401E90
006E2A6C  lea     ecx, [ebp+70h+var_2748]; void *
006E2A72  jmp     sub_401E90
006E2A77  lea     ecx, [ebp+70h+var_2780]; void *
006E2A7D  jmp     sub_401E90
006E2A82  lea     ecx, [ebp+70h+var_27B8]; void *
006E2A88  jmp     sub_401E90
006E2A8D  lea     ecx, [ebp+70h+var_27F0]; void *
006E2A93  jmp     sub_401E90
006E2A98  lea     ecx, [ebp+70h+var_2828]; void *
006E2A9E  jmp     sub_401E90
006E2AA3  lea     ecx, [ebp+70h+var_2860]; void *
006E2AA9  jmp     sub_401E90
006E2AAE  lea     ecx, [ebp+70h+var_2898]; void *
006E2AB4  jmp     sub_401E90
006E2AB9  lea     ecx, [ebp+70h+var_28D0]; void *
006E2ABF  jmp     sub_401E90
006E2AC4  lea     ecx, [ebp+70h+var_2908]; void *
006E2ACA  jmp     sub_401E90
006E2ACF  lea     ecx, [ebp+70h+var_2940]; void *
006E2AD5  jmp     sub_401E90
006E2ADA  lea     ecx, [ebp+70h+var_2978]; void *
006E2AE0  jmp     sub_401E90
006E2AE5  lea     ecx, [ebp+70h+var_29B0]; void *
006E2AEB  jmp     sub_401E90
006E2AF0  lea     ecx, [ebp+70h+var_29E8]; void *
006E2AF6  jmp     sub_401E90
006E2AFB  lea     ecx, [ebp+70h+var_2A20]; void *
006E2B01  jmp     sub_401E90
006E2B06  lea     ecx, [ebp+70h+var_2A58]; void *
006E2B0C  jmp     sub_401E90
006E2B11  lea     ecx, [ebp+70h+var_2A90]; void *
006E2B17  jmp     sub_401E90
006E2B1C  lea     ecx, [ebp+70h+var_2AC8]; void *
006E2B22  jmp     sub_401E90
006E2B27  lea     ecx, [ebp+70h+var_2B00]; void *
006E2B2D  jmp     sub_401E90
006E2B32  lea     ecx, [ebp+70h+var_2B38]; void *
006E2B38  jmp     sub_401E90
006E2B3D  lea     ecx, [ebp+70h+var_2B70]; void *
006E2B43  jmp     sub_401E90
006E2B48  lea     ecx, [ebp+70h+var_2BA8]; void *
006E2B4E  jmp     sub_401E90
006E2B53  lea     ecx, [ebp+70h+var_2BE0]; void *
006E2B59  jmp     sub_401E90
006E2B5E  lea     ecx, [ebp+70h+var_2C18]; void *
006E2B64  jmp     sub_401E90
006E2B69  lea     ecx, [ebp+70h+var_2C50]; void *
006E2B6F  jmp     sub_401E90
006E2B74  lea     ecx, [ebp+70h+var_2C88]; void *
006E2B7A  jmp     sub_401E90
006E2B7F  lea     ecx, [ebp+70h+var_2CC0]; void *
006E2B85  jmp     sub_401E90
006E2B8A  lea     ecx, [ebp+70h+var_2CF8]; void *
006E2B90  jmp     sub_401E90
006E2B95  lea     ecx, [ebp+70h+var_2D30]; void *
006E2B9B  jmp     sub_401E90
006E2BA0  lea     ecx, [ebp+70h+var_2D68]; void *
006E2BA6  jmp     sub_401E90
006E2BAB  lea     ecx, [ebp+70h+var_2DA0]; void *
006E2BB1  jmp     sub_401E90
006E2BB6  lea     ecx, [ebp+70h+var_2DD8]; void *
006E2BBC  jmp     sub_401E90
006E2BC1  lea     ecx, [ebp+70h+var_2E10]; void *
006E2BC7  jmp     sub_401E90
006E2BCC  lea     ecx, [ebp+70h+var_2E48]; void *
006E2BD2  jmp     sub_401E90
006E2BD7  lea     ecx, [ebp+70h+var_2E80]; void *
006E2BDD  jmp     sub_401E90
006E2BE2  lea     ecx, [ebp+70h+var_2EB8]; void *
006E2BE8  jmp     sub_401E90
006E2BED  lea     ecx, [ebp+70h+var_2EF0]; void *
006E2BF3  jmp     sub_401E90
006E2BF8  lea     ecx, [ebp+70h+var_2F28]; void *
006E2BFE  jmp     sub_401E90
006E2C03  lea     ecx, [ebp+70h+var_2F60]; void *
006E2C09  jmp     sub_401E90
006E2C0E  lea     ecx, [ebp+70h+var_2F98]; void *
006E2C14  jmp     sub_401E90
006E2C19  lea     ecx, [ebp+70h+var_2FD0]; void *
006E2C1F  jmp     sub_401E90
006E2C24  lea     ecx, [ebp+70h+var_3008]; void *
006E2C2A  jmp     sub_401E90
006E2C2F  lea     ecx, [ebp+70h+var_3040]; void *
006E2C35  jmp     sub_401E90
006E2C3A  lea     ecx, [ebp+70h+var_3078]; void *
006E2C40  jmp     sub_401E90
006E2C45  lea     ecx, [ebp+70h+var_30B0]; void *
006E2C4B  jmp     sub_401E90
006E2C50  lea     ecx, [ebp+70h+var_30E8]; void *
006E2C56  jmp     sub_401E90
006E2C5B  lea     ecx, [ebp+70h+var_3120]; void *
006E2C61  jmp     sub_401E90
006E2C66  lea     ecx, [ebp+70h+var_3158]; void *
006E2C6C  jmp     sub_401E90
006E2C71  lea     ecx, [ebp+70h+var_3190]; void *
006E2C77  jmp     sub_401E90
006E2C7C  lea     ecx, [ebp+70h+var_31C8]; void *
006E2C82  jmp     sub_401E90
006E2C87  lea     ecx, [ebp+70h+var_3200]; void *
006E2C8D  jmp     sub_401E90
006E2C92  lea     ecx, [ebp+70h+var_3238]; void *
006E2C98  jmp     sub_401E90
006E2C9D  lea     ecx, [ebp+70h+var_3270]; void *
006E2CA3  jmp     sub_401E90
006E2CA8  lea     ecx, [ebp+70h+var_32A8]; void *
006E2CAE  jmp     sub_401E90
006E2CB3  lea     ecx, [ebp+70h+var_32E0]; void *
006E2CB9  jmp     sub_401E90
006E2CBE  lea     ecx, [ebp+70h+var_3318]; void *
006E2CC4  jmp     sub_401E90
006E2CC9  lea     ecx, [ebp+70h+var_3350]; void *
006E2CCF  jmp     sub_401E90
006E2CD4  lea     ecx, [ebp+70h+var_3388]; void *
006E2CDA  jmp     sub_401E90
006E2CDF  lea     ecx, [ebp+70h+var_33C0]; void *
006E2CE5  jmp     sub_401E90
006E2CEA  lea     ecx, [ebp+70h+var_33F8]; void *
006E2CF0  jmp     sub_401E90
006E2CF5  lea     ecx, [ebp+70h+var_3430]; void *
006E2CFB  jmp     sub_401E90
006E2D00  lea     ecx, [ebp+70h+var_3468]; void *
006E2D06  jmp     sub_401E90
006E2D0B  lea     ecx, [ebp+70h+var_34A0]; void *
006E2D11  jmp     sub_401E90
006E2D16  lea     ecx, [ebp+70h+var_34D8]; void *
006E2D1C  jmp     sub_401E90
006E2D21  lea     ecx, [ebp+70h+var_3510]; void *
006E2D27  jmp     sub_401E90
006E2D2C  lea     ecx, [ebp+70h+var_3548]; void *
006E2D32  jmp     sub_401E90
006E2D37  lea     ecx, [ebp+70h+var_3580]; void *
006E2D3D  jmp     sub_401E90
006E2D42  lea     ecx, [ebp+70h+var_35B8]; void *
006E2D48  jmp     sub_401E90
006E2D4D  lea     ecx, [ebp+70h+var_35F0]; void *
006E2D53  jmp     sub_401E90
006E2D58  lea     ecx, [ebp+70h+var_3628]; void *
006E2D5E  jmp     sub_401E90
006E2D63  lea     ecx, [ebp+70h+var_3660]; void *
006E2D69  jmp     sub_401E90
006E2D6E  lea     ecx, [ebp+70h+var_3698]; void *
006E2D74  jmp     sub_401E90
006E2D79  lea     ecx, [ebp+70h+var_36D0]; void *
006E2D7F  jmp     sub_401E90
006E2D84  lea     ecx, [ebp+70h+var_3708]; void *
006E2D8A  jmp     sub_401E90
006E2D8F  lea     ecx, [ebp+70h+var_3740]; void *
006E2D95  jmp     sub_401E90
006E2D9A  lea     ecx, [ebp+70h+var_3778]; void *
006E2DA0  jmp     sub_401E90
006E2DA5  lea     ecx, [ebp+70h+var_37B0]; void *
006E2DAB  jmp     sub_401E90
006E2DB0  lea     ecx, [ebp+70h+var_37E8]; void *
006E2DB6  jmp     sub_401E90
006E2DBB  lea     ecx, [ebp+70h+var_3820]; void *
006E2DC1  jmp     sub_401E90
006E2DC6  lea     ecx, [ebp+70h+var_3858]; void *
006E2DCC  jmp     sub_401E90
006E2DD1  lea     ecx, [ebp+70h+var_3890]; void *
006E2DD7  jmp     sub_401E90
006E2DDC  lea     ecx, [ebp+70h+var_38C8]; void *
006E2DE2  jmp     sub_401E90
006E2DE7  lea     ecx, [ebp+70h+var_3900]; void *
006E2DED  jmp     sub_401E90
006E2DF2  lea     ecx, [ebp+70h+var_3938]; void *
006E2DF8  jmp     sub_401E90
006E2DFD  lea     ecx, [ebp+70h+var_3970]; void *
006E2E03  jmp     sub_401E90
006E2E08  lea     ecx, [ebp+70h+var_39A8]; void *
006E2E0E  jmp     sub_401E90
006E2E13  lea     ecx, [ebp+70h+var_39E0]; void *
006E2E19  jmp     sub_401E90
006E2E1E  mov     eax, offset stru_7352E4
006E2E23  jmp     ___CxxFrameHandler3
