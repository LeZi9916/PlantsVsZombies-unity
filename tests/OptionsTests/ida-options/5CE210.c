void __thiscall sub_5CE210(int this, int a2)
{
  unknown_libname_14(a2);
  if ( !*(_DWORD *)(this + 188) && *(_DWORD *)(this + 176) && *(_DWORD *)(this + 172) )
  {
    if ( *(_BYTE *)(this + 168) )
      sub_592F90(*(Concurrency::details::ExecutionResource **)(this + 176), 0, 0);
    else
      sub_592F90(*(Concurrency::details::ExecutionResource **)(this + 172), 0, 0);
  }
  else if ( *(_DWORD *)(this + 188) && *(_DWORD *)(this + 172) )
  {
    if ( *(_BYTE *)(this + 168) )
      sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 172), 0, 0, this + 180);
    else
      sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 172), 0, 0, this + 196);
  }
  else if ( !*(_DWORD *)(this + 172) && !*(_DWORD *)(this + 176) )
  {
    sub_592880(this + 212);
    sub_592990(0, 0, *(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
    sub_592880(this + 228);
    sub_592990(1, 1, *(_DWORD *)(this + 72) - 2, *(_DWORD *)(this + 76) - 2);
    if ( *(_BYTE *)(this + 168) )
    {
      sub_592880(this + 244);
      sub_592E90(1, 1, *(_DWORD *)(this + 72) - 2, *(_DWORD *)(this + 76) - 2);
      sub_592E90(*(_DWORD *)(this + 72) - 1, 1, 1, *(_DWORD *)(this + 76) - 2);
    }
  }
}

