void (__thiscall ****__thiscall sub_5D1150(void (__thiscall ****this)(_DWORD, int), char a2))(_DWORD, int)
{
  sub_452710(this);
  if ( (a2 & 1) != 0 )
    operator delete(this);
  return this;
}

