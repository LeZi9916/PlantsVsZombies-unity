005C5370  push    ebp
005C5371  mov     ebp, esp
005C5373  push    ecx
005C5374  mov     [ebp+var_4], ecx
005C5377  cmp     [ebp+arg_0], 9
005C537B  jnz     short loc_5C53E4
005C537D  mov     eax, [ebp+var_4]
005C5380  mov     ecx, [eax+20h]
005C5383  movzx   edx, byte ptr [ecx+12Ch]
005C538A  test    edx, edx
005C538C  jz      short loc_5C53BB
005C538E  mov     eax, [ebp+var_4]
005C5391  cmp     dword ptr [eax+98h], 0
005C5398  jz      short loc_5C53B9
005C539A  mov     ecx, [ebp+var_4]
005C539D  mov     edx, [ecx+98h]
005C53A3  push    edx
005C53A4  mov     eax, [ebp+var_4]
005C53A7  mov     ecx, [eax+20h]
005C53AA  mov     edx, [ebp+var_4]
005C53AD  mov     eax, [edx+20h]
005C53B0  mov     edx, [ecx]
005C53B2  mov     ecx, eax
005C53B4  mov     eax, [edx+20h]
005C53B7  call    eax
005C53B9  jmp     short loc_5C53E4
005C53BB  mov     ecx, [ebp+var_4]
005C53BE  cmp     dword ptr [ecx+9Ch], 0
005C53C5  jz      short loc_5C53E4
005C53C7  mov     edx, [ebp+var_4]
005C53CA  mov     eax, [edx+9Ch]
005C53D0  push    eax
005C53D1  mov     ecx, [ebp+var_4]
005C53D4  mov     edx, [ecx+20h]
005C53D7  mov     eax, [ebp+var_4]
005C53DA  mov     ecx, [eax+20h]
005C53DD  mov     edx, [edx]
005C53DF  mov     eax, [edx+20h]
005C53E2  call    eax
005C53E4  mov     esp, ebp
005C53E6  pop     ebp
005C53E7  retn    4
