_DWORD *__cdecl sub_453310(int a1, int a2, int a3)
{
  _DWORD *v3; // esi

  v3 = operator new(0x150u);
  if ( v3 )
  {
    sub_5D1090(0, a1, a2);
    *v3 = &off_70D628;
  }
  else
  {
    v3 = 0;
  }
  sub_453240(v3, a3);
  v3[79] = 1;
  v3[80] = 1;
  *((_BYTE *)v3 + 80) = 1;
  *((_BYTE *)v3 + 106) = 1;
  v3[19] = 33;
  return v3;
}

