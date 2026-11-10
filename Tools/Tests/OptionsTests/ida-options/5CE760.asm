005CE760  push    ebp
005CE761  mov     ebp, esp
005CE763  sub     esp, 8
005CE766  mov     [ebp+var_4], ecx
005CE769  mov     eax, [ebp+arg_8]
005CE76C  cmp     dword ptr [eax+8], 0
005CE770  jz      short loc_5CE7AC
005CE772  mov     ecx, [ebp+var_4]
005CE775  cmp     dword ptr [ecx+0C8h], 0
005CE77C  jnz     short loc_5CE786
005CE77E  mov     edx, [ebp+arg_4]
005CE781  mov     [ebp+var_8], edx
005CE784  jmp     short loc_5CE792
005CE786  mov     eax, [ebp+var_4]
005CE789  mov     ecx, [eax+0C8h]
005CE78F  mov     [ebp+var_8], ecx
005CE792  mov     edx, [ebp+arg_8]
005CE795  push    edx; int
005CE796  mov     eax, [ebp+arg_10]
005CE799  push    eax; int
005CE79A  mov     ecx, [ebp+arg_C]
005CE79D  push    ecx; int
005CE79E  mov     edx, [ebp+var_8]
005CE7A1  push    edx; Concurrency::details::ExecutionResource *
005CE7A2  mov     ecx, [ebp+arg_0]
005CE7A5  call    sub_5930D0
005CE7AA  jmp     short loc_5CE7C0
005CE7AC  mov     eax, [ebp+arg_10]
005CE7AF  push    eax; int
005CE7B0  mov     ecx, [ebp+arg_C]
005CE7B3  push    ecx; int
005CE7B4  mov     edx, [ebp+arg_4]
005CE7B7  push    edx; Concurrency::details::ExecutionResource *
005CE7B8  mov     ecx, [ebp+arg_0]
005CE7BB  call    sub_592F90
005CE7C0  mov     esp, ebp
005CE7C2  pop     ebp
005CE7C3  retn    14h
