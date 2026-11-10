int __thiscall sub_5C5110(_DWORD *this, int a2, int a3, int a4, int a5)
{
  int result; // eax

  if ( this[16] != a2 || this[17] != a3 || this[18] != a4 || (result = (int)this, this[19] != a5) )
  {
    (*(void (__thiscall **)(_DWORD *))(*this + 76))(this);
    this[16] = a2;
    this[17] = a3;
    this[18] = a4;
    this[19] = a5;
    (*(void (__thiscall **)(_DWORD *))(*this + 68))(this);
    result = (int)this;
    if ( this[8] )
      return sub_5C41E0(this[8]);
  }
  return result;
}

