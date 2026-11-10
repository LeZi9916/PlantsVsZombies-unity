005C6400  push    ebp
005C6401  mov     ebp, esp
005C6403  sub     esp, 2Ch
005C6406  mov     [ebp+var_24], ecx
005C6409  mov     eax, [ebp+var_24]
005C640C  movzx   ecx, byte ptr [eax+0C8h]
005C6413  test    ecx, ecx
005C6415  jz      loc_5C64BB
005C641B  mov     edx, [ebp+var_24]
005C641E  mov     ecx, [edx+0B8h]; this
005C6424  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C6429  mov     ecx, [ebp+var_24]
005C642C  mov     edx, [ecx+48h]
005C642F  sub     edx, eax
005C6431  mov     [ebp+var_28], edx
005C6434  fild    [ebp+var_28]
005C6437  mov     eax, [ebp+var_24]
005C643A  fmul    qword ptr [eax+0A8h]
005C6440  call    __ftol2_sse
005C6445  mov     [ebp+var_4], eax
005C6448  mov     ecx, [ebp+arg_0]
005C644B  cmp     ecx, [ebp+var_4]
005C644E  jl      short loc_5C6491
005C6450  mov     edx, [ebp+var_24]
005C6453  mov     ecx, [edx+0B8h]; this
005C6459  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C645E  add     eax, [ebp+var_4]
005C6461  cmp     [ebp+arg_0], eax
005C6464  jge     short loc_5C6491
005C6466  push    2
005C6468  mov     eax, [ebp+var_24]
005C646B  mov     ecx, [eax+20h]
005C646E  mov     ecx, [ecx+6Ch]
005C6471  call    sub_62C810
005C6476  mov     edx, [ebp+var_24]
005C6479  mov     byte ptr [edx+0BCh], 1
005C6480  mov     eax, [ebp+arg_0]
005C6483  sub     eax, [ebp+var_4]
005C6486  mov     ecx, [ebp+var_24]
005C6489  mov     [ecx+0C0h], eax
005C648F  jmp     short loc_5C64B6
005C6491  fild    [ebp+arg_0]
005C6494  mov     edx, [ebp+var_24]
005C6497  fidiv   dword ptr [edx+48h]
005C649A  fstp    [ebp+var_10]
005C649D  sub     esp, 8
005C64A0  fld     [ebp+var_10]
005C64A3  fstp    [esp+34h+var_34]
005C64A6  mov     eax, [ebp+var_24]
005C64A9  mov     edx, [eax]
005C64AB  mov     ecx, [ebp+var_24]
005C64AE  mov     eax, [edx+118h]
005C64B4  call    eax
005C64B6  jmp     loc_5C6556
005C64BB  mov     ecx, [ebp+var_24]
005C64BE  mov     ecx, [ecx+0B8h]
005C64C4  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C64C9  mov     edx, [ebp+var_24]
005C64CC  mov     ecx, [edx+4Ch]
005C64CF  sub     ecx, eax
005C64D1  mov     [ebp+var_2C], ecx
005C64D4  fild    [ebp+var_2C]
005C64D7  mov     edx, [ebp+var_24]
005C64DA  fmul    qword ptr [edx+0A8h]
005C64E0  call    __ftol2_sse
005C64E5  mov     [ebp+var_14], eax
005C64E8  mov     eax, [ebp+arg_4]
005C64EB  cmp     eax, [ebp+var_14]
005C64EE  jl      short loc_5C6531
005C64F0  mov     ecx, [ebp+var_24]
005C64F3  mov     ecx, [ecx+0B8h]
005C64F9  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C64FE  add     eax, [ebp+var_14]
005C6501  cmp     [ebp+arg_4], eax
005C6504  jge     short loc_5C6531
005C6506  push    2
005C6508  mov     edx, [ebp+var_24]
005C650B  mov     eax, [edx+20h]
005C650E  mov     ecx, [eax+6Ch]
005C6511  call    sub_62C810
005C6516  mov     ecx, [ebp+var_24]
005C6519  mov     byte ptr [ecx+0BCh], 1
005C6520  mov     edx, [ebp+arg_4]
005C6523  sub     edx, [ebp+var_14]
005C6526  mov     eax, [ebp+var_24]
005C6529  mov     [eax+0C4h], edx
005C652F  jmp     short loc_5C6556
005C6531  fild    [ebp+arg_4]
005C6534  mov     ecx, [ebp+var_24]
005C6537  fidiv   dword ptr [ecx+4Ch]
005C653A  fstp    [ebp+var_20]
005C653D  sub     esp, 8
005C6540  fld     [ebp+var_20]
005C6543  fstp    [esp+34h+var_34]
005C6546  mov     edx, [ebp+var_24]
005C6549  mov     eax, [edx]
005C654B  mov     ecx, [ebp+var_24]
005C654E  mov     edx, [eax+118h]
005C6554  call    edx
005C6556  mov     esp, ebp
005C6558  pop     ebp
005C6559  retn    0Ch
