int __thiscall sub_5C6400(void *this, int a2, int a3, int a4)
{
  int result; // eax
  double v6; // [esp+14h] [ebp-20h]
  int v7; // [esp+20h] [ebp-14h]
  double v8; // [esp+24h] [ebp-10h]
  int v9; // [esp+30h] [ebp-4h]

  if ( *((_BYTE *)this + 200) )
  {
    v9 = (int)((double)(int)(*((_DWORD *)this + 18)
                           - Concurrency::details::ExecutionResource::GetNodeId(*((Concurrency::details::ExecutionResource **)this
                                                                                + 46)))
             * *((double *)this + 21));
    if ( a2 < v9
      || a2 >= (int)(v9
                   + Concurrency::details::ExecutionResource::GetNodeId(*((Concurrency::details::ExecutionResource **)this
                                                                        + 46))) )
    {
      v8 = (double)a2 / (double)*((int *)this + 18);
      return (*(int (__thiscall **)(void *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(this, LODWORD(v8), HIDWORD(v8));
    }
    else
    {
      sub_62C810(2);
      *((_BYTE *)this + 188) = 1;
      result = a2 - v9;
      *((_DWORD *)this + 48) = a2 - v9;
    }
  }
  else
  {
    v7 = (int)((double)(*((_DWORD *)this + 19) - unknown_libname_11(*((_DWORD *)this + 46))) * *((double *)this + 21));
    if ( a3 < v7 || a3 >= v7 + unknown_libname_11(*((_DWORD *)this + 46)) )
    {
      v6 = (double)a3 / (double)*((int *)this + 19);
      return (*(int (__thiscall **)(void *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(this, LODWORD(v6), HIDWORD(v6));
    }
    else
    {
      sub_62C810(2);
      *((_BYTE *)this + 188) = 1;
      result = (int)this;
      *((_DWORD *)this + 49) = a3 - v7;
    }
  }
  return result;
}

