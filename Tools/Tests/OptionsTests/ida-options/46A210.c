int __thiscall sub_46A210(_DWORD *this, int a2)
{
  int result; // eax
  int v3; // edx
  int v4; // edx
  _DWORD *v5; // edx

  result = *this;
  *this = *(_DWORD *)(*this + 8);
  v3 = *(_DWORD *)(result + 8);
  if ( !*(_BYTE *)(v3 + 65) )
    *(_DWORD *)(v3 + 4) = this;
  *(_DWORD *)(result + 4) = this[1];
  v4 = *(_DWORD *)(a2 + 24);
  if ( this == *(_DWORD **)(v4 + 4) )
  {
    *(_DWORD *)(v4 + 4) = result;
    *(_DWORD *)(result + 8) = this;
    this[1] = result;
  }
  else
  {
    v5 = (_DWORD *)this[1];
    if ( this == (_DWORD *)v5[2] )
      v5[2] = result;
    else
      *v5 = result;
    *(_DWORD *)(result + 8) = this;
    this[1] = result;
  }
  return result;
}

