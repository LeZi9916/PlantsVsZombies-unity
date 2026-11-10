004574B0 cmp     byte ptr [ecx+50h], 0
004574B4 mov     eax, [esp+arg_0]
004574B8 jz      short loc_4574BF
004574BA cmp     eax, 65h ; 'e'
004574BD jz      short locret_45752C
004574BF cmp     byte ptr [ecx+51h], 0
004574C3 jz      short loc_4574CA
004574C5 cmp     eax, 66h ; 'f'
004574C8 jz      short locret_45752C
004574CA cmp     byte ptr [ecx+52h], 0
004574CE jz      short loc_4574D5
004574D0 cmp     eax, 6Eh ; 'n'
004574D3 jz      short locret_45752C
004574D5 mov     ecx, [ecx+4]
004574D8 cmp     byte ptr [ecx+9E9h], 0
004574DF jnz     short locret_45752C
004574E1 fldz
004574E3 push    esi
004574E4 mov     esi, [ecx+888h]
004574EA fst     [esp+4+arg_0]
004574EE mov     ecx, dword_755E50
004574F4 fcomp   dword ptr [ecx+20Ch]
004574FA fnstsw  ax
004574FC test    ah, 44h
004574FF jnp     short loc_457517
00457501 fld     dword ptr [ecx+20Ch]
00457507 push    ecx
00457508 fstp    [esp+8+var_8]; float
0045750B call    sub_5EECE0
00457510 fstp    [esp+8+arg_0]
00457514 add     esp, 4
00457517 fld     [esp+4+arg_0]
0045751B push    ecx
0045751C mov     eax, 0Ah
00457521 fstp    [esp+8+var_8]; float
00457524 mov     ecx, esi
00457526 call    sub_52A5A0
0045752B pop     esi
0045752C retn    4
