0046BC60  push    ebp
0046BC61  mov     ebp, esp
0046BC63  and     esp, 0FFFFFFF8h
0046BC66  sub     esp, 10h
0046BC69  push    esi
0046BC6A  push    edi
0046BC6B  mov     edi, [ebp+arg_0]
0046BC6E  push    edi
0046BC6F  mov     esi, ecx
0046BC71  call    sub_4651D0
0046BC76  mov     eax, [esi+18Ch]
0046BC7C  mov     ecx, [eax+40h]
0046BC7F  mov     edx, [eax+44h]
0046BC82  sub     ecx, 8
0046BC85  mov     [esp+18h+var_10], ecx
0046BC89  mov     ecx, [eax+48h]
0046BC8C  sub     edx, 4
0046BC8F  mov     [esp+18h+var_C], edx
0046BC93  mov     edx, [eax+4Ch]
0046BC96  mov     eax, dword_75346C
0046BC9B  add     ecx, 10h
0046BC9E  mov     [esp+18h+var_8], ecx
0046BCA2  push    eax
0046BCA3  lea     ecx, [esp+1Ch+var_10]
0046BCA7  add     edx, 8
0046BCAA  push    ecx
0046BCAB  mov     ecx, edi
0046BCAD  mov     [esp+20h+var_4], edx
0046BCB1  call    sub_593B10
0046BCB6  pop     edi
0046BCB7  pop     esi
0046BCB8  mov     esp, ebp
0046BCBA  pop     ebp
0046BCBB  retn    4
