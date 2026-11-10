00453480 sub     esp, 8
00453483 push    esi
00453484 mov     esi, ecx
00453486 cmp     byte ptr [esi+119h], 0
0045348D jnz     loc_4538AD
00453493 cmp     byte ptr [esi+68h], 0
00453497 push    ebx
00453498 jz      short loc_4534AA
0045349A cmp     byte ptr [esi+69h], 0
0045349E jz      short loc_4534AA
004534A0 cmp     byte ptr [esi+66h], 0
004534A4 jnz     short loc_4534AA
004534A6 mov     bl, 1
004534A8 jmp     short loc_4534AC
004534AA xor     bl, bl
004534AC xor     bl, [esi+118h]
004534B2 push    ebp
004534B3 mov     ebp, [esi+148h]
004534B9 add     ebp, [esi+140h]
004534BF push    edi
004534C0 mov     edi, [esi+144h]
004534C6 add     edi, [esi+13Ch]
004534CC cmp     dword ptr [esi+0C4h], 0
004534D3 mov     [esp+18h+var_8], edi
004534D7 mov     [esp+18h+var_4], ebp
004534DB jz      loc_453570
004534E1 mov     eax, [esi+0C0h]
004534E7 test    eax, eax
004534E9 jnz     short loc_45350F
004534EB mov     ecx, [esi+0C4h]
004534F1 mov     eax, [ecx]
004534F3 mov     eax, [eax+1Ch]
004534F6 lea     edx, [esi+0A4h]
004534FC push    edx
004534FD call    eax
004534FF mov     ecx, [esi+48h]
00453502 sub     ecx, eax
00453504 mov     eax, ecx
00453506 cdq
00453507 sub     eax, edx
00453509 sar     eax, 1
0045350B add     edi, eax
0045350D jmp     short loc_45352F
0045350F cmp     eax, 1
00453512 jnz     short loc_453533
00453514 mov     ecx, [esi+0C4h]
0045351A mov     edx, [ecx]
0045351C mov     edx, [edx+1Ch]
0045351F lea     eax, [esi+0A4h]
00453525 push    eax
00453526 call    edx
00453528 mov     ecx, [esi+48h]
0045352B sub     ecx, eax
0045352D add     edi, ecx
0045352F mov     [esp+18h+var_8], edi
00453533 mov     ecx, [esi+0C4h]
00453539 mov     edx, [ecx]
0045353B mov     eax, [edx+4]
0045353E call    eax
00453540 mov     edi, [esi+4Ch]
00453543 mov     ecx, eax
00453545 mov     eax, 2AAAAAABh
0045354A imul    ecx
0045354C mov     ecx, edx
0045354E shr     ecx, 1Fh
00453551 add     ecx, edx
00453553 sub     edi, ecx
00453555 mov     ecx, [esi+0C4h]
0045355B mov     edx, [ecx]
0045355D mov     eax, [edx+4]
00453560 dec     edi
00453561 call    eax
00453563 add     eax, edi
00453565 cdq
00453566 sub     eax, edx
00453568 sar     eax, 1
0045356A add     ebp, eax
0045356C mov     [esp+18h+var_4], ebp
00453570 mov     edi, [esp+18h+arg_0]
00453574 push    1
00453576 mov     ecx, edi
00453578 call    sub_592910
0045357D mov     ecx, [esi+7Ch]
00453580 sub     ecx, [esi+78h]
00453583 sar     ecx, 4
00453586 test    bl, bl
00453588 jnz     loc_45378A
0045358E cmp     ecx, 5
00453591 ja      short loc_453598
00453593 call    __invalid_parameter_noinfo
00453598 mov     edx, [esi+78h]
0045359B add     edx, 50h ; 'P'
0045359E push    edx
0045359F mov     ecx, edi
004535A1 call    sub_592880
004535A6 cmp     byte ptr [esi+66h], 0
004535AA jz      short loc_4535D7
004535AC mov     eax, [esi+0D4h]
004535B2 lea     ebx, [esi+108h]
004535B8 push    ebx
004535B9 push    eax
004535BA mov     ecx, esi
004535BC call    sub_5CE720
004535C1 test    al, al
004535C3 jz      short loc_4535D7
004535C5 mov     eax, [esi+160h]
004535CB push    eax
004535CC mov     eax, [esi+0D4h]
004535D2 jmp     loc_4536ED
004535D7 fldz
004535D9 fcomp   qword ptr [esi+120h]
004535DF fnstsw  ax
004535E1 test    ah, 5
004535E4 jp      loc_453693
004535EA mov     eax, [esi+0CCh]
004535F0 lea     ebp, [esi+0E8h]
004535F6 push    ebp
004535F7 push    eax
004535F8 mov     ecx, esi
004535FA call    sub_5CE720
004535FF test    al, al
00453601 jz      loc_45368F
00453607 mov     ecx, [esi+0C8h]
0045360D lea     ebx, [esi+0D8h]
00453613 push    ebx
00453614 push    ecx
00453615 mov     ecx, esi
00453617 call    sub_5CE720
0045361C test    al, al
0045361E jz      short loc_453652
00453620 fld1
00453622 fcomp   qword ptr [esi+120h]
00453628 fnstsw  ax
0045362A test    ah, 41h
0045362D jnz     short loc_453652
0045362F mov     eax, [esi+160h]
00453635 mov     ecx, [esi+15Ch]
0045363B mov     edx, [esi]
0045363D mov     edx, [edx+118h]
00453643 push    eax
00453644 mov     eax, [esi+0C8h]
0045364A push    ecx
0045364B push    ebx
0045364C push    eax
0045364D push    edi
0045364E mov     ecx, esi
00453650 call    edx
00453652 fld     qword ptr [esi+120h]
00453658 fmul    ds:dbl_727188
0045365E call    __ftol2_sse
00453663 mov     [edi+3Ch], eax
00453666 mov     ecx, [esi+160h]
0045366C mov     edx, [esi+15Ch]
00453672 mov     eax, [esi]
00453674 push    ecx
00453675 mov     ecx, [esi+0CCh]
0045367B push    edx
0045367C mov     edx, [eax+118h]
00453682 push    ebp
00453683 push    ecx
00453684 push    edi
00453685 mov     ecx, esi
00453687 call    edx
00453689 mov     ebp, [esp+18h+var_4]
0045368D jmp     short loc_453703
0045368F mov     ebp, [esp+18h+var_4]
00453693 cmp     byte ptr [esi+69h], 0
00453697 jnz     short loc_45369F
00453699 cmp     byte ptr [esi+68h], 0
0045369D jz      short loc_4536C7
0045369F mov     eax, [esi+0CCh]
004536A5 lea     ebx, [esi+0E8h]
004536AB push    ebx
004536AC push    eax
004536AD mov     ecx, esi
004536AF call    sub_5CE720
004536B4 test    al, al
004536B6 jz      short loc_4536C7
004536B8 mov     eax, [esi+160h]
004536BE push    eax
004536BF mov     eax, [esi+0CCh]
004536C5 jmp     short loc_4536ED
004536C7 mov     eax, [esi+0C8h]
004536CD lea     ebx, [esi+0D8h]
004536D3 push    ebx
004536D4 push    eax
004536D5 mov     ecx, esi
004536D7 call    sub_5CE720
004536DC test    al, al
004536DE jz      short loc_453703
004536E0 mov     eax, [esi+160h]
004536E6 push    eax
004536E7 mov     eax, [esi+0C8h]
004536ED mov     ecx, [esi+15Ch]
004536F3 mov     edx, [esi]
004536F5 mov     edx, [edx+118h]
004536FB push    ecx
004536FC push    ebx
004536FD push    eax
004536FE push    edi
004536FF mov     ecx, esi
00453701 call    edx
00453703 push    0
00453705 mov     ecx, edi
00453707 call    sub_592910
0045370C cmp     byte ptr [esi+69h], 0
00453710 mov     ecx, edi
00453712 jz      short loc_453756
00453714 mov     eax, [esi+150h]
0045371A test    eax, eax
0045371C jnz     short loc_453724
0045371E mov     eax, [esi+0C4h]
00453724 push    eax
00453725 call    sub_592860
0045372A mov     ecx, [esi+7Ch]
0045372D sub     ecx, [esi+78h]
00453730 sar     ecx, 4
00453733 cmp     ecx, 1
00453736 ja      short loc_45373D
00453738 call    __invalid_parameter_noinfo
0045373D mov     edx, [esi+78h]
00453740 add     edx, 10h
00453743 push    edx
00453744 mov     ecx, edi
00453746 call    sub_592880
0045374B mov     eax, [esp+18h+var_8]
0045374F push    ebp
00453750 push    eax
00453751 jmp     loc_45389C
00453756 mov     eax, [esi+0C4h]
0045375C push    eax
0045375D call    sub_592860
00453762 mov     ecx, [esi+7Ch]
00453765 sub     ecx, [esi+78h]
00453768 sar     ecx, 4
0045376B test    ecx, ecx
0045376D ja      short loc_453774
0045376F call    __invalid_parameter_noinfo
00453774 mov     edx, [esi+78h]
00453777 push    edx
00453778 mov     ecx, edi
0045377A call    sub_592880
0045377F mov     eax, [esp+18h+var_8]
00453783 push    ebp
00453784 push    eax
00453785 jmp     loc_45389C
0045378A cmp     ecx, 5
0045378D ja      short loc_453794
0045378F call    __invalid_parameter_noinfo
00453794 mov     edx, [esi+78h]
00453797 add     edx, 50h ; 'P'
0045379A push    edx
0045379B mov     ecx, edi
0045379D call    sub_592880
004537A2 mov     eax, [esi+0D0h]
004537A8 lea     ebx, [esi+0F8h]
004537AE push    ebx
004537AF push    eax
004537B0 mov     ecx, esi
004537B2 call    sub_5CE720
004537B7 test    al, al
004537B9 jz      short loc_4537D0
004537BB mov     eax, [esi+160h]
004537C1 add     eax, [esi+140h]
004537C7 push    eax
004537C8 mov     eax, [esi+0D0h]
004537CE jmp     short loc_45382A
004537D0 mov     eax, [esi+0CCh]
004537D6 lea     ebx, [esi+0E8h]
004537DC push    ebx
004537DD push    eax
004537DE mov     ecx, esi
004537E0 call    sub_5CE720
004537E5 test    al, al
004537E7 jz      short loc_4537FE
004537E9 mov     eax, [esi+160h]
004537EF add     eax, [esi+140h]
004537F5 push    eax
004537F6 mov     eax, [esi+0CCh]
004537FC jmp     short loc_45382A
004537FE mov     eax, [esi+0C8h]
00453804 lea     ebx, [esi+0D8h]
0045380A push    ebx
0045380B push    eax
0045380C mov     ecx, esi
0045380E call    sub_5CE720
00453813 test    al, al
00453815 jz      short loc_453846
00453817 mov     eax, [esi+160h]
0045381D add     eax, [esi+140h]
00453823 push    eax
00453824 mov     eax, [esi+0C8h]
0045382A mov     ecx, [esi+15Ch]
00453830 add     ecx, [esi+13Ch]
00453836 mov     edx, [esi]
00453838 mov     edx, [edx+118h]
0045383E push    ecx
0045383F push    ebx
00453840 push    eax
00453841 push    edi
00453842 mov     ecx, esi
00453844 call    edx
00453846 push    0
00453848 mov     ecx, edi
0045384A call    sub_592910
0045384F mov     eax, [esi+150h]
00453855 mov     ecx, edi
00453857 test    eax, eax
00453859 jnz     short loc_453861
0045385B mov     eax, [esi+0C4h]
00453861 push    eax
00453862 call    sub_592860
00453867 mov     ecx, [esi+7Ch]
0045386A sub     ecx, [esi+78h]
0045386D sar     ecx, 4
00453870 cmp     ecx, 1
00453873 ja      short loc_45387A
00453875 call    __invalid_parameter_noinfo
0045387A mov     edx, [esi+78h]
0045387D add     edx, 10h
00453880 push    edx
00453881 mov     ecx, edi
00453883 call    sub_592880
00453888 mov     eax, [esi+158h]
0045388E mov     ecx, [esi+154h]
00453894 add     eax, ebp
00453896 add     ecx, [esp+18h+var_8]
0045389A push    eax
0045389B push    ecx
0045389C add     esi, 0A4h
004538A2 push    esi
004538A3 mov     ecx, edi
004538A5 call    sub_592F40
004538AA pop     edi
004538AB pop     ebp
004538AC pop     ebx
004538AD pop     esi
004538AE add     esp, 8
004538B1 retn    4
