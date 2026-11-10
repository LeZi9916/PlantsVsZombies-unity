005D0A80  push    ebp
005D0A81  mov     ebp, esp
005D0A83  push    ecx
005D0A84  mov     [ebp+var_4], ecx
005D0A87  mov     ecx, [ebp+var_4]
005D0A8A  call    unknown_libname_30; Microsoft VisualC 2-14/net runtime
005D0A8F  mov     eax, [ebp+var_4]
005D0A92  cmp     dword ptr [eax+0ACh], 0
005D0A99  jz      short loc_5D0ABC
005D0A9B  mov     ecx, [ebp+var_4]
005D0A9E  push    ecx
005D0A9F  mov     edx, [ebp+var_4]
005D0AA2  mov     eax, [edx+0ACh]
005D0AA8  push    eax
005D0AA9  mov     ecx, [ebp+var_4]
005D0AAC  mov     edx, [ecx+20h]
005D0AAF  mov     eax, [ebp+var_4]
005D0AB2  mov     ecx, [eax+20h]
005D0AB5  mov     edx, [edx]
005D0AB7  mov     eax, [edx+38h]
005D0ABA  call    eax
005D0ABC  mov     ecx, [ebp+var_4]
005D0ABF  cmp     dword ptr [ecx+0B0h], 0
005D0AC6  jz      short loc_5D0AE9
005D0AC8  mov     edx, [ebp+var_4]
005D0ACB  push    edx
005D0ACC  mov     eax, [ebp+var_4]
005D0ACF  mov     ecx, [eax+0B0h]
005D0AD5  push    ecx
005D0AD6  mov     edx, [ebp+var_4]
005D0AD9  mov     eax, [edx+20h]
005D0ADC  mov     ecx, [ebp+var_4]
005D0ADF  mov     ecx, [ecx+20h]
005D0AE2  mov     edx, [eax]
005D0AE4  mov     eax, [edx+38h]
005D0AE7  call    eax
005D0AE9  mov     esp, ebp
005D0AEB  pop     ebp
005D0AEC  retn
