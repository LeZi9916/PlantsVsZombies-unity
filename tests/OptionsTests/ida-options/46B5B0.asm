0046B5B0  mov     eax, large fs:0
0046B5B6  push    0FFFFFFFFh
0046B5B8  push    offset SEH_46B5B0
0046B5BD  push    eax
0046B5BE  mov     large fs:0, esp
0046B5C5  sub     esp, 8Ch
0046B5CB  push    ebp
0046B5CC  push    esi
0046B5CD  mov     esi, [esp+0A0h+arg_0]
0046B5D4  push    esi
0046B5D5  mov     ebp, ecx
0046B5D7  call    sub_5D1040
0046B5DC  cmp     esi, 3; switch 4 cases
0046B5DF  ja      def_46B5E7; jumptable 0046B5E7 default case
0046B5E5  push    ebx
0046B5E6  push    edi
0046B5E7  jmp     ds:jpt_46B5E7[esi*4]; switch jump
0046B5EE  mov     edi, [ebp+0D0h]; jumptable 0046B5E7 case 0
0046B5F4  or      ebx, 0FFFFFFFFh
0046B5F7  call    sub_45CDA0
0046B5FC  mov     edx, [eax]
0046B5FE  mov     ecx, eax
0046B600  mov     eax, [edx+12Ch]
0046B606  push    1
0046B608  call    eax
0046B60A  jmp     loc_46B8BC; jumptable 0046B5E7 case 3
0046B60F  cmp     byte ptr [ebp+0F4h], 0; jumptable 0046B5E7 case 1
0046B616  mov     esi, [ebp+0D0h]
0046B61C  jz      short loc_46B642
0046B61E  call    sub_45E740
0046B623  mov     esi, [ebp+0D0h]
0046B629  call    sub_45C590
0046B62E  mov     esi, [ebp+0D0h]
0046B634  push    0
0046B636  push    1
0046B638  call    sub_45C5D0
0046B63D  jmp     loc_46B8BC; jumptable 0046B5E7 case 3
0046B642  mov     edx, [esi+868h]
0046B648  test    edx, edx
0046B64A  jz      short loc_46B67A
0046B64C  mov     eax, edx
0046B64E  call    sub_414B70
0046B653  test    al, al
0046B655  jnz     short loc_46B66E
0046B657  test    edx, edx
0046B659  jz      short loc_46B67A
0046B65B  mov     edx, [edx+174h]
0046B661  test    edx, edx
0046B663  jz      short loc_46B67A
0046B665  call    sub_446350
0046B66A  test    al, al
0046B66C  jz      short loc_46B67A
0046B66E  mov     ecx, esi
0046B670  call    sub_45CB10
0046B675  jmp     loc_46B8BC; jumptable 0046B5E7 case 3
0046B67A  mov     ecx, esi
0046B67C  mov     dword ptr [ecx+9ACh], 4
0046B686  mov     eax, [ebp+0D0h]
0046B68C  call    sub_45CAC0
0046B691  jmp     loc_46B8BC; jumptable 0046B5E7 case 3
0046B696  mov     ecx, [ebp+0D0h]; jumptable 0046B5E7 case 2
0046B69C  xor     eax, eax
0046B69E  cmp     [ecx+868h], eax
0046B6A4  jz      loc_46B8BC; jumptable 0046B5E7 case 3
0046B6AA  mov     edx, 7
0046B6AF  xor     esi, esi
0046B6B1  mov     [esp+0A8h+var_64], edx
0046B6B5  mov     [esp+0A8h+var_68], eax
0046B6B9  mov     [esp+0A8h+var_78], si
0046B6BE  mov     [esp+0A8h+var_4], eax
0046B6C5  mov     [esp+0A8h+var_80], edx
0046B6C9  xor     edx, edx
0046B6CB  mov     [esp+0A8h+var_84], eax
0046B6CF  mov     [esp+0A8h+var_94], dx
0046B6D4  mov     ebx, 1
0046B6D9  mov     eax, ecx
0046B6DB  mov     byte ptr [esp+0A8h+var_4], bl
0046B6E2  call    sub_460AE0
0046B6E7  test    al, al
0046B6E9  jz      short loc_46B701
0046B6EB  lea     eax, [esp+0A8h+var_7C]
0046B6EF  push    eax
0046B6F0  mov     edx, offset aRestartPuzzleH; "[RESTART_PUZZLE_HEADER]"
0046B6F5  call    sub_40D090
0046B6FA  mov     edx, offset aRestartPuzzleB; "[RESTART_PUZZLE_BODY]"
0046B6FF  jmp     short loc_46B761
0046B701  mov     eax, ecx
0046B703  call    sub_460B00
0046B708  test    al, al
0046B70A  jz      short loc_46B727
0046B70C  lea     edx, [esp+0A8h+var_7C]
0046B710  push    edx
0046B711  mov     edx, offset aRestartChallen; "[RESTART_CHALLENGE_HEADER]"
0046B716  call    sub_40D090
0046B71B  lea     eax, [esp+0A8h+var_98]
0046B71F  push    eax
0046B720  mov     edx, offset aRestartChallen_0; "[RESTART_CHALLENGE_BODY]"
0046B725  jmp     short loc_46B766
0046B727  mov     eax, ecx
0046B729  call    sub_460AC0
0046B72E  test    al, al
0046B730  jz      short loc_46B74D
0046B732  lea     ecx, [esp+0A8h+var_7C]
0046B736  push    ecx
0046B737  mov     edx, offset aRestartSurviva; "[RESTART_SURVIVAL_HEADER]"
0046B73C  call    sub_40D090
0046B741  lea     edx, [esp+0A8h+var_98]
0046B745  push    edx
0046B746  mov     edx, offset aRestartSurviva_0; "[RESTART_SURVIVAL_BODY]"
0046B74B  jmp     short loc_46B766
0046B74D  lea     eax, [esp+0A8h+var_7C]
0046B751  push    eax
0046B752  mov     edx, offset aRestartLevelHe; "[RESTART_LEVEL_HEADER]"
0046B757  call    sub_40D090
0046B75C  mov     edx, offset aRestartLevelBo; "[RESTART_LEVEL_BODY]"
0046B761  lea     ecx, [esp+0A8h+var_98]
0046B765  push    ecx
0046B766  call    sub_40D090
0046B76B  push    offset unk_708460
0046B770  lea     ecx, [esp+0ACh+var_60]
0046B774  call    sub_40D030
0046B779  push    ebx
0046B77A  lea     eax, [esp+0ACh+var_60]
0046B77E  push    eax
0046B77F  lea     eax, [esp+0B0h+var_98]
0046B783  push    eax
0046B784  mov     byte ptr [esp+0B4h+var_4], 2
0046B78C  mov     ecx, [ebp+0D0h]
0046B792  mov     edx, [ecx]
0046B794  mov     edx, [edx+120h]
0046B79A  lea     eax, [esp+0B4h+var_7C]
0046B79E  push    eax
0046B79F  push    ebx
0046B7A0  push    17h
0046B7A2  call    edx
0046B7A4  lea     ecx, [esp+0A8h+var_60]; void *
0046B7A8  mov     edi, eax
0046B7AA  mov     byte ptr [esp+0A8h+var_4], bl
0046B7B1  call    sub_401330
0046B7B6  mov     edx, offset aRestartLabel; "[RESTART_LABEL]"
0046B7BB  lea     esi, [esp+0A8h+var_44]
0046B7BF  call    sub_52EB40
0046B7C4  push    0FFFFFFFFh
0046B7C6  mov     byte ptr [esp+0ACh+var_4], 3
0046B7CE  mov     ecx, [edi+178h]
0046B7D4  push    0
0046B7D6  add     ecx, 0A4h
0046B7DC  push    eax
0046B7DD  call    sub_40D0E0
0046B7E2  mov     ecx, esi; void *
0046B7E4  mov     byte ptr [esp+0A8h+var_4], bl
0046B7EB  call    sub_401330
0046B7F0  mov     edx, offset aDialogButtonCa; "[DIALOG_BUTTON_CANCEL]"
0046B7F5  lea     esi, [esp+0A8h+var_28]
0046B7FC  call    sub_52EB40
0046B801  push    0FFFFFFFFh
0046B803  mov     byte ptr [esp+0ACh+var_4], 4
0046B80B  mov     ecx, [edi+17Ch]
0046B811  push    0
0046B813  add     ecx, 0A4h
0046B819  push    eax
0046B81A  call    sub_40D0E0
0046B81F  mov     ecx, esi; void *
0046B821  mov     byte ptr [esp+0A8h+var_4], bl
0046B828  call    sub_401330
0046B82D  mov     eax, [edi]
0046B82F  mov     edx, [eax+12Ch]
0046B835  push    ebx
0046B836  mov     ecx, edi
0046B838  call    edx
0046B83A  cmp     eax, 3E8h
0046B83F  jnz     short loc_46B8AA
0046B841  mov     eax, [ebp+0D0h]
0046B847  mov     esi, [eax+960h]
0046B84D  call    sub_468F30
0046B852  mov     ecx, [ebp+0D0h]
0046B858  mov     eax, [ecx+888h]
0046B85E  call    sub_52A960
0046B863  mov     esi, [ebp+0D0h]
0046B869  call    sub_45E740
0046B86E  mov     edx, [ebp+0D0h]
0046B874  mov     dword ptr [edx+9ACh], 3
0046B87E  mov     eax, [ebp+0D0h]
0046B884  mov     ecx, [eax+868h]
0046B88A  mov     dl, [ecx+5778h]
0046B890  mov     [eax+9B0h], dl
0046B896  mov     esi, [ebp+0D0h]
0046B89C  mov     eax, [esi+91Ch]
0046B8A2  push    0
0046B8A4  push    eax
0046B8A5  call    sub_45C100
0046B8AA  lea     ecx, [esp+0A8h+var_98]; void *
0046B8AE  call    sub_401330
0046B8B3  lea     ecx, [esp+0A8h+var_7C]; void *
0046B8B7  call    sub_401330
0046B8BC  pop     edi; jumptable 0046B5E7 case 3
0046B8BD  pop     ebx
0046B8BE  mov     ecx, [esp+0A0h+var_C]; jumptable 0046B5E7 default case
0046B8C5  pop     esi
0046B8C6  pop     ebp
0046B8C7  mov     large fs:0, ecx
0046B8CE  add     esp, 98h
0046B8D4  retn    4
006EE8B0  lea     ecx, [ebp-7Ch]; void *
006EE8B3  jmp     sub_401330
006EE8B8  lea     ecx, [ebp-98h]; void *
006EE8BE  jmp     sub_401330
006EE8C3  lea     ecx, [ebp-60h]; void *
006EE8C6  jmp     sub_401330
006EE8CB  lea     ecx, [ebp-44h]; void *
006EE8CE  jmp     sub_401330
006EE8D3  lea     ecx, [ebp-28h]; void *
006EE8D6  jmp     sub_401330
006EE8DB  mov     eax, offset stru_741330
006EE8E0  jmp     ___CxxFrameHandler3
