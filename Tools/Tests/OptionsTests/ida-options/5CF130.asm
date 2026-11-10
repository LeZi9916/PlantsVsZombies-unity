005CF130  push    ebp
005CF131  mov     ebp, esp
005CF133  push    ecx
005CF134  push    esi
005CF135  push    edi
005CF136  mov     [ebp+var_4], ecx
005CF139  mov     ecx, [ebp+var_4]
005CF13C  call    unknown_libname_30; Microsoft VisualC 2-14/net runtime
005CF141  mov     eax, [ebp+var_4]
005CF144  fldz
005CF146  fcomp   qword ptr [eax+130h]
005CF14C  fnstsw  ax
005CF14E  test    ah, 44h
005CF151  jp      short loc_5CF170
005CF153  mov     ecx, [ebp+var_4]
005CF156  fldz
005CF158  fcomp   qword ptr [ecx+120h]
005CF15E  fnstsw  ax
005CF160  test    ah, 5
005CF163  jp      short loc_5CF170
005CF165  mov     edx, [ebp+var_4]
005CF168  fldz
005CF16A  fstp    qword ptr [edx+120h]
005CF170  mov     eax, [ebp+var_4]
005CF173  movzx   ecx, byte ptr [eax+68h]
005CF177  test    ecx, ecx
005CF179  jnz     short loc_5CF1CC
005CF17B  mov     edx, [ebp+var_4]
005CF17E  add     edx, 0E8h
005CF184  push    edx
005CF185  mov     eax, [ebp+var_4]
005CF188  mov     ecx, [eax+0CCh]
005CF18E  push    ecx
005CF18F  mov     ecx, [ebp+var_4]
005CF192  call    sub_5CE720
005CF197  movzx   edx, al
005CF19A  test    edx, edx
005CF19C  jnz     short loc_5CF1CC
005CF19E  mov     esi, [ebp+var_4]
005CF1A1  add     esi, 6Ch ; 'l'
005CF1A4  xor     edi, edi
005CF1A6  call    sub_43B0D0
005CF1AB  push    eax
005CF1AC  mov     esi, [ebp+var_4]
005CF1AF  add     esi, 6Ch ; 'l'
005CF1B2  mov     edi, 1
005CF1B7  call    sub_43B0D0
005CF1BC  push    eax
005CF1BD  call    sub_55BCF0
005CF1C2  add     esp, 8
005CF1C5  movzx   eax, al
005CF1C8  test    eax, eax
005CF1CA  jz      short loc_5CF1D9
005CF1CC  mov     ecx, [ebp+var_4]
005CF1CF  mov     edx, [ecx]
005CF1D1  mov     ecx, [ebp+var_4]
005CF1D4  mov     eax, [edx+44h]
005CF1D7  call    eax
005CF1D9  mov     ecx, [ebp+var_4]
005CF1DC  mov     edx, [ecx+0A0h]
005CF1E2  push    edx
005CF1E3  mov     eax, [ebp+var_4]
005CF1E6  mov     ecx, [eax+11Ch]
005CF1EC  mov     edx, [ebp+var_4]
005CF1EF  mov     eax, [edx+11Ch]
005CF1F5  mov     edx, [ecx]
005CF1F7  mov     ecx, eax
005CF1F9  mov     eax, [edx+10h]
005CF1FC  call    eax
005CF1FE  pop     edi
005CF1FF  pop     esi
005CF200  mov     esp, ebp
005CF202  pop     ebp
005CF203  retn
