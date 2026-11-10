int __stdcall sub_5C5820(int a1)
{
  int v2; // [esp+4h] [ebp-8h]
  int v3; // [esp+8h] [ebp-4h]

  v3 = 10;
  v2 = 1;
  while ( a1 >= v3 )
  {
    ++v2;
    v3 *= 10;
  }
  return v2;
}

