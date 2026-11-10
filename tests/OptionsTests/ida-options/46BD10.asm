0046BD10  push    0FFFFFFFFh
0046BD12  push    offset SEH_46BD10
0046BD17  mov     eax, large fs:0
0046BD1D  push    eax
0046BD1E  mov     large fs:0, esp
0046BD25  sub     esp, 8
0046BD28  mov     eax, [esp+14h+arg_0]
0046BD2C  push    ebx
0046BD2D  push    esi
0046BD2E  xor     ebx, ebx
0046BD30  push    edi
0046BD31  mov     edi, [esp+20h+arg_4]
0046BD35  mov     [esp+20h+var_10], ebx
0046BD39  mov     esi, [eax+18Ch]
0046BD3F  xor     ecx, ecx
0046BD41  mov     dword ptr [edi+18h], 7
0046BD48  mov     [edi+14h], ebx
0046BD4B  mov     [esp+20h+var_14], 20h ; ' '
0046BD53  add     esi, 0A4h
0046BD59  mov     [edi+4], cx
0046BD5D  mov     [esp+20h+var_4], ebx
0046BD61  mov     eax, [esi+14h]
0046BD64  cmp     ebx, eax
0046BD66  mov     [esp+20h+var_10], 1
0046BD6E  jge     short loc_46BDB7
0046BD70  push    ebp
0046BD71  jbe     short loc_46BD78
0046BD73  call    __invalid_parameter_noinfo
0046BD78  cmp     dword ptr [esi+18h], 8
0046BD7C  jb      short loc_46BD83
0046BD7E  mov     eax, [esi+4]
0046BD81  jmp     short loc_46BD86
0046BD83  lea     eax, [esi+4]
0046BD86  movzx   ebp, word ptr [eax+ebx*2]
0046BD8A  cmp     bp, 20h ; ' '
0046BD8E  jnz     short loc_46BD9B
0046BD90  cmp     word ptr [esp+24h+var_14], bp
0046BD95  jz      short loc_46BDA5
0046BD97  push    20h ; ' '
0046BD99  jmp     short loc_46BD9C
0046BD9B  push    ebp
0046BD9C  push    1
0046BD9E  mov     ecx, edi
0046BDA0  call    sub_468350
0046BDA5  mov     eax, [esi+14h]
0046BDA8  movzx   edx, bp
0046BDAB  inc     ebx
0046BDAC  cmp     ebx, eax
0046BDAE  mov     [esp+24h+var_14], edx
0046BDB2  jl      short loc_46BD71
0046BDB4  xor     ebx, ebx
0046BDB6  pop     ebp
0046BDB7  mov     eax, [edi+14h]
0046BDBA  cmp     eax, ebx
0046BDBC  jz      short loc_46BE00
0046BDBE  lea     esi, [eax-1]
0046BDC1  cmp     esi, eax
0046BDC3  jbe     short loc_46BDCA
0046BDC5  call    __invalid_parameter_noinfo
0046BDCA  cmp     dword ptr [edi+18h], 8
0046BDCE  jb      short loc_46BDD5
0046BDD0  mov     eax, [edi+4]
0046BDD3  jmp     short loc_46BDD8
0046BDD5  lea     eax, [edi+4]
0046BDD8  cmp     word ptr [eax+esi*2], 20h ; ' '
0046BDDD  jnz     short loc_46BE00
0046BDDF  mov     ecx, [edi+14h]
0046BDE2  lea     eax, [ecx-1]
0046BDE5  cmp     eax, ecx
0046BDE7  ja      short loc_46BDF5
0046BDE9  push    0FFFFFFFFh
0046BDEB  push    eax
0046BDEC  mov     ecx, edi
0046BDEE  call    sub_40D5C0
0046BDF3  jmp     short loc_46BE00
0046BDF5  sub     eax, ecx
0046BDF7  push    ebx
0046BDF8  push    eax
0046BDF9  mov     ecx, edi
0046BDFB  call    sub_468350
0046BE00  mov     ecx, [esp+20h+var_C]
0046BE04  mov     eax, edi
0046BE06  pop     edi
0046BE07  pop     esi
0046BE08  pop     ebx
0046BE09  mov     large fs:0, ecx
0046BE10  add     esp, 14h
0046BE13  retn    8
006DE5A0  mov     eax, [ebp-10h]
006DE5A3  and     eax, 1
006DE5A6  jz      locret_6DE5B8
006DE5AC  and     dword ptr [ebp-10h], 0FFFFFFFEh
006DE5B0  mov     ecx, [ebp+8]; void *
006DE5B3  jmp     sub_401330
006DE5B8  retn
006DE5B9  mov     eax, offset stru_7307BC
006DE5BE  jmp     ___CxxFrameHandler3
