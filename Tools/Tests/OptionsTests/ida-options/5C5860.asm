005C5860  push    ebp
005C5861  mov     ebp, esp
005C5863  sub     esp, 28h
005C5866  mov     [ebp+var_24], ecx
005C5869  mov     [ebp+var_8], 0Ah
005C5870  mov     [ebp+var_C], 1
005C5877  mov     eax, [ebp+arg_4]
005C587A  cmp     eax, [ebp+var_8]
005C587D  jl      short loc_5C5893
005C587F  mov     ecx, [ebp+var_C]
005C5882  add     ecx, 1
005C5885  mov     [ebp+var_C], ecx
005C5888  mov     edx, [ebp+var_8]
005C588B  imul    edx, 0Ah
005C588E  mov     [ebp+var_8], edx
005C5891  jmp     short loc_5C5877
005C5893  cmp     [ebp+arg_4], 0
005C5897  jnz     short loc_5C58A0
005C5899  mov     [ebp+var_8], 0Ah
005C58A0  mov     ecx, [ebp+arg_10]; this
005C58A3  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C58A8  cdq
005C58A9  mov     ecx, 0Ah
005C58AE  idiv    ecx
005C58B0  mov     [ebp+var_4], eax
005C58B3  mov     [ebp+var_10], 0
005C58BA  jmp     short loc_5C58C5
005C58BC  mov     edx, [ebp+var_10]
005C58BF  add     edx, 1
005C58C2  mov     [ebp+var_10], edx
005C58C5  mov     eax, [ebp+var_10]
005C58C8  cmp     eax, [ebp+var_C]
005C58CB  jge     loc_5C597B
005C58D1  mov     eax, [ebp+var_8]
005C58D4  cdq
005C58D5  mov     ecx, 0Ah
005C58DA  idiv    ecx
005C58DC  mov     [ebp+var_8], eax
005C58DF  mov     eax, [ebp+arg_4]
005C58E2  cdq
005C58E3  idiv    [ebp+var_8]
005C58E6  cdq
005C58E7  mov     ecx, 0Ah
005C58EC  idiv    ecx
005C58EE  mov     [ebp+var_14], edx
005C58F1  mov     ecx, [ebp+arg_0]
005C58F4  call    sub_5927C0
005C58F9  mov     [ebp+var_18], eax
005C58FC  mov     ecx, [ebp+arg_10]
005C58FF  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C5904  push    eax
005C5905  mov     edx, [ebp+var_4]
005C5908  push    edx
005C5909  mov     eax, [ebp+arg_C]
005C590C  push    eax
005C590D  mov     ecx, [ebp+var_4]
005C5910  add     ecx, [ebp+arg_14]
005C5913  imul    ecx, [ebp+var_10]
005C5917  add     ecx, [ebp+arg_8]
005C591A  push    ecx
005C591B  mov     ecx, [ebp+var_18]
005C591E  call    sub_5939F0
005C5923  mov     edx, [ebp+arg_C]
005C5926  push    edx; int
005C5927  mov     eax, [ebp+var_4]
005C592A  add     eax, [ebp+arg_14]
005C592D  imul    eax, [ebp+var_10]
005C5931  add     eax, [ebp+arg_8]
005C5934  mov     ecx, [ebp+var_14]
005C5937  imul    ecx, [ebp+var_4]
005C593B  sub     eax, ecx
005C593D  push    eax; int
005C593E  mov     edx, [ebp+arg_10]
005C5941  push    edx; Concurrency::details::ExecutionResource *
005C5942  mov     ecx, [ebp+var_18]
005C5945  call    sub_592F90
005C594A  mov     eax, [ebp+var_18]
005C594D  mov     [ebp+var_20], eax
005C5950  mov     ecx, [ebp+var_20]
005C5953  mov     [ebp+var_1C], ecx
005C5956  cmp     [ebp+var_1C], 0
005C595A  jz      short loc_5C596F
005C595C  push    1
005C595E  mov     edx, [ebp+var_1C]
005C5961  mov     eax, [edx]
005C5963  mov     ecx, [ebp+var_1C]
005C5966  mov     edx, [eax]
005C5968  call    edx
005C596A  mov     [ebp+var_28], eax
005C596D  jmp     short loc_5C5976
005C596F  mov     [ebp+var_28], 0
005C5976  jmp     loc_5C58BC
005C597B  mov     esp, ebp
005C597D  pop     ebp
005C597E  retn    18h
