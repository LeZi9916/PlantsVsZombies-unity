int __thiscall sub_5D0D50(_DWORD *this, int a2, int a3)
{
  int result; // eax
  int v4; // [esp+4h] [ebp-8h]
  int v5; // [esp+8h] [ebp-4h]

  result = (int)this;
  if ( *((_BYTE *)this + 328) )
  {
    v4 = a2 + this[16] - this[83];
    v5 = a3 + this[17] - this[84];
    if ( v4 >= -8 )
    {
      if ( this[18] + v4 > *(_DWORD *)(this[8] + 72) + 8 )
        v4 = *(_DWORD *)(this[8] + 72) - this[18] + 8;
    }
    else
    {
      v4 = -8;
    }
    if ( v5 >= -8 )
    {
      if ( this[19] + v5 > *(_DWORD *)(this[8] + 76) + 8 )
        v5 = *(_DWORD *)(this[8] + 76) - this[19] + 8;
    }
    else
    {
      v5 = -8;
    }
    this[83] = a2 + this[16] - v4;
    this[84] = a3 + this[17] - v5;
    if ( (int)this[83] >= 8 )
    {
      if ( this[83] > this[18] - 9 )
        this[83] = this[18] - 9;
    }
    else
    {
      this[83] = 8;
    }
    if ( (int)this[84] >= 8 )
    {
      if ( this[84] > this[19] - 9 )
        this[84] = this[19] - 9;
    }
    else
    {
      this[84] = 8;
    }
    return (*(int (__thiscall **)(_DWORD *, int, int))(*this + 164))(this, v4, v5);
  }
  return result;
}

