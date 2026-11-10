char __thiscall sub_5CF0E0(int this, unsigned __int8 a2)
{
  char result; // al

  sub_5C5240((_BYTE *)this, a2);
  result = sub_5CE720(*(_DWORD *)(this + 212), this + 264);
  if ( result )
    return (*(int (__thiscall **)(int))(*(_DWORD *)this + 68))(this);
  return result;
}

