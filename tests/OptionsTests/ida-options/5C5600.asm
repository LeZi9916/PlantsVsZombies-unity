005C5600  push    ebp
005C5601  mov     ebp, esp
005C5603  sub     esp, 18h
005C5606  push    esi
005C5607  mov     [ebp+var_10], ecx
005C560A  mov     ecx, [ebp+arg_4]
005C560D  call    unknown_libname_8; Microsoft VisualC 2-14/net runtime
005C5612  mov     [ebp+var_4], eax
005C5615  mov     eax, [ebp+arg_C]
005C5618  push    eax
005C5619  mov     ecx, [ebp+var_4]
005C561C  mov     edx, [ecx]
005C561E  mov     ecx, [ebp+var_4]
005C5621  mov     eax, [edx+1Ch]
005C5624  call    eax
005C5626  mov     [ebp+var_8], eax
005C5629  mov     ecx, [ebp+var_10]
005C562C  mov     eax, [ecx+48h]
005C562F  sub     eax, [ebp+var_8]
005C5632  cdq
005C5633  sub     eax, edx
005C5635  sar     eax, 1
005C5637  mov     [ebp+var_C], eax
005C563A  lea     edx, [ebp+arg_20]
005C563D  push    edx
005C563E  mov     ecx, [ebp+arg_4]
005C5641  call    sub_592880
005C5646  mov     eax, [ebp+arg_30]
005C5649  mov     ecx, [ebp+arg_8]
005C564C  add     ecx, [eax+4]
005C564F  push    ecx
005C5650  mov     edx, [ebp+var_10]
005C5653  mov     eax, [edx+48h]
005C5656  sub     eax, [ebp+var_8]
005C5659  cdq
005C565A  sub     eax, edx
005C565C  sar     eax, 1
005C565E  mov     ecx, [ebp+arg_30]
005C5661  add     eax, [ecx]
005C5663  push    eax
005C5664  mov     edx, [ebp+arg_C]
005C5667  push    edx
005C5668  mov     ecx, [ebp+arg_4]
005C566B  call    sub_592F40
005C5670  lea     eax, [ebp+arg_10]
005C5673  push    eax
005C5674  mov     ecx, [ebp+arg_4]
005C5677  call    sub_592880
005C567C  mov     ecx, [ebp+arg_8]
005C567F  push    ecx
005C5680  mov     edx, [ebp+var_10]
005C5683  mov     eax, [edx+48h]
005C5686  sub     eax, [ebp+var_8]
005C5689  cdq
005C568A  sub     eax, edx
005C568C  sar     eax, 1
005C568E  push    eax
005C568F  mov     eax, [ebp+arg_C]
005C5692  push    eax
005C5693  mov     ecx, [ebp+arg_4]
005C5696  call    sub_592F40
005C569B  mov     ecx, [ebp+arg_30]
005C569E  cmp     dword ptr [ecx+4], 0
005C56A2  jle     short loc_5C56AD
005C56A4  mov     [ebp+var_14], 0
005C56AB  jmp     short loc_5C56B6
005C56AD  mov     edx, [ebp+arg_30]
005C56B0  mov     eax, [edx+4]
005C56B3  mov     [ebp+var_14], eax
005C56B6  mov     ecx, [ebp+arg_30]
005C56B9  cmp     dword ptr [ecx], 0
005C56BC  jle     short loc_5C56C7
005C56BE  mov     [ebp+var_18], 0
005C56C5  jmp     short loc_5C56CF
005C56C7  mov     edx, [ebp+arg_30]
005C56CA  mov     eax, [edx]
005C56CC  mov     [ebp+var_18], eax
005C56CF  mov     ecx, [ebp+var_4]
005C56D2  mov     edx, [ecx]
005C56D4  mov     ecx, [ebp+var_4]
005C56D7  mov     eax, [edx+10h]
005C56DA  call    eax
005C56DC  mov     esi, eax
005C56DE  mov     ecx, [ebp+arg_30]
005C56E1  mov     edx, [ecx+4]
005C56E4  push    edx; Number
005C56E5  call    _abs
005C56EA  add     esp, 4
005C56ED  add     esi, eax
005C56EF  push    esi
005C56F0  mov     eax, [ebp+arg_30]
005C56F3  mov     ecx, [eax]
005C56F5  push    ecx; Number
005C56F6  call    _abs
005C56FB  add     esp, 4
005C56FE  add     eax, [ebp+var_8]
005C5701  push    eax
005C5702  mov     edx, [ebp+var_4]
005C5705  mov     eax, [edx]
005C5707  mov     ecx, [ebp+var_4]
005C570A  mov     edx, [eax+4]
005C570D  call    edx
005C570F  mov     edx, [ebp+arg_8]
005C5712  sub     edx, eax
005C5714  add     edx, [ebp+var_14]
005C5717  mov     ecx, [ebp+var_C]
005C571A  add     ecx, [ebp+var_18]
005C571D  mov     eax, [ebp+arg_0]
005C5720  call    sub_4091F0
005C5725  mov     eax, [ebp+arg_0]
005C5728  pop     esi
005C5729  mov     esp, ebp
005C572B  pop     ebp
005C572C  retn    34h ; '4'
