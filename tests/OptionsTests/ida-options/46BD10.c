int __stdcall sub_46BD10(int a1, int a2)
{
  int v2; // ebx
  int v3; // esi
  _DWORD *v4; // esi
  bool v5; // cc
  _DWORD *v6; // eax
  __int16 v7; // bp
  unsigned int v8; // eax
  int v9; // eax
  int v10; // esi
  int v11; // eax
  int v12; // ecx
  __int16 v14; // [esp+Ch] [ebp-14h]

  v2 = 0;
  v3 = *(_DWORD *)(a1 + 396);
  *(_DWORD *)(a2 + 24) = 7;
  *(_DWORD *)(a2 + 20) = 0;
  v14 = 32;
  v4 = (_DWORD *)(v3 + 164);
  *(_WORD *)(a2 + 4) = 0;
  v5 = 1;
  if ( (int)v4[5] > 0 )
  {
    do
    {
      if ( !v5 )
        _invalid_parameter_noinfo();
      if ( v4[6] < 8u )
        v6 = v4 + 1;
      else
        v6 = (_DWORD *)v4[1];
      v7 = *((_WORD *)v6 + v2);
      if ( v7 == 32 )
      {
        if ( v14 != 32 )
          sub_468350(1, 32);
      }
      else
      {
        sub_468350(1, *((unsigned __int16 *)v6 + v2));
      }
      v8 = v4[5];
      v5 = ++v2 <= v8;
      v14 = v7;
    }
    while ( v2 < (int)v8 );
  }
  v9 = *(_DWORD *)(a2 + 20);
  if ( v9 )
  {
    v10 = v9 - 1;
    if ( *(_DWORD *)(a2 + 24) < 8u )
      v11 = a2 + 4;
    else
      v11 = *(_DWORD *)(a2 + 4);
    if ( *(_WORD *)(v11 + 2 * v10) == 32 )
    {
      v12 = *(_DWORD *)(a2 + 20);
      if ( v12 )
        sub_40D5C0(v12 - 1, -1);
      else
        sub_468350(-1, 0);
    }
  }
  return a2;
}

