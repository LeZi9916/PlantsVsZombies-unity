int __thiscall sub_5C5600(
        _DWORD *this,
        int a2,
        int a3,
        int a4,
        int a5,
        char a6,
        int a7,
        int a8,
        int a9,
        char a10,
        int a11,
        int a12,
        int a13,
        int *a14)
{
  int v14; // esi
  int v16; // [esp-8h] [ebp-24h]
  int v17; // [esp-4h] [ebp-20h]
  int v19; // [esp+14h] [ebp-8h]
  int v20; // [esp+18h] [ebp-4h]

  v20 = unknown_libname_8(a3);
  v19 = (*(int (__thiscall **)(int, int))(*(_DWORD *)v20 + 28))(v20, a5);
  sub_592880(&a10);
  sub_592F40(a5, *a14 + (this[18] - v19) / 2, a14[1] + a4);
  sub_592880(&a6);
  sub_592F40(a5, (this[18] - v19) / 2, a4);
  v14 = (*(int (__thiscall **)(int))(*(_DWORD *)v20 + 16))(v20);
  v17 = abs(a14[1]) + v14;
  v16 = v19 + abs(*a14);
  (*(void (__thiscall **)(int))(*(_DWORD *)v20 + 4))(v20);
  sub_4091F0(v16, v17);
  return a2;
}

