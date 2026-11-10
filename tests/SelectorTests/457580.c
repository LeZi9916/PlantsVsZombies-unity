int __fastcall sub_457580(int a1, int a2, int a3)
{
  int v3; // esi
  int v4; // ebx
  int v5; // ecx
  int v6; // eax
  int result; // eax
  int v8; // ecx
  int v9; // edi
  _DWORD *v10; // esi
  int v11; // edx
  double v12; // st7
  int v13; // ebp
  int v14; // edi
  _BYTE v15[4]; // [esp+54h] [ebp-1Ch] BYREF
  void *v16; // [esp+58h] [ebp-18h]
  unsigned int v17; // [esp+6Ch] [ebp-4h]
  float v18; // [esp+74h] [ebp+4h]
  int v19; // [esp+74h] [ebp+4h]

  v3 = *(_DWORD *)(a3 + 164);
  v4 = 0;
  if ( (*(_BYTE *)(v3 + 2536)
     || ((v5 = *(_DWORD *)(v3 + 2460)) == 0 || !(unsigned __int8)sub_651F30(v5, a2)) && *(_DWORD *)(v3 + 2532) == 1)
    && (*(int *)(a3 + 332) >= 25 || (v6 = *(_DWORD *)(*(_DWORD *)(a3 + 164) + 2384)) != 0 && *(int *)(v6 + 84) > 0) )
  {
    result = sub_45D250(
               L"[REPLAY_LEVEL_BODY]",
               *(_DWORD *)(a3 + 164),
               49,
               L"[REPLAY_LEVEL_HEADER]",
               L"[DIALOG_BUTTON_YES]",
               (int)L"[DIALOG_BUTTON_NO]",
               1);
    if ( result == 1001 )
      return result;
    *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a3 + 164) + 2384) + 76) = 24;
    *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a3 + 164) + 2384) + 84) = 0;
    sub_464510(*(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a3 + 164) + 2384) + 32), (int)v15);
    sub_620130(v15);
    if ( v17 >= 0x10 )
      operator delete(v16);
  }
  sub_468F30();
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 164) + 216))(*(_DWORD *)(a3 + 164), dword_752D1C);
  v8 = *(_DWORD *)(a3 + 172);
  *(_BYTE *)(a3 + 232) = 1;
  (*(void (__thiscall **)(int, int))(*(_DWORD *)v8 + 148))(v8, 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 176) + 148))(*(_DWORD *)(a3 + 176), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 180) + 148))(*(_DWORD *)(a3 + 180), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 184) + 148))(*(_DWORD *)(a3 + 184), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 188) + 148))(*(_DWORD *)(a3 + 188), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 192) + 148))(*(_DWORD *)(a3 + 192), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 224) + 148))(*(_DWORD *)(a3 + 224), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 208) + 148))(*(_DWORD *)(a3 + 208), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 212) + 148))(*(_DWORD *)(a3 + 212), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 220) + 148))(*(_DWORD *)(a3 + 220), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 216) + 148))(*(_DWORD *)(a3 + 216), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 196) + 148))(*(_DWORD *)(a3 + 196), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 200) + 148))(*(_DWORD *)(a3 + 200), 1);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a3 + 168) + 148))(*(_DWORD *)(a3 + 168), 1);
  v9 = *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a3 + 164) + 2372) + 8);
  v10 = (_DWORD *)sub_483BC0();
  v10[24] = v9;
  v10[26] = 0;
  sub_482AA0(110, 0);
  *v10 = 110;
  sub_480E40((int)v10, -70.0, 10.0);
  v11 = v10[39];
  v10[4] = 3;
  *(_DWORD *)(a3 + 320) = v11;
  if ( !*(_BYTE *)(*(_DWORD *)(a3 + 164) + 2537) )
  {
    v18 = 0.0;
    if ( 0.0 != *(float *)(dword_755E50 + 4164) )
    {
      v12 = *(float *)(dword_755E50 + 4164);
      sub_5EECE0(*(float *)(dword_755E50 + 4164));
      v18 = v12;
    }
    sub_52A5A0(v18);
  }
  result = v10[3];
  v19 = 0;
  if ( *(int *)(result + 4) > 0 )
  {
    v13 = 0;
    do
    {
      v14 = v4 + v10[22];
      if ( !_strnicmp(*(const char **)(*(_DWORD *)v10[3] + v13), "rock", 4u) )
        *(_BYTE *)(v14 + 92) = 1;
      result = v19 + 1;
      v13 += 12;
      v4 += 96;
      ++v19;
    }
    while ( v19 < *(_DWORD *)(v10[3] + 4) );
  }
  return result;
}
