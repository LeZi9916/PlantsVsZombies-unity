int __thiscall sub_5C5370(int this, int a2)
{
  int result; // eax

  if ( a2 == 9 )
  {
    result = this;
    if ( *(_BYTE *)(*(_DWORD *)(this + 32) + 300) )
    {
      result = this;
      if ( *(_DWORD *)(this + 152) )
        return (*(int (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(this + 32) + 32))(
                 *(_DWORD *)(this + 32),
                 *(_DWORD *)(this + 152));
    }
    else if ( *(_DWORD *)(this + 156) )
    {
      return (*(int (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(this + 32) + 32))(
               *(_DWORD *)(this + 32),
               *(_DWORD *)(this + 156));
    }
  }
  return result;
}

