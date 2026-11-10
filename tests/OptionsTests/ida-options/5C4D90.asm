005C4D90  push    ebp
005C4D91  mov     ebp, esp
005C4D93  sub     esp, 0A0h
005C4D99  mov     [ebp+var_A0], ecx
005C4D9F  mov     eax, [ebp+var_A0]
005C4DA5  add     eax, 6Ch ; 'l'
005C4DA8  mov     [ebp+var_9C], eax
005C4DAE  lea     ecx, [ebp+var_24]
005C4DB1  push    ecx
005C4DB2  mov     ecx, [ebp+var_9C]
005C4DB8  call    sub_5B74F0
005C4DBD  mov     edx, [eax]
005C4DBF  mov     eax, [eax+4]
005C4DC2  mov     [ebp+var_1C], edx
005C4DC5  mov     [ebp+var_18], eax
005C4DC8  lea     ecx, [ebp+var_34]
005C4DCB  push    ecx
005C4DCC  mov     ecx, [ebp+var_9C]
005C4DD2  call    sub_5B4100
005C4DD7  mov     edx, [eax]
005C4DD9  mov     eax, [eax+4]
005C4DDC  mov     [ebp+var_2C], edx
005C4DDF  mov     [ebp+var_28], eax
005C4DE2  mov     ecx, [ebp+var_18]
005C4DE5  push    ecx
005C4DE6  mov     edx, [ebp+var_1C]
005C4DE9  push    edx
005C4DEA  mov     eax, [ebp+var_28]
005C4DED  push    eax
005C4DEE  mov     ecx, [ebp+var_2C]
005C4DF1  push    ecx
005C4DF2  lea     edx, [ebp+var_3C]
005C4DF5  push    edx
005C4DF6  mov     ecx, [ebp+var_9C]
005C4DFC  call    sub_5D2E90
005C4E01  mov     [ebp+var_4], 0
005C4E08  jmp     short loc_5C4E13
005C4E0A  mov     eax, [ebp+var_4]
005C4E0D  add     eax, 1
005C4E10  mov     [ebp+var_4], eax
005C4E13  mov     ecx, [ebp+var_4]
005C4E16  cmp     ecx, [ebp+arg_4]
005C4E19  jge     short loc_5C4E69
005C4E1B  mov     edx, [ebp+var_4]
005C4E1E  imul    edx, 0Ch
005C4E21  mov     eax, [ebp+arg_0]
005C4E24  mov     ecx, [eax+edx+8]
005C4E28  push    ecx
005C4E29  mov     edx, [ebp+var_4]
005C4E2C  imul    edx, 0Ch
005C4E2F  mov     eax, [ebp+arg_0]
005C4E32  mov     ecx, [eax+edx+4]
005C4E36  push    ecx
005C4E37  mov     edx, [ebp+var_4]
005C4E3A  imul    edx, 0Ch
005C4E3D  mov     eax, [ebp+arg_0]
005C4E40  mov     ecx, [eax+edx]
005C4E43  push    ecx
005C4E44  lea     ecx, [ebp+var_14]
005C4E47  call    sub_55BBB0
005C4E4C  push    eax
005C4E4D  mov     edx, [ebp+var_4]
005C4E50  push    edx
005C4E51  mov     eax, [ebp+var_A0]
005C4E57  mov     edx, [eax]
005C4E59  mov     ecx, [ebp+var_A0]
005C4E5F  mov     eax, [edx+88h]
005C4E65  call    eax
005C4E67  jmp     short loc_5C4E0A
005C4E69  mov     ecx, [ebp+var_A0]
005C4E6F  mov     edx, [ecx]
005C4E71  mov     ecx, [ebp+var_A0]
005C4E77  mov     eax, [edx+44h]
005C4E7A  call    eax
005C4E7C  mov     esp, ebp
005C4E7E  pop     ebp
005C4E7F  retn    8
