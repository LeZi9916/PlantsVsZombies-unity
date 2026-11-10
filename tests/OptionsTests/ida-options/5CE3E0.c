int __thiscall sub_5CE3E0(_BYTE *this, int a2, int a3, int a4, int a5)
{
  sub_5C54C0(a2, a3, a4, a5);
  this[168] = this[168] == 0;
  if ( *((_DWORD *)this + 40) )
    (***((void (__thiscall ****)(_DWORD, _DWORD, _DWORD))this + 40))(
      *((_DWORD *)this + 40),
      *((_DWORD *)this + 41),
      (unsigned __int8)this[168]);
  return (*(int (__thiscall **)(_BYTE *))(*(_DWORD *)this + 68))(this);
}

