005C5040  push    ebp
005C5041  mov     ebp, esp
005C5043  push    ecx
005C5044  push    esi
005C5045  push    edi
005C5046  mov     [ebp+var_4], ecx
005C5049  mov     eax, dword_815EAC
005C504E  and     eax, 1
005C5051  jnz     short loc_5C506C
005C5053  mov     ecx, dword_815EAC
005C5059  or      ecx, 1
005C505C  mov     dword_815EAC, ecx
005C5062  mov     ecx, offset unk_815E9C
005C5067  call    sub_55BAB0
005C506C  mov     ecx, [ebp+var_4]
005C506F  add     ecx, 6Ch ; 'l'
005C5072  call    sub_43B1D0
005C5077  cmp     [ebp+arg_0], eax
005C507A  jge     short loc_5C508C
005C507C  mov     esi, [ebp+var_4]
005C507F  add     esi, 6Ch ; 'l'
005C5082  mov     edi, [ebp+arg_0]
005C5085  call    sub_43B0D0
005C508A  jmp     short loc_5C5091
005C508C  mov     eax, offset unk_815E9C
005C5091  pop     edi
005C5092  pop     esi
005C5093  mov     esp, ebp
005C5095  pop     ebp
005C5096  retn    4
