0045CCF0  push    0FFFFFFFFh
0045CCF2  push    offset SEH_4643D0
0045CCF7  mov     eax, large fs:0
0045CCFD  push    eax
0045CCFE  mov     large fs:0, esp
0045CD05  push    ecx
0045CD06  push    ebx
0045CD07  push    ebp
0045CD08  push    esi
0045CD09  push    1ACh; Size
0045CD0E  call    ??2@YAPAXI@Z; operator new(uint)
0045CD13  add     esp, 4
0045CD16  mov     [esp+1Ch+var_10], eax
0045CD1A  xor     esi, esi
0045CD1C  mov     [esp+1Ch+var_4], esi
0045CD20  cmp     eax, esi
0045CD22  jz      short loc_45CD32
0045CD24  mov     ecx, [esp+1Ch+arg_0]
0045CD28  push    ecx
0045CD29  push    edi
0045CD2A  push    eax
0045CD2B  call    sub_46A350
0045CD30  mov     esi, eax
0045CD32  mov     [esp+1Ch+var_4], 0FFFFFFFFh
0045CD3A  mov     eax, dword_752CC8
0045CD3F  mov     ecx, [eax+28h]
0045CD42  mov     ebx, [eax+24h]
0045CD45  mov     ebp, [esi]
0045CD47  mov     eax, 258h
0045CD4C  sub     eax, ecx
0045CD4E  cdq
0045CD4F  sub     eax, edx
0045CD51  push    ecx
0045CD52  sar     eax, 1
0045CD54  push    ebx
0045CD55  push    eax
0045CD56  mov     eax, 320h
0045CD5B  sub     eax, ebx
0045CD5D  cdq
0045CD5E  sub     eax, edx
0045CD60  mov     edx, [ebp+0A0h]
0045CD66  sar     eax, 1
0045CD68  push    eax
0045CD69  mov     ecx, esi
0045CD6B  call    edx
0045CD6D  mov     eax, [edi]
0045CD6F  mov     edx, [eax+12Ch]
0045CD75  push    esi
0045CD76  push    2
0045CD78  mov     ecx, edi
0045CD7A  call    edx
0045CD7C  mov     ecx, [edi+320h]
0045CD82  mov     eax, [ecx]
0045CD84  mov     edx, [eax+20h]
0045CD87  push    esi
0045CD88  call    edx
0045CD8A  mov     ecx, [esp+1Ch+var_C]
0045CD8E  pop     esi
0045CD8F  pop     ebp
0045CD90  pop     ebx
0045CD91  mov     large fs:0, ecx
0045CD98  add     esp, 10h
0045CD9B  retn    4
006EA680  mov     eax, [ebp-10h]; Microsoft VisualC 2-14/net runtime
006EA683  push    eax; void *
006EA684  call    ??3@YAXPAX@Z; operator delete(void *)
006EA689  pop     ecx
006EA68A  retn
006EA68B  mov     eax, offset stru_73CE58
006EA690  jmp     ___CxxFrameHandler3
