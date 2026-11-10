_DWORD *__thiscall sub_46A1C0(_DWORD *this, int a2)
{
  _DWORD *result; // eax
  int v3; // edx
  _DWORD *v4; // edx

  result = (_DWORD *)this[2];
  this[2] = *result;
  if ( !*(_BYTE *)(*result + 65) )
    *(_DWORD *)(*result + 4) = this;
  result[1] = this[1];
  v3 = *(_DWORD *)(a2 + 24);
  if ( this == *(_DWORD **)(v3 + 4) )
  {
    *(_DWORD *)(v3 + 4) = result;
    *result = this;
    this[1] = result;
  }
  else
  {
    v4 = (_DWORD *)this[1];
    if ( this == (_DWORD *)*v4 )
      *v4 = result;
    else
      v4[2] = result;
    *result = this;
    this[1] = result;
  }
  return result;
}

