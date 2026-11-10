int __thiscall sub_5C5F20(int this, int a2)
{
  int v2; // eax
  int v3; // eax
  int v4; // eax
  int v5; // eax
  int v6; // eax
  int v7; // eax
  double v8; // st7
  int result; // eax
  unsigned int v10; // eax
  int v11; // [esp-4h] [ebp-194h]
  int v12; // [esp-4h] [ebp-194h]
  int v13; // [esp+Ch] [ebp-184h]
  int NodeId; // [esp+10h] [ebp-180h]
  int j; // [esp+78h] [ebp-118h]
  _BYTE v17[124]; // [esp+7Ch] [ebp-114h] BYREF
  int i; // [esp+F8h] [ebp-98h]
  _BYTE v19[124]; // [esp+FCh] [ebp-94h] BYREF
  int v20; // [esp+178h] [ebp-18h]
  int v21; // [esp+17Ch] [ebp-14h]
  int v22; // [esp+180h] [ebp-10h]
  int v23; // [esp+18Ch] [ebp-4h]

  if ( *(_DWORD *)(this + 180) )
  {
    if ( *(_BYTE *)(this + 200) )
      NodeId = (int)Concurrency::details::ExecutionResource::GetNodeId(*(Concurrency::details::ExecutionResource **)(this + 180))
             / 3;
    else
      NodeId = Concurrency::details::ExecutionResource::GetNodeId(*(Concurrency::details::ExecutionResource **)(this + 180));
    v22 = NodeId;
    if ( *(_BYTE *)(this + 200) )
      v13 = unknown_libname_11(*(_DWORD *)(this + 180));
    else
      v13 = unknown_libname_11(*(_DWORD *)(this + 180)) / 3;
    v21 = v13;
    if ( *(_BYTE *)(this + 200) )
    {
      v20 = (*(_DWORD *)(this + 76) - v21) / 2;
      v2 = sub_4091F0(v22, v21);
      sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 180), 0, v20, v2);
      sub_5924D0(a2);
      v23 = 0;
      sub_5939F0(v22, v20, *(_DWORD *)(this + 72) - 2 * v22, v21);
      for ( i = 0; i < (*(_DWORD *)(this + 72) - 2 * v22 + v22 - 1) / v22; ++i )
      {
        v3 = sub_4091F0(v22, v21);
        sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 180), v22 + v22 * i, v20, v3);
      }
      v4 = sub_4091F0(v22, v21);
      sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 180), *(_DWORD *)(this + 72) - v22, v20, v4);
      v23 = -1;
      sub_5926D0(v19);
    }
    else
    {
      v5 = sub_4091F0(v22, v21);
      sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 180), 0, 0, v5);
      sub_5924D0(a2);
      v23 = 1;
      sub_5939F0(0, v21, v22, *(_DWORD *)(this + 76) - 2 * v21);
      for ( j = 0; j < (*(_DWORD *)(this + 76) - 2 * v21 + v21 - 1) / v21; ++j )
      {
        v6 = sub_4091F0(v22, v21);
        sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 180), 0, v21 + v21 * j, v6);
      }
      v7 = sub_4091F0(v22, v21);
      sub_5930D0(*(Concurrency::details::ExecutionResource **)(this + 180), 0, *(_DWORD *)(this + 76) - v21, v7);
      v23 = -1;
      sub_5926D0(v17);
    }
  }
  if ( *(_BYTE *)(this + 200) && *(_DWORD *)(this + 184) )
  {
    v11 = (*(_DWORD *)(this + 76) - unknown_libname_11(*(_DWORD *)(this + 184))) / 2;
    v8 = (double)(int)(*(_DWORD *)(this + 72)
                     - Concurrency::details::ExecutionResource::GetNodeId(*(Concurrency::details::ExecutionResource **)(this + 184)))
       * *(double *)(this + 168);
    return sub_592F90(*(Concurrency::details::ExecutionResource **)(this + 184), (int)v8, v11);
  }
  else
  {
    result = this;
    if ( !*(_BYTE *)(this + 200) )
    {
      if ( *(_DWORD *)(this + 184) )
      {
        v12 = (int)((double)(*(_DWORD *)(this + 76) - unknown_libname_11(*(_DWORD *)(this + 184)))
                  * *(double *)(this + 168));
        v10 = Concurrency::details::ExecutionResource::GetNodeId(*(Concurrency::details::ExecutionResource **)(this + 184));
        return sub_592F90(
                 *(Concurrency::details::ExecutionResource **)(this + 184),
                 (int)(*(_DWORD *)(this + 72) - v10) / 2,
                 v12);
      }
    }
  }
  return result;
}

