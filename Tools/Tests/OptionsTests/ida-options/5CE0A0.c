int __thiscall sub_5CE0A0(void *this, int a2, int a3, int a4, int a5)
{
  int v6; // [esp+0h] [ebp-4h]

  sub_5C4970(this);
  *(_DWORD *)v6 = &Sexy::Checkbox::`vftable';
  *(_DWORD *)(v6 + 160) = a5;
  *(_DWORD *)(v6 + 164) = a4;
  *(_BYTE *)(v6 + 168) = 0;
  *(_DWORD *)(v6 + 172) = a2;
  *(_DWORD *)(v6 + 176) = a3;
  sub_409210();
  sub_409210();
  *(_DWORD *)(v6 + 212) = dword_7DCA98;
  *(_DWORD *)(v6 + 216) = dword_7DCA9C;
  *(_DWORD *)(v6 + 220) = dword_7DCAA0;
  *(_DWORD *)(v6 + 224) = dword_7DCAA4;
  sub_55BBB0(80, 80, 80);
  sub_55BBB0(255, 255, 0);
  *(_BYTE *)(v6 + 148) = 1;
  return v6;
}

