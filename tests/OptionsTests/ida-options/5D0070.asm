005D0070  push    ebp
005D0071  mov     ebp, esp
005D0073  sub     esp, 10h
005D0076  mov     [ebp+var_C], ecx
005D0079  mov     eax, [ebp+var_C]
005D007C  mov     ecx, [eax+114h]
005D0082  mov     [ebp+var_8], ecx
005D0085  mov     edx, [ebp+var_8]
005D0088  mov     [ebp+var_4], edx
005D008B  cmp     [ebp+var_4], 0
005D008F  jz      short loc_5D00A4
005D0091  push    1
005D0093  mov     eax, [ebp+var_4]
005D0096  mov     edx, [eax]
005D0098  mov     ecx, [ebp+var_4]
005D009B  mov     eax, [edx]
005D009D  call    eax
005D009F  mov     [ebp+var_10], eax
005D00A2  jmp     short loc_5D00AB
005D00A4  mov     [ebp+var_10], 0
005D00AB  mov     ecx, [ebp+arg_0]
005D00AE  mov     edx, [ecx]
005D00B0  mov     ecx, [ebp+arg_0]
005D00B3  mov     eax, [edx+2Ch]
005D00B6  call    eax
005D00B8  mov     ecx, [ebp+var_C]
005D00BB  mov     [ecx+114h], eax
005D00C1  mov     esp, ebp
005D00C3  pop     ebp
005D00C4  retn    4
