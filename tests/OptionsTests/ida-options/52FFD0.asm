0052FFD0  push    0FFFFFFFFh
0052FFD2  push    offset SEH_538080
0052FFD7  mov     eax, large fs:0
0052FFDD  push    eax
0052FFDE  mov     large fs:0, esp
0052FFE5  sub     esp, 20h
0052FFE8  mov     eax, [esp+2Ch+arg_0]
0052FFEC  push    edi
0052FFED  push    eax
0052FFEE  lea     edi, [esp+34h+var_28]
0052FFF2  call    sub_52EA80
0052FFF7  add     esp, 4
0052FFFA  push    0FFFFFFFFh
0052FFFC  xor     edi, edi
0052FFFE  push    edi
0052FFFF  push    eax
00530000  lea     ecx, [esi+1Ch]
00530003  mov     [esp+3Ch+var_4], edi
00530007  call    sub_40D0E0
0053000C  mov     [esp+30h+var_4], 0FFFFFFFFh
00530014  cmp     [esp+30h+var_10], 8
00530019  jb      short loc_530028
0053001B  mov     ecx, [esp+30h+var_24]
0053001F  push    ecx; void *
00530020  call    ??3@YAXPAX@Z; operator delete(void *)
00530025  add     esp, 4
00530028  xor     edx, edx
0053002A  push    esi
0053002B  mov     [esp+34h+var_10], 7
00530033  mov     [esp+34h+var_14], edi
00530037  mov     word ptr [esp+34h+var_24], dx
0053003C  call    sub_52FD00
00530041  mov     ecx, [esp+30h+var_C]
00530045  mov     large fs:0, ecx
0053004C  pop     edi
0053004D  add     esp, 2Ch
00530050  retn    4
006ED5E0  lea     ecx, [ebp-28h]; void *
006ED5E3  jmp     sub_401330
006ED5E8  mov     eax, offset stru_73F6DC
006ED5ED  jmp     ___CxxFrameHandler3
