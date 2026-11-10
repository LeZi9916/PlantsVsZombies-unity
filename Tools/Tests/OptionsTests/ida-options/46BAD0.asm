0046BAD0  push    esi
0046BAD1  mov     esi, ecx
0046BAD3  call    sub_46BAF0
0046BAD8  test    [esp+4+arg_0], 1
0046BADD  jz      short loc_46BAE8
0046BADF  push    esi; void *
0046BAE0  call    ??3@YAXPAX@Z; operator delete(void *)
0046BAE5  add     esp, 4
0046BAE8  mov     eax, esi
0046BAEA  pop     esi
0046BAEB  retn    4
