005C5820  push    ebp
005C5821  mov     ebp, esp
005C5823  sub     esp, 0Ch
005C5826  mov     [ebp+var_C], ecx
005C5829  mov     [ebp+var_4], 0Ah
005C5830  mov     [ebp+var_8], 1
005C5837  mov     eax, [ebp+arg_0]
005C583A  cmp     eax, [ebp+var_4]
005C583D  jl      short loc_5C5853
005C583F  mov     ecx, [ebp+var_8]
005C5842  add     ecx, 1
005C5845  mov     [ebp+var_8], ecx
005C5848  mov     edx, [ebp+var_4]
005C584B  imul    edx, 0Ah
005C584E  mov     [ebp+var_4], edx
005C5851  jmp     short loc_5C5837
005C5853  mov     eax, [ebp+var_8]
005C5856  mov     esp, ebp
005C5858  pop     ebp
005C5859  retn    4
