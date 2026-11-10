int __thiscall sub_5C0720(_DWORD *this, _DWORD *a2)
{
  int v2; // ecx
  int v3; // ecx
  _DWORD *v4; // eax
  int result; // eax
  int v6; // ecx
  char v7; // [esp-10h] [ebp-A4h]
  int v8; // [esp-Ch] [ebp-A0h]
  int v10; // [esp+8h] [ebp-8Ch]
  int v11; // [esp+60h] [ebp-34h]
  _DWORD *v12; // [esp+64h] [ebp-30h]
  int v13; // [esp+68h] [ebp-2Ch]
  _BYTE v14[4]; // [esp+6Ch] [ebp-28h] BYREF
  int v15; // [esp+70h] [ebp-24h]
  _BYTE v16[4]; // [esp+74h] [ebp-20h] BYREF
  int v17; // [esp+78h] [ebp-1Ch]
  _BYTE v18[8]; // [esp+7Ch] [ebp-18h] BYREF
  _BYTE v19[8]; // [esp+84h] [ebp-10h] BYREF
  int v20; // [esp+8Ch] [ebp-8h] BYREF
  int v21; // [esp+90h] [ebp-4h]

  v12 = this + 1;
  v13 = this[6];
  sub_4086A0();
  v17 = v13;
  v2 = 1;
  if ( !v12 )
    _invalid_parameter_noinfo();
  sub_408680(v2, v16);
  v11 = this[6];
  sub_4086A0();
  v21 = v11;
  if ( this == (_DWORD *)-4 )
    _invalid_parameter_noinfo();
  sub_408680(v3, &v20);
  v8 = v21;
  v7 = v20;
  v4 = (_DWORD *)sub_62EFA0(v19);
  sub_5D48D0(v18, *v4, v4[1], v7, v8, &a2);
  result = sub_632700(v16);
  if ( (_BYTE)result )
  {
    v10 = this[6];
    sub_4086A0();
    v15 = v10;
    if ( this == (_DWORD *)-4 )
      _invalid_parameter_noinfo();
    sub_408680(v6, v14);
    sub_5C0FD0(v14, a2);
    a2[8] = this[8];
    a2[9] = this;
    if ( this[8] )
    {
      (*(void (__thiscall **)(_DWORD *, _DWORD))(*a2 + 80))(a2, this[8]);
      (*(void (__thiscall **)(_DWORD *))(*a2 + 76))(a2);
      sub_5C41E0(this[8]);
    }
    return (*(int (__thiscall **)(_DWORD *))(*this + 68))(this);
  }
  return result;
}

