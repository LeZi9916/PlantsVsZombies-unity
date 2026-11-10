int __thiscall sub_46AD90(int this, int a2, int a3, int a4, int a5)
{
  int result; // eax

  sub_5D0AF0(a2, a3, a4, a5);
  (*(void (__thiscall **)(_DWORD, int, int, int, int))(**(_DWORD **)(this + 372) + 160))(
    *(_DWORD *)(this + 372),
    199,
    116,
    135,
    40);
  (*(void (__thiscall **)(_DWORD, int, int, int, int))(**(_DWORD **)(this + 376) + 160))(
    *(_DWORD *)(this + 376),
    199,
    143,
    135,
    40);
  (*(void (__thiscall **)(_DWORD, int, int, int, int))(**(_DWORD **)(this + 384) + 160))(
    *(_DWORD *)(this + 384),
    283,
    175,
    46,
    45);
  (*(void (__thiscall **)(_DWORD, int, int, int, int))(**(_DWORD **)(this + 380) + 160))(
    *(_DWORD *)(this + 380),
    284,
    206,
    46,
    45);
  (*(void (__thiscall **)(_DWORD, int, int, int, int))(**(_DWORD **)(this + 388) + 160))(
    *(_DWORD *)(this + 388),
    107,
    241,
    209,
    46);
  (*(void (__thiscall **)(_DWORD, _DWORD, int, int, int))(**(_DWORD **)(this + 396) + 160))(
    *(_DWORD *)(this + 396),
    *(_DWORD *)(*(_DWORD *)(this + 388) + 64),
    *(_DWORD *)(*(_DWORD *)(this + 388) + 68) + 43,
    209,
    46);
  (*(void (__thiscall **)(_DWORD, _DWORD, int, int, int))(**(_DWORD **)(this + 392) + 160))(
    *(_DWORD *)(this + 392),
    *(_DWORD *)(*(_DWORD *)(this + 396) + 64),
    *(_DWORD *)(*(_DWORD *)(this + 396) + 68) + 43,
    209,
    46);
  (*(void (__thiscall **)(_DWORD, int, int, _DWORD, _DWORD))(**(_DWORD **)(this + 400) + 160))(
    *(_DWORD *)(this + 400),
    30,
    381,
    *(_DWORD *)(*(_DWORD *)(this + 400) + 72),
    *(_DWORD *)(*(_DWORD *)(this + 400) + 76));
  if ( *(_BYTE *)(this + 404) )
  {
    *(_DWORD *)(*(_DWORD *)(this + 372) + 68) += 5;
    *(_DWORD *)(*(_DWORD *)(this + 376) + 68) += 10;
    *(_DWORD *)(*(_DWORD *)(this + 384) + 68) += 15;
    *(_DWORD *)(*(_DWORD *)(this + 380) + 68) += 20;
  }
  result = *(_DWORD *)(*(_DWORD *)(this + 368) + 2332);
  if ( result == 43 || result == 50 )
    *(_DWORD *)(*(_DWORD *)(this + 388) + 68) += 43;
  return result;
}

