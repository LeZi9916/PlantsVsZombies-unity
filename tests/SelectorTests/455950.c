int __thiscall sub_455950(_DWORD **this, int a2)
{
  int result; // eax

  result = (*(int (__thiscall **)(_DWORD *, int))(*this[41] + 292))(this[41], 4);
  if ( !result )
  {
    result = (*(int (__thiscall **)(_DWORD *, int))(*this[41] + 292))(this[41], 3);
    if ( !result )
    {
      sub_458090(this);
      return sub_458450();
    }
  }
  return result;
}
