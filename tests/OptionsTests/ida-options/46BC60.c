int __thiscall sub_46BC60(_DWORD *this, int a2)
{
  _DWORD *v3; // eax
  int v4; // edx
  int v5; // ecx
  int v6; // edx
  _DWORD v8[4]; // [esp+8h] [ebp-10h] BYREF

  sub_4651D0(a2);
  v3 = (_DWORD *)this[99];
  v4 = v3[17];
  v8[0] = v3[16] - 8;
  v5 = v3[18];
  v8[1] = v4 - 4;
  v6 = v3[19];
  v8[2] = v5 + 16;
  v8[3] = v6 + 8;
  return sub_593B10(v8, (Concurrency::details::ExecutionResource *)dword_75346C);
}

