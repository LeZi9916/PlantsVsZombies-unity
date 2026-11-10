int __thiscall sub_5CF3D0(_DWORD **this, int a2, int a3, int a4, int a5)
{
  sub_5C54C0(a2, a3, a4, a5);
  if ( *((_BYTE *)this + 105) && *((_BYTE *)this[8] + 156) )
    (*(void (__thiscall **)(_DWORD *, _DWORD *))(*this[71] + 8))(this[71], this[40]);
  return ((int (__thiscall *)(_DWORD **))(*this)[17])(this);
}

