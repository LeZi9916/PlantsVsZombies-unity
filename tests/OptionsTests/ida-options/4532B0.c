void __thiscall sub_4532B0(int this, int a2)
{
  bool v2; // al

  if ( !*(_BYTE *)(this + 281) )
  {
    v2 = *(_BYTE *)(this + 104) && *(_BYTE *)(this + 105) && !*(_BYTE *)(this + 102);
    sub_452720(
      0,
      *(_DWORD *)(this + 72),
      *(_DWORD *)(this + 76),
      v2 ^ *(_BYTE *)(this + 280),
      *(_BYTE *)(this + 105),
      this + 164);
  }
}

