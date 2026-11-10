int __usercall sub_450020@<eax>(void *a1@<edi>, int a2@<esi>, int a3)
{
  int v3; // eax

  sub_4032C0(a1, strlen((const char *)a1));
  v3 = sub_403060(a3, 0, 0xFFFFFFFF);
  *(_DWORD *)(a2 + 24) = 15;
  *(_DWORD *)(a2 + 20) = 0;
  *(_BYTE *)(a2 + 4) = 0;
  sub_401D80(v3, 0, -1);
  return a2;
}

