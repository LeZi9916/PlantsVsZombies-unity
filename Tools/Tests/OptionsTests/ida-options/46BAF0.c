int __thiscall sub_46BAF0(_DWORD *this)
{
  void (__thiscall ***v2)(_DWORD, int); // ecx

  *this = &off_7117E0;
  this[40] = &off_718C68;
  this[97] = &off_711918;
  v2 = (void (__thiscall ***)(_DWORD, int))this[99];
  if ( v2 )
    (**v2)(v2, 1);
  return sub_464AB0(this);
}

