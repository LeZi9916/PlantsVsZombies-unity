int __cdecl sub_5F0020(int a1)
{
  char *v2; // [esp-4h] [ebp-130h]
  char DstBuf[260]; // [esp+24h] [ebp-108h] BYREF

  v2 = _getcwd(DstBuf, 256);
  sub_4033F0(v2);
  return a1;
}

