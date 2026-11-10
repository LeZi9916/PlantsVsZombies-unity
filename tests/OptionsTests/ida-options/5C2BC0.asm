005C2BC0  push    ebp
005C2BC1  mov     ebp, esp
005C2BC3  sub     esp, 30h
005C2BC6  mov     [ebp+var_30], ecx
005C2BC9  mov     eax, [ebp+var_30]
005C2BCC  mov     edx, [eax]
005C2BCE  mov     ecx, [ebp+var_30]
005C2BD1  mov     eax, [edx+74h]
005C2BD4  call    eax
005C2BD6  mov     ecx, [ebp+var_30]
005C2BD9  mov     edx, [ecx+1Ch]
005C2BDC  mov     [ebp+var_18], edx
005C2BDF  cmp     [ebp+var_18], 0
005C2BE3  jbe     short loc_5C2BF2
005C2BE5  mov     eax, dword_815D00
005C2BEA  add     eax, 1
005C2BED  mov     dword_815D00, eax
005C2BF2  lea     ecx, [ebp+var_8]
005C2BF5  push    ecx
005C2BF6  mov     ecx, [ebp+var_30]
005C2BF9  add     ecx, 4
005C2BFC  call    sub_62EFA0
005C2C01  mov     edx, [ebp+var_30]
005C2C04  add     edx, 4
005C2C07  mov     [ebp+var_28], edx
005C2C0A  mov     eax, [ebp+var_28]
005C2C0D  mov     ecx, [eax+14h]
005C2C10  mov     [ebp+var_24], ecx
005C2C13  lea     eax, [ebp+var_14]
005C2C16  call    sub_4086A0
005C2C1B  mov     edx, [ebp+var_24]
005C2C1E  mov     [ebp+var_10], edx
005C2C21  mov     eax, 1
005C2C26  test    eax, eax
005C2C28  jz      short loc_5C2C35
005C2C2A  cmp     [ebp+var_28], 0
005C2C2E  jnz     short loc_5C2C35
005C2C30  call    __invalid_parameter_noinfo
005C2C35  mov     eax, [ebp+var_28]
005C2C38  lea     edx, [ebp+var_14]
005C2C3B  call    sub_408680
005C2C40  lea     ecx, [ebp+var_14]
005C2C43  push    ecx
005C2C44  lea     ecx, [ebp+var_8]
005C2C47  call    sub_632700
005C2C4C  movzx   edx, al
005C2C4F  neg     edx
005C2C51  sbb     edx, edx
005C2C53  add     edx, 1
005C2C56  movzx   eax, dl
005C2C59  test    eax, eax
005C2C5B  jz      short loc_5C2C81
005C2C5D  lea     ecx, [ebp+var_8]
005C2C60  call    sub_5B7240
005C2C65  mov     ecx, [eax]
005C2C67  mov     [ebp+var_C], ecx
005C2C6A  mov     edx, [ebp+var_C]
005C2C6D  mov     eax, [edx]
005C2C6F  mov     ecx, [ebp+var_C]
005C2C72  mov     edx, [eax+70h]
005C2C75  call    edx
005C2C77  lea     ecx, [ebp+var_8]
005C2C7A  call    sub_5D3980
005C2C7F  jmp     short loc_5C2C01
005C2C81  mov     esp, ebp
005C2C83  pop     ebp
005C2C84  retn
