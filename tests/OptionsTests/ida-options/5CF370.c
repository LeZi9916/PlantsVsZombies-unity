int __thiscall sub_5CF370(void *this, int a2, int a3, int a4, int a5)
{
  sub_5C54C0(a2, a3, a4, a5);
  (***((void (__thiscall ****)(_DWORD, _DWORD, int))this + 71))(*((_DWORD *)this + 71), *((_DWORD *)this + 40), a5);
  return (*(int (__thiscall **)(void *))(*(_DWORD *)this + 68))(this);
}

