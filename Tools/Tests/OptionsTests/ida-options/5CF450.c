void __thiscall sub_5CF450(void *this)
{
  int v1; // [esp+0h] [ebp-4h]

  sub_5C5350(this, this);
  if ( *(_BYTE *)(v1 + 104) && *(_BYTE *)(v1 + 105) )
    (*(void (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(v1 + 284) + 12))(
      *(_DWORD *)(v1 + 284),
      *(_DWORD *)(v1 + 160));
  if ( !*(_BYTE *)(v1 + 104) && !*(_BYTE *)(v1 + 105) && *(double *)(v1 + 288) > 0.0 )
  {
    if ( *(double *)(v1 + 296) <= 0.0 )
    {
      *(double *)(v1 + 288) = 0.0;
    }
    else
    {
      *(double *)(v1 + 288) = *(double *)(v1 + 288) - *(double *)(v1 + 296);
      if ( *(double *)(v1 + 288) < 0.0 )
        *(double *)(v1 + 288) = 0.0;
    }
LABEL_17:
    (*(void (__thiscall **)(int))(*(_DWORD *)v1 + 68))(v1);
    return;
  }
  if ( *(_BYTE *)(v1 + 105) && *(double *)(v1 + 304) > 0.0 && *(double *)(v1 + 288) < 1.0 )
  {
    *(double *)(v1 + 288) = *(double *)(v1 + 288) + *(double *)(v1 + 304);
    if ( *(double *)(v1 + 288) > 1.0 )
      *(double *)(v1 + 288) = 1.0;
    goto LABEL_17;
  }
}

