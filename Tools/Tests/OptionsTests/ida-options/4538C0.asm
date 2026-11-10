004538C0  push    ebp
004538C1  mov     ebp, esp
004538C3  and     esp, 0FFFFFFF8h
004538C6  sub     esp, 0Ch
004538C9  cmp     byte ptr [ecx+164h], 0
004538D0  push    esi
004538D1  jnz     short loc_4538E7
004538D3  mov     eax, [ebp+arg_4]
004538D6  mov     edx, [ebp+arg_0]
004538D9  push    eax
004538DA  push    edx
004538DB  call    sub_553280
004538E0  pop     esi
004538E1  mov     esp, ebp
004538E3  pop     ebp
004538E4  retn    8
004538E7  fild    [ebp+arg_0]
004538EA  lea     edx, [ecx+168h]
004538F0  lea     esi, [esp+10h+var_8]
004538F4  fstp    [esp+10h+var_8]
004538F8  fild    [ebp+arg_4]
004538FB  fstp    [esp+10h+var_4]
004538FF  call    sub_528D90
00453904  test    al, al
00453906  setnz   al
00453909  pop     esi
0045390A  mov     esp, ebp
0045390C  pop     ebp
0045390D  retn    8
