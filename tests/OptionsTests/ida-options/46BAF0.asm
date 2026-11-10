0046BAF0  push    0FFFFFFFFh
0046BAF2  push    offset SEH_43EB30
0046BAF7  mov     eax, large fs:0
0046BAFD  push    eax
0046BAFE  mov     large fs:0, esp
0046BB05  push    ecx
0046BB06  push    esi
0046BB07  mov     esi, ecx
0046BB09  mov     [esp+14h+var_10], esi
0046BB0D  mov     dword ptr [esi], offset off_7117E0
0046BB13  mov     dword ptr [esi+0A0h], offset off_718C68
0046BB1D  mov     dword ptr [esi+184h], offset off_711918
0046BB27  mov     [esp+14h+var_4], 0
0046BB2F  mov     ecx, [esi+18Ch]
0046BB35  test    ecx, ecx
0046BB37  jz      short loc_46BB41
0046BB39  mov     eax, [ecx]
0046BB3B  mov     edx, [eax]
0046BB3D  push    1
0046BB3F  call    edx
0046BB41  mov     ecx, esi
0046BB43  mov     [esp+14h+var_4], 0FFFFFFFFh
0046BB4B  call    sub_464AB0
0046BB50  mov     ecx, [esp+14h+var_C]
0046BB54  pop     esi
0046BB55  mov     large fs:0, ecx
0046BB5C  add     esp, 10h
0046BB5F  retn
006DDE30  mov     ecx, [ebp-10h]
006DDE33  jmp     sub_464AB0
006DDE38  mov     eax, offset stru_72FE4C
006DDE3D  jmp     ___CxxFrameHandler3
