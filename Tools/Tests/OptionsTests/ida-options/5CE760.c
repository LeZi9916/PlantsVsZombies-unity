int __thiscall sub_5CE760(
        Concurrency::details::ExecutionResource **this,
        int a2,
        Concurrency::details::ExecutionResource *a3,
        int a4,
        int a5,
        int a6)
{
  if ( !*(_DWORD *)(a4 + 8) )
    return sub_592F90(a3, a5, a6);
  if ( this[50] )
    return sub_5930D0(this[50], a5, a6, a4);
  return sub_5930D0(a3, a5, a6, a4);
}

