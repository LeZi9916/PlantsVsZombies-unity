005C6560  push    ebp
005C6561  mov     ebp, esp
005C6563  sub     esp, 14h
005C6566  mov     [ebp+var_C], ecx
005C6569  mov     eax, [ebp+var_C]
005C656C  movzx   ecx, byte ptr [eax+0C8h]
005C6573  test    ecx, ecx
005C6575  jz      short loc_5C65E6
005C6577  mov     edx, [ebp+var_C]
005C657A  mov     ecx, [edx+0B8h]; this
005C6580  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C6585  mov     ecx, [ebp+var_C]
005C6588  mov     edx, [ecx+48h]
005C658B  sub     edx, eax
005C658D  mov     [ebp+var_10], edx
005C6590  fild    [ebp+var_10]
005C6593  mov     eax, [ebp+var_C]
005C6596  fmul    qword ptr [eax+0A8h]
005C659C  call    __ftol2_sse
005C65A1  mov     [ebp+var_4], eax
005C65A4  mov     ecx, [ebp+arg_0]
005C65A7  cmp     ecx, [ebp+var_4]
005C65AA  jl      short loc_5C65D4
005C65AC  mov     edx, [ebp+var_C]
005C65AF  mov     ecx, [edx+0B8h]; this
005C65B5  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C65BA  add     eax, [ebp+var_4]
005C65BD  cmp     [ebp+arg_0], eax
005C65C0  jge     short loc_5C65D4
005C65C2  push    2
005C65C4  mov     eax, [ebp+var_C]
005C65C7  mov     ecx, [eax+20h]
005C65CA  mov     ecx, [ecx+6Ch]
005C65CD  call    sub_62C810
005C65D2  jmp     short loc_5C65E4
005C65D4  push    0
005C65D6  mov     edx, [ebp+var_C]
005C65D9  mov     eax, [edx+20h]
005C65DC  mov     ecx, [eax+6Ch]
005C65DF  call    sub_62C810
005C65E4  jmp     short loc_5C6653
005C65E6  mov     ecx, [ebp+var_C]
005C65E9  mov     ecx, [ecx+0B8h]
005C65EF  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C65F4  mov     edx, [ebp+var_C]
005C65F7  mov     ecx, [edx+4Ch]
005C65FA  sub     ecx, eax
005C65FC  mov     [ebp+var_14], ecx
005C65FF  fild    [ebp+var_14]
005C6602  mov     edx, [ebp+var_C]
005C6605  fmul    qword ptr [edx+0A8h]
005C660B  call    __ftol2_sse
005C6610  mov     [ebp+var_8], eax
005C6613  mov     eax, [ebp+arg_4]
005C6616  cmp     eax, [ebp+var_8]
005C6619  jl      short loc_5C6643
005C661B  mov     ecx, [ebp+var_C]
005C661E  mov     ecx, [ecx+0B8h]
005C6624  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C6629  add     eax, [ebp+var_8]
005C662C  cmp     [ebp+arg_4], eax
005C662F  jge     short loc_5C6643
005C6631  push    2
005C6633  mov     edx, [ebp+var_C]
005C6636  mov     eax, [edx+20h]
005C6639  mov     ecx, [eax+6Ch]
005C663C  call    sub_62C810
005C6641  jmp     short loc_5C6653
005C6643  push    0
005C6645  mov     ecx, [ebp+var_C]
005C6648  mov     edx, [ecx+20h]
005C664B  mov     ecx, [edx+6Ch]
005C664E  call    sub_62C810
005C6653  mov     esp, ebp
005C6655  pop     ebp
005C6656  retn    8
