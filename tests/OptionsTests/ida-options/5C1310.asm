005C1310  push    ebp
005C1311  mov     ebp, esp
005C1313  sub     esp, 0C4h
005C1319  mov     [ebp+var_C4], ecx
005C131F  mov     eax, [ebp+var_C4]
005C1325  add     eax, 4
005C1328  mov     [ebp+var_48], eax
005C132B  mov     ecx, [ebp+var_48]
005C132E  mov     edx, [ecx+14h]
005C1331  mov     [ebp+var_44], edx
005C1334  lea     eax, [ebp+var_10]
005C1337  call    sub_4086A0
005C133C  mov     eax, [ebp+var_44]
005C133F  mov     [ebp+var_C], eax
005C1342  mov     ecx, 1
005C1347  test    ecx, ecx
005C1349  jz      short loc_5C1356
005C134B  cmp     [ebp+var_48], 0
005C134F  jnz     short loc_5C1356
005C1351  call    __invalid_parameter_noinfo
005C1356  mov     eax, [ebp+var_48]
005C1359  lea     edx, [ebp+var_10]
005C135C  call    sub_408680
005C1361  lea     edx, [ebp+arg_0]
005C1364  push    edx
005C1365  mov     eax, [ebp+var_C]
005C1368  push    eax
005C1369  mov     ecx, [ebp+var_10]
005C136C  push    ecx
005C136D  lea     edx, [ebp+var_18]
005C1370  push    edx
005C1371  mov     ecx, [ebp+var_C4]
005C1377  add     ecx, 4
005C137A  call    sub_62EFA0
005C137F  mov     ecx, [eax+4]
005C1382  push    ecx
005C1383  mov     edx, [eax]
005C1385  push    edx
005C1386  lea     eax, [ebp+var_8]
005C1389  push    eax
005C138A  call    sub_5D48D0
005C138F  add     esp, 18h
005C1392  mov     ecx, [ebp+var_C4]
005C1398  add     ecx, 4
005C139B  mov     [ebp+var_88], ecx
005C13A1  mov     edx, [ebp+var_88]
005C13A7  mov     eax, [edx+14h]
005C13AA  mov     [ebp+var_84], eax
005C13B0  lea     eax, [ebp+var_20]
005C13B3  call    sub_4086A0
005C13B8  mov     ecx, [ebp+var_84]
005C13BE  mov     [ebp+var_1C], ecx
005C13C1  mov     edx, 1
005C13C6  test    edx, edx
005C13C8  jz      short loc_5C13D8
005C13CA  cmp     [ebp+var_88], 0
005C13D1  jnz     short loc_5C13D8
005C13D3  call    __invalid_parameter_noinfo
005C13D8  mov     eax, [ebp+var_88]
005C13DE  lea     edx, [ebp+var_20]
005C13E1  call    sub_408680
005C13E6  lea     eax, [ebp+var_20]
005C13E9  push    eax
005C13EA  lea     ecx, [ebp+var_8]
005C13ED  call    sub_632700
005C13F2  movzx   ecx, al
005C13F5  neg     ecx
005C13F7  sbb     ecx, ecx
005C13F9  add     ecx, 1
005C13FC  movzx   edx, cl
005C13FF  test    edx, edx
005C1401  jz      loc_5C1507
005C1407  mov     eax, [ebp+var_C4]
005C140D  add     eax, 2Ch ; ','
005C1410  push    eax
005C1411  lea     ecx, [ebp+var_8]
005C1414  call    sub_632700
005C1419  movzx   ecx, al
005C141C  test    ecx, ecx
005C141E  jz      short loc_5C146D
005C1420  mov     edx, [ebp+var_C4]
005C1426  add     edx, 2Ch ; ','
005C1429  mov     [ebp+var_98], edx
005C142F  mov     eax, [ebp+var_98]
005C1435  mov     ecx, [eax]
005C1437  mov     edx, [eax+4]
005C143A  mov     [ebp+var_90], ecx
005C1440  mov     [ebp+var_8C], edx
005C1446  mov     ecx, [ebp+var_98]
005C144C  call    sub_5D3980
005C1451  mov     eax, [ebp+var_90]
005C1457  mov     [ebp+var_28], eax
005C145A  mov     ecx, [ebp+var_8C]
005C1460  mov     [ebp+var_24], ecx
005C1463  mov     edx, [ebp+var_C4]
005C1469  mov     byte ptr [edx+28h], 1
005C146D  mov     eax, [ebp+var_8]
005C1470  mov     [ebp+var_30], eax
005C1473  mov     ecx, [ebp+var_4]
005C1476  mov     [ebp+var_2C], ecx
005C1479  mov     edx, [ebp+var_2C]
005C147C  push    edx
005C147D  mov     eax, [ebp+var_30]
005C1480  push    eax
005C1481  lea     ecx, [ebp+var_38]
005C1484  push    ecx
005C1485  mov     ecx, [ebp+var_C4]
005C148B  add     ecx, 4
005C148E  call    sub_5D1840
005C1493  mov     edx, [ebp+var_C4]
005C1499  add     edx, 4
005C149C  mov     [ebp+var_C0], edx
005C14A2  mov     eax, [ebp+var_C0]
005C14A8  mov     ecx, [eax+14h]
005C14AB  mov     [ebp+var_BC], ecx
005C14B1  lea     eax, [ebp+var_40]
005C14B4  call    sub_4086A0
005C14B9  mov     edx, [ebp+var_BC]
005C14BF  mov     [ebp+var_3C], edx
005C14C2  mov     eax, 1
005C14C7  test    eax, eax
005C14C9  jz      short loc_5C14D9
005C14CB  cmp     [ebp+var_C0], 0
005C14D2  jnz     short loc_5C14D9
005C14D4  call    __invalid_parameter_noinfo
005C14D9  mov     eax, [ebp+var_C0]
005C14DF  lea     edx, [ebp+var_40]
005C14E2  call    sub_408680
005C14E7  mov     ecx, [ebp+arg_0]
005C14EA  push    ecx
005C14EB  lea     edx, [ebp+var_40]
005C14EE  push    edx
005C14EF  mov     ecx, [ebp+var_C4]
005C14F5  call    sub_5C0FD0
005C14FA  mov     eax, [ebp+arg_0]
005C14FD  mov     edx, [eax]
005C14FF  mov     ecx, [ebp+arg_0]
005C1502  mov     eax, [edx+78h]
005C1505  call    eax
005C1507  mov     esp, ebp
005C1509  pop     ebp
005C150A  retn    4
