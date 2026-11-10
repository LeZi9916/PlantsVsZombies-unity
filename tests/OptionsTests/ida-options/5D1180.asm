005D1180  push    ebp
005D1181  mov     ebp, esp
005D1183  push    0FFFFFFFFh
005D1185  push    offset SEH_5D1180
005D118A  mov     eax, large fs:0
005D1190  push    eax
005D1191  sub     esp, 0D4h
005D1197  push    esi
005D1198  push    edi
005D1199  mov     eax, ___security_cookie
005D119E  xor     eax, ebp
005D11A0  push    eax
005D11A1  lea     eax, [ebp+var_C]
005D11A4  mov     large fs:0, eax
005D11AA  mov     [ebp+var_DC], ecx
005D11B0  mov     [ebp+var_C0], 0
005D11BA  mov     eax, [ebp+var_DC]
005D11C0  movzx   ecx, byte ptr [eax+119h]
005D11C7  test    ecx, ecx
005D11C9  jz      short loc_5D11D0
005D11CB  jmp     loc_5D1789
005D11D0  mov     edx, [ebp+var_DC]
005D11D6  cmp     dword ptr [edx+138h], 0
005D11DD  jnz     short loc_5D11F3
005D11DF  mov     eax, [ebp+arg_0]
005D11E2  push    eax
005D11E3  mov     ecx, [ebp+var_DC]
005D11E9  call    sub_5CE7D0
005D11EE  jmp     loc_5D1789
005D11F3  mov     ecx, [ebp+var_DC]
005D11F9  cmp     dword ptr [ecx+0C4h], 0
005D1200  jnz     loc_5D12C2
005D1206  mov     eax, [ebp+var_DC]
005D120C  add     eax, 0A4h
005D1211  call    sub_43B340
005D1216  test    eax, eax
005D1218  jbe     loc_5D12C2
005D121E  push    20h ; ' '; Size
005D1220  call    ??2@YAPAXI@Z; operator new(uint)
005D1225  add     esp, 4
005D1228  mov     [ebp+var_20], eax
005D122B  mov     [ebp+var_4], 0
005D1232  cmp     [ebp+var_20], 0
005D1236  jz      short loc_5D127F
005D1238  push    offset aArialUnicodeMs; "Arial Unicode MS"
005D123D  lea     ecx, [ebp+var_3C]
005D1240  call    sub_4033F0
005D1245  mov     edx, [ebp+var_C0]
005D124B  or      edx, 1
005D124E  mov     [ebp+var_C0], edx
005D1254  push    0; char
005D1256  push    0; char
005D1258  push    0; char
005D125A  push    1; iCharSet
005D125C  push    0Ch; nNumber
005D125E  lea     eax, [ebp+var_3C]
005D1261  push    eax; int
005D1262  mov     ecx, [ebp+var_DC]
005D1268  mov     edx, [ecx+20h]
005D126B  mov     eax, [edx+6Ch]
005D126E  push    eax; int
005D126F  mov     ecx, [ebp+var_20]
005D1272  call    sub_5AF2A0
005D1277  mov     [ebp+var_E0], eax
005D127D  jmp     short loc_5D1289
005D127F  mov     [ebp+var_E0], 0
005D1289  mov     ecx, [ebp+var_E0]
005D128F  mov     [ebp+var_1C], ecx
005D1292  mov     [ebp+var_4], 0FFFFFFFFh
005D1299  mov     edx, [ebp+var_DC]
005D129F  mov     eax, [ebp+var_1C]
005D12A2  mov     [edx+0C4h], eax
005D12A8  mov     ecx, [ebp+var_C0]
005D12AE  and     ecx, 1
005D12B1  jz      short loc_5D12C2
005D12B3  and     [ebp+var_C0], 0FFFFFFFEh
005D12BA  lea     ecx, [ebp+var_3C]; void *
005D12BD  call    sub_401E90
005D12C2  mov     edx, [ebp+var_DC]
005D12C8  mov     eax, [edx]
005D12CA  mov     ecx, [ebp+var_DC]
005D12D0  mov     edx, [eax+120h]
005D12D6  call    edx
005D12D8  mov     [ebp+var_D], al
005D12DB  mov     eax, [ebp+var_DC]
005D12E1  cmp     dword ptr [eax+0E0h], 0
005D12E8  jnz     short loc_5D134F
005D12EA  movzx   ecx, [ebp+var_D]
005D12EE  test    ecx, ecx
005D12F0  jz      short loc_5D1314
005D12F2  mov     edx, [ebp+var_DC]
005D12F8  mov     eax, [edx+140h]
005D12FE  push    eax
005D12FF  mov     ecx, [ebp+var_DC]
005D1305  mov     edx, [ecx+13Ch]
005D130B  push    edx
005D130C  mov     ecx, [ebp+arg_0]
005D130F  call    sub_593AA0
005D1314  mov     eax, [ebp+var_DC]
005D131A  mov     ecx, [eax+138h]
005D1320  push    ecx
005D1321  mov     edx, [ebp+var_DC]
005D1327  mov     eax, [edx+4Ch]
005D132A  push    eax
005D132B  mov     ecx, [ebp+var_DC]
005D1331  mov     edx, [ecx+48h]
005D1334  push    edx
005D1335  xor     edx, edx
005D1337  xor     ecx, ecx
005D1339  lea     eax, [ebp+var_4C]
005D133C  call    sub_4091F0
005D1341  push    eax
005D1342  mov     ecx, [ebp+arg_0]
005D1345  call    sub_593B10
005D134A  jmp     loc_5D15F7
005D134F  mov     eax, [ebp+var_DC]
005D1355  movzx   ecx, byte ptr [eax+66h]
005D1359  test    ecx, ecx
005D135B  jz      short loc_5D13C3
005D135D  mov     edx, [ebp+var_DC]
005D1363  cmp     dword ptr [edx+110h], 0
005D136A  jle     short loc_5D13C3
005D136C  mov     eax, [ebp+var_DC]
005D1372  cmp     dword ptr [eax+114h], 0
005D1379  jle     short loc_5D13C3
005D137B  mov     ecx, [ebp+var_DC]
005D1381  mov     edx, [ecx+138h]
005D1387  push    edx; Concurrency::details::ExecutionResource *
005D1388  mov     eax, [ebp+var_DC]
005D138E  mov     ecx, [eax+4Ch]
005D1391  push    ecx
005D1392  mov     edx, [ebp+var_DC]
005D1398  mov     eax, [edx+48h]
005D139B  push    eax
005D139C  xor     edx, edx
005D139E  xor     ecx, ecx
005D13A0  lea     eax, [ebp+var_5C]
005D13A3  call    sub_4091F0
005D13A8  push    eax; int
005D13A9  mov     ecx, [ebp+var_DC]
005D13AF  add     ecx, 108h
005D13B5  push    ecx; int
005D13B6  mov     ecx, [ebp+arg_0]
005D13B9  call    sub_593B50
005D13BE  jmp     loc_5D15CD
005D13C3  mov     edx, [ebp+var_DC]
005D13C9  mov     eax, [edx]
005D13CB  mov     ecx, [ebp+var_DC]
005D13D1  mov     edx, [eax+120h]
005D13D7  call    edx
005D13D9  movzx   eax, al
005D13DC  test    eax, eax
005D13DE  jz      short loc_5D1428
005D13E0  mov     ecx, [ebp+var_DC]
005D13E6  mov     edx, [ecx+138h]
005D13EC  push    edx; Concurrency::details::ExecutionResource *
005D13ED  mov     eax, [ebp+var_DC]
005D13F3  mov     ecx, [eax+4Ch]
005D13F6  push    ecx
005D13F7  mov     edx, [ebp+var_DC]
005D13FD  mov     eax, [edx+48h]
005D1400  push    eax
005D1401  xor     edx, edx
005D1403  xor     ecx, ecx
005D1405  lea     eax, [ebp+var_6C]
005D1408  call    sub_4091F0
005D140D  push    eax; int
005D140E  mov     ecx, [ebp+var_DC]
005D1414  add     ecx, 0F8h
005D141A  push    ecx; int
005D141B  mov     ecx, [ebp+arg_0]
005D141E  call    sub_593B50
005D1423  jmp     loc_5D15CD
005D1428  mov     edx, [ebp+var_DC]
005D142E  fldz
005D1430  fcomp   qword ptr [edx+120h]
005D1436  fnstsw  ax
005D1438  test    ah, 5
005D143B  jp      loc_5D1532
005D1441  mov     eax, [ebp+var_DC]
005D1447  fld1
005D1449  fcomp   qword ptr [eax+120h]
005D144F  fnstsw  ax
005D1451  test    ah, 41h
005D1454  jnz     short loc_5D1499
005D1456  mov     ecx, [ebp+var_DC]
005D145C  mov     edx, [ecx+138h]
005D1462  push    edx; Concurrency::details::ExecutionResource *
005D1463  mov     eax, [ebp+var_DC]
005D1469  mov     ecx, [eax+4Ch]
005D146C  push    ecx
005D146D  mov     edx, [ebp+var_DC]
005D1473  mov     eax, [edx+48h]
005D1476  push    eax
005D1477  xor     edx, edx
005D1479  xor     ecx, ecx
005D147B  lea     eax, [ebp+var_7C]
005D147E  call    sub_4091F0
005D1483  push    eax; int
005D1484  mov     ecx, [ebp+var_DC]
005D148A  add     ecx, 0D8h
005D1490  push    ecx; int
005D1491  mov     ecx, [ebp+arg_0]
005D1494  call    sub_593B50
005D1499  push    1
005D149B  mov     ecx, [ebp+arg_0]
005D149E  call    sub_592910
005D14A3  mov     edx, [ebp+var_DC]
005D14A9  fld     qword ptr [edx+120h]
005D14AF  fmul    ds:dbl_727188
005D14B5  call    __ftol2_sse
005D14BA  push    eax
005D14BB  push    0FFh
005D14C0  push    0FFh
005D14C5  push    0FFh
005D14CA  lea     ecx, [ebp+var_8C]
005D14D0  call    sub_5CA5A0
005D14D5  push    eax
005D14D6  mov     ecx, [ebp+arg_0]
005D14D9  call    sub_592880
005D14DE  mov     eax, [ebp+var_DC]
005D14E4  mov     ecx, [eax+138h]
005D14EA  push    ecx; Concurrency::details::ExecutionResource *
005D14EB  mov     edx, [ebp+var_DC]
005D14F1  mov     eax, [edx+4Ch]
005D14F4  push    eax
005D14F5  mov     ecx, [ebp+var_DC]
005D14FB  mov     edx, [ecx+48h]
005D14FE  push    edx
005D14FF  xor     edx, edx
005D1501  xor     ecx, ecx
005D1503  lea     eax, [ebp+var_9C]
005D1509  call    sub_4091F0
005D150E  push    eax; int
005D150F  mov     eax, [ebp+var_DC]
005D1515  add     eax, 0E8h
005D151A  push    eax; int
005D151B  mov     ecx, [ebp+arg_0]
005D151E  call    sub_593B50
005D1523  push    0
005D1525  mov     ecx, [ebp+arg_0]
005D1528  call    sub_592910
005D152D  jmp     loc_5D15CD
005D1532  mov     ecx, [ebp+var_DC]
005D1538  movzx   edx, byte ptr [ecx+69h]
005D153C  test    edx, edx
005D153E  jz      short loc_5D1587
005D1540  mov     eax, [ebp+var_DC]
005D1546  mov     ecx, [eax+138h]
005D154C  push    ecx; Concurrency::details::ExecutionResource *
005D154D  mov     edx, [ebp+var_DC]
005D1553  mov     eax, [edx+4Ch]
005D1556  push    eax
005D1557  mov     ecx, [ebp+var_DC]
005D155D  mov     edx, [ecx+48h]
005D1560  push    edx
005D1561  xor     edx, edx
005D1563  xor     ecx, ecx
005D1565  lea     eax, [ebp+var_AC]
005D156B  call    sub_4091F0
005D1570  push    eax; int
005D1571  mov     eax, [ebp+var_DC]
005D1577  add     eax, 0E8h
005D157C  push    eax; int
005D157D  mov     ecx, [ebp+arg_0]
005D1580  call    sub_593B50
005D1585  jmp     short loc_5D15CD
005D1587  mov     ecx, [ebp+var_DC]
005D158D  mov     edx, [ecx+138h]
005D1593  push    edx; Concurrency::details::ExecutionResource *
005D1594  mov     eax, [ebp+var_DC]
005D159A  mov     ecx, [eax+4Ch]
005D159D  push    ecx
005D159E  mov     edx, [ebp+var_DC]
005D15A4  mov     eax, [edx+48h]
005D15A7  push    eax
005D15A8  xor     edx, edx
005D15AA  xor     ecx, ecx
005D15AC  lea     eax, [ebp+var_BC]
005D15B2  call    sub_4091F0
005D15B7  push    eax; int
005D15B8  mov     ecx, [ebp+var_DC]
005D15BE  add     ecx, 0D8h
005D15C4  push    ecx; int
005D15C5  mov     ecx, [ebp+arg_0]
005D15C8  call    sub_593B50
005D15CD  movzx   edx, [ebp+var_D]
005D15D1  test    edx, edx
005D15D3  jz      short loc_5D15F7
005D15D5  mov     eax, [ebp+var_DC]
005D15DB  mov     ecx, [eax+140h]
005D15E1  push    ecx
005D15E2  mov     edx, [ebp+var_DC]
005D15E8  mov     eax, [edx+13Ch]
005D15EE  push    eax
005D15EF  mov     ecx, [ebp+arg_0]
005D15F2  call    sub_593AA0
005D15F7  mov     ecx, [ebp+var_DC]
005D15FD  cmp     dword ptr [ecx+0C4h], 0
005D1604  jz      loc_5D175B
005D160A  mov     edx, [ebp+var_DC]
005D1610  mov     eax, [edx+0C4h]
005D1616  push    eax
005D1617  mov     ecx, [ebp+arg_0]
005D161A  call    sub_592860
005D161F  mov     ecx, [ebp+var_DC]
005D1625  movzx   edx, byte ptr [ecx+69h]
005D1629  test    edx, edx
005D162B  jz      short loc_5D164B
005D162D  mov     esi, [ebp+var_DC]
005D1633  add     esi, 6Ch ; 'l'
005D1636  mov     edi, 1
005D163B  call    sub_43B0D0
005D1640  push    eax
005D1641  mov     ecx, [ebp+arg_0]
005D1644  call    sub_592880
005D1649  jmp     short loc_5D1664
005D164B  mov     esi, [ebp+var_DC]
005D1651  add     esi, 6Ch ; 'l'
005D1654  xor     edi, edi
005D1656  call    sub_43B0D0
005D165B  push    eax
005D165C  mov     ecx, [ebp+arg_0]
005D165F  call    sub_592880
005D1664  mov     eax, [ebp+var_DC]
005D166A  add     eax, 0A4h
005D166F  push    eax
005D1670  mov     ecx, [ebp+var_DC]
005D1676  mov     edx, [ecx+0C4h]
005D167C  mov     eax, [ebp+var_DC]
005D1682  mov     ecx, [eax+0C4h]
005D1688  mov     edx, [edx]
005D168A  mov     eax, [edx+1Ch]
005D168D  call    eax
005D168F  mov     ecx, [ebp+var_DC]
005D1695  mov     edx, [ecx+48h]
005D1698  sub     edx, eax
005D169A  mov     eax, edx
005D169C  cdq
005D169D  sub     eax, edx
005D169F  sar     eax, 1
005D16A1  mov     [ebp+var_14], eax
005D16A4  mov     eax, [ebp+var_DC]
005D16AA  mov     ecx, [eax+0C4h]
005D16B0  mov     edx, [ebp+var_DC]
005D16B6  mov     eax, [edx+0C4h]
005D16BC  mov     edx, [ecx]
005D16BE  mov     ecx, eax
005D16C0  mov     eax, [edx+4]
005D16C3  call    eax
005D16C5  mov     ecx, [ebp+var_DC]
005D16CB  mov     esi, [ecx+4Ch]
005D16CE  add     esi, eax
005D16D0  mov     edx, [ebp+var_DC]
005D16D6  mov     eax, [edx+0C4h]
005D16DC  mov     ecx, [ebp+var_DC]
005D16E2  mov     ecx, [ecx+0C4h]
005D16E8  mov     edx, [eax]
005D16EA  mov     eax, [edx+8]
005D16ED  call    eax
005D16EF  sub     esi, eax
005D16F1  mov     ecx, [ebp+var_DC]
005D16F7  mov     edx, [ecx+0C4h]
005D16FD  mov     eax, [ebp+var_DC]
005D1703  mov     ecx, [eax+0C4h]
005D1709  mov     edx, [edx]
005D170B  mov     eax, [edx+4]
005D170E  call    eax
005D1710  cdq
005D1711  mov     ecx, 6
005D1716  idiv    ecx
005D1718  sub     esi, eax
005D171A  sub     esi, 1
005D171D  mov     eax, esi
005D171F  cdq
005D1720  sub     eax, edx
005D1722  sar     eax, 1
005D1724  mov     [ebp+var_18], eax
005D1727  mov     edx, [ebp+var_DC]
005D172D  mov     eax, [ebp+var_18]
005D1730  add     eax, [edx+148h]
005D1736  push    eax
005D1737  mov     ecx, [ebp+var_DC]
005D173D  mov     edx, [ebp+var_14]
005D1740  add     edx, [ecx+144h]
005D1746  push    edx
005D1747  mov     eax, [ebp+var_DC]
005D174D  add     eax, 0A4h
005D1752  push    eax
005D1753  mov     ecx, [ebp+arg_0]
005D1756  call    sub_592F40
005D175B  movzx   ecx, [ebp+var_D]
005D175F  test    ecx, ecx
005D1761  jz      short loc_5D1789
005D1763  mov     edx, [ebp+var_DC]
005D1769  mov     eax, [edx+140h]
005D176F  neg     eax
005D1771  push    eax
005D1772  mov     ecx, [ebp+var_DC]
005D1778  mov     edx, [ecx+13Ch]
005D177E  neg     edx
005D1780  push    edx
005D1781  mov     ecx, [ebp+arg_0]
005D1784  call    sub_593AA0
005D1789  mov     ecx, [ebp+var_C]
005D178C  mov     large fs:0, ecx
005D1793  pop     ecx
005D1794  pop     edi
005D1795  pop     esi
005D1796  mov     esp, ebp
005D1798  pop     ebp
005D1799  retn    4
006E54A0  mov     eax, [ebp+var_20]
006E54A3  push    eax; void *
006E54A4  call    ??3@YAXPAX@Z; operator delete(void *)
006E54A9  pop     ecx
006E54AA  retn
006E54AB  mov     edx, [esp-4+arg_4]
006E54AF  lea     eax, [edx+0Ch]
006E54B2  mov     ecx, [edx-0E0h]
006E54B8  xor     ecx, eax; StackCookie
006E54BA  call    @__security_check_cookie@4; __security_check_cookie(x)
006E54BF  mov     eax, offset stru_73819C
006E54C4  jmp     ___CxxFrameHandler3
