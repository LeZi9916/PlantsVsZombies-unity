005C4F90  push    ebp
005C4F91  mov     ebp, esp
005C4F93  sub     esp, 94h
005C4F99  push    ebx
005C4F9A  push    esi
005C4F9B  push    edi
005C4F9C  mov     [ebp+var_94], ecx
005C4FA2  mov     ecx, [ebp+var_94]
005C4FA8  add     ecx, 6Ch ; 'l'
005C4FAB  call    sub_43B1D0
005C4FB0  cmp     [ebp+arg_0], eax
005C4FB3  jl      short loc_5C4FED
005C4FB5  lea     ecx, [ebp+var_10]
005C4FB8  call    sub_55BAB0
005C4FBD  sub     esp, 10h
005C4FC0  mov     ecx, esp
005C4FC2  mov     edx, [eax]
005C4FC4  mov     [ecx], edx
005C4FC6  mov     edx, [eax+4]
005C4FC9  mov     [ecx+4], edx
005C4FCC  mov     edx, [eax+8]
005C4FCF  mov     [ecx+8], edx
005C4FD2  mov     eax, [eax+0Ch]
005C4FD5  mov     [ecx+0Ch], eax
005C4FD8  mov     ecx, [ebp+arg_0]
005C4FDB  add     ecx, 1
005C4FDE  push    ecx
005C4FDF  mov     ecx, [ebp+var_94]
005C4FE5  add     ecx, 6Ch ; 'l'
005C4FE8  call    sub_5D2D90
005C4FED  mov     ebx, [ebp+arg_4]
005C4FF0  mov     esi, [ebp+var_94]
005C4FF6  add     esi, 6Ch ; 'l'
005C4FF9  mov     edi, [ebp+arg_0]
005C4FFC  call    sub_43B0D0
005C5001  mov     edx, [ebx]
005C5003  mov     [eax], edx
005C5005  mov     ecx, [ebx+4]
005C5008  mov     [eax+4], ecx
005C500B  mov     edx, [ebx+8]
005C500E  mov     [eax+8], edx
005C5011  mov     ecx, [ebx+0Ch]
005C5014  mov     [eax+0Ch], ecx
005C5017  mov     edx, [ebp+var_94]
005C501D  mov     eax, [edx]
005C501F  mov     ecx, [ebp+var_94]
005C5025  mov     edx, [eax+44h]
005C5028  call    edx
005C502A  pop     edi
005C502B  pop     esi
005C502C  pop     ebx
005C502D  mov     esp, ebp
005C502F  pop     ebp
005C5030  retn    8
