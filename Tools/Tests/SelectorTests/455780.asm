00455780 sub     esp, 8
00455783 push    ebx
00455784 push    ebp
00455785 mov     ebp, [esp+10h+arg_0]
00455789 mov     eax, [ebp+0A4h]
0045578F cmp     dword ptr [eax+950h], 0
00455796 push    esi
00455797 push    edi
00455798 jz      loc_455937
0045579E mov     eax, [eax+950h]
004557A4 test    eax, eax
004557A6 jz      short loc_4557E3
004557A8 cmp     dword ptr [eax+54h], 0
004557AC jle     short loc_4557E3
004557AE sub     esp, 1Ch
004557B1 xor     eax, eax
004557B3 mov     esi, esp
004557B5 mov     byte ptr [esp+34h+arg_0], 1
004557BA mov     [esp+34h+var_8], 0
004557C2 lea     edi, [esp+34h+arg_0]
004557C6 lea     ebx, [esp+34h+var_8]
004557CA mov     [esp+34h+var_4], esp
004557CE call    sub_463B60
004557D3 mov     eax, [ebp+0A4h]
004557D9 push    eax; int
004557DA mov     ecx, edi
004557DC mov     edx, ebx
004557DE call    sub_463910
004557E3 mov     eax, [ebp+0A4h]
004557E9 call    sub_463170
004557EE test    al, al
004557F0 jz      short loc_455826
004557F2 sub     esp, 1Ch
004557F5 mov     eax, 1
004557FA mov     esi, esp
004557FC mov     byte ptr [esp+34h+arg_0], 1
00455801 mov     [esp+34h+var_8], eax
00455805 lea     edi, [esp+34h+arg_0]
00455809 lea     ebx, [esp+34h+var_8]
0045580D mov     [esp+34h+var_4], esp
00455811 call    sub_463B60
00455816 mov     ecx, [ebp+0A4h]
0045581C push    ecx; int
0045581D mov     ecx, edi
0045581F mov     edx, ebx
00455821 call    sub_463910
00455826 mov     eax, [ebp+0A4h]
0045582C mov     eax, [eax+950h]
00455832 test    eax, eax
00455834 jz      short loc_455881
00455836 mov     ecx, [eax+54h]
00455839 test    ecx, ecx
0045583B jle     short loc_455881
0045583D cmp     ecx, 2
00455840 jge     short loc_45584D
00455842 mov     edx, [eax+4Ch]
00455845 cmp     edx, dword_747D90
0045584B jl      short loc_455881
0045584D sub     esp, 1Ch
00455850 mov     eax, 0Ah
00455855 mov     esi, esp
00455857 mov     byte ptr [esp+34h+arg_0], 1
0045585C mov     [esp+34h+var_8], eax
00455860 lea     edi, [esp+34h+arg_0]
00455864 lea     ebx, [esp+34h+var_8]
00455868 mov     [esp+34h+var_4], esp
0045586C call    sub_463B60
00455871 mov     eax, [ebp+0A4h]
00455877 push    eax; int
00455878 mov     ecx, edi
0045587A mov     edx, ebx
0045587C call    sub_463910
00455881 mov     ecx, [ebp+0A4h]
00455887 mov     edx, [ecx+950h]
0045588D cmp     dword ptr [edx+11Ch], 64h ; 'd'
00455894 jl      short loc_4558CA
00455896 sub     esp, 1Ch
00455899 mov     eax, 12h
0045589E mov     esi, esp
004558A0 mov     byte ptr [esp+34h+arg_0], 1
004558A5 mov     [esp+34h+var_8], eax
004558A9 lea     edi, [esp+34h+arg_0]
004558AD lea     ebx, [esp+34h+var_8]
004558B1 mov     [esp+34h+var_4], esp
004558B5 call    sub_463B60
004558BA mov     eax, [ebp+0A4h]
004558C0 push    eax; int
004558C1 mov     ecx, edi
004558C3 mov     edx, ebx
004558C5 call    sub_463910
004558CA mov     bl, 1
004558CC mov     edi, 28h ; '('
004558D1 mov     esi, [ebp+0A4h]
004558D7 cmp     edi, 28h ; '('
004558DA jnz     short loc_4558EE
004558DC mov     ecx, [esi+950h]
004558E2 cmp     dword ptr [ecx+1E8h], 0
004558E9 setnle  al
004558EC jmp     short loc_4558F3
004558EE call    sub_460F00
004558F3 test    al, al
004558F5 jnz     short loc_4558F9
004558F7 xor     bl, bl
004558F9 inc     edi
004558FA cmp     edi, 30h ; '0'
004558FD jle     short loc_4558D1
004558FF test    bl, bl
00455901 jz      short loc_455937
00455903 sub     esp, 1Ch
00455906 mov     eax, 6
0045590B mov     esi, esp
0045590D mov     byte ptr [esp+34h+arg_0], 1
00455912 mov     [esp+34h+var_8], eax
00455916 lea     edi, [esp+34h+arg_0]
0045591A lea     ebx, [esp+34h+var_8]
0045591E mov     [esp+34h+var_4], esp
00455922 call    sub_463B60
00455927 mov     edx, [ebp+0A4h]
0045592D push    edx; int
0045592E mov     ecx, edi
00455930 mov     edx, ebx
00455932 call    sub_463910
00455937 pop     edi
00455938 pop     esi
00455939 pop     ebp
0045593A pop     ebx
0045593B add     esp, 8
0045593E retn    4
