005CE3E0  push    ebp
005CE3E1  mov     ebp, esp
005CE3E3  push    ecx
005CE3E4  mov     [ebp+var_4], ecx
005CE3E7  mov     eax, [ebp+arg_C]
005CE3EA  push    eax
005CE3EB  mov     ecx, [ebp+arg_8]
005CE3EE  push    ecx
005CE3EF  mov     edx, [ebp+arg_4]
005CE3F2  push    edx
005CE3F3  mov     eax, [ebp+arg_0]
005CE3F6  push    eax
005CE3F7  mov     ecx, [ebp+var_4]
005CE3FA  call    sub_5C54C0
005CE3FF  mov     ecx, [ebp+var_4]
005CE402  movzx   edx, byte ptr [ecx+0A8h]
005CE409  neg     edx
005CE40B  sbb     edx, edx
005CE40D  add     edx, 1
005CE410  mov     eax, [ebp+var_4]
005CE413  mov     [eax+0A8h], dl
005CE419  mov     ecx, [ebp+var_4]
005CE41C  cmp     dword ptr [ecx+0A0h], 0
005CE423  jz      short loc_5CE454
005CE425  mov     edx, [ebp+var_4]
005CE428  movzx   eax, byte ptr [edx+0A8h]
005CE42F  push    eax
005CE430  mov     ecx, [ebp+var_4]
005CE433  mov     edx, [ecx+0A4h]
005CE439  push    edx
005CE43A  mov     eax, [ebp+var_4]
005CE43D  mov     ecx, [eax+0A0h]
005CE443  mov     edx, [ebp+var_4]
005CE446  mov     eax, [edx+0A0h]
005CE44C  mov     edx, [ecx]
005CE44E  mov     ecx, eax
005CE450  mov     eax, [edx]
005CE452  call    eax
005CE454  mov     ecx, [ebp+var_4]
005CE457  mov     edx, [ecx]
005CE459  mov     ecx, [ebp+var_4]
005CE45C  mov     eax, [edx+44h]
005CE45F  call    eax
005CE461  mov     esp, ebp
005CE463  pop     ebp
005CE464  retn    10h
