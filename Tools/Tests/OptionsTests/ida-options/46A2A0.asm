0046A2A0  cmp     dword ptr [esi], 0
0046A2A3  jnz     short loc_46A2AA
0046A2A5  call    __invalid_parameter_noinfo
0046A2AA  mov     eax, [esi+4]
0046A2AD  cmp     byte ptr [eax+41h], 0
0046A2B1  jz      short loc_46A2C4
0046A2B3  mov     eax, [eax+8]
0046A2B6  mov     [esi+4], eax
0046A2B9  cmp     byte ptr [eax+41h], 0
0046A2BD  jz      short locret_46A316
0046A2BF  jmp     __invalid_parameter_noinfo
0046A2C4  mov     ecx, [eax]
0046A2C6  cmp     byte ptr [ecx+41h], 0
0046A2CA  jnz     short loc_46A2E4
0046A2CC  mov     eax, [ecx+8]
0046A2CF  cmp     byte ptr [eax+41h], 0
0046A2D3  jnz     short loc_46A2E0
0046A2D5  mov     ecx, eax
0046A2D7  mov     eax, [ecx+8]
0046A2DA  cmp     byte ptr [eax+41h], 0
0046A2DE  jz      short loc_46A2D5
0046A2E0  mov     [esi+4], ecx
0046A2E3  retn
0046A2E4  mov     eax, [eax+4]
0046A2E7  cmp     byte ptr [eax+41h], 0
0046A2EB  jnz     short loc_46A305
0046A2ED  lea     ecx, [ecx+0]
0046A2F0  mov     ecx, [esi+4]
0046A2F3  cmp     ecx, [eax]
0046A2F5  jnz     short loc_46A305
0046A2F7  mov     [esi+4], eax
0046A2FA  mov     edx, eax
0046A2FC  mov     eax, [edx+4]
0046A2FF  cmp     byte ptr [eax+41h], 0
0046A303  jz      short loc_46A2F0
0046A305  mov     ecx, [esi+4]
0046A308  cmp     byte ptr [ecx+41h], 0
0046A30C  jz      short loc_46A313
0046A30E  jmp     __invalid_parameter_noinfo
0046A313  mov     [esi+4], eax
0046A316  retn
