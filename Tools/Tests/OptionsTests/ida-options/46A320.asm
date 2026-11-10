0046A320  mov     ecx, [eax+8]
0046A323  cmp     byte ptr [ecx+41h], 0
0046A327  jnz     short locret_46A33B
0046A329  lea     esp, [esp+0]
0046A330  mov     eax, ecx
0046A332  mov     ecx, [eax+8]
0046A335  cmp     byte ptr [ecx+41h], 0
0046A339  jz      short loc_46A330
0046A33B  retn
