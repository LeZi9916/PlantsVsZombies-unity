_DWORD *__thiscall sub_5D1180(int this, int a2)
{
  _DWORD *result; // eax
  int v3; // eax
  int v4; // eax
  int v5; // eax
  int v6; // eax
  int v7; // eax
  int v8; // eax
  int v9; // eax
  int v10; // eax
  int v11; // eax
  int v12; // esi
  int v13; // esi
  Concurrency::details::ExecutionResource *v14; // [esp-4h] [ebp-F0h]
  Concurrency::details::ExecutionResource *v15; // [esp-4h] [ebp-F0h]
  Concurrency::details::ExecutionResource *v16; // [esp-4h] [ebp-F0h]
  Concurrency::details::ExecutionResource *v17; // [esp-4h] [ebp-F0h]
  Concurrency::details::ExecutionResource *v18; // [esp-4h] [ebp-F0h]
  Concurrency::details::ExecutionResource *v19; // [esp-4h] [ebp-F0h]
  int v20; // [esp+Ch] [ebp-E0h]
  char v22; // [esp+2Ch] [ebp-C0h]
  int v23[7]; // [esp+B0h] [ebp-3Ch] BYREF
  void *v24; // [esp+CCh] [ebp-20h]
  int v25; // [esp+D0h] [ebp-1Ch]
  int v26; // [esp+D4h] [ebp-18h]
  int v27; // [esp+D8h] [ebp-14h]
  char v28; // [esp+DFh] [ebp-Dh]
  int v29; // [esp+E8h] [ebp-4h]

  v22 = 0;
  result = (_DWORD *)this;
  if ( !*(_BYTE *)(this + 281) )
  {
    if ( *(_DWORD *)(this + 312) )
    {
      if ( !*(_DWORD *)(this + 196) && sub_43B340() )
      {
        v24 = operator new(0x20u);
        v29 = 0;
        if ( v24 )
        {
          sub_4033F0("Arial Unicode MS");
          v22 = 1;
          v20 = sub_5AF2A0(*(_DWORD *)(*(_DWORD *)(this + 32) + 108), (int)v23, 12, 1u, 0, 0, 0);
        }
        else
        {
          v20 = 0;
        }
        v25 = v20;
        v29 = -1;
        *(_DWORD *)(this + 196) = v20;
        if ( (v22 & 1) != 0 )
          sub_401E90(v23);
      }
      v28 = (*(int (__thiscall **)(int))(*(_DWORD *)this + 288))(this);
      if ( *(_DWORD *)(this + 224) )
      {
        if ( *(_BYTE *)(this + 102) && *(int *)(this + 272) > 0 && *(int *)(this + 276) > 0 )
        {
          v15 = *(Concurrency::details::ExecutionResource **)(this + 312);
          v4 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
          result = (_DWORD *)sub_593B50(this + 264, v4, v15);
        }
        else if ( (*(unsigned __int8 (__thiscall **)(int))(*(_DWORD *)this + 288))(this) )
        {
          v16 = *(Concurrency::details::ExecutionResource **)(this + 312);
          v5 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
          result = (_DWORD *)sub_593B50(this + 248, v5, v16);
        }
        else if ( *(double *)(this + 288) <= 0.0 )
        {
          v19 = *(Concurrency::details::ExecutionResource **)(this + 312);
          if ( *(_BYTE *)(this + 105) )
          {
            v9 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
            result = (_DWORD *)sub_593B50(this + 232, v9, v19);
          }
          else
          {
            v10 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
            result = (_DWORD *)sub_593B50(this + 216, v10, v19);
          }
        }
        else
        {
          if ( *(double *)(this + 288) < 1.0 )
          {
            v17 = *(Concurrency::details::ExecutionResource **)(this + 312);
            v6 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
            sub_593B50(this + 216, v6, v17);
          }
          sub_592910(1);
          v7 = sub_5CA5A0(255, 255, 255, (int)(*(double *)(this + 288) * 255.0));
          sub_592880(v7);
          v18 = *(Concurrency::details::ExecutionResource **)(this + 312);
          v8 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
          sub_593B50(this + 232, v8, v18);
          result = (_DWORD *)sub_592910(0);
        }
        if ( v28 )
          result = (_DWORD *)sub_593AA0(*(_DWORD *)(this + 316), *(_DWORD *)(this + 320));
      }
      else
      {
        if ( v28 )
          sub_593AA0(*(_DWORD *)(this + 316), *(_DWORD *)(this + 320));
        v14 = *(Concurrency::details::ExecutionResource **)(this + 312);
        v3 = sub_4091F0(*(_DWORD *)(this + 72), *(_DWORD *)(this + 76));
        result = (_DWORD *)sub_593B10(v3, v14);
      }
      if ( *(_DWORD *)(this + 196) )
      {
        sub_592860(*(_DWORD *)(this + 196));
        v11 = sub_43B0D0();
        sub_592880(v11);
        v27 = (*(_DWORD *)(this + 72)
             - (*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(this + 196) + 28))(
                 *(_DWORD *)(this + 196),
                 this + 164))
            / 2;
        v12 = (*(int (__thiscall **)(_DWORD))(**(_DWORD **)(this + 196) + 4))(*(_DWORD *)(this + 196))
            + *(_DWORD *)(this + 76);
        v13 = v12 - (*(int (__thiscall **)(_DWORD))(**(_DWORD **)(this + 196) + 8))(*(_DWORD *)(this + 196));
        v26 = (v13 - (*(int (__thiscall **)(_DWORD))(**(_DWORD **)(this + 196) + 4))(*(_DWORD *)(this + 196)) / 6 - 1)
            / 2;
        result = (_DWORD *)sub_592F40(this + 164, *(_DWORD *)(this + 324) + v27, *(_DWORD *)(this + 328) + v26);
      }
      if ( v28 )
        return (_DWORD *)sub_593AA0(-*(_DWORD *)(this + 316), -*(_DWORD *)(this + 320));
    }
    else
    {
      return sub_5CE7D0((_DWORD *)this, a2);
    }
  }
  return result;
}

