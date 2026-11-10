005CE7D0  push    ebp
005CE7D1  mov     ebp, esp
005CE7D3  push    0FFFFFFFFh
005CE7D5  push    offset SEH_5CE7D0
005CE7DA  mov     eax, large fs:0
005CE7E0  push    eax
005CE7E1  sub     esp, 68h
005CE7E4  push    esi
005CE7E5  push    edi
005CE7E6  mov     eax, ___security_cookie
005CE7EB  xor     eax, ebp
005CE7ED  push    eax
005CE7EE  lea     eax, [ebp+var_C]
005CE7F1  mov     large fs:0, eax
005CE7F7  mov     [ebp+var_6C], ecx
005CE7FA  mov     [ebp+var_50], 0
005CE801  mov     eax, [ebp+var_6C]
005CE804  movzx   ecx, byte ptr [eax+119h]
005CE80B  test    ecx, ecx
005CE80D  jz      short loc_5CE814
005CE80F  jmp     loc_5CF0C8
005CE814  mov     edx, [ebp+var_6C]
005CE817  cmp     dword ptr [edx+0C4h], 0
005CE81E  jnz     loc_5CE8C2
005CE824  mov     eax, [ebp+var_6C]
005CE827  add     eax, 0A4h
005CE82C  call    sub_43B340
005CE831  test    eax, eax
005CE833  jbe     loc_5CE8C2
005CE839  push    20h ; ' '; Size
005CE83B  call    ??2@YAPAXI@Z; operator new(uint)
005CE840  add     esp, 4
005CE843  mov     [ebp+var_20], eax
005CE846  mov     [ebp+var_4], 0
005CE84D  cmp     [ebp+var_20], 0
005CE851  jz      short loc_5CE88E
005CE853  push    offset aArialUnicodeMs; "Arial Unicode MS"
005CE858  lea     ecx, [ebp+var_3C]
005CE85B  call    sub_4033F0
005CE860  mov     eax, [ebp+var_50]
005CE863  or      eax, 1
005CE866  mov     [ebp+var_50], eax
005CE869  push    0; char
005CE86B  push    0; char
005CE86D  push    0; char
005CE86F  push    0; iCharSet
005CE871  push    0Ah; nNumber
005CE873  lea     ecx, [ebp+var_3C]
005CE876  push    ecx; int
005CE877  mov     edx, [ebp+var_6C]
005CE87A  mov     eax, [edx+20h]
005CE87D  mov     ecx, [eax+6Ch]
005CE880  push    ecx; int
005CE881  mov     ecx, [ebp+var_20]
005CE884  call    sub_5AF2A0
005CE889  mov     [ebp+var_70], eax
005CE88C  jmp     short loc_5CE895
005CE88E  mov     [ebp+var_70], 0
005CE895  mov     edx, [ebp+var_70]
005CE898  mov     [ebp+var_1C], edx
005CE89B  mov     [ebp+var_4], 0FFFFFFFFh
005CE8A2  mov     eax, [ebp+var_6C]
005CE8A5  mov     ecx, [ebp+var_1C]
005CE8A8  mov     [eax+0C4h], ecx
005CE8AE  mov     edx, [ebp+var_50]
005CE8B1  and     edx, 1
005CE8B4  jz      short loc_5CE8C2
005CE8B6  and     [ebp+var_50], 0FFFFFFFEh
005CE8BA  lea     ecx, [ebp+var_3C]; void *
005CE8BD  call    sub_401E90
005CE8C2  mov     eax, [ebp+var_6C]
005CE8C5  movzx   ecx, byte ptr [eax+68h]
005CE8C9  test    ecx, ecx
005CE8CB  jz      short loc_5CE8EC
005CE8CD  mov     edx, [ebp+var_6C]
005CE8D0  movzx   eax, byte ptr [edx+69h]
005CE8D4  test    eax, eax
005CE8D6  jz      short loc_5CE8EC
005CE8D8  mov     ecx, [ebp+var_6C]
005CE8DB  movzx   edx, byte ptr [ecx+66h]
005CE8DF  test    edx, edx
005CE8E1  jnz     short loc_5CE8EC
005CE8E3  mov     [ebp+var_74], 1
005CE8EA  jmp     short loc_5CE8F3
005CE8EC  mov     [ebp+var_74], 0
005CE8F3  mov     al, byte ptr [ebp+var_74]
005CE8F6  mov     [ebp+var_11], al
005CE8F9  movzx   ecx, [ebp+var_11]
005CE8FD  mov     edx, [ebp+var_6C]
005CE900  movzx   eax, byte ptr [edx+118h]
005CE907  xor     ecx, eax
005CE909  mov     [ebp+var_11], cl
005CE90C  mov     [ebp+var_10], 0
005CE913  mov     [ebp+var_18], 0
005CE91A  mov     ecx, [ebp+var_6C]
005CE91D  cmp     dword ptr [ecx+0C4h], 0
005CE924  jz      loc_5CE9F9
005CE92A  mov     edx, [ebp+var_6C]
005CE92D  cmp     dword ptr [edx+0C0h], 0
005CE934  jnz     short loc_5CE96C
005CE936  mov     eax, [ebp+var_6C]
005CE939  add     eax, 0A4h
005CE93E  push    eax
005CE93F  mov     ecx, [ebp+var_6C]
005CE942  mov     edx, [ecx+0C4h]
005CE948  mov     eax, [ebp+var_6C]
005CE94B  mov     ecx, [eax+0C4h]
005CE951  mov     edx, [edx]
005CE953  mov     eax, [edx+1Ch]
005CE956  call    eax
005CE958  mov     ecx, [ebp+var_6C]
005CE95B  mov     edx, [ecx+48h]
005CE95E  sub     edx, eax
005CE960  mov     eax, edx
005CE962  cdq
005CE963  sub     eax, edx
005CE965  sar     eax, 1
005CE967  mov     [ebp+var_10], eax
005CE96A  jmp     short loc_5CE9A6
005CE96C  mov     eax, [ebp+var_6C]
005CE96F  cmp     dword ptr [eax+0C0h], 1
005CE976  jnz     short loc_5CE9A6
005CE978  mov     ecx, [ebp+var_6C]
005CE97B  add     ecx, 0A4h
005CE981  push    ecx
005CE982  mov     edx, [ebp+var_6C]
005CE985  mov     eax, [edx+0C4h]
005CE98B  mov     ecx, [ebp+var_6C]
005CE98E  mov     ecx, [ecx+0C4h]
005CE994  mov     edx, [eax]
005CE996  mov     eax, [edx+1Ch]
005CE999  call    eax
005CE99B  mov     ecx, [ebp+var_6C]
005CE99E  mov     edx, [ecx+48h]
005CE9A1  sub     edx, eax
005CE9A3  mov     [ebp+var_10], edx
005CE9A6  mov     eax, [ebp+var_6C]
005CE9A9  mov     ecx, [eax+0C4h]
005CE9AF  mov     edx, [ebp+var_6C]
005CE9B2  mov     eax, [edx+0C4h]
005CE9B8  mov     edx, [ecx]
005CE9BA  mov     ecx, eax
005CE9BC  mov     eax, [edx+4]
005CE9BF  call    eax
005CE9C1  mov     ecx, [ebp+var_6C]
005CE9C4  mov     esi, [ecx+4Ch]
005CE9C7  add     esi, eax
005CE9C9  mov     edx, [ebp+var_6C]
005CE9CC  mov     eax, [edx+0C4h]
005CE9D2  mov     ecx, [ebp+var_6C]
005CE9D5  mov     ecx, [ecx+0C4h]
005CE9DB  mov     edx, [eax]
005CE9DD  mov     eax, [edx+4]
005CE9E0  call    eax
005CE9E2  cdq
005CE9E3  mov     ecx, 6
005CE9E8  idiv    ecx
005CE9EA  sub     esi, eax
005CE9EC  sub     esi, 1
005CE9EF  mov     eax, esi
005CE9F1  cdq
005CE9F2  sub     eax, edx
005CE9F4  sar     eax, 1
005CE9F6  mov     [ebp+var_18], eax
005CE9F9  mov     edx, [ebp+var_6C]
005CE9FC  mov     eax, [edx+0C4h]
005CEA02  push    eax
005CEA03  mov     ecx, [ebp+arg_0]
005CEA06  call    sub_592860
005CEA0B  mov     ecx, [ebp+var_6C]
005CEA0E  cmp     dword ptr [ecx+0C8h], 0
005CEA15  jnz     loc_5CED32
005CEA1B  mov     edx, [ebp+var_6C]
005CEA1E  cmp     dword ptr [edx+0D0h], 0
005CEA25  jnz     loc_5CED32
005CEA2B  mov     eax, [ebp+var_6C]
005CEA2E  movzx   ecx, byte ptr [eax+11Ah]
005CEA35  test    ecx, ecx
005CEA37  jnz     short loc_5CEA6C
005CEA39  mov     esi, [ebp+var_6C]
005CEA3C  add     esi, 6Ch ; 'l'
005CEA3F  mov     edi, 5
005CEA44  call    sub_43B0D0
005CEA49  push    eax
005CEA4A  mov     ecx, [ebp+arg_0]
005CEA4D  call    sub_592880
005CEA52  mov     edx, [ebp+var_6C]
005CEA55  mov     eax, [edx+4Ch]
005CEA58  push    eax
005CEA59  mov     ecx, [ebp+var_6C]
005CEA5C  mov     edx, [ecx+48h]
005CEA5F  push    edx
005CEA60  push    0
005CEA62  push    0
005CEA64  mov     ecx, [ebp+arg_0]
005CEA67  call    sub_592990
005CEA6C  movzx   eax, [ebp+var_11]
005CEA70  test    eax, eax
005CEA72  jz      loc_5CEBD0
005CEA78  mov     ecx, [ebp+var_6C]
005CEA7B  movzx   edx, byte ptr [ecx+11Ah]
005CEA82  test    edx, edx
005CEA84  jnz     loc_5CEB6F
005CEA8A  mov     esi, [ebp+var_6C]
005CEA8D  add     esi, 6Ch ; 'l'
005CEA90  mov     edi, 2
005CEA95  call    sub_43B0D0
005CEA9A  push    eax
005CEA9B  mov     ecx, [ebp+arg_0]
005CEA9E  call    sub_592880
005CEAA3  push    1
005CEAA5  mov     eax, [ebp+var_6C]
005CEAA8  mov     ecx, [eax+48h]
005CEAAB  sub     ecx, 1
005CEAAE  push    ecx
005CEAAF  push    0
005CEAB1  push    0
005CEAB3  mov     ecx, [ebp+arg_0]
005CEAB6  call    sub_592990
005CEABB  mov     edx, [ebp+var_6C]
005CEABE  mov     eax, [edx+4Ch]
005CEAC1  sub     eax, 1
005CEAC4  push    eax
005CEAC5  push    1
005CEAC7  push    0
005CEAC9  push    0
005CEACB  mov     ecx, [ebp+arg_0]
005CEACE  call    sub_592990
005CEAD3  mov     esi, [ebp+var_6C]
005CEAD6  add     esi, 6Ch ; 'l'
005CEAD9  mov     edi, 3
005CEADE  call    sub_43B0D0
005CEAE3  push    eax
005CEAE4  mov     ecx, [ebp+arg_0]
005CEAE7  call    sub_592880
005CEAEC  push    1
005CEAEE  mov     ecx, [ebp+var_6C]
005CEAF1  mov     edx, [ecx+48h]
005CEAF4  push    edx
005CEAF5  mov     eax, [ebp+var_6C]
005CEAF8  mov     ecx, [eax+4Ch]
005CEAFB  sub     ecx, 1
005CEAFE  push    ecx
005CEAFF  push    0
005CEB01  mov     ecx, [ebp+arg_0]
005CEB04  call    sub_592990
005CEB09  mov     edx, [ebp+var_6C]
005CEB0C  mov     eax, [edx+4Ch]
005CEB0F  push    eax
005CEB10  push    1
005CEB12  push    0
005CEB14  mov     ecx, [ebp+var_6C]
005CEB17  mov     edx, [ecx+48h]
005CEB1A  sub     edx, 1
005CEB1D  push    edx
005CEB1E  mov     ecx, [ebp+arg_0]
005CEB21  call    sub_592990
005CEB26  mov     esi, [ebp+var_6C]
005CEB29  add     esi, 6Ch ; 'l'
005CEB2C  mov     edi, 4
005CEB31  call    sub_43B0D0
005CEB36  push    eax
005CEB37  mov     ecx, [ebp+arg_0]
005CEB3A  call    sub_592880
005CEB3F  push    1
005CEB41  mov     eax, [ebp+var_6C]
005CEB44  mov     ecx, [eax+48h]
005CEB47  sub     ecx, 3
005CEB4A  push    ecx
005CEB4B  push    1
005CEB4D  push    1
005CEB4F  mov     ecx, [ebp+arg_0]
005CEB52  call    sub_592990
005CEB57  mov     edx, [ebp+var_6C]
005CEB5A  mov     eax, [edx+4Ch]
005CEB5D  sub     eax, 3
005CEB60  push    eax
005CEB61  push    1
005CEB63  push    1
005CEB65  push    1
005CEB67  mov     ecx, [ebp+arg_0]
005CEB6A  call    sub_592990
005CEB6F  mov     ecx, [ebp+var_6C]
005CEB72  movzx   edx, byte ptr [ecx+69h]
005CEB76  test    edx, edx
005CEB78  jz      short loc_5CEB95
005CEB7A  mov     esi, [ebp+var_6C]
005CEB7D  add     esi, 6Ch ; 'l'
005CEB80  mov     edi, 1
005CEB85  call    sub_43B0D0
005CEB8A  push    eax
005CEB8B  mov     ecx, [ebp+arg_0]
005CEB8E  call    sub_592880
005CEB93  jmp     short loc_5CEBAB
005CEB95  mov     esi, [ebp+var_6C]
005CEB98  add     esi, 6Ch ; 'l'
005CEB9B  xor     edi, edi
005CEB9D  call    sub_43B0D0
005CEBA2  push    eax
005CEBA3  mov     ecx, [ebp+arg_0]
005CEBA6  call    sub_592880
005CEBAB  mov     eax, [ebp+var_18]
005CEBAE  add     eax, 1
005CEBB1  push    eax
005CEBB2  mov     ecx, [ebp+var_10]
005CEBB5  add     ecx, 1
005CEBB8  push    ecx
005CEBB9  mov     edx, [ebp+var_6C]
005CEBBC  add     edx, 0A4h
005CEBC2  push    edx
005CEBC3  mov     ecx, [ebp+arg_0]
005CEBC6  call    sub_592F40
005CEBCB  jmp     loc_5CED2D
005CEBD0  mov     eax, [ebp+var_6C]
005CEBD3  movzx   ecx, byte ptr [eax+11Ah]
005CEBDA  test    ecx, ecx
005CEBDC  jnz     loc_5CECD7
005CEBE2  mov     esi, [ebp+var_6C]
005CEBE5  add     esi, 6Ch ; 'l'
005CEBE8  mov     edi, 3
005CEBED  call    sub_43B0D0
005CEBF2  push    eax
005CEBF3  mov     ecx, [ebp+arg_0]
005CEBF6  call    sub_592880
005CEBFB  push    1
005CEBFD  mov     edx, [ebp+var_6C]
005CEC00  mov     eax, [edx+48h]
005CEC03  sub     eax, 1
005CEC06  push    eax
005CEC07  push    0
005CEC09  push    0
005CEC0B  mov     ecx, [ebp+arg_0]
005CEC0E  call    sub_592990
005CEC13  mov     ecx, [ebp+var_6C]
005CEC16  mov     edx, [ecx+4Ch]
005CEC19  sub     edx, 1
005CEC1C  push    edx
005CEC1D  push    1
005CEC1F  push    0
005CEC21  push    0
005CEC23  mov     ecx, [ebp+arg_0]
005CEC26  call    sub_592990
005CEC2B  mov     esi, [ebp+var_6C]
005CEC2E  add     esi, 6Ch ; 'l'
005CEC31  mov     edi, 2
005CEC36  call    sub_43B0D0
005CEC3B  push    eax
005CEC3C  mov     ecx, [ebp+arg_0]
005CEC3F  call    sub_592880
005CEC44  push    1
005CEC46  mov     eax, [ebp+var_6C]
005CEC49  mov     ecx, [eax+48h]
005CEC4C  push    ecx
005CEC4D  mov     edx, [ebp+var_6C]
005CEC50  mov     eax, [edx+4Ch]
005CEC53  sub     eax, 1
005CEC56  push    eax
005CEC57  push    0
005CEC59  mov     ecx, [ebp+arg_0]
005CEC5C  call    sub_592990
005CEC61  mov     ecx, [ebp+var_6C]
005CEC64  mov     edx, [ecx+4Ch]
005CEC67  push    edx
005CEC68  push    1
005CEC6A  push    0
005CEC6C  mov     eax, [ebp+var_6C]
005CEC6F  mov     ecx, [eax+48h]
005CEC72  sub     ecx, 1
005CEC75  push    ecx
005CEC76  mov     ecx, [ebp+arg_0]
005CEC79  call    sub_592990
005CEC7E  mov     esi, [ebp+var_6C]
005CEC81  add     esi, 6Ch ; 'l'
005CEC84  mov     edi, 4
005CEC89  call    sub_43B0D0
005CEC8E  push    eax
005CEC8F  mov     ecx, [ebp+arg_0]
005CEC92  call    sub_592880
005CEC97  push    1
005CEC99  mov     edx, [ebp+var_6C]
005CEC9C  mov     eax, [edx+48h]
005CEC9F  sub     eax, 2
005CECA2  push    eax
005CECA3  mov     ecx, [ebp+var_6C]
005CECA6  mov     edx, [ecx+4Ch]
005CECA9  sub     edx, 2
005CECAC  push    edx
005CECAD  push    1
005CECAF  mov     ecx, [ebp+arg_0]
005CECB2  call    sub_592990
005CECB7  mov     eax, [ebp+var_6C]
005CECBA  mov     ecx, [eax+4Ch]
005CECBD  sub     ecx, 2
005CECC0  push    ecx
005CECC1  push    1
005CECC3  push    1
005CECC5  mov     edx, [ebp+var_6C]
005CECC8  mov     eax, [edx+48h]
005CECCB  sub     eax, 2
005CECCE  push    eax
005CECCF  mov     ecx, [ebp+arg_0]
005CECD2  call    sub_592990
005CECD7  mov     ecx, [ebp+var_6C]
005CECDA  movzx   edx, byte ptr [ecx+69h]
005CECDE  test    edx, edx
005CECE0  jz      short loc_5CECFD
005CECE2  mov     esi, [ebp+var_6C]
005CECE5  add     esi, 6Ch ; 'l'
005CECE8  mov     edi, 1
005CECED  call    sub_43B0D0
005CECF2  push    eax
005CECF3  mov     ecx, [ebp+arg_0]
005CECF6  call    sub_592880
005CECFB  jmp     short loc_5CED13
005CECFD  mov     esi, [ebp+var_6C]
005CED00  add     esi, 6Ch ; 'l'
005CED03  xor     edi, edi
005CED05  call    sub_43B0D0
005CED0A  push    eax
005CED0B  mov     ecx, [ebp+arg_0]
005CED0E  call    sub_592880
005CED13  mov     eax, [ebp+var_18]
005CED16  push    eax
005CED17  mov     ecx, [ebp+var_10]
005CED1A  push    ecx
005CED1B  mov     edx, [ebp+var_6C]
005CED1E  add     edx, 0A4h
005CED24  push    edx
005CED25  mov     ecx, [ebp+arg_0]
005CED28  call    sub_592F40
005CED2D  jmp     loc_5CF0C8
005CED32  movzx   eax, [ebp+var_11]
005CED36  test    eax, eax
005CED38  jnz     loc_5CEFC5
005CED3E  mov     ecx, [ebp+var_6C]
005CED41  movzx   edx, byte ptr [ecx+66h]
005CED45  test    edx, edx
005CED47  jz      short loc_5CED9C
005CED49  mov     eax, [ebp+var_6C]
005CED4C  add     eax, 108h
005CED51  push    eax
005CED52  mov     ecx, [ebp+var_6C]
005CED55  mov     edx, [ecx+0D4h]
005CED5B  push    edx
005CED5C  mov     ecx, [ebp+var_6C]
005CED5F  call    sub_5CE720
005CED64  movzx   eax, al
005CED67  test    eax, eax
005CED69  jz      short loc_5CED9C
005CED6B  push    0
005CED6D  push    0
005CED6F  mov     ecx, [ebp+var_6C]
005CED72  add     ecx, 108h
005CED78  push    ecx
005CED79  mov     edx, [ebp+var_6C]
005CED7C  mov     eax, [edx+0D4h]
005CED82  push    eax
005CED83  mov     ecx, [ebp+arg_0]
005CED86  push    ecx
005CED87  mov     edx, [ebp+var_6C]
005CED8A  mov     eax, [edx]
005CED8C  mov     ecx, [ebp+var_6C]
005CED8F  mov     edx, [eax+118h]
005CED95  call    edx
005CED97  jmp     loc_5CEF6A
005CED9C  mov     eax, [ebp+var_6C]
005CED9F  fldz
005CEDA1  fcomp   qword ptr [eax+120h]
005CEDA7  fnstsw  ax
005CEDA9  test    ah, 5
005CEDAC  jp      loc_5CEEB4
005CEDB2  mov     ecx, [ebp+var_6C]
005CEDB5  add     ecx, 0E8h
005CEDBB  push    ecx
005CEDBC  mov     edx, [ebp+var_6C]
005CEDBF  mov     eax, [edx+0CCh]
005CEDC5  push    eax
005CEDC6  mov     ecx, [ebp+var_6C]
005CEDC9  call    sub_5CE720
005CEDCE  movzx   ecx, al
005CEDD1  test    ecx, ecx
005CEDD3  jz      loc_5CEEB4
005CEDD9  mov     edx, [ebp+var_6C]
005CEDDC  add     edx, 0D8h
005CEDE2  push    edx
005CEDE3  mov     eax, [ebp+var_6C]
005CEDE6  mov     ecx, [eax+0C8h]
005CEDEC  push    ecx
005CEDED  mov     ecx, [ebp+var_6C]
005CEDF0  call    sub_5CE720
005CEDF5  movzx   edx, al
005CEDF8  test    edx, edx
005CEDFA  jz      short loc_5CEE3A
005CEDFC  mov     eax, [ebp+var_6C]
005CEDFF  fld1
005CEE01  fcomp   qword ptr [eax+120h]
005CEE07  fnstsw  ax
005CEE09  test    ah, 41h
005CEE0C  jnz     short loc_5CEE3A
005CEE0E  push    0
005CEE10  push    0
005CEE12  mov     ecx, [ebp+var_6C]
005CEE15  add     ecx, 0D8h
005CEE1B  push    ecx
005CEE1C  mov     edx, [ebp+var_6C]
005CEE1F  mov     eax, [edx+0C8h]
005CEE25  push    eax
005CEE26  mov     ecx, [ebp+arg_0]
005CEE29  push    ecx
005CEE2A  mov     edx, [ebp+var_6C]
005CEE2D  mov     eax, [edx]
005CEE2F  mov     ecx, [ebp+var_6C]
005CEE32  mov     edx, [eax+118h]
005CEE38  call    edx
005CEE3A  push    1
005CEE3C  mov     ecx, [ebp+arg_0]
005CEE3F  call    sub_592910
005CEE44  mov     eax, [ebp+var_6C]
005CEE47  fld     qword ptr [eax+120h]
005CEE4D  fmul    ds:dbl_727188
005CEE53  call    __ftol2_sse
005CEE58  push    eax
005CEE59  push    0FFh
005CEE5E  push    0FFh
005CEE63  push    0FFh
005CEE68  lea     ecx, [ebp+var_4C]
005CEE6B  call    sub_5CA5A0
005CEE70  push    eax
005CEE71  mov     ecx, [ebp+arg_0]
005CEE74  call    sub_592880
005CEE79  push    0
005CEE7B  push    0
005CEE7D  mov     ecx, [ebp+var_6C]
005CEE80  add     ecx, 0E8h
005CEE86  push    ecx
005CEE87  mov     edx, [ebp+var_6C]
005CEE8A  mov     eax, [edx+0CCh]
005CEE90  push    eax
005CEE91  mov     ecx, [ebp+arg_0]
005CEE94  push    ecx
005CEE95  mov     edx, [ebp+var_6C]
005CEE98  mov     eax, [edx]
005CEE9A  mov     ecx, [ebp+var_6C]
005CEE9D  mov     edx, [eax+118h]
005CEEA3  call    edx
005CEEA5  push    0
005CEEA7  mov     ecx, [ebp+arg_0]
005CEEAA  call    sub_592910
005CEEAF  jmp     loc_5CEF6A
005CEEB4  mov     eax, [ebp+var_6C]
005CEEB7  movzx   ecx, byte ptr [eax+69h]
005CEEBB  test    ecx, ecx
005CEEBD  jnz     short loc_5CEECA
005CEEBF  mov     edx, [ebp+var_6C]
005CEEC2  movzx   eax, byte ptr [edx+68h]
005CEEC6  test    eax, eax
005CEEC8  jz      short loc_5CEF1B
005CEECA  mov     ecx, [ebp+var_6C]
005CEECD  add     ecx, 0E8h
005CEED3  push    ecx
005CEED4  mov     edx, [ebp+var_6C]
005CEED7  mov     eax, [edx+0CCh]
005CEEDD  push    eax
005CEEDE  mov     ecx, [ebp+var_6C]
005CEEE1  call    sub_5CE720
005CEEE6  movzx   ecx, al
005CEEE9  test    ecx, ecx
005CEEEB  jz      short loc_5CEF1B
005CEEED  push    0
005CEEEF  push    0
005CEEF1  mov     edx, [ebp+var_6C]
005CEEF4  add     edx, 0E8h
005CEEFA  push    edx
005CEEFB  mov     eax, [ebp+var_6C]
005CEEFE  mov     ecx, [eax+0CCh]
005CEF04  push    ecx
005CEF05  mov     edx, [ebp+arg_0]
005CEF08  push    edx
005CEF09  mov     eax, [ebp+var_6C]
005CEF0C  mov     edx, [eax]
005CEF0E  mov     ecx, [ebp+var_6C]
005CEF11  mov     eax, [edx+118h]
005CEF17  call    eax
005CEF19  jmp     short loc_5CEF6A
005CEF1B  mov     ecx, [ebp+var_6C]
005CEF1E  add     ecx, 0D8h
005CEF24  push    ecx
005CEF25  mov     edx, [ebp+var_6C]
005CEF28  mov     eax, [edx+0C8h]
005CEF2E  push    eax
005CEF2F  mov     ecx, [ebp+var_6C]
005CEF32  call    sub_5CE720
005CEF37  movzx   ecx, al
005CEF3A  test    ecx, ecx
005CEF3C  jz      short loc_5CEF6A
005CEF3E  push    0
005CEF40  push    0
005CEF42  mov     edx, [ebp+var_6C]
005CEF45  add     edx, 0D8h
005CEF4B  push    edx
005CEF4C  mov     eax, [ebp+var_6C]
005CEF4F  mov     ecx, [eax+0C8h]
005CEF55  push    ecx
005CEF56  mov     edx, [ebp+arg_0]
005CEF59  push    edx
005CEF5A  mov     eax, [ebp+var_6C]
005CEF5D  mov     edx, [eax]
005CEF5F  mov     ecx, [ebp+var_6C]
005CEF62  mov     eax, [edx+118h]
005CEF68  call    eax
005CEF6A  mov     ecx, [ebp+var_6C]
005CEF6D  movzx   edx, byte ptr [ecx+69h]
005CEF71  test    edx, edx
005CEF73  jz      short loc_5CEF90
005CEF75  mov     esi, [ebp+var_6C]
005CEF78  add     esi, 6Ch ; 'l'
005CEF7B  mov     edi, 1
005CEF80  call    sub_43B0D0
005CEF85  push    eax
005CEF86  mov     ecx, [ebp+arg_0]
005CEF89  call    sub_592880
005CEF8E  jmp     short loc_5CEFA6
005CEF90  mov     esi, [ebp+var_6C]
005CEF93  add     esi, 6Ch ; 'l'
005CEF96  xor     edi, edi
005CEF98  call    sub_43B0D0
005CEF9D  push    eax
005CEF9E  mov     ecx, [ebp+arg_0]
005CEFA1  call    sub_592880
005CEFA6  mov     eax, [ebp+var_18]
005CEFA9  push    eax
005CEFAA  mov     ecx, [ebp+var_10]
005CEFAD  push    ecx
005CEFAE  mov     edx, [ebp+var_6C]
005CEFB1  add     edx, 0A4h
005CEFB7  push    edx
005CEFB8  mov     ecx, [ebp+arg_0]
005CEFBB  call    sub_592F40
005CEFC0  jmp     loc_5CF0C8
005CEFC5  mov     eax, [ebp+var_6C]
005CEFC8  add     eax, 0F8h
005CEFCD  push    eax
005CEFCE  mov     ecx, [ebp+var_6C]
005CEFD1  mov     edx, [ecx+0D0h]
005CEFD7  push    edx
005CEFD8  mov     ecx, [ebp+var_6C]
005CEFDB  call    sub_5CE720
005CEFE0  movzx   eax, al
005CEFE3  test    eax, eax
005CEFE5  jz      short loc_5CF015
005CEFE7  push    0
005CEFE9  push    0
005CEFEB  mov     ecx, [ebp+var_6C]
005CEFEE  add     ecx, 0F8h
005CEFF4  push    ecx
005CEFF5  mov     edx, [ebp+var_6C]
005CEFF8  mov     eax, [edx+0D0h]
005CEFFE  push    eax
005CEFFF  mov     ecx, [ebp+arg_0]
005CF002  push    ecx
005CF003  mov     edx, [ebp+var_6C]
005CF006  mov     eax, [edx]
005CF008  mov     ecx, [ebp+var_6C]
005CF00B  mov     edx, [eax+118h]
005CF011  call    edx
005CF013  jmp     short loc_5CF090
005CF015  mov     eax, [ebp+var_6C]
005CF018  add     eax, 0E8h
005CF01D  push    eax
005CF01E  mov     ecx, [ebp+var_6C]
005CF021  mov     edx, [ecx+0CCh]
005CF027  push    edx
005CF028  mov     ecx, [ebp+var_6C]
005CF02B  call    sub_5CE720
005CF030  movzx   eax, al
005CF033  test    eax, eax
005CF035  jz      short loc_5CF065
005CF037  push    1
005CF039  push    1
005CF03B  mov     ecx, [ebp+var_6C]
005CF03E  add     ecx, 0E8h
005CF044  push    ecx
005CF045  mov     edx, [ebp+var_6C]
005CF048  mov     eax, [edx+0CCh]
005CF04E  push    eax
005CF04F  mov     ecx, [ebp+arg_0]
005CF052  push    ecx
005CF053  mov     edx, [ebp+var_6C]
005CF056  mov     eax, [edx]
005CF058  mov     ecx, [ebp+var_6C]
005CF05B  mov     edx, [eax+118h]
005CF061  call    edx
005CF063  jmp     short loc_5CF090
005CF065  push    1
005CF067  push    1
005CF069  mov     eax, [ebp+var_6C]
005CF06C  add     eax, 0D8h
005CF071  push    eax
005CF072  mov     ecx, [ebp+var_6C]
005CF075  mov     edx, [ecx+0C8h]
005CF07B  push    edx
005CF07C  mov     eax, [ebp+arg_0]
005CF07F  push    eax
005CF080  mov     ecx, [ebp+var_6C]
005CF083  mov     edx, [ecx]
005CF085  mov     ecx, [ebp+var_6C]
005CF088  mov     eax, [edx+118h]
005CF08E  call    eax
005CF090  mov     esi, [ebp+var_6C]
005CF093  add     esi, 6Ch ; 'l'
005CF096  mov     edi, 1
005CF09B  call    sub_43B0D0
005CF0A0  push    eax
005CF0A1  mov     ecx, [ebp+arg_0]
005CF0A4  call    sub_592880
005CF0A9  mov     ecx, [ebp+var_18]
005CF0AC  add     ecx, 1
005CF0AF  push    ecx
005CF0B0  mov     edx, [ebp+var_10]
005CF0B3  add     edx, 1
005CF0B6  push    edx
005CF0B7  mov     eax, [ebp+var_6C]
005CF0BA  add     eax, 0A4h
005CF0BF  push    eax
005CF0C0  mov     ecx, [ebp+arg_0]
005CF0C3  call    sub_592F40
005CF0C8  mov     ecx, [ebp+var_C]
005CF0CB  mov     large fs:0, ecx
005CF0D2  pop     ecx
005CF0D3  pop     edi
005CF0D4  pop     esi
005CF0D5  mov     esp, ebp
005CF0D7  pop     ebp
005CF0D8  retn    4
006E10B0  mov     eax, [ebp+var_20]
006E10B3  push    eax; void *
006E10B4  call    ??3@YAXPAX@Z; operator delete(void *)
006E10B9  pop     ecx
006E10BA  retn
006E10BB  mov     edx, [esp-4+arg_4]
006E10BF  lea     eax, [edx+0Ch]
006E10C2  mov     ecx, [edx-74h]
006E10C5  xor     ecx, eax; StackCookie
006E10C7  call    @__security_check_cookie@4; __security_check_cookie(x)
006E10CC  mov     eax, offset stru_7335F0
006E10D1  jmp     ___CxxFrameHandler3
