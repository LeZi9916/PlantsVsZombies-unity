005C1D00  push    ebp
005C1D01  mov     ebp, esp
005C1D03  sub     esp, 2Ch
005C1D06  mov     [ebp+var_2C], ecx
005C1D09  lea     eax, [ebp+var_8]
005C1D0C  push    eax
005C1D0D  mov     ecx, [ebp+var_2C]
005C1D10  add     ecx, 4
005C1D13  call    sub_62EFA0
005C1D18  jmp     short loc_5C1D22
005C1D1A  lea     ecx, [ebp+var_8]
005C1D1D  call    sub_5D3980
005C1D22  mov     ecx, [ebp+var_2C]
005C1D25  add     ecx, 4
005C1D28  mov     [ebp+var_28], ecx
005C1D2B  mov     edx, [ebp+var_28]
005C1D2E  mov     eax, [edx+14h]
005C1D31  mov     [ebp+var_24], eax
005C1D34  lea     eax, [ebp+var_14]
005C1D37  call    sub_4086A0
005C1D3C  mov     ecx, [ebp+var_24]
005C1D3F  mov     [ebp+var_10], ecx
005C1D42  mov     edx, 1
005C1D47  test    edx, edx
005C1D49  jz      short loc_5C1D56
005C1D4B  cmp     [ebp+var_28], 0
005C1D4F  jnz     short loc_5C1D56
005C1D51  call    __invalid_parameter_noinfo
005C1D56  mov     eax, [ebp+var_28]
005C1D59  lea     edx, [ebp+var_14]
005C1D5C  call    sub_408680
005C1D61  lea     eax, [ebp+var_14]
005C1D64  push    eax
005C1D65  lea     ecx, [ebp+var_8]
005C1D68  call    sub_632700
005C1D6D  movzx   ecx, al
005C1D70  neg     ecx
005C1D72  sbb     ecx, ecx
005C1D74  add     ecx, 1
005C1D77  movzx   edx, cl
005C1D7A  test    edx, edx
005C1D7C  jz      short loc_5C1DBC
005C1D7E  lea     ecx, [ebp+var_8]
005C1D81  call    sub_5B7240
005C1D86  mov     eax, [eax]
005C1D88  mov     [ebp+var_C], eax
005C1D8B  mov     ecx, [ebp+var_C]
005C1D8E  push    ecx
005C1D8F  mov     edx, [ebp+arg_0]
005C1D92  mov     eax, [edx]
005C1D94  mov     ecx, [ebp+arg_0]
005C1D97  mov     edx, [eax+18h]
005C1D9A  call    edx
005C1D9C  mov     eax, [ebp+arg_0]
005C1D9F  push    eax
005C1DA0  mov     ecx, [ebp+var_C]
005C1DA3  mov     edx, [ecx]
005C1DA5  mov     ecx, [ebp+var_C]
005C1DA8  mov     eax, [edx+54h]
005C1DAB  call    eax
005C1DAD  mov     ecx, [ebp+var_C]
005C1DB0  mov     dword ptr [ecx+20h], 0
005C1DB7  jmp     loc_5C1D1A
005C1DBC  mov     edx, [ebp+arg_0]
005C1DBF  mov     eax, [edx+7Ch]
005C1DC2  cmp     eax, [ebp+var_2C]
005C1DC5  jnz     short loc_5C1DD1
005C1DC7  mov     ecx, [ebp+arg_0]
005C1DCA  mov     dword ptr [ecx+7Ch], 0
005C1DD1  mov     esp, ebp
005C1DD3  pop     ebp
005C1DD4  retn    4
