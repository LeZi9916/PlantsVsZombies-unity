int __thiscall sub_5C1310(int this, int a2)
{
  int v2; // ecx
  _DWORD *v3; // eax
  int v4; // ecx
  int result; // eax
  int v6; // ecx
  char v7; // [esp-Ch] [ebp-D0h]
  int v8; // [esp-8h] [ebp-CCh]
  int v10; // [esp+8h] [ebp-BCh]
  int v11; // [esp+34h] [ebp-90h]
  int v12; // [esp+38h] [ebp-8Ch]
  int v13; // [esp+40h] [ebp-84h]
  int v14; // [esp+7Ch] [ebp-48h]
  int v15; // [esp+80h] [ebp-44h]
  _BYTE v16[4]; // [esp+84h] [ebp-40h] BYREF
  int v17; // [esp+88h] [ebp-3Ch]
  _BYTE v18[8]; // [esp+8Ch] [ebp-38h] BYREF
  int v19; // [esp+94h] [ebp-30h]
  int v20; // [esp+98h] [ebp-2Ch]
  int v21; // [esp+9Ch] [ebp-28h]
  int v22; // [esp+A0h] [ebp-24h]
  _BYTE v23[4]; // [esp+A4h] [ebp-20h] BYREF
  int v24; // [esp+A8h] [ebp-1Ch]
  _BYTE v25[8]; // [esp+ACh] [ebp-18h] BYREF
  int v26; // [esp+B4h] [ebp-10h] BYREF
  int v27; // [esp+B8h] [ebp-Ch]
  int v28; // [esp+BCh] [ebp-8h] BYREF
  int v29; // [esp+C0h] [ebp-4h]

  v14 = this + 4;
  v15 = *(_DWORD *)(this + 24);
  sub_4086A0();
  v27 = v15;
  v2 = 1;
  if ( !v14 )
    _invalid_parameter_noinfo();
  sub_408680(v2, &v26);
  v8 = v27;
  v7 = v26;
  v3 = (_DWORD *)sub_62EFA0(v25);
  sub_5D48D0(&v28, *v3, v3[1], v7, v8, &a2);
  v13 = *(_DWORD *)(this + 24);
  sub_4086A0();
  v4 = v13;
  v24 = v13;
  if ( this == -4 )
    _invalid_parameter_noinfo();
  sub_408680(v4, v23);
  result = sub_632700(v23);
  if ( !(_BYTE)result )
  {
    if ( (unsigned __int8)sub_632700(this + 44) )
    {
      v11 = *(_DWORD *)(this + 44);
      v12 = *(_DWORD *)(this + 48);
      sub_5D3980(this + 44);
      v21 = v11;
      v22 = v12;
      *(_BYTE *)(this + 40) = 1;
    }
    v19 = v28;
    v20 = v29;
    sub_5D1840(v18, v28, v29);
    v10 = *(_DWORD *)(this + 24);
    sub_4086A0();
    v17 = v10;
    if ( this == -4 )
      _invalid_parameter_noinfo();
    sub_408680(v6, v16);
    sub_5C0FD0(v16, a2);
    return (*(int (__thiscall **)(int))(*(_DWORD *)a2 + 120))(a2);
  }
  return result;
}

