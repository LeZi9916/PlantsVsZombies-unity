int __thiscall sub_5C16D0(int this, int a2, char a3)
{
  _DWORD *v3; // eax
  int v4; // ecx
  int result; // eax
  int v6; // ecx
  _DWORD *v7; // eax
  int *v8; // eax
  int v9; // edx
  int v10; // eax
  char v11; // [esp-Ch] [ebp-130h]
  char v12; // [esp-Ch] [ebp-130h]
  int v13; // [esp-8h] [ebp-12Ch]
  int v14; // [esp-8h] [ebp-12Ch]
  int v16; // [esp+58h] [ebp-CCh]
  int v17; // [esp+84h] [ebp-A0h]
  int v18; // [esp+88h] [ebp-9Ch]
  int v19; // [esp+90h] [ebp-94h]
  _BYTE v20[8]; // [esp+D4h] [ebp-50h] BYREF
  _BYTE v21[8]; // [esp+DCh] [ebp-48h] BYREF
  int v22; // [esp+E4h] [ebp-40h] BYREF
  int v23; // [esp+E8h] [ebp-3Ch]
  _BYTE v24[8]; // [esp+ECh] [ebp-38h] BYREF
  int v25; // [esp+F4h] [ebp-30h]
  int v26; // [esp+F8h] [ebp-2Ch]
  int v27; // [esp+FCh] [ebp-28h]
  int v28; // [esp+100h] [ebp-24h]
  _BYTE v29[4]; // [esp+104h] [ebp-20h] BYREF
  int v30; // [esp+108h] [ebp-1Ch]
  _BYTE v31[12]; // [esp+10Ch] [ebp-18h] BYREF
  int v32; // [esp+118h] [ebp-Ch]
  int v33; // [esp+11Ch] [ebp-8h] BYREF
  int v34; // [esp+120h] [ebp-4h]

  sub_65DB10(*(_DWORD *)(this + 24), this + 4);
  v13 = v32;
  v11 = v31[8];
  v3 = (_DWORD *)sub_62EFA0(v31);
  sub_5D48D0(&v33, *v3, v3[1], v11, v13, &a2);
  v19 = *(_DWORD *)(this + 24);
  sub_4086A0();
  v4 = v19;
  v30 = v19;
  if ( this == -4 )
    _invalid_parameter_noinfo();
  sub_408680(v4, v29);
  result = sub_632700(v29);
  if ( !(_BYTE)result )
  {
    if ( (unsigned __int8)sub_632700(this + 44) )
    {
      v17 = *(_DWORD *)(this + 44);
      v18 = *(_DWORD *)(this + 48);
      sub_5D3980(this + 44);
      v27 = v17;
      v28 = v18;
      *(_BYTE *)(this + 40) = 1;
    }
    v25 = v33;
    v26 = v34;
    sub_5D1840(v24, v33, v34);
    v16 = *(_DWORD *)(this + 24);
    sub_4086A0();
    v23 = v16;
    if ( this == -4 )
      _invalid_parameter_noinfo();
    sub_408680(v6, &v22);
    v14 = v23;
    v12 = v22;
    v7 = (_DWORD *)sub_62EFA0(v21);
    v8 = (int *)sub_5D48D0(v20, *v7, v7[1], v12, v14, &a3);
    v9 = *v8;
    v10 = v8[1];
    v33 = v9;
    v34 = v10;
    sub_5C0FD0(&v33, a2);
    return (*(int (__thiscall **)(int))(*(_DWORD *)a2 + 120))(a2);
  }
  return result;
}

