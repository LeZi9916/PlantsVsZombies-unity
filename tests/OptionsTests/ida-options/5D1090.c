int __thiscall sub_5D1090(int this, int a2, int a3, int a4)
{
  sub_5CE470((_DWORD *)this, a3, a4);
  *(_DWORD *)this = &Sexy::DialogButton::`vftable';
  *(_DWORD *)(this + 312) = a2;
  *(_DWORD *)(this + 328) = 0;
  *(_DWORD *)(this + 324) = 0;
  *(_DWORD *)(this + 320) = 1;
  *(_DWORD *)(this + 316) = 1;
  *(_BYTE *)(this + 148) = 1;
  sub_5C4D90((void *)this, (int)&unk_747B20, 6);
  return this;
}

