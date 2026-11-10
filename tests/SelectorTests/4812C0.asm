004812C0 sub     esp, 0Ch
004812C3 push    ebp
004812C4 mov     ebp, [esp+10h+arg_0]
004812C8 push    esi
004812C9 push    edi
004812CA mov     esi, eax
004812CC lea     edi, [esp+18h+var_C]
004812D0 mov     eax, ebp
004812D2 call    sub_482180
004812D7 mov     eax, ebx
004812D9 mov     ecx, ebp
004812DB call    sub_481350
004812E0 fld     dword ptr [esi+18h]
004812E3 fstp    [esp+18h+arg_0]
004812E7 lea     edi, [ebx+ebx*2]
004812EA fldz
004812EC shl     edi, 5
004812EF fld     [esp+18h+arg_0]
004812F3 add     edi, [ebp+58h]
004812F6 fcom    st(1)
004812F8 fnstsw  ax
004812FA fstp    st(1)
004812FC test    ah, 41h
004812FF jnz     short loc_481309
00481301 fadd    ds:dbl_7271A8
00481307 jmp     short loc_48130F
00481309 fsub    ds:dbl_7271A8
0048130F call    __ftol2_sse
00481314 test    eax, eax
00481316 jl      short loc_481344
00481318 mov     eax, [edi]
0048131A mov     [esp+18h+arg_0], eax
0048131E test    eax, eax
00481320 jle     short loc_481344
00481322 fild    [esp+18h+arg_0]
00481326 push    ecx
00481327 lea     eax, [edi+8]
0048132A mov     ecx, esi
0048132C fidiv   dword ptr [edi+4]
0048132F mov     edx, esi
00481331 fstp    [esp+1Ch+arg_0]
00481335 fld     [esp+1Ch+arg_0]
00481339 fstp    [esp+1Ch+var_1C]; float
0048133C call    sub_481180
00481341 add     esp, 4
00481344 pop     edi
00481345 pop     esi
00481346 pop     ebp
00481347 add     esp, 0Ch
0048134A retn    4
