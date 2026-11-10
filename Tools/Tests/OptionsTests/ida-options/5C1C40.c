__int64 __thiscall sub_5C1C40(_DWORD *this, int a2)
{
  int v2; // ecx
  __int64 result; // rax
  _DWORD *v4; // [esp+0h] [ebp-2Ch]
  int v5; // [esp+Ch] [ebp-20h]
  _BYTE v6[4]; // [esp+18h] [ebp-14h] BYREF
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
    LODWORD(result) = sub_632700(v6);
    HIDWORD(result) = (unsigned __int8)result == 0;
    if ( (_BYTE)result )
      break;
    v8 = *(_DWORD **)sub_5B7240(v9, v4);
    v8[8] = a2;
    (*(void (__thiscall **)(_DWORD *, int))(*v8 + 80))(v8, a2);
    sub_5D3980(v9);
    (*(void (__thiscall **)(_DWORD *))(*v4 + 68))(v4);
  }
  return result;
}

