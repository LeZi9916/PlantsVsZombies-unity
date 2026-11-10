00455420 push    0FFFFFFFFh
00455422 mov     eax, large fs:0
00455428 push    offset SEH_43B380
0045542D push    eax
0045542E mov     large fs:0, esp
00455435 sub     esp, 54h
00455438 push    ebx
00455439 push    ebp
0045543A push    esi
0045543B xor     ebx, ebx
0045543D push    edi
0045543E mov     esi, ecx
00455440 cmp     [esp+70h+arg_0], bl
00455444 jz      loc_45558C
0045544A push    ebx; int
0045544B xor     eax, eax
0045544D mov     edi, 7
00455452 push    offset unk_708460; Source
00455457 lea     ecx, [esp+78h+var_28]
0045545B mov     byte ptr [esi+150h], 1
00455462 mov     [esp+78h+var_10], edi
00455466 mov     [esp+78h+var_14], ebx
0045546A mov     [esp+78h+var_24], ax
0045546F call    sub_40D1E0
00455474 mov     [esp+70h+var_4], ebx
00455478 xor     ecx, ecx
0045547A push    ebx; int
0045547B mov     [esp+74h+var_40], cx
00455480 push    offset unk_708460; Source
00455485 lea     ecx, [esp+78h+var_44]
00455489 mov     [esp+78h+var_2C], edi
0045548D mov     [esp+78h+var_30], ebx
00455491 call    sub_40D1E0
00455496 mov     byte ptr [esp+70h+var_4], 1
0045549B push    9; int
0045549D xor     edx, edx
0045549F push    offset off_70D88C; Source
004554A4 lea     ecx, [esp+78h+var_60]
004554A8 mov     [esp+78h+var_48], edi
004554AC mov     [esp+78h+var_4C], ebx
004554B0 mov     [esp+78h+var_5C], dx
004554B5 call    sub_40D1E0
004554BA push    ebx
004554BB lea     edx, [esp+74h+var_28]
004554BF push    edx
004554C0 lea     edx, [esp+78h+var_44]
004554C4 push    edx
004554C5 mov     byte ptr [esp+7Ch+var_4], 2
004554CA mov     ecx, [esi+0A4h]
004554D0 mov     eax, [ecx]
004554D2 mov     eax, [eax+120h]
004554D8 lea     edx, [esp+7Ch+var_60]
004554DC push    edx
004554DD push    1
004554DF push    31h ; '1'
004554E1 call    eax
004554E3 mov     ebp, 8
004554E8 cmp     [esp+6Ch+var_44], ebp
004554EC jb      short loc_4554FB
004554EE mov     ecx, [esp+6Ch+var_58]
004554F2 push    ecx; void *
004554F3 call    ??3@YAXPAX@Z; operator delete(void *)
004554F8 add     esp, 4
004554FB xor     edx, edx
004554FD mov     [esp+6Ch+var_44], edi
00455501 mov     [esp+6Ch+var_48], ebx
00455505 mov     word ptr [esp+6Ch+var_58], dx
0045550A cmp     [esp+6Ch+var_28], ebp
0045550E jb      short loc_45551D
00455510 mov     eax, [esp+6Ch+var_3C]
00455514 push    eax; void *
00455515 call    ??3@YAXPAX@Z; operator delete(void *)
0045551A add     esp, 4
0045551D xor     ecx, ecx
0045551F mov     dword ptr [esp+6Ch], 0FFFFFFFFh
00455527 mov     [esp+6Ch+var_28], edi
0045552B mov     [esp+6Ch+var_2C], ebx
0045552F mov     word ptr [esp+6Ch+var_3C], cx
00455534 cmp     [esp+6Ch+var_C], ebp
00455538 jb      short loc_455547
0045553A mov     edx, [esp+6Ch+var_20]
0045553E push    edx; void *
0045553F call    ??3@YAXPAX@Z; operator delete(void *)
00455544 add     esp, 4
00455547 mov     ecx, [esi+0A4h]
0045554D mov     edx, [ecx]
0045554F xor     eax, eax
00455551 mov     word ptr [esp+6Ch+var_20], ax
00455556 mov     eax, [edx+16Ch]
0045555C mov     [esp+6Ch+var_C], edi
00455560 mov     [esp+6Ch+var_10], ebx
00455564 call    eax
00455566 mov     ebx, [esi+0A4h]
0045556C call    sub_462C30
00455571 mov     ecx, [esi+0A4h]
00455577 mov     edx, [ecx]
00455579 mov     eax, [edx+134h]
0045557F push    31h ; '1'
00455581 call    eax
00455583 mov     byte ptr [esi+150h], 0
0045558A xor     ebx, ebx
0045558C mov     edx, [esi+0A4h]
00455592 mov     ecx, [esi+0F8h]
00455598 mov     eax, [edx+944h]
0045559E mov     edx, [eax]
004555A0 cmp     ecx, ebx
004555A2 jz      short loc_4555C2
004555A4 movzx   eax, cx
004555A7 cmp     eax, [edx+8]
004555AA jnb     short loc_4555C2
004555AC imul    eax, 2Ch ; ','
004555AF add     eax, [edx]
004555B1 cmp     [eax+28h], ecx
004555B4 jnz     short loc_4555C2
004555B6 push    eax
004555B7 call    sub_52B620
004555BC mov     [esi+0F8h], ebx
004555C2 mov     edi, [esi+0A4h]
004555C8 mov     ebp, 1
004555CD mov     [esi+14Ch], ebp
004555D3 mov     eax, [edi+950h]
004555D9 cmp     eax, ebx
004555DB jz      short loc_4555E6
004555DD mov     ecx, [eax+4Ch]
004555E0 mov     [esi+14Ch], ecx
004555E6 mov     byte ptr [esi+0F3h], 1
004555ED mov     byte ptr [esi+0F0h], 1
004555F4 mov     byte ptr [esi+0F1h], 1
004555FB mov     byte ptr [esi+0F2h], 1
00455602 mov     [esi+0F4h], bl
00455608 cmp     [edi+950h], ebx
0045560E jz      loc_4556D2
00455614 cmp     dword ptr [esi+14Ch], 2
0045561B jl      short loc_455623
0045561D mov     [esi+0F3h], bl
00455623 mov     eax, [edi+950h]
00455629 cmp     eax, ebx
0045562B jz      short loc_45564A
0045562D cmp     [eax+54h], ebx
00455630 jle     short loc_45564A
00455632 mov     [esi+0F0h], bl
00455638 mov     [esi+0F2h], bl
0045563E mov     [esi+0F1h], bl
00455644 mov     [esi+0F3h], bl
0045564A mov     edx, [edi+950h]
00455650 cmp     [edx+34Ch], ebx
00455656 jle     short loc_45565E
00455658 mov     [esi+0F0h], bl
0045565E mov     eax, [edi+950h]
00455664 cmp     [eax+350h], ebx
0045566A jle     short loc_455672
0045566C mov     [esi+0F1h], bl
00455672 mov     ecx, [edi+950h]
00455678 cmp     [ecx+364h], ebx
0045567E jle     short loc_455686
00455680 mov     [esi+0F2h], bl
00455686 cmp     [edi+9E8h], bl
0045568C jnz     short loc_4556A9
0045568E mov     ecx, [edi+99Ch]
00455694 cmp     ecx, ebx
00455696 jz      short loc_4556A1
00455698 call    sub_651F30
0045569D cmp     al, bl
0045569F jnz     short loc_4556B7
004556A1 cmp     [edi+9E4h], ebp
004556A7 jnz     short loc_4556B7
004556A9 mov     byte ptr [esi+0F1h], 1
004556B0 mov     byte ptr [esi+0F2h], 1
004556B7 mov     edi, [esi+0A4h]
004556BD mov     edx, [edi+950h]
004556C3 cmp     [edx+340h], bl
004556C9 jz      short loc_4556D2
004556CB mov     byte ptr [esi+0F4h], 1
004556D2 mov     eax, [edi+950h]
004556D8 cmp     eax, ebx
004556DA jz      short loc_45570D
004556DC cmp     [eax+54h], ebx
004556DF jle     short loc_45570D
004556E1 cmp     [edi+9E8h], bl
004556E7 jnz     short loc_45570D
004556E9 mov     ecx, [edi+99Ch]
004556EF cmp     ecx, ebx
004556F1 jz      short loc_4556FC
004556F3 call    sub_651F30
004556F8 cmp     al, bl
004556FA jnz     short loc_455704
004556FC cmp     [edi+9E4h], ebp
00455702 jz      short loc_45570D
00455704 mov     byte ptr [esi+158h], 1
0045570B jmp     short loc_455713
0045570D mov     [esi+158h], bl
00455713 cmp     [esi+158h], bl
00455719 jz      short loc_45572A
0045571B cmp     [esi+148h], ebx
00455721 jz      short loc_45572A
00455723 mov     edi, esi
00455725 call    sub_4553E0
0045572A call    sub_454F30
0045572F xor     eax, eax
00455731 push    esi
00455732 mov     dword_752A58, eax
00455737 mov     dword_752A5C, eax
0045573C mov     dword_752A60, eax
00455741 mov     dword_752A64, eax
00455746 mov     dword_752A68, eax
0045574B mov     dword_752A6C, eax
00455750 mov     dword_752A70, eax
00455755 mov     dword_752A74, eax
0045575A mov     word_752A78, ax
00455760 mov     byte_755DF4, bl
00455766 call    sub_455780
0045576B mov     ecx, [esp+70h+var_C]
0045576F pop     edi
00455770 pop     esi
00455771 pop     ebp
00455772 mov     large fs:0, ecx
00455779 pop     ebx
0045577A add     esp, 60h
0045577D retn    4
006DFD40 lea     ecx, [ebp-28h]; void *
006DFD43 jmp     sub_401330
006DFD48 lea     ecx, [ebp-44h]; void *
006DFD4B jmp     sub_401330
006DFD50 lea     ecx, [ebp-60h]; void *
006DFD53 jmp     sub_401330
006DFD58 mov     eax, offset stru_7322E8
006DFD5D jmp     ___CxxFrameHandler3
