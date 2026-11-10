005CE470  push    ebp
005CE471  mov     ebp, esp
005CE473  push    0FFFFFFFFh
005CE475  push    offset SEH_5CE470
005CE47A  mov     eax, large fs:0
005CE480  push    eax
005CE481  sub     esp, 10h
005CE484  mov     eax, ___security_cookie
005CE489  xor     eax, ebp
005CE48B  push    eax
005CE48C  lea     eax, [ebp+var_C]
005CE48F  mov     large fs:0, eax
005CE495  mov     [ebp+var_1C], ecx
005CE498  mov     ecx, [ebp+var_1C]
005CE49B  call    sub_5C4970
005CE4A0  mov     [ebp+var_4], 0
005CE4A7  mov     eax, [ebp+var_1C]
005CE4AA  mov     dword ptr [eax], offset ??_7ButtonWidget@Sexy@@6B@; const Sexy::ButtonWidget::`vftable'
005CE4B0  mov     ecx, [ebp+var_1C]
005CE4B3  add     ecx, 0A4h; void *
005CE4B9  call    sub_401310
005CE4BE  mov     byte ptr [ebp+var_4], 1
005CE4C2  mov     eax, [ebp+var_1C]
005CE4C5  add     eax, 0D8h
005CE4CA  call    sub_409210
005CE4CF  mov     eax, [ebp+var_1C]
005CE4D2  add     eax, 0E8h
005CE4D7  call    sub_409210
005CE4DC  mov     eax, [ebp+var_1C]
005CE4DF  add     eax, 0F8h
005CE4E4  call    sub_409210
005CE4E9  mov     eax, [ebp+var_1C]
005CE4EC  add     eax, 108h
005CE4F1  call    sub_409210
005CE4F6  mov     ecx, [ebp+var_1C]
005CE4F9  mov     edx, [ebp+arg_0]
005CE4FC  mov     [ecx+0A0h], edx
005CE502  mov     eax, [ebp+var_1C]
005CE505  mov     dword ptr [eax+0C4h], 0
005CE50F  mov     ecx, [ebp+var_1C]
005CE512  mov     dword ptr [ecx+0C0h], 0
005CE51C  mov     edx, [ebp+var_1C]
005CE51F  mov     dword ptr [edx+0C8h], 0
005CE529  mov     eax, [ebp+var_1C]
005CE52C  mov     dword ptr [eax+0CCh], 0
005CE536  mov     ecx, [ebp+var_1C]
005CE539  mov     dword ptr [ecx+0D0h], 0
005CE543  mov     edx, [ebp+var_1C]
005CE546  mov     dword ptr [edx+0D4h], 0
005CE550  mov     eax, [ebp+var_1C]
005CE553  mov     byte ptr [eax+118h], 0
005CE55A  mov     ecx, [ebp+var_1C]
005CE55D  mov     byte ptr [ecx+119h], 0
005CE564  mov     edx, [ebp+var_1C]
005CE567  mov     byte ptr [edx+11Ah], 0
005CE56E  mov     eax, [ebp+var_1C]
005CE571  mov     ecx, [ebp+arg_4]
005CE574  mov     [eax+11Ch], ecx
005CE57A  mov     edx, [ebp+var_1C]
005CE57D  mov     byte ptr [edx+50h], 1
005CE581  mov     eax, [ebp+var_1C]
005CE584  fldz
005CE586  fstp    qword ptr [eax+120h]
005CE58C  mov     ecx, [ebp+var_1C]
005CE58F  fldz
005CE591  fstp    qword ptr [ecx+128h]
005CE597  mov     edx, [ebp+var_1C]
005CE59A  fldz
005CE59C  fstp    qword ptr [edx+130h]
005CE5A2  push    6
005CE5A4  push    offset unk_747A80
005CE5A9  mov     ecx, [ebp+var_1C]
005CE5AC  call    sub_5C4D90
005CE5B1  mov     [ebp+var_4], 0FFFFFFFFh
005CE5B8  mov     eax, [ebp+var_1C]
005CE5BB  mov     ecx, [ebp+var_C]
005CE5BE  mov     large fs:0, ecx
005CE5C5  pop     ecx
005CE5C6  mov     esp, ebp
005CE5C8  pop     ebp
005CE5C9  retn    8
006E5500  mov     ecx, [ebp+var_1C]
006E5503  jmp     sub_5C4A90
006E5508  mov     ecx, [ebp+var_1C]
006E550B  add     ecx, 0A4h; void *
006E5511  jmp     sub_401330
006E5516  mov     edx, [esp-4+arg_4]
006E551A  lea     eax, [edx+0Ch]
006E551D  mov     ecx, [edx-14h]
006E5520  xor     ecx, eax; StackCookie
006E5522  call    @__security_check_cookie@4; __security_check_cookie(x)
006E5527  mov     eax, offset stru_7381FC
006E552C  jmp     ___CxxFrameHandler3
