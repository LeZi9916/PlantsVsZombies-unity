0040FE70  push    0FFFFFFFFh
0040FE72  mov     eax, large fs:0
0040FE78  push    offset loc_6EDD30
0040FE7D  push    eax
0040FE7E  mov     large fs:0, esp
0040FE85  sub     esp, 54h
0040FE88  push    ebx
0040FE89  push    ebp
0040FE8A  push    esi
0040FE8B  push    edi
0040FE8C  mov     edi, ecx
0040FE8E  mov     eax, [edi+0B0h]
0040FE94  mov     ecx, [eax+950h]
0040FE9A  mov     eax, [ecx+4Ch]
0040FE9D  add     eax, 0FFFFFFFEh
0040FEA0  cdq
0040FEA1  mov     ecx, 0Ah
0040FEA6  idiv    ecx
0040FEA8  lea     ecx, [edx+1]
0040FEAB  cmp     ecx, 0Ah
0040FEAE  lea     esi, [ecx+eax*8]
0040FEB1  jl      short loc_40FEB8
0040FEB3  sub     esi, 2
0040FEB6  jmp     short loc_40FEBE
0040FEB8  cmp     ecx, 5
0040FEBB  jl      short loc_40FEBE
0040FEBD  dec     esi
0040FEBE  cmp     esi, 28h ; '('
0040FEC1  jle     short loc_40FEC8
0040FEC3  mov     esi, 28h ; '('
0040FEC8  lea     edx, [esp+70h+var_28]
0040FECC  push    edx
0040FECD  or      ecx, 0FFFFFFFFh
0040FED0  mov     edx, esi
0040FED2  call    sub_4760E0
0040FED7  add     esp, 4
0040FEDA  xor     ebx, ebx
0040FEDC  mov     [esp+70h+var_4], ebx
0040FEE0  xor     eax, eax
0040FEE2  mov     [esp+70h+var_48], 7
0040FEEA  mov     [esp+70h+var_4C], ebx
0040FEEE  mov     word ptr [esp+70h+var_5C], ax
0040FEF3  mov     byte ptr [esp+70h+var_4], 1
0040FEF8  mov     ebp, [edi+0B0h]
0040FEFE  cmp     [ebp+9E8h], bl
0040FF04  jnz     short loc_40FF22
0040FF06  mov     ecx, [ebp+99Ch]
0040FF0C  cmp     ecx, ebx
0040FF0E  jz      short loc_40FF19
0040FF10  call    sub_651F30
0040FF15  cmp     al, bl
0040FF17  jnz     short loc_40FF43
0040FF19  cmp     dword ptr [ebp+9E4h], 1
0040FF20  jnz     short loc_40FF43
0040FF22  cmp     esi, 11h
0040FF25  jl      short loc_40FF43
0040FF27  cmp     esi, 13h
0040FF2A  jz      short loc_40FF43
0040FF2C  push    1Bh; int
0040FF2E  push    offset off_708BAC; Source
0040FF33  lea     ecx, [esp+78h+var_60]
0040FF37  call    sub_40D1E0
0040FF3C  mov     ebp, 8
0040FF41  jmp     short loc_40FF7F
0040FF43  push    esi
0040FF44  lea     ecx, [esp+74h+var_44]
0040FF48  call    sub_4763C0
0040FF4D  add     esp, 4
0040FF50  push    0FFFFFFFFh
0040FF52  push    ebx
0040FF53  push    eax
0040FF54  lea     ecx, [esp+7Ch+var_60]
0040FF58  mov     byte ptr [esp+7Ch+var_4], 2
0040FF5D  call    sub_40D0E0
0040FF62  mov     ebp, 8
0040FF67  mov     byte ptr [esp+70h+var_4], 1
0040FF6C  cmp     [esp+70h+var_2C], ebp
0040FF70  jb      short loc_40FF7F
0040FF72  mov     ecx, [esp+70h+var_40]
0040FF76  push    ecx; void *
0040FF77  call    ??3@YAXPAX@Z; operator delete(void *)
0040FF7C  add     esp, 4
0040FF7F  push    0Bh; int
0040FF81  xor     edx, edx
0040FF83  push    offset off_708BE4; Source
0040FF88  lea     ecx, [esp+78h+var_44]
0040FF8C  mov     [esp+78h+var_2C], 7
0040FF94  mov     [esp+78h+var_30], ebx
0040FF98  mov     word ptr [esp+78h+var_40], dx
0040FF9D  call    sub_40D1E0
0040FFA2  lea     eax, [esp+70h+var_60]
0040FFA6  push    eax
0040FFA7  lea     ecx, [esp+74h+var_28]
0040FFAB  push    ecx
0040FFAC  lea     edx, [esp+78h+var_44]
0040FFB0  push    edx
0040FFB1  push    edi
0040FFB2  mov     edi, [esp+80h+arg_0]
0040FFB9  mov     byte ptr [esp+80h+var_4], 3
0040FFBE  call    sub_40FCF0
0040FFC3  mov     byte ptr [esp+70h+var_4], 1
0040FFC8  cmp     [esp+70h+var_2C], ebp
0040FFCC  jb      short loc_40FFDB
0040FFCE  mov     eax, [esp+70h+var_40]
0040FFD2  push    eax; void *
0040FFD3  call    ??3@YAXPAX@Z; operator delete(void *)
0040FFD8  add     esp, 4
0040FFDB  fld     ds:flt_727940
0040FFE1  sub     esp, 10h
0040FFE4  fstp    [esp+80h+var_74]; float
0040FFE8  mov     ecx, edi
0040FFEA  fld     ds:flt_727F20
0040FFF0  fstp    [esp+80h+var_78]; float
0040FFF4  fld     ds:flt_727298
0040FFFA  fst     [esp+80h+var_7C]; float
0040FFFE  fstp    [esp+80h+var_80]; float
00410001  call    sub_593AD0
00410006  fldz
00410008  push    ebx; char
00410009  push    1; char
0041000B  push    0FFh; int
00410010  push    ecx
00410011  fstp    [esp+80h+var_80]; float
00410014  push    0FFFFFFFFh; int
00410016  fld     ds:flt_727940
0041001C  push    esi; int
0041001D  sub     esp, 8
00410020  fstp    [esp+90h+var_8C]; float
00410024  fld     ds:flt_727F20
0041002A  fstp    [esp+90h+var_90]; float
0041002D  push    edi; int
0041002E  call    sub_49D260
00410033  fldz
00410035  add     esp, 14h
00410038  fst     [esp+80h+var_74]; float
0041003C  fstp    [esp+80h+var_78]; float
00410040  mov     ecx, edi
00410042  fld1
00410044  fst     [esp+80h+var_7C]; float
00410048  fstp    [esp+80h+var_80]; float
0041004B  call    sub_593AD0
00410050  cmp     [esp+70h+var_48], ebp
00410054  jb      short loc_410063
00410056  mov     ecx, [esp+70h+var_5C]
0041005A  push    ecx; void *
0041005B  call    ??3@YAXPAX@Z; operator delete(void *)
00410060  add     esp, 4
00410063  xor     edx, edx
00410065  mov     [esp+70h+var_48], 7
0041006D  mov     [esp+70h+var_4C], ebx
00410071  mov     word ptr [esp+70h+var_5C], dx
00410076  cmp     [esp+70h+var_10], ebp
0041007A  jb      short loc_410089
0041007C  mov     eax, [esp+70h+var_24]
00410080  push    eax; void *
00410081  call    ??3@YAXPAX@Z; operator delete(void *)
00410086  add     esp, 4
00410089  mov     ecx, [esp+70h+var_C]
0041008D  pop     edi
0041008E  pop     esi
0041008F  pop     ebp
00410090  mov     large fs:0, ecx
00410097  pop     ebx
00410098  add     esp, 60h
0041009B  retn    4
006EDD10  lea     ecx, [ebp-28h]; void *
006EDD13  jmp     sub_401330
006EDD18  lea     ecx, [ebp-60h]; void *
006EDD1B  jmp     sub_401330
006EDD20  lea     ecx, [ebp-44h]; void *
006EDD23  jmp     sub_401330
006EDD28  lea     ecx, [ebp-44h]; void *
006EDD2B  jmp     sub_401330
006EDD30  mov     eax, offset stru_7401A8
006EDD35  jmp     ___CxxFrameHandler3
