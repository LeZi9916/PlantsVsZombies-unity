_DWORD *__thiscall sub_5CE470(_DWORD *this, int a2, int a3)
{
  int v4; // [esp+0h] [ebp-20h]

  sub_5C4970(v4);
  *this = &Sexy::ButtonWidget::`vftable';
  sub_401310(this + 41);
  sub_409210();
  sub_409210();
  sub_409210();
  sub_409210();
  this[40] = a2;
  this[49] = 0;
  this[48] = 0;
  this[50] = 0;
  this[51] = 0;
  this[52] = 0;
  this[53] = 0;
  *((_BYTE *)this + 280) = 0;
  *((_BYTE *)this + 281) = 0;
  *((_BYTE *)this + 282) = 0;
  this[71] = a3;
  *((_BYTE *)this + 80) = 1;
  *((double *)this + 36) = 0.0;
  *((double *)this + 37) = 0.0;
  *((double *)this + 38) = 0.0;
  sub_5C4D90(this, (int)&unk_747A80, 6);
  return this;
}

