005CE5D0  push    ebp
005CE5D1  mov     ebp, esp
005CE5D3  push    0FFFFFFFFh
005CE5D5  push    offset SEH_5CE5D0
005CE5DA  mov     eax, large fs:0
005CE5E0  push    eax
005CE5E1  sub     esp, 18h
005CE5E4  mov     eax, ___security_cookie
005CE5E9  xor     eax, ebp
005CE5EB  push    eax
005CE5EC  lea     eax, [ebp+var_C]
005CE5EF  mov     large fs:0, eax
005CE5F5  mov     [ebp+var_20], ecx
005CE5F8  mov     eax, [ebp+var_20]
005CE5FB  mov     dword ptr [eax], offset ??_7ButtonWidget@Sexy@@6B@; const Sexy::ButtonWidget::`vftable'
005CE601  mov     [ebp+var_4], 1
005CE608  mov     ecx, [ebp+var_20]
005CE60B  mov     edx, [ecx+0C4h]
005CE611  mov     [ebp+var_14], edx
005CE614  mov     eax, [ebp+var_14]
005CE617  mov     [ebp+var_10], eax
005CE61A  cmp     [ebp+var_10], 0
005CE61E  jz      short loc_5CE633
005CE620  push    1
005CE622  mov     ecx, [ebp+var_10]
005CE625  mov     edx, [ecx]
005CE627  mov     ecx, [ebp+var_10]
005CE62A  mov     eax, [edx]
005CE62C  call    eax
005CE62E  mov     [ebp+var_24], eax
005CE631  jmp     short loc_5CE63A
005CE633  mov     [ebp+var_24], 0
005CE63A  mov     byte ptr [ebp+var_4], 0
005CE63E  mov     ecx, [ebp+var_20]
005CE641  add     ecx, 0A4h; void *
005CE647  call    sub_401330
005CE64C  mov     [ebp+var_4], 0FFFFFFFFh
005CE653  mov     ecx, [ebp+var_20]
005CE656  call    sub_5C4A90
005CE65B  mov     ecx, [ebp+var_C]
005CE65E  mov     large fs:0, ecx
005CE665  pop     ecx
005CE666  mov     esp, ebp
005CE668  pop     ebp
005CE669  retn
006DDC20  mov     ecx, [ebp+var_20]
006DDC23  jmp     sub_5C4A90
006DDC28  mov     ecx, [ebp+var_20]
006DDC2B  add     ecx, 0A4h; void *
006DDC31  jmp     sub_401330
006DDC36  mov     edx, [esp-4+arg_4]
006DDC3A  lea     eax, [edx+0Ch]
006DDC3D  mov     ecx, [edx-1Ch]
006DDC40  xor     ecx, eax; StackCookie
006DDC42  call    @__security_check_cookie@4; __security_check_cookie(x)
006DDC47  mov     eax, offset stru_72FC70
006DDC4C  jmp     ___CxxFrameHandler3
