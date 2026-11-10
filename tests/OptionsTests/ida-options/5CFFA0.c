int __thiscall sub_5CFFA0(_DWORD **this, int a2)
{
  int result; // eax

  result = (int)this;
  if ( this[43] )
    result = (*(int (__thiscall **)(_DWORD *, int))(*this[43] + 284))(this[43], a2);
  if ( this[44] )
    return (*(int (__thiscall **)(_DWORD *, int))(*this[44] + 284))(this[44], a2);
  return result;
}

