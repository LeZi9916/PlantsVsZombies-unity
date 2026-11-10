int __thiscall sub_5CFEB0(_DWORD **this, int a2, int a3)
{
  int result; // eax

  result = sub_5C4F90(a2, a3);
  if ( a2 == 3 )
  {
    if ( this[43] )
      result = (*(int (__thiscall **)(_DWORD *, _DWORD, int))(*this[43] + 136))(this[43], 0, a3);
    if ( this[44] )
      return (*(int (__thiscall **)(_DWORD *, _DWORD, int))(*this[44] + 136))(this[44], 0, a3);
  }
  else if ( a2 == 4 )
  {
    if ( this[43] )
      result = (*(int (__thiscall **)(_DWORD *, int, int))(*this[43] + 136))(this[43], 1, a3);
    if ( this[44] )
      return (*(int (__thiscall **)(_DWORD *, int, int))(*this[44] + 136))(this[44], 1, a3);
  }
  return result;
}

