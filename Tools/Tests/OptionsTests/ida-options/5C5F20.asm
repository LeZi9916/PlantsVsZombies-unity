005C5F20  push    ebp
005C5F21  mov     ebp, esp
005C5F23  push    0FFFFFFFFh
005C5F25  push    offset SEH_5C5F20
005C5F2A  mov     eax, large fs:0
005C5F30  push    eax
005C5F31  sub     esp, 180h
005C5F37  mov     eax, ___security_cookie
005C5F3C  xor     eax, ebp
005C5F3E  push    eax
005C5F3F  lea     eax, [ebp+var_C]
005C5F42  mov     large fs:0, eax
005C5F48  mov     [ebp+var_17C], ecx
005C5F4E  mov     eax, [ebp+var_17C]
005C5F54  cmp     dword ptr [eax+0B4h], 0
005C5F5B  jz      loc_5C62B9
005C5F61  mov     ecx, [ebp+var_17C]
005C5F67  movzx   edx, byte ptr [ecx+0C8h]
005C5F6E  test    edx, edx
005C5F70  jz      short loc_5C5F93
005C5F72  mov     eax, [ebp+var_17C]
005C5F78  mov     ecx, [eax+0B4h]; this
005C5F7E  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C5F83  cdq
005C5F84  mov     ecx, 3
005C5F89  idiv    ecx
005C5F8B  mov     [ebp+var_180], eax
005C5F91  jmp     short loc_5C5FAA
005C5F93  mov     edx, [ebp+var_17C]
005C5F99  mov     ecx, [edx+0B4h]; this
005C5F9F  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C5FA4  mov     [ebp+var_180], eax
005C5FAA  mov     eax, [ebp+var_180]
005C5FB0  mov     [ebp+var_10], eax
005C5FB3  mov     ecx, [ebp+var_17C]
005C5FB9  movzx   edx, byte ptr [ecx+0C8h]
005C5FC0  test    edx, edx
005C5FC2  jz      short loc_5C5FDD
005C5FC4  mov     eax, [ebp+var_17C]
005C5FCA  mov     ecx, [eax+0B4h]
005C5FD0  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C5FD5  mov     [ebp+var_184], eax
005C5FDB  jmp     short loc_5C5FFC
005C5FDD  mov     ecx, [ebp+var_17C]
005C5FE3  mov     ecx, [ecx+0B4h]
005C5FE9  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C5FEE  cdq
005C5FEF  mov     ecx, 3
005C5FF4  idiv    ecx
005C5FF6  mov     [ebp+var_184], eax
005C5FFC  mov     edx, [ebp+var_184]
005C6002  mov     [ebp+var_14], edx
005C6005  mov     eax, [ebp+var_17C]
005C600B  movzx   ecx, byte ptr [eax+0C8h]
005C6012  test    ecx, ecx
005C6014  jz      loc_5C617A
005C601A  mov     edx, [ebp+var_17C]
005C6020  mov     eax, [edx+4Ch]
005C6023  sub     eax, [ebp+var_14]
005C6026  cdq
005C6027  sub     eax, edx
005C6029  sar     eax, 1
005C602B  mov     [ebp+var_18], eax
005C602E  mov     eax, [ebp+var_14]
005C6031  push    eax
005C6032  mov     ecx, [ebp+var_10]
005C6035  push    ecx
005C6036  xor     edx, edx
005C6038  xor     ecx, ecx
005C603A  lea     eax, [ebp+var_128]
005C6040  call    sub_4091F0
005C6045  push    eax; int
005C6046  mov     edx, [ebp+var_18]
005C6049  push    edx; int
005C604A  push    0; int
005C604C  mov     eax, [ebp+var_17C]
005C6052  mov     ecx, [eax+0B4h]
005C6058  push    ecx; Concurrency::details::ExecutionResource *
005C6059  mov     ecx, [ebp+arg_0]
005C605C  call    sub_5930D0
005C6061  mov     edx, [ebp+arg_0]
005C6064  push    edx
005C6065  lea     ecx, [ebp+var_94]
005C606B  call    sub_5924D0
005C6070  mov     [ebp+var_4], 0
005C6077  mov     eax, [ebp+var_14]
005C607A  push    eax
005C607B  mov     ecx, [ebp+var_10]
005C607E  shl     ecx, 1
005C6080  mov     edx, [ebp+var_17C]
005C6086  mov     eax, [edx+48h]
005C6089  sub     eax, ecx
005C608B  push    eax
005C608C  mov     ecx, [ebp+var_18]
005C608F  push    ecx
005C6090  mov     edx, [ebp+var_10]
005C6093  push    edx
005C6094  lea     ecx, [ebp+var_94]
005C609A  call    sub_5939F0
005C609F  mov     [ebp+var_98], 0
005C60A9  jmp     short loc_5C60BA
005C60AB  mov     eax, [ebp+var_98]
005C60B1  add     eax, 1
005C60B4  mov     [ebp+var_98], eax
005C60BA  mov     ecx, [ebp+var_10]
005C60BD  shl     ecx, 1
005C60BF  mov     edx, [ebp+var_17C]
005C60C5  mov     eax, [edx+48h]
005C60C8  sub     eax, ecx
005C60CA  mov     ecx, [ebp+var_10]
005C60CD  lea     eax, [eax+ecx-1]
005C60D1  cdq
005C60D2  idiv    [ebp+var_10]
005C60D5  cmp     [ebp+var_98], eax
005C60DB  jge     short loc_5C6122
005C60DD  mov     edx, [ebp+var_14]
005C60E0  push    edx
005C60E1  mov     eax, [ebp+var_10]
005C60E4  push    eax
005C60E5  xor     edx, edx
005C60E7  mov     ecx, [ebp+var_10]
005C60EA  lea     eax, [ebp+var_138]
005C60F0  call    sub_4091F0
005C60F5  push    eax; int
005C60F6  mov     ecx, [ebp+var_18]
005C60F9  push    ecx; int
005C60FA  mov     edx, [ebp+var_98]
005C6100  imul    edx, [ebp+var_10]
005C6104  add     edx, [ebp+var_10]
005C6107  push    edx; int
005C6108  mov     eax, [ebp+var_17C]
005C610E  mov     ecx, [eax+0B4h]
005C6114  push    ecx; Concurrency::details::ExecutionResource *
005C6115  lea     ecx, [ebp+var_94]
005C611B  call    sub_5930D0
005C6120  jmp     short loc_5C60AB
005C6122  mov     edx, [ebp+var_14]
005C6125  push    edx
005C6126  mov     eax, [ebp+var_10]
005C6129  push    eax
005C612A  mov     ecx, [ebp+var_10]
005C612D  shl     ecx, 1
005C612F  xor     edx, edx
005C6131  lea     eax, [ebp+var_148]
005C6137  call    sub_4091F0
005C613C  push    eax; int
005C613D  mov     ecx, [ebp+var_18]
005C6140  push    ecx; int
005C6141  mov     edx, [ebp+var_17C]
005C6147  mov     eax, [edx+48h]
005C614A  sub     eax, [ebp+var_10]
005C614D  push    eax; int
005C614E  mov     ecx, [ebp+var_17C]
005C6154  mov     edx, [ecx+0B4h]
005C615A  push    edx; Concurrency::details::ExecutionResource *
005C615B  mov     ecx, [ebp+arg_0]
005C615E  call    sub_5930D0
005C6163  mov     [ebp+var_4], 0FFFFFFFFh
005C616A  lea     ecx, [ebp+var_94]
005C6170  call    sub_5926D0
005C6175  jmp     loc_5C62B9
005C617A  mov     eax, [ebp+var_14]
005C617D  push    eax
005C617E  mov     ecx, [ebp+var_10]
005C6181  push    ecx
005C6182  xor     edx, edx
005C6184  xor     ecx, ecx
005C6186  lea     eax, [ebp+var_158]
005C618C  call    sub_4091F0
005C6191  push    eax; int
005C6192  push    0; int
005C6194  push    0; int
005C6196  mov     edx, [ebp+var_17C]
005C619C  mov     eax, [edx+0B4h]
005C61A2  push    eax; Concurrency::details::ExecutionResource *
005C61A3  mov     ecx, [ebp+arg_0]
005C61A6  call    sub_5930D0
005C61AB  mov     ecx, [ebp+arg_0]
005C61AE  push    ecx
005C61AF  lea     ecx, [ebp+var_114]
005C61B5  call    sub_5924D0
005C61BA  mov     [ebp+var_4], 1
005C61C1  mov     edx, [ebp+var_14]
005C61C4  shl     edx, 1
005C61C6  mov     eax, [ebp+var_17C]
005C61CC  mov     ecx, [eax+4Ch]
005C61CF  sub     ecx, edx
005C61D1  push    ecx
005C61D2  mov     edx, [ebp+var_10]
005C61D5  push    edx
005C61D6  mov     eax, [ebp+var_14]
005C61D9  push    eax
005C61DA  push    0
005C61DC  lea     ecx, [ebp+var_114]
005C61E2  call    sub_5939F0
005C61E7  mov     [ebp+var_118], 0
005C61F1  jmp     short loc_5C6202
005C61F3  mov     ecx, [ebp+var_118]
005C61F9  add     ecx, 1
005C61FC  mov     [ebp+var_118], ecx
005C6202  mov     edx, [ebp+var_14]
005C6205  shl     edx, 1
005C6207  mov     eax, [ebp+var_17C]
005C620D  mov     ecx, [eax+4Ch]
005C6210  sub     ecx, edx
005C6212  mov     edx, [ebp+var_14]
005C6215  lea     eax, [ecx+edx-1]
005C6219  cdq
005C621A  idiv    [ebp+var_14]
005C621D  cmp     [ebp+var_118], eax
005C6223  jge     short loc_5C6268
005C6225  mov     eax, [ebp+var_14]
005C6228  push    eax
005C6229  mov     ecx, [ebp+var_10]
005C622C  push    ecx
005C622D  mov     edx, [ebp+var_14]
005C6230  xor     ecx, ecx
005C6232  lea     eax, [ebp+var_168]
005C6238  call    sub_4091F0
005C623D  push    eax; int
005C623E  mov     edx, [ebp+var_118]
005C6244  imul    edx, [ebp+var_14]
005C6248  add     edx, [ebp+var_14]
005C624B  push    edx; int
005C624C  push    0; int
005C624E  mov     eax, [ebp+var_17C]
005C6254  mov     ecx, [eax+0B4h]
005C625A  push    ecx; Concurrency::details::ExecutionResource *
005C625B  lea     ecx, [ebp+var_114]
005C6261  call    sub_5930D0
005C6266  jmp     short loc_5C61F3
005C6268  mov     edx, [ebp+var_14]
005C626B  push    edx
005C626C  mov     eax, [ebp+var_10]
005C626F  push    eax
005C6270  mov     edx, [ebp+var_14]
005C6273  shl     edx, 1
005C6275  xor     ecx, ecx
005C6277  lea     eax, [ebp+var_178]
005C627D  call    sub_4091F0
005C6282  push    eax; int
005C6283  mov     ecx, [ebp+var_17C]
005C6289  mov     edx, [ecx+4Ch]
005C628C  sub     edx, [ebp+var_14]
005C628F  push    edx; int
005C6290  push    0; int
005C6292  mov     eax, [ebp+var_17C]
005C6298  mov     ecx, [eax+0B4h]
005C629E  push    ecx; Concurrency::details::ExecutionResource *
005C629F  mov     ecx, [ebp+arg_0]
005C62A2  call    sub_5930D0
005C62A7  mov     [ebp+var_4], 0FFFFFFFFh
005C62AE  lea     ecx, [ebp+var_114]
005C62B4  call    sub_5926D0
005C62B9  mov     edx, [ebp+var_17C]
005C62BF  movzx   eax, byte ptr [edx+0C8h]
005C62C6  test    eax, eax
005C62C8  jz      loc_5C6355
005C62CE  mov     ecx, [ebp+var_17C]
005C62D4  cmp     dword ptr [ecx+0B8h], 0
005C62DB  jz      short loc_5C6355
005C62DD  mov     edx, [ebp+var_17C]
005C62E3  mov     ecx, [edx+0B8h]
005C62E9  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C62EE  mov     ecx, [ebp+var_17C]
005C62F4  mov     edx, [ecx+4Ch]
005C62F7  sub     edx, eax
005C62F9  mov     eax, edx
005C62FB  cdq
005C62FC  sub     eax, edx
005C62FE  sar     eax, 1
005C6300  push    eax; int
005C6301  mov     eax, [ebp+var_17C]
005C6307  mov     ecx, [eax+0B8h]; this
005C630D  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C6312  mov     ecx, [ebp+var_17C]
005C6318  mov     edx, [ecx+48h]
005C631B  sub     edx, eax
005C631D  mov     [ebp+var_188], edx
005C6323  fild    [ebp+var_188]
005C6329  mov     eax, [ebp+var_17C]
005C632F  fmul    qword ptr [eax+0A8h]
005C6335  call    __ftol2_sse
005C633A  push    eax; int
005C633B  mov     ecx, [ebp+var_17C]
005C6341  mov     edx, [ecx+0B8h]
005C6347  push    edx; Concurrency::details::ExecutionResource *
005C6348  mov     ecx, [ebp+arg_0]
005C634B  call    sub_592F90
005C6350  jmp     loc_5C63EC
005C6355  mov     eax, [ebp+var_17C]
005C635B  movzx   ecx, byte ptr [eax+0C8h]
005C6362  test    ecx, ecx
005C6364  jnz     loc_5C63EC
005C636A  mov     edx, [ebp+var_17C]
005C6370  cmp     dword ptr [edx+0B8h], 0
005C6377  jz      short loc_5C63EC
005C6379  mov     eax, [ebp+var_17C]
005C637F  mov     ecx, [eax+0B8h]
005C6385  call    unknown_libname_11; Microsoft VisualC 2-14/net runtime
005C638A  mov     ecx, [ebp+var_17C]
005C6390  mov     edx, [ecx+4Ch]
005C6393  sub     edx, eax
005C6395  mov     [ebp+var_18C], edx
005C639B  fild    [ebp+var_18C]
005C63A1  mov     eax, [ebp+var_17C]
005C63A7  fmul    qword ptr [eax+0A8h]
005C63AD  call    __ftol2_sse
005C63B2  push    eax; int
005C63B3  mov     ecx, [ebp+var_17C]
005C63B9  mov     ecx, [ecx+0B8h]; this
005C63BF  call    ?GetNodeId@ExecutionResource@details@Concurrency@@UBEIXZ; Concurrency::details::ExecutionResource::GetNodeId(void)
005C63C4  mov     edx, [ebp+var_17C]
005C63CA  mov     ecx, [edx+48h]
005C63CD  sub     ecx, eax
005C63CF  mov     eax, ecx
005C63D1  cdq
005C63D2  sub     eax, edx
005C63D4  sar     eax, 1
005C63D6  push    eax; int
005C63D7  mov     edx, [ebp+var_17C]
005C63DD  mov     eax, [edx+0B8h]
005C63E3  push    eax; Concurrency::details::ExecutionResource *
005C63E4  mov     ecx, [ebp+arg_0]
005C63E7  call    sub_592F90
005C63EC  mov     ecx, [ebp+var_C]
005C63EF  mov     large fs:0, ecx
005C63F6  pop     ecx
005C63F7  mov     esp, ebp
005C63F9  pop     ebp
005C63FA  retn    4
006DD850  lea     ecx, [ebp+var_94]
006DD856  jmp     sub_5926D0
006DD85B  lea     ecx, [ebp+var_114]
006DD861  jmp     sub_5926D0
006DD866  mov     edx, [esp-4+arg_4]
006DD86A  lea     eax, [edx+0Ch]
006DD86D  mov     ecx, [edx-184h]
006DD873  xor     ecx, eax; StackCookie
006DD875  call    @__security_check_cookie@4; __security_check_cookie(x)
006DD87A  mov     eax, offset stru_72F84C
006DD87F  jmp     ___CxxFrameHandler3
