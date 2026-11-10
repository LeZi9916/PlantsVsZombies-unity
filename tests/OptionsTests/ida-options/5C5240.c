int __thiscall sub_5C5240(_BYTE *this, unsigned __int8 a2)
{
  int result; // eax

  result = (int)this;
  if ( (unsigned __int8)this[102] != a2 )
  {
    this[102] = a2;
    if ( a2 && *((_DWORD *)this + 8) )
      (*(void (__thiscall **)(_DWORD, _BYTE *))(**((_DWORD **)this + 8) + 24))(*((_DWORD *)this + 8), this);
    result = (*(int (__thiscall **)(_BYTE *))(*(_DWORD *)this + 68))(this);
    if ( !a2 && *((_DWORD *)this + 8) )
    {
      result = (*(int (__thiscall **)(_BYTE *, _DWORD, _DWORD))(*(_DWORD *)this + 272))(
                 this,
                 *(_DWORD *)(*((_DWORD *)this + 8) + 264),
                 *(_DWORD *)(*((_DWORD *)this + 8) + 268));
      if ( (_BYTE)result )
        return sub_5C40E0(*(_DWORD *)(*((_DWORD *)this + 8) + 264), *(_DWORD *)(*((_DWORD *)this + 8) + 268));
    }
  }
  return result;
}

