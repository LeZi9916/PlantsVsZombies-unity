0049B120  push    ebx
0049B121  mov     ebx, [esi+0D28h]
0049B127  cmp     dword ptr [ebx+91Ch], 16h
0049B12E  push    edi
0049B12F  jnz     short loc_49B16A
0049B131  mov     edi, 1Dh
0049B136  mov     ecx, esi
0049B138  call    sub_49C7E0
0049B13D  test    al, al
0049B13F  jnz     short loc_49B16A
0049B141  push    1; int
0049B143  push    offset aRepickButton; "[REPICK_BUTTON]"
0049B148  push    offset aDialogButtonYe; "[DIALOG_BUTTON_YES]"
0049B14D  push    offset aDialogWarning; "[DIALOG_WARNING]"
0049B152  push    1Ch; int
0049B154  push    ebx; int
0049B155  mov     ecx, offset off_718668; Source
0049B15A  call    sub_45D250
0049B15F  cmp     eax, 3E8h
0049B164  jnz     loc_49B4C6
0049B16A  mov     eax, [esi+0D28h]
0049B170  cmp     dword ptr [eax+91Ch], 0
0049B177  mov     ebx, 0Bh
0049B17C  jnz     short loc_49B1C0
0049B17E  mov     eax, [eax+950h]
0049B184  test    eax, eax
0049B186  jz      short loc_49B18E
0049B188  cmp     dword ptr [eax+54h], 0
0049B18C  jg      short loc_49B1C0
0049B18E  mov     eax, [esi+0D2Ch]
0049B194  cmp     [eax+5568h], ebx
0049B19A  jnz     short loc_49B1C0
0049B19C  mov     edi, 8
0049B1A1  mov     ecx, esi
0049B1A3  call    sub_49C7E0
0049B1A8  test    al, al
0049B1AA  jnz     short loc_49B1C0
0049B1AC  push    offset off_7186B0; Source
0049B1B1  mov     eax, esi
0049B1B3  call    sub_49AE80
0049B1B8  test    al, al
0049B1BA  jz      loc_49B4C6
0049B1C0  mov     edi, 1
0049B1C5  mov     ecx, esi
0049B1C7  call    sub_49C7E0
0049B1CC  test    al, al
0049B1CE  jnz     short loc_49B236
0049B1D0  mov     edi, 29h ; ')'
0049B1D5  mov     ecx, esi
0049B1D7  call    sub_49C7E0
0049B1DC  test    al, al
0049B1DE  jnz     short loc_49B236
0049B1E0  mov     edi, 9
0049B1E5  mov     ecx, esi
0049B1E7  call    sub_49C7E0
0049B1EC  test    al, al
0049B1EE  jnz     short loc_49B236
0049B1F0  mov     edi, [esi+0D2Ch]
0049B1F6  mov     edx, [edi+174h]
0049B1FC  call    sub_446350
0049B201  test    al, al
0049B203  jnz     short loc_49B236
0049B205  mov     eax, [esi+0D28h]
0049B20B  cmp     dword ptr [eax+91Ch], 1Fh
0049B212  jz      short loc_49B236
0049B214  call    sub_461580
0049B219  test    al, al
0049B21B  jz      short loc_49B22F
0049B21D  cmp     [edi+5568h], ebx
0049B223  jnz     short loc_49B22F
0049B225  push    offset off_7186F8
0049B22A  jmp     loc_49B2AF
0049B22F  push    offset off_71873C
0049B234  jmp     short loc_49B2AF
0049B236  mov     ebx, [esi+0D2Ch]
0049B23C  mov     eax, [ebx+5564h]
0049B242  cmp     eax, 2
0049B245  jz      short loc_49B24C
0049B247  cmp     eax, 3
0049B24A  jnz     short loc_49B2BE
0049B24C  mov     edi, 10h
0049B251  mov     ecx, esi
0049B253  call    sub_49C7E0
0049B258  test    al, al
0049B25A  jnz     short loc_49B2BE
0049B25C  mov     edi, 18h
0049B261  mov     ecx, esi
0049B263  call    sub_49C7E0
0049B268  test    al, al
0049B26A  jnz     short loc_49B2BE
0049B26C  mov     edi, 13h
0049B271  mov     ecx, esi
0049B273  call    sub_49C7E0
0049B278  test    al, al
0049B27A  jnz     short loc_49B2BE
0049B27C  mov     edx, [ebx+174h]
0049B282  call    sub_446350
0049B287  test    al, al
0049B289  jnz     short loc_49B2BE
0049B28B  mov     eax, [esi+0D28h]
0049B291  call    sub_461580
0049B296  test    al, al
0049B298  jz      short loc_49B2AA
0049B29A  cmp     dword ptr [ebx+5568h], 15h
0049B2A1  jnz     short loc_49B2AA
0049B2A3  push    offset off_718774
0049B2A8  jmp     short loc_49B2AF
0049B2AA  push    offset off_7187AC; Source
0049B2AF  mov     eax, esi
0049B2B1  call    sub_49AE80
0049B2B6  test    al, al
0049B2B8  jz      loc_49B4C6
0049B2BE  mov     eax, [esi+0D2Ch]
0049B2C4  mov     eax, [eax+5564h]
0049B2CA  cmp     eax, 4
0049B2CD  jz      short loc_49B2D4
0049B2CF  cmp     eax, 5
0049B2D2  jnz     short loc_49B32E
0049B2D4  mov     edi, 21h ; '!'
0049B2D9  mov     ecx, esi
0049B2DB  call    sub_49C7E0
0049B2E0  test    al, al
0049B2E2  jnz     short loc_49B32E
0049B2E4  mov     edi, [esi+0D28h]
0049B2EA  cmp     [edi+9E8h], al
0049B2F0  jnz     short loc_49B32E
0049B2F2  mov     ecx, [edi+99Ch]
0049B2F8  test    ecx, ecx
0049B2FA  jz      short loc_49B305
0049B2FC  call    sub_651F30
0049B301  test    al, al
0049B303  jnz     short loc_49B30E
0049B305  cmp     dword ptr [edi+9E4h], 1
0049B30C  jz      short loc_49B32E
0049B30E  mov     eax, edi
0049B310  call    sub_460EB0
0049B315  cmp     eax, 21h ; '!'
0049B318  jle     short loc_49B32E
0049B31A  push    offset off_7187E4; Source
0049B31F  mov     eax, esi
0049B321  call    sub_49AE80
0049B326  test    al, al
0049B328  jz      loc_49B4C6
0049B32E  mov     ebx, [esi+0D28h]
0049B334  cmp     dword ptr [ebx+91Ch], 24h ; '$'
0049B33B  jnz     short loc_49B376
0049B33D  mov     edi, 3
0049B342  mov     ecx, esi
0049B344  call    sub_49C7E0
0049B349  test    al, al
0049B34B  jnz     short loc_49B376
0049B34D  push    1; int
0049B34F  push    offset aRepickButton; "[REPICK_BUTTON]"
0049B354  push    offset aDialogButtonYe; "[DIALOG_BUTTON_YES]"
0049B359  push    offset aDialogWarning; "[DIALOG_WARNING]"
0049B35E  push    1Ch; int
0049B360  push    ebx; int
0049B361  mov     ecx, offset off_718820; Source
0049B366  call    sub_45D250
0049B36B  cmp     eax, 3E8h
0049B370  jnz     loc_49B4C6
0049B376  mov     ebx, [esi+0D28h]
0049B37C  cmp     dword ptr [ebx+91Ch], 28h ; '('
0049B383  jnz     short loc_49B3DE
0049B385  mov     edi, 1Dh
0049B38A  mov     ecx, esi
0049B38C  call    sub_49C7E0
0049B391  test    al, al
0049B393  jz      short loc_49B3B5
0049B395  mov     edi, 25h ; '%'
0049B39A  mov     ecx, esi
0049B39C  call    sub_49C7E0
0049B3A1  test    al, al
0049B3A3  jz      short loc_49B3B5
0049B3A5  mov     edi, 3
0049B3AA  mov     ecx, esi
0049B3AC  call    sub_49C7E0
0049B3B1  test    al, al
0049B3B3  jnz     short loc_49B3DE
0049B3B5  push    1; int
0049B3B7  push    offset aRepickButton; "[REPICK_BUTTON]"
0049B3BC  push    offset aDialogButtonYe; "[DIALOG_BUTTON_YES]"
0049B3C1  push    offset aDialogWarning; "[DIALOG_WARNING]"
0049B3C6  push    1Ch; int
0049B3C8  push    ebx; int
0049B3C9  mov     ecx, offset off_718868; Source
0049B3CE  call    sub_45D250
0049B3D3  cmp     eax, 3E8h
0049B3D8  jnz     loc_49B4C6
0049B3DE  mov     eax, esi
0049B3E0  call    sub_49AEB0
0049B3E5  test    al, al
0049B3E7  jz      short loc_49B438
0049B3E9  mov     edi, esi
0049B3EB  call    sub_49AF00
0049B3F0  test    al, al
0049B3F2  jnz     short loc_49B438
0049B3F4  mov     edi, 2Bh ; '+'
0049B3F9  mov     ecx, esi
0049B3FB  call    sub_49C7E0
0049B400  test    al, al
0049B402  jnz     short loc_49B438
0049B404  mov     edi, 1Ah
0049B409  mov     ecx, esi
0049B40B  call    sub_49C7E0
0049B410  test    al, al
0049B412  jnz     short loc_49B438
0049B414  mov     edi, 1Bh
0049B419  mov     ecx, esi
0049B41B  call    sub_49C7E0
0049B420  test    al, al
0049B422  jnz     short loc_49B438
0049B424  push    offset off_7188A4; Source
0049B429  mov     eax, esi
0049B42B  call    sub_49AE80
0049B430  test    al, al
0049B432  jz      loc_49B4C6
0049B438  push    28h ; '('
0049B43A  push    esi
0049B43B  mov     ecx, 7
0049B440  call    sub_49AF50
0049B445  test    al, al
0049B447  jz      short loc_49B4C6
0049B449  push    2Ch ; ','
0049B44B  push    esi
0049B44C  mov     ecx, 27h ; '''
0049B451  call    sub_49AF50
0049B456  test    al, al
0049B458  jz      short loc_49B4C6
0049B45A  push    29h ; ')'
0049B45C  push    esi
0049B45D  mov     ecx, 1
0049B462  call    sub_49AF50
0049B467  test    al, al
0049B469  jz      short loc_49B4C6
0049B46B  push    2Eh ; '.'
0049B46D  push    esi
0049B46E  mov     ecx, 15h
0049B473  call    sub_49AF50
0049B478  test    al, al
0049B47A  jz      short loc_49B4C6
0049B47C  push    2Fh ; '/'
0049B47E  push    esi
0049B47F  mov     ecx, 22h ; '"'
0049B484  call    sub_49AF50
0049B489  test    al, al
0049B48B  jz      short loc_49B4C6
0049B48D  push    2Dh ; '-'
0049B48F  push    esi
0049B490  mov     ecx, 1Fh
0049B495  call    sub_49AF50
0049B49A  test    al, al
0049B49C  jz      short loc_49B4C6
0049B49E  push    2Ah ; '*'
0049B4A0  push    esi
0049B4A1  mov     ecx, 0Ah
0049B4A6  call    sub_49AF50
0049B4AB  test    al, al
0049B4AD  jz      short loc_49B4C6
0049B4AF  push    2Bh ; '+'
0049B4B1  push    esi
0049B4B2  mov     ecx, 10h
0049B4B7  call    sub_49AF50
0049B4BC  test    al, al
0049B4BE  jz      short loc_49B4C6
0049B4C0  push    esi
0049B4C1  call    sub_49C820
0049B4C6  pop     edi
0049B4C7  pop     ebx
0049B4C8  retn
