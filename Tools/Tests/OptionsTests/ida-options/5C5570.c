int __thiscall sub_5C5570(_DWORD *this, int a2, int a3, int a4, int a5)
{
  int v6; // [esp-4h] [ebp-14h]
  int v8; // [esp+8h] [ebp-8h]
  int v9; // [esp+Ch] [ebp-4h]

  v9 = unknown_libname_8(a3);
  v8 = (*(int (__thiscall **)(int, int))(*(_DWORD *)v9 + 28))(v9, a5);
  sub_592F40(a5, (this[18] - v8) / 2, a4);
  v6 = (*(int (__thiscall **)(int))(*(_DWORD *)v9 + 16))(v9);
  (*(void (__thiscall **)(int))(*(_DWORD *)v9 + 4))(v9);
  sub_4091F0(v8, v6);
  return a2;
}

