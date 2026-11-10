00453910  push    0FFFFFFFFh
00453912  push    offset SEH_453910
00453917  mov     eax, large fs:0
0045391D  push    eax
0045391E  mov     large fs:0, esp
00453925  push    ecx
00453926  push    ebp
00453927  mov     ebp, [esp+14h+arg_10]
0045392B  push    esi
0045392C  push    190h; Size
00453931  call    ??2@YAPAXI@Z; operator new(uint)
00453936  add     esp, 4
00453939  mov     [esp+18h+arg_10], eax
0045393D  xor     esi, esi
0045393F  mov     [esp+18h+var_4], esi
00453943  cmp     eax, esi
00453945  jz      short loc_453958
00453947  mov     ecx, [esp+18h+arg_4]
0045394B  push    ecx
0045394C  mov     ecx, [esp+1Ch+arg_0]
00453950  push    eax
00453951  call    sub_4533B0
00453956  mov     esi, eax
00453958  mov     eax, [esp+18h+arg_C]
0045395C  mov     [esp+18h+var_4], 0FFFFFFFFh
00453964  test    eax, eax
00453966  jnz     short loc_45396D
00453968  mov     eax, dword_75339C
0045396D  mov     edx, [esi]
0045396F  push    eax
00453970  mov     eax, [edx+11Ch]
00453976  mov     ecx, esi
00453978  call    eax
0045397A  mov     ecx, [esp+18h+arg_8]
0045397E  push    ecx
0045397F  push    esi
00453980  call    sub_453240
00453985  mov     edx, [edi+24h]
00453988  mov     ecx, [esp+18h+var_C]
0045398C  mov     [esi+48h], edx
0045398F  mov     eax, [edi+28h]
00453992  mov     [esi+4Ch], eax
00453995  mov     eax, 1
0045399A  mov     [esi+50h], al
0045399D  mov     [esi+6Ah], al
004539A0  mov     [esi+13Ch], eax
004539A6  mov     [esi+140h], eax
004539AC  mov     [esi+0CCh], ebp
004539B2  mov     [esi+0C8h], edi
004539B8  mov     [esi+0D0h], ebx
004539BE  mov     eax, esi
004539C0  pop     esi
004539C1  mov     large fs:0, ecx
004539C8  pop     ebp
004539C9  add     esp, 10h
004539CC  retn
006E9790  mov     eax, [ebp+14h]
006E9793  push    eax; void *
006E9794  call    ??3@YAXPAX@Z; operator delete(void *)
006E9799  pop     ecx
006E979A  retn
006E979B  mov     eax, offset stru_73C168
006E97A0  jmp     ___CxxFrameHandler3
