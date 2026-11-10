int __thiscall sub_4559A0(int this, int a2)
{
  int v2; // edi
  int result; // eax
  int v4; // ecx
  int v5; // esi
  int v6; // ecx
  int v7; // eax
  int v8; // ebx
  int v9; // esi
  int v10; // eax
  int v11; // esi
  double v12; // st7
  int v13; // eax
  int v14; // ebx
  int v15; // ecx
  int v16; // eax
  _DWORD *v17; // ecx
  int v18; // eax
  int v19; // esi
  int v20; // edi
  int v21; // eax
  int v22; // esi
  int v23; // esi
  int v24; // ecx
  _DWORD *v25; // edx
  int v26; // [esp-4h] [ebp-90h]
  float v27; // [esp+0h] [ebp-8Ch]
  float v28; // [esp+0h] [ebp-8Ch]
  float v29; // [esp+4h] [ebp-88h]
  float v30; // [esp+4h] [ebp-88h]
  float v31; // [esp+4h] [ebp-88h]
  int v32; // [esp+8h] [ebp-84h]
  int v33; // [esp+8h] [ebp-84h]
  int v34; // [esp+18h] [ebp-74h]
  float v35; // [esp+1Ch] [ebp-70h]
  float v36; // [esp+20h] [ebp-6Ch]
  float v37; // [esp+20h] [ebp-6Ch]
  float v38; // [esp+24h] [ebp-68h]
  float v39; // [esp+24h] [ebp-68h]
  int v41; // [esp+2Ch] [ebp-60h]
  int v42; // [esp+2Ch] [ebp-60h]
  float v43; // [esp+2Ch] [ebp-60h]
  float v44; // [esp+2Ch] [ebp-60h]
  int v45; // [esp+2Ch] [ebp-60h]
  int v46; // [esp+30h] [ebp-5Ch]
  float v47; // [esp+30h] [ebp-5Ch]
  float v48; // [esp+30h] [ebp-5Ch]
  float v49; // [esp+30h] [ebp-5Ch]
  float v50; // [esp+30h] [ebp-5Ch]
  float v51; // [esp+30h] [ebp-5Ch]
  int v52; // [esp+30h] [ebp-5Ch]
  int v53; // [esp+30h] [ebp-5Ch]
  float v54; // [esp+30h] [ebp-5Ch]
  float v55; // [esp+30h] [ebp-5Ch]
  float v56; // [esp+30h] [ebp-5Ch]
  float v57; // [esp+30h] [ebp-5Ch]
  _BYTE v58[4]; // [esp+34h] [ebp-58h] BYREF
  void *v59; // [esp+38h] [ebp-54h]
  int v60; // [esp+48h] [ebp-44h]
  unsigned int v61; // [esp+4Ch] [ebp-40h]
  float v62; // [esp+50h] [ebp-3Ch] BYREF
  void *v63; // [esp+54h] [ebp-38h]
  float v64; // [esp+58h] [ebp-34h]
  float v65; // [esp+5Ch] [ebp-30h]
  float v66; // [esp+60h] [ebp-2Ch]
  float v67; // [esp+64h] [ebp-28h]
  int v68; // [esp+68h] [ebp-24h]
  float v69; // [esp+6Ch] [ebp-20h]
  int v70; // [esp+70h] [ebp-1Ch]
  int v71; // [esp+74h] [ebp-18h]
  const CHAR *v72; // [esp+78h] [ebp-14h]
  int v73; // [esp+88h] [ebp-4h]

  v2 = this;
  sub_592970(1);
  result = *(_DWORD *)(v2 + 164);
  if ( !*(_DWORD *)(result + 2384) )
    return result;
  if ( !*(_BYTE *)(v2 + 243) )
  {
    v4 = *(_DWORD *)(v2 + 172);
    if ( *(_BYTE *)(v4 + 104) && *(_BYTE *)(v4 + 105) )
    {
      v46 = 1;
      v41 = 1;
    }
    else
    {
      v46 = 0;
      v41 = 0;
    }
    v5 = **(_DWORD **)(*(_DWORD *)(result + 2372) + 8) + 160 * (unsigned __int16)*(_DWORD *)(v2 + 252);
    sub_4823E0("SelectorScreen_BG_Right");
    v62 = -10000.0;
    *(float *)&v63 = -10000.0;
    v64 = -10000.0;
    v71 = 0;
    v65 = -10000.0;
    v70 = 0;
    v66 = -10000.0;
    v67 = -10000.0;
    *(float *)&v68 = -10000.0;
    v72 = &Class;
    v69 = -10000.0;
    sub_4812C0(v5);
    v6 = *(_DWORD *)(v2 + 332);
    v38 = (double)v46 + (float)-10000.0;
    v36 = (double)v41 + (float)-10000.0;
    v7 = (v6 - 1) / 10 + 1;
    v35 = v38;
    v47 = v36;
    if ( v7 > 1 )
    {
      if ( v7 < 6 )
      {
        v34 = (v6 - 1) / 10 + 1;
        v8 = v34;
      }
      else
      {
        v34 = 6;
        v8 = 6;
      }
    }
    else
    {
      v8 = 1;
      v34 = 1;
    }
    v42 = v6 - 10 * v8 + 10;
    if ( (unsigned __int8)sub_4631D0() && (*(int *)(v2 + 332) >= 25 || (unsigned __int8)sub_461560()) )
    {
      v34 = 3;
      v42 = 4;
    }
    else
    {
      if ( v8 == 1 )
      {
        v36 = v36 + 1.0;
      }
      else if ( v8 == 4 )
      {
        v38 = v38 - 1.0;
      }
      if ( v42 == 3 )
        v35 = v35 - 1.0;
    }
    sub_592910(1);
    v9 = *(_DWORD *)(v2 + 172);
    v10 = *(_DWORD *)(v9 + 124) - *(_DWORD *)(v9 + 120);
    v11 = v9 + 108;
    if ( (unsigned int)(v10 >> 4) <= 5 )
      _invalid_parameter_noinfo();
    sub_592880(*(_DWORD *)(v11 + 12) + 80);
    v37 = v36 + 47.0;
    v39 = v38 + 486.0;
    sub_527AF0(a2, LODWORD(v39), LODWORD(v37), v34);
    if ( v42 >= 10 )
    {
      if ( v42 != 10 )
      {
LABEL_29:
        sub_592910(0);
        goto LABEL_30;
      }
      v43 = v47 + 50.0;
      v30 = v43;
      v44 = v35 + 509.0;
      sub_527AF0(a2, LODWORD(v44), LODWORD(v30), 1);
      v32 = 0;
      v50 = v47 + 51.0;
      v29 = v50;
      v51 = v35 + 518.0;
      v12 = v51;
    }
    else
    {
      v32 = v42;
      v48 = v47 + 50.0;
      v29 = v48;
      v49 = v35 + 509.0;
      v12 = v49;
    }
    v27 = v12;
    sub_527AF0(a2, LODWORD(v27), LODWORD(v29), v32);
    goto LABEL_29;
  }
LABEL_30:
  if ( *(_BYTE *)(*(_DWORD *)(v2 + 216) + 100) && (unsigned __int8)sub_533FE0(*(_DWORD *)(*(_DWORD *)(v2 + 164) + 2368)) )
  {
    v13 = *(_DWORD *)(v2 + 216);
    v14 = *(_DWORD *)(v13 + 64) - 694;
    v52 = *(_DWORD *)(v13 + 68) + 36;
    sub_592F90(dword_753754, v14, v52);
    sub_592F90(dword_7534FC, v14 + 17, v52 + 9);
  }
  v15 = *(_DWORD *)(*(_DWORD *)(v2 + 164) + 2372);
  v16 = *(_DWORD *)(v2 + 320);
  if ( v15 )
  {
    v17 = *(_DWORD **)(v15 + 8);
    if ( v16 )
    {
      if ( (unsigned int)(unsigned __int16)v16 < v17[2] && *(_DWORD *)(*v17 + 160 * (unsigned __int16)v16 + 156) == v16 )
      {
        sub_593920(0, 0, 800, 560);
        sub_482230();
        sub_5938D0(a2);
        v2 = this;
      }
    }
  }
  sub_593AA0(-800 - *(_DWORD *)(v2 + 64), 0);
  sub_482230();
  sub_593AA0(*(_DWORD *)(this + 64) + 800, 0);
  v53 = this + 304;
  v45 = 3;
  do
  {
    sub_482230();
    v53 += 4;
    --v45;
  }
  while ( v45 );
  if ( *(_BYTE *)(*(_DWORD *)(this + 164) + 284) )
  {
    sub_592860(dword_752B48);
    v18 = sub_55BBB0(200, 200, 200);
    sub_592880(v18);
    if ( byte_755DF5 )
    {
      v68 = 7;
      v67 = 0.0;
      LOWORD(v63) = 0;
      sub_40D1E0(&off_70D8A0, 46);
      v73 = 0;
      sub_592F40(&v62, 27, 594);
      v73 = -1;
      if ( (unsigned int)v68 >= 8 )
        operator delete(v63);
    }
    else
    {
      v61 = 7;
      v60 = 0;
      LOWORD(v59) = 0;
      sub_40D1E0(&off_70D900, 29);
      v73 = 1;
      sub_592F40(v58, 27, 594);
      v73 = -1;
      if ( v61 >= 8 )
        operator delete(v59);
      v61 = 7;
      v60 = 0;
      LOWORD(v59) = 0;
    }
  }
  v19 = **(_DWORD **)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2372) + 8) + 160 * (unsigned __int16)*(_DWORD *)(this + 252);
  sub_4823E0("SelectorScreen_BG_Left");
  v62 = -10000.0;
  *(float *)&v63 = -10000.0;
  v64 = -10000.0;
  v71 = 0;
  v65 = -10000.0;
  v70 = 0;
  v66 = -10000.0;
  v67 = -10000.0;
  *(float *)&v68 = -10000.0;
  v72 = &Class;
  v69 = -10000.0;
  sub_4812C0(v19);
  v20 = this;
  if ( *(_BYTE *)(this + 344) )
  {
    v21 = *(_DWORD *)(*(_DWORD *)(this + 164) + 2384);
    if ( v21
      && *(int *)(v21 + 84) > 0
      && (v22 = 48 - sub_4630C0(0), v23 = v22 - sub_4630C0(1), v23 - sub_4630C0(3) <= 0) )
    {
      v33 = 1;
      v54 = *(float *)&v63 + 345.0;
      v31 = v54;
      v55 = v62 + 12.0;
      v28 = v55;
      v26 = a2;
    }
    else
    {
      v33 = 0;
      v56 = *(float *)&v63 + 345.0;
      v31 = v56;
      v57 = v62 + 12.0;
      v28 = v57;
      v26 = a2;
    }
    sub_527AF0(v26, LODWORD(v28), LODWORD(v31), v33);
    v24 = *(_DWORD *)(this + 248);
    v25 = **(_DWORD ***)(*(_DWORD *)(this + 164) + 2372);
    if ( v24
      && (unsigned int)(unsigned __int16)v24 < v25[2]
      && *(_DWORD *)(*v25 + 44 * (unsigned __int16)v24 + 40) == v24 )
    {
      sub_52D8C0();
      v20 = this;
    }
  }
  return sub_530180(*(_DWORD *)(v20 + 340));
}
