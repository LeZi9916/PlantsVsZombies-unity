int __thiscall sub_5C5DF0(void *this, int a2, int a3, int a4, int a5)
{
  int v6; // [esp+0h] [ebp-4h]

  sub_5C4970(this);
  *(_DWORD *)v6 = &Sexy::Slider::`vftable';
  *(_DWORD *)(v6 + 160) = a5;
  *(double *)(v6 + 168) = 0.0;
  *(_DWORD *)(v6 + 176) = a4;
  *(_DWORD *)(v6 + 180) = a2;
  *(_DWORD *)(v6 + 184) = a3;
  *(_BYTE *)(v6 + 188) = 0;
  *(_BYTE *)(v6 + 200) = 1;
  *(_DWORD *)(v6 + 196) = 0;
  *(_DWORD *)(v6 + 192) = 0;
  return v6;
}

