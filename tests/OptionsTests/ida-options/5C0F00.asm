005C0F00  push    ebp
005C0F01  mov     ebp, esp
005C0F03  sub     esp, 34h
005C0F06  mov     [ebp+var_30], ecx
005C0F09  mov     eax, [ebp+var_30]
005C0F0C  mov     edx, [eax]
005C0F0E  mov     ecx, [ebp+var_30]
005C0F11  mov     eax, [edx+44h]
005C0F14  call    eax
005C0F16  lea     ecx, [ebp+var_8]
005C0F19  push    ecx
005C0F1A  mov     ecx, [ebp+var_30]
005C0F1D  add     ecx, 4
005C0F20  call    sub_62EFA0
005C0F25  mov     edx, [ebp+var_30]
005C0F28  add     edx, 4
005C0F2B  mov     [ebp+var_20], edx
005C0F2E  mov     eax, [ebp+var_20]
005C0F31  mov     ecx, [eax+14h]
005C0F34  mov     [ebp+var_1C], ecx
005C0F37  lea     eax, [ebp+var_10]
005C0F3A  call    sub_4086A0
005C0F3F  mov     edx, [ebp+var_1C]
005C0F42  mov     [ebp+var_C], edx
005C0F45  mov     eax, 1
005C0F4A  test    eax, eax
005C0F4C  jz      short loc_5C0F59
005C0F4E  cmp     [ebp+var_20], 0
005C0F52  jnz     short loc_5C0F59
005C0F54  call    __invalid_parameter_noinfo
005C0F59  mov     eax, [ebp+var_20]
005C0F5C  lea     edx, [ebp+var_10]
005C0F5F  call    sub_408680
005C0F64  lea     ecx, [ebp+var_10]
005C0F67  push    ecx
005C0F68  lea     ecx, [ebp+var_8]
005C0F6B  call    sub_632700
005C0F70  movzx   edx, al
005C0F73  neg     edx
005C0F75  sbb     edx, edx
005C0F77  add     edx, 1
005C0F7A  movzx   eax, dl
005C0F7D  test    eax, eax
005C0F7F  jz      short loc_5C0FC2
005C0F81  lea     ecx, [ebp+var_8]
005C0F84  call    sub_5B7240
005C0F89  mov     [ebp+var_24], eax
005C0F8C  mov     ecx, [ebp+var_24]
005C0F8F  mov     edx, [ecx]
005C0F91  mov     byte ptr [edx+3Ch], 1
005C0F95  lea     ecx, [ebp+var_8]
005C0F98  call    sub_5B7240
005C0F9D  mov     [ebp+var_28], eax
005C0FA0  mov     eax, [ebp+var_28]
005C0FA3  mov     ecx, [eax]
005C0FA5  mov     [ebp+var_34], ecx
005C0FA8  mov     edx, [ebp+var_34]
005C0FAB  mov     eax, [edx]
005C0FAD  mov     ecx, [ebp+var_34]
005C0FB0  mov     edx, [eax+28h]
005C0FB3  call    edx
005C0FB5  lea     ecx, [ebp+var_8]
005C0FB8  call    sub_5D3980
005C0FBD  jmp     loc_5C0F25
005C0FC2  mov     esp, ebp
005C0FC4  pop     ebp
005C0FC5  retn
