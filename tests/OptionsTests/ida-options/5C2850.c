int *__thiscall sub_5C2850(_DWORD *this, int *a2, int a3)
{
  int *result; // eax
  int v4; // ecx
  int v5; // [esp+0h] [ebp-D8h]
  int v6; // [esp+4h] [ebp-D4h]
  int v7; // [esp+8h] [ebp-D0h]
  int v8; // [esp+Ch] [ebp-CCh]
  int v10; // [esp+1Ch] [ebp-BCh]
  char v11[4]; // [esp+34h] [ebp-A4h] BYREF
  int v12; // [esp+38h] [ebp-A0h]
  _BYTE v13[120]; // [esp+3Ch] [ebp-9Ch] BYREF
  int v14; // [esp+B4h] [ebp-24h]
  int *v15; // [esp+B8h] [ebp-20h]
  int *v16; // [esp+BCh] [ebp-1Ch]
  int v17; // [esp+C0h] [ebp-18h]
  _BYTE v18[8]; // [esp+C4h] [ebp-14h] BYREF
  int v19; // [esp+D4h] [ebp-4h]

  if ( this[23] > *(_DWORD *)(this[8] + 152) )
    sub_5C3160(this[23]);
  v15 = a2;
  v16 = (int *)*a2;
  v17 = a2[1];
  *a2 = ~this[22] & (this[21] | *a2);
  a2[1] = ~this[22] & (this[21] | a2[1]);
  v19 = 0;
  if ( *((_BYTE *)this + 81) )
  {
    v8 = *((_BYTE *)a2 + 8) ? *a2 : a2[1];
    if ( (v8 & 8) != 0 )
      sub_5939F0(0, 0, this[18], this[19]);
  }
  if ( this[7] )
  {
    if ( *((_BYTE *)a2 + 8) )
      v6 = *a2;
    else
      v6 = a2[1];
    if ( (v6 & 4) != 0 )
    {
      sub_592700(a3);
      (*(void (__thiscall **)(_DWORD *, int))(*this + 104))(this, a3);
      sub_592780(a3);
    }
    sub_62EFA0(v18);
    while ( 1 )
    {
      v10 = this[6];
      sub_4086A0();
      v12 = v10;
      if ( this == (_DWORD *)-4 )
        _invalid_parameter_noinfo();
      sub_408680(v4, v11);
      if ( (unsigned __int8)sub_632700(v11) )
        break;
      v14 = *(_DWORD *)sub_5B7240(v18, v5);
      if ( *(_BYTE *)(v14 + 100) )
      {
        if ( v14 == *(_DWORD *)(this[8] + 172) )
          *((_BYTE *)a2 + 8) = 1;
        sub_5924D0(a3);
        LOBYTE(v19) = 1;
        sub_593AA0(*(_DWORD *)(v14 + 64), *(_DWORD *)(v14 + 68));
        (*(void (__thiscall **)(int, int *, _BYTE *))(*(_DWORD *)v14 + 108))(v14, a2, v13);
        *(_BYTE *)(v14 + 60) = 0;
        LOBYTE(v19) = 0;
        sub_5926D0(v13);
      }
      sub_5D3980(v18);
    }
    result = v16;
    *v15 = (int)v16;
    v15[1] = v17;
  }
  else
  {
    if ( *((_BYTE *)a2 + 8) )
      v7 = *a2;
    else
      v7 = a2[1];
    if ( (v7 & 4) != 0 )
      (*(void (__thiscall **)(_DWORD *, int))(*this + 104))(this, a3);
    v19 = -1;
    *v15 = (int)v16;
    result = v15;
    v15[1] = v17;
  }
  return result;
}

