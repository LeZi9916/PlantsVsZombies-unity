int __usercall sub_454F30@<eax>(int a1@<esi>)
{
  int *v1; // ecx
  bool v2; // bl
  int v3; // edi
  int v4; // ebp
  int v5; // ebp
  int v6; // ebx
  int v7; // eax
  bool v8; // zf
  int v9; // ebx
  int v10; // ebx
  int v11; // ecx
  int v12; // ecx
  _DWORD *v13; // ebp
  _DWORD *v14; // ebx
  _DWORD *v15; // eax
  _DWORD *v16; // eax
  _DWORD *v17; // eax
  int result; // eax
  int v19; // [esp+8h] [ebp-30h]
  int v20; // [esp+8h] [ebp-30h]
  char v21; // [esp+1Eh] [ebp-1Ah]
  char v22; // [esp+1Fh] [ebp-19h]
  _DWORD *v23; // [esp+20h] [ebp-18h]

  v1 = *(int **)(*(_DWORD *)(a1 + 164) + 2384);
  if ( v1 && (v1[21] > 0 || v1[19] >= 15) || (v21 = 0, *(_BYTE *)(a1 + 345)) )
    v21 = 1;
  v2 = v1 && (v1[21] > 0 || v1[221] || v1[19] >= 25) || *(_BYTE *)(a1 + 345);
  if ( (unsigned __int8)sub_4614B0() || (v22 = 0, *(_BYTE *)(a1 + 345)) )
    v22 = 1;
  *(_BYTE *)(*(_DWORD *)(a1 + 212) + 102) = v21 == 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 212) + 100) = v21;
  *(_BYTE *)(*(_DWORD *)(a1 + 208) + 102) = !v2;
  *(_BYTE *)(*(_DWORD *)(a1 + 208) + 100) = v2;
  *(_BYTE *)(*(_DWORD *)(a1 + 196) + 102) = 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 196) + 100) = 1;
  v3 = **(_DWORD **)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2372) + 8) + 160 * (unsigned __int16)*(_DWORD *)(a1 + 252);
  if ( v21 )
  {
    sub_482F10(v3, "almanac_key_shadow", 0);
    if ( v2 )
    {
      *(_DWORD *)(96 * sub_4823E0("almanac_key_shadow") + *(_DWORD *)(v3 + 88) + 68) = 0;
    }
    else
    {
      v4 = dword_7536BC;
      *(_DWORD *)(96 * sub_4823E0("almanac_key_shadow") + *(_DWORD *)(v3 + 88) + 68) = v4;
    }
  }
  else if ( v2 )
  {
    sub_482F10(v3, "almanac_key_shadow", 0);
    v5 = dword_752DE8;
    *(_DWORD *)(96 * sub_4823E0("almanac_key_shadow") + *(_DWORD *)(v3 + 88) + 68) = v5;
  }
  else
  {
    sub_482F10(v3, "almanac_key_shadow", -1);
  }
  *(_BYTE *)(*(_DWORD *)(a1 + 216) + 102) = v22 == 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 216) + 100) = v22;
  if ( *(_BYTE *)(a1 + 240) )
  {
    *(_DWORD *)(*(_DWORD *)(a1 + 176) + 204) = dword_7531D4;
    *(_DWORD *)(*(_DWORD *)(a1 + 176) + 208) = dword_7531D4;
    v6 = **(_DWORD **)(a1 + 176);
    v7 = sub_55BBB0(128, 128, 128);
    (*(void (__thiscall **)(_DWORD, int, int))(v6 + 136))(*(_DWORD *)(a1 + 176), 5, v7);
  }
  else
  {
    *(_DWORD *)(*(_DWORD *)(a1 + 176) + 204) = dword_7531D4;
    *(_DWORD *)(*(_DWORD *)(a1 + 176) + 208) = dword_7531D4;
    (*(void (__thiscall **)(_DWORD, int, int *))(**(_DWORD **)(a1 + 176) + 136))(
      *(_DWORD *)(a1 + 176),
      5,
      &dword_7DCA98);
  }
  v8 = *(_BYTE *)(a1 + 241) == 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 180) + 204) = dword_752B28;
  *(_DWORD *)(*(_DWORD *)(a1 + 180) + 208) = dword_752B28;
  if ( v8 )
  {
    (*(void (__stdcall **)(int, int *))(**(_DWORD **)(a1 + 180) + 136))(5, &dword_7DCA98);
  }
  else
  {
    v9 = **(_DWORD **)(a1 + 180);
    v19 = sub_55BBB0(128, 128, 128);
    (*(void (__stdcall **)(int, int))(v9 + 136))(5, v19);
  }
  v8 = *(_BYTE *)(a1 + 242) == 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 220) + 204) = dword_752CDC;
  *(_DWORD *)(*(_DWORD *)(a1 + 220) + 208) = dword_752CDC;
  if ( v8 )
  {
    (*(void (__stdcall **)(int, int *))(**(_DWORD **)(a1 + 220) + 136))(5, &dword_7DCA98);
  }
  else
  {
    v10 = **(_DWORD **)(a1 + 220);
    v20 = sub_55BBB0(128, 128, 128);
    (*(void (__stdcall **)(int, int))(v10 + 136))(5, v20);
  }
  v11 = *(_DWORD *)(a1 + 168);
  if ( *(_BYTE *)(a1 + 244) )
  {
    *(_DWORD *)(v11 + 200) = dword_752AB4;
    v12 = dword_752AB4;
  }
  else
  {
    *(_DWORD *)(v11 + 200) = dword_752BB0;
    v12 = dword_752F20;
  }
  *(_DWORD *)(*(_DWORD *)(a1 + 168) + 204) = v12;
  *(_DWORD *)(*(_DWORD *)(a1 + 168) + 208) = dword_752AB4;
  v13 = (_DWORD *)(*(_DWORD *)(v3 + 88) + 96 * sub_4823E0("SelectorScreen_Survival_button"));
  v23 = (_DWORD *)(*(_DWORD *)(v3 + 88) + 96 * sub_4823E0("SelectorScreen_Challenges_button"));
  v14 = (_DWORD *)(*(_DWORD *)(v3 + 88) + 96 * sub_4823E0("SelectorScreen_ZenGarden_button"));
  v15 = (_DWORD *)(*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 176) + 144))(*(_DWORD *)(a1 + 176), 5);
  v13[19] = *v15;
  v13[20] = v15[1];
  v13[21] = v15[2];
  v13[22] = v15[3];
  v16 = (_DWORD *)(*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 180) + 144))(*(_DWORD *)(a1 + 180), 5);
  v23[19] = *v16;
  v23[20] = v16[1];
  v23[21] = v16[2];
  v23[22] = v16[3];
  v17 = (_DWORD *)(*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 220) + 144))(*(_DWORD *)(a1 + 220), 5);
  v14[19] = *v17;
  v14[20] = v17[1];
  v14[21] = v17[2];
  v14[22] = v17[3];
  if ( *(_BYTE *)(a1 + 243) )
  {
    sub_482F10(v3, "SelectorScreen_Adventure_", -1);
    *(_DWORD *)(*(_DWORD *)(a1 + 172) + 200) = dword_753820;
    result = dword_752EB0;
    *(_DWORD *)(*(_DWORD *)(a1 + 172) + 204) = dword_752EB0;
    *(_DWORD *)(*(_DWORD *)(a1 + 172) + 208) = dword_752EB0;
  }
  else
  {
    sub_482F10(v3, "SelectorScreen_StartAdventure_", -1);
    *(_DWORD *)(*(_DWORD *)(a1 + 172) + 200) = dword_753774;
    result = dword_7536A4;
    *(_DWORD *)(*(_DWORD *)(a1 + 172) + 204) = dword_7536A4;
    *(_DWORD *)(*(_DWORD *)(a1 + 172) + 208) = dword_7536A4;
  }
  return result;
}
