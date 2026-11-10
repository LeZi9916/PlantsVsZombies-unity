005C05F0  push    ebp
005C05F1  mov     ebp, esp
005C05F3  sub     esp, 28h
005C05F6  mov     [ebp+var_24], ecx
005C05F9  mov     eax, [ebp+var_24]
005C05FC  xor     ecx, ecx
005C05FE  cmp     dword ptr [eax+1Ch], 0
005C0602  setz    cl
005C0605  movzx   edx, cl
005C0608  test    edx, edx
005C060A  jnz     loc_5C0698
005C0610  lea     eax, [ebp+var_14]
005C0613  push    eax
005C0614  mov     ecx, [ebp+var_24]
005C0617  add     ecx, 4
005C061A  call    sub_62EFA0
005C061F  mov     [ebp+var_20], eax
005C0622  mov     ecx, [ebp+var_20]
005C0625  call    sub_5B7240
005C062A  mov     ecx, [eax]
005C062C  mov     [ebp+var_4], ecx
005C062F  mov     edx, [ebp+var_4]
005C0632  push    edx
005C0633  mov     eax, [ebp+var_24]
005C0636  mov     edx, [eax]
005C0638  mov     ecx, [ebp+var_24]
005C063B  mov     eax, [edx+10h]
005C063E  call    eax
005C0640  movzx   ecx, [ebp+arg_4]
005C0644  test    ecx, ecx
005C0646  jz      short loc_5C065F
005C0648  movzx   edx, [ebp+arg_4]
005C064C  push    edx
005C064D  movzx   eax, [ebp+arg_0]
005C0651  push    eax
005C0652  mov     ecx, [ebp+var_4]
005C0655  mov     edx, [ecx]
005C0657  mov     ecx, [ebp+var_4]
005C065A  mov     eax, [edx+1Ch]
005C065D  call    eax
005C065F  movzx   ecx, [ebp+arg_0]
005C0663  test    ecx, ecx
005C0665  jz      short loc_5C0693
005C0667  mov     edx, [ebp+var_4]
005C066A  mov     [ebp+var_C], edx
005C066D  mov     eax, [ebp+var_C]
005C0670  mov     [ebp+var_8], eax
005C0673  cmp     [ebp+var_8], 0
005C0677  jz      short loc_5C068C
005C0679  push    1
005C067B  mov     ecx, [ebp+var_8]
005C067E  mov     edx, [ecx]
005C0680  mov     ecx, [ebp+var_8]
005C0683  mov     eax, [edx]
005C0685  call    eax
005C0687  mov     [ebp+var_28], eax
005C068A  jmp     short loc_5C0693
005C068C  mov     [ebp+var_28], 0
005C0693  jmp     loc_5C05F9
005C0698  mov     esp, ebp
005C069A  pop     ebp
005C069B  retn    8
