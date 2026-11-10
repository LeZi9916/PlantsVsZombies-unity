int __thiscall sub_46B1D0(int this, int a2, double a3)
{
  int result; // eax

  if ( a2 == 4 )
  {
    (*(void (__thiscall **)(_DWORD, _DWORD, _DWORD))(**(_DWORD **)(this + 8) + 240))(
      *(_DWORD *)(this + 8),
      LODWORD(a3),
      HIDWORD(a3));
    return sub_52AA10();
  }
  else
  {
    result = a2 - 5;
    if ( a2 == 5 )
    {
      (*(void (__thiscall **)(_DWORD, _DWORD, _DWORD))(**(_DWORD **)(this + 8) + 244))(
        *(_DWORD *)(this + 8),
        COERCE_UNSIGNED_INT64(a3 * 0.6499999761581421),
        HIDWORD(COERCE_UNSIGNED_INT64(a3 * 0.6499999761581421)));
      result = sub_52AA10();
      if ( !*(_BYTE *)(*(_DWORD *)(this + 16) + 188) )
        return (*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(this + 8) + 216))(*(_DWORD *)(this + 8), dword_753038);
    }
  }
  return result;
}

