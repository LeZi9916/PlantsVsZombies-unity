005C6660  push    ebp
005C6661  mov     ebp, esp
005C6663  sub     esp, 2Ch
005C6666  mov     [ebp+var_C], ecx
005C6669  mov     eax, [ebp+var_C]
005C666C  movzx   ecx, byte ptr [eax+0BCh]
005C6673  test    ecx, ecx
005C6675  jz      loc_5C679E
005C667B  mov     edx, [ebp+var_C]
005C667E  fld     qword ptr [edx+0A8h]
005C6684  fstp    [ebp+var_8]
005C6687  mov     eax, [ebp+var_C]
005C668A  movzx   ecx, byte ptr [eax+0C8h]
005C6691  test    ecx, ecx
005C6693  jz      short loc_5C66D4
005C6695  mov     edx, [ebp+var_C]
005C6698  mov     eax, [ebp+arg_0]
005C669B  sub     eax, [edx+0C0h]
005C66A1  mov     [ebp+var_10], eax
005C66A4  fild    [ebp+var_10]
005C66A7  mov     ecx, [ebp+var_C]
005C66AA  mov     ecx, [ecx+0B8h]; this
005C66B0  fstp    [ebp+var_18]
005C66B3  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C66B8  mov     edx, [ebp+var_C]
005C66BB  mov     ecx, [edx+48h]
005C66BE  sub     ecx, eax
005C66C0  mov     [ebp+var_1C], ecx
005C66C3  fild    [ebp+var_1C]
005C66C6  fdivr   [ebp+var_18]
005C66C9  mov     edx, [ebp+var_C]
005C66CC  fstp    qword ptr [edx+0A8h]
005C66D2  jmp     short loc_5C6711
005C66D4  mov     eax, [ebp+var_C]
005C66D7  mov     ecx, [ebp+arg_4]
005C66DA  sub     ecx, [eax+0C4h]
005C66E0  mov     [ebp+var_20], ecx
005C66E3  fild    [ebp+var_20]
005C66E6  mov     edx, [ebp+var_C]
005C66E9  mov     ecx, [edx+0B8h]
005C66EF  fstp    [ebp+var_28]
005C66F2  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C66F7  mov     ecx, [ebp+var_C]
005C66FA  mov     edx, [ecx+4Ch]
005C66FD  sub     edx, eax
005C66FF  mov     [ebp+var_2C], edx
005C6702  fild    [ebp+var_2C]
005C6705  fdivr   [ebp+var_28]
005C6708  mov     eax, [ebp+var_C]
005C670B  fstp    qword ptr [eax+0A8h]
005C6711  mov     ecx, [ebp+var_C]
005C6714  fldz
005C6716  fcomp   qword ptr [ecx+0A8h]
005C671C  fnstsw  ax
005C671E  test    ah, 41h
005C6721  jnz     short loc_5C672E
005C6723  mov     edx, [ebp+var_C]
005C6726  fldz
005C6728  fstp    qword ptr [edx+0A8h]
005C672E  mov     eax, [ebp+var_C]
005C6731  fld1
005C6733  fcomp   qword ptr [eax+0A8h]
005C6739  fnstsw  ax
005C673B  test    ah, 5
005C673E  jp      short loc_5C674B
005C6740  mov     ecx, [ebp+var_C]
005C6743  fld1
005C6745  fstp    qword ptr [ecx+0A8h]
005C674B  mov     edx, [ebp+var_C]
005C674E  fld     [ebp+var_8]
005C6751  fcomp   qword ptr [edx+0A8h]
005C6757  fnstsw  ax
005C6759  test    ah, 44h
005C675C  jnp     short loc_5C679E
005C675E  mov     eax, [ebp+var_C]
005C6761  sub     esp, 8
005C6764  fld     qword ptr [eax+0A8h]
005C676A  fstp    [esp+34h+var_34]
005C676D  mov     ecx, [ebp+var_C]
005C6770  mov     edx, [ecx+0B0h]
005C6776  push    edx
005C6777  mov     eax, [ebp+var_C]
005C677A  mov     ecx, [eax+0A0h]
005C6780  mov     edx, [ebp+var_C]
005C6783  mov     eax, [edx+0A0h]
005C6789  mov     edx, [ecx]
005C678B  mov     ecx, eax
005C678D  mov     eax, [edx]
005C678F  call    eax
005C6791  mov     ecx, [ebp+var_C]
005C6794  mov     edx, [ecx]
005C6796  mov     ecx, [ebp+var_C]
005C6799  mov     eax, [edx+4Ch]
005C679C  call    eax
005C679E  mov     esp, ebp
005C67A0  pop     ebp
005C67A1  retn    8
