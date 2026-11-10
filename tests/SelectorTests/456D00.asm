00456D00 sub     esp, 2Ch
00456D03 mov     ecx, [eax+0A4h]
00456D09 mov     eax, [eax+0FCh]
00456D0F mov     edx, [ecx+944h]
00456D15 mov     ecx, [edx+8]
00456D18 mov     edx, [esp+2Ch+String2]
00456D1C push    ebx
00456D1D push    esi
00456D1E and     eax, 0FFFFh
00456D23 lea     esi, [eax+eax*4]
00456D26 shl     esi, 5
00456D29 add     esi, [ecx]
00456D2B push    edx; String2
00456D2C mov     ebx, esi
00456D2E call    sub_4823E0
00456D33 fld     ds:flt_708310
00456D39 fst     [esp+34h+var_2C]
00456D3D mov     ebx, eax
00456D3F fst     [esp+34h+var_28]
00456D43 xor     eax, eax
00456D45 fst     [esp+34h+var_24]
00456D49 mov     [esp+34h+var_8], eax
00456D4D fst     [esp+34h+var_20]
00456D51 mov     [esp+34h+var_C], eax
00456D55 fst     [esp+34h+var_1C]
00456D59 push    esi
00456D5A fst     [esp+38h+var_18]
00456D5E lea     eax, [esp+38h+var_2C]
00456D62 fst     [esp+38h+var_14]
00456D66 mov     [esp+38h+var_4], offset Class
00456D6E fstp    [esp+38h+var_10]
00456D72 call    sub_4812C0
00456D77 fld     [esp+34h+var_2C]
00456D7B fadd    [esp+34h+arg_4]
00456D7F call    __ftol2_sse
00456D84 fld     [esp+34h+var_28]
00456D88 fadd    [esp+34h+arg_8]
00456D8C mov     [edi+40h], eax
00456D8F call    __ftol2_sse
00456D94 pop     esi
00456D95 mov     [edi+44h], eax
00456D98 pop     ebx
00456D99 add     esp, 2Ch
00456D9C retn    0Ch
