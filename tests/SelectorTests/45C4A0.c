int __thiscall sub_45C4A0(_DWORD **this)
{
  void *v2; // eax
  _DWORD *v3; // eax

  sub_45BED0();
  if ( this[540] )
  {
    (*(void (__thiscall **)(_DWORD *, _DWORD *))(*this[200] + 16))(this[200], this[540]);
    ((void (__thiscall *)(_DWORD **, _DWORD *))(*this)[34])(this, this[540]);
  }
  this[584] = (_DWORD *)1;
  v2 = operator new(0x178u);
  if ( v2 )
    v3 = (_DWORD *)sub_4539E0(v2, this);
  else
    v3 = 0;
  this[540] = v3;
  (*(void (__thiscall **)(_DWORD *, _DWORD, _DWORD, int, int))(*v3 + 160))(v3, 0, 0, 2400, 3000);
  (*(void (__thiscall **)(_DWORD *, _DWORD *))(*this[200] + 12))(this[200], this[540]);
  (*(void (__thiscall **)(_DWORD *, _DWORD *))(*this[200] + 48))(this[200], this[540]);
  return (*(int (__thiscall **)(_DWORD *, _DWORD *))(*this[200] + 32))(this[200], this[540]);
}
