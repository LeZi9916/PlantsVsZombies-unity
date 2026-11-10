__int64 __thiscall sub_5C05F0(_DWORD *this, unsigned __int8 a2, unsigned __int8 a3)
{
  __int64 result; // rax
  int v4; // [esp+0h] [ebp-28h]
  int v6; // [esp+8h] [ebp-20h]
  char v7[8]; // [esp+14h] [ebp-14h] BYREF
  int (__thiscall ***v8)(_DWORD, int); // [esp+1Ch] [ebp-Ch]
  int (__thiscall ***v9)(_DWORD, int); // [esp+20h] [ebp-8h]
  int (__thiscall ***v10)(_DWORD, int); // [esp+24h] [ebp-4h]

  while ( 1 )
  {
    LODWORD(result) = this;
    HIDWORD(result) = this[7] == 0;
    if ( !this[7] )
      break;
    v6 = sub_62EFA0(v7);
    v10 = *(int (__thiscall ****)(_DWORD, int))sub_5B7240(v6, v4);
    (*(void (__thiscall **)(_DWORD *, int (__thiscall ***)(_DWORD, int)))(*this + 16))(this, v10);
    if ( a3 )
      ((void (__thiscall *)(int (__thiscall ***)(_DWORD, int), _DWORD, _DWORD))(*v10)[7])(v10, a2, a3);
    if ( a2 )
    {
      v8 = v10;
      v9 = v10;
      if ( v10 )
        v4 = (**v9)(v9, 1);
      else
        v4 = 0;
    }
  }
  return result;
}

