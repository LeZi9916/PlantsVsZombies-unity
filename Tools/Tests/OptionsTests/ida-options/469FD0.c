_DWORD *__userpurge sub_469FD0@<eax>(_DWORD *a1@<ecx>, int *a2@<edi>, _DWORD *a3, char a4, int a5)
{
  int v6; // ebp
  _DWORD *v7; // eax
  _DWORD *v8; // eax
  int v9; // eax
  int *v10; // eax
  int i; // esi
  int v12; // ecx
  int *v13; // edx
  int v14; // edx
  int v15; // edx
  int v16; // eax
  _DWORD *v17; // ecx
  int v18; // edx
  _DWORD *v19; // edx
  int v20; // ecx
  _DWORD *result; // eax
  _BYTE v22[20]; // [esp+Ch] [ebp-54h] BYREF
  int v23; // [esp+20h] [ebp-40h]
  int v24; // [esp+24h] [ebp-3Ch]
  _DWORD pExceptionObject[14]; // [esp+28h] [ebp-38h] BYREF

  if ( (unsigned int)a2[7] >= 0x5555554 )
  {
    v24 = 15;
    v23 = 0;
    v22[4] = 0;
    sub_4032C0("map/set<T> too long", 0x13u);
    pExceptionObject[13] = 0;
    sub_408720(v22);
    pExceptionObject[0] = &std::length_error::`vftable';
    _CxxThrowException(pExceptionObject, (_ThrowInfo *)&_TI3_AVlength_error_std__);
  }
  v6 = sub_46A260(a2[6], a1, a2[6], a5);
  v7 = (_DWORD *)a2[6];
  ++a2[7];
  if ( a1 == v7 )
  {
    v7[1] = v6;
    *(_DWORD *)a2[6] = v6;
    *(_DWORD *)(a2[6] + 8) = v6;
  }
  else if ( a4 )
  {
    *a1 = v6;
    v8 = (_DWORD *)a2[6];
    if ( a1 == (_DWORD *)*v8 )
      *v8 = v6;
  }
  else
  {
    a1[2] = v6;
    v9 = a2[6];
    if ( a1 == *(_DWORD **)(v9 + 8) )
      *(_DWORD *)(v9 + 8) = v6;
  }
  v10 = (int *)(v6 + 4);
  for ( i = v6; !*(_BYTE *)(*(_DWORD *)(i + 4) + 64); v10 = (int *)(i + 4) )
  {
    v12 = *v10;
    v13 = *(int **)(*v10 + 4);
    if ( *v10 == *v13 )
    {
      v14 = v13[2];
      if ( *(_BYTE *)(v14 + 64) )
      {
        if ( i == *(_DWORD *)(v12 + 8) )
        {
          i = *v10;
          sub_46A1C0(a2);
        }
        *(_BYTE *)(*(_DWORD *)(i + 4) + 64) = 1;
        *(_BYTE *)(*(_DWORD *)(*(_DWORD *)(i + 4) + 4) + 64) = 0;
        sub_46A210(a2);
      }
      else
      {
        *(_BYTE *)(v12 + 64) = 1;
        *(_BYTE *)(v14 + 64) = 1;
        *(_BYTE *)(*(_DWORD *)(*v10 + 4) + 64) = 0;
        i = *(_DWORD *)(*v10 + 4);
      }
    }
    else
    {
      v15 = *v13;
      if ( *(_BYTE *)(v15 + 64) )
      {
        if ( i == *(_DWORD *)v12 )
        {
          i = *v10;
          sub_46A210(a2);
        }
        *(_BYTE *)(*(_DWORD *)(i + 4) + 64) = 1;
        *(_BYTE *)(*(_DWORD *)(*(_DWORD *)(i + 4) + 4) + 64) = 0;
        v16 = *(_DWORD *)(*(_DWORD *)(i + 4) + 4);
        v17 = *(_DWORD **)(v16 + 8);
        *(_DWORD *)(v16 + 8) = *v17;
        if ( !*(_BYTE *)(*v17 + 65) )
          *(_DWORD *)(*v17 + 4) = v16;
        v17[1] = *(_DWORD *)(v16 + 4);
        v18 = a2[6];
        if ( v16 == *(_DWORD *)(v18 + 4) )
        {
          *(_DWORD *)(v18 + 4) = v17;
        }
        else
        {
          v19 = *(_DWORD **)(v16 + 4);
          if ( v16 == *v19 )
            *v19 = v17;
          else
            v19[2] = v17;
        }
        *v17 = v16;
        *(_DWORD *)(v16 + 4) = v17;
      }
      else
      {
        *(_BYTE *)(v12 + 64) = 1;
        *(_BYTE *)(v15 + 64) = 1;
        *(_BYTE *)(*(_DWORD *)(*v10 + 4) + 64) = 0;
        i = *(_DWORD *)(*v10 + 4);
      }
    }
  }
  *(_BYTE *)(*(_DWORD *)(a2[6] + 4) + 64) = 1;
  v20 = *a2;
  result = a3;
  a3[1] = v6;
  *a3 = v20;
  return result;
}

