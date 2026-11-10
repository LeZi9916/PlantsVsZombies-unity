005C4E90  push    ebp
005C4E91  mov     ebp, esp
005C4E93  sub     esp, 0A0h
005C4E99  mov     [ebp+var_A0], ecx
005C4E9F  mov     eax, [ebp+var_A0]
005C4EA5  add     eax, 6Ch ; 'l'
005C4EA8  mov     [ebp+var_9C], eax
005C4EAE  lea     ecx, [ebp+var_24]
005C4EB1  push    ecx
005C4EB2  mov     ecx, [ebp+var_9C]
005C4EB8  call    sub_5B74F0
005C4EBD  mov     edx, [eax]
005C4EBF  mov     eax, [eax+4]
005C4EC2  mov     [ebp+var_1C], edx
005C4EC5  mov     [ebp+var_18], eax
005C4EC8  lea     ecx, [ebp+var_34]
005C4ECB  push    ecx
005C4ECC  mov     ecx, [ebp+var_9C]
005C4ED2  call    sub_5B4100
005C4ED7  mov     edx, [eax]
005C4ED9  mov     eax, [eax+4]
005C4EDC  mov     [ebp+var_2C], edx
005C4EDF  mov     [ebp+var_28], eax
005C4EE2  mov     ecx, [ebp+var_18]
005C4EE5  push    ecx
005C4EE6  mov     edx, [ebp+var_1C]
005C4EE9  push    edx
005C4EEA  mov     eax, [ebp+var_28]
005C4EED  push    eax
005C4EEE  mov     ecx, [ebp+var_2C]
005C4EF1  push    ecx
005C4EF2  lea     edx, [ebp+var_3C]
005C4EF5  push    edx
005C4EF6  mov     ecx, [ebp+var_9C]
005C4EFC  call    sub_5D2E90
005C4F01  mov     [ebp+var_4], 0
005C4F08  jmp     short loc_5C4F13
005C4F0A  mov     eax, [ebp+var_4]
005C4F0D  add     eax, 1
005C4F10  mov     [ebp+var_4], eax
005C4F13  mov     ecx, [ebp+var_4]
005C4F16  cmp     ecx, [ebp+arg_4]
005C4F19  jge     short loc_5C4F77
005C4F1B  mov     edx, [ebp+var_4]
005C4F1E  shl     edx, 4
005C4F21  mov     eax, [ebp+arg_0]
005C4F24  mov     ecx, [eax+edx+0Ch]
005C4F28  push    ecx
005C4F29  mov     edx, [ebp+var_4]
005C4F2C  shl     edx, 4
005C4F2F  mov     eax, [ebp+arg_0]
005C4F32  mov     ecx, [eax+edx+8]
005C4F36  push    ecx
005C4F37  mov     edx, [ebp+var_4]
005C4F3A  shl     edx, 4
005C4F3D  mov     eax, [ebp+arg_0]
005C4F40  mov     ecx, [eax+edx+4]
005C4F44  push    ecx
005C4F45  mov     edx, [ebp+var_4]
005C4F48  shl     edx, 4
005C4F4B  mov     eax, [ebp+arg_0]
005C4F4E  mov     ecx, [eax+edx]
005C4F51  push    ecx
005C4F52  lea     ecx, [ebp+var_14]
005C4F55  call    sub_5CA5A0
005C4F5A  push    eax
005C4F5B  mov     edx, [ebp+var_4]
005C4F5E  push    edx
005C4F5F  mov     eax, [ebp+var_A0]
005C4F65  mov     edx, [eax]
005C4F67  mov     ecx, [ebp+var_A0]
005C4F6D  mov     eax, [edx+88h]
005C4F73  call    eax
005C4F75  jmp     short loc_5C4F0A
005C4F77  mov     ecx, [ebp+var_A0]
005C4F7D  mov     edx, [ecx]
005C4F7F  mov     ecx, [ebp+var_A0]
005C4F85  mov     eax, [edx+44h]
005C4F88  call    eax
005C4F8A  mov     esp, ebp
005C4F8C  pop     ebp
005C4F8D  retn    8
