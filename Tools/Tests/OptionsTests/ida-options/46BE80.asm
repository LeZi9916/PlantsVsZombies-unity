0046BE80  push    ebp
0046BE81  mov     ebp, esp
0046BE83  and     esp, 0FFFFFFF8h
0046BE86  mov     ecx, [ebp+arg_4]
0046BE89  sub     esp, 1Ch
0046BE8C  push    ebx
0046BE8D  mov     ebx, [ebp+arg_0]
0046BE90  mov     edx, [ebx+4]
0046BE93  push    esi
0046BE94  push    edi
0046BE95  xor     edi, edi
0046BE97  mov     esi, eax
0046BE99  mov     [ebx+28h], ecx
0046BE9C  mov     [ebx+1Ch], esi
0046BE9F  cmp     edx, edi
0046BEA1  jz      short loc_46BEFA
0046BEA3  mov     eax, [edx+0A4h]
0046BEA9  cmp     dword ptr [eax+91Ch], 2Bh ; '+'
0046BEB0  jnz     short loc_46BEDB
0046BEB2  mov     edi, [edx+5564h]
0046BEB8  cmp     edi, 6
0046BEBB  jz      short loc_46BEC7
0046BEBD  cmp     edi, 8
0046BEC0  jz      short loc_46BEC7
0046BEC2  cmp     edi, 7
0046BEC5  jnz     short loc_46BEDB
0046BEC7  mov     eax, [eax+940h]
0046BECD  mov     edi, ecx
0046BECF  call    sub_537170
0046BED4  mov     edx, [ebx+4]
0046BED7  mov     ecx, edi
0046BED9  jmp     short loc_46BEE4
0046BEDB  lea     eax, [ecx+ecx*4]
0046BEDE  shl     eax, 4
0046BEE1  add     eax, 28h ; '('
0046BEE4  mov     [ebx+8], eax
0046BEE7  mov     eax, esi
0046BEE9  mov     ebx, edx
0046BEEB  call    sub_428350
0046BEF0  mov     ecx, [ebp+arg_0]
0046BEF3  mov     [ecx+0Ch], eax
0046BEF6  mov     ebx, ecx
0046BEF8  xor     edi, edi
0046BEFA  push    7
0046BEFC  mov     [ebx+2Ch], edi
0046BEFF  mov     byte ptr [ebx+140h], 1
0046BF06  mov     [ebx+30h], edi
0046BF09  mov     [ebx+90h], edi
0046BF0F  call    sub_5EECC0
0046BF14  fldz
0046BF16  mov     edx, [ebp+arg_C]
0046BF19  fst     dword ptr [ebx+0C0h]
0046BF1F  mov     esi, [ebp+arg_8]
0046BF22  fstp    dword ptr [ebx+0C4h]
0046BF28  add     eax, 0Ch
0046BF2B  mov     [ebx+34h], eax
0046BF2E  or      eax, 0FFFFFFFFh
0046BF31  add     esp, 4
0046BF34  mov     [ebx+80h], eax
0046BF3A  mov     [ebx+84h], eax
0046BF40  mov     eax, [ebx+1Ch]
0046BF43  push    64h ; 'd'; Size
0046BF45  mov     [ebx+88h], eax
0046BF4B  mov     eax, 50h ; 'P'
0046BF50  lea     ecx, [ebx+0C8h]
0046BF56  push    edi; Val
0046BF57  push    ecx; void *
0046BF58  mov     dword ptr [ebx+38h], 5
0046BF5F  mov     [ebx+3Ch], edi
0046BF62  mov     byte ptr [ebx+141h], 0
0046BF69  mov     byte ptr [ebx+142h], 0
0046BF70  mov     [ebx+24h], esi
0046BF73  mov     [ebx+138h], edx
0046BF79  mov     dword ptr [ebx+40h], 12Ch
0046BF80  mov     [ebx+50h], edi
0046BF83  mov     dword ptr [ebx+4Ch], 0C8h
0046BF8A  mov     [ebx+54h], edi
0046BF8D  mov     [ebx+8Ch], edi
0046BF93  mov     [ebx+94h], edi
0046BF99  mov     [ebx+98h], edi
0046BF9F  mov     [ebx+9Ch], edi
0046BFA5  mov     [ebx+0A0h], edi
0046BFAB  mov     [ebx+0A4h], edi
0046BFB1  mov     [ebx+0A8h], edi
0046BFB7  mov     [ebx+0ACh], edi
0046BFBD  mov     [ebx+0B0h], edi
0046BFC3  mov     [ebx+0B4h], edi
0046BFC9  mov     [ebx+0B8h], edi
0046BFCF  mov     [ebx+0BCh], edi
0046BFD5  mov     [ebx+10h], eax
0046BFD8  mov     [ebx+14h], eax
0046BFDB  call    _memset
0046BFE0  lea     eax, [esi+esi*8]
0046BFE3  lea     eax, ds:74AA00h[eax*4]
0046BFEA  mov     byte ptr [ebx+143h], 0
0046BFF1  mov     [ebx+130h], edi
0046BFF7  mov     [ebx+134h], edi
0046BFFD  mov     dword ptr [ebx+13Ch], 0FFFFFFFFh
0046C007  mov     edx, [eax+1Ch]
0046C00A  mov     [esp+34h+var_18], eax
0046C00E  mov     [ebx+5Ch], edx
0046C011  mov     eax, [eax+18h]
0046C014  add     esp, 0Ch
0046C017  mov     esi, ebx
0046C019  mov     [ebx+48h], eax
0046C01C  call    sub_46CAF0
0046C021  mov     ecx, [esp+28h+var_18]
0046C025  xor     esi, esi
0046C027  mov     [ebx+20h], eax
0046C02A  cmp     dword ptr [ecx+8], 0FFFFFFFFh
0046C02E  mov     [esp+28h+var_14], esi
0046C032  jz      loc_46C17D
0046C038  mov     edx, [ebx+1Ch]
0046C03B  mov     eax, [ebx+28h]
0046C03E  mov     esi, [ebx+24h]
0046C041  mov     edi, [ebx+4]
0046C044  push    edx
0046C045  push    eax
0046C046  mov     eax, ebx
0046C048  call    sub_473490
0046C04D  fstp    [esp+30h+var_14]
0046C051  mov     ecx, [ebx+20h]
0046C054  fld     [esp+30h+var_14]
0046C058  mov     edx, [esp+30h+var_18]
0046C05C  mov     esi, [ebp+arg_0]
0046C05F  mov     ebx, [edx+8]
0046C062  mov     eax, [esi]
0046C064  add     esp, 8
0046C067  inc     ecx
0046C068  push    ecx; int
0046C069  sub     esp, 8
0046C06C  fstp    [esp+34h+var_30]; float
0046C070  fldz
0046C072  fstp    [esp+34h+var_34]; float
0046C075  call    sub_461000
0046C07A  fld     ds:flt_72753C
0046C080  mov     ebx, eax
0046C082  push    ecx
0046C083  fstp    [esp+2Ch+var_2C]; float
0046C086  mov     [esp+2Ch+var_14], ebx
0046C08A  mov     dword ptr [ebx+10h], 0
0046C091  call    sub_5EECE0
0046C096  fadd    ds:dbl_727208
0046C09C  add     esp, 4
0046C09F  push    offset aAnimIdle; "anim_idle"
0046C0A4  fstp    dword ptr [ebx+8]
0046C0A7  call    sub_482780
0046C0AC  test    al, al
0046C0AE  jz      short loc_46C0BC
0046C0B0  push    offset aAnimIdle; "anim_idle"
0046C0B5  mov     ecx, ebx
0046C0B7  call    sub_482740
0046C0BC  mov     ecx, [esi]
0046C0BE  cmp     dword ptr [ecx+868h], 0
0046C0C5  jz      loc_46C14D
0046C0CB  mov     eax, [ecx+91Ch]
0046C0D1  cmp     eax, 11h
0046C0D4  jz      short loc_46C0EB
0046C0D6  cmp     eax, 21h ; '!'
0046C0D9  jz      short loc_46C0EB
0046C0DB  test    eax, eax
0046C0DD  jnz     short loc_46C14D
0046C0DF  mov     eax, [ecx+950h]
0046C0E5  cmp     dword ptr [eax+4Ch], 5
0046C0E9  jnz     short loc_46C14D
0046C0EB  push    offset aGround; "_ground"
0046C0F0  call    sub_482780
0046C0F5  test    al, al
0046C0F7  jz      short loc_46C14D
0046C0F9  push    offset aGround; "_ground"
0046C0FE  mov     ecx, ebx
0046C100  call    sub_482740
0046C105  mov     eax, [esi+24h]
0046C108  cmp     eax, 3
0046C10B  jz      short loc_46C12C
0046C10D  cmp     eax, 31h ; '1'
0046C110  jz      short loc_46C12C
0046C112  cmp     eax, 32h ; '2'
0046C115  jnz     short loc_46C14D
0046C117  fld     ds:flt_727540
0046C11D  sub     esp, 8
0046C120  fstp    [esp+30h+var_2C]
0046C124  fld     ds:flt_7271C8
0046C12A  jmp     short loc_46C13F
0046C12C  fld     ds:flt_727474
0046C132  sub     esp, 8
0046C135  fstp    [esp+30h+var_2C]; float
0046C139  fld     ds:flt_727238
0046C13F  fstp    [esp+30h+var_30]; float
0046C142  call    sub_526F90
0046C147  fstp    dword ptr [ebx+8]
0046C14A  add     esp, 8
0046C14D  mov     ecx, [ebx+9Ch]
0046C153  mov     byte ptr [ebx+64h], 1
0046C157  push    190h
0046C15C  mov     [esi+94h], ecx
0046C162  call    sub_5EECC0
0046C167  add     eax, 190h
0046C16C  add     esp, 4
0046C16F  mov     [esi+0B0h], eax
0046C175  mov     ebx, esi
0046C177  mov     esi, [esp+28h+var_14]
0046C17B  xor     edi, edi
0046C17D  mov     eax, [ebx+24h]
0046C180  cmp     eax, 8
0046C183  jz      short loc_46C1B2
0046C185  cmp     eax, 18h
0046C188  jz      short loc_46C1B2
0046C18A  cmp     eax, 9
0046C18D  jz      short loc_46C1B2
0046C18F  cmp     eax, 0Ah
0046C192  jz      short loc_46C1B2
0046C194  cmp     eax, 0Ch
0046C197  jz      short loc_46C1B2
0046C199  cmp     eax, 0Fh
0046C19C  jz      short loc_46C1B2
0046C19E  cmp     eax, 0Eh
0046C1A1  jz      short loc_46C1B2
0046C1A3  cmp     eax, 1Fh
0046C1A6  jz      short loc_46C1B2
0046C1A8  cmp     eax, 0Dh
0046C1AB  jz      short loc_46C1B2
0046C1AD  cmp     eax, 2Ah ; '*'
0046C1B0  jnz     short loc_46C1E3
0046C1B2  mov     eax, [ebx+4]
0046C1B5  cmp     eax, edi
0046C1B7  jz      short loc_46C1E3
0046C1B9  mov     eax, [eax+5564h]
0046C1BF  cmp     eax, 1
0046C1C2  jz      short loc_46C1E3
0046C1C4  cmp     eax, 3
0046C1C7  jz      short loc_46C1E3
0046C1C9  cmp     eax, 5
0046C1CC  jz      short loc_46C1E3
0046C1CE  cmp     eax, 6
0046C1D1  jz      short loc_46C1E3
0046C1D3  cmp     eax, 8
0046C1D6  jz      short loc_46C1E3
0046C1D8  push    1; float
0046C1DA  mov     edi, ebx
0046C1DC  call    sub_46CB90
0046C1E1  xor     edi, edi
0046C1E3  mov     eax, [ebx+5Ch]
0046C1E6  cmp     eax, edi
0046C1E8  jle     short loc_46C228
0046C1EA  mov     ecx, [ebx+24h]
0046C1ED  cmp     ecx, 1
0046C1F0  jz      short loc_46C1FC
0046C1F2  cmp     ecx, 29h ; ')'
0046C1F5  jz      short loc_46C1FC
0046C1F7  cmp     ecx, 9
0046C1FA  jnz     short loc_46C219
0046C1FC  cdq
0046C1FD  sub     eax, edx
0046C1FF  sar     eax, 1
0046C201  add     eax, 0FFFFFED5h
0046C206  push    eax
0046C207  call    sub_5EECC0
0046C20C  add     esp, 4
0046C20F  add     eax, 12Ch
0046C214  mov     [ebx+58h], eax
0046C217  jmp     short loc_46C22B
0046C219  inc     eax
0046C21A  push    eax
0046C21B  call    sub_5EECC0
0046C220  add     esp, 4
0046C223  mov     [ebx+58h], eax
0046C226  jmp     short loc_46C22B
0046C228  mov     [ebx+58h], edi
0046C22B  mov     eax, [ebp+arg_8]
0046C22E  cmp     eax, 1Bh
0046C231  jnz     short loc_46C290
0046C233  cmp     byte ptr [ebx+144h], 0
0046C23A  mov     dword ptr [ebx+50h], 32h ; '2'
0046C241  jz      short loc_46C276
0046C243  mov     edx, [ebx]
0046C245  mov     eax, [edx+91Ch]
0046C24B  cmp     eax, 2Bh ; '+'
0046C24E  jz      short loc_46C276
0046C250  cmp     eax, 32h ; '2'
0046C253  jz      short loc_46C276
0046C255  push    offset aAnimBlow; "anim_blow"
0046C25A  mov     ecx, esi
0046C25C  call    sub_482740
0046C261  fld     ds:flt_727358
0046C267  fstp    dword ptr [esi+8]
0046C26A  mov     dword ptr [esi+10h], 3
0046C271  jmp     loc_46CA66
0046C276  push    offset aAnimIdle; "anim_idle"
0046C27B  mov     ecx, esi
0046C27D  call    sub_482740
0046C282  fld     ds:flt_727540
0046C288  fstp    dword ptr [esi+8]
0046C28B  jmp     loc_46CA66
0046C290  cmp     eax, edi
0046C292  jz      loc_46C9BD
0046C298  cmp     eax, 5
0046C29B  jz      loc_46C9BD
0046C2A1  cmp     eax, 7
0046C2A4  jz      loc_46C9BD
0046C2AA  cmp     eax, 34h ; '4'
0046C2AD  jz      loc_46C9BD
0046C2B3  cmp     eax, 28h ; '('
0046C2B6  jz      loc_46C9BD
0046C2BC  cmp     eax, 1Ch
0046C2BF  jnz     loc_46C39E
0046C2C5  fld     ds:flt_727358
0046C2CB  sub     esp, 8
0046C2CE  fstp    [esp+30h+var_2C]; float
0046C2D2  fld     ds:flt_727544
0046C2D8  fstp    [esp+30h+var_30]; float
0046C2DB  call    sub_526F90
0046C2E0  mov     ecx, [esp+30h+var_18]
0046C2E4  fstp    dword ptr [esi+8]
0046C2E7  mov     eax, [ebx+20h]
0046C2EA  fldz
0046C2EC  mov     edi, [ebp+arg_0]
0046C2EF  mov     ebx, [ecx+8]
0046C2F2  add     esp, 8
0046C2F5  add     eax, 2
0046C2F8  push    eax; int
0046C2F9  mov     eax, [edi]
0046C2FB  sub     esp, 8
0046C2FE  fst     [esp+34h+var_30]; float
0046C302  fstp    [esp+34h+var_34]; float
0046C305  call    sub_461000
0046C30A  mov     ebx, eax
0046C30C  mov     dword ptr [ebx+10h], 0
0046C313  fld     dword ptr [esi+8]
0046C316  push    offset aAnimHeadIdle; "anim_head_idle"
0046C31B  fstp    dword ptr [ebx+8]
0046C31E  mov     ecx, ebx
0046C320  call    sub_482740
0046C325  mov     eax, esi
0046C327  mov     edx, offset aAnimIdle; "anim_idle"
0046C32C  mov     esi, ebx
0046C32E  call    sub_482430
0046C333  fldz
0046C335  mov     eax, [edi+20h]
0046C338  mov     edx, [ebx+9Ch]
0046C33E  mov     ecx, [esp+28h+var_18]
0046C342  add     eax, 2
0046C345  push    eax; int
0046C346  mov     eax, [edi]
0046C348  sub     esp, 8
0046C34B  mov     [edi+98h], edx
0046C351  fst     [esp+34h+var_30]; float
0046C355  mov     ebx, [ecx+8]
0046C358  fstp    [esp+34h+var_34]; float
0046C35B  call    sub_461000
0046C360  mov     ebx, [esp+28h+var_14]
0046C364  mov     esi, eax
0046C366  mov     dword ptr [esi+10h], 0
0046C36D  fld     dword ptr [ebx+8]
0046C370  push    offset aAnimSplitpeaId; "anim_splitpea_idle"
0046C375  fstp    dword ptr [esi+8]
0046C378  mov     ecx, esi
0046C37A  call    sub_482740
0046C37F  mov     edx, offset aAnimIdle; "anim_idle"
0046C384  mov     eax, ebx
0046C386  call    sub_482430
0046C38B  mov     edx, [esi+9Ch]
0046C391  mov     [edi+9Ch], edx
0046C397  mov     ebx, edi
0046C399  jmp     loc_46CA66
0046C39E  cmp     eax, 12h
0046C3A1  jnz     loc_46C4D8
0046C3A7  fld     ds:flt_727358
0046C3AD  sub     esp, 8
0046C3B0  fstp    [esp+30h+var_2C]; float
0046C3B4  fld     ds:flt_727544
0046C3BA  fstp    [esp+30h+var_30]; float
0046C3BD  call    sub_526F90
0046C3C2  mov     ecx, [esp+30h+var_18]
0046C3C6  fstp    dword ptr [esi+8]
0046C3C9  mov     eax, [ebx+20h]
0046C3CC  fldz
0046C3CE  mov     edi, [ebp+arg_0]
0046C3D1  mov     ebx, [ecx+8]
0046C3D4  add     esp, 8
0046C3D7  add     eax, 2
0046C3DA  push    eax; int
0046C3DB  mov     eax, [edi]
0046C3DD  sub     esp, 8
0046C3E0  fst     [esp+34h+var_30]; float
0046C3E4  fstp    [esp+34h+var_34]; float
0046C3E7  call    sub_461000
0046C3EC  mov     ebx, eax
0046C3EE  mov     dword ptr [ebx+10h], 0
0046C3F5  fld     dword ptr [esi+8]
0046C3F8  push    offset aAnimHeadIdle1; "anim_head_idle1"
0046C3FD  fstp    dword ptr [ebx+8]
0046C400  mov     ecx, ebx
0046C402  call    sub_482740
0046C407  mov     eax, esi
0046C409  mov     edx, offset aAnimHead1; "anim_head1"
0046C40E  mov     esi, ebx
0046C410  call    sub_482430
0046C415  fldz
0046C417  mov     eax, [edi+20h]
0046C41A  mov     edx, [ebx+9Ch]
0046C420  mov     ecx, [esp+28h+var_18]
0046C424  add     eax, 2
0046C427  push    eax; int
0046C428  mov     eax, [edi]
0046C42A  sub     esp, 8
0046C42D  mov     [edi+98h], edx
0046C433  fst     [esp+34h+var_30]; float
0046C437  mov     ebx, [ecx+8]
0046C43A  fstp    [esp+34h+var_34]; float
0046C43D  call    sub_461000
0046C442  mov     ebx, [esp+28h+var_14]
0046C446  mov     esi, eax
0046C448  mov     dword ptr [esi+10h], 0
0046C44F  fld     dword ptr [ebx+8]
0046C452  push    offset aAnimHeadIdle2; "anim_head_idle2"
0046C457  fstp    dword ptr [esi+8]
0046C45A  mov     ecx, esi
0046C45C  call    sub_482740
0046C461  mov     edx, offset aAnimHead2; "anim_head2"
0046C466  mov     eax, ebx
0046C468  call    sub_482430
0046C46D  fldz
0046C46F  mov     eax, [edi+20h]
0046C472  mov     edx, [esi+9Ch]
0046C478  mov     ecx, [esp+28h+var_18]
0046C47C  add     eax, 2
0046C47F  push    eax; int
0046C480  mov     eax, [edi]
0046C482  sub     esp, 8
0046C485  mov     [edi+9Ch], edx
0046C48B  fst     [esp+34h+var_30]; float
0046C48F  mov     ebx, [ecx+8]
0046C492  fstp    [esp+34h+var_34]; float
0046C495  call    sub_461000
0046C49A  mov     ebx, [esp+28h+var_14]
0046C49E  mov     esi, eax
0046C4A0  mov     dword ptr [esi+10h], 0
0046C4A7  fld     dword ptr [ebx+8]
0046C4AA  push    offset aAnimHeadIdle3; "anim_head_idle3"
0046C4AF  fstp    dword ptr [esi+8]
0046C4B2  mov     ecx, esi
0046C4B4  call    sub_482740
0046C4B9  mov     edx, offset aAnimHead3; "anim_head3"
0046C4BE  mov     eax, ebx
0046C4C0  call    sub_482430
0046C4C5  mov     edx, [esi+9Ch]
0046C4CB  mov     [edi+0A0h], edx
0046C4D1  mov     ebx, edi
0046C4D3  jmp     loc_46CA66
0046C4D8  cmp     eax, 3
0046C4DB  jnz     short loc_46C4E6
0046C4DD  mov     dword ptr [ebx+40h], 0FA0h
0046C4E4  jmp     short loc_46C559
0046C4E6  cmp     eax, 31h ; '1'
0046C4E9  jnz     short loc_46C538
0046C4EB  push    3E8h
0046C4F0  mov     dword ptr [ebx+40h], 0FA0h
0046C4F7  call    sub_5EECC0
0046C4FC  add     esp, 4
0046C4FF  push    40h ; '@'
0046C501  push    40h ; '@'
0046C503  add     eax, 3E8h
0046C508  push    0FFh
0046C50D  lea     ecx, [esp+34h+var_10]
0046C511  mov     [ebx+0B0h], eax
0046C517  call    sub_55BBB0
0046C51C  mov     ecx, [eax]
0046C51E  mov     [esi+48h], ecx
0046C521  mov     edx, [eax+4]
0046C524  mov     [esi+4Ch], edx
0046C527  mov     ecx, [eax+8]
0046C52A  mov     [esi+50h], ecx
0046C52D  mov     edx, [eax+0Ch]
0046C530  mov     [esi+54h], edx
0046C533  jmp     loc_46CA66
0046C538  cmp     eax, 32h ; '2'
0046C53B  jnz     short loc_46C546
0046C53D  mov     dword ptr [ebx+40h], 0FA0h
0046C544  jmp     short loc_46C559
0046C546  cmp     eax, 17h
0046C549  jnz     short loc_46C576
0046C54B  mov     dword ptr [ebx+40h], 1F40h
0046C552  mov     dword ptr [ebx+14h], 50h ; 'P'
0046C559  push    3E8h
0046C55E  call    sub_5EECC0
0046C563  add     esp, 4
0046C566  add     eax, 3E8h
0046C56B  mov     [ebx+0B0h], eax
0046C571  jmp     loc_46CA66
0046C576  cmp     eax, 24h ; '$'
0046C579  jnz     short loc_46C5A9
0046C57B  mov     dword ptr [ebx+40h], 190h
0046C582  mov     ecx, [esi+0Ch]
0046C585  xor     eax, eax
0046C587  cmp     [ecx+4], edi
0046C58A  jle     loc_46CA66
0046C590  mov     edx, [esi+58h]
0046C593  mov     byte ptr [edi+edx+5Dh], 0
0046C598  mov     ecx, [esi+0Ch]
0046C59B  inc     eax
0046C59C  add     edi, 60h ; '`'
0046C59F  cmp     eax, [ecx+4]
0046C5A2  jl      short loc_46C590
0046C5A4  jmp     loc_46CA66
0046C5A9  cmp     eax, 2Dh ; '-'
0046C5AC  jnz     short loc_46C5D9
0046C5AE  mov     edx, [esi+0Ch]
0046C5B1  xor     eax, eax
0046C5B3  cmp     [edx+4], edi
0046C5B6  jle     loc_46CA66
0046C5BC  lea     esp, [esp+0]
0046C5C0  mov     ecx, [esi+58h]
0046C5C3  mov     byte ptr [edi+ecx+5Dh], 0
0046C5C8  mov     edx, [esi+0Ch]
0046C5CB  inc     eax
0046C5CC  add     edi, 60h ; '`'
0046C5CF  cmp     eax, [edx+4]
0046C5D2  jl      short loc_46C5C0
0046C5D4  jmp     loc_46CA66
0046C5D9  cmp     eax, 2
0046C5DC  jz      short loc_46C615
0046C5DE  cmp     eax, 30h ; '0'
0046C5E1  jnz     short loc_46C610
0046C5E3  fld     ds:flt_70830C
0046C5E9  sub     esp, 8
0046C5EC  fstp    [esp+30h+var_2C]; float
0046C5F0  fld     ds:flt_727498
0046C5F6  fstp    [esp+30h+var_30]; float
0046C5F9  call    sub_526F90
0046C5FE  fstp    dword ptr [esi+8]
0046C601  add     esp, 8
0046C604  mov     dword ptr [ebx+54h], 0C8h
0046C60B  jmp     loc_46CA66
0046C610  cmp     eax, 14h
0046C613  jnz     short loc_46C64F
0046C615  mov     eax, ebx
0046C617  call    sub_471D30
0046C61C  test    al, al
0046C61E  jz      loc_46CA66
0046C624  push    offset aAnimExplode; "anim_explode"
0046C629  mov     ecx, esi
0046C62B  mov     dword ptr [ebx+50h], 64h ; 'd'
0046C632  call    sub_482740
0046C637  mov     dword ptr [esi+10h], 3
0046C63E  mov     eax, [ebx]
0046C640  mov     esi, 39h ; '9'
0046C645  call    sub_460A20
0046C64A  jmp     loc_46CA66
0046C64F  cmp     eax, 4
0046C652  jnz     short loc_46C69F
0046C654  fld     ds:flt_727238
0046C65A  mov     eax, ebx
0046C65C  fstp    dword ptr [esi+8]
0046C65F  call    sub_471D30
0046C664  test    al, al
0046C666  jz      short loc_46C687
0046C668  push    0FFFFFFFFh; int
0046C66A  push    offset aAnimGlow; "anim_glow"
0046C66F  mov     ebx, esi
0046C671  call    sub_482EC0
0046C676  mov     eax, [ebp+arg_0]
0046C679  mov     dword ptr [eax+54h], 5DCh
0046C680  mov     ebx, eax
0046C682  jmp     loc_46CA66
0046C687  push    offset aAnimArmed; "anim_armed"
0046C68C  mov     ecx, esi
0046C68E  call    sub_482740
0046C693  mov     dword ptr [ebx+3Ch], 10h
0046C69A  jmp     loc_46CA66
0046C69F  cmp     eax, 0Bh
0046C6A2  jnz     short loc_46C6DE
0046C6A4  mov     eax, ebx
0046C6A6  call    sub_471D30
0046C6AB  test    al, al
0046C6AD  jz      loc_46CA66
0046C6B3  push    offset aAnimLand; "anim_land"
0046C6B8  mov     ecx, esi
0046C6BA  call    sub_482740
0046C6BF  mov     dword ptr [esi+10h], 3
0046C6C6  mov     eax, [ebx]
0046C6C8  mov     esi, 36h ; '6'
0046C6CD  mov     dword ptr [ebx+3Ch], 8
0046C6D4  call    sub_460A20
0046C6D9  jmp     loc_46CA66
0046C6DE  cmp     eax, 9
0046C6E1  jnz     short loc_46C757
0046C6E3  mov     eax, ebx
0046C6E5  mov     dword ptr [esi+20h], 6
0046C6EC  call    sub_471D30
0046C6F1  test    al, al
0046C6F3  jz      short loc_46C728
0046C6F5  push    0Ah
0046C6F7  call    sub_5EECC0
0046C6FC  add     esp, 4
0046C6FF  sub     eax, 5
0046C702  add     [ebx+8], eax
0046C705  push    0Ah
0046C707  call    sub_5EECC0
0046C70C  sub     eax, 5
0046C70F  add     esp, 4
0046C712  add     [ebx+0Ch], eax
0046C715  mov     dword ptr [ebx+3Ch], 17h
0046C71C  mov     dword ptr [ebx+54h], 2EE0h
0046C723  jmp     loc_46CA66
0046C728  cmp     byte ptr [ebx+143h], 0
0046C72F  mov     ecx, esi
0046C731  jz      short loc_46C73A
0046C733  push    offset aAnimBigsleep; "anim_bigsleep"
0046C738  jmp     short loc_46C73F
0046C73A  push    offset aAnimBigidle; "anim_bigidle"
0046C73F  call    sub_482740
0046C744  mov     dword ptr [ebx+3Ch], 17h
0046C74B  mov     dword ptr [ebx+54h], 2EE0h
0046C752  jmp     loc_46CA66
0046C757  cmp     eax, 8
0046C75A  jz      loc_46C989
0046C760  cmp     eax, 18h
0046C763  jz      loc_46C989
0046C769  cmp     eax, 1Eh
0046C76C  jnz     short loc_46C78F
0046C76E  push    1; int
0046C770  mov     dword ptr [ebx+40h], 0FA0h
0046C777  mov     dword ptr [ebx+10h], 78h ; 'x'
0046C77E  push    offset aPumpkinBack; "Pumpkin_back"
0046C783  mov     ebx, esi
0046C785  call    sub_482EC0
0046C78A  jmp     loc_46CA63
0046C78F  cmp     eax, 6
0046C792  jnz     short loc_46C7A0
0046C794  mov     dword ptr [ebx+3Ch], 1
0046C79B  jmp     loc_46CA66
0046C7A0  cmp     eax, 19h
0046C7A3  jnz     short loc_46C802
0046C7A5  cmp     byte ptr [ebx+144h], 0
0046C7AC  mov     dword ptr [ebx+54h], 32h ; '2'
0046C7B3  jz      short loc_46C7C0
0046C7B5  mov     ecx, [ebx]
0046C7B7  cmp     dword ptr [ecx+91Ch], 2Bh ; '+'
0046C7BE  jz      short loc_46C7DC
0046C7C0  mov     edx, [ebx+0Ch]
0046C7C3  mov     eax, [ebx+8]
0046C7C6  push    2Eh ; '.'
0046C7C8  push    7A121h
0046C7CD  add     edx, 28h ; '('
0046C7D0  push    edx
0046C7D1  add     eax, 28h ; '('
0046C7D4  push    eax
0046C7D5  mov     edi, ebx
0046C7D7  call    sub_46D1D0
0046C7DC  mov     eax, ebx
0046C7DE  call    sub_471D30
0046C7E3  test    al, al
0046C7E5  jz      loc_46CA66
0046C7EB  mov     ecx, [ebx]
0046C7ED  mov     edx, [ecx]
0046C7EF  mov     eax, dword_7530D8
0046C7F4  mov     edx, [edx+0D8h]
0046C7FA  push    eax
0046C7FB  call    edx
0046C7FD  jmp     loc_46CA66
0046C802  cmp     eax, 16h
0046C805  jz      loc_46CA66
0046C80B  cmp     eax, 26h ; '&'
0046C80E  jnz     short loc_46C836
0046C810  fld     ds:flt_727358
0046C816  sub     esp, 8
0046C819  fstp    [esp+30h+var_2C]; float
0046C81D  fld     ds:flt_727544
0046C823  fstp    [esp+30h+var_30]; float
0046C826  call    sub_526F90
0046C82B  fstp    dword ptr [esi+8]
0046C82E  add     esp, 8
0046C831  jmp     loc_46CA66
0046C836  cmp     eax, 1Ah
0046C839  jnz     short loc_46C847
0046C83B  mov     dword ptr [ebx+3Ch], 1Eh
0046C842  jmp     loc_46CA66
0046C847  cmp     eax, 23h ; '#'
0046C84A  jnz     short loc_46C858
0046C84C  mov     dword ptr [ebx+50h], 64h ; 'd'
0046C853  jmp     loc_46CA66
0046C858  cmp     eax, 0Dh
0046C85B  jnz     short loc_46C869
0046C85D  mov     dword ptr [ebx+3Ch], 1
0046C864  jmp     loc_46CA66
0046C869  cmp     eax, 2Fh ; '/'
0046C86C  jnz     short loc_46C89C
0046C86E  mov     eax, ebx
0046C870  call    sub_471D30
0046C875  test    al, al
0046C877  jz      loc_46CA66
0046C87D  push    offset aAnimUnarmedIdl; "anim_unarmed_idle"
0046C882  mov     ecx, esi
0046C884  mov     dword ptr [ebx+3Ch], 23h ; '#'
0046C88B  mov     dword ptr [ebx+54h], 1F4h
0046C892  call    sub_482740
0046C897  jmp     loc_46CA66
0046C89C  cmp     eax, 22h ; '"'
0046C89F  jnz     short loc_46C8B3
0046C8A1  push    0FFFFFFFFh; int
0046C8A3  push    offset aCornpultButter; "Cornpult_butter"
0046C8A8  push    esi; int
0046C8A9  call    sub_482F10
0046C8AE  jmp     loc_46CA66
0046C8B3  cmp     eax, 1Fh
0046C8B6  jnz     short loc_46C8E9
0046C8B8  mov     ecx, [esi+0Ch]
0046C8BB  xor     eax, eax
0046C8BD  cmp     [ecx+4], edi
0046C8C0  jle     loc_46CA66
0046C8C6  jmp     short loc_46C8D0
0046C8D0  mov     edx, [esi+58h]
0046C8D3  mov     byte ptr [edi+edx+5Dh], 0
0046C8D8  mov     ecx, [esi+0Ch]
0046C8DB  inc     eax
0046C8DC  add     edi, 60h ; '`'
0046C8DF  cmp     eax, [ecx+4]
0046C8E2  jl      short loc_46C8D0
0046C8E4  jmp     loc_46CA66
0046C8E9  cmp     eax, 2Eh ; '.'
0046C8EC  jnz     short loc_46C8FA
0046C8EE  mov     dword ptr [ebx+40h], 1C2h
0046C8F5  jmp     loc_46CA66
0046C8FA  cmp     eax, 33h ; '3'
0046C8FD  jz      loc_46CA66
0046C903  cmp     eax, 21h ; '!'
0046C906  jnz     short loc_46C92A
0046C908  mov     eax, ebx
0046C90A  call    sub_471D30
0046C90F  test    al, al
0046C911  jz      loc_46CA66
0046C917  mov     dword ptr [ebx+3Ch], 2Fh ; '/'
0046C91E  mov     dword ptr [ebx+54h], 64h ; 'd'
0046C925  jmp     loc_46CA66
0046C92A  cmp     eax, 10h
0046C92D  jnz     short loc_46C951
0046C92F  mov     eax, ebx
0046C931  call    sub_471D30
0046C936  test    al, al
0046C938  jz      loc_46CA66
0046C93E  mov     dword ptr [ebx+3Ch], 30h ; '0'
0046C945  mov     dword ptr [ebx+54h], 64h ; 'd'
0046C94C  jmp     loc_46CA66
0046C951  cmp     eax, 13h
0046C954  jnz     loc_46CA66
0046C95A  mov     edx, [esi+0Ch]
0046C95D  xor     eax, eax
0046C95F  cmp     [edx+4], edi
0046C962  jle     loc_46CA66
0046C968  jmp     short loc_46C970
0046C970  mov     ecx, [esi+58h]
0046C973  mov     byte ptr [edi+ecx+5Dh], 0
0046C978  mov     edx, [esi+0Ch]
0046C97B  inc     eax
0046C97C  add     edi, 60h ; '`'
0046C97F  cmp     eax, [edx+4]
0046C982  jl      short loc_46C970
0046C984  jmp     loc_46CA66
0046C989  mov     eax, ebx
0046C98B  call    sub_471D30
0046C990  test    al, al
0046C992  jz      loc_46CA66
0046C998  push    0Ah
0046C99A  call    sub_5EECC0
0046C99F  add     esp, 4
0046C9A2  sub     eax, 5
0046C9A5  add     [ebx+8], eax
0046C9A8  push    6
0046C9AA  call    sub_5EECC0
0046C9AF  sub     eax, 3
0046C9B2  add     esp, 4
0046C9B5  add     [ebx+0Ch], eax
0046C9B8  jmp     loc_46CA66
0046C9BD  cmp     esi, edi
0046C9BF  jz      loc_46CA66
0046C9C5  fld     ds:flt_72753C
0046C9CB  push    ecx
0046C9CC  fstp    [esp+2Ch+var_2C]; float
0046C9CF  call    sub_5EECE0
0046C9D4  fadd    ds:dbl_727210
0046C9DA  mov     ecx, [esp+2Ch+var_18]
0046C9DE  mov     edx, [ebp+arg_0]
0046C9E1  add     esp, 4
0046C9E4  fstp    dword ptr [esi+8]
0046C9E7  mov     eax, [ebx+20h]
0046C9EA  fldz
0046C9EC  mov     ebx, [ecx+8]
0046C9EF  add     eax, 2
0046C9F2  push    eax; int
0046C9F3  mov     eax, [edx]
0046C9F5  sub     esp, 8
0046C9F8  fst     [esp+34h+var_30]; float
0046C9FC  fstp    [esp+34h+var_34]; float
0046C9FF  call    sub_461000
0046CA04  mov     edi, eax
0046CA06  mov     dword ptr [edi+10h], 0
0046CA0D  fld     dword ptr [esi+8]
0046CA10  push    offset aAnimHeadIdle; "anim_head_idle"
0046CA15  fstp    dword ptr [edi+8]
0046CA18  mov     ecx, edi
0046CA1A  call    sub_482740
0046CA1F  mov     eax, [edi+9Ch]
0046CA25  mov     ecx, [ebp+arg_0]
0046CA28  push    offset aAnimStem; "anim_stem"
0046CA2D  mov     ebx, esi
0046CA2F  mov     [ecx+98h], eax
0046CA35  call    sub_482780
0046CA3A  test    al, al
0046CA3C  jz      short loc_46CA45
0046CA3E  mov     edx, offset aAnimStem; "anim_stem"
0046CA43  jmp     short loc_46CA5A
0046CA45  push    offset aAnimIdle; "anim_idle"
0046CA4A  mov     ebx, esi
0046CA4C  call    sub_482780
0046CA51  test    al, al
0046CA53  jz      short loc_46CA63
0046CA55  mov     edx, offset aAnimIdle; "anim_idle"
0046CA5A  mov     eax, esi
0046CA5C  mov     esi, edi
0046CA5E  call    sub_482430
0046CA63  mov     ebx, [ebp+arg_0]
0046CA66  mov     edx, [ebx]
0046CA68  cmp     dword ptr [edx+91Ch], 27h ; '''
0046CA6F  jnz     short loc_46CA8B
0046CA71  mov     eax, [ebx+24h]
0046CA74  cmp     eax, 3
0046CA77  jz      short loc_46CA83
0046CA79  cmp     eax, 1
0046CA7C  jz      short loc_46CA83
0046CA7E  cmp     eax, 26h ; '&'
0046CA81  jnz     short loc_46CA8B
0046CA83  mov     eax, [ebx+40h]
0046CA86  add     eax, eax
0046CA88  mov     [ebx+40h], eax
0046CA8B  cmp     dword ptr [ebx+24h], 21h ; '!'
0046CA8F  mov     ecx, [ebx+40h]
0046CA92  mov     [ebx+44h], ecx
0046CA95  jz      short loc_46CADA
0046CA97  cmp     byte ptr [ebx+144h], 0
0046CA9E  jz      short loc_46CADA
0046CAA0  mov     edx, [ebx+28h]
0046CAA3  mov     esi, [ebp+arg_0]
0046CAA6  mov     ebx, [ebx+1Ch]
0046CAA9  push    edx
0046CAAA  mov     edx, [esi+4]
0046CAAD  call    sub_418F80
0046CAB2  test    eax, eax
0046CAB4  jz      short loc_46CADA
0046CAB6  mov     ecx, [esi]
0046CAB8  fldz
0046CABA  mov     eax, [eax+94h]
0046CAC0  mov     edx, [ecx+944h]
0046CAC6  mov     ecx, [edx+8]
0046CAC9  and     eax, 0FFFFh
0046CACE  lea     edx, [eax+eax*4]
0046CAD1  mov     eax, [ecx]
0046CAD3  shl     edx, 5
0046CAD6  fstp    dword ptr [eax+edx+8]
0046CADA  pop     edi
0046CADB  pop     esi
0046CADC  pop     ebx
0046CADD  mov     esp, ebp
0046CADF  pop     ebp
0046CAE0  retn    10h
