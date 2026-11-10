int __thiscall sub_5C1E20(int this)
{
  int result; // eax

  result = this;
  if ( *(_DWORD *)(this + 36) )
    return (*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(this + 36) + 72))(*(_DWORD *)(this + 36), this);
  *(_BYTE *)(this + 60) = 1;
  return result;
}

