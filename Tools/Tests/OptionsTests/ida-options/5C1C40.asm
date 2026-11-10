005C1C40  push    ebp
005C1C41  mov     ebp, esp
005C1C43  sub     esp, 2Ch
005C1C46  mov     [ebp+var_2C], ecx
005C1C49  lea     eax, [ebp+var_8]
005C1C4C  push    eax
005C1C4D  mov     ecx, [ebp+var_2C]
005C1C50  add     ecx, 4
005C1C53  call    sub_62EFA0
005C1C58  mov     ecx, [ebp+var_2C]
005C1C5B  add     ecx, 4
005C1C5E  mov     [ebp+var_24], ecx
005C1C61  mov     edx, [ebp+var_24]
005C1C64  mov     eax, [edx+14h]
005C1C67  mov     [ebp+var_20], eax
005C1C6A  lea     eax, [ebp+var_14]
005C1C6D  call    sub_4086A0
005C1C72  mov     ecx, [ebp+var_20]
005C1C75  mov     [ebp+var_10], ecx
005C1C78  mov     edx, 1
005C1C7D  test    edx, edx
005C1C7F  jz      short loc_5C1C8C
005C1C81  cmp     [ebp+var_24], 0
005C1C85  jnz     short loc_5C1C8C
005C1C87  call    __invalid_parameter_noinfo
005C1C8C  mov     eax, [ebp+var_24]
005C1C8F  lea     edx, [ebp+var_14]
005C1C92  call    sub_408680
005C1C97  lea     eax, [ebp+var_14]
005C1C9A  push    eax
005C1C9B  lea     ecx, [ebp+var_8]
005C1C9E  call    sub_632700
005C1CA3  movzx   ecx, al
005C1CA6  neg     ecx
005C1CA8  sbb     ecx, ecx
005C1CAA  add     ecx, 1
005C1CAD  movzx   edx, cl
005C1CB0  test    edx, edx
005C1CB2  jz      short loc_5C1CF5
005C1CB4  lea     ecx, [ebp+var_8]
005C1CB7  call    sub_5B7240
005C1CBC  mov     eax, [eax]
005C1CBE  mov     [ebp+var_C], eax
005C1CC1  mov     ecx, [ebp+var_C]
005C1CC4  mov     edx, [ebp+arg_0]
005C1CC7  mov     [ecx+20h], edx
005C1CCA  mov     eax, [ebp+arg_0]
005C1CCD  push    eax
005C1CCE  mov     ecx, [ebp+var_C]
005C1CD1  mov     edx, [ecx]
005C1CD3  mov     ecx, [ebp+var_C]
005C1CD6  mov     eax, [edx+50h]
005C1CD9  call    eax
005C1CDB  lea     ecx, [ebp+var_8]
005C1CDE  call    sub_5D3980
005C1CE3  mov     ecx, [ebp+var_2C]
005C1CE6  mov     edx, [ecx]
005C1CE8  mov     ecx, [ebp+var_2C]
005C1CEB  mov     eax, [edx+44h]
005C1CEE  call    eax
005C1CF0  jmp     loc_5C1C58
005C1CF5  mov     esp, ebp
005C1CF7  pop     ebp
005C1CF8  retn    4
