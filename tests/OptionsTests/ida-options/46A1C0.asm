0046A1C0  mov     eax, [ecx+8]
0046A1C3  mov     edx, [eax]
0046A1C5  mov     [ecx+8], edx
0046A1C8  mov     edx, [eax]
0046A1CA  cmp     byte ptr [edx+41h], 0
0046A1CE  jnz     short loc_46A1D3
0046A1D0  mov     [edx+4], ecx
0046A1D3  mov     edx, [ecx+4]
0046A1D6  mov     [eax+4], edx
0046A1D9  mov     edx, [esp+arg_0]
0046A1DD  mov     edx, [edx+18h]
0046A1E0  cmp     ecx, [edx+4]
0046A1E3  jnz     short loc_46A1F0
0046A1E5  mov     [edx+4], eax
0046A1E8  mov     [eax], ecx
0046A1EA  mov     [ecx+4], eax
0046A1ED  retn    4
0046A1F0  mov     edx, [ecx+4]
0046A1F3  cmp     ecx, [edx]
0046A1F5  jnz     short loc_46A201
0046A1F7  mov     [edx], eax
0046A1F9  mov     [eax], ecx
0046A1FB  mov     [ecx+4], eax
0046A1FE  retn    4
0046A201  mov     [edx+8], eax
0046A204  mov     [eax], ecx
0046A206  mov     [ecx+4], eax
0046A209  retn    4
