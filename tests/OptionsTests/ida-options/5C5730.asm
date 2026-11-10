005C5730  push    ebp
005C5731  mov     ebp, esp
005C5733  sub     esp, 0Ch
005C5736  mov     [ebp+var_C], ecx
005C5739  mov     eax, [ebp+arg_0]
005C573C  mov     cl, [eax+4Ah]
005C573F  mov     [ebp+var_1], cl
005C5742  mov     edx, [ebp+arg_0]
005C5745  mov     al, byte_746BAA
005C574A  mov     [edx+4Ah], al
005C574D  push    0FFFFFFFFh
005C574F  mov     ecx, [ebp+arg_20]
005C5752  push    ecx
005C5753  mov     edx, [ebp+arg_1C]
005C5756  push    edx
005C5757  movzx   eax, [ebp+arg_18]
005C575B  push    eax
005C575C  mov     ecx, [ebp+arg_14]
005C575F  push    ecx
005C5760  mov     edx, [ebp+arg_10]
005C5763  push    edx
005C5764  mov     eax, [ebp+arg_C]
005C5767  push    eax
005C5768  mov     ecx, [ebp+arg_8]
005C576B  push    ecx
005C576C  mov     edx, [ebp+arg_4]
005C576F  push    edx
005C5770  mov     ecx, [ebp+arg_0]
005C5773  call    sub_594280
005C5778  mov     [ebp+var_8], eax
005C577B  mov     eax, [ebp+arg_0]
005C577E  mov     cl, [ebp+var_1]
005C5781  mov     [eax+4Ah], cl
005C5784  mov     eax, [ebp+var_8]
005C5787  mov     esp, ebp
005C5789  pop     ebp
005C578A  retn    24h ; '$'
