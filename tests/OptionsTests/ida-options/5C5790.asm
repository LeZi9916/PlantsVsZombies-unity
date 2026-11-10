005C5790  push    ebp
005C5791  mov     ebp, esp
005C5793  sub     esp, 0Ch
005C5796  mov     [ebp+var_C], ecx
005C5799  mov     eax, [ebp+arg_0]
005C579C  mov     cl, [eax+4Ah]
005C579F  mov     [ebp+var_1], cl
005C57A2  mov     edx, [ebp+arg_0]
005C57A5  mov     al, byte_746BAA
005C57AA  mov     [edx+4Ah], al
005C57AD  push    0
005C57AF  push    0
005C57B1  push    0FFFFFFFFh
005C57B3  push    0
005C57B5  mov     ecx, [ebp+arg_10]
005C57B8  push    ecx
005C57B9  mov     edx, [ebp+arg_C]
005C57BC  push    edx
005C57BD  mov     eax, [ebp+arg_8]
005C57C0  push    eax
005C57C1  mov     ecx, [ebp+arg_4]
005C57C4  push    ecx
005C57C5  mov     ecx, [ebp+arg_0]
005C57C8  call    sub_594700
005C57CD  mov     [ebp+var_8], eax
005C57D0  mov     edx, [ebp+arg_0]
005C57D3  mov     al, [ebp+var_1]
005C57D6  mov     [edx+4Ah], al
005C57D9  mov     eax, [ebp+var_8]
005C57DC  mov     esp, ebp
005C57DE  pop     ebp
005C57DF  retn    14h
