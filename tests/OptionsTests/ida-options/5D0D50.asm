005D0D50  push    ebp
005D0D51  mov     ebp, esp
005D0D53  sub     esp, 0Ch
005D0D56  mov     [ebp+var_C], ecx
005D0D59  mov     eax, [ebp+var_C]
005D0D5C  movzx   ecx, byte ptr [eax+148h]
005D0D63  test    ecx, ecx
005D0D65  jz      loc_5D0ED3
005D0D6B  mov     edx, [ebp+var_C]
005D0D6E  mov     eax, [edx+40h]
005D0D71  add     eax, [ebp+arg_0]
005D0D74  mov     ecx, [ebp+var_C]
005D0D77  sub     eax, [ecx+14Ch]
005D0D7D  mov     [ebp+var_8], eax
005D0D80  mov     edx, [ebp+var_C]
005D0D83  mov     eax, [edx+44h]
005D0D86  add     eax, [ebp+arg_4]
005D0D89  mov     ecx, [ebp+var_C]
005D0D8C  sub     eax, [ecx+150h]
005D0D92  mov     [ebp+var_4], eax
005D0D95  cmp     [ebp+var_8], 0FFFFFFF8h
005D0D99  jge     short loc_5D0DA4
005D0D9B  mov     [ebp+var_8], 0FFFFFFF8h
005D0DA2  jmp     short loc_5D0DD2
005D0DA4  mov     edx, [ebp+var_C]
005D0DA7  mov     eax, [ebp+var_8]
005D0DAA  add     eax, [edx+48h]
005D0DAD  mov     ecx, [ebp+var_C]
005D0DB0  mov     edx, [ecx+20h]
005D0DB3  mov     ecx, [edx+48h]
005D0DB6  add     ecx, 8
005D0DB9  cmp     eax, ecx
005D0DBB  jle     short loc_5D0DD2
005D0DBD  mov     edx, [ebp+var_C]
005D0DC0  mov     eax, [edx+20h]
005D0DC3  mov     ecx, [ebp+var_C]
005D0DC6  mov     edx, [eax+48h]
005D0DC9  sub     edx, [ecx+48h]
005D0DCC  add     edx, 8
005D0DCF  mov     [ebp+var_8], edx
005D0DD2  cmp     [ebp+var_4], 0FFFFFFF8h
005D0DD6  jge     short loc_5D0DE1
005D0DD8  mov     [ebp+var_4], 0FFFFFFF8h
005D0DDF  jmp     short loc_5D0E0F
005D0DE1  mov     eax, [ebp+var_C]
005D0DE4  mov     ecx, [ebp+var_4]
005D0DE7  add     ecx, [eax+4Ch]
005D0DEA  mov     edx, [ebp+var_C]
005D0DED  mov     eax, [edx+20h]
005D0DF0  mov     edx, [eax+4Ch]
005D0DF3  add     edx, 8
005D0DF6  cmp     ecx, edx
005D0DF8  jle     short loc_5D0E0F
005D0DFA  mov     eax, [ebp+var_C]
005D0DFD  mov     ecx, [eax+20h]
005D0E00  mov     edx, [ebp+var_C]
005D0E03  mov     eax, [ecx+4Ch]
005D0E06  sub     eax, [edx+4Ch]
005D0E09  add     eax, 8
005D0E0C  mov     [ebp+var_4], eax
005D0E0F  mov     ecx, [ebp+var_C]
005D0E12  mov     edx, [ecx+40h]
005D0E15  add     edx, [ebp+arg_0]
005D0E18  sub     edx, [ebp+var_8]
005D0E1B  mov     eax, [ebp+var_C]
005D0E1E  mov     [eax+14Ch], edx
005D0E24  mov     ecx, [ebp+var_C]
005D0E27  mov     edx, [ecx+44h]
005D0E2A  add     edx, [ebp+arg_4]
005D0E2D  sub     edx, [ebp+var_4]
005D0E30  mov     eax, [ebp+var_C]
005D0E33  mov     [eax+150h], edx
005D0E39  mov     ecx, [ebp+var_C]
005D0E3C  cmp     dword ptr [ecx+14Ch], 8
005D0E43  jge     short loc_5D0E54
005D0E45  mov     edx, [ebp+var_C]
005D0E48  mov     dword ptr [edx+14Ch], 8
005D0E52  jmp     short loc_5D0E7A
005D0E54  mov     eax, [ebp+var_C]
005D0E57  mov     ecx, [eax+48h]
005D0E5A  sub     ecx, 9
005D0E5D  mov     edx, [ebp+var_C]
005D0E60  cmp     [edx+14Ch], ecx
005D0E66  jle     short loc_5D0E7A
005D0E68  mov     eax, [ebp+var_C]
005D0E6B  mov     ecx, [eax+48h]
005D0E6E  sub     ecx, 9
005D0E71  mov     edx, [ebp+var_C]
005D0E74  mov     [edx+14Ch], ecx
005D0E7A  mov     eax, [ebp+var_C]
005D0E7D  cmp     dword ptr [eax+150h], 8
005D0E84  jge     short loc_5D0E95
005D0E86  mov     ecx, [ebp+var_C]
005D0E89  mov     dword ptr [ecx+150h], 8
005D0E93  jmp     short loc_5D0EBB
005D0E95  mov     edx, [ebp+var_C]
005D0E98  mov     eax, [edx+4Ch]
005D0E9B  sub     eax, 9
005D0E9E  mov     ecx, [ebp+var_C]
005D0EA1  cmp     [ecx+150h], eax
005D0EA7  jle     short loc_5D0EBB
005D0EA9  mov     edx, [ebp+var_C]
005D0EAC  mov     eax, [edx+4Ch]
005D0EAF  sub     eax, 9
005D0EB2  mov     ecx, [ebp+var_C]
005D0EB5  mov     [ecx+150h], eax
005D0EBB  mov     edx, [ebp+var_4]
005D0EBE  push    edx
005D0EBF  mov     eax, [ebp+var_8]
005D0EC2  push    eax
005D0EC3  mov     ecx, [ebp+var_C]
005D0EC6  mov     edx, [ecx]
005D0EC8  mov     ecx, [ebp+var_C]
005D0ECB  mov     eax, [edx+0A4h]
005D0ED1  call    eax
005D0ED3  mov     esp, ebp
005D0ED5  pop     ebp
005D0ED6  retn    8
