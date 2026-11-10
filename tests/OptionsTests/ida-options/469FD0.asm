00469FD0  mov     eax, large fs:0
00469FD6  push    0FFFFFFFFh
00469FD8  push    offset SEH_5297A0
00469FDD  push    eax
00469FDE  mov     large fs:0, esp
00469FE5  sub     esp, 48h
00469FE8  cmp     dword ptr [edi+1Ch], 5555554h
00469FEF  push    ebx
00469FF0  push    ebp
00469FF1  push    esi
00469FF2  mov     esi, ecx
00469FF4  jb      short loc_46A042
00469FF6  push    13h; SourceSize
00469FF8  xor     esi, esi
00469FFA  push    offset aMapSetTTooLong; "map/set<T> too long"
00469FFF  lea     ecx, [esp+68h+var_54]
0046A003  mov     [esp+68h+var_3C], 0Fh
0046A00B  mov     [esp+68h+var_40], esi
0046A00F  mov     [esp+68h+var_50], 0
0046A014  call    sub_4032C0
0046A019  lea     eax, [esp+60h+var_54]
0046A01D  push    eax
0046A01E  lea     ecx, [esp+64h+pExceptionObject]
0046A022  mov     [esp+64h+var_4], esi
0046A026  call    sub_408720
0046A02B  push    offset __TI3?AVlength_error@std@@; pThrowInfo
0046A030  lea     ecx, [esp+64h+pExceptionObject]
0046A034  push    ecx; pExceptionObject
0046A035  mov     [esp+68h+pExceptionObject], offset ??_7length_error@std@@6B@; const std::length_error::`vftable'
0046A03D  call    __CxxThrowException@8; _CxxThrowException(x,x)
0046A042  mov     edx, [esp+60h+arg_8]
0046A046  mov     eax, [edi+18h]
0046A049  push    edx
0046A04A  push    eax
0046A04B  push    esi
0046A04C  push    eax
0046A04D  call    sub_46A260
0046A052  mov     ebp, eax
0046A054  mov     eax, [edi+18h]
0046A057  mov     ebx, 1
0046A05C  add     [edi+1Ch], ebx
0046A05F  cmp     esi, eax
0046A061  jnz     short loc_46A073
0046A063  mov     [eax+4], ebp
0046A066  mov     eax, [edi+18h]
0046A069  mov     [eax], ebp
0046A06B  mov     ecx, [edi+18h]
0046A06E  mov     [ecx+8], ebp
0046A071  jmp     short loc_46A095
0046A073  cmp     [esp+60h+arg_4], 0
0046A078  jz      short loc_46A087
0046A07A  mov     [esi], ebp
0046A07C  mov     eax, [edi+18h]
0046A07F  cmp     esi, [eax]
0046A081  jnz     short loc_46A095
0046A083  mov     [eax], ebp
0046A085  jmp     short loc_46A095
0046A087  mov     [esi+8], ebp
0046A08A  mov     eax, [edi+18h]
0046A08D  cmp     esi, [eax+8]
0046A090  jnz     short loc_46A095
0046A092  mov     [eax+8], ebp
0046A095  mov     edx, [ebp+4]
0046A098  cmp     byte ptr [edx+40h], 0
0046A09C  lea     eax, [ebp+4]
0046A09F  mov     esi, ebp
0046A0A1  jnz     loc_46A186
0046A0A7  mov     ecx, [eax]
0046A0A9  mov     edx, [ecx+4]
0046A0AC  cmp     ecx, [edx]
0046A0AE  jnz     short loc_46A0FD
0046A0B0  mov     edx, [edx+8]
0046A0B3  cmp     byte ptr [edx+40h], 0
0046A0B7  jnz     short loc_46A0D2
0046A0B9  mov     [ecx+40h], bl
0046A0BC  mov     [edx+40h], bl
0046A0BF  mov     edx, [eax]
0046A0C1  mov     ecx, [edx+4]
0046A0C4  mov     byte ptr [ecx+40h], 0
0046A0C8  mov     edx, [eax]
0046A0CA  mov     esi, [edx+4]
0046A0CD  jmp     loc_46A176
0046A0D2  cmp     esi, [ecx+8]
0046A0D5  jnz     short loc_46A0DF
0046A0D7  push    edi
0046A0D8  mov     esi, ecx
0046A0DA  call    sub_46A1C0
0046A0DF  mov     eax, [esi+4]
0046A0E2  mov     [eax+40h], bl
0046A0E5  mov     ecx, [esi+4]
0046A0E8  mov     edx, [ecx+4]
0046A0EB  mov     byte ptr [edx+40h], 0
0046A0EF  mov     eax, [esi+4]
0046A0F2  mov     ecx, [eax+4]
0046A0F5  push    edi
0046A0F6  call    sub_46A210
0046A0FB  jmp     short loc_46A176
0046A0FD  mov     edx, [edx]
0046A0FF  cmp     byte ptr [edx+40h], 0
0046A103  jnz     short loc_46A11B
0046A105  mov     [ecx+40h], bl
0046A108  mov     [edx+40h], bl
0046A10B  mov     edx, [eax]
0046A10D  mov     ecx, [edx+4]
0046A110  mov     byte ptr [ecx+40h], 0
0046A114  mov     edx, [eax]
0046A116  mov     esi, [edx+4]
0046A119  jmp     short loc_46A176
0046A11B  cmp     esi, [ecx]
0046A11D  jnz     short loc_46A127
0046A11F  push    edi
0046A120  mov     esi, ecx
0046A122  call    sub_46A210
0046A127  mov     eax, [esi+4]
0046A12A  mov     [eax+40h], bl
0046A12D  mov     ecx, [esi+4]
0046A130  mov     edx, [ecx+4]
0046A133  mov     byte ptr [edx+40h], 0
0046A137  mov     eax, [esi+4]
0046A13A  mov     eax, [eax+4]
0046A13D  mov     ecx, [eax+8]
0046A140  mov     edx, [ecx]
0046A142  mov     [eax+8], edx
0046A145  mov     edx, [ecx]
0046A147  cmp     byte ptr [edx+41h], 0
0046A14B  jnz     short loc_46A150
0046A14D  mov     [edx+4], eax
0046A150  mov     edx, [eax+4]
0046A153  mov     [ecx+4], edx
0046A156  mov     edx, [edi+18h]
0046A159  cmp     eax, [edx+4]
0046A15C  jnz     short loc_46A163
0046A15E  mov     [edx+4], ecx
0046A161  jmp     short loc_46A171
0046A163  mov     edx, [eax+4]
0046A166  cmp     eax, [edx]
0046A168  jnz     short loc_46A16E
0046A16A  mov     [edx], ecx
0046A16C  jmp     short loc_46A171
0046A16E  mov     [edx+8], ecx
0046A171  mov     [ecx], eax
0046A173  mov     [eax+4], ecx
0046A176  mov     ecx, [esi+4]
0046A179  cmp     byte ptr [ecx+40h], 0
0046A17D  lea     eax, [esi+4]
0046A180  jz      loc_46A0A7
0046A186  mov     edx, [edi+18h]
0046A189  mov     eax, [edx+4]
0046A18C  mov     [eax+40h], bl
0046A18F  mov     ecx, [edi]
0046A191  mov     eax, [esp+60h+arg_0]
0046A195  pop     esi
0046A196  mov     [eax+4], ebp
0046A199  mov     [eax], ecx
0046A19B  mov     ecx, [esp+5Ch+var_C]
0046A19F  pop     ebp
0046A1A0  mov     large fs:0, ecx
0046A1A7  pop     ebx
0046A1A8  add     esp, 54h
0046A1AB  retn    0Ch
006E1830  lea     ecx, [ebp-54h]; void *
006E1833  jmp     sub_401E90
006E1838  mov     eax, offset stru_734010
006E183D  jmp     ___CxxFrameHandler3
