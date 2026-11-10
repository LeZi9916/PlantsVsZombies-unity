int __usercall sub_453910@<eax>(int a1@<ebx>, int a2@<edi>, int a3, int a4, int a5, int a6, int a7)
{
  void *v7; // eax
  int v8; // esi
  int v9; // eax

  v7 = operator new(0x190u);
  v8 = 0;
  if ( v7 )
    v8 = sub_4533B0(v7, a4);
  v9 = a6;
  if ( !a6 )
    v9 = dword_75339C;
  (*(void (__thiscall **)(int, int))(*(_DWORD *)v8 + 284))(v8, v9);
  sub_453240(v8, a5);
  *(_DWORD *)(v8 + 72) = *(_DWORD *)(a2 + 36);
  *(_DWORD *)(v8 + 76) = *(_DWORD *)(a2 + 40);
  *(_BYTE *)(v8 + 80) = 1;
  *(_BYTE *)(v8 + 106) = 1;
  *(_DWORD *)(v8 + 316) = 1;
  *(_DWORD *)(v8 + 320) = 1;
  *(_DWORD *)(v8 + 204) = a7;
  *(_DWORD *)(v8 + 200) = a2;
  *(_DWORD *)(v8 + 208) = a1;
  return v8;
}
