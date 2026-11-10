005CF3D0  push    ebp
005CF3D1  mov     ebp, esp
005CF3D3  push    ecx
005CF3D4  mov     [ebp+var_4], ecx
005CF3D7  mov     eax, [ebp+arg_C]
005CF3DA  push    eax
005CF3DB  mov     ecx, [ebp+arg_8]
005CF3DE  push    ecx
005CF3DF  mov     edx, [ebp+arg_4]
005CF3E2  push    edx
005CF3E3  mov     eax, [ebp+arg_0]
005CF3E6  push    eax
005CF3E7  mov     ecx, [ebp+var_4]
005CF3EA  call    sub_5C54C0
005CF3EF  mov     ecx, [ebp+var_4]
005CF3F2  movzx   edx, byte ptr [ecx+69h]
005CF3F6  test    edx, edx
005CF3F8  jz      short loc_5CF42E
005CF3FA  mov     eax, [ebp+var_4]
005CF3FD  mov     ecx, [eax+20h]
005CF400  movzx   edx, byte ptr [ecx+9Ch]
005CF407  test    edx, edx
005CF409  jz      short loc_5CF42E
005CF40B  mov     eax, [ebp+var_4]
005CF40E  mov     ecx, [eax+0A0h]
005CF414  push    ecx
005CF415  mov     edx, [ebp+var_4]
005CF418  mov     eax, [edx+11Ch]
005CF41E  mov     ecx, [ebp+var_4]
005CF421  mov     ecx, [ecx+11Ch]
005CF427  mov     edx, [eax]
005CF429  mov     eax, [edx+8]
005CF42C  call    eax
005CF42E  mov     ecx, [ebp+var_4]
005CF431  mov     edx, [ecx]
005CF433  mov     ecx, [ebp+var_4]
005CF436  mov     eax, [edx+44h]
005CF439  call    eax
005CF43B  mov     esp, ebp
005CF43D  pop     ebp
005CF43E  retn    10h
