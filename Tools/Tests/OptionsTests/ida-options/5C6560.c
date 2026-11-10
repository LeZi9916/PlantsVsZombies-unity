int __thiscall sub_5C6560(int this, signed int a2, int a3)
{
  int v5; // [esp+Ch] [ebp-8h]
  int v6; // [esp+10h] [ebp-4h]

  if ( *(_BYTE *)(this + 200) )
  {
    v6 = (int)((double)(int)(*(_DWORD *)(this + 72)
                           - Concurrency::details::ExecutionResource::GetNodeId(*(Concurrency::details::ExecutionResource **)(this + 184)))
             * *(double *)(this + 168));
    if ( a2 >= v6
      && a2 < (int)(v6
                  + Concurrency::details::ExecutionResource::GetNodeId(*(Concurrency::details::ExecutionResource **)(this + 184))) )
    {
      return sub_62C810(2);
    }
  }
  else
  {
    v5 = (int)((double)(*(_DWORD *)(this + 76) - unknown_libname_11(*(_DWORD *)(this + 184))) * *(double *)(this + 168));
    if ( a3 >= v5 && a3 < v5 + unknown_libname_11(*(_DWORD *)(this + 184)) )
      return sub_62C810(2);
  }
  return sub_62C810(0);
}

