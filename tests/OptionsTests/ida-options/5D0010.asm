005D0010  push    ebp
005D0011  mov     ebp, esp
005D0013  sub     esp, 10h
005D0016  mov     [ebp+var_C], ecx
005D0019  mov     eax, [ebp+var_C]
005D001C  mov     ecx, [eax+110h]
005D0022  mov     [ebp+var_8], ecx
005D0025  mov     edx, [ebp+var_8]
005D0028  mov     [ebp+var_4], edx
005D002B  cmp     [ebp+var_4], 0
005D002F  jz      short loc_5D0044
005D0031  push    1
005D0033  mov     eax, [ebp+var_4]
005D0036  mov     edx, [eax]
005D0038  mov     ecx, [ebp+var_4]
005D003B  mov     eax, [edx]
005D003D  call    eax
005D003F  mov     [ebp+var_10], eax
005D0042  jmp     short loc_5D004B
005D0044  mov     [ebp+var_10], 0
005D004B  mov     ecx, [ebp+arg_0]
005D004E  mov     edx, [ecx]
005D0050  mov     ecx, [ebp+arg_0]
005D0053  mov     eax, [edx+2Ch]
005D0056  call    eax
005D0058  mov     ecx, [ebp+var_C]
005D005B  mov     [ecx+110h], eax
005D0061  mov     esp, ebp
005D0063  pop     ebp
005D0064  retn    4
