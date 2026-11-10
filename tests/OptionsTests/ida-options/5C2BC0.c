BOOL __thiscall sub_5C2BC0(_DWORD *this)
{
  int v1; // ecx
  bool v2; // dl
  BOOL result; // eax
  _DWORD *v4; // [esp+0h] [ebp-30h]
  int v5; // [esp+Ch] [ebp-24h]
  char v6[4]; // [esp+1Ch] [ebp-14h] BYREF
  int v7; // [esp+20h] [ebp-10h]
  int v8; // [esp+24h] [ebp-Ch]
  _BYTE v9[8]; // [esp+28h] [ebp-8h] BYREF

  v4 = this;
  (*(void (__thiscall **)(_DWORD *))(*this + 116))(this);
  if ( v4[7] )
    ++dword_815D00;
  sub_62EFA0(v9);
  while ( 1 )
  {
    v5 = v4[6];
    sub_4086A0();
    v7 = v5;
    if ( v4 == (_DWORD *)-4 )
      _invalid_parameter_noinfo();
    sub_408680(v1, v6);
    v2 = (unsigned __int8)sub_632700(v6) == 0;
    result = v2;
    if ( !v2 )
      break;
    v8 = *(_DWORD *)sub_5B7240(v9, v4);
    (*(void (__thiscall **)(int))(*(_DWORD *)v8 + 112))(v8);
    sub_5D3980(v9);
  }
  return result;
}

