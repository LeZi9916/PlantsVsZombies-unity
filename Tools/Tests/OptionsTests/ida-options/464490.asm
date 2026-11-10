00464490  push    0FFFFFFFFh
00464492  push    offset SEH_4643D0
00464497  mov     eax, large fs:0
0046449D  push    eax
0046449E  mov     large fs:0, esp
004644A5  push    ecx
004644A6  push    104h; Size
004644AB  call    ??2@YAPAXI@Z; operator new(uint)
004644B0  add     esp, 4
004644B3  mov     [esp+10h+var_10], eax
004644B6  mov     [esp+10h+var_4], 0
004644BE  test    eax, eax
004644C0  jz      short loc_4644E3
004644C2  mov     ecx, [esp+10h+arg_4]
004644C6  mov     edx, [esp+10h+arg_0]
004644CA  push    ecx
004644CB  mov     ecx, dword_752A98
004644D1  push    edx
004644D2  mov     edx, dword_75382C
004644D8  push    ecx
004644D9  push    edx
004644DA  mov     ecx, eax
004644DC  call    sub_5CE0A0
004644E1  jmp     short loc_4644E5
004644E3  xor     eax, eax
004644E5  mov     cl, [esp+10h+arg_8]
004644E9  mov     [eax+0A8h], cl
004644EF  mov     cl, 1
004644F1  mov     [eax+50h], cl
004644F4  mov     [eax+6Ah], cl
004644F7  mov     ecx, [esp+10h+var_C]
004644FB  mov     large fs:0, ecx
00464502  add     esp, 10h
00464505  retn
006EA680  mov     eax, [ebp-10h]; Microsoft VisualC 2-14/net runtime
006EA683  push    eax; void *
006EA684  call    ??3@YAXPAX@Z; operator delete(void *)
006EA689  pop     ecx
006EA68A  retn
006EA68B  mov     eax, offset stru_73CE58
006EA690  jmp     ___CxxFrameHandler3
