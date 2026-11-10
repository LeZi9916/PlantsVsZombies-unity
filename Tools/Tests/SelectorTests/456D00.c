int __userpurge sub_456D00@<eax>(int a1@<eax>, int a2@<edi>, char *String2, float a4, float a5)
{
  int v5; // esi
  int result; // eax

  v5 = **(_DWORD **)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2372) + 8) + 160 * (unsigned __int16)*(_DWORD *)(a1 + 252);
  sub_4823E0(String2);
  sub_4812C0(v5);
  *(_DWORD *)(a2 + 64) = (int)((float)-10000.0 + a4);
  result = (int)((float)-10000.0 + a5);
  *(_DWORD *)(a2 + 68) = result;
  return result;
}
