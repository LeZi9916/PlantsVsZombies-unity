_DWORD *__thiscall sub_5C1D00(_DWORD *this, _DWORD *a2)
{
  int v2; // ecx
  _DWORD *result; // eax
  _DWORD *v4; // [esp+0h] [ebp-2Ch]
  int v5; // [esp+8h] [ebp-24h]
  char v6[4]; // [esp+18h] [ebp-14h] BYREF
  int v7; // [esp+1Ch] [ebp-10h]
  _DWORD *v8; // [esp+20h] [ebp-Ch]
  _BYTE v9[8]; // [esp+24h] [ebp-8h] BYREF

  v4 = this;
  sub_62EFA0(v9);
  while ( 1 )
  {
    v5 = v4[6];
    sub_4086A0();
    v2 = v5;
    v7 = v5;
    if ( v4 == (_DWORD *)-4 )
      _invalid_parameter_noinfo();
    sub_408680(v2, v6);
    if ( (unsigned __int8)sub_632700(v6) )
      break;
    v8 = *(_DWORD **)sub_5B7240(v9, v4);
    (*(void (__thiscall **)(_DWORD *, _DWORD *))(*a2 + 24))(a2, v8);
    (*(void (__thiscall **)(_DWORD *, _DWORD *))(*v8 + 84))(v8, a2);
    v8[8] = 0;
    sub_5D3980(v9);
  }
  result = (_DWORD *)a2[31];
  if ( result == v4 )
    a2[31] = 0;
  return result;
}

