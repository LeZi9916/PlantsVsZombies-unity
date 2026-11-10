005D1090  push    ebp
005D1091  mov     ebp, esp
005D1093  push    0FFFFFFFFh
005D1095  push    offset SEH_5D1090
005D109A  mov     eax, large fs:0
005D10A0  push    eax
005D10A1  push    ecx
005D10A2  mov     eax, ___security_cookie
005D10A7  xor     eax, ebp
005D10A9  push    eax
005D10AA  lea     eax, [ebp+var_C]
005D10AD  mov     large fs:0, eax
005D10B3  mov     [ebp+var_10], ecx
005D10B6  mov     eax, [ebp+arg_8]
005D10B9  push    eax
005D10BA  mov     ecx, [ebp+arg_4]
005D10BD  push    ecx
005D10BE  mov     ecx, [ebp+var_10]
005D10C1  call    sub_5CE470
005D10C6  mov     [ebp+var_4], 0
005D10CD  mov     edx, [ebp+var_10]
005D10D0  mov     dword ptr [edx], offset ??_7DialogButton@Sexy@@6B@; const Sexy::DialogButton::`vftable'
005D10D6  mov     eax, [ebp+var_10]
005D10D9  mov     ecx, [ebp+arg_0]
005D10DC  mov     [eax+138h], ecx
005D10E2  mov     edx, [ebp+var_10]
005D10E5  mov     dword ptr [edx+148h], 0
005D10EF  mov     eax, [ebp+var_10]
005D10F2  mov     dword ptr [eax+144h], 0
005D10FC  mov     ecx, [ebp+var_10]
005D10FF  mov     dword ptr [ecx+140h], 1
005D1109  mov     edx, [ebp+var_10]
005D110C  mov     dword ptr [edx+13Ch], 1
005D1116  mov     eax, [ebp+var_10]
005D1119  mov     byte ptr [eax+94h], 1
005D1120  push    6
005D1122  push    offset unk_747B20
005D1127  mov     ecx, [ebp+var_10]
005D112A  call    sub_5C4D90
005D112F  mov     [ebp+var_4], 0FFFFFFFFh
005D1136  mov     eax, [ebp+var_10]
005D1139  mov     ecx, [ebp+var_C]
005D113C  mov     large fs:0, ecx
005D1143  pop     ecx
005D1144  mov     esp, ebp
005D1146  pop     ebp
005D1147  retn    0Ch
006E7A30  mov     ecx, [ebp+var_10]
006E7A33  jmp     sub_5CE5D0
006E7A38  mov     edx, [esp-4+arg_4]
006E7A3C  lea     eax, [edx+0Ch]
006E7A3F  mov     ecx, [edx-8]
006E7A42  xor     ecx, eax; StackCookie
006E7A44  call    @__security_check_cookie@4; __security_check_cookie(x)
006E7A49  mov     eax, offset stru_73A420
006E7A4E  jmp     ___CxxFrameHandler3
