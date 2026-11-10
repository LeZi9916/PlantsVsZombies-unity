005C1BB0  push    ebp
005C1BB1  mov     ebp, esp
005C1BB3  sub     esp, 1Ch
005C1BB6  mov     [ebp+var_1C], ecx
005C1BB9  mov     eax, [ebp+var_1C]
005C1BBC  cmp     dword ptr [eax+24h], 0
005C1BC0  jnz     short loc_5C1BDD
005C1BC2  mov     ecx, [ebp+var_1C]
005C1BC5  mov     edx, [ecx+44h]
005C1BC8  mov     eax, [ebp+var_1C]
005C1BCB  mov     ecx, [eax+40h]
005C1BCE  mov     eax, [ebp+arg_0]
005C1BD1  call    sub_409250
005C1BD6  mov     eax, [ebp+arg_0]
005C1BD9  jmp     short loc_5C1C2F
005C1BDB  jmp     short loc_5C1C2F
005C1BDD  lea     ecx, [ebp+var_10]
005C1BE0  push    ecx
005C1BE1  mov     edx, [ebp+var_1C]
005C1BE4  mov     ecx, [edx+24h]
005C1BE7  mov     eax, [ebp+var_1C]
005C1BEA  mov     edx, [eax+24h]
005C1BED  mov     eax, [edx]
005C1BEF  mov     edx, [eax+3Ch]
005C1BF2  call    edx
005C1BF4  mov     [ebp+var_18], eax
005C1BF7  mov     eax, [ebp+var_1C]
005C1BFA  mov     edx, [eax+44h]
005C1BFD  mov     ecx, [ebp+var_1C]
005C1C00  mov     ecx, [ecx+40h]
005C1C03  lea     eax, [ebp+var_8]
005C1C06  call    sub_409250
005C1C0B  mov     [ebp+var_14], eax
005C1C0E  mov     edx, [ebp+var_14]
005C1C11  mov     edx, [edx+4]
005C1C14  mov     eax, [ebp+var_18]
005C1C17  add     edx, [eax+4]
005C1C1A  mov     ecx, [ebp+var_14]
005C1C1D  mov     ecx, [ecx]
005C1C1F  mov     eax, [ebp+var_18]
005C1C22  add     ecx, [eax]
005C1C24  mov     eax, [ebp+arg_0]
005C1C27  call    sub_409250
005C1C2C  mov     eax, [ebp+arg_0]
005C1C2F  mov     esp, ebp
005C1C31  pop     ebp
005C1C32  retn    4
