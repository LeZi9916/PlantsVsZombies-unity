void __thiscall sub_5C6660(void *this, int a2, int a3)
{
  int v3; // [esp+8h] [ebp-2Ch]
  double v4; // [esp+Ch] [ebp-28h]
  double v5; // [esp+1Ch] [ebp-18h]
  double v7; // [esp+2Ch] [ebp-8h]

  if ( *((_BYTE *)this + 188) )
  {
    v7 = *((double *)this + 21);
    if ( *((_BYTE *)this + 200) )
    {
      v5 = (double)(a2 - *((_DWORD *)this + 48));
      *((double *)this + 21) = v5
                             / (double)(int)(*((_DWORD *)this + 18)
                                           - Concurrency::details::ExecutionResource::GetNodeId(*((Concurrency::details::ExecutionResource **)this
                                                                                                + 46)));
    }
    else
    {
      v4 = (double)(a3 - *((_DWORD *)this + 49));
      v3 = *((_DWORD *)this + 19) - unknown_libname_11(*((_DWORD *)this + 46));
      *((double *)this + 21) = v4 / (double)v3;
    }
    if ( *((double *)this + 21) < 0.0 )
      *((double *)this + 21) = 0.0;
    if ( *((double *)this + 21) > 1.0 )
      *((double *)this + 21) = 1.0;
    if ( v7 != *((double *)this + 21) )
    {
      (***((void (__thiscall ****)(_DWORD, _DWORD, _DWORD, _DWORD))this + 40))(
        *((_DWORD *)this + 40),
        *((_DWORD *)this + 44),
        COERCE_UNSIGNED_INT64(*((double *)this + 21)),
        HIDWORD(COERCE_UNSIGNED_INT64(*((double *)this + 21))));
      (*(void (__thiscall **)(void *, int))(*(_DWORD *)this + 76))(this, v3);
    }
  }
}

