005CFEB0  push    ebp
005CFEB1  mov     ebp, esp
005CFEB3  push    ecx
005CFEB4  mov     [ebp+var_4], ecx
005CFEB7  mov     eax, [ebp+arg_4]
005CFEBA  push    eax
005CFEBB  mov     ecx, [ebp+arg_0]
005CFEBE  push    ecx
005CFEBF  mov     ecx, [ebp+var_4]
005CFEC2  call    sub_5C4F90
005CFEC7  cmp     [ebp+arg_0], 3
005CFECB  jnz     short loc_5CFF2D
005CFECD  mov     edx, [ebp+var_4]
005CFED0  cmp     dword ptr [edx+0ACh], 0
005CFED7  jz      short loc_5CFEFB
005CFED9  mov     eax, [ebp+arg_4]
005CFEDC  push    eax
005CFEDD  push    0
005CFEDF  mov     ecx, [ebp+var_4]
005CFEE2  mov     edx, [ecx+0ACh]
005CFEE8  mov     eax, [ebp+var_4]
005CFEEB  mov     ecx, [eax+0ACh]
005CFEF1  mov     edx, [edx]
005CFEF3  mov     eax, [edx+88h]
005CFEF9  call    eax
005CFEFB  mov     ecx, [ebp+var_4]
005CFEFE  cmp     dword ptr [ecx+0B0h], 0
005CFF05  jz      short loc_5CFF2B
005CFF07  mov     edx, [ebp+arg_4]
005CFF0A  push    edx
005CFF0B  push    0
005CFF0D  mov     eax, [ebp+var_4]
005CFF10  mov     ecx, [eax+0B0h]
005CFF16  mov     edx, [ebp+var_4]
005CFF19  mov     eax, [edx+0B0h]
005CFF1F  mov     edx, [ecx]
005CFF21  mov     ecx, eax
005CFF23  mov     eax, [edx+88h]
005CFF29  call    eax
005CFF2B  jmp     short loc_5CFF93
005CFF2D  cmp     [ebp+arg_0], 4
005CFF31  jnz     short loc_5CFF93
005CFF33  mov     ecx, [ebp+var_4]
005CFF36  cmp     dword ptr [ecx+0ACh], 0
005CFF3D  jz      short loc_5CFF63
005CFF3F  mov     edx, [ebp+arg_4]
005CFF42  push    edx
005CFF43  push    1
005CFF45  mov     eax, [ebp+var_4]
005CFF48  mov     ecx, [eax+0ACh]
005CFF4E  mov     edx, [ebp+var_4]
005CFF51  mov     eax, [edx+0ACh]
005CFF57  mov     edx, [ecx]
005CFF59  mov     ecx, eax
005CFF5B  mov     eax, [edx+88h]
005CFF61  call    eax
005CFF63  mov     ecx, [ebp+var_4]
005CFF66  cmp     dword ptr [ecx+0B0h], 0
005CFF6D  jz      short loc_5CFF93
005CFF6F  mov     edx, [ebp+arg_4]
005CFF72  push    edx
005CFF73  push    1
005CFF75  mov     eax, [ebp+var_4]
005CFF78  mov     ecx, [eax+0B0h]
005CFF7E  mov     edx, [ebp+var_4]
005CFF81  mov     eax, [edx+0B0h]
005CFF87  mov     edx, [ecx]
005CFF89  mov     ecx, eax
005CFF8B  mov     eax, [edx+88h]
005CFF91  call    eax
005CFF93  mov     esp, ebp
005CFF95  pop     ebp
005CFF96  retn    8
