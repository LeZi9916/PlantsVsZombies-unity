void __usercall sub_46A2A0(_DWORD *a1@<esi>)
{
  int *v1; // eax
  int v2; // eax
  int v3; // ecx
  int j; // eax
  int i; // eax

  if ( !*a1 )
    _invalid_parameter_noinfo();
  v1 = (int *)a1[1];
  if ( *((_BYTE *)v1 + 65) )
  {
    v2 = v1[2];
    a1[1] = v2;
    if ( !*(_BYTE *)(v2 + 65) )
      return;
    goto LABEL_5;
  }
  v3 = *v1;
  if ( *(_BYTE *)(*v1 + 65) )
  {
    for ( i = v1[1]; !*(_BYTE *)(i + 65); i = *(_DWORD *)(i + 4) )
    {
      if ( a1[1] != *(_DWORD *)i )
        break;
      a1[1] = i;
    }
    if ( *(_BYTE *)(a1[1] + 65) )
    {
LABEL_5:
      _invalid_parameter_noinfo();
      return;
    }
    a1[1] = i;
  }
  else
  {
    for ( j = *(_DWORD *)(v3 + 8); !*(_BYTE *)(j + 65); j = *(_DWORD *)(j + 8) )
      v3 = j;
    a1[1] = v3;
  }
}

