void __thiscall sub_456280(_DWORD *this)
{
  int v2; // edi
  int v3; // eax
  int v4; // ecx
  _DWORD *v5; // edx
  int v6; // eax
  int v7; // eax
  int v8; // esi
  int v9; // eax
  int v10; // esi
  int v11; // edi
  void (__stdcall **v12)(int, int); // esi
  _DWORD *v13; // ecx
  int v14; // eax
  _DWORD *v15; // esi
  int v16; // eax
  int *v17; // edi
  int v18; // ebx
  int v19; // eax
  int v20; // ecx
  int v21; // edi
  double v23; // st7
  bool v24; // c0
  bool v25; // c3
  _DWORD *v26; // edi
  int v27; // ebx
  int v28; // ecx
  int v29; // eax
  _DWORD *v30; // edx
  int v31; // ecx
  int v32; // eax
  _DWORD *v33; // eax
  int v34; // edi
  int v35; // ebx
  int v36; // ebp
  _DWORD *v37; // eax
  Concurrency::details::ExecutionResource *v38; // ebp
  int v39; // edi
  int v40; // ebx
  float v41; // [esp+14h] [ebp-58h]
  int v42; // [esp+18h] [ebp-54h]
  int v43; // [esp+18h] [ebp-54h]
  float v44; // [esp+18h] [ebp-54h]
  float v45; // [esp+2Ch] [ebp-40h]
  int v46; // [esp+2Ch] [ebp-40h]

  sub_5C5350();
  (*(void (__thiscall **)(_DWORD *))(*this + 68))(this);
  sub_4560B0(this);
  if ( *(_DWORD *)(this[41] + 2384) )
  {
    if ( !byte_755E34 )
    {
      byte_755E34 = 1;
      if ( *(_DWORD *)(this[41] + 2384) )
        sub_4784A0(*(_DWORD *)(this[41] + 2384));
    }
  }
  if ( (int)this[87] > 0 )
  {
    v2 = *this;
    v42 = sub_526F20(this[89], this[91], 4);
    v3 = sub_526F20(this[88], this[90], 4);
    (*(void (__thiscall **)(_DWORD *, int, int))(v2 + 164))(this, v3, v42);
    --this[87];
  }
  sub_5346F0(*(_DWORD *)(this[41] + 2368));
  v4 = this[62];
  v5 = **(_DWORD ***)(this[41] + 2372);
  if ( v4 )
  {
    if ( (unsigned int)(unsigned __int16)v4 < v5[2] )
    {
      v6 = *v5 + 44 * (unsigned __int16)v4;
      if ( *(_DWORD *)(v6 + 40) == v4 )
        sub_52C8D0(v6);
    }
  }
  if ( *((_BYTE *)this + 232) )
  {
    v7 = ++this[59];
    if ( v7 > 450 )
    {
      sub_45C590();
      v8 = this[41];
      if ( *(_DWORD *)(v8 + 2332)
        || (v9 = *(_DWORD *)(v8 + 2384)) != 0 && *(int *)(v9 + 84) > 0
        || this[83] != 1
        || sub_45C2E0(v8) )
      {
        v10 = this[41];
        if ( *(_DWORD *)(*(_DWORD *)(v10 + 2384) + 876) && this[83] == 35 )
        {
          v11 = sub_45C950();
          sub_4A2EB0(601);
          (*(void (__thiscall **)(int, int))(*(_DWORD *)v11 + 300))(v11, 1);
          sub_45C100(this[41], 0, 0);
        }
        else if ( (unsigned __int8)sub_4578B0() )
        {
          sub_45C100(v10, 43, 0);
          sub_538080(*(_DWORD *)(this[41] + 2368));
        }
        else
        {
          sub_45C100(v10, 0, 1);
        }
      }
      else
      {
        sub_45C100(this[41], 72, 0);
      }
      return;
    }
    if ( v7 % 20 >= 10 )
    {
      (*(void (__stdcall **)(int, int *))(*(_DWORD *)this[43] + 136))(5, &dword_7DCA98);
    }
    else
    {
      v12 = (void (__stdcall **)(int, int))(*(_DWORD *)this[43] + 136);
      v43 = sub_55BBB0(128, 128, 128);
      (*v12)(5, v43);
    }
    if ( this[59] == 125 )
      (*(void (__thiscall **)(_DWORD, int))(*(_DWORD *)this[41] + 216))(this[41], dword_752C34);
  }
  v13 = (_DWORD *)this[41];
  v14 = this[82];
  v15 = (_DWORD *)(**(_DWORD **)(v13[593] + 8) + 160 * (unsigned __int16)this[63]);
  if ( v14 )
  {
    if ( v14 == 1 )
    {
      if ( !(*(int (__thiscall **)(_DWORD *, int))(*v13 + 292))(v13, 30) )
      {
        sub_4830B0((int)"anim_sign", 3, 30.0);
        this[82] = 2;
      }
    }
    else if ( v14 == 2 && (int)v15[23] > 0 )
    {
      this[82] = 3;
    }
  }
  else
  {
    if ( this[8] )
      sub_5C41E0();
    if ( (int)v15[23] > 0 )
    {
      sub_482EC0("SelectorScreen_Adventure_button", -1);
      sub_482EC0("SelectorScreen_StartAdventure_button", -1);
      sub_482EC0("SelectorScreen_Survival_button", -1);
      sub_482EC0("SelectorScreen_Challenges_button", -1);
      sub_482EC0("SelectorScreen_ZenGarden_button", -1);
      *(_BYTE *)(this[43] + 281) = 0;
      *(_BYTE *)(this[44] + 281) = 0;
      *(_BYTE *)(this[45] + 281) = 0;
      *(_BYTE *)(this[55] + 281) = 0;
      *(_BYTE *)(this[54] + 281) = 0;
      *(_BYTE *)(this[48] + 281) = 0;
      *(_BYTE *)(this[46] + 281) = 0;
      *(_BYTE *)(this[47] + 281) = 0;
      *(_BYTE *)(this[49] + 281) = 0;
      *(_BYTE *)(this[50] + 281) = 0;
      *(_BYTE *)(this[42] + 281) = 0;
      *(_BYTE *)(this[42] + 101) = 1;
      *(_BYTE *)(this[43] + 101) = 1;
      *(_BYTE *)(this[44] + 101) = 1;
      *(_BYTE *)(this[45] + 101) = 1;
      *(_BYTE *)(this[55] + 101) = 1;
      *(_BYTE *)(this[54] + 101) = 1;
      *(_BYTE *)(this[48] + 101) = 1;
      *(_BYTE *)(this[46] + 101) = 1;
      *(_BYTE *)(this[47] + 101) = 1;
      *(_BYTE *)(this[52] + 101) = 1;
      *(_BYTE *)(this[53] + 101) = 1;
      *(_BYTE *)(this[56] + 101) = 1;
      *(_BYTE *)(this[49] + 101) = 1;
      *(_BYTE *)(this[50] + 101) = 1;
      if ( *(_DWORD *)(this[41] + 2384) )
      {
        sub_4830B0((int)"anim_sign", 3, 30.0);
        this[82] = 2;
      }
      else
      {
        sub_45D7E0();
        if ( byte_755DF5 )
          sub_457BC0(this);
        this[82] = 1;
      }
      if ( *((_BYTE *)this + 344) )
        sub_4553E0();
      v16 = *(_DWORD *)(this[41] + 2384);
      if ( v16 && *(_DWORD *)(v16 + 872) )
      {
        *(_DWORD *)(v16 + 872) = 0;
        sub_45C0E0();
        sub_45D250(&off_70DA74, this[41], 49, &off_70DAA8, L"[DIALOG_BUTTON_OK]", (int)&unk_708460, 3);
      }
    }
  }
  v17 = this + 70;
  v18 = 6;
  do
  {
    v19 = **(_DWORD **)(*(_DWORD *)(this[41] + 2372) + 8) + 160 * (unsigned __int16)*(v17 - 6);
    v20 = *v17;
    if ( *v17 <= 0 )
    {
      if ( *(int *)(v19 + 92) > 0 )
        *v17 = sub_5EECC0(2001) + 2000;
    }
    else
    {
      *v17 = v20 - 1;
      if ( v20 == 1 )
      {
        *(_DWORD *)(v19 + 92) = 0;
        *(float *)(v19 + 4) = 0.0;
        *(float *)(v19 + 8) = 0.5;
      }
    }
    ++v17;
    --v18;
  }
  while ( v18 );
  sub_480F00(v15);
  v21 = **(_DWORD **)(*(_DWORD *)(this[41] + 2372) + 8) + 160 * (unsigned __int16)this[79];
  sub_480F00(v21);
  sub_4823E0("SelectorScreen_BG_Right");
  sub_4812C0(v15);
  *(float *)(v21 + 44) = (double)(this[16] + 800) + (float)-10000.0 - 71.0;
  *(float *)(v21 + 56) = (float)-10000.0 - 41.0;
  if ( this[81]-- == 1 )
  {
    sub_5EECE0(9.0);
    v45 = 9.0 + 3.0;
    sub_4827D0(v21, 20);
    v23 = 0.0;
    if ( v45 != 0.0 )
      *(float *)(v21 + 8) = v45;
    v24 = *(float *)(v21 + 8) > 0.0;
    v25 = 0.0 == *(float *)(v21 + 8);
    *(_DWORD *)(v21 + 16) = 0;
    *(_DWORD *)(v21 + 92) = 0;
    if ( !v24 && !v25 )
      v23 = 0.99999988;
    *(float *)(v21 + 4) = v23;
    *(float *)(v21 + 148) = -1.0;
    sub_482690(v21 + 24, v21 + 28);
    this[81] = sub_5EECC0(201) + 200;
  }
  v26 = this + 64;
  v27 = 6;
  do
  {
    sub_480F00(**(_DWORD **)(*(_DWORD *)(this[41] + 2372) + 8) + 160 * (unsigned __int16)*v26++);
    --v27;
  }
  while ( v27 );
  v28 = *(_DWORD *)(this[41] + 2372);
  v29 = this[80];
  if ( v28 )
  {
    v30 = *(_DWORD **)(v28 + 8);
    if ( v29 )
    {
      if ( (unsigned int)(unsigned __int16)v29 < v30[2] )
      {
        v31 = *v30 + 160 * (unsigned __int16)v29;
        if ( *(_DWORD *)(v31 + 156) == v29 )
          sub_480F00(v31);
      }
    }
  }
  v44 = 0.0;
  v41 = 800.0;
  if ( *((_BYTE *)this + 243) )
    sub_456D00("SelectorScreen_StartAdventure_button", v41, v44);
  else
    sub_456D00("SelectorScreen_Adventure_button", v41, v44);
  sub_456D00("SelectorScreen_Survival_button", 800.0, 0.0);
  sub_456D00("SelectorScreen_ZenGarden_button", 800.0, 0.0);
  sub_456D00("SelectorScreen_Challenges_button", 800.0, 0.0);
  sub_456D00("SelectorScreen_BG_Right", 1421.0, 288.0);
  sub_456D00("SelectorScreen_BG_Right", 900.0, 360.0);
  sub_456D00("SelectorScreen_BG_Right", 1294.0, 434.0);
  sub_456D00("SelectorScreen_BG_Right", 1444.0, 469.0);
  sub_456D00("SelectorScreen_BG_Right", 1376.0, 458.0);
  sub_456D00("SelectorScreen_BG_Right", 1056.0, 387.0);
  sub_456D00("SelectorScreen_BG_Right", 1134.0, 441.0);
  sub_456D00("woodsign2", 824.0, 10.0);
  sub_456D00("woodsign3", 800.0, 0.0);
  sub_456D00("SelectorScreen_BG_Left", 820.0, 480.0);
  v32 = this[56];
  if ( *(_BYTE *)(v32 + 105) || *(_BYTE *)(v32 + 104) )
  {
    v33 = (_DWORD *)v15[3];
    v34 = 0;
    v46 = dword_7532A0;
    if ( (int)v33[1] <= 0 )
    {
LABEL_80:
      v34 = 0;
    }
    else
    {
      v35 = 0;
      while ( _stricmp(*(const char **)(v35 + *v33), "woodsign2") )
      {
        v33 = (_DWORD *)v15[3];
        ++v34;
        v35 += 12;
        if ( v34 >= v33[1] )
          goto LABEL_80;
      }
    }
    *(_DWORD *)(96 * v34 + v15[22] + 68) = v46;
  }
  else
  {
    *(_DWORD *)(96 * sub_4823E0("woodsign2") + v15[22] + 68) = 0;
  }
  v36 = this[49];
  if ( *(_BYTE *)(v36 + 105) || *(_BYTE *)(v36 + 104) )
  {
    v37 = (_DWORD *)v15[3];
    v38 = dword_752F14;
    v39 = 0;
    if ( (int)v37[1] <= 0 )
    {
LABEL_89:
      v39 = 0;
    }
    else
    {
      v40 = 0;
      while ( _stricmp(*(const char **)(v40 + *v37), "woodsign3") )
      {
        v37 = (_DWORD *)v15[3];
        ++v39;
        v40 += 12;
        if ( v39 >= v37[1] )
          goto LABEL_89;
      }
    }
    *(_DWORD *)(96 * v39 + v15[22] + 68) = v38;
  }
  else
  {
    *(_DWORD *)(96 * sub_4823E0("woodsign3") + v15[22] + 68) = 0;
  }
}
