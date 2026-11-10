int __thiscall sub_5CE180(_BYTE *this, char a2, char a3)
{
  this[168] = a2;
  if ( a3 && *((_DWORD *)this + 40) )
    (***((void (__thiscall ****)(_DWORD, _DWORD, _DWORD))this + 40))(
      *((_DWORD *)this + 40),
      *((_DWORD *)this + 41),
      (unsigned __int8)this[168]);
  return (*(int (__thiscall **)(_BYTE *))(*(_DWORD *)this + 68))(this);
}

