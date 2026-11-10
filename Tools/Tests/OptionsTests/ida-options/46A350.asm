0046A350  push    0FFFFFFFFh
0046A352  push    offset SEH_46A350
0046A357  mov     eax, large fs:0
0046A35D  push    eax
0046A35E  mov     large fs:0, esp
0046A365  sub     esp, 74h
0046A368  push    ebx
0046A369  push    ebp
0046A36A  push    esi
0046A36B  mov     esi, [esp+8Ch+arg_0]
0046A372  push    edi
0046A373  xor     ebp, ebp
0046A375  push    ebp; int
0046A376  xor     eax, eax
0046A378  mov     ebx, 7
0046A37D  push    offset unk_708460; Source
0046A382  lea     ecx, [esp+98h+var_28]
0046A386  mov     [esp+98h+var_10], ebx
0046A38D  mov     [esp+98h+var_14], ebp
0046A394  mov     word ptr [esp+98h+var_24], ax
0046A399  call    sub_40D1E0
0046A39E  mov     [esp+90h+var_4], ebp
0046A3A5  xor     ecx, ecx
0046A3A7  push    ebp; int
0046A3A8  mov     word ptr [esp+94h+var_40], cx
0046A3AD  push    offset unk_708460; Source
0046A3B2  lea     ecx, [esp+98h+var_44]
0046A3B6  mov     [esp+98h+var_2C], ebx
0046A3BA  mov     [esp+98h+var_30], ebp
0046A3BE  call    sub_40D1E0
0046A3C3  mov     byte ptr [esp+90h+var_4], 1
0046A3CB  push    ebx; int
0046A3CC  xor     edx, edx
0046A3CE  push    offset off_71128C; Source
0046A3D3  lea     ecx, [esp+98h+var_60]
0046A3D7  mov     [esp+98h+var_48], ebx
0046A3DB  mov     [esp+98h+var_4C], ebp
0046A3DF  mov     word ptr [esp+98h+var_5C], dx
0046A3E4  call    sub_40D1E0
0046A3E9  push    ebp
0046A3EA  lea     eax, [esp+94h+var_28]
0046A3EE  push    eax
0046A3EF  lea     ecx, [esp+98h+var_44]
0046A3F3  push    ecx
0046A3F4  lea     edx, [esp+9Ch+var_60]
0046A3F8  push    edx
0046A3F9  push    1
0046A3FB  push    2
0046A3FD  push    ebp
0046A3FE  push    ebp
0046A3FF  mov     ecx, esi
0046A401  mov     byte ptr [esp+0B0h+var_4], 2
0046A409  call    sub_5CF5B0
0046A40E  mov     edi, 8
0046A413  cmp     [esp+90h+var_48], edi
0046A417  jb      short loc_46A426
0046A419  mov     eax, [esp+90h+var_5C]
0046A41D  push    eax; void *
0046A41E  call    ??3@YAXPAX@Z; operator delete(void *)
0046A423  add     esp, 4
0046A426  xor     ecx, ecx
0046A428  mov     [esp+90h+var_48], ebx
0046A42C  mov     [esp+90h+var_4C], ebp
0046A430  mov     word ptr [esp+90h+var_5C], cx
0046A435  cmp     [esp+90h+var_2C], edi
0046A439  jb      short loc_46A448
0046A43B  mov     edx, [esp+90h+var_40]
0046A43F  push    edx; void *
0046A440  call    ??3@YAXPAX@Z; operator delete(void *)
0046A445  add     esp, 4
0046A448  xor     eax, eax
0046A44A  mov     [esp+90h+var_2C], ebx
0046A44E  mov     [esp+90h+var_30], ebp
0046A452  mov     word ptr [esp+90h+var_40], ax
0046A457  mov     byte ptr [esp+90h+var_4], 6
0046A45F  cmp     [esp+90h+var_10], edi
0046A466  jb      short loc_46A475
0046A468  mov     ecx, [esp+90h+var_24]
0046A46C  push    ecx; void *
0046A46D  call    ??3@YAXPAX@Z; operator delete(void *)
0046A472  add     esp, 4
0046A475  mov     cl, [esp+90h+arg_8]
0046A47C  mov     eax, [esp+90h+arg_4]
0046A483  mov     dword ptr [esi+168h], offset off_708968
0046A48D  mov     dword ptr [esi+16Ch], offset off_711628
0046A497  push    64h ; 'd'
0046A499  push    0FFh
0046A49E  xor     edx, edx
0046A4A0  mov     [esi+194h], cl
0046A4A6  lea     edi, [esi+0A0h]
0046A4AC  push    0FFh
0046A4B1  lea     ecx, [esp+9Ch+var_7C]
0046A4B5  mov     [esp+9Ch+var_10], ebx
0046A4BC  mov     [esp+9Ch+var_14], ebp
0046A4C3  mov     word ptr [esp+9Ch+var_24], dx
0046A4C8  mov     dword ptr [esi], offset off_711630
0046A4CE  mov     dword ptr [edi], offset off_711760
0046A4D4  mov     dword ptr [esi+168h], offset off_71177C
0046A4DE  mov     dword ptr [esi+16Ch], offset off_711780
0046A4E8  mov     [esi+170h], eax
0046A4EE  call    sub_55BBB0
0046A4F3  push    eax
0046A4F4  push    3
0046A4F6  mov     ecx, esi
0046A4F8  call    sub_5CFEB0
0046A4FD  push    15h; int
0046A4FF  xor     edx, edx
0046A501  push    offset off_708B58; Source
0046A506  lea     ecx, [esp+98h+var_7C]
0046A50A  mov     [esp+98h+var_64], ebx
0046A50E  mov     [esp+98h+var_68], ebp
0046A512  mov     word ptr [esp+98h+var_78], dx
0046A517  call    sub_40D1E0
0046A51C  lea     eax, [esp+90h+var_7C]
0046A520  push    eax
0046A521  push    edi
0046A522  push    ebp
0046A523  mov     byte ptr [esp+9Ch+var_4], bl
0046A52A  call    sub_453310
0046A52F  add     esp, 0Ch
0046A532  mov     byte ptr [esp+90h+var_4], 6
0046A53A  cmp     [esp+90h+var_64], 8
0046A53F  mov     [esi+184h], eax
0046A545  jb      short loc_46A554
0046A547  mov     ecx, [esp+90h+var_78]
0046A54B  push    ecx; void *
0046A54C  call    ??3@YAXPAX@Z; operator delete(void *)
0046A551  add     esp, 4
0046A554  push    16h; int
0046A556  xor     edx, edx
0046A558  push    offset off_70C874; Source
0046A55D  lea     ecx, [esp+98h+var_7C]
0046A561  mov     [esp+98h+var_64], ebx
0046A565  mov     [esp+98h+var_68], ebp
0046A569  mov     word ptr [esp+98h+var_78], dx
0046A56E  call    sub_40D1E0
0046A573  lea     eax, [esp+90h+var_7C]
0046A577  push    eax
0046A578  push    edi
0046A579  push    2
0046A57B  mov     byte ptr [esp+9Ch+var_4], 8
0046A583  call    sub_453310
0046A588  add     esp, 0Ch
0046A58B  mov     byte ptr [esp+90h+var_4], 6
0046A593  cmp     [esp+90h+var_64], 8
0046A598  mov     [esi+18Ch], eax
0046A59E  jb      short loc_46A5AD
0046A5A0  mov     ecx, [esp+90h+var_78]
0046A5A4  push    ecx; void *
0046A5A5  call    ??3@YAXPAX@Z; operator delete(void *)
0046A5AA  add     esp, 4
0046A5AD  push    12h; int
0046A5AF  xor     edx, edx
0046A5B1  push    offset off_708B0C; Source
0046A5B6  lea     ecx, [esp+98h+var_7C]
0046A5BA  mov     [esp+98h+var_64], ebx
0046A5BE  mov     [esp+98h+var_68], ebp
0046A5C2  mov     word ptr [esp+98h+var_78], dx
0046A5C7  call    sub_40D1E0
0046A5CC  lea     eax, [esp+90h+var_7C]
0046A5D0  push    eax
0046A5D1  push    edi
0046A5D2  push    1
0046A5D4  mov     byte ptr [esp+9Ch+var_4], 9
0046A5DC  call    sub_453310
0046A5E1  add     esp, 0Ch
0046A5E4  mov     byte ptr [esp+90h+var_4], 6
0046A5EC  cmp     [esp+90h+var_64], 8
0046A5F1  mov     [esi+188h], eax
0046A5F7  jb      short loc_46A606
0046A5F9  mov     ecx, [esp+90h+var_78]
0046A5FD  push    ecx; void *
0046A5FE  call    ??3@YAXPAX@Z; operator delete(void *)
0046A603  add     esp, 4
0046A606  push    0Eh; int
0046A608  xor     edx, edx
0046A60A  push    offset aBackToGame; "[BACK_TO_GAME]"
0046A60F  lea     ecx, [esp+98h+var_7C]
0046A613  mov     [esp+98h+var_64], ebx
0046A617  mov     [esp+98h+var_68], ebp
0046A61B  mov     word ptr [esp+98h+var_78], dx
0046A620  call    sub_40D1E0
0046A625  mov     byte ptr [esp+90h+var_4], 0Ah
0046A62D  mov     eax, dword_753094
0046A632  mov     ebx, dword_753454
0046A638  push    eax
0046A639  push    ebp
0046A63A  lea     ecx, [esp+98h+var_7C]
0046A63E  push    ecx
0046A63F  push    edi
0046A640  push    3E8h
0046A645  mov     edi, eax
0046A647  call    sub_453910
0046A64C  add     esp, 14h
0046A64F  mov     byte ptr [esp+90h+var_4], 6
0046A657  cmp     [esp+90h+var_64], 8
0046A65C  mov     [esi+190h], eax
0046A662  jb      short loc_46A671
0046A664  mov     edx, [esp+90h+var_78]
0046A668  push    edx; void *
0046A669  call    ??3@YAXPAX@Z; operator delete(void *)
0046A66E  add     esp, 4
0046A671  mov     eax, [esi+190h]
0046A677  xor     ebx, ebx
0046A679  mov     [eax+13Ch], ebx
0046A67F  mov     ecx, [esi+190h]
0046A685  mov     [ecx+140h], ebx
0046A68B  mov     edx, [esi+190h]
0046A691  mov     dword ptr [edx+144h], 0FFFFFFFEh
0046A69B  mov     eax, [esi+190h]
0046A6A1  mov     dword ptr [eax+148h], 0FFFFFFFBh
0046A6AB  mov     ecx, [esi+190h]
0046A6B1  mov     [ecx+154h], ebx
0046A6B7  mov     edx, [esi+190h]
0046A6BD  mov     dword ptr [edx+158h], 1
0046A6C7  mov     ecx, [esi+190h]
0046A6CD  mov     eax, [ecx]
0046A6CF  mov     edx, dword_75334C
0046A6D5  mov     eax, [eax+11Ch]
0046A6DB  push    edx
0046A6DC  call    eax
0046A6DE  mov     ecx, [esi+190h]
0046A6E4  mov     edx, [ecx]
0046A6E6  mov     eax, [edx+88h]
0046A6EC  push    offset dword_7DCA98
0046A6F1  push    ebx
0046A6F2  call    eax
0046A6F4  mov     ecx, [esi+190h]
0046A6FA  mov     edx, [ecx]
0046A6FC  mov     eax, [edx+88h]
0046A702  push    offset dword_7DCA98
0046A707  push    1
0046A709  call    eax
0046A70B  mov     ecx, [esi+190h]
0046A711  mov     edx, dword_7530A0
0046A717  push    0D0h; Size
0046A71C  mov     [ecx+150h], edx
0046A722  call    ??2@YAPAXI@Z; operator new(uint)
0046A727  add     esp, 4
0046A72A  mov     [esp+90h+var_80], eax
0046A72E  mov     byte ptr [esp+90h+var_4], 0Bh
0046A736  cmp     eax, ebx
0046A738  jz      short loc_46A75A
0046A73A  mov     edx, dword_753420
0046A740  lea     ecx, [esi+168h]
0046A746  push    ecx
0046A747  mov     ecx, dword_753710
0046A74D  push    4
0046A74F  push    ecx
0046A750  push    edx
0046A751  mov     ecx, eax
0046A753  call    sub_5C5DF0
0046A758  jmp     short loc_46A75C
0046A75A  xor     eax, eax
0046A75C  mov     edi, [esp+90h+arg_4]
0046A763  mov     byte ptr [esp+90h+var_4], 6
0046A76B  mov     [esi+174h], eax
0046A771  mov     eax, [edi]
0046A773  mov     edx, [eax+0E0h]
0046A779  mov     ecx, edi
0046A77B  call    edx
0046A77D  fstp    [esp+90h+var_80]
0046A781  fld     [esp+90h+var_80]
0046A785  fld1
0046A787  fcom    st(1)
0046A789  fnstsw  ax
0046A78B  fld1
0046A78D  test    ah, 5
0046A790  jp      short loc_46A798
0046A792  fst     [esp+90h+var_80]
0046A796  jmp     short loc_46A7A0
0046A798  fxch    st(2)
0046A79A  fst     [esp+90h+var_80]
0046A79E  fxch    st(2)
0046A7A0  fld     [esp+90h+var_80]
0046A7A4  fcomp   ds:dbl_727150
0046A7AA  fnstsw  ax
0046A7AC  test    ah, 5
0046A7AF  jp      short loc_46A7C3
0046A7B1  fstp    st(2)
0046A7B3  fstp    st
0046A7B5  fstp    st
0046A7B7  fldz
0046A7B9  fstp    [esp+90h+var_80]
0046A7BD  fld     [esp+90h+var_80]
0046A7C1  jmp     short loc_46A7DC
0046A7C3  fxch    st(1)
0046A7C5  fcomp   st(2)
0046A7C7  fnstsw  ax
0046A7C9  test    ah, 5
0046A7CC  jp      short loc_46A7DA
0046A7CE  fstp    st(1)
0046A7D0  fstp    [esp+90h+var_80]
0046A7D4  fld     [esp+90h+var_80]
0046A7D8  jmp     short loc_46A7DC
0046A7DA  fstp    st
0046A7DC  mov     ecx, [esi+174h]
0046A7E2  mov     eax, [ecx]
0046A7E4  mov     edx, [eax+118h]
0046A7EA  sub     esp, 8
0046A7ED  fstp    [esp+98h+var_98]
0046A7F0  call    edx
0046A7F2  push    0D0h; Size
0046A7F7  call    ??2@YAPAXI@Z; operator new(uint)
0046A7FC  add     esp, 4
0046A7FF  mov     [esp+90h+var_80], eax
0046A803  mov     byte ptr [esp+90h+var_4], 0Ch
0046A80B  cmp     eax, ebx
0046A80D  jz      short loc_46A82F
0046A80F  mov     edx, dword_753420
0046A815  lea     ecx, [esi+168h]
0046A81B  push    ecx
0046A81C  mov     ecx, dword_753710
0046A822  push    5
0046A824  push    ecx
0046A825  push    edx
0046A826  mov     ecx, eax
0046A828  call    sub_5C5DF0
0046A82D  jmp     short loc_46A831
0046A82F  xor     eax, eax
0046A831  mov     byte ptr [esp+90h+var_4], 6
0046A839  mov     [esi+178h], eax
0046A83F  mov     ebp, [eax]
0046A841  mov     eax, [edi]
0046A843  mov     edx, [eax+0E4h]
0046A849  mov     ecx, edi
0046A84B  add     ebp, 118h
0046A851  call    edx
0046A853  fdiv    ds:dbl_727360
0046A859  mov     ecx, [esi+178h]
0046A85F  mov     eax, [ebp+0]
0046A862  sub     esp, 8
0046A865  fstp    [esp+98h+var_98]
0046A868  call    eax
0046A86A  cmp     [edi+367h], bl
0046A870  lea     ebp, [esi+16Ch]
0046A876  setz    cl
0046A879  movzx   edx, cl
0046A87C  push    edx
0046A87D  push    ebp
0046A87E  push    6
0046A880  call    sub_464490
0046A885  add     esp, 0Ch
0046A888  mov     ecx, edi
0046A88A  mov     [esi+17Ch], eax
0046A890  call    sub_62D190
0046A895  movzx   eax, al
0046A898  push    eax
0046A899  push    ebp
0046A89A  push    7
0046A89C  call    sub_464490
0046A8A1  add     esp, 0Ch
0046A8A4  mov     [esi+180h], eax
0046A8AA  cmp     [esi+194h], bl
0046A8B0  jz      loc_46A99D
0046A8B6  mov     ecx, [esi+18Ch]
0046A8BC  mov     edx, [ecx]
0046A8BE  mov     eax, [edx+7Ch]
0046A8C1  push    ebx
0046A8C2  call    eax
0046A8C4  xor     ecx, ecx
0046A8C6  push    12h; int
0046A8C8  mov     word ptr [esp+94h+var_78], cx
0046A8CD  push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
0046A8D2  lea     ecx, [esp+98h+var_7C]
0046A8D6  mov     [esp+98h+var_64], 7
0046A8DE  mov     [esp+98h+var_68], ebx
0046A8E2  call    sub_40D1E0
0046A8E7  lea     edx, [esp+90h+var_7C]
0046A8EB  mov     byte ptr [esp+90h+var_4], 0Dh
0046A8F3  mov     eax, [esi+190h]
0046A8F9  push    edx
0046A8FA  push    eax
0046A8FB  call    sub_453240
0046A900  mov     byte ptr [esp+90h+var_4], 6
0046A908  cmp     [esp+90h+var_64], 8
0046A90D  jb      short loc_46A91C
0046A90F  mov     ecx, [esp+90h+var_78]
0046A913  push    ecx; void *
0046A914  call    ??3@YAXPAX@Z; operator delete(void *)
0046A919  add     esp, 4
0046A91C  mov     edi, [esi+170h]
0046A922  mov     eax, [edi+950h]
0046A928  cmp     eax, ebx
0046A92A  jz      short loc_46A98F
0046A92C  cmp     [eax+54h], ebx
0046A92F  jle     short loc_46A98F
0046A931  cmp     [edi+9E8h], bl
0046A937  jnz     short loc_46A98F
0046A939  mov     ecx, [edi+99Ch]
0046A93F  cmp     ecx, ebx
0046A941  jz      short loc_46A94C
0046A943  call    sub_651F30
0046A948  cmp     al, bl
0046A94A  jnz     short loc_46A955
0046A94C  cmp     dword ptr [edi+9E4h], 1
0046A953  jz      short loc_46A98F
0046A955  push    offset aCredits_0; "[CREDITS]"
0046A95A  lea     ecx, [esp+94h+var_7C]
0046A95E  call    sub_40D030
0046A963  lea     edx, [esp+90h+var_7C]
0046A967  mov     byte ptr [esp+90h+var_4], 0Eh
0046A96F  mov     eax, [esi+188h]
0046A975  push    edx
0046A976  push    eax
0046A977  call    sub_453240
0046A97C  lea     ecx, [esp+90h+var_7C]; void *
0046A980  mov     byte ptr [esp+90h+var_4], 6
0046A988  call    sub_401330
0046A98D  jmp     short loc_46A99D
0046A98F  mov     ecx, [esi+188h]
0046A995  mov     edx, [ecx]
0046A997  mov     eax, [edx+7Ch]
0046A99A  push    ebx
0046A99B  call    eax
0046A99D  mov     ecx, [esi+170h]
0046A9A3  mov     eax, [ecx+91Ch]
0046A9A9  cmp     eax, 2Ah ; '*'
0046A9AC  jz      short loc_46A9B8
0046A9AE  cmp     eax, 2Bh ; '+'
0046A9B1  jz      short loc_46A9B8
0046A9B3  cmp     eax, 32h ; '2'
0046A9B6  jnz     short loc_46A9C6
0046A9B8  mov     ecx, [esi+18Ch]
0046A9BE  mov     edx, [ecx]
0046A9C0  mov     eax, [edx+7Ch]
0046A9C3  push    ebx
0046A9C4  call    eax
0046A9C6  mov     eax, [esi+170h]
0046A9CC  mov     edi, 2
0046A9D1  cmp     [eax+920h], edi
0046A9D7  jnz     short loc_46AA1B
0046A9D9  mov     ecx, [eax+868h]
0046A9DF  mov     ecx, [ecx+174h]
0046A9E5  mov     edx, [ecx]
0046A9E7  mov     eax, [edx+91Ch]
0046A9ED  cmp     eax, 1
0046A9F0  jl      short loc_46AA0D
0046A9F2  cmp     eax, 0Fh
0046A9F5  jg      short loc_46AA0D
0046A9F7  mov     eax, [ecx+4]
0046A9FA  mov     ecx, [eax+178h]
0046AA00  cmp     [ecx+6Ch], ebx
0046AA03  jle     short loc_46AA0D
0046AA05  cmp     [edx+920h], edi
0046AA0B  jz      short loc_46AA1B
0046AA0D  mov     ecx, [esi+18Ch]
0046AA13  mov     edx, [ecx]
0046AA15  mov     eax, [edx+7Ch]
0046AA18  push    ebx
0046AA19  call    eax
0046AA1B  mov     ecx, [esi+170h]
0046AA21  mov     eax, [ecx+950h]
0046AA27  cmp     eax, ebx
0046AA29  jz      short loc_46AA56
0046AA2B  cmp     [eax+54h], ebx
0046AA2E  jg      short loc_46AA36
0046AA30  cmp     dword ptr [eax+4Ch], 0Fh
0046AA34  jl      short loc_46AA56
0046AA36  cmp     [ecx+920h], edi
0046AA3C  jz      short loc_46AA56
0046AA3E  mov     ecx, [ecx+91Ch]
0046AA44  cmp     ecx, 2Bh ; '+'
0046AA47  jz      short loc_46AA56
0046AA49  cmp     ecx, 32h ; '2'
0046AA4C  jz      short loc_46AA56
0046AA4E  cmp     [esi+194h], bl
0046AA54  jz      short loc_46AA64
0046AA56  mov     ecx, [esi+184h]
0046AA5C  mov     edx, [ecx]
0046AA5E  mov     eax, [edx+7Ch]
0046AA61  push    ebx
0046AA62  call    eax
0046AA64  mov     [esi+19Ch], ebx
0046AA6A  mov     [esi+1A0h], ebx
0046AA70  mov     [esi+1A4h], ebx
0046AA76  mov     [esi+1A8h], ebx
0046AA7C  mov     ecx, dword_752C9C
0046AA82  push    1Bh; SourceSize
0046AA84  mov     [esi+198h], ecx
0046AA8A  push    offset aOptionDlgLabel; "OPTION_DLG_LABEL_FONT_SCALE"
0046AA8F  lea     ecx, [esp+98h+var_7C]
0046AA93  mov     [esp+98h+var_64], 0Fh
0046AA9B  mov     [esp+98h+var_68], ebx
0046AA9F  mov     byte ptr [esp+98h+var_78], bl
0046AAA3  call    sub_4032C0
0046AAA8  mov     byte ptr [esp+90h+var_4], 0Fh
0046AAB0  fld1
0046AAB2  mov     edx, [esi+198h]
0046AAB8  mov     ecx, dword_755F28
0046AABE  mov     edi, [edx]
0046AAC0  sub     esp, 8
0046AAC3  lea     eax, [esp+98h+var_7C]
0046AAC7  fstp    [esp+98h+var_98]; double
0046AACA  push    eax; int
0046AACB  call    sub_62A130
0046AAD0  mov     ecx, [esi+198h]
0046AAD6  mov     edx, [edi+40h]
0046AAD9  sub     esp, 8
0046AADC  fstp    [esp+98h+var_98]
0046AADF  call    edx
0046AAE1  cmp     [esp+90h+var_64], 10h
0046AAE6  jb      short loc_46AAF5
0046AAE8  mov     eax, [esp+90h+var_78]
0046AAEC  push    eax; void *
0046AAED  call    ??3@YAXPAX@Z; operator delete(void *)
0046AAF2  add     esp, 4
0046AAF5  mov     ecx, [esp+90h+var_C]
0046AAFC  pop     edi
0046AAFD  mov     eax, esi
0046AAFF  pop     esi
0046AB00  pop     ebp
0046AB01  pop     ebx
0046AB02  mov     large fs:0, ecx
0046AB09  add     esp, 80h
0046AB0F  retn    0Ch
006EA580  lea     ecx, [ebp-28h]; void *
006EA583  jmp     sub_401330
006EA588  lea     ecx, [ebp-44h]; void *
006EA58B  jmp     sub_401330
006EA590  lea     ecx, [ebp-60h]; void *
006EA593  jmp     sub_401330
006EA598  mov     ecx, [ebp+4]
006EA59B  jmp     sub_5CFD40
006EA5A0  lea     ecx, [ebp-7Ch]; void *
006EA5A3  jmp     sub_401330
006EA5A8  lea     ecx, [ebp-7Ch]; void *
006EA5AB  jmp     sub_401330
006EA5B0  lea     ecx, [ebp-7Ch]; void *
006EA5B3  jmp     sub_401330
006EA5B8  lea     ecx, [ebp-7Ch]; void *
006EA5BB  jmp     sub_401330
006EA5C0  mov     eax, [ebp-80h]
006EA5C3  push    eax; void *
006EA5C4  call    ??3@YAXPAX@Z; operator delete(void *)
006EA5C9  pop     ecx
006EA5CA  retn
006EA5CB  mov     eax, [ebp-80h]
006EA5CE  push    eax; void *
006EA5CF  call    ??3@YAXPAX@Z; operator delete(void *)
006EA5D4  pop     ecx
006EA5D5  retn
006EA5D6  lea     ecx, [ebp-7Ch]; void *
006EA5D9  jmp     sub_401330
006EA5DE  lea     ecx, [ebp-7Ch]; void *
006EA5E1  jmp     sub_401330
006EA5E6  lea     ecx, [ebp-7Ch]; void *
006EA5E9  jmp     sub_401E90
006EA5EE  mov     eax, offset stru_73CD80
006EA5F3  jmp     ___CxxFrameHandler3
