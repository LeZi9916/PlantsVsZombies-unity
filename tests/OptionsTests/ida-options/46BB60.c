int __thiscall sub_46BB60(_DWORD *this, int a2)
{
  sub_5D09C0(a2);
  (*(void (__thiscall **)(_DWORD *, _DWORD))(*this + 12))(this, this[92]);
  if ( this[94] )
    (*(void (__thiscall **)(_DWORD *, _DWORD))(*this + 12))(this, this[94]);
  if ( this[95] )
    (*(void (__thiscall **)(_DWORD *, _DWORD))(*this + 12))(this, this[95]);
  (*(void (__thiscall **)(_DWORD *, _DWORD))(*this + 12))(this, this[99]);
  return (*(int (__thiscall **)(int, _DWORD))(*(_DWORD *)a2 + 32))(a2, this[99]);
}

