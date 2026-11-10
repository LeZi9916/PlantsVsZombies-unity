0046B270  mov     eax, large fs:0
0046B276  push    0FFFFFFFFh
0046B278  push    offset SEH_46B270
0046B27D  push    eax
0046B27E  mov     large fs:0, esp
0046B285  mov     edx, dword_753038
0046B28B  sub     esp, 0FCh
0046B291  push    esi
0046B292  mov     esi, ecx
0046B294  mov     ecx, [esi+4]
0046B297  mov     eax, [ecx]
0046B299  mov     eax, [eax+0D8h]
0046B29F  push    edx
0046B2A0  call    eax
0046B2A2  mov     eax, [esp+10Ch+arg_0]
0046B2A9  sub     eax, 6
0046B2AC  jz      loc_46B43F
0046B2B2  sub     eax, 1
0046B2B5  jnz     loc_46B510
0046B2BB  cmp     [esp+10Ch+arg_4], al
0046B2C2  jz      loc_46B510
0046B2C8  mov     ecx, [esi+4]
0046B2CB  call    sub_62D1B0
0046B2D0  test    al, al
0046B2D2  jnz     loc_46B38C
0046B2D8  mov     ecx, [esi+14h]
0046B2DB  mov     edx, [ecx]
0046B2DD  mov     eax, [edx+118h]
0046B2E3  push    0
0046B2E5  push    0
0046B2E7  call    eax
0046B2E9  push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
0046B2EE  lea     ecx, [esp+110h+var_B4]
0046B2F2  call    sub_40D030
0046B2F7  push    offset aHardwareAccele_2; "[HARDWARE_ACCELERATION_NOT_SUPPORTED]"
0046B2FC  lea     ecx, [esp+110h+var_7C]
0046B303  mov     [esp+110h+var_4], 3
0046B30E  call    sub_40D030
0046B313  push    offset aNotSupported; "[NOT_SUPPORTED]"
0046B318  lea     ecx, [esp+110h+var_EC]
0046B31C  mov     byte ptr [esp+110h+var_4], 4
0046B324  call    sub_40D030
0046B329  push    3
0046B32B  lea     eax, [esp+110h+var_B4]
0046B32F  push    eax
0046B330  lea     eax, [esp+114h+var_7C]
0046B337  push    eax
0046B338  mov     byte ptr [esp+118h+var_4], 5
0046B340  mov     ecx, [esi+4]
0046B343  mov     edx, [ecx]
0046B345  mov     edx, [edx+120h]
0046B34B  lea     eax, [esp+118h+var_EC]
0046B34F  push    eax
0046B350  push    1
0046B352  push    10h
0046B354  call    edx
0046B356  lea     ecx, [esp+10Ch+var_EC]; void *
0046B35A  call    sub_401330
0046B35F  lea     ecx, [esp+10Ch+var_7C]; void *
0046B366  call    sub_401330
0046B36B  lea     ecx, [esp+10Ch+var_B4]; void *
0046B36F  call    sub_401330
0046B374  pop     esi
0046B375  mov     ecx, [esp+108h+var_C]
0046B37C  mov     large fs:0, ecx
0046B383  add     esp, 108h
0046B389  retn    8
0046B38C  mov     ecx, [esi+4]
0046B38F  call    sub_62D200
0046B394  test    al, al
0046B396  jnz     loc_46B510
0046B39C  push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
0046B3A1  lea     ecx, [esp+110h+var_D0]
0046B3A5  call    sub_40D030
0046B3AA  push    offset off_711464
0046B3AF  lea     ecx, [esp+110h+var_108]
0046B3B3  mov     [esp+110h+var_4], 6
0046B3BE  call    sub_40D030
0046B3C3  push    offset aDialogWarning; "[DIALOG_WARNING]"
0046B3C8  lea     ecx, [esp+110h+var_44]
0046B3CF  mov     byte ptr [esp+110h+var_4], 7
0046B3D7  call    sub_40D030
0046B3DC  push    3
0046B3DE  lea     edx, [esp+110h+var_D0]
0046B3E2  push    edx
0046B3E3  lea     edx, [esp+114h+var_108]
0046B3E7  push    edx
0046B3E8  mov     byte ptr [esp+118h+var_4], 8
0046B3F0  mov     ecx, [esi+4]
0046B3F3  mov     eax, [ecx]
0046B3F5  mov     eax, [eax+120h]
0046B3FB  lea     edx, [esp+118h+var_44]
0046B402  push    edx
0046B403  push    1
0046B405  push    10h
0046B407  call    eax
0046B409  lea     ecx, [esp+10Ch+var_44]; void *
0046B410  call    sub_401330
0046B415  lea     ecx, [esp+10Ch+var_108]; void *
0046B419  call    sub_401330
0046B41E  lea     ecx, [esp+10Ch+var_D0]; void *
0046B422  call    sub_401330
0046B427  pop     esi
0046B428  mov     ecx, [esp+108h+var_C]
0046B42F  mov     large fs:0, ecx
0046B436  add     esp, 108h
0046B43C  retn    8
0046B43F  cmp     [esp+10Ch+arg_4], 0
0046B447  jnz     loc_46B510
0046B44D  mov     ecx, [esi+4]
0046B450  cmp     byte ptr [ecx+36Ah], 0
0046B457  jz      loc_46B510
0046B45D  push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
0046B462  lea     ecx, [esp+110h+var_28]
0046B469  call    sub_40D030
0046B46E  push    offset off_71139C
0046B473  lea     ecx, [esp+110h+var_60]
0046B47A  mov     [esp+110h+var_4], 0
0046B485  call    sub_40D030
0046B48A  push    offset aNoWindowedMode; "[NO_WINDOWED_MODE]"
0046B48F  lea     ecx, [esp+110h+var_98]
0046B493  mov     byte ptr [esp+110h+var_4], 1
0046B49B  call    sub_40D030
0046B4A0  push    3
0046B4A2  lea     eax, [esp+110h+var_28]
0046B4A9  push    eax
0046B4AA  lea     eax, [esp+114h+var_60]
0046B4B1  push    eax
0046B4B2  mov     byte ptr [esp+118h+var_4], 2
0046B4BA  mov     ecx, [esi+4]
0046B4BD  mov     edx, [ecx]
0046B4BF  mov     edx, [edx+120h]
0046B4C5  lea     eax, [esp+118h+var_98]
0046B4CC  push    eax
0046B4CD  push    1
0046B4CF  push    0Ah
0046B4D1  call    edx
0046B4D3  lea     ecx, [esp+10Ch+var_98]; void *
0046B4D7  call    sub_401330
0046B4DC  lea     ecx, [esp+10Ch+var_60]; void *
0046B4E3  call    sub_401330
0046B4E8  lea     ecx, [esp+10Ch+var_28]; void *
0046B4EF  mov     [esp+10Ch+var_4], 0FFFFFFFFh
0046B4FA  call    sub_401330
0046B4FF  mov     ecx, [esi+10h]
0046B502  mov     eax, [ecx]
0046B504  mov     edx, [eax+118h]
0046B50A  push    0
0046B50C  push    1
0046B50E  call    edx
0046B510  mov     ecx, [esp+10Ch+var_C]
0046B517  pop     esi
0046B518  mov     large fs:0, ecx
0046B51F  add     esp, 108h
0046B525  retn    8
006DE5D0  lea     ecx, [ebp-0B4h]; void *
006DE5D6  jmp     sub_401330
006DE5DB  lea     ecx, [ebp-7Ch]; void *
006DE5DE  jmp     sub_401330
006DE5E3  lea     ecx, [ebp-0ECh]; void *
006DE5E9  jmp     sub_401330
006DE5EE  lea     ecx, [ebp-0D0h]; void *
006DE5F4  jmp     sub_401330
006DE5F9  lea     ecx, [ebp-108h]; void *
006DE5FF  jmp     sub_401330
006DE604  lea     ecx, [ebp-44h]; void *
006DE607  jmp     sub_401330
006DE60C  lea     ecx, [ebp-28h]; void *
006DE60F  jmp     sub_401330
006DE614  lea     ecx, [ebp-60h]; void *
006DE617  jmp     sub_401330
006DE61C  lea     ecx, [ebp-98h]; void *
006DE622  jmp     sub_401330
006DE627  mov     eax, offset stru_730828
006DE62C  jmp     ___CxxFrameHandler3
