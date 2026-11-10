005D17C0  push    ebp
005D17C1  mov     ebp, esp
005D17C3  push    0FFFFFFFFh
005D17C5  push    offset SEH_62F000
005D17CA  mov     eax, large fs:0
005D17D0  push    eax
005D17D1  sub     esp, 38h
005D17D4  mov     eax, ___security_cookie
005D17D9  xor     eax, ebp
005D17DB  push    eax
005D17DC  lea     eax, [ebp+var_C]
005D17DF  mov     large fs:0, eax
005D17E5  mov     [ebp+var_40], ecx
005D17E8  push    ecx
005D17E9  mov     eax, esp
005D17EB  mov     [ebp+var_10], esp
005D17EE  mov     [ebp+var_14], eax
005D17F1  mov     ecx, [ebp+var_40]
005D17F4  call    sub_605A50
005D17F9  mov     [ebp+var_44], eax
005D17FC  mov     [ebp+var_4], 0
005D1803  mov     ecx, [ebp+var_40]
005D1806  call    sub_5D2890
005D180B  mov     ecx, [ebp+var_40]
005D180E  mov     [ecx+14h], eax
005D1811  mov     edx, [ebp+var_40]
005D1814  mov     dword ptr [edx+18h], 0
005D181B  mov     [ebp+var_4], 0FFFFFFFFh
005D1822  mov     eax, [ebp+var_40]
005D1825  mov     ecx, [ebp+var_C]
005D1828  mov     large fs:0, ecx
005D182F  pop     ecx
005D1830  mov     esp, ebp
005D1832  pop     ebp
005D1833  retn
006DD230  mov     ecx, [ebp+var_40]
006DD233  jmp     sub_62FE00
006DD238  mov     edx, [esp-4+arg_4]
006DD23C  lea     eax, [edx+0Ch]
006DD23F  mov     ecx, [edx-3Ch]
006DD242  xor     ecx, eax; StackCookie
006DD244  call    @__security_check_cookie@4; __security_check_cookie(x)
006DD249  mov     eax, offset stru_72F004
006DD24E  jmp     ___CxxFrameHandler3
