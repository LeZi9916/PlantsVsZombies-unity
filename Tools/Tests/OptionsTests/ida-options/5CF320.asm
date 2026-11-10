005CF320  push    ebp
005CF321  mov     ebp, esp
005CF323  push    ecx
005CF324  mov     [ebp+var_4], ecx
005CF327  mov     eax, [ebp+arg_4]
005CF32A  push    eax
005CF32B  mov     ecx, [ebp+arg_0]
005CF32E  push    ecx
005CF32F  mov     ecx, [ebp+var_4]
005CF332  call    unknown_libname_15; Microsoft VisualC 2-14/net runtime
005CF337  mov     edx, [ebp+arg_4]
005CF33A  push    edx
005CF33B  mov     eax, [ebp+arg_0]
005CF33E  push    eax
005CF33F  mov     ecx, [ebp+var_4]
005CF342  mov     edx, [ecx+0A0h]
005CF348  push    edx
005CF349  mov     eax, [ebp+var_4]
005CF34C  mov     ecx, [eax+11Ch]
005CF352  mov     edx, [ebp+var_4]
005CF355  mov     eax, [edx+11Ch]
005CF35B  mov     edx, [ecx]
005CF35D  mov     ecx, eax
005CF35F  mov     eax, [edx+18h]
005CF362  call    eax
005CF364  mov     esp, ebp
005CF366  pop     ebp
005CF367  retn    8
