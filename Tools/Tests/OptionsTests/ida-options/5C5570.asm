005C5570  push    ebp
005C5571  mov     ebp, esp
005C5573  sub     esp, 10h
005C5576  mov     [ebp+var_10], ecx
005C5579  mov     ecx, [ebp+arg_4]
005C557C  call    unknown_libname_8; Microsoft VisualC 2-14/net runtime
005C5581  mov     [ebp+var_4], eax
005C5584  mov     eax, [ebp+arg_C]
005C5587  push    eax
005C5588  mov     ecx, [ebp+var_4]
005C558B  mov     edx, [ecx]
005C558D  mov     ecx, [ebp+var_4]
005C5590  mov     eax, [edx+1Ch]
005C5593  call    eax
005C5595  mov     [ebp+var_8], eax
005C5598  mov     ecx, [ebp+var_10]
005C559B  mov     eax, [ecx+48h]
005C559E  sub     eax, [ebp+var_8]
005C55A1  cdq
005C55A2  sub     eax, edx
005C55A4  sar     eax, 1
005C55A6  mov     [ebp+var_C], eax
005C55A9  mov     edx, [ebp+arg_8]
005C55AC  push    edx
005C55AD  mov     eax, [ebp+var_C]
005C55B0  push    eax
005C55B1  mov     ecx, [ebp+arg_C]
005C55B4  push    ecx
005C55B5  mov     ecx, [ebp+arg_4]
005C55B8  call    sub_592F40
005C55BD  mov     edx, [ebp+var_4]
005C55C0  mov     eax, [edx]
005C55C2  mov     ecx, [ebp+var_4]
005C55C5  mov     edx, [eax+10h]
005C55C8  call    edx
005C55CA  push    eax
005C55CB  mov     eax, [ebp+var_8]
005C55CE  push    eax
005C55CF  mov     ecx, [ebp+var_4]
005C55D2  mov     edx, [ecx]
005C55D4  mov     ecx, [ebp+var_4]
005C55D7  mov     eax, [edx+4]
005C55DA  call    eax
005C55DC  mov     edx, [ebp+arg_8]
005C55DF  sub     edx, eax
005C55E1  mov     ecx, [ebp+var_C]
005C55E4  mov     eax, [ebp+arg_0]
005C55E7  call    sub_4091F0
005C55EC  mov     eax, [ebp+arg_0]
005C55EF  mov     esp, ebp
005C55F1  pop     ebp
005C55F2  retn    10h
