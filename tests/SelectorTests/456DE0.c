char __thiscall sub_456DE0(int *this, char a2)
{
  int v3; // eax
  double v4; // st7
  int v5; // ecx
  char v6; // al
  int v7; // edi
  double v8; // st7
  double v9; // st7
  int v11[13]; // [esp-18h] [ebp-34h] BYREF
  float v12; // [esp+20h] [ebp+4h]
  float v13; // [esp+20h] [ebp+4h]
  float v14; // [esp+20h] [ebp+4h]

  LOBYTE(v3) = sub_531F80(*(_DWORD *)(this[41] + 2484), a2);
  if ( (_BYTE)v3 )
  {
    if ( !*(_BYTE *)(this[41] + 2537) )
    {
      v12 = 0.0;
      if ( 0.0 != *(float *)(dword_755E50 + 472) )
      {
        v4 = *(float *)(dword_755E50 + 472);
        sub_5EECE0(*(float *)(dword_755E50 + 472));
        v12 = v4;
      }
      LOBYTE(v3) = sub_52A5A0(v12);
    }
    return v3;
  }
  if ( (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2488), a2)
    || (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2492), a2) )
  {
    if ( !*(_BYTE *)(this[41] + 2537) )
    {
      v14 = 0.0;
      if ( 0.0 != *(float *)(dword_755E50 + 4060) )
      {
        v9 = *(float *)(dword_755E50 + 4060);
        sub_5EECE0(*(float *)(dword_755E50 + 4060));
        v14 = v9;
      }
      sub_52A5A0(v14);
    }
    *(_BYTE *)(this[41] + 2524) = *(_BYTE *)(this[41] + 2524) == 0;
    v11[11] = 19;
    v11[12] = (int)v11;
    sub_463B60();
    LOBYTE(v3) = sub_463910(this[41], v11[0], (void *)v11[1], v11[2], v11[3], v11[4], v11[5], v11[6]);
  }
  else if ( (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2496), a2)
         || (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2500), a2) )
  {
    v3 = this[41];
    if ( !*(_BYTE *)(v3 + 2537) )
    {
      v13 = 0.0;
      if ( 0.0 != *(float *)(dword_755E50 + 836) )
      {
        v8 = *(float *)(dword_755E50 + 836);
        sub_5EECE0(*(float *)(dword_755E50 + 836));
        v13 = v8;
      }
      LOBYTE(v3) = sub_52A5A0(v13);
    }
    *(_BYTE *)(this[41] + 2525) = *(_BYTE *)(this[41] + 2525) == 0;
  }
  else
  {
    if ( (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2504), a2) )
    {
      LOBYTE(v3) = (*(int (__thiscall **)(int, int))(*(_DWORD *)this[41] + 216))(this[41], dword_752BB8);
      *(_BYTE *)(this[41] + 2526) = *(_BYTE *)(this[41] + 2526) == 0;
      return v3;
    }
    if ( (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2508), a2) )
    {
      if ( (unsigned __int8)sub_463840() )
      {
        LOBYTE(v3) = sub_460A20();
        *(_BYTE *)(this[41] + 2527) = *(_BYTE *)(this[41] + 2527) == 0;
        return v3;
      }
LABEL_23:
      LOBYTE(v3) = (*(int (__thiscall **)(int, int))(*(_DWORD *)v5 + 216))(v5, dword_753850);
      return v3;
    }
    if ( (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2512), a2) )
    {
      if ( (unsigned __int8)sub_463860() )
      {
        LOBYTE(v3) = sub_460A20();
        *(_BYTE *)(this[41] + 2528) = *(_BYTE *)(this[41] + 2528) == 0;
        return v3;
      }
      goto LABEL_23;
    }
    if ( (unsigned __int8)sub_531F80(*(_DWORD *)(this[41] + 2516), a2) )
    {
      v6 = sub_463880();
      v5 = this[41];
      if ( v6 )
      {
        LOBYTE(v3) = (*(int (__thiscall **)(int, int))(*(_DWORD *)v5 + 216))(v5, dword_7531C4);
        *(_BYTE *)(this[41] + 2529) = *(_BYTE *)(this[41] + 2529) == 0;
        return v3;
      }
      goto LABEL_23;
    }
    LOBYTE(v3) = sub_531F80(*(_DWORD *)(this[41] + 2520), a2);
    if ( (_BYTE)v3 )
    {
      (*(void (__thiscall **)(int, int))(*(_DWORD *)this[41] + 216))(this[41], dword_753244);
      v7 = this[41];
      LOBYTE(v3) = *(_BYTE *)(v7 + 2530) == 0;
      *(_BYTE *)(v7 + 2530) = v3;
    }
  }
  return v3;
}
