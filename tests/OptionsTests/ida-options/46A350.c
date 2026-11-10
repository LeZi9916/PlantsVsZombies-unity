int __stdcall sub_46A350(int a1, _BYTE *a2, char a3)
{
  int v3; // eax
  int v4; // eax
  bool v5; // cf
  int v6; // eax
  int v7; // eax
  int v8; // eax
  int v9; // eax
  unsigned __int64 v10; // st7
  _DWORD *v11; // eax
  void (__thiscall **v12)(_DWORD, _DWORD, _DWORD); // ebp
  double v13; // st7
  char v14; // al
  int v15; // edi
  int v16; // eax
  int v17; // eax
  int v18; // eax
  _DWORD *v19; // ecx
  int v20; // eax
  _DWORD *v21; // ecx
  int v22; // eax
  int v23; // ecx
  int v24; // edi
  void *v26; // [esp+50h] [ebp-80h]
  float v27; // [esp+50h] [ebp-80h]
  void *v28; // [esp+50h] [ebp-80h]
  int v29; // [esp+54h] [ebp-7Ch] BYREF
  void *v30; // [esp+58h] [ebp-78h]
  int v31; // [esp+68h] [ebp-68h]
  unsigned int v32; // [esp+6Ch] [ebp-64h]
  _BYTE v33[4]; // [esp+70h] [ebp-60h] BYREF
  void *v34; // [esp+74h] [ebp-5Ch]
  int v35; // [esp+84h] [ebp-4Ch]
  unsigned int v36; // [esp+88h] [ebp-48h]
  _BYTE v37[4]; // [esp+8Ch] [ebp-44h] BYREF
  void *v38; // [esp+90h] [ebp-40h]
  int v39; // [esp+A0h] [ebp-30h]
  unsigned int v40; // [esp+A4h] [ebp-2Ch]
  _BYTE v41[4]; // [esp+A8h] [ebp-28h] BYREF
  void *v42; // [esp+ACh] [ebp-24h]
  int v43; // [esp+BCh] [ebp-14h]
  unsigned int v44; // [esp+C0h] [ebp-10h]
  int v45; // [esp+CCh] [ebp-4h]

  v44 = 7;
  v43 = 0;
  LOWORD(v42) = 0;
  sub_40D1E0(&unk_708460, 0);
  v45 = 2;
  LOWORD(v38) = 0;
  v40 = 7;
  v39 = 0;
  sub_40D1E0(&unk_708460, 0);
  v36 = 7;
  v35 = 0;
  LOWORD(v34) = 0;
  sub_40D1E0(&off_71128C, 7);
  sub_5CF5B0(0, 0, 2, 1, v33, v37, v41, 0);
  if ( v36 >= 8 )
    operator delete(v34);
  v36 = 7;
  v35 = 0;
  LOWORD(v34) = 0;
  if ( v40 >= 8 )
    operator delete(v38);
  v40 = 7;
  v39 = 0;
  LOWORD(v38) = 0;
  LOBYTE(v45) = 6;
  if ( v44 >= 8 )
    operator delete(v42);
  *(_DWORD *)(a1 + 360) = &off_708968;
  *(_DWORD *)(a1 + 364) = &off_711628;
  *(_BYTE *)(a1 + 404) = a3;
  v44 = 7;
  v43 = 0;
  LOWORD(v42) = 0;
  *(_DWORD *)a1 = &off_711630;
  *(_DWORD *)(a1 + 160) = &off_711760;
  *(_DWORD *)(a1 + 360) = &off_71177C;
  *(_DWORD *)(a1 + 364) = &off_711780;
  *(_DWORD *)(a1 + 368) = a2;
  v3 = sub_55BBB0(255, 255, 100);
  sub_5CFEB0(3, v3);
  v32 = 7;
  v31 = 0;
  LOWORD(v30) = 0;
  sub_40D1E0(&off_708B58, 21);
  LOBYTE(v45) = 7;
  v4 = sub_453310(0, a1 + 160, &v29);
  LOBYTE(v45) = 6;
  v5 = v32 < 8;
  *(_DWORD *)(a1 + 388) = v4;
  if ( !v5 )
    operator delete(v30);
  v32 = 7;
  v31 = 0;
  LOWORD(v30) = 0;
  sub_40D1E0(&off_70C874, 22);
  LOBYTE(v45) = 8;
  v6 = sub_453310(2, a1 + 160, &v29);
  LOBYTE(v45) = 6;
  v5 = v32 < 8;
  *(_DWORD *)(a1 + 396) = v6;
  if ( !v5 )
    operator delete(v30);
  v32 = 7;
  v31 = 0;
  LOWORD(v30) = 0;
  sub_40D1E0(&off_708B0C, 18);
  LOBYTE(v45) = 9;
  v7 = sub_453310(1, a1 + 160, &v29);
  LOBYTE(v45) = 6;
  v5 = v32 < 8;
  *(_DWORD *)(a1 + 392) = v7;
  if ( !v5 )
    operator delete(v30);
  v32 = 7;
  v31 = 0;
  LOWORD(v30) = 0;
  sub_40D1E0(L"[BACK_TO_GAME]", 14);
  LOBYTE(v45) = 10;
  v8 = sub_453910(1000, a1 + 160, &v29, 0, dword_753094);
  LOBYTE(v45) = 6;
  v5 = v32 < 8;
  *(_DWORD *)(a1 + 400) = v8;
  if ( !v5 )
    operator delete(v30);
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 316) = 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 320) = 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 324) = -2;
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 328) = -5;
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 340) = 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 344) = 1;
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 400) + 284))(*(_DWORD *)(a1 + 400), dword_75334C);
  (*(void (__thiscall **)(_DWORD, _DWORD, int *))(**(_DWORD **)(a1 + 400) + 136))(
    *(_DWORD *)(a1 + 400),
    0,
    &dword_7DCA98);
  (*(void (__thiscall **)(_DWORD, int, int *))(**(_DWORD **)(a1 + 400) + 136))(*(_DWORD *)(a1 + 400), 1, &dword_7DCA98);
  *(_DWORD *)(*(_DWORD *)(a1 + 400) + 336) = dword_7530A0;
  v26 = operator new(0xD0u);
  LOBYTE(v45) = 11;
  if ( v26 )
    v9 = sub_5C5DF0(dword_753420, dword_753710, 4, a1 + 360);
  else
    v9 = 0;
  LOBYTE(v45) = 6;
  *(_DWORD *)(a1 + 372) = v9;
  v27 = ((double (__thiscall *)(_BYTE *))*(_DWORD *)(*(_DWORD *)a2 + 224))(a2);
  *(double *)&v10 = v27;
  if ( v27 > 1.0 )
    v27 = 1.0;
  if ( v27 >= 0.0 )
  {
    if ( *(double *)&v10 > 1.0 )
      *(double *)&v10 = (float)1.0;
  }
  else
  {
    *(double *)&v10 = (float)0.0;
  }
  (*(void (__thiscall **)(_DWORD, _DWORD, _DWORD))(**(_DWORD **)(a1 + 372) + 280))(
    *(_DWORD *)(a1 + 372),
    v10,
    HIDWORD(v10));
  v28 = operator new(0xD0u);
  LOBYTE(v45) = 12;
  if ( v28 )
    v11 = (_DWORD *)sub_5C5DF0(dword_753420, dword_753710, 5, a1 + 360);
  else
    v11 = 0;
  LOBYTE(v45) = 6;
  *(_DWORD *)(a1 + 376) = v11;
  v12 = (void (__thiscall **)(_DWORD, _DWORD, _DWORD))(*v11 + 280);
  v13 = ((double (__thiscall *)(_BYTE *))*(_DWORD *)(*(_DWORD *)a2 + 228))(a2);
  (*v12)(
    *(_DWORD *)(a1 + 376),
    COERCE_UNSIGNED_INT64(v13 / 0.6499999761581421),
    HIDWORD(COERCE_UNSIGNED_INT64(v13 / 0.6499999761581421)));
  *(_DWORD *)(a1 + 380) = sub_464490(6, a1 + 364, a2[871] == 0);
  v14 = sub_62D190(a2);
  *(_DWORD *)(a1 + 384) = sub_464490(7, a1 + 364, v14);
  if ( *(_BYTE *)(a1 + 404) )
  {
    (*(void (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(a1 + 396) + 124))(*(_DWORD *)(a1 + 396), 0);
    LOWORD(v30) = 0;
    v32 = 7;
    v31 = 0;
    sub_40D1E0(L"[DIALOG_BUTTON_OK]", 18);
    LOBYTE(v45) = 13;
    sub_453240(*(_DWORD *)(a1 + 400), &v29);
    LOBYTE(v45) = 6;
    if ( v32 >= 8 )
      operator delete(v30);
    v15 = *(_DWORD *)(a1 + 368);
    v16 = *(_DWORD *)(v15 + 2384);
    if ( v16
      && *(int *)(v16 + 84) > 0
      && !*(_BYTE *)(v15 + 2536)
      && (*(_DWORD *)(v15 + 2460) && (unsigned __int8)sub_651F30() || *(_DWORD *)(v15 + 2532) != 1) )
    {
      sub_40D030(L"[CREDITS]");
      LOBYTE(v45) = 14;
      sub_453240(*(_DWORD *)(a1 + 392), &v29);
      LOBYTE(v45) = 6;
      sub_401330(&v29);
    }
    else
    {
      (*(void (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(a1 + 392) + 124))(*(_DWORD *)(a1 + 392), 0);
    }
  }
  v17 = *(_DWORD *)(*(_DWORD *)(a1 + 368) + 2332);
  if ( v17 == 42 || v17 == 43 || v17 == 50 )
    (*(void (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(a1 + 396) + 124))(*(_DWORD *)(a1 + 396), 0);
  v18 = *(_DWORD *)(a1 + 368);
  if ( *(_DWORD *)(v18 + 2336) == 2 )
  {
    v19 = *(_DWORD **)(*(_DWORD *)(v18 + 2152) + 372);
    v20 = *(_DWORD *)(*v19 + 2332);
    if ( v20 < 1 || v20 > 15 || *(int *)(*(_DWORD *)(v19[1] + 376) + 108) <= 0 || *(_DWORD *)(*v19 + 2336) != 2 )
      (*(void (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(a1 + 396) + 124))(*(_DWORD *)(a1 + 396), 0);
  }
  v21 = *(_DWORD **)(a1 + 368);
  v22 = v21[596];
  if ( !v22
    || *(int *)(v22 + 84) <= 0 && *(int *)(v22 + 76) < 15
    || v21[584] == 2
    || (v23 = v21[583], v23 == 43)
    || v23 == 50
    || *(_BYTE *)(a1 + 404) )
  {
    (*(void (__thiscall **)(_DWORD, _DWORD))(**(_DWORD **)(a1 + 388) + 124))(*(_DWORD *)(a1 + 388), 0);
  }
  *(_DWORD *)(a1 + 412) = 0;
  *(_DWORD *)(a1 + 416) = 0;
  *(_DWORD *)(a1 + 420) = 0;
  *(_DWORD *)(a1 + 424) = 0;
  *(_DWORD *)(a1 + 408) = dword_752C9C;
  v32 = 15;
  v31 = 0;
  LOBYTE(v30) = 0;
  sub_4032C0("OPTION_DLG_LABEL_FONT_SCALE", 0x1Bu);
  LOBYTE(v45) = 15;
  v24 = **(_DWORD **)(a1 + 408);
  sub_62A130((int)&v29, 1.0);
  (*(void (__thiscall **)(_DWORD, _DWORD, _DWORD))(v24 + 64))(
    *(_DWORD *)(a1 + 408),
    COERCE_UNSIGNED_INT64(1.0),
    HIDWORD(COERCE_UNSIGNED_INT64(1.0)));
  if ( v32 >= 0x10 )
    operator delete(v30);
  return a1;
}

