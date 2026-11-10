int __userpurge sub_52FFD0@<eax>(int a1@<esi>, int a2)
{
  int v2; // eax
  void *v4; // [esp+Ch] [ebp-24h]
  unsigned int v5; // [esp+20h] [ebp-10h]

  v2 = sub_52EA80(a2);
  sub_40D0E0(v2, 0, -1);
  if ( v5 >= 8 )
    operator delete(v4);
  return sub_52FD00(a1);
}

