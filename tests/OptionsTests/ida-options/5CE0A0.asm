005CE0A0  push    ebp
005CE0A1  mov     ebp, esp
005CE0A3  push    ecx
005CE0A4  mov     [ebp+var_4], ecx
005CE0A7  mov     ecx, [ebp+var_4]
005CE0AA  call    sub_5C4970
005CE0AF  mov     eax, [ebp+var_4]
005CE0B2  mov     dword ptr [eax], offset ??_7Checkbox@Sexy@@6B@; const Sexy::Checkbox::`vftable'
005CE0B8  mov     ecx, [ebp+var_4]
005CE0BB  mov     edx, [ebp+arg_C]
005CE0BE  mov     [ecx+0A0h], edx
005CE0C4  mov     eax, [ebp+var_4]
005CE0C7  mov     ecx, [ebp+arg_8]
005CE0CA  mov     [eax+0A4h], ecx
005CE0D0  mov     edx, [ebp+var_4]
005CE0D3  mov     byte ptr [edx+0A8h], 0
005CE0DA  mov     eax, [ebp+var_4]
005CE0DD  mov     ecx, [ebp+arg_0]
005CE0E0  mov     [eax+0ACh], ecx
005CE0E6  mov     edx, [ebp+var_4]
005CE0E9  mov     eax, [ebp+arg_4]
005CE0EC  mov     [edx+0B0h], eax
005CE0F2  mov     eax, [ebp+var_4]
005CE0F5  add     eax, 0B4h
005CE0FA  call    sub_409210
005CE0FF  mov     eax, [ebp+var_4]
005CE102  add     eax, 0C4h
005CE107  call    sub_409210
005CE10C  mov     ecx, [ebp+var_4]
005CE10F  add     ecx, 0D4h
005CE115  mov     edx, dword_7DCA98
005CE11B  mov     [ecx], edx
005CE11D  mov     eax, dword_7DCA9C
005CE122  mov     [ecx+4], eax
005CE125  mov     edx, dword_7DCAA0
005CE12B  mov     [ecx+8], edx
005CE12E  mov     eax, dword_7DCAA4
005CE133  mov     [ecx+0Ch], eax
005CE136  push    50h ; 'P'
005CE138  push    50h ; 'P'
005CE13A  push    50h ; 'P'
005CE13C  mov     ecx, [ebp+var_4]
005CE13F  add     ecx, 0E4h
005CE145  call    sub_55BBB0
005CE14A  push    0
005CE14C  push    0FFh
005CE151  push    0FFh
005CE156  mov     ecx, [ebp+var_4]
005CE159  add     ecx, 0F4h
005CE15F  call    sub_55BBB0
005CE164  mov     ecx, [ebp+var_4]
005CE167  mov     byte ptr [ecx+94h], 1
005CE16E  mov     eax, [ebp+var_4]
005CE171  mov     esp, ebp
005CE173  pop     ebp
005CE174  retn    10h
