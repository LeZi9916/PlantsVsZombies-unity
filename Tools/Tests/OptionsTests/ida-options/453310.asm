00453310  push    0FFFFFFFFh
00453312  push    offset SEH_4643D0
00453317  mov     eax, large fs:0
0045331D  push    eax
0045331E  mov     large fs:0, esp
00453325  sub     esp, 8
00453328  push    esi
00453329  push    150h; Size
0045332E  call    ??2@YAPAXI@Z; operator new(uint)
00453333  mov     esi, eax
00453335  add     esp, 4
00453338  mov     [esp+18h+var_10], esi
0045333C  mov     [esp+18h+var_4], 0
00453344  test    esi, esi
00453346  jz      short loc_453363
00453348  mov     eax, [esp+18h+arg_4]
0045334C  mov     ecx, [esp+18h+arg_0]
00453350  push    eax
00453351  push    ecx
00453352  push    0
00453354  mov     ecx, esi
00453356  call    sub_5D1090
0045335B  mov     dword ptr [esi], offset off_70D628
00453361  jmp     short loc_453365
00453363  xor     esi, esi
00453365  mov     edx, [esp+18h+arg_8]
00453369  push    edx
0045336A  push    esi
0045336B  mov     [esp+20h+var_4], 0FFFFFFFFh
00453373  call    sub_453240
00453378  mov     ecx, [esp+18h+var_C]
0045337C  mov     eax, 1
00453381  mov     [esi+13Ch], eax
00453387  mov     [esi+140h], eax
0045338D  mov     [esi+50h], al
00453390  mov     [esi+6Ah], al
00453393  mov     dword ptr [esi+4Ch], 21h ; '!'
0045339A  mov     eax, esi
0045339C  mov     large fs:0, ecx
004533A3  pop     esi
004533A4  add     esp, 14h
004533A7  retn
006EA680  mov     eax, [ebp-10h]; Microsoft VisualC 2-14/net runtime
006EA683  push    eax; void *
006EA684  call    ??3@YAXPAX@Z; operator delete(void *)
006EA689  pop     ecx
006EA68A  retn
006EA68B  mov     eax, offset stru_73CE58
006EA690  jmp     ___CxxFrameHandler3
