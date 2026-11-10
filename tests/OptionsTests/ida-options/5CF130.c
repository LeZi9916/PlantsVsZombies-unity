int __thiscall sub_5CF130(void *this)
{
  int v1; // eax
  int v3; // [esp-4h] [ebp-10h]

  unknown_libname_30();
  if ( 0.0 == *((double *)this + 38) && *((double *)this + 36) > 0.0 )
    *((double *)this + 36) = 0.0;
  if ( *((_BYTE *)this + 104)
    || sub_5CE720(*((_DWORD *)this + 51), (int)this + 232)
    || (v3 = sub_43B0D0(), v1 = sub_43B0D0(), (unsigned __int8)sub_55BCF0(v1, v3)) )
  {
    (*(void (__thiscall **)(void *))(*(_DWORD *)this + 68))(this);
  }
  return (*(int (__thiscall **)(_DWORD, _DWORD))(**((_DWORD **)this + 71) + 16))(
           *((_DWORD *)this + 71),
           *((_DWORD *)this + 40));
}

