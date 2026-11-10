005C1510  push    ebp
005C1511  mov     ebp, esp
005C1513  sub     esp, 0C4h
005C1519  mov     [ebp+var_C4], ecx
005C151F  mov     eax, [ebp+var_C4]
005C1525  add     eax, 4
005C1528  mov     [ebp+var_48], eax
005C152B  mov     ecx, [ebp+var_48]
005C152E  mov     edx, [ecx+14h]
005C1531  mov     [ebp+var_44], edx
005C1534  lea     eax, [ebp+var_10]
005C1537  call    sub_4086A0
005C153C  mov     eax, [ebp+var_44]
005C153F  mov     [ebp+var_C], eax
005C1542  mov     ecx, 1
005C1547  test    ecx, ecx
005C1549  jz      short loc_5C1556
005C154B  cmp     [ebp+var_48], 0
005C154F  jnz     short loc_5C1556
005C1551  call    __invalid_parameter_noinfo
005C1556  mov     eax, [ebp+var_48]
005C1559  lea     edx, [ebp+var_10]
005C155C  call    sub_408680
005C1561  lea     edx, [ebp+arg_0]
005C1564  push    edx
005C1565  mov     eax, [ebp+var_C]
005C1568  push    eax
005C1569  mov     ecx, [ebp+var_10]
005C156C  push    ecx
005C156D  lea     edx, [ebp+var_18]
005C1570  push    edx
005C1571  mov     ecx, [ebp+var_C4]
005C1577  add     ecx, 4
005C157A  call    sub_62EFA0
005C157F  mov     ecx, [eax+4]
005C1582  push    ecx
005C1583  mov     edx, [eax]
005C1585  push    edx
005C1586  lea     eax, [ebp+var_8]
005C1589  push    eax
005C158A  call    sub_5D48D0
005C158F  add     esp, 18h
005C1592  mov     ecx, [ebp+var_C4]
005C1598  add     ecx, 4
005C159B  mov     [ebp+var_88], ecx
005C15A1  mov     edx, [ebp+var_88]
005C15A7  mov     eax, [edx+14h]
005C15AA  mov     [ebp+var_84], eax
005C15B0  lea     eax, [ebp+var_20]
005C15B3  call    sub_4086A0
005C15B8  mov     ecx, [ebp+var_84]
005C15BE  mov     [ebp+var_1C], ecx
005C15C1  mov     edx, 1
005C15C6  test    edx, edx
005C15C8  jz      short loc_5C15D8
005C15CA  cmp     [ebp+var_88], 0
005C15D1  jnz     short loc_5C15D8
005C15D3  call    __invalid_parameter_noinfo
005C15D8  mov     eax, [ebp+var_88]
005C15DE  lea     edx, [ebp+var_20]
005C15E1  call    sub_408680
005C15E6  lea     eax, [ebp+var_20]
005C15E9  push    eax
005C15EA  lea     ecx, [ebp+var_8]
005C15ED  call    sub_632700
005C15F2  movzx   ecx, al
005C15F5  neg     ecx
005C15F7  sbb     ecx, ecx
005C15F9  add     ecx, 1
005C15FC  movzx   edx, cl
005C15FF  test    edx, edx
005C1601  jz      loc_5C16C2
005C1607  mov     eax, [ebp+var_C4]
005C160D  add     eax, 2Ch ; ','
005C1610  push    eax
005C1611  lea     ecx, [ebp+var_8]
005C1614  call    sub_632700
005C1619  movzx   ecx, al
005C161C  test    ecx, ecx
005C161E  jz      short loc_5C166D
005C1620  mov     edx, [ebp+var_C4]
005C1626  add     edx, 2Ch ; ','
005C1629  mov     [ebp+var_98], edx
005C162F  mov     eax, [ebp+var_98]
005C1635  mov     ecx, [eax]
005C1637  mov     edx, [eax+4]
005C163A  mov     [ebp+var_90], ecx
005C1640  mov     [ebp+var_8C], edx
005C1646  mov     ecx, [ebp+var_98]
005C164C  call    sub_5D3980
005C1651  mov     eax, [ebp+var_90]
005C1657  mov     [ebp+var_28], eax
005C165A  mov     ecx, [ebp+var_8C]
005C1660  mov     [ebp+var_24], ecx
005C1663  mov     edx, [ebp+var_C4]
005C1669  mov     byte ptr [edx+28h], 1
005C166D  mov     eax, [ebp+var_8]
005C1670  mov     [ebp+var_30], eax
005C1673  mov     ecx, [ebp+var_4]
005C1676  mov     [ebp+var_2C], ecx
005C1679  mov     edx, [ebp+var_2C]
005C167C  push    edx
005C167D  mov     eax, [ebp+var_30]
005C1680  push    eax
005C1681  lea     ecx, [ebp+var_38]
005C1684  push    ecx
005C1685  mov     ecx, [ebp+var_C4]
005C168B  add     ecx, 4
005C168E  call    sub_5D1840
005C1693  mov     edx, [ebp+arg_0]
005C1696  push    edx
005C1697  lea     eax, [ebp+var_40]
005C169A  push    eax
005C169B  mov     ecx, [ebp+var_C4]
005C16A1  add     ecx, 4
005C16A4  call    sub_62EFA0
005C16A9  push    eax
005C16AA  mov     ecx, [ebp+var_C4]
005C16B0  call    sub_5C0FD0
005C16B5  mov     ecx, [ebp+arg_0]
005C16B8  mov     edx, [ecx]
005C16BA  mov     ecx, [ebp+arg_0]
005C16BD  mov     eax, [edx+78h]
005C16C0  call    eax
005C16C2  mov     esp, ebp
005C16C4  pop     ebp
005C16C5  retn    4
