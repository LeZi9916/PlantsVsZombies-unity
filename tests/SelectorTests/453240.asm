00453240 push    0FFFFFFFFh
00453242 push    offset SEH_538080
00453247 mov     eax, large fs:0
0045324D push    eax
0045324E mov     large fs:0, esp
00453255 sub     esp, 1Ch
00453258 mov     eax, [esp+28h+arg_4]
0045325C push    edi
0045325D push    eax
0045325E lea     edi, [esp+30h+var_28]
00453262 call    sub_52EA80
00453267 add     esp, 4
0045326A mov     ecx, [esp+2Ch+arg_0]
0045326E push    0FFFFFFFFh
00453270 push    0
00453272 push    eax
00453273 add     ecx, 0A4h
00453279 mov     [esp+38h+var_4], 0
00453281 call    sub_40D0E0
00453286 cmp     [esp+2Ch+var_10], 8
0045328B jb      short loc_45329A
0045328D mov     ecx, [esp+2Ch+var_24]
00453291 push    ecx; void *
00453292 call    ??3@YAXPAX@Z; operator delete(void *)
00453297 add     esp, 4
0045329A mov     ecx, [esp+2Ch+var_C]
0045329E mov     large fs:0, ecx
004532A5 pop     edi
004532A6 add     esp, 28h
004532A9 retn    8
006ED5E0 lea     ecx, [ebp-28h]; void *
006ED5E3 jmp     sub_401330
006ED5E8 mov     eax, offset stru_73F6DC
006ED5ED jmp     ___CxxFrameHandler3
