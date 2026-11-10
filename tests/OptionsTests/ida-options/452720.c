int __usercall sub_452720@<eax>(int a1@<ecx>, int a2@<eax>, int a3, int a4, char a5, char a6, int a7)
{
  Concurrency::details::ExecutionResource *v7; // ebp
  int v9; // esi
  double v10; // st7
  double v11; // st7
  int v12; // esi
  int v13; // esi
  int v14; // ebx
  int v15; // ebx
  int *v17; // [esp+Ch] [ebp-10h]
  Concurrency::details::ExecutionResource *v18; // [esp+10h] [ebp-Ch]
  int v19; // [esp+14h] [ebp-8h]
  int v20; // [esp+18h] [ebp-4h]
  float v21; // [esp+28h] [ebp+Ch]
  int v22; // [esp+28h] [ebp+Ch]

  v7 = dword_752BF4;
  v9 = a1;
  v19 = a1;
  v20 = a2;
  v17 = (int *)dword_7534EC;
  v18 = (Concurrency::details::ExecutionResource *)dword_752B94;
  if ( a5 )
  {
    v7 = (Concurrency::details::ExecutionResource *)dword_752D5C;
    v9 = a1 + 1;
    v17 = (int *)dword_7535C4;
    v18 = (Concurrency::details::ExecutionResource *)dword_753484;
    v19 = a1 + 1;
    v20 = a2 + 1;
  }
  v21 = (double)(a3 - *((_DWORD *)v18 + 9) - *((_DWORD *)v7 + 9)) / (double)v17[9];
  v10 = v21;
  if ( v21 <= 0.0 )
    v11 = v10 - 0.5;
  else
    v11 = v10 + 0.5;
  v22 = (int)v11;
  sub_592F90(v7, v9, a2);
  v12 = *((_DWORD *)v7 + 9) + v9;
  if ( (int)v11 > 0 )
  {
    do
    {
      sub_592F90((Concurrency::details::ExecutionResource *)v17, v12, a2);
      v12 += v17[9];
      --v22;
    }
    while ( v22 );
  }
  sub_592F90(v18, v12, a2);
  if ( a6 )
    sub_592860(dword_752B80);
  else
    sub_592860(dword_753790);
  v13 = v19 + (a3 - (*(int (__thiscall **)(int, int))(*(_DWORD *)dword_753790 + 28))(dword_753790, a7)) / 2 + 1;
  v14 = a4 - (*(int (__thiscall **)(int))(*(_DWORD *)dword_753790 + 4))(dword_753790) / 6 - 1;
  v15 = v20 + (v14 + (*(int (__thiscall **)(int))(*(_DWORD *)dword_753790 + 4))(dword_753790)) / 2 - 4;
  sub_592880(&dword_7DCA98);
  return sub_592F40(a7, v13, v15);
}

