int __stdcall sub_5C5790(int a1, int a2, int a3, int a4, int a5)
{
  int result; // eax
  char v6; // [esp+Bh] [ebp-1h]

  v6 = *(_BYTE *)(a1 + 74);
  *(_BYTE *)(a1 + 74) = byte_746BAA;
  result = sub_594700(a2, a3, a4, a5, 0, -1, 0, 0);
  *(_BYTE *)(a1 + 74) = v6;
  return result;
}

