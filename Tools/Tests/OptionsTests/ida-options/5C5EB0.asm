005C5EB0  push    ebp
005C5EB1  mov     ebp, esp
005C5EB3  push    ecx
005C5EB4  mov     [ebp+var_4], ecx
005C5EB7  mov     eax, [ebp+var_4]
005C5EBA  fld     [ebp+arg_0]
005C5EBD  fstp    qword ptr [eax+0A8h]
005C5EC3  mov     ecx, [ebp+var_4]
005C5EC6  fldz
005C5EC8  fcomp   qword ptr [ecx+0A8h]
005C5ECE  fnstsw  ax
005C5ED0  test    ah, 41h
005C5ED3  jnz     short loc_5C5EE2
005C5ED5  mov     edx, [ebp+var_4]
005C5ED8  fldz
005C5EDA  fstp    qword ptr [edx+0A8h]
005C5EE0  jmp     short loc_5C5EFF
005C5EE2  mov     eax, [ebp+var_4]
005C5EE5  fld1
005C5EE7  fcomp   qword ptr [eax+0A8h]
005C5EED  fnstsw  ax
005C5EEF  test    ah, 5
005C5EF2  jp      short loc_5C5EFF
005C5EF4  mov     ecx, [ebp+var_4]
005C5EF7  fld1
005C5EF9  fstp    qword ptr [ecx+0A8h]
005C5EFF  mov     edx, [ebp+var_4]
005C5F02  mov     eax, [edx]
005C5F04  mov     ecx, [ebp+var_4]
005C5F07  mov     edx, [eax+4Ch]
005C5F0A  call    edx
005C5F0C  mov     esp, ebp
005C5F0E  pop     ebp
005C5F0F  retn    8
