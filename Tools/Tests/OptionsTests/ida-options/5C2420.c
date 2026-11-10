int __thiscall sub_5C2420(_DWORD *this, int *a2)
{
  int result; // eax
  int *v3; // eax
  int v4; // edx
  int v5; // eax
  int v6; // ecx
  int v7; // [esp+0h] [ebp-60h]
  int v8; // [esp+4h] [ebp-5Ch]
  int v9; // [esp+8h] [ebp-58h]
  int v11; // [esp+1Ch] [ebp-44h]
  _BYTE v12[4]; // [esp+30h] [ebp-30h] BYREF
  int v13; // [esp+34h] [ebp-2Ch]
  _BYTE v14[8]; // [esp+38h] [ebp-28h] BYREF
  int v15; // [esp+40h] [ebp-20h]
  int *v16; // [esp+44h] [ebp-1Ch]
  int v17; // [esp+48h] [ebp-18h]
  int v18; // [esp+4Ch] [ebp-14h]
  int v19; // [esp+50h] [ebp-10h]
  int v20; // [esp+5Ch] [ebp-4h]

  v16 = a2;
  v17 = *a2;
  v18 = a2[1];
  *a2 = ~this[22] & (this[21] | *a2);
  a2[1] = ~this[22] & (this[21] | a2[1]);
  v20 = 0;
  if ( *((_BYTE *)a2 + 8) )
    v9 = *a2;
  else
    v9 = a2[1];
  if ( (v9 & 2) != 0 )
    (*(void (__thiscall **)(_DWORD *))(*this + 68))(this);
  v19 = this[8];
  if ( v19 )
  {
    if ( *((_BYTE *)a2 + 8) )
      v8 = *a2;
    else
      v8 = a2[1];
    if ( (v8 & 1) != 0 && this[13] != *(_DWORD *)(this[8] + 56) )
    {
      this[13] = *(_DWORD *)(this[8] + 56);
      (*(void (__thiscall **)(_DWORD *))(*this + 88))(this);
    }
    v3 = (int *)sub_62EFA0(v14);
    v4 = *v3;
    v5 = v3[1];
    this[11] = v4;
    this[12] = v5;
    while ( 1 )
    {
      v11 = this[6];
      sub_4086A0();
      v13 = v11;
      if ( this == (_DWORD *)-4 )
        _invalid_parameter_noinfo();
      sub_408680(v6, v12);
      if ( (unsigned __int8)sub_632700(v12) )
        break;
      *((_BYTE *)this + 40) = 0;
      v15 = *(_DWORD *)sub_5B7240(this + 11, v7);
      if ( v15 == *(_DWORD *)(v19 + 172) )
        *((_BYTE *)a2 + 8) = 1;
      (*(void (__thiscall **)(int, int *))(*(_DWORD *)v15 + 92))(v15, a2);
      if ( !*((_BYTE *)this + 40) )
        sub_5D3980(this + 11);
    }
    *((_BYTE *)this + 40) = 1;
    result = v17;
    *v16 = v17;
    v16[1] = v18;
  }
  else
  {
    v20 = -1;
    *v16 = v17;
    result = v18;
    v16[1] = v18;
  }
  return result;
}

