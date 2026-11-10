int __thiscall sub_4574B0(int this, int a2)
{
  int result; // eax
  double v3; // st7
  float v4; // [esp+Ch] [ebp+4h]

  result = a2;
  if ( (!*(_BYTE *)(this + 80) || a2 != 101)
    && (!*(_BYTE *)(this + 81) || a2 != 102)
    && (!*(_BYTE *)(this + 82) || a2 != 110)
    && !*(_BYTE *)(*(_DWORD *)(this + 4) + 2537) )
  {
    v4 = 0.0;
    if ( 0.0 != *(float *)(dword_755E50 + 524) )
    {
      v3 = *(float *)(dword_755E50 + 524);
      sub_5EECE0(*(float *)(dword_755E50 + 524));
      v4 = v3;
    }
    return sub_52A5A0(v4);
  }
  return result;
}
