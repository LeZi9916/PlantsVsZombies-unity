int __thiscall sub_5C5860(
        void *this,
        int a2,
        int a3,
        int a4,
        int a5,
        Concurrency::details::ExecutionResource *a6,
        int a7)
{
  int result; // eax
  int v8; // eax
  int v9; // [esp+0h] [ebp-28h]
  void *v10; // [esp+4h] [ebp-24h]
  int (__thiscall ***v11)(_DWORD, int); // [esp+8h] [ebp-20h]
  int (__thiscall ***v12)(_DWORD, int); // [esp+10h] [ebp-18h]
  int i; // [esp+18h] [ebp-10h]
  int v14; // [esp+1Ch] [ebp-Ch]
  int v15; // [esp+20h] [ebp-8h]
  int v16; // [esp+24h] [ebp-4h]

  v10 = this;
  v15 = 10;
  v14 = 1;
  while ( a3 >= v15 )
  {
    ++v14;
    v15 *= 10;
  }
  if ( !a3 )
    v15 = 10;
  v16 = (int)Concurrency::details::ExecutionResource::GetNodeId(a6) / 10;
  for ( i = 0; ; ++i )
  {
    result = i;
    if ( i >= v14 )
      break;
    v15 /= 10;
    v12 = (int (__thiscall ***)(_DWORD, int))sub_5927C0(a2, v9, v10, v11);
    v8 = unknown_libname_11(a6);
    sub_5939F0(a4 + i * (a7 + v16), a5, v16, v8);
    sub_592F90(a6, a4 + i * (a7 + v16) - v16 * (a3 / v15 % 10), a5);
    v11 = v12;
    if ( v12 )
      v9 = (**v12)(v12, 1);
    else
      v9 = 0;
  }
  return result;
}

