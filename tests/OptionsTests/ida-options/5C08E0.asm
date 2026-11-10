005C08E0  push    ebp
005C08E1  mov     ebp, esp
005C08E3  sub     esp, 9Ch
005C08E9  mov     [ebp+var_9C], ecx
005C08EF  mov     eax, [ebp+var_9C]
005C08F5  add     eax, 4
005C08F8  mov     [ebp+var_28], eax
005C08FB  mov     ecx, [ebp+var_28]
005C08FE  mov     edx, [ecx+14h]
005C0901  mov     [ebp+var_24], edx
005C0904  lea     eax, [ebp+var_20]
005C0907  call    sub_4086A0
005C090C  mov     eax, [ebp+var_24]
005C090F  mov     [ebp+var_1C], eax
005C0912  mov     ecx, 1
005C0917  test    ecx, ecx
005C0919  jz      short loc_5C0926
005C091B  cmp     [ebp+var_28], 0
005C091F  jnz     short loc_5C0926
005C0921  call    __invalid_parameter_noinfo
005C0926  mov     eax, [ebp+var_28]
005C0929  lea     edx, [ebp+var_20]
005C092C  call    sub_408680
005C0931  mov     edx, [ebp+var_9C]
005C0937  add     edx, 4
005C093A  mov     [ebp+var_30], edx
005C093D  mov     eax, [ebp+var_30]
005C0940  mov     ecx, [eax+14h]
005C0943  mov     [ebp+var_2C], ecx
005C0946  lea     eax, [ebp+var_8]
005C0949  call    sub_4086A0
005C094E  mov     edx, [ebp+var_2C]
005C0951  mov     [ebp+var_4], edx
005C0954  mov     eax, 1
005C0959  test    eax, eax
005C095B  jz      short loc_5C0968
005C095D  cmp     [ebp+var_30], 0
005C0961  jnz     short loc_5C0968
005C0963  call    __invalid_parameter_noinfo
005C0968  mov     eax, [ebp+var_30]
005C096B  lea     edx, [ebp+var_8]
005C096E  call    sub_408680
005C0973  lea     ecx, [ebp+arg_0]
005C0976  push    ecx
005C0977  mov     edx, [ebp+var_4]
005C097A  push    edx
005C097B  mov     eax, [ebp+var_8]
005C097E  push    eax
005C097F  lea     ecx, [ebp+var_10]
005C0982  push    ecx
005C0983  mov     ecx, [ebp+var_9C]
005C0989  add     ecx, 4
005C098C  call    sub_62EFA0
005C0991  mov     edx, [eax+4]
005C0994  push    edx
005C0995  mov     eax, [eax]
005C0997  push    eax
005C0998  lea     ecx, [ebp+var_18]
005C099B  push    ecx
005C099C  call    sub_5D48D0
005C09A1  add     esp, 18h
005C09A4  mov     [ebp+var_98], eax
005C09AA  lea     edx, [ebp+var_20]
005C09AD  push    edx
005C09AE  mov     ecx, [ebp+var_98]
005C09B4  call    sub_632700
005C09B9  movzx   eax, al
005C09BC  neg     eax
005C09BE  sbb     eax, eax
005C09C0  add     eax, 1
005C09C3  mov     esp, ebp
005C09C5  pop     ebp
005C09C6  retn    4
