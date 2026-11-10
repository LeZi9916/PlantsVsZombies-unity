BOOL __thiscall sub_5C08E0(_DWORD *this, char a2)
{
  int v2; // ecx
  int v3; // ecx
  _DWORD *v4; // eax
  char v6; // [esp-Ch] [ebp-A8h]
  int v7; // [esp-8h] [ebp-A4h]
  int v9; // [esp+70h] [ebp-2Ch]
  _DWORD *v10; // [esp+74h] [ebp-28h]
  int v11; // [esp+78h] [ebp-24h]
  _BYTE v12[4]; // [esp+7Ch] [ebp-20h] BYREF
  int v13; // [esp+80h] [ebp-1Ch]
  _BYTE v14[8]; // [esp+84h] [ebp-18h] BYREF
  _BYTE v15[8]; // [esp+8Ch] [ebp-10h] BYREF
  int v16; // [esp+94h] [ebp-8h] BYREF
  int v17; // [esp+98h] [ebp-4h]

  v10 = this + 1;
  v11 = this[6];
  sub_4086A0();
  v13 = v11;
  v2 = 1;
  if ( !v10 )
    _invalid_parameter_noinfo();
  sub_408680(v2, v12);
  v9 = this[6];
  sub_4086A0();
  v17 = v9;
  if ( this == (_DWORD *)-4 )
    _invalid_parameter_noinfo();
  sub_408680(v3, &v16);
  v7 = v17;
  v6 = v16;
  v4 = (_DWORD *)sub_62EFA0(v15);
  sub_5D48D0(v14, *v4, v4[1], v6, v7, &a2);
  return (unsigned __int8)sub_632700(v12) == 0;
}

