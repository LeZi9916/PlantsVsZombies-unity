005CE6D0  push    ebp
005CE6D1  mov     ebp, esp
005CE6D3  sub     esp, 8
005CE6D6  mov     [ebp+var_4], ecx
005CE6D9  mov     eax, [ebp+var_4]
005CE6DC  movzx   ecx, byte ptr [eax+68h]
005CE6E0  test    ecx, ecx
005CE6E2  jz      short loc_5CE703
005CE6E4  mov     edx, [ebp+var_4]
005CE6E7  movzx   eax, byte ptr [edx+69h]
005CE6EB  test    eax, eax
005CE6ED  jz      short loc_5CE703
005CE6EF  mov     ecx, [ebp+var_4]
005CE6F2  movzx   edx, byte ptr [ecx+66h]
005CE6F6  test    edx, edx
005CE6F8  jnz     short loc_5CE703
005CE6FA  mov     [ebp+var_8], 1
005CE701  jmp     short loc_5CE70A
005CE703  mov     [ebp+var_8], 0
005CE70A  mov     al, byte ptr [ebp+var_8]
005CE70D  mov     esp, ebp
005CE70F  pop     ebp
005CE710  retn
