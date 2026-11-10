00457580 sub     esp, 1Ch
00457583 push    ebx
00457584 push    ebp
00457585 mov     ebp, [esp+24h+arg_0]
00457589 push    esi
0045758A mov     esi, [ebp+0A4h]
00457590 xor     ebx, ebx
00457592 cmp     [esi+9E8h], bl
00457598 jnz     short loc_4575BE
0045759A mov     ecx, [esi+99Ch]
004575A0 cmp     ecx, ebx
004575A2 jz      short loc_4575B1
004575A4 call    sub_651F30
004575A9 cmp     al, bl
004575AB jnz     loc_457675
004575B1 cmp     dword ptr [esi+9E4h], 1
004575B8 jnz     loc_457675
004575BE cmp     dword ptr [ebp+14Ch], 19h
004575C5 jge     short loc_4575E4
004575C7 mov     eax, [ebp+0A4h]
004575CD mov     eax, [eax+950h]
004575D3 cmp     eax, ebx
004575D5 jz      loc_457675
004575DB cmp     [eax+54h], ebx
004575DE jle     loc_457675
004575E4 mov     eax, [ebp+0A4h]
004575EA push    1; int
004575EC push    offset aDialogButtonNo; "[DIALOG_BUTTON_NO]"
004575F1 push    offset aDialogButtonYe; "[DIALOG_BUTTON_YES]"
004575F6 push    offset aReplayLevelHea; "[REPLAY_LEVEL_HEADER]"
004575FB push    31h ; '1'; int
004575FD push    eax; int
004575FE mov     ecx, offset aReplayLevelBod; "[REPLAY_LEVEL_BODY]"
00457603 call    sub_45D250
00457608 cmp     eax, 3E9h
0045760D jz      loc_4578A4
00457613 mov     ecx, [ebp+0A4h]
00457619 mov     edx, [ecx+950h]
0045761F mov     dword ptr [edx+4Ch], 18h
00457626 mov     eax, [ebp+0A4h]
0045762C mov     ecx, [eax+950h]
00457632 mov     [ecx+54h], ebx
00457635 mov     edx, [ebp+0A4h]
0045763B mov     eax, [edx+950h]
00457641 mov     ecx, [eax+20h]
00457644 push    ebx
00457645 lea     edx, [esp+2Ch+var_1C]
00457649 call    sub_464510
0045764E add     esp, 4
00457651 lea     ecx, [esp+28h+var_1C]
00457655 push    ecx
00457656 mov     ecx, [ebp+0A4h]
0045765C call    sub_620130
00457661 cmp     [esp+28h+var_4], 10h
00457666 jb      short loc_457675
00457668 mov     edx, [esp+28h+var_18]
0045766C push    edx; void *
0045766D call    ??3@YAXPAX@Z; operator delete(void *)
00457672 add     esp, 4
00457675 mov     eax, [ebp+0A4h]
0045767B mov     esi, [eax+960h]
00457681 push    edi
00457682 call    sub_468F30
00457687 mov     ecx, [ebp+0A4h]
0045768D mov     edx, [ecx]
0045768F mov     eax, dword_752D1C
00457694 mov     edx, [edx+0D8h]
0045769A push    eax
0045769B call    edx
0045769D mov     ecx, [ebp+0ACh]
004576A3 mov     byte ptr [ebp+0E8h], 1
004576AA mov     eax, [ecx]
004576AC mov     edx, [eax+94h]
004576B2 push    1
004576B4 call    edx
004576B6 mov     ecx, [ebp+0B0h]
004576BC mov     eax, [ecx]
004576BE mov     edx, [eax+94h]
004576C4 push    1
004576C6 call    edx
004576C8 mov     ecx, [ebp+0B4h]
004576CE mov     eax, [ecx]
004576D0 mov     edx, [eax+94h]
004576D6 push    1
004576D8 call    edx
004576DA mov     ecx, [ebp+0B8h]
004576E0 mov     eax, [ecx]
004576E2 mov     edx, [eax+94h]
004576E8 push    1
004576EA call    edx
004576EC mov     ecx, [ebp+0BCh]
004576F2 mov     eax, [ecx]
004576F4 mov     edx, [eax+94h]
004576FA push    1
004576FC call    edx
004576FE mov     ecx, [ebp+0C0h]
00457704 mov     eax, [ecx]
00457706 mov     edx, [eax+94h]
0045770C push    1
0045770E call    edx
00457710 mov     ecx, [ebp+0E0h]
00457716 mov     eax, [ecx]
00457718 mov     edx, [eax+94h]
0045771E push    1
00457720 call    edx
00457722 mov     ecx, [ebp+0D0h]
00457728 mov     eax, [ecx]
0045772A mov     edx, [eax+94h]
00457730 push    1
00457732 call    edx
00457734 mov     ecx, [ebp+0D4h]
0045773A mov     eax, [ecx]
0045773C mov     edx, [eax+94h]
00457742 push    1
00457744 call    edx
00457746 mov     ecx, [ebp+0DCh]
0045774C mov     eax, [ecx]
0045774E mov     edx, [eax+94h]
00457754 push    1
00457756 call    edx
00457758 mov     ecx, [ebp+0D8h]
0045775E mov     eax, [ecx]
00457760 mov     edx, [eax+94h]
00457766 push    1
00457768 call    edx
0045776A mov     ecx, [ebp+0C4h]
00457770 mov     eax, [ecx]
00457772 mov     edx, [eax+94h]
00457778 push    1
0045777A call    edx
0045777C mov     ecx, [ebp+0C8h]
00457782 mov     eax, [ecx]
00457784 mov     edx, [eax+94h]
0045778A push    1
0045778C call    edx
0045778E mov     ecx, [ebp+0A8h]
00457794 mov     eax, [ecx]
00457796 mov     edx, [eax+94h]
0045779C push    1
0045779E call    edx
004577A0 mov     eax, [ebp+0A4h]
004577A6 mov     ecx, [eax+944h]
004577AC mov     edi, [ecx+8]
004577AF call    sub_483BC0
004577B4 mov     esi, eax
004577B6 push    ebx
004577B7 push    6Eh ; 'n'
004577B9 mov     [esi+60h], edi
004577BC mov     [esi+68h], ebx
004577BF call    sub_482AA0
004577C4 fld     ds:flt_727540
004577CA mov     eax, dword_755E3C
004577CF fstp    [esp+34h+var_30]; float
004577D3 fld     ds:flt_727960
004577D9 add     eax, 6E0h
004577DE fstp    [esp+34h+var_34]; float
004577E1 push    esi; int
004577E2 mov     dword ptr [esi], 6Eh ; 'n'
004577E8 call    sub_480E40
004577ED mov     edx, [esi+9Ch]
004577F3 mov     dword ptr [esi+10h], 3
004577FA mov     [ebp+140h], edx
00457800 mov     ebp, [ebp+0A4h]
00457806 cmp     [ebp+9E9h], bl
0045780C jnz     short loc_457857
0045780E fldz
00457810 mov     ecx, dword_755E50
00457816 mov     ebp, [ebp+888h]
0045781C fst     [esp+2Ch+arg_0]
00457820 fcomp   dword ptr [ecx+1044h]
00457826 fnstsw  ax
00457828 test    ah, 44h
0045782B jnp     short loc_457843
0045782D fld     dword ptr [ecx+1044h]
00457833 push    ecx
00457834 fstp    [esp+30h+var_30]; float
00457837 call    sub_5EECE0
0045783C fstp    [esp+30h+arg_0]
00457840 add     esp, 4
00457843 fld     [esp+2Ch+arg_0]
00457847 push    ecx
00457848 mov     eax, 50h ; 'P'
0045784D fstp    [esp+30h+var_30]; float
00457850 mov     ecx, ebp
00457852 call    sub_52A5A0
00457857 mov     eax, [esi+0Ch]
0045785A cmp     [eax+4], ebx
0045785D mov     [esp+2Ch+arg_0], ebx
00457861 jle     short loc_4578A3
00457863 xor     ebp, ebp
00457865 mov     ecx, [esi+0Ch]
00457868 mov     eax, [ecx]
0045786A mov     edx, [eax+ebp]
0045786D mov     edi, [esi+58h]
00457870 add     eax, ebp
00457872 push    4; MaxCount
00457874 push    offset aRock; "rock"
00457879 push    edx; String1
0045787A add     edi, ebx
0045787C call    __strnicmp
00457881 add     esp, 0Ch
00457884 test    eax, eax
00457886 jnz     short loc_45788C
00457888 mov     byte ptr [edi+5Ch], 1
0045788C mov     eax, [esp+2Ch+arg_0]
00457890 mov     ecx, [esi+0Ch]
00457893 inc     eax
00457894 add     ebp, 0Ch
00457897 add     ebx, 60h ; '`'
0045789A cmp     eax, [ecx+4]
0045789D mov     [esp+2Ch+arg_0], eax
004578A1 jl      short loc_457865
004578A3 pop     edi
004578A4 pop     esi
004578A5 pop     ebp
004578A6 pop     ebx
004578A7 add     esp, 1Ch
004578AA retn    4
