0045C4A0 push    0FFFFFFFFh
0045C4A2 push    offset SEH_4643D0
0045C4A7 mov     eax, large fs:0
0045C4AD push    eax
0045C4AE mov     large fs:0, esp
0045C4B5 push    ecx
0045C4B6 push    edi
0045C4B7 mov     edi, ecx
0045C4B9 call    sub_45BED0
0045C4BE mov     eax, [edi+870h]
0045C4C4 test    eax, eax
0045C4C6 jz      short loc_45C4E9
0045C4C8 mov     ecx, [edi+320h]
0045C4CE mov     edx, [ecx]
0045C4D0 push    eax
0045C4D1 mov     eax, [edx+10h]
0045C4D4 call    eax
0045C4D6 mov     edx, [edi]
0045C4D8 mov     eax, [edi+870h]
0045C4DE mov     edx, [edx+88h]
0045C4E4 push    eax
0045C4E5 mov     ecx, edi
0045C4E7 call    edx
0045C4E9 push    178h; Size
0045C4EE mov     dword ptr [edi+920h], 1
0045C4F8 call    ??2@YAPAXI@Z; operator new(uint)
0045C4FD add     esp, 4
0045C500 mov     [esp+14h+var_10], eax
0045C504 mov     [esp+14h+var_4], 0
0045C50C test    eax, eax
0045C50E jz      short loc_45C519
0045C510 push    edi
0045C511 push    eax
0045C512 call    sub_4539E0
0045C517 jmp     short loc_45C51B
0045C519 xor     eax, eax
0045C51B push    0BB8h
0045C520 mov     [esp+18h+var_4], 0FFFFFFFFh
0045C528 push    960h
0045C52D mov     [edi+870h], eax
0045C533 mov     edx, [eax]
0045C535 push    0
0045C537 mov     ecx, eax
0045C539 mov     eax, [edx+0A0h]
0045C53F push    0
0045C541 call    eax
0045C543 mov     ecx, [edi+320h]
0045C549 mov     edx, [ecx]
0045C54B mov     eax, [edi+870h]
0045C551 mov     edx, [edx+0Ch]
0045C554 push    eax
0045C555 call    edx
0045C557 mov     ecx, [edi+320h]
0045C55D mov     eax, [ecx]
0045C55F mov     edx, [edi+870h]
0045C565 mov     eax, [eax+30h]
0045C568 push    edx
0045C569 call    eax
0045C56B mov     ecx, [edi+320h]
0045C571 mov     edx, [ecx]
0045C573 mov     eax, [edi+870h]
0045C579 mov     edx, [edx+20h]
0045C57C push    eax
0045C57D call    edx
0045C57F mov     ecx, [esp+14h+var_C]
0045C583 mov     large fs:0, ecx
0045C58A pop     edi
0045C58B add     esp, 10h
0045C58E retn
006EA680 mov     eax, [ebp-10h]; Microsoft VisualC 2-14/net runtime
006EA683 push    eax; void *
006EA684 call    ??3@YAXPAX@Z; operator delete(void *)
006EA689 pop     ecx
006EA68A retn
006EA68B mov     eax, offset stru_73CE58
006EA690 jmp     ___CxxFrameHandler3
