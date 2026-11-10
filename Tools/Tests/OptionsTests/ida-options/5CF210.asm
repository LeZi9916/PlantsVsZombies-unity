005CF210  push    ebp
005CF211  mov     ebp, esp
005CF213  push    ecx
005CF214  push    esi
005CF215  push    edi
005CF216  mov     [ebp+var_4], ecx
005CF219  mov     ecx, [ebp+var_4]
005CF21C  call    unknown_libname_30; Microsoft VisualC 2-14/net runtime
005CF221  mov     eax, [ebp+var_4]
005CF224  fldz
005CF226  fcomp   qword ptr [eax+128h]
005CF22C  fnstsw  ax
005CF22E  test    ah, 44h
005CF231  jp      short loc_5CF252
005CF233  mov     ecx, [ebp+var_4]
005CF236  fldz
005CF238  fcomp   qword ptr [ecx+120h]
005CF23E  fnstsw  ax
005CF240  test    ah, 5
005CF243  jp      short loc_5CF252
005CF245  mov     edx, [ebp+var_4]
005CF248  fldz
005CF24A  fstp    qword ptr [edx+120h]
005CF250  jmp     short loc_5CF281
005CF252  mov     eax, [ebp+var_4]
005CF255  fldz
005CF257  fcomp   qword ptr [eax+128h]
005CF25D  fnstsw  ax
005CF25F  test    ah, 5
005CF262  jp      short loc_5CF281
005CF264  mov     ecx, [ebp+var_4]
005CF267  fldz
005CF269  fcomp   qword ptr [ecx+120h]
005CF26F  fnstsw  ax
005CF271  test    ah, 44h
005CF274  jp      short loc_5CF281
005CF276  mov     edx, [ebp+var_4]
005CF279  fld1
005CF27B  fstp    qword ptr [edx+120h]
005CF281  mov     eax, [ebp+var_4]
005CF284  movzx   ecx, byte ptr [eax+68h]
005CF288  test    ecx, ecx
005CF28A  jnz     short loc_5CF2DD
005CF28C  mov     edx, [ebp+var_4]
005CF28F  add     edx, 0E8h
005CF295  push    edx
005CF296  mov     eax, [ebp+var_4]
005CF299  mov     ecx, [eax+0CCh]
005CF29F  push    ecx
005CF2A0  mov     ecx, [ebp+var_4]
005CF2A3  call    sub_5CE720
005CF2A8  movzx   edx, al
005CF2AB  test    edx, edx
005CF2AD  jnz     short loc_5CF2DD
005CF2AF  mov     esi, [ebp+var_4]
005CF2B2  add     esi, 6Ch ; 'l'
005CF2B5  xor     edi, edi
005CF2B7  call    sub_43B0D0
005CF2BC  push    eax
005CF2BD  mov     esi, [ebp+var_4]
005CF2C0  add     esi, 6Ch ; 'l'
005CF2C3  mov     edi, 1
005CF2C8  call    sub_43B0D0
005CF2CD  push    eax
005CF2CE  call    sub_55BCF0
005CF2D3  add     esp, 8
005CF2D6  movzx   eax, al
005CF2D9  test    eax, eax
005CF2DB  jz      short loc_5CF2EA
005CF2DD  mov     ecx, [ebp+var_4]
005CF2E0  mov     edx, [ecx]
005CF2E2  mov     ecx, [ebp+var_4]
005CF2E5  mov     eax, [edx+44h]
005CF2E8  call    eax
005CF2EA  mov     ecx, [ebp+var_4]
005CF2ED  mov     edx, [ecx+0A0h]
005CF2F3  push    edx
005CF2F4  mov     eax, [ebp+var_4]
005CF2F7  mov     ecx, [eax+11Ch]
005CF2FD  mov     edx, [ebp+var_4]
005CF300  mov     eax, [edx+11Ch]
005CF306  mov     edx, [ecx]
005CF308  mov     ecx, eax
005CF30A  mov     eax, [edx+14h]
005CF30D  call    eax
005CF30F  pop     edi
005CF310  pop     esi
005CF311  mov     esp, ebp
005CF313  pop     ebp
005CF314  retn
