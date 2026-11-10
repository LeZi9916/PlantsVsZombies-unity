int __thiscall sub_5CE670(int (__thiscall ****this)(_DWORD, _DWORD), int a2)
{
  int result; // eax
  int v3; // [esp+0h] [ebp-10h]

  if ( this[49] )
  {
    v3 = (**this[49])(this[49], 1);
    result = (*(int (__thiscall **)(int, int))(*(_DWORD *)a2 + 44))(a2, v3);
  }
  else
  {
    result = (*(int (__thiscall **)(int, _DWORD))(*(_DWORD *)a2 + 44))(a2, 0);
  }
  this[49] = (int (__thiscall ***)(_DWORD, _DWORD))result;
  return result;
}

