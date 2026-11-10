005CF450  push    ebp
005CF451  mov     ebp, esp
005CF453  push    ecx
005CF454  mov     [ebp+var_4], ecx
005CF457  mov     ecx, [ebp+var_4]
005CF45A  call    sub_5C5350
005CF45F  mov     eax, [ebp+var_4]
005CF462  movzx   ecx, byte ptr [eax+68h]
005CF466  test    ecx, ecx
005CF468  jz      short loc_5CF49A
005CF46A  mov     edx, [ebp+var_4]
005CF46D  movzx   eax, byte ptr [edx+69h]
005CF471  test    eax, eax
005CF473  jz      short loc_5CF49A
005CF475  mov     ecx, [ebp+var_4]
005CF478  mov     edx, [ecx+0A0h]
005CF47E  push    edx
005CF47F  mov     eax, [ebp+var_4]
005CF482  mov     ecx, [eax+11Ch]
005CF488  mov     edx, [ebp+var_4]
005CF48B  mov     eax, [edx+11Ch]
005CF491  mov     edx, [ecx]
005CF493  mov     ecx, eax
005CF495  mov     eax, [edx+0Ch]
005CF498  call    eax
005CF49A  mov     ecx, [ebp+var_4]
005CF49D  movzx   edx, byte ptr [ecx+68h]
005CF4A1  test    edx, edx
005CF4A3  jnz     loc_5CF52C
005CF4A9  mov     eax, [ebp+var_4]
005CF4AC  movzx   ecx, byte ptr [eax+69h]
005CF4B0  test    ecx, ecx
005CF4B2  jnz     short loc_5CF52C
005CF4B4  mov     edx, [ebp+var_4]
005CF4B7  fldz
005CF4B9  fcomp   qword ptr [edx+120h]
005CF4BF  fnstsw  ax
005CF4C1  test    ah, 5
005CF4C4  jp      short loc_5CF52C
005CF4C6  mov     eax, [ebp+var_4]
005CF4C9  fldz
005CF4CB  fcomp   qword ptr [eax+128h]
005CF4D1  fnstsw  ax
005CF4D3  test    ah, 5
005CF4D6  jp      short loc_5CF512
005CF4D8  mov     ecx, [ebp+var_4]
005CF4DB  mov     edx, [ebp+var_4]
005CF4DE  fld     qword ptr [ecx+120h]
005CF4E4  fsub    qword ptr [edx+128h]
005CF4EA  mov     eax, [ebp+var_4]
005CF4ED  fstp    qword ptr [eax+120h]
005CF4F3  mov     ecx, [ebp+var_4]
005CF4F6  fldz
005CF4F8  fcomp   qword ptr [ecx+120h]
005CF4FE  fnstsw  ax
005CF500  test    ah, 41h
005CF503  jnz     short loc_5CF510
005CF505  mov     edx, [ebp+var_4]
005CF508  fldz
005CF50A  fstp    qword ptr [edx+120h]
005CF510  jmp     short loc_5CF51D
005CF512  mov     eax, [ebp+var_4]
005CF515  fldz
005CF517  fstp    qword ptr [eax+120h]
005CF51D  mov     ecx, [ebp+var_4]
005CF520  mov     edx, [ecx]
005CF522  mov     ecx, [ebp+var_4]
005CF525  mov     eax, [edx+44h]
005CF528  call    eax
005CF52A  jmp     short loc_5CF5A0
005CF52C  mov     ecx, [ebp+var_4]
005CF52F  movzx   edx, byte ptr [ecx+69h]
005CF533  test    edx, edx
005CF535  jz      short loc_5CF5A0
005CF537  mov     eax, [ebp+var_4]
005CF53A  fldz
005CF53C  fcomp   qword ptr [eax+130h]
005CF542  fnstsw  ax
005CF544  test    ah, 5
005CF547  jp      short loc_5CF5A0
005CF549  mov     ecx, [ebp+var_4]
005CF54C  fld1
005CF54E  fcomp   qword ptr [ecx+120h]
005CF554  fnstsw  ax
005CF556  test    ah, 41h
005CF559  jnz     short loc_5CF5A0
005CF55B  mov     edx, [ebp+var_4]
005CF55E  mov     eax, [ebp+var_4]
005CF561  fld     qword ptr [edx+120h]
005CF567  fadd    qword ptr [eax+130h]
005CF56D  mov     ecx, [ebp+var_4]
005CF570  fstp    qword ptr [ecx+120h]
005CF576  mov     edx, [ebp+var_4]
005CF579  fld1
005CF57B  fcomp   qword ptr [edx+120h]
005CF581  fnstsw  ax
005CF583  test    ah, 5
005CF586  jp      short loc_5CF593
005CF588  mov     eax, [ebp+var_4]
005CF58B  fld1
005CF58D  fstp    qword ptr [eax+120h]
005CF593  mov     ecx, [ebp+var_4]
005CF596  mov     edx, [ecx]
005CF598  mov     ecx, [ebp+var_4]
005CF59B  mov     eax, [edx+44h]
005CF59E  call    eax
005CF5A0  mov     esp, ebp
005CF5A2  pop     ebp
005CF5A3  retn
