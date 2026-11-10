005C5110  push    ebp
005C5111  mov     ebp, esp
005C5113  push    ecx
005C5114  mov     [ebp+var_4], ecx
005C5117  mov     eax, [ebp+var_4]
005C511A  mov     ecx, [eax+40h]
005C511D  cmp     ecx, [ebp+arg_0]
005C5120  jnz     short loc_5C5145
005C5122  mov     edx, [ebp+var_4]
005C5125  mov     eax, [edx+44h]
005C5128  cmp     eax, [ebp+arg_4]
005C512B  jnz     short loc_5C5145
005C512D  mov     ecx, [ebp+var_4]
005C5130  mov     edx, [ecx+48h]
005C5133  cmp     edx, [ebp+arg_8]
005C5136  jnz     short loc_5C5145
005C5138  mov     eax, [ebp+var_4]
005C513B  mov     ecx, [eax+4Ch]
005C513E  cmp     ecx, [ebp+arg_C]
005C5141  jnz     short loc_5C5145
005C5143  jmp     short loc_5C5197
005C5145  mov     edx, [ebp+var_4]
005C5148  mov     eax, [edx]
005C514A  mov     ecx, [ebp+var_4]
005C514D  mov     edx, [eax+4Ch]
005C5150  call    edx
005C5152  mov     eax, [ebp+var_4]
005C5155  mov     ecx, [ebp+arg_0]
005C5158  mov     [eax+40h], ecx
005C515B  mov     edx, [ebp+var_4]
005C515E  mov     eax, [ebp+arg_4]
005C5161  mov     [edx+44h], eax
005C5164  mov     ecx, [ebp+var_4]
005C5167  mov     edx, [ebp+arg_8]
005C516A  mov     [ecx+48h], edx
005C516D  mov     eax, [ebp+var_4]
005C5170  mov     ecx, [ebp+arg_C]
005C5173  mov     [eax+4Ch], ecx
005C5176  mov     edx, [ebp+var_4]
005C5179  mov     eax, [edx]
005C517B  mov     ecx, [ebp+var_4]
005C517E  mov     edx, [eax+44h]
005C5181  call    edx
005C5183  mov     eax, [ebp+var_4]
005C5186  cmp     dword ptr [eax+20h], 0
005C518A  jz      short loc_5C5197
005C518C  mov     ecx, [ebp+var_4]
005C518F  mov     ecx, [ecx+20h]
005C5192  call    sub_5C41E0
005C5197  mov     esp, ebp
005C5199  pop     ebp
005C519A  retn    10h
