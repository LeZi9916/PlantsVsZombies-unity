005C5DF0  push    ebp
005C5DF1  mov     ebp, esp
005C5DF3  push    ecx
005C5DF4  mov     [ebp+var_4], ecx
005C5DF7  mov     ecx, [ebp+var_4]
005C5DFA  call    sub_5C4970
005C5DFF  mov     eax, [ebp+var_4]
005C5E02  mov     dword ptr [eax], offset ??_7Slider@Sexy@@6B@; const Sexy::Slider::`vftable'
005C5E08  mov     ecx, [ebp+var_4]
005C5E0B  mov     edx, [ebp+arg_C]
005C5E0E  mov     [ecx+0A0h], edx
005C5E14  mov     eax, [ebp+var_4]
005C5E17  fldz
005C5E19  fstp    qword ptr [eax+0A8h]
005C5E1F  mov     ecx, [ebp+var_4]
005C5E22  mov     edx, [ebp+arg_8]
005C5E25  mov     [ecx+0B0h], edx
005C5E2B  mov     eax, [ebp+var_4]
005C5E2E  mov     ecx, [ebp+arg_0]
005C5E31  mov     [eax+0B4h], ecx
005C5E37  mov     edx, [ebp+var_4]
005C5E3A  mov     eax, [ebp+arg_4]
005C5E3D  mov     [edx+0B8h], eax
005C5E43  mov     ecx, [ebp+var_4]
005C5E46  mov     byte ptr [ecx+0BCh], 0
005C5E4D  mov     edx, [ebp+var_4]
005C5E50  mov     byte ptr [edx+0C8h], 1
005C5E57  mov     eax, [ebp+var_4]
005C5E5A  mov     dword ptr [eax+0C4h], 0
005C5E64  mov     ecx, [ebp+var_4]
005C5E67  mov     dword ptr [ecx+0C0h], 0
005C5E71  mov     eax, [ebp+var_4]
005C5E74  mov     esp, ebp
005C5E76  pop     ebp
005C5E77  retn    10h
