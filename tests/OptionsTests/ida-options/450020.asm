00450020  push    0FFFFFFFFh
00450022  push    offset SEH_531EF0
00450027  mov     eax, large fs:0
0045002D  push    eax
0045002E  mov     large fs:0, esp
00450035  sub     esp, 20h
00450038  push    ebx
00450039  xor     ebx, ebx
0045003B  mov     eax, edi
0045003D  push    ebp
0045003E  mov     [esp+34h+var_2C], ebx
00450042  mov     [esp+34h+var_10], 0Fh
0045004A  mov     [esp+34h+var_14], ebx
0045004E  mov     byte ptr [esp+34h+var_24], bl
00450052  lea     ebp, [eax+1]
00450055  mov     cl, [eax]
00450057  inc     eax
00450058  cmp     cl, bl
0045005A  jnz     short loc_450055
0045005C  sub     eax, ebp
0045005E  push    eax; SourceSize
0045005F  push    edi; Source
00450060  lea     ecx, [esp+3Ch+var_28]
00450064  call    sub_4032C0
00450069  mov     eax, [esp+34h+arg_0]
0045006D  push    0FFFFFFFFh; SourceSize
0045006F  push    ebx; int
00450070  push    eax; int
00450071  lea     ecx, [esp+40h+var_28]
00450075  mov     [esp+40h+var_4], ebx
00450079  call    sub_403060
0045007E  push    0FFFFFFFFh
00450080  push    ebx
00450081  mov     dword ptr [esi+18h], 0Fh
00450088  mov     [esi+14h], ebx
0045008B  push    eax
0045008C  mov     ecx, esi
0045008E  mov     [esi+4], bl
00450091  call    sub_401D80
00450096  cmp     [esp+34h+var_10], 10h
0045009B  pop     ebp
0045009C  pop     ebx
0045009D  jb      short loc_4500AC
0045009F  mov     ecx, [esp+2Ch+var_24]
004500A3  push    ecx; void *
004500A4  call    ??3@YAXPAX@Z; operator delete(void *)
004500A9  add     esp, 4
004500AC  mov     ecx, [esp+2Ch+var_C]
004500B0  mov     eax, esi
004500B2  mov     large fs:0, ecx
004500B9  add     esp, 2Ch
004500BC  retn
006ED790  lea     ecx, [ebp-28h]; void *
006ED793  jmp     sub_401E90
006ED798  mov     eax, offset stru_73F8F4
006ED79D  jmp     ___CxxFrameHandler3
