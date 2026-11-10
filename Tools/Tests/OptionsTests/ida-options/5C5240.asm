005C5240  push    ebp
005C5241  mov     ebp, esp
005C5243  push    ecx
005C5244  mov     [ebp+var_4], ecx
005C5247  mov     eax, [ebp+var_4]
005C524A  movzx   ecx, byte ptr [eax+66h]
005C524E  movzx   edx, [ebp+arg_0]
005C5252  cmp     ecx, edx
005C5254  jnz     short loc_5C525B
005C5256  jmp     loc_5C5300
005C525B  mov     eax, [ebp+var_4]
005C525E  mov     cl, [ebp+arg_0]
005C5261  mov     [eax+66h], cl
005C5264  movzx   edx, [ebp+arg_0]
005C5268  test    edx, edx
005C526A  jz      short loc_5C528C
005C526C  mov     eax, [ebp+var_4]
005C526F  cmp     dword ptr [eax+20h], 0
005C5273  jz      short loc_5C528C
005C5275  mov     ecx, [ebp+var_4]
005C5278  push    ecx
005C5279  mov     edx, [ebp+var_4]
005C527C  mov     eax, [edx+20h]
005C527F  mov     ecx, [ebp+var_4]
005C5282  mov     ecx, [ecx+20h]
005C5285  mov     edx, [eax]
005C5287  mov     eax, [edx+18h]
005C528A  call    eax
005C528C  mov     ecx, [ebp+var_4]
005C528F  mov     edx, [ecx]
005C5291  mov     ecx, [ebp+var_4]
005C5294  mov     eax, [edx+44h]
005C5297  call    eax
005C5299  movzx   ecx, [ebp+arg_0]
005C529D  test    ecx, ecx
005C529F  jnz     short loc_5C5300
005C52A1  mov     edx, [ebp+var_4]
005C52A4  cmp     dword ptr [edx+20h], 0
005C52A8  jz      short loc_5C5300
005C52AA  mov     eax, [ebp+var_4]
005C52AD  mov     ecx, [eax+20h]
005C52B0  mov     edx, [ecx+10Ch]
005C52B6  push    edx
005C52B7  mov     eax, [ebp+var_4]
005C52BA  mov     ecx, [eax+20h]
005C52BD  mov     edx, [ecx+108h]
005C52C3  push    edx
005C52C4  mov     eax, [ebp+var_4]
005C52C7  mov     edx, [eax]
005C52C9  mov     ecx, [ebp+var_4]
005C52CC  mov     eax, [edx+110h]
005C52D2  call    eax
005C52D4  movzx   ecx, al
005C52D7  test    ecx, ecx
005C52D9  jz      short loc_5C5300
005C52DB  mov     edx, [ebp+var_4]
005C52DE  mov     eax, [edx+20h]
005C52E1  mov     ecx, [eax+10Ch]
005C52E7  push    ecx
005C52E8  mov     edx, [ebp+var_4]
005C52EB  mov     eax, [edx+20h]
005C52EE  mov     ecx, [eax+108h]
005C52F4  push    ecx
005C52F5  mov     edx, [ebp+var_4]
005C52F8  mov     ecx, [edx+20h]
005C52FB  call    sub_5C40E0
005C5300  mov     esp, ebp
005C5302  pop     ebp
005C5303  retn    4
