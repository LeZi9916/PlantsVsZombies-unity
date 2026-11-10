005CE720  push    ebp
005CE721  mov     ebp, esp
005CE723  sub     esp, 8
005CE726  mov     [ebp+var_4], ecx
005CE729  cmp     [ebp+arg_0], 0
005CE72D  jnz     short loc_5CE741
005CE72F  mov     eax, [ebp+arg_4]
005CE732  cmp     dword ptr [eax+8], 0
005CE736  jnz     short loc_5CE741
005CE738  mov     [ebp+var_8], 0
005CE73F  jmp     short loc_5CE748
005CE741  mov     [ebp+var_8], 1
005CE748  mov     al, byte ptr [ebp+var_8]
005CE74B  mov     esp, ebp
005CE74D  pop     ebp
005CE74E  retn    8
