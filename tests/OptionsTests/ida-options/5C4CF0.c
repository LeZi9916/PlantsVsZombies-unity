int __thiscall sub_5C4CF0(_BYTE *this, unsigned __int8 a2)
{
  int result; // eax

  result = (int)this;
  if ( (unsigned __int8)this[100] != a2 )
  {
    this[100] = a2;
    if ( this[100] )
      result = (*(int (__thiscall **)(_BYTE *))(*(_DWORD *)this + 68))(this);
    else
      result = (*(int (__thiscall **)(_BYTE *))(*(_DWORD *)this + 76))(this);
    if ( *((_DWORD *)this + 8) )
      return sub_5C41E0(*((_DWORD *)this + 8));
  }
  return result;
}

