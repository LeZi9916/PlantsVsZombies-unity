0046A210  mov     eax, [ecx]
0046A212  mov     edx, [eax+8]
0046A215  mov     [ecx], edx
0046A217  mov     edx, [eax+8]
0046A21A  cmp     byte ptr [edx+41h], 0
0046A21E  jnz     short loc_46A223
0046A220  mov     [edx+4], ecx
0046A223  mov     edx, [ecx+4]
0046A226  mov     [eax+4], edx
0046A229  mov     edx, [esp+arg_0]
0046A22D  mov     edx, [edx+18h]
0046A230  cmp     ecx, [edx+4]
0046A233  jnz     short loc_46A241
0046A235  mov     [edx+4], eax
0046A238  mov     [eax+8], ecx
0046A23B  mov     [ecx+4], eax
0046A23E  retn    4
0046A241  mov     edx, [ecx+4]
0046A244  cmp     ecx, [edx+8]
0046A247  jnz     short loc_46A255
0046A249  mov     [edx+8], eax
0046A24C  mov     [eax+8], ecx
0046A24F  mov     [ecx+4], eax
0046A252  retn    4
0046A255  mov     [edx], eax
0046A257  mov     [eax+8], ecx
0046A25A  mov     [ecx+4], eax
0046A25D  retn    4
