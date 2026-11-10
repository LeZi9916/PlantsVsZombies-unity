int __thiscall sub_5C09D0(int this, int a2)
{
  int v2; // ecx
  _DWORD *v3; // eax
  int v4; // ecx
  int result; // eax
  char v6; // [esp-Ch] [ebp-C0h]
  int v7; // [esp-8h] [ebp-BCh]
  int v9; // [esp+28h] [ebp-8Ch]
  int v10; // [esp+2Ch] [ebp-88h]
  int v11; // [esp+34h] [ebp-80h]
  int v12; // [esp+70h] [ebp-44h]
  int v13; // [esp+74h] [ebp-40h]
  _BYTE v14[8]; // [esp+78h] [ebp-3Ch] BYREF
  int v15; // [esp+80h] [ebp-34h]
  int v16; // [esp+84h] [ebp-30h]
  int v17; // [esp+88h] [ebp-2Ch]
  int v18; // [esp+8Ch] [ebp-28h]
  _BYTE v19[4]; // [esp+90h] [ebp-24h] BYREF
  int v20; // [esp+94h] [ebp-20h]
  _BYTE v21[8]; // [esp+98h] [ebp-1Ch] BYREF
  int v22; // [esp+A0h] [ebp-14h] BYREF
  int v23; // [esp+A4h] [ebp-10h]
  char v24; // [esp+ABh] [ebp-9h]
  int v25; // [esp+ACh] [ebp-8h] BYREF
  int v26; // [esp+B0h] [ebp-4h]

  v12 = this + 4;
  v13 = *(_DWORD *)(this + 24);
  sub_4086A0();
  v23 = v13;
  v2 = 1;
  if ( !v12 )
    _invalid_parameter_noinfo();
  sub_408680(v2, &v22);
  v7 = v23;
  v6 = v22;
  v3 = (_DWORD *)sub_62EFA0(v21);
  sub_5D48D0(&v25, *v3, v3[1], v6, v7, &a2);
  v11 = *(_DWORD *)(this + 24);
  sub_4086A0();
  v4 = v11;
  v20 = v11;
  if ( this == -4 )
    _invalid_parameter_noinfo();
  sub_408680(v4, v19);
  result = sub_632700(v19);
  if ( !(_BYTE)result )
  {
    sub_5C4B40(a2);
    *(_DWORD *)(a2 + 36) = 0;
    v24 = sub_632700(this + 44);
    v9 = v25;
    v10 = v26;
    sub_5D3980(&v25);
    v15 = v9;
    v16 = v10;
    v17 = v9;
    v18 = v10;
    result = sub_5D1840(v14, v9, v10);
    if ( v24 )
    {
      *(_DWORD *)(this + 44) = v25;
      *(_DWORD *)(this + 48) = v26;
      result = this;
      *(_BYTE *)(this + 40) = 1;
    }
  }
  return result;
}

