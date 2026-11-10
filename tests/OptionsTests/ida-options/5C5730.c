int __stdcall sub_5C5730(int a1, int a2, int a3, int a4, int a5, int a6, char a7, int a8, int a9)
{
  int result; // eax
  char v10; // [esp+Bh] [ebp-1h]

  v10 = *(_BYTE *)(a1 + 74);
  *(_BYTE *)(a1 + 74) = byte_746BAA;
  result = sub_594280(a2, a3, a4, a5, a6, a7, a8, a9, -1);
  *(_BYTE *)(a1 + 74) = v10;
  return result;
}

