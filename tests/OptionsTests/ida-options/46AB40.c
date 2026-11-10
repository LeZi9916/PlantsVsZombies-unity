int __thiscall sub_46AB40(_DWORD *this)
{
  void (__thiscall ***v2)(_DWORD, int); // ecx
  void (__thiscall ***v3)(_DWORD, int); // ecx
  void (__thiscall ***v4)(_DWORD, int); // ecx
  void (__thiscall ***v5)(_DWORD, int); // ecx
  void (__thiscall ***v6)(_DWORD, int); // ecx
  void (__thiscall ***v7)(_DWORD, int); // ecx
  void (__thiscall ***v8)(_DWORD, int); // ecx
  void (__thiscall ***v9)(_DWORD, int); // ecx

  *this = &off_711630;
  this[40] = &off_711760;
  this[90] = &off_71177C;
  this[91] = &off_711780;
  v2 = (void (__thiscall ***)(_DWORD, int))this[93];
  if ( v2 )
    (**v2)(v2, 1);
  v3 = (void (__thiscall ***)(_DWORD, int))this[94];
  if ( v3 )
    (**v3)(v3, 1);
  v4 = (void (__thiscall ***)(_DWORD, int))this[95];
  if ( v4 )
    (**v4)(v4, 1);
  v5 = (void (__thiscall ***)(_DWORD, int))this[96];
  if ( v5 )
    (**v5)(v5, 1);
  v6 = (void (__thiscall ***)(_DWORD, int))this[97];
  if ( v6 )
    (**v6)(v6, 1);
  v7 = (void (__thiscall ***)(_DWORD, int))this[99];
  if ( v7 )
    (**v7)(v7, 1);
  v8 = (void (__thiscall ***)(_DWORD, int))this[98];
  if ( v8 )
    (**v8)(v8, 1);
  v9 = (void (__thiscall ***)(_DWORD, int))this[100];
  if ( v9 )
    (**v9)(v9, 1);
  return sub_5CFD40();
}

