int __thiscall sub_5D0A80(int this)
{
  int result; // eax

  unknown_libname_30(this);
  result = this;
  if ( *(_DWORD *)(this + 172) )
    result = (*(int (__thiscall **)(_DWORD, _DWORD, int))(**(_DWORD **)(this + 32) + 56))(
               *(_DWORD *)(this + 32),
               *(_DWORD *)(this + 172),
               this);
  if ( *(_DWORD *)(this + 176) )
    return (*(int (__thiscall **)(_DWORD, _DWORD, int))(**(_DWORD **)(this + 32) + 56))(
             *(_DWORD *)(this + 32),
             *(_DWORD *)(this + 176),
             this);
  return result;
}

