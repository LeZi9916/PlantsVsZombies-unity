int __stdcall sub_4539E0(int a1, int a2)
{
  int v2; // eax
  int v3; // eax
  bool v4; // cf
  int v5; // ecx
  int v6; // edx
  int v7; // eax
  float v8; // edx
  int v9; // eax
  float v10; // edx
  int v11; // eax
  float v12; // edx
  float v13; // edx
  int v14; // eax
  float v15; // edx
  float v16; // edx
  int v17; // eax
  int v18; // eax
  int v19; // eax
  int v20; // ecx
  int v21; // edx
  float v22; // ecx
  int v23; // eax
  float v24; // edx
  float v25; // ecx
  int v26; // eax
  float v27; // edx
  float v28; // ecx
  int v29; // eax
  float v30; // edx
  float v31; // ecx
  int v32; // eax
  float v33; // edx
  int v34; // eax
  int v35; // eax
  int v36; // eax
  int v37; // ecx
  int v38; // edx
  float v39; // ecx
  int v40; // eax
  float v41; // edx
  float v42; // ecx
  int v43; // eax
  float v44; // edx
  float v45; // ecx
  int v46; // eax
  float v47; // edx
  float v48; // ecx
  int v49; // eax
  float v50; // edx
  int v51; // eax
  int v52; // eax
  int v53; // eax
  int v54; // ecx
  int v55; // edx
  float v56; // ecx
  int v57; // eax
  float v58; // edx
  float v59; // ecx
  int v60; // eax
  float v61; // edx
  float v62; // ecx
  int v63; // eax
  float v64; // edx
  float v65; // ecx
  int v66; // eax
  float v67; // edx
  int v68; // eax
  int v69; // eax
  void (__thiscall **v70)(_DWORD, int, _DWORD, unsigned int, int); // edi
  unsigned int NodeId; // eax
  int v72; // eax
  int v73; // eax
  void (__thiscall **v74)(_DWORD, _DWORD, _DWORD, unsigned int, int); // edi
  unsigned int v75; // eax
  int v76; // eax
  void (__thiscall **v77)(_DWORD, int, int, unsigned int, int); // edi
  int v78; // eax
  int v79; // eax
  int v80; // eax
  int v81; // eax
  int v82; // eax
  int v83; // eax
  int v84; // eax
  _DWORD *v85; // edi
  _DWORD *v86; // ecx
  int v87; // eax
  int v88; // eax
  int v89; // eax
  int v90; // eax
  int v91; // eax
  int v92; // eax
  int v93; // edi
  int v94; // ebx
  int v95; // esi
  int v96; // edi
  int v97; // eax
  int v98; // eax
  double v99; // st7
  bool v100; // c0
  bool v101; // c3
  float v102; // esi
  int v103; // eax
  bool v104; // sf
  int v105; // eax
  bool v106; // cc
  int v107; // edi
  int v108; // esi
  int v109; // edi
  double v110; // st7
  bool v111; // c0
  bool v112; // c3
  int v113; // ecx
  _DWORD *v114; // edx
  int v115; // edi
  int v116; // esi
  void *v117; // eax
  int v118; // eax
  int v119; // ecx
  int v120; // eax
  int v121; // ecx
  unsigned int v123; // [esp+100h] [ebp-64h]
  int v124; // [esp+104h] [ebp-60h]
  int v125; // [esp+104h] [ebp-60h]
  int v126; // [esp+104h] [ebp-60h]
  float v127; // [esp+118h] [ebp-4Ch] BYREF
  float v128; // [esp+11Ch] [ebp-48h]
  int v129; // [esp+120h] [ebp-44h] BYREF
  void *v130; // [esp+124h] [ebp-40h]
  int v131; // [esp+134h] [ebp-30h]
  unsigned int v132; // [esp+138h] [ebp-2Ch]
  int v133[6]; // [esp+13Ch] [ebp-28h] BYREF
  unsigned int v134; // [esp+154h] [ebp-10h]
  int v135; // [esp+160h] [ebp-4h]

  sub_5C4970();
  v135 = 0;
  *(_DWORD *)(a1 + 160) = &Sexy::ButtonListener::`vftable';
  *(_DWORD *)a1 = &off_70DCD0;
  *(_DWORD *)(a1 + 160) = &off_70DDE8;
  v132 = 15;
  v131 = 0;
  LOBYTE(v130) = 0;
  sub_4032C0("DelayLoad_Zombatar", 0x12u);
  LOBYTE(v135) = 1;
  sub_528720(*(_DWORD *)(dword_755F28 + 1844), (int)&v129);
  LOBYTE(v135) = 0;
  if ( v132 >= 0x10 )
    operator delete(v130);
  *(_DWORD *)(a1 + 164) = a2;
  *(_DWORD *)(a1 + 332) = 1;
  *(_BYTE *)(a1 + 336) = 0;
  *(_BYTE *)(a1 + 344) = 0;
  if ( operator new(0x7Cu) )
    v2 = sub_52FAB0();
  else
    v2 = 0;
  *(_DWORD *)(a1 + 340) = v2;
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 2;
  v3 = sub_453910(100, a1 + 160, &v129, 0, dword_7536A4);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 172) = v3;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, _DWORD, int))(**(_DWORD **)(a1 + 172) + 160))(
    *(_DWORD *)(a1 + 172),
    800,
    0,
    *(_DWORD *)(dword_753774 + 36),
    125);
  v5 = *(_DWORD *)(a1 + 172);
  v127 = 7.0;
  *(_BYTE *)(v5 + 81) = 0;
  v6 = *(_DWORD *)(a1 + 172);
  v128 = 1.0;
  *(_BYTE *)(v6 + 281) = 1;
  v7 = *(_DWORD *)(a1 + 172);
  v8 = v127;
  v127 = 328.0;
  *(_BYTE *)(v7 + 101) = 0;
  v9 = *(_DWORD *)(a1 + 172);
  *(float *)(v9 + 360) = v8;
  v10 = v128;
  v128 = 30.0;
  *(float *)(v9 + 364) = v10;
  v11 = *(_DWORD *)(a1 + 172);
  v12 = v127;
  v127 = 314.0;
  *(float *)(v11 + 368) = v12;
  v13 = v128;
  v128 = 125.0;
  *(float *)(v11 + 372) = v13;
  v14 = *(_DWORD *)(a1 + 172);
  v15 = v127;
  v127 = 1.0;
  *(float *)(v14 + 376) = v15;
  v16 = v128;
  v128 = 78.0;
  *(float *)(v14 + 380) = v16;
  v17 = *(_DWORD *)(a1 + 172);
  *(float *)(v17 + 384) = v127;
  *(float *)(v17 + 388) = v128;
  *(_BYTE *)(*(_DWORD *)(a1 + 172) + 356) = 1;
  LOWORD(v130) = 0;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 3;
  v18 = sub_453910(101, a1 + 160, &v129, 0, dword_7531D4);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 176) = v18;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, _DWORD, int))(**(_DWORD **)(a1 + 176) + 160))(
    *(_DWORD *)(a1 + 176),
    800,
    0,
    *(_DWORD *)(dword_752F68 + 36),
    130);
  v19 = *(_DWORD *)(a1 + 176);
  v127 = 4.0;
  *(_BYTE *)(v19 + 81) = 0;
  v20 = *(_DWORD *)(a1 + 176);
  v128 = 2.0;
  *(_BYTE *)(v20 + 281) = 0;
  v21 = *(_DWORD *)(a1 + 176);
  v22 = v127;
  v127 = 312.0;
  *(_BYTE *)(v21 + 101) = 0;
  v23 = *(_DWORD *)(a1 + 176);
  v24 = v128;
  v128 = 51.0;
  *(float *)(v23 + 360) = v22;
  v25 = v127;
  v127 = 296.0;
  *(float *)(v23 + 364) = v24;
  v26 = *(_DWORD *)(a1 + 176);
  v27 = v128;
  v128 = 130.0;
  *(float *)(v26 + 368) = v25;
  v28 = v127;
  v127 = 7.0;
  *(float *)(v26 + 372) = v27;
  v29 = *(_DWORD *)(a1 + 176);
  v30 = v128;
  v128 = 77.0;
  *(float *)(v29 + 376) = v28;
  v31 = v127;
  *(float *)(v29 + 380) = v30;
  v32 = *(_DWORD *)(a1 + 176);
  v33 = v128;
  *(float *)(v32 + 384) = v31;
  *(float *)(v32 + 388) = v33;
  v34 = *(_DWORD *)(a1 + 176);
  LOWORD(v130) = 0;
  *(_BYTE *)(v34 + 356) = 1;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 4;
  v35 = sub_453910(102, a1 + 160, &v129, 0, dword_752B28);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 180) = v35;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, _DWORD, int))(**(_DWORD **)(a1 + 180) + 160))(
    *(_DWORD *)(a1 + 180),
    800,
    0,
    *(_DWORD *)(dword_7537A8 + 36),
    121);
  v36 = *(_DWORD *)(a1 + 180);
  v127 = 2.0;
  *(_BYTE *)(v36 + 81) = 0;
  v37 = *(_DWORD *)(a1 + 180);
  v128 = 0.0;
  *(_BYTE *)(v37 + 281) = 0;
  v38 = *(_DWORD *)(a1 + 180);
  v39 = v127;
  v127 = 281.0;
  *(_BYTE *)(v38 + 101) = 0;
  v40 = *(_DWORD *)(a1 + 180);
  v41 = v128;
  v128 = 55.0;
  *(float *)(v40 + 360) = v39;
  v42 = v127;
  v127 = 268.0;
  *(float *)(v40 + 364) = v41;
  v43 = *(_DWORD *)(a1 + 180);
  v44 = v128;
  v128 = 121.0;
  *(float *)(v43 + 368) = v42;
  v45 = v127;
  v127 = 3.0;
  *(float *)(v43 + 372) = v44;
  v46 = *(_DWORD *)(a1 + 180);
  v47 = v128;
  v128 = 60.0;
  *(float *)(v46 + 376) = v45;
  v48 = v127;
  *(float *)(v46 + 380) = v47;
  v49 = *(_DWORD *)(a1 + 180);
  v50 = v128;
  *(float *)(v49 + 384) = v48;
  *(float *)(v49 + 388) = v50;
  v51 = *(_DWORD *)(a1 + 180);
  LOWORD(v130) = 0;
  *(_BYTE *)(v51 + 356) = 1;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 5;
  v52 = sub_453910(110, a1 + 160, &v129, 0, dword_752CDC);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 220) = v52;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, _DWORD, int))(**(_DWORD **)(a1 + 220) + 160))(
    *(_DWORD *)(a1 + 220),
    800,
    0,
    *(_DWORD *)(dword_7535B4 + 36),
    124);
  v53 = *(_DWORD *)(a1 + 220);
  v127 = 7.0;
  *(_BYTE *)(v53 + 81) = 0;
  v54 = *(_DWORD *)(a1 + 220);
  v128 = 1.0;
  *(_BYTE *)(v54 + 281) = 0;
  v55 = *(_DWORD *)(a1 + 220);
  v56 = v127;
  v127 = 267.0;
  *(_BYTE *)(v55 + 101) = 0;
  v57 = *(_DWORD *)(a1 + 220);
  v58 = v128;
  v128 = 62.0;
  *(float *)(v57 + 360) = v56;
  v59 = v127;
  v127 = 257.0;
  *(float *)(v57 + 364) = v58;
  v60 = *(_DWORD *)(a1 + 220);
  v61 = v128;
  v128 = 124.0;
  *(float *)(v60 + 368) = v59;
  v62 = v127;
  v127 = 7.0;
  *(float *)(v60 + 372) = v61;
  v63 = *(_DWORD *)(a1 + 220);
  v64 = v128;
  v128 = 57.0;
  *(float *)(v63 + 376) = v62;
  v65 = v127;
  *(float *)(v63 + 380) = v64;
  v66 = *(_DWORD *)(a1 + 220);
  v67 = v128;
  *(float *)(v66 + 384) = v65;
  *(float *)(v66 + 388) = v67;
  v68 = *(_DWORD *)(a1 + 220);
  LOWORD(v130) = 0;
  *(_BYTE *)(v68 + 356) = 1;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 6;
  v69 = sub_453910(115, a1 + 160, &v129, 0, dword_752F20);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 168) = v69;
  if ( !v4 )
    operator delete(v130);
  v70 = (void (__thiscall **)(_DWORD, int, _DWORD, unsigned int, int))(**(_DWORD **)(a1 + 168) + 160);
  v124 = unknown_libname_11(dword_752BB0);
  NodeId = Concurrency::details::ExecutionResource::GetNodeId(dword_752BB0);
  (*v70)(*(_DWORD *)(a1 + 168), 800, 0, NodeId, v124);
  *(_BYTE *)(*(_DWORD *)(a1 + 168) + 81) = 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 168) + 281) = 1;
  v72 = *(_DWORD *)(a1 + 168);
  LOWORD(v130) = 0;
  *(_BYTE *)(v72 + 101) = 0;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 7;
  v73 = sub_453910(111, a1 + 160, &v129, 0, dword_753350);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 196) = v73;
  if ( !v4 )
    operator delete(v130);
  v74 = (void (__thiscall **)(_DWORD, _DWORD, _DWORD, unsigned int, int))(**(_DWORD **)(a1 + 196) + 160);
  v125 = unknown_libname_11(dword_752F14);
  v75 = Concurrency::details::ExecutionResource::GetNodeId(dword_752F14);
  (*v74)(*(_DWORD *)(a1 + 196), 0, 0, v75, v125);
  *(_BYTE *)(*(_DWORD *)(a1 + 196) + 81) = 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 196) + 281) = 1;
  *(_BYTE *)(*(_DWORD *)(a1 + 196) + 101) = 0;
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 8;
  v76 = sub_453910(113, a1 + 160, &v129, 0, dword_753468);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 200) = v76;
  if ( !v4 )
    operator delete(v130);
  v77 = (void (__thiscall **)(_DWORD, int, int, unsigned int, int))(**(_DWORD **)(a1 + 200) + 160);
  v126 = unknown_libname_11(dword_752D50);
  v123 = Concurrency::details::ExecutionResource::GetNodeId(dword_752D50);
  v78 = unknown_libname_11(dword_752D50);
  (*v77)(*(_DWORD *)(a1 + 200), 820, *(_DWORD *)(*(_DWORD *)(a1 + 164) + 196) - v78 - 35, v123, v126);
  *(_BYTE *)(*(_DWORD *)(a1 + 200) + 81) = 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 200) + 281) = 0;
  v79 = *(_DWORD *)(a1 + 200);
  LOWORD(v130) = 0;
  *(_BYTE *)(v79 + 101) = 0;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 9;
  v80 = sub_453910(109, a1 + 160, &v129, 0, dword_752FBC);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 216) = v80;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, int, int))(**(_DWORD **)(a1 + 216) + 160))(
    *(_DWORD *)(a1 + 216),
    800,
    0,
    130,
    130);
  *(_BYTE *)(*(_DWORD *)(a1 + 216) + 101) = 0;
  *(_BYTE *)(*(_DWORD *)(a1 + 216) + 81) = 0;
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 10;
  v81 = sub_453910(103, a1 + 160, &v129, 0, dword_7530DC);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 184) = v81;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, _DWORD, int))(**(_DWORD **)(a1 + 184) + 160))(
    *(_DWORD *)(a1 + 184),
    800,
    0,
    *(_DWORD *)(dword_753034 + 36),
    *(_DWORD *)(dword_753034 + 40) + 23);
  *(_BYTE *)(*(_DWORD *)(a1 + 184) + 281) = 1;
  *(_BYTE *)(*(_DWORD *)(a1 + 184) + 101) = 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 184) + 352) = 15;
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 11;
  v82 = sub_453910(104, a1 + 160, &v129, 0, dword_753630);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 192) = v82;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, _DWORD, int))(**(_DWORD **)(a1 + 192) + 160))(
    *(_DWORD *)(a1 + 192),
    800,
    0,
    *(_DWORD *)(dword_753104 + 36),
    *(_DWORD *)(dword_753104 + 40) + 33);
  *(_BYTE *)(*(_DWORD *)(a1 + 192) + 281) = 1;
  *(_BYTE *)(*(_DWORD *)(a1 + 192) + 101) = 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 192) + 352) = 30;
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 12;
  v83 = sub_453910(105, a1 + 160, &v129, 0, dword_752BA4);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 188) = v83;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, int, int))(**(_DWORD **)(a1 + 188) + 160))(
    *(_DWORD *)(a1 + 188),
    800,
    0,
    *(_DWORD *)(dword_752F80 + 36) + 10,
    *(_DWORD *)(dword_752F80 + 40) + 10);
  *(_BYTE *)(*(_DWORD *)(a1 + 188) + 281) = 1;
  *(_BYTE *)(*(_DWORD *)(a1 + 188) + 101) = 0;
  *(_DWORD *)(*(_DWORD *)(a1 + 188) + 348) = 5;
  *(_DWORD *)(*(_DWORD *)(a1 + 188) + 352) = 5;
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 13;
  v84 = sub_453910(106, a1 + 160, &v129, 0, dword_753350);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 224) = v84;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, _DWORD, int, int))(**(_DWORD **)(a1 + 224) + 160))(
    *(_DWORD *)(a1 + 224),
    800,
    0,
    250,
    30);
  *(_BYTE *)(*(_DWORD *)(a1 + 224) + 281) = 1;
  *(_BYTE *)(*(_DWORD *)(a1 + 224) + 101) = 0;
  v85 = operator new(0xA4u);
  a2 = (int)v85;
  LOBYTE(v135) = 14;
  if ( v85 )
  {
    sub_5C4970();
    *v85 = &off_70CE58;
    v85[40] = a1;
    *((_BYTE *)v85 + 101) = 0;
    *((_BYTE *)v85 + 80) = 1;
    v86 = v85;
  }
  else
  {
    v86 = 0;
  }
  LOBYTE(v135) = 0;
  *(_DWORD *)(a1 + 228) = v86;
  (*(void (__thiscall **)(_DWORD *, int, _DWORD, int, int))(*v86 + 160))(v86, 800, 0, 800, 600);
  v132 = 7;
  v131 = 0;
  LOWORD(v130) = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 15;
  v87 = sub_453910(107, a1 + 160, &v129, 0, dword_753404);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 208) = v87;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, int, _DWORD, _DWORD))(**(_DWORD **)(a1 + 208) + 160))(
    *(_DWORD *)(a1 + 208),
    1205,
    484,
    *(_DWORD *)(dword_752C44 + 36),
    *(_DWORD *)(dword_752C44 + 40));
  v88 = *(_DWORD *)(a1 + 208);
  LOWORD(v130) = 0;
  *(_BYTE *)(v88 + 101) = 0;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 16;
  v89 = sub_453910(108, a1 + 160, &v129, 0, dword_752BF0);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 212) = v89;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, int, _DWORD, _DWORD))(**(_DWORD **)(a1 + 212) + 160))(
    *(_DWORD *)(a1 + 212),
    1127,
    428,
    *(_DWORD *)(dword_7533EC + 36),
    *(_DWORD *)(dword_7533EC + 40));
  v90 = *(_DWORD *)(a1 + 212);
  LOWORD(v130) = 0;
  *(_BYTE *)(v90 + 101) = 0;
  v132 = 7;
  v131 = 0;
  sub_40D1E0(&unk_708460, 0);
  LOBYTE(v135) = 17;
  v91 = sub_453910(114, a1 + 160, &v129, 0, dword_752E20);
  LOBYTE(v135) = 0;
  v4 = v132 < 8;
  *(_DWORD *)(a1 + 204) = v91;
  if ( !v4 )
    operator delete(v130);
  (*(void (__thiscall **)(_DWORD, int, int, _DWORD, _DWORD))(**(_DWORD **)(a1 + 204) + 160))(
    *(_DWORD *)(a1 + 204),
    *(_DWORD *)(*(_DWORD *)(a1 + 164) + 192) - 150,
    455,
    *(_DWORD *)(dword_75381C + 36),
    *(_DWORD *)(dword_75381C + 40));
  if ( *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2400) + 8) != 7 )
  {
    sub_468F30();
    sub_469140(-1);
  }
  v92 = *(_DWORD *)(a1 + 164);
  *(_BYTE *)(a1 + 232) = 0;
  *(_DWORD *)(a1 + 236) = 0;
  *(_BYTE *)(a1 + 240) = 0;
  *(_BYTE *)(a1 + 241) = 0;
  *(_BYTE *)(a1 + 242) = 0;
  *(_BYTE *)(a1 + 345) = 0;
  *(_DWORD *)(a1 + 248) = 0;
  *(_BYTE *)(a1 + 243) = 0;
  *(_BYTE *)(a1 + 244) = 1;
  v93 = *(_DWORD *)(*(_DWORD *)(v92 + 2372) + 8);
  v94 = sub_483BC0();
  *(_DWORD *)(v94 + 96) = v93;
  *(_DWORD *)(v94 + 104) = 0;
  sub_482AA0(101, 0);
  *(_DWORD *)v94 = 101;
  sub_480E40(v94, 0.5, 0.5);
  *(float *)(v94 + 8) = 30.0;
  *(float *)(v94 + 4) = 0.0;
  *(float *)(v94 + 148) = -1.0;
  *(_DWORD *)(v94 + 16) = 3;
  *(_DWORD *)(v94 + 92) = 0;
  sub_482690(v94 + 24, v94 + 28);
  sub_482F10(v94, "flower", -1);
  sub_482F10(v94, "leaf", -1);
  sub_482EC0("SelectorScreen_BG", 1);
  *(_DWORD *)(a1 + 252) = *(_DWORD *)(v94 + 156);
  *(_DWORD *)(a1 + 328) = 0;
  sub_482690(&a2, &v127);
  *(_DWORD *)(v94 + 32) = a2 + LODWORD(v127) - 1;
  a2 = 0;
  LODWORD(v127) = a1 + 280;
  do
  {
    v95 = *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2372) + 8);
    v96 = sub_483BC0();
    *(_DWORD *)(v96 + 96) = v95;
    *(_DWORD *)(v96 + 104) = 0;
    sub_482AA0(101, 0);
    *(_DWORD *)v96 = 101;
    sub_480E40(v96, 0.5, 0.5);
    v132 = 15;
    v131 = 0;
    LOBYTE(v130) = 0;
    LOBYTE(v135) = 18;
    if ( a2 > 1 )
    {
      v98 = sub_5F0BD0((int)v133, "anim_cloud%d", a2 + 2);
      LOBYTE(v135) = 20;
      sub_401D80(v98, 0, -1);
      if ( v134 < 0x10 )
        goto LABEL_47;
    }
    else
    {
      v97 = sub_5F0BD0((int)v133, "anim_cloud%d", a2 + 1);
      LOBYTE(v135) = 19;
      sub_401D80(v97, 0, -1);
      if ( v134 < 0x10 )
        goto LABEL_47;
    }
    operator delete((void *)v133[1]);
LABEL_47:
    v99 = 0.0;
    *(_DWORD *)(v96 + 16) = 3;
    v100 = *(float *)(v96 + 8) > 0.0;
    v101 = 0.0 == *(float *)(v96 + 8);
    *(_DWORD *)(v96 + 92) = 0;
    if ( !v100 && !v101 )
      v99 = 0.99999988;
    *(float *)(v96 + 4) = v99;
    *(float *)(v96 + 148) = -1.0;
    sub_482690(v96 + 24, v96 + 28);
    v102 = v127;
    *(_DWORD *)(LODWORD(v127) - 24) = *(_DWORD *)(v96 + 156);
    v103 = sub_5EECC0(8001);
    v104 = v103 - 6000 < 0;
    v105 = v103 - 6000;
    *(_DWORD *)LODWORD(v102) = v105;
    if ( v104 )
    {
      LODWORD(v127) = -v105;
      *(float *)(v96 + 4) = (double)-v105 / 6000.0;
      *(float *)(v96 + 8) = 0.5;
      *(_DWORD *)LODWORD(v102) = 0;
    }
    else
    {
      *(float *)(v96 + 8) = 0.0;
    }
    LOBYTE(v135) = 0;
    if ( v132 >= 0x10 )
      operator delete(v130);
    v106 = ++a2 < 6;
    LODWORD(v127) = LODWORD(v102) + 4;
  }
  while ( v106 );
  v127 = 0.0;
  a2 = a1 + 304;
  do
  {
    v107 = *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2372) + 8);
    v108 = sub_483BC0();
    *(_DWORD *)(v108 + 96) = v107;
    *(_DWORD *)(v108 + 104) = 0;
    sub_482AA0(101, 0);
    *(_DWORD *)v108 = 101;
    sub_480E40(v108, 0.5, 0.5);
    v109 = LODWORD(v127) + 1;
    sub_5F0BD0((int)&v129, "anim_flower%d", LODWORD(v127) + 1);
    v110 = 0.0;
    *(_DWORD *)(v108 + 16) = 3;
    v111 = *(float *)(v108 + 8) > 0.0;
    v112 = 0.0 == *(float *)(v108 + 8);
    *(_DWORD *)(v108 + 92) = 0;
    if ( !v111 && !v112 )
      v110 = 0.99999988;
    *(float *)(v108 + 4) = v110;
    *(float *)(v108 + 148) = -1.0;
    sub_482690(v108 + 24, v108 + 28);
    *(float *)(v108 + 8) = 0.0;
    sub_482430();
    v113 = *(_DWORD *)(v108 + 156);
    v114 = (_DWORD *)a2;
    *(_BYTE *)(v108 + 100) = 0;
    v4 = v132 < 0x10;
    *v114 = v113;
    if ( !v4 )
      operator delete(v130);
    a2 += 4;
    v127 = *(float *)&v109;
  }
  while ( v109 < 3 );
  v115 = *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(a1 + 164) + 2372) + 8);
  v116 = sub_483BC0();
  *(_DWORD *)(v116 + 96) = v115;
  *(_DWORD *)(v116 + 104) = 0;
  sub_482AA0(101, 0);
  *(_DWORD *)v116 = 101;
  sub_480E40(v116, 0.5, 0.5);
  *(float *)(v116 + 8) = 6.0;
  *(float *)(v116 + 4) = 0.0;
  *(float *)(v116 + 148) = -1.0;
  *(_DWORD *)(v116 + 16) = 0;
  *(_DWORD *)(v116 + 92) = 0;
  sub_482690(v116 + 24, v116 + 28);
  *(float *)(v116 + 8) = 0.0;
  *(_DWORD *)(a1 + 316) = *(_DWORD *)(v116 + 156);
  *(_DWORD *)(a1 + 324) = 50;
  sub_455420(0);
  (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 164) + 216))(*(_DWORD *)(a1 + 164), dword_752CBC);
  *(_DWORD *)(a1 + 348) = 0;
  *(_DWORD *)(a1 + 352) = 0;
  *(_DWORD *)(a1 + 356) = 0;
  *(_DWORD *)(a1 + 360) = 0;
  *(_DWORD *)(a1 + 364) = 0;
  v117 = operator new(0x198u);
  a2 = (int)v117;
  LOBYTE(v135) = 21;
  if ( v117 )
    v118 = sub_53CBF0(v117, *(_DWORD *)(a1 + 164));
  else
    v118 = 0;
  LOBYTE(v135) = 0;
  v119 = *(_DWORD *)(a1 + 164);
  *(_DWORD *)(a1 + 368) = v118;
  (*(void (__thiscall **)(int, int, _DWORD, _DWORD, _DWORD))(*(_DWORD *)v118 + 160))(
    v118,
    1600,
    0,
    *(_DWORD *)(v119 + 192),
    *(_DWORD *)(v119 + 196));
  a2 = (int)operator new(0xD0u);
  LOBYTE(v135) = 22;
  if ( a2 )
    v120 = sub_408830(*(_DWORD *)(a1 + 164));
  else
    v120 = 0;
  LOBYTE(v135) = 0;
  v121 = *(_DWORD *)(a1 + 164);
  *(_DWORD *)(a1 + 372) = v120;
  (*(void (__thiscall **)(int, int, _DWORD))(*(_DWORD *)v120 + 164))(v120, 800, *(_DWORD *)(v121 + 196));
  sub_5C0720(*(_DWORD *)(a1 + 172));
  sub_5C0720(*(_DWORD *)(a1 + 176));
  sub_5C0720(*(_DWORD *)(a1 + 180));
  sub_5C0720(*(_DWORD *)(a1 + 184));
  sub_5C0720(*(_DWORD *)(a1 + 188));
  sub_5C0720(*(_DWORD *)(a1 + 192));
  sub_5C0720(*(_DWORD *)(a1 + 208));
  sub_5C0720(*(_DWORD *)(a1 + 212));
  sub_5C0720(*(_DWORD *)(a1 + 220));
  sub_5C0720(*(_DWORD *)(a1 + 216));
  sub_5C0720(*(_DWORD *)(a1 + 224));
  sub_5C0720(*(_DWORD *)(a1 + 196));
  sub_5C0720(*(_DWORD *)(a1 + 368));
  sub_5C0720(*(_DWORD *)(a1 + 200));
  sub_5C0720(*(_DWORD *)(a1 + 372));
  sub_5C0720(*(_DWORD *)(a1 + 204));
  sub_5C0720(*(_DWORD *)(a1 + 228));
  sub_5C0720(*(_DWORD *)(a1 + 168));
  return a1;
}
