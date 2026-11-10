005CE210  push    ebp
005CE211  mov     ebp, esp
005CE213  push    ecx
005CE214  mov     [ebp+var_4], ecx
005CE217  mov     eax, [ebp+arg_0]
005CE21A  push    eax
005CE21B  mov     ecx, [ebp+var_4]
005CE21E  call    unknown_libname_14; Microsoft VisualC 2-14/net runtime
005CE223  mov     ecx, [ebp+var_4]
005CE226  cmp     dword ptr [ecx+0BCh], 0
005CE22D  jnz     short loc_5CE288
005CE22F  mov     edx, [ebp+var_4]
005CE232  cmp     dword ptr [edx+0B0h], 0
005CE239  jz      short loc_5CE288
005CE23B  mov     eax, [ebp+var_4]
005CE23E  cmp     dword ptr [eax+0ACh], 0
005CE245  jz      short loc_5CE288
005CE247  mov     ecx, [ebp+var_4]
005CE24A  movzx   edx, byte ptr [ecx+0A8h]
005CE251  test    edx, edx
005CE253  jz      short loc_5CE26D
005CE255  push    0; int
005CE257  push    0; int
005CE259  mov     eax, [ebp+var_4]
005CE25C  mov     ecx, [eax+0B0h]
005CE262  push    ecx; Concurrency::details::ExecutionResource *
005CE263  mov     ecx, [ebp+arg_0]
005CE266  call    sub_592F90
005CE26B  jmp     short loc_5CE283
005CE26D  push    0; int
005CE26F  push    0; int
005CE271  mov     edx, [ebp+var_4]
005CE274  mov     eax, [edx+0ACh]
005CE27A  push    eax; Concurrency::details::ExecutionResource *
005CE27B  mov     ecx, [ebp+arg_0]
005CE27E  call    sub_592F90
005CE283  jmp     loc_5CE3D2
005CE288  mov     ecx, [ebp+var_4]
005CE28B  cmp     dword ptr [ecx+0BCh], 0
005CE292  jz      short loc_5CE2F5
005CE294  mov     edx, [ebp+var_4]
005CE297  cmp     dword ptr [edx+0ACh], 0
005CE29E  jz      short loc_5CE2F5
005CE2A0  mov     eax, [ebp+var_4]
005CE2A3  movzx   ecx, byte ptr [eax+0A8h]
005CE2AA  test    ecx, ecx
005CE2AC  jz      short loc_5CE2D0
005CE2AE  mov     edx, [ebp+var_4]
005CE2B1  add     edx, 0B4h
005CE2B7  push    edx; int
005CE2B8  push    0; int
005CE2BA  push    0; int
005CE2BC  mov     eax, [ebp+var_4]
005CE2BF  mov     ecx, [eax+0ACh]
005CE2C5  push    ecx; Concurrency::details::ExecutionResource *
005CE2C6  mov     ecx, [ebp+arg_0]
005CE2C9  call    sub_5930D0
005CE2CE  jmp     short loc_5CE2F0
005CE2D0  mov     edx, [ebp+var_4]
005CE2D3  add     edx, 0C4h
005CE2D9  push    edx; int
005CE2DA  push    0; int
005CE2DC  push    0; int
005CE2DE  mov     eax, [ebp+var_4]
005CE2E1  mov     ecx, [eax+0ACh]
005CE2E7  push    ecx; Concurrency::details::ExecutionResource *
005CE2E8  mov     ecx, [ebp+arg_0]
005CE2EB  call    sub_5930D0
005CE2F0  jmp     loc_5CE3D2
005CE2F5  mov     edx, [ebp+var_4]
005CE2F8  cmp     dword ptr [edx+0ACh], 0
005CE2FF  jnz     loc_5CE3D2
005CE305  mov     eax, [ebp+var_4]
005CE308  cmp     dword ptr [eax+0B0h], 0
005CE30F  jnz     loc_5CE3D2
005CE315  mov     ecx, [ebp+var_4]
005CE318  add     ecx, 0D4h
005CE31E  push    ecx
005CE31F  mov     ecx, [ebp+arg_0]
005CE322  call    sub_592880
005CE327  mov     edx, [ebp+var_4]
005CE32A  mov     eax, [edx+4Ch]
005CE32D  push    eax
005CE32E  mov     ecx, [ebp+var_4]
005CE331  mov     edx, [ecx+48h]
005CE334  push    edx
005CE335  push    0
005CE337  push    0
005CE339  mov     ecx, [ebp+arg_0]
005CE33C  call    sub_592990
005CE341  mov     eax, [ebp+var_4]
005CE344  add     eax, 0E4h
005CE349  push    eax
005CE34A  mov     ecx, [ebp+arg_0]
005CE34D  call    sub_592880
005CE352  mov     ecx, [ebp+var_4]
005CE355  mov     edx, [ecx+4Ch]
005CE358  sub     edx, 2
005CE35B  push    edx
005CE35C  mov     eax, [ebp+var_4]
005CE35F  mov     ecx, [eax+48h]
005CE362  sub     ecx, 2
005CE365  push    ecx
005CE366  push    1
005CE368  push    1
005CE36A  mov     ecx, [ebp+arg_0]
005CE36D  call    sub_592990
005CE372  mov     edx, [ebp+var_4]
005CE375  movzx   eax, byte ptr [edx+0A8h]
005CE37C  test    eax, eax
005CE37E  jz      short loc_5CE3D2
005CE380  mov     ecx, [ebp+var_4]
005CE383  add     ecx, 0F4h
005CE389  push    ecx
005CE38A  mov     ecx, [ebp+arg_0]
005CE38D  call    sub_592880
005CE392  mov     edx, [ebp+var_4]
005CE395  mov     eax, [edx+4Ch]
005CE398  sub     eax, 2
005CE39B  push    eax
005CE39C  mov     ecx, [ebp+var_4]
005CE39F  mov     edx, [ecx+48h]
005CE3A2  sub     edx, 2
005CE3A5  push    edx
005CE3A6  push    1
005CE3A8  push    1
005CE3AA  mov     ecx, [ebp+arg_0]
005CE3AD  call    sub_592E90
005CE3B2  mov     eax, [ebp+var_4]
005CE3B5  mov     ecx, [eax+4Ch]
005CE3B8  sub     ecx, 2
005CE3BB  push    ecx
005CE3BC  push    1
005CE3BE  push    1
005CE3C0  mov     edx, [ebp+var_4]
005CE3C3  mov     eax, [edx+48h]
005CE3C6  sub     eax, 1
005CE3C9  push    eax
005CE3CA  mov     ecx, [ebp+arg_0]
005CE3CD  call    sub_592E90
005CE3D2  mov     esp, ebp
005CE3D4  pop     ebp
005CE3D5  retn    4
