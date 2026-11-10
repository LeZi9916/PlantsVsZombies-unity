BOOL __thiscall sub_5C0F00(_DWORD *this)
{
  int v1; // ecx
  bool v2; // dl
  BOOL result; // eax
  int v4; // [esp+0h] [ebp-34h]
  int v5; // [esp+0h] [ebp-34h]
  _DWORD *v7; // [esp+Ch] [ebp-28h]
  int v8; // [esp+18h] [ebp-1Ch]
  _BYTE v9[4]; // [esp+24h] [ebp-10h] BYREF
  int v10; // [esp+28h] [ebp-Ch]
  _BYTE v11[8]; // [esp+2Ch] [ebp-8h] BYREF

  (*(void (__thiscall **)(_DWORD *))(*this + 68))(this);
  sub_62EFA0(v11);
  while ( 1 )
  {
    v8 = this[6];
    sub_4086A0();
    v10 = v8;
    if ( this == (_DWORD *)-4 )
      _invalid_parameter_noinfo();
    sub_408680(v1, v9);
    v2 = (unsigned __int8)sub_632700(v9) == 0;
    result = v2;
    if ( !v2 )
      break;
    *(_BYTE *)(*(_DWORD *)sub_5B7240(v11, v4) + 60) = 1;
    v7 = (_DWORD *)sub_5B7240(v11, v5);
    v4 = *v7;
    (*(void (__thiscall **)(_DWORD))(*(_DWORD *)*v7 + 40))(*v7);
    sub_5D3980(v11);
  }
  return result;
}

