0046B8F0  push    0FFFFFFFFh
0046B8F2  push    offset SEH_46B8F0
0046B8F7  mov     eax, large fs:0
0046B8FD  push    eax
0046B8FE  mov     large fs:0, esp
0046B905  sub     esp, 54h
0046B908  push    ebx
0046B909  push    ebp
0046B90A  mov     ebp, [esp+68h+arg_4]
0046B90E  push    esi
0046B90F  mov     esi, [esp+6Ch+arg_0]
0046B913  push    edi
0046B914  xor     ebx, ebx
0046B916  push    ebx; int
0046B917  xor     eax, eax
0046B919  mov     edi, 7
0046B91E  push    offset unk_708460; Source
0046B923  lea     ecx, [esp+78h+var_28]
0046B927  mov     [esp+78h+var_10], edi
0046B92B  mov     [esp+78h+var_14], ebx
0046B92F  mov     word ptr [esp+78h+var_24], ax
0046B934  call    sub_40D1E0
0046B939  mov     [esp+70h+var_4], ebx
0046B93D  xor     ecx, ecx
0046B93F  push    13h; int
0046B941  mov     word ptr [esp+74h+var_40], cx
0046B946  push    offset off_711784; Source
0046B94B  lea     ecx, [esp+78h+var_44]
0046B94F  mov     [esp+78h+var_2C], edi
0046B953  mov     [esp+78h+var_30], ebx
0046B957  call    sub_40D1E0
0046B95C  mov     byte ptr [esp+70h+var_4], 1
0046B961  mov     edx, offset off_7117AC
0046B966  cmp     [esp+70h+arg_8], bl
0046B96A  jnz     short loc_46B971
0046B96C  mov     edx, offset off_7117C8
0046B971  xor     eax, eax
0046B973  mov     word ptr [esp+70h+var_5C], ax
0046B978  mov     eax, edx
0046B97A  mov     [esp+70h+var_48], edi
0046B97E  mov     [esp+70h+var_4C], ebx
0046B982  lea     edi, [eax+2]
0046B985  mov     cx, [eax]
0046B988  add     eax, 2
0046B98B  cmp     cx, bx
0046B98E  jnz     short loc_46B985
0046B990  sub     eax, edi
0046B992  sar     eax, 1
0046B994  push    eax; int
0046B995  push    edx; Source
0046B996  lea     ecx, [esp+78h+var_60]
0046B99A  call    sub_40D1E0
0046B99F  xor     eax, eax
0046B9A1  cmp     [esp+70h+arg_8], bl
0046B9A5  push    2
0046B9A7  setnz   al
0046B9AA  lea     ecx, [esp+74h+var_28]
0046B9AE  push    ecx
0046B9AF  lea     edx, [esp+78h+var_44]
0046B9B3  push    edx
0046B9B4  push    1
0046B9B6  lea     ecx, [esp+80h+var_60]
0046B9BA  lea     eax, [eax+eax+1Eh]
0046B9BE  push    eax
0046B9BF  push    ebp
0046B9C0  push    esi
0046B9C1  mov     byte ptr [esp+8Ch+var_4], 2
0046B9C9  call    sub_4646C0
0046B9CE  mov     edi, 8
0046B9D3  cmp     [esp+70h+var_48], edi
0046B9D7  jb      short loc_46B9E6
0046B9D9  mov     ecx, [esp+70h+var_5C]
0046B9DD  push    ecx; void *
0046B9DE  call    ??3@YAXPAX@Z; operator delete(void *)
0046B9E3  add     esp, 4
0046B9E6  xor     edx, edx
0046B9E8  mov     [esp+70h+var_48], 7
0046B9F0  mov     [esp+70h+var_4C], ebx
0046B9F4  mov     word ptr [esp+70h+var_5C], dx
0046B9F9  cmp     [esp+70h+var_2C], edi
0046B9FD  jb      short loc_46BA0C
0046B9FF  mov     eax, [esp+70h+var_40]
0046BA03  push    eax; void *
0046BA04  call    ??3@YAXPAX@Z; operator delete(void *)
0046BA09  add     esp, 4
0046BA0C  xor     ecx, ecx
0046BA0E  mov     [esp+70h+var_2C], 7
0046BA16  mov     [esp+70h+var_30], ebx
0046BA1A  mov     word ptr [esp+70h+var_40], cx
0046BA1F  mov     byte ptr [esp+70h+var_4], 6
0046BA24  cmp     [esp+70h+var_10], edi
0046BA28  jb      short loc_46BA37
0046BA2A  mov     edx, [esp+70h+var_24]
0046BA2E  push    edx; void *
0046BA2F  call    ??3@YAXPAX@Z; operator delete(void *)
0046BA34  add     esp, 4
0046BA37  xor     eax, eax
0046BA39  mov     dword ptr [esi+184h], offset off_70C644
0046BA43  mov     word ptr [esp+70h+var_24], ax
0046BA48  lea     eax, [esi+184h]
0046BA4E  push    eax
0046BA4F  mov     edi, esi
0046BA51  mov     [esp+74h+var_10], 7
0046BA59  mov     [esp+74h+var_14], ebx
0046BA5D  mov     dword ptr [esi], offset off_7117E0
0046BA63  mov     dword ptr [esi+0A0h], offset off_718C68
0046BA6D  mov     dword ptr [eax], offset off_711918
0046BA73  mov     [esi+188h], ebp
0046BA79  mov     [esi+181h], bl
0046BA7F  call    sub_4643D0
0046BA84  mov     [esi+18Ch], eax
0046BA8A  add     esp, 4
0046BA8D  mov     dword ptr [eax+118h], 0Ch
0046BA97  mov     ecx, dword_752B48
0046BA9D  push    0DCh
0046BAA2  push    ecx
0046BAA3  mov     ecx, [esi+18Ch]
0046BAA9  call    sub_5CAC50
0046BAAE  push    1
0046BAB0  push    28h ; '('
0046BAB2  push    6Eh ; 'n'
0046BAB4  call    sub_464B40
0046BAB9  mov     ecx, [esp+70h+var_C]
0046BABD  pop     edi
0046BABE  mov     eax, esi
0046BAC0  pop     esi
0046BAC1  pop     ebp
0046BAC2  mov     large fs:0, ecx
0046BAC9  pop     ebx
0046BACA  add     esp, 60h
0046BACD  retn    0Ch
006EB9E0  lea     ecx, [ebp-28h]; void *
006EB9E3  jmp     sub_401330
006EB9E8  lea     ecx, [ebp-44h]; void *
006EB9EB  jmp     sub_401330
006EB9F0  lea     ecx, [ebp-60h]; void *
006EB9F3  jmp     sub_401330
006EB9F8  mov     ecx, [ebp+4]
006EB9FB  jmp     sub_464AB0
006EBA00  mov     eax, offset stru_73E0C4
006EBA05  jmp     ___CxxFrameHandler3
