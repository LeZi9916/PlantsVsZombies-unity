int __thiscall sub_5C18F0(int this, int a2, char a3)
{
  int result; // eax
  int v4; // ecx
  _DWORD *v5; // eax
  int *v6; // eax
  int v7; // edx
  int v8; // ecx
  char v9; // [esp-Ch] [ebp-13Ch]
  int v10; // [esp-8h] [ebp-138h]
  int v12; // [esp+14h] [ebp-11Ch]
  int v13; // [esp+54h] [ebp-DCh]
  int v14; // [esp+7Ch] [ebp-B4h]
  int v15; // [esp+80h] [ebp-B0h]
  _BYTE v16[4]; // [esp+D8h] [ebp-58h] BYREF
  int v17; // [esp+DCh] [ebp-54h]
  _BYTE v18[8]; // [esp+E0h] [ebp-50h] BYREF
  _BYTE v19[8]; // [esp+E8h] [ebp-48h] BYREF
  int v20; // [esp+F0h] [ebp-40h] BYREF
  int v21; // [esp+F4h] [ebp-3Ch]
  _BYTE v22[8]; // [esp+F8h] [ebp-38h] BYREF
  int v23; // [esp+100h] [ebp-30h]
  int v24; // [esp+104h] [ebp-2Ch]
  int v25; // [esp+108h] [ebp-28h]
  int v26; // [esp+10Ch] [ebp-24h]
  _BYTE v27[8]; // [esp+110h] [ebp-20h] BYREF
  int v28; // [esp+118h] [ebp-18h]
  int v29; // [esp+11Ch] [ebp-14h]
  int v30; // [esp+120h] [ebp-10h]
  int v31; // [esp+124h] [ebp-Ch]
  int v32; // [esp+128h] [ebp-8h] BYREF
  int v33; // [esp+12Ch] [ebp-4h]

  sub_65DB10(*(_DWORD *)(this + 24), this + 4);
  sub_65DB10(**(_DWORD **)(this + 24), this + 4);
  sub_5D48D0(&v32, v28, v29, v30, v31, &a2);
  sub_65DB10(*(_DWORD *)(this + 24), this + 4);
  result = (unsigned __int8)sub_632700(v27) == 0;
  if ( (_BYTE)result )
  {
    if ( (unsigned __int8)sub_632700(this + 44) )
    {
      v14 = *(_DWORD *)(this + 44);
      v15 = *(_DWORD *)(this + 48);
      sub_5D3980(this + 44);
      v25 = v14;
      v26 = v15;
      *(_BYTE *)(this + 40) = 1;
    }
    v23 = v32;
    v24 = v33;
    sub_5D1840(v22, v32, v33);
    v13 = *(_DWORD *)(this + 24);
    sub_4086A0();
    v4 = v13;
    v21 = v13;
    if ( this == -4 )
      _invalid_parameter_noinfo();
    sub_408680(v4, &v20);
    v10 = v21;
    v9 = v20;
    v5 = (_DWORD *)sub_62EFA0(v19);
    v6 = (int *)sub_5D48D0(v18, *v5, v5[1], v9, v10, &a3);
    v7 = v6[1];
    v32 = *v6;
    v33 = v7;
    v12 = *(_DWORD *)(this + 24);
    sub_4086A0();
    v17 = v12;
    v8 = 1;
    if ( this == -4 )
      _invalid_parameter_noinfo();
    sub_408680(v8, v16);
    if ( !(unsigned __int8)sub_632700(v16) )
      sub_5D3980(&v32);
    sub_5C0FD0(&v32, a2);
    return (*(int (__thiscall **)(int))(*(_DWORD *)a2 + 120))(a2);
  }
  return result;
}

