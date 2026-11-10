005F0020  push    ebp
005F0021  mov     ebp, esp
005F0023  sub     esp, 12Ch
005F0029  mov     eax, ___security_cookie
005F002E  xor     eax, ebp
005F0030  mov     [ebp+var_4], eax
005F0033  mov     [ebp+var_10C], 0
005F003D  push    100h; SizeInBytes
005F0042  lea     eax, [ebp+DstBuf]
005F0048  push    eax; DstBuf
005F0049  call    __getcwd
005F004E  add     esp, 8
005F0051  push    eax; Source
005F0052  mov     ecx, [ebp+arg_0]
005F0055  call    sub_4033F0
005F005A  mov     ecx, [ebp+var_10C]
005F0060  or      ecx, 1
005F0063  mov     [ebp+var_10C], ecx
005F0069  mov     eax, [ebp+arg_0]
005F006C  mov     ecx, [ebp+var_4]
005F006F  xor     ecx, ebp; StackCookie
005F0071  call    @__security_check_cookie@4; __security_check_cookie(x)
005F0076  mov     esp, ebp
005F0078  pop     ebp
005F0079  retn
