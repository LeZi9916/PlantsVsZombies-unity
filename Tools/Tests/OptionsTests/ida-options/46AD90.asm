0046AD90  mov     eax, [esp+arg_C]
0046AD94  mov     edx, [esp+arg_4]
0046AD98  push    esi
0046AD99  push    edi
0046AD9A  push    eax
0046AD9B  mov     eax, [esp+0Ch+arg_0]
0046AD9F  mov     esi, ecx
0046ADA1  mov     ecx, [esp+0Ch+arg_8]
0046ADA5  push    ecx
0046ADA6  push    edx
0046ADA7  push    eax
0046ADA8  mov     ecx, esi
0046ADAA  call    sub_5D0AF0
0046ADAF  mov     ecx, [esi+174h]
0046ADB5  mov     edx, [ecx]
0046ADB7  mov     eax, [edx+0A0h]
0046ADBD  push    28h ; '('
0046ADBF  push    87h
0046ADC4  push    74h ; 't'
0046ADC6  push    0C7h
0046ADCB  call    eax
0046ADCD  mov     ecx, [esi+178h]
0046ADD3  mov     edx, [ecx]
0046ADD5  mov     eax, [edx+0A0h]
0046ADDB  push    28h ; '('
0046ADDD  push    87h
0046ADE2  push    8Fh
0046ADE7  push    0C7h
0046ADEC  call    eax
0046ADEE  mov     ecx, [esi+180h]
0046ADF4  mov     edx, [ecx]
0046ADF6  mov     eax, [edx+0A0h]
0046ADFC  push    2Dh ; '-'
0046ADFE  push    2Eh ; '.'
0046AE00  push    0AFh
0046AE05  push    11Bh
0046AE0A  call    eax
0046AE0C  mov     ecx, [esi+17Ch]
0046AE12  mov     edx, [ecx]
0046AE14  mov     eax, [edx+0A0h]
0046AE1A  push    2Dh ; '-'
0046AE1C  push    2Eh ; '.'
0046AE1E  push    0CEh
0046AE23  push    11Ch
0046AE28  call    eax
0046AE2A  mov     ecx, [esi+184h]
0046AE30  mov     edx, [ecx]
0046AE32  mov     eax, [edx+0A0h]
0046AE38  push    2Eh ; '.'
0046AE3A  push    0D1h
0046AE3F  push    0F1h
0046AE44  push    6Bh ; 'k'
0046AE46  call    eax
0046AE48  mov     eax, [esi+184h]
0046AE4E  mov     edi, [eax+44h]
0046AE51  mov     ecx, [esi+18Ch]
0046AE57  mov     edx, [ecx]
0046AE59  mov     eax, [eax+40h]
0046AE5C  mov     edx, [edx+0A0h]
0046AE62  push    2Eh ; '.'
0046AE64  push    0D1h
0046AE69  add     edi, 2Bh ; '+'
0046AE6C  push    edi
0046AE6D  push    eax
0046AE6E  call    edx
0046AE70  mov     eax, [esi+18Ch]
0046AE76  mov     edi, [eax+44h]
0046AE79  mov     ecx, [esi+188h]
0046AE7F  mov     edx, [ecx]
0046AE81  mov     eax, [eax+40h]
0046AE84  mov     edx, [edx+0A0h]
0046AE8A  push    2Eh ; '.'
0046AE8C  push    0D1h
0046AE91  add     edi, 2Bh ; '+'
0046AE94  push    edi
0046AE95  push    eax
0046AE96  call    edx
0046AE98  mov     ecx, [esi+190h]
0046AE9E  mov     edx, [ecx+4Ch]
0046AEA1  mov     eax, [ecx]
0046AEA3  push    edx
0046AEA4  mov     edx, [ecx+48h]
0046AEA7  mov     eax, [eax+0A0h]
0046AEAD  push    edx
0046AEAE  push    17Dh
0046AEB3  push    1Eh
0046AEB5  call    eax
0046AEB7  cmp     byte ptr [esi+194h], 0
0046AEBE  jz      short loc_46AEE8
0046AEC0  mov     eax, [esi+174h]
0046AEC6  add     dword ptr [eax+44h], 5
0046AECA  mov     eax, [esi+178h]
0046AED0  add     dword ptr [eax+44h], 0Ah
0046AED4  mov     eax, [esi+180h]
0046AEDA  add     dword ptr [eax+44h], 0Fh
0046AEDE  mov     eax, [esi+17Ch]
0046AEE4  add     dword ptr [eax+44h], 14h
0046AEE8  mov     ecx, [esi+170h]
0046AEEE  mov     eax, [ecx+91Ch]
0046AEF4  cmp     eax, 2Bh ; '+'
0046AEF7  jz      short loc_46AEFE
0046AEF9  cmp     eax, 32h ; '2'
0046AEFC  jnz     short loc_46AF08
0046AEFE  mov     esi, [esi+184h]
0046AF04  add     dword ptr [esi+44h], 2Bh ; '+'
0046AF08  pop     edi
0046AF09  pop     esi
0046AF0A  retn    10h
