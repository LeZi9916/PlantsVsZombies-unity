004571F0 sub     esp, 1Ch
004571F3 push    esi
004571F4 mov     esi, ecx
004571F6 cmp     byte ptr [esi+0E8h], 0
004571FD jnz     loc_457342
00457203 cmp     byte_755DF5, 0
0045720A jz      loc_457342
00457210 cmp     [esp+20h+arg_0], 75h ; 'u'
00457216 jnz     loc_457342
0045721C mov     eax, [esi+0A4h]
00457222 cmp     dword ptr [eax+950h], 0
00457229 jz      loc_457342
0045722F mov     ecx, eax
00457231 mov     edx, [ecx+950h]
00457237 mov     dword ptr [edx+54h], 2
0045723E mov     eax, [esi+0A4h]
00457244 mov     eax, [eax+950h]
0045724A add     dword ptr [eax+50h], 0C350h
00457251 mov     ecx, [eax+50h]
00457254 cmp     ecx, 1869Fh
0045725A jle     short loc_457265
0045725C mov     dword ptr [eax+50h], 1869Fh
00457263 jmp     short loc_457270
00457265 test    ecx, ecx
00457267 jge     short loc_457270
00457269 mov     dword ptr [eax+50h], 0
00457270 mov     ecx, [esi+0A4h]
00457276 mov     edx, [ecx+950h]
0045727C push    ebx
0045727D push    edi
0045727E mov     edi, 1
00457283 mov     [edx+330h], edi
00457289 mov     eax, [esi+0A4h]
0045728F mov     ecx, [eax+950h]
00457295 mov     [ecx+34Ch], edi
0045729B mov     edx, [esi+0A4h]
004572A1 mov     eax, [edx+950h]
004572A7 mov     [eax+350h], edi
004572AD mov     ecx, [esi+0A4h]
004572B3 mov     edx, [ecx+950h]
004572B9 mov     [edx+364h], edi
004572BF mov     eax, edi
004572C1 lea     ecx, [edi+57h]
004572C4 lea     edx, [edi+63h]
004572C7 cmp     eax, 32h ; '2'
004572CA jz      short loc_4572EE
004572CC cmp     eax, 3Ch ; '<'
004572CF jz      short loc_4572EE
004572D1 cmp     eax, 46h ; 'F'
004572D4 jz      short loc_4572EE
004572D6 cmp     eax, 0Dh
004572D9 jz      short loc_4572EE
004572DB mov     ebx, [esi+0A4h]
004572E1 mov     ebx, [ebx+950h]
004572E7 mov     dword ptr [ecx+ebx], 14h
004572EE add     ecx, 4
004572F1 add     eax, edi
004572F3 sub     edx, edi
004572F5 jnz     short loc_4572C7
004572F7 push    edi
004572F8 mov     ecx, esi
004572FA call    sub_455420
004572FF mov     eax, [esi+0A4h]
00457305 mov     ecx, [eax+950h]
0045730B mov     ecx, [ecx+20h]
0045730E push    0
00457310 lea     edx, [esp+2Ch+var_1C]
00457314 call    sub_464510
00457319 mov     ecx, [esi+0A4h]
0045731F add     esp, 4
00457322 lea     edx, [esp+28h+var_1C]
00457326 push    edx
00457327 call    sub_620130
0045732C cmp     [esp+28h+var_4], 10h
00457331 pop     edi
00457332 pop     ebx
00457333 jb      short loc_457342
00457335 mov     eax, [esp+20h+var_18]
00457339 push    eax; void *
0045733A call    ??3@YAXPAX@Z; operator delete(void *)
0045733F add     esp, 4
00457342 pop     esi
00457343 add     esp, 1Ch
00457346 retn    4
