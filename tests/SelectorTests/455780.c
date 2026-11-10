char __stdcall sub_455780(int a1)
{
  int v1; // eax
  int v2; // eax
  int v3; // eax
  int v4; // ecx
  char v5; // bl
  int i; // edi
  int v8; // [esp-1Ch] [ebp-34h] BYREF
  void *v9; // [esp-18h] [ebp-30h]
  int v10; // [esp-14h] [ebp-2Ch]
  int v11; // [esp-10h] [ebp-28h]
  int v12; // [esp-Ch] [ebp-24h]
  int v13; // [esp-8h] [ebp-20h]
  int v14; // [esp-4h] [ebp-1Ch]
  int v15; // [esp+10h] [ebp-8h]
  int *v16; // [esp+14h] [ebp-4h]

  v1 = *(_DWORD *)(a1 + 164);
  if ( *(_DWORD *)(v1 + 2384) )
  {
    v2 = *(_DWORD *)(v1 + 2384);
    if ( v2 && *(int *)(v2 + 84) > 0 )
    {
      v15 = 0;
      v16 = &v8;
      sub_463B60();
      sub_463910(*(_DWORD *)(a1 + 164), v8, v9, v10, v11, v12, v13, v14);
    }
    if ( (unsigned __int8)sub_463170() )
    {
      v15 = 1;
      v16 = &v8;
      sub_463B60();
      sub_463910(*(_DWORD *)(a1 + 164), v8, v9, v10, v11, v12, v13, v14);
    }
    v3 = *(_DWORD *)(*(_DWORD *)(a1 + 164) + 2384);
    if ( v3 )
    {
      v4 = *(_DWORD *)(v3 + 84);
      if ( v4 > 0 && (v4 >= 2 || *(_DWORD *)(v3 + 76) >= dword_747D90) )
      {
        v15 = 10;
        v16 = &v8;
        sub_463B60();
        sub_463910(*(_DWORD *)(a1 + 164), v8, v9, v10, v11, v12, v13, v14);
      }
    }
    if ( *(int *)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2384) + 284) >= 100 )
    {
      v15 = 18;
      v16 = &v8;
      sub_463B60();
      sub_463910(*(_DWORD *)(a1 + 164), v8, v9, v10, v11, v12, v13, v14);
    }
    v5 = 1;
    for ( i = 40; i <= 48; ++i )
    {
      if ( i == 40 )
        LOBYTE(v1) = *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2384) + 488) > 0;
      else
        LOBYTE(v1) = sub_460F00();
      if ( !(_BYTE)v1 )
        v5 = 0;
    }
    if ( v5 )
    {
      v15 = 6;
      v16 = &v8;
      sub_463B60();
      LOBYTE(v1) = sub_463910(*(_DWORD *)(a1 + 164), v8, v9, v10, v11, v12, v13, v14);
    }
  }
  return v1;
}
