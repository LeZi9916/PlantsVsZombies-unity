004578E0 push    ebp
004578E1 mov     ebp, ecx
004578E3 cmp     dword ptr [ebp+0BCh], 0
004578EA jnz     def_457996; jumptable 00457996 default case
004578F0 mov     eax, [esp+4+arg_0]
004578F4 cmp     eax, 65h ; 'e'
004578F7 jnz     short loc_457928
004578F9 cmp     byte ptr [ebp+50h], 0
004578FD jz      loc_457988
00457903 mov     eax, [ebp+4]
00457906 push    3; int
00457908 push    offset unk_708460; int
0045790D push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
00457912 push    offset aModeLocked; "[MODE_LOCKED]"
00457917 push    31h ; '1'; int
00457919 push    eax; int
0045791A mov     ecx, offset aMinigameLocked; "[MINIGAME_LOCKED_MESSAGE]"
0045791F call    sub_45D250
00457924 pop     ebp
00457925 retn    4
00457928 cmp     eax, 66h ; 'f'
0045792B jnz     short loc_457958
0045792D cmp     byte ptr [ebp+51h], 0
00457931 jz      short loc_457988
00457933 mov     ecx, [ebp+4]
00457936 push    3; int
00457938 push    offset unk_708460; int
0045793D push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
00457942 push    offset aModeLocked; "[MODE_LOCKED]"
00457947 push    31h ; '1'; int
00457949 push    ecx; int
0045794A mov     ecx, offset aPuzzleLockedMe; "[PUZZLE_LOCKED_MESSAGE]"
0045794F call    sub_45D250
00457954 pop     ebp
00457955 retn    4
00457958 cmp     eax, 6Eh ; 'n'
0045795B jnz     short loc_457988
0045795D cmp     byte ptr [ebp+52h], 0
00457961 jz      short loc_457988
00457963 mov     edx, [ebp+4]
00457966 push    3; int
00457968 push    offset unk_708460; int
0045796D push    offset aDialogButtonOk; "[DIALOG_BUTTON_OK]"
00457972 push    offset aModeLocked; "[MODE_LOCKED]"
00457977 push    31h ; '1'; int
00457979 push    edx; int
0045797A mov     ecx, offset off_70DC24; Source
0045797F call    sub_45D250
00457984 pop     ebp
00457985 retn    4
00457988 add     eax, 0FFFFFF9Ch; switch 16 cases
0045798B cmp     eax, 0Fh
0045798E ja      def_457996; jumptable 00457996 default case
00457994 push    esi
00457995 push    edi
00457996 jmp     ds:jpt_457996[eax*4]; switch jump
0045799D add     ebp, 0FFFFFF60h; jumptable 00457996 case 100
004579A3 push    ebp
004579A4 call    sub_457580
004579A9 pop     edi
004579AA pop     esi
004579AB pop     ebp
004579AC retn    4
004579AF mov     esi, [ebp+4]; jumptable 00457996 case 101
004579B2 call    sub_45C590
004579B7 mov     esi, [ebp+4]
004579BA push    1
004579BC call    sub_45C840
004579C1 pop     edi
004579C2 pop     esi
004579C3 pop     ebp
004579C4 retn    4
004579C7 mov     esi, [ebp+4]; jumptable 00457996 case 102
004579CA call    sub_45C590
004579CF mov     esi, [ebp+4]
004579D2 push    3
004579D4 call    sub_45C840
004579D9 pop     edi
004579DA pop     esi
004579DB pop     ebp
004579DC retn    4
004579DF mov     esi, [ebp+4]; jumptable 00457996 case 110
004579E2 call    sub_45C590
004579E7 mov     esi, [ebp+4]
004579EA push    0
004579EC call    sub_45C840
004579F1 pop     edi
004579F2 pop     esi
004579F3 pop     ebp
004579F4 retn    4
004579F7 mov     esi, [ebp+4]; jumptable 00457996 case 111
004579FA mov     eax, [esi+950h]
00457A00 cmp     byte ptr [eax+381h], 1
00457A07 jnz     short loc_457A1A
00457A09 lea     esi, [ebp-0A0h]
00457A0F call    sub_4584E0
00457A14 pop     edi
00457A15 pop     esi
00457A16 pop     ebp
00457A17 retn    4
00457A1A call    sub_45CE50
00457A1F pop     edi
00457A20 pop     esi
00457A21 pop     ebp
00457A22 retn    4
00457A25 lea     eax, [ebp-0A0h]; jumptable 00457996 case 112
00457A2B xor     edx, edx
00457A2D xor     ecx, ecx
00457A2F call    sub_4584B0
00457A34 pop     edi
00457A35 pop     esi
00457A36 pop     ebp
00457A37 retn    4
00457A3A lea     eax, [ebp-0A0h]; jumptable 00457996 case 113
00457A40 call    sub_458570
00457A45 pop     edi
00457A46 pop     esi
00457A47 pop     ebp
00457A48 retn    4
00457A4B mov     ecx, [ebp+4]; jumptable 00457996 case 105
00457A4E call    sub_460410
00457A53 pop     edi
00457A54 pop     esi
00457A55 pop     ebp
00457A56 retn    4
00457A59 mov     esi, [ebp+4]; jumptable 00457996 case 104
00457A5C call    sub_45C590
00457A61 mov     esi, [ebp+4]
00457A64 push    0
00457A66 push    2
00457A68 call    sub_45C5D0
00457A6D pop     edi
00457A6E pop     esi
00457A6F pop     ebp
00457A70 retn    4
00457A73 mov     edi, [ebp+4]; jumptable 00457996 case 103
00457A76 push    1
00457A78 call    sub_45CCF0
00457A7D pop     edi
00457A7E pop     esi
00457A7F pop     ebp
00457A80 retn    4
00457A83 mov     edi, [ebp+4]; jumptable 00457996 case 106
00457A86 call    sub_45D640
00457A8B pop     edi
00457A8C pop     esi
00457A8D pop     ebp
00457A8E retn    4
00457A91 mov     edi, [ebp+4]; jumptable 00457996 case 107
00457A94 call    sub_45C950
00457A99 mov     esi, eax
00457A9B mov     edx, [esi]
00457A9D mov     eax, [edx+12Ch]
00457AA3 push    1
00457AA5 mov     ecx, esi
00457AA7 call    eax
00457AA9 cmp     byte ptr [esi+245h], 0
00457AB0 jz      short loc_457ACC
00457AB2 mov     esi, [ebp+4]
00457AB5 call    sub_45C590
00457ABA mov     esi, [ebp+4]
00457ABD push    0
00457ABF push    32h ; '2'
00457AC1 call    sub_45C100
00457AC6 pop     edi
00457AC7 pop     esi
00457AC8 pop     ebp
00457AC9 retn    4
00457ACC mov     ecx, [ebp+4]
00457ACF mov     eax, [ecx+960h]
00457AD5 mov     edi, 7
00457ADA call    sub_469A10
00457ADF pop     edi
00457AE0 pop     esi
00457AE1 pop     ebp
00457AE2 retn    4
00457AE5 mov     edi, [ebp+4]; jumptable 00457996 case 108
00457AE8 push    ebx
00457AE9 or      ebx, 0FFFFFFFFh
00457AEC call    sub_45CDA0
00457AF1 mov     edx, [eax]
00457AF3 mov     ecx, eax
00457AF5 mov     eax, [edx+12Ch]
00457AFB push    1
00457AFD call    eax
00457AFF mov     ecx, [ebp+4]
00457B02 mov     eax, [ecx+960h]
00457B08 lea     edi, [ebx+8]
00457B0B call    sub_469A10
00457B10 pop     ebx
00457B11 pop     edi
00457B12 pop     esi
00457B13 pop     ebp
00457B14 retn    4
00457B17 mov     esi, [ebp+4]; jumptable 00457996 case 109
00457B1A call    sub_45C590
00457B1F mov     esi, [ebp+4]
00457B22 push    0
00457B24 push    2Bh ; '+'
00457B26 call    sub_45C100
00457B2B lea     eax, [ebp-0A0h]
00457B31 call    sub_4578B0
00457B36 test    al, al
00457B38 jz      short loc_457B70
00457B3A mov     edx, [ebp+4]
00457B3D mov     ecx, [edx+940h]
00457B43 call    sub_538080
00457B48 pop     edi
00457B49 pop     esi
00457B4A pop     ebp
00457B4B retn    4
00457B4E lea     eax, [ebp-0A0h]; jumptable 00457996 case 114
00457B54 call    sub_458530
00457B59 pop     edi
00457B5A pop     esi
00457B5B pop     ebp
00457B5C retn    4
00457B5F mov     ecx, [ebp+4]; jumptable 00457996 case 115
00457B62 cmp     dword ptr [ecx+950h], 0
00457B69 jz      short loc_457B70
00457B6B call    sub_45DBC0
00457B70 pop     edi
00457B71 pop     esi
00457B72 pop     ebp; jumptable 00457996 default case
00457B73 retn    4
