005CE670  push    ebp
005CE671  mov     ebp, esp
005CE673  sub     esp, 10h
005CE676  mov     [ebp+var_C], ecx
005CE679  mov     eax, [ebp+var_C]
005CE67C  mov     ecx, [eax+0C4h]
005CE682  mov     [ebp+var_8], ecx
005CE685  mov     edx, [ebp+var_8]
005CE688  mov     [ebp+var_4], edx
005CE68B  cmp     [ebp+var_4], 0
005CE68F  jz      short loc_5CE6A4
005CE691  push    1
005CE693  mov     eax, [ebp+var_4]
005CE696  mov     edx, [eax]
005CE698  mov     ecx, [ebp+var_4]
005CE69B  mov     eax, [edx]
005CE69D  call    eax
005CE69F  mov     [ebp+var_10], eax
005CE6A2  jmp     short loc_5CE6AB
005CE6A4  mov     [ebp+var_10], 0
005CE6AB  mov     ecx, [ebp+arg_0]
005CE6AE  mov     edx, [ecx]
005CE6B0  mov     ecx, [ebp+arg_0]
005CE6B3  mov     eax, [edx+2Ch]
005CE6B6  call    eax
005CE6B8  mov     ecx, [ebp+var_C]
005CE6BB  mov     [ecx+0C4h], eax
005CE6C1  mov     esp, ebp
005CE6C3  pop     ebp
005CE6C4  retn    4
