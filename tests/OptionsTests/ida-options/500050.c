int __usercall sub_500050@<eax>(int *a1@<eax>, unsigned int a2@<edx>, int *a3@<ecx>, int a4, int a5)
{
  int v5; // ebx
  int v6; // edi
  int *v7; // ecx
  int *v8; // esi
  int *v9; // ebp
  int *v10; // eax
  int result; // eax
  __int64 v12; // rax
  int v13; // esi
  int v14; // ecx
  int v15; // eax
  int v16; // ecx
  int v17; // edx
  int v18; // ebp
  signed int v19; // ebp
  BOOL v20; // esi
  signed int v21; // edx
  int v22; // ecx
  unsigned int v23; // ebx
  int v24; // ecx
  int v25; // eax
  unsigned int v26; // ebp
  unsigned int v27; // ecx
  unsigned int v28; // esi
  unsigned int v29; // esi
  unsigned int v30; // eax
  unsigned int v31; // ecx
  unsigned int v32; // eax
  unsigned int v33; // eax
  int v34; // eax
  int v35; // ecx
  int v36; // edx
  int v37; // ebp
  signed int v38; // ebp
  BOOL v39; // esi
  signed int v40; // edx
  int v41; // ecx
  unsigned int v42; // ebx
  int v43; // ecx
  int v44; // eax
  unsigned int v45; // ecx
  unsigned int v46; // esi
  unsigned int v47; // ebp
  unsigned int v48; // edx
  unsigned int v49; // eax
  unsigned int v50; // ecx
  unsigned int v51; // eax
  unsigned int v52; // eax
  int v53; // ecx
  int v54; // ecx
  int v55; // eax
  int v56; // esi
  signed int v57; // esi
  BOOL v58; // edx
  signed int v59; // ecx
  int v60; // edx
  bool v61; // sf
  bool v62; // of
  int v63; // eax
  unsigned int v64; // ebx
  int v65; // eax
  unsigned int v66; // ebp
  int v67; // eax
  int v68; // edx
  unsigned int v69; // ebp
  unsigned int v70; // esi
  unsigned int v71; // ecx
  unsigned int v72; // eax
  unsigned int v73; // ecx
  unsigned int v74; // eax
  unsigned int v75; // eax
  int v76; // eax
  int v77; // ecx
  int v78; // edx
  int v79; // ebp
  signed int v80; // ebp
  BOOL v81; // esi
  signed int v82; // edx
  int v83; // ecx
  unsigned int v84; // ebx
  int v85; // ecx
  int v86; // eax
  unsigned int v87; // ecx
  unsigned int v88; // esi
  unsigned int v89; // ebp
  unsigned int v90; // edx
  unsigned int v91; // eax
  unsigned int v92; // ecx
  unsigned int v93; // eax
  unsigned int v94; // eax
  int v95; // [esp+10h] [ebp-B0h]
  unsigned int v96; // [esp+10h] [ebp-B0h]
  int v97; // [esp+10h] [ebp-B0h]
  unsigned int v98; // [esp+10h] [ebp-B0h]
  unsigned int v99; // [esp+10h] [ebp-B0h]
  int v100; // [esp+14h] [ebp-ACh]
  int v101; // [esp+14h] [ebp-ACh]
  int v102; // [esp+14h] [ebp-ACh]
  unsigned int v103; // [esp+14h] [ebp-ACh]
  int v104; // [esp+18h] [ebp-A8h]
  int v105; // [esp+18h] [ebp-A8h]
  BOOL v106; // [esp+18h] [ebp-A8h]
  unsigned int v107; // [esp+18h] [ebp-A8h]
  BOOL v108; // [esp+18h] [ebp-A8h]
  int v109; // [esp+18h] [ebp-A8h]
  int v110; // [esp+1Ch] [ebp-A4h]
  unsigned int v111; // [esp+1Ch] [ebp-A4h]
  unsigned int v112; // [esp+1Ch] [ebp-A4h]
  unsigned int v113; // [esp+1Ch] [ebp-A4h]
  int v114; // [esp+20h] [ebp-A0h]
  int v115; // [esp+20h] [ebp-A0h]
  unsigned int v116; // [esp+20h] [ebp-A0h]
  int v117; // [esp+20h] [ebp-A0h]
  _WORD *v118; // [esp+20h] [ebp-A0h]
  int v119; // [esp+24h] [ebp-9Ch]
  int v120; // [esp+24h] [ebp-9Ch]
  _WORD *v121; // [esp+24h] [ebp-9Ch]
  int v122; // [esp+28h] [ebp-98h]
  int v123; // [esp+28h] [ebp-98h]
  unsigned int v124; // [esp+28h] [ebp-98h]
  int v125; // [esp+28h] [ebp-98h]
  int v126; // [esp+2Ch] [ebp-94h]
  int v127; // [esp+2Ch] [ebp-94h]
  int v128; // [esp+30h] [ebp-90h]
  int v129; // [esp+30h] [ebp-90h]
  int v130; // [esp+30h] [ebp-90h]
  int v131; // [esp+34h] [ebp-8Ch]
  int v132; // [esp+34h] [ebp-8Ch]
  int v133; // [esp+34h] [ebp-8Ch]
  int v134; // [esp+38h] [ebp-88h]
  _WORD *v135; // [esp+38h] [ebp-88h]
  int v136; // [esp+38h] [ebp-88h]
  int v137; // [esp+38h] [ebp-88h]
  _WORD *v138; // [esp+3Ch] [ebp-84h]
  int v139; // [esp+3Ch] [ebp-84h]
  int v140; // [esp+3Ch] [ebp-84h]
  int v141; // [esp+3Ch] [ebp-84h]
  BOOL v142; // [esp+40h] [ebp-80h]
  unsigned int v143; // [esp+40h] [ebp-80h]
  int v144; // [esp+40h] [ebp-80h]
  int v145; // [esp+40h] [ebp-80h]
  int v146; // [esp+44h] [ebp-7Ch]
  int v147; // [esp+48h] [ebp-78h]
  unsigned int v148; // [esp+48h] [ebp-78h]
  BOOL v149; // [esp+48h] [ebp-78h]
  int v150; // [esp+48h] [ebp-78h]
  int v151; // [esp+4Ch] [ebp-74h]
  int v152; // [esp+50h] [ebp-70h]
  int v153; // [esp+54h] [ebp-6Ch]
  __int64 v154; // [esp+58h] [ebp-68h]
  int v155; // [esp+64h] [ebp-5Ch]
  int v156; // [esp+68h] [ebp-58h]
  unsigned int v157; // [esp+68h] [ebp-58h]
  unsigned int v158; // [esp+68h] [ebp-58h]
  int *v159; // [esp+6Ch] [ebp-54h]
  __int64 v160; // [esp+70h] [ebp-50h]
  int v161; // [esp+78h] [ebp-48h]
  int v162; // [esp+7Ch] [ebp-44h]
  __int64 v163; // [esp+80h] [ebp-40h]
  int v164; // [esp+80h] [ebp-40h]
  int v165; // [esp+80h] [ebp-40h]
  int v166; // [esp+80h] [ebp-40h]
  int v167; // [esp+8Ch] [ebp-34h]
  int v168; // [esp+98h] [ebp-28h]
  unsigned int v169; // [esp+98h] [ebp-28h]
  int v170; // [esp+98h] [ebp-28h]
  int *v171; // [esp+A0h] [ebp-20h]
  unsigned int v172; // [esp+A4h] [ebp-1Ch]
  int v173; // [esp+A8h] [ebp-18h]
  int v174; // [esp+ACh] [ebp-14h]
  int v175; // [esp+B0h] [ebp-10h]
  int v176; // [esp+B0h] [ebp-10h]
  int v177; // [esp+B8h] [ebp-8h]
  int v178; // [esp+BCh] [ebp-4h]

  v5 = a3[4];
  v172 = a2 >> 1;
  v6 = *a3;
  v178 = a3[6];
  v7 = a1 + 8;
  v8 = a1 + 16;
  v146 = v5;
  v9 = a1;
  v159 = a1 + 8;
  v171 = a1 + 16;
  if ( a1[1] > a1[9] )
  {
    v9 = a1 + 8;
    v159 = a1;
    v7 = a1;
  }
  if ( v7[1] > a1[17] )
  {
    v171 = v7;
    v159 = a1 + 16;
    v7 = a1 + 16;
    v8 = v171;
  }
  if ( v9[1] > v7[1] )
  {
    v10 = v9;
    v9 = v7;
    v159 = v10;
    v7 = v10;
  }
  result = v8[1];
  v114 = (v9[1] + 0xFFFF) >> 16;
  v131 = (result + 0xFFFF) >> 16;
  if ( v114 != v131 )
  {
    v104 = v7[1];
    v156 = (v104 + 0xFFFF) >> 16;
    v12 = 0x1000000000000LL / (result - v9[1]);
    v110 = *v9;
    v174 = (unsigned __int64)((*v171 - *v9) * v12) >> 32;
    v177 = (unsigned __int64)((v171[6] - v9[6]) * v12) >> 32;
    v173 = (unsigned __int64)((v171[7] - v9[7]) * v12) >> 32;
    v154 = v104 - v9[1];
    result = *v9 + ((v174 * v154) >> 16);
    v155 = result;
    v95 = *v159;
    if ( *v159 != result )
    {
      v160 = (v114 << 16) - v9[1];
      v119 = v9[6];
      v13 = v110 + ((v174 * v160) >> 16);
      v162 = v13;
      v151 = v119 + ((v177 * v160) >> 16);
      v147 = v9[7];
      v152 = v147 + ((v173 * v160) >> 16);
      v163 = 0x1000000000000LL / (v95 - result);
      v167 = (unsigned __int64)((v159[6] - ((v177 * v154) >> 16) - v119) * v163) >> 32;
      v161 = (unsigned __int64)((v159[7] - ((v173 * v154) >> 16) - v147) * v163) >> 32;
      v153 = a4 + 2 * v172 * v114;
      v126 = v156 - v114;
      if ( v156 != v114 )
      {
        v105 = (unsigned __int64)((v95 - v110) * (0x1000000000000LL / v154)) >> 32;
        v14 = v110 + ((v105 * v160) >> 16);
        if ( result >= *v159 )
        {
          if ( result > *v159 && v126 > 0 )
          {
            v125 = v13 + 0xFFFF;
            v97 = v14 + 0xFFFF;
            do
            {
              --v126;
              v176 = v151 + ((v167 * (__int64)(int)((v97 & 0xFFFF0000) - v13)) >> 16);
              v34 = v152 + ((v161 * (__int64)(int)((v97 & 0xFFFF0000) - v13)) >> 16);
              v135 = (_WORD *)(v153 + 2 * ((int)(v97 & 0xFFFF0000) >> 16));
              v35 = (int)((v125 & 0xFFFF0000) - (v97 & 0xFFFF0000)) >> 16;
              if ( v35 > 0 )
              {
                v36 = v34 - 0x8000;
                v37 = v176 - 0x8000;
                v144 = v34 - 0x8000;
                v139 = v176 - 0x8000;
                do
                {
                  v164 = v35 - 1;
                  if ( v37 <= 0 )
                  {
                    v38 = 0;
                    v39 = 0;
                    v149 = 0;
                  }
                  else
                  {
                    v38 = v37 & 0xFFFF0000;
                    v39 = v38 >> 16 < v5 - 1;
                    v149 = v39;
                  }
                  if ( v36 <= 0 )
                  {
                    v40 = 0;
                    v168 = 0;
                  }
                  else
                  {
                    v39 = v149;
                    v168 = (int)(v36 & 0xFFFF0000) >> 16 >= v178 - 1 ? 0 : v5;
                    v40 = v36 & 0xFFFF0000;
                  }
                  v41 = (v38 >> 16) + v5 * (v40 >> 16);
                  v42 = *(_DWORD *)(v6 + 4 * v41);
                  v112 = *(_DWORD *)(v6 + 4 * (v41 + v39));
                  v43 = v168 + v41;
                  v169 = *(_DWORD *)(v6 + 4 * v43);
                  v44 = (((_WORD)v144 - (_WORD)v40) & 0xFFFE) + 1;
                  v150 = (((_WORD)v139 - (_WORD)v38) & 0xFFFE) + 1;
                  v45 = *(_DWORD *)(v6 + 4 * (v39 + v43));
                  v116 = (HIBYTE(v45) * ((unsigned int)(v150 * v44) >> 16)) >> 16;
                  v46 = (HIBYTE(v42) * ((unsigned int)((0x10000 - v44) * (0x10000 - v150)) >> 16)) >> 16;
                  v47 = (HIBYTE(v112) * ((unsigned int)(v150 * (0x10000 - v44)) >> 16)) >> 16;
                  v48 = (HIBYTE(v169) * ((unsigned int)(v44 * (0x10000 - v150)) >> 16)) >> 16;
                  if ( (((v46 + v47 + v48 + v116) << 24)
                      | (((v46 * (v42 & 0xFF00) + v47 * (v112 & 0xFF00) + v48 * (v169 & 0xFF00) + v116 * (v45 & 0xFF00))
                        & 0xFF0000
                        | ((_WORD)v46 * (unsigned __int8)v42
                         + (_WORD)v47 * (unsigned __int8)v112
                         + (_WORD)v48 * (unsigned __int8)v169
                         + (_WORD)v116 * (unsigned __int8)v45)
                        & 0xFF00
                        | (v46 * (v42 & 0xFF0000)
                         + v47 * (v112 & 0xFF0000)
                         + v48 * (v169 & 0xFF0000)
                         + v116 * (v45 & 0xFF0000))
                        & 0xFF0000FF) >> 8)
                      & 0xFF000000) > 0x8000000 )
                  {
                    v49 = ((v46 + v47 + v48 + v116) << 24)
                        | (((v46 * (v42 & 0xFF00) + v47 * (v112 & 0xFF00)
                                                  + v48 * (v169 & 0xFF00)
                                                  + v116 * (v45 & 0xFF00))
                          & 0xFF0000
                          | ((_WORD)v46 * (unsigned __int8)v42
                           + (_WORD)v47 * (unsigned __int8)v112
                           + (_WORD)v48 * (unsigned __int8)v169
                           + (_WORD)v116 * (unsigned __int8)v45)
                          & 0xFF00
                          | (v46 * (v42 & 0xFF0000)
                           + v47 * (v112 & 0xFF0000)
                           + v48 * (v169 & 0xFF0000)
                           + v116 * (v45 & 0xFF0000))
                          & 0xFF0000FF) >> 8);
                    v50 = ((unsigned __int16)*(_DWORD *)a5
                         * (unsigned __int8)((unsigned __int16)((*(_WORD *)(a5 + 12) * (unsigned __int8)v49) & 0xFF00) >> 8))
                        & 0xFF00
                        | (*(_DWORD *)a5
                         * ((((*(_DWORD *)(a5 + 8) * (v49 & 0xFF00)) & 0xFF0000
                            | (*(_WORD *)(a5 + 12) * (unsigned __int8)v49) & 0xFF00
                            | (*(_DWORD *)(a5 + 4) * (v49 & 0xFF0000)) & 0xFF0000FF) >> 8)
                          & 0xFF00))
                        & 0xFF0000
                        | (*(_DWORD *)a5
                         * ((((*(_DWORD *)(a5 + 8) * (v49 & 0xFF00)) & 0xFF0000
                            | (*(_WORD *)(a5 + 12) * (unsigned __int8)v49) & 0xFF00
                            | (*(_DWORD *)(a5 + 4) * (v49 & 0xFF0000)) & 0xFF0000FF) >> 8)
                          & 0xFF0000))
                        & 0xFF0000FF;
                    v51 = ((v50 >> 8) & 0xFF00FF) + ((8 * (*v135 & 0x1F | (32 * (*v135 & 0xF800)))) | 0x70007);
                    v52 = (255 * ((v51 >> 8) & 1)) | (16711680 * (HIBYTE(v51) & 1)) | v51;
                    *v135 = (v52 >> 8) & 0xF800
                          | ((unsigned __int8)v52 >> 3)
                          | (((((v50 >> 8) & 0xFF00) + 32 * (*v135 & 0x7E0 | 0x18)) >> 5)
                           | (2040 * (((((v50 >> 8) & 0xFF00) + 32 * (*v135 & 0x7E0 | 0x18)) >> 16) & 1)))
                          & 0x7E0;
                  }
                  v37 = v167 + v139;
                  v36 = v161 + v144;
                  v35 = v164;
                  ++v135;
                  v5 = v146;
                  v139 += v167;
                  v144 += v161;
                }
                while ( v164 > 0 );
                v13 = v162;
              }
              v125 += v174;
              v151 += v177;
              v152 += v173;
              v13 += v174;
              v97 += v105;
              v153 += 2 * v172;
              v162 = v13;
            }
            while ( v126 > 0 );
          }
        }
        else if ( v126 > 0 )
        {
          v120 = v14 + 0xFFFF;
          v115 = v13 + 0xFFFF;
          do
          {
            --v126;
            v100 = v151 + ((v167 * (__int64)(int)((v115 & 0xFFFF0000) - v13)) >> 16);
            v15 = v152 + ((v161 * (__int64)(int)((v115 & 0xFFFF0000) - v13)) >> 16);
            v138 = (_WORD *)(v153 + 2 * ((int)(v115 & 0xFFFF0000) >> 16));
            v16 = (int)((v120 & 0xFFFF0000) - (v115 & 0xFFFF0000)) >> 16;
            if ( v16 > 0 )
            {
              v17 = v15 - 0x8000;
              v18 = v100 - 0x8000;
              v128 = v15 - 0x8000;
              v134 = v100 - 0x8000;
              do
              {
                v175 = v16 - 1;
                if ( v18 <= 0 )
                {
                  v19 = 0;
                  v20 = 0;
                  v142 = 0;
                }
                else
                {
                  v19 = v18 & 0xFFFF0000;
                  v20 = v19 >> 16 < v5 - 1;
                  v142 = v20;
                }
                if ( v17 <= 0 )
                {
                  v21 = 0;
                  v122 = 0;
                }
                else
                {
                  v20 = v142;
                  v122 = (int)(v17 & 0xFFFF0000) >> 16 >= v178 - 1 ? 0 : v5;
                  v21 = v17 & 0xFFFF0000;
                }
                v22 = (v19 >> 16) + v5 * (v21 >> 16);
                v23 = *(_DWORD *)(v6 + 4 * v22);
                v111 = *(_DWORD *)(v6 + 4 * (v22 + v20));
                v24 = v122 + v22;
                v148 = *(_DWORD *)(v6 + 4 * v24);
                v25 = (((_WORD)v128 - (_WORD)v21) & 0xFFFE) + 1;
                v123 = (((_WORD)v134 - (_WORD)v19) & 0xFFFE) + 1;
                v26 = v25 * (0x10000 - v123);
                v27 = *(_DWORD *)(v6 + 4 * (v20 + v24));
                v28 = HIBYTE(v23) * ((unsigned int)((0x10000 - v25) * (0x10000 - v123)) >> 16);
                v96 = (HIBYTE(v111) * ((unsigned int)(v123 * (0x10000 - v25)) >> 16)) >> 16;
                v124 = (HIBYTE(v27) * ((unsigned int)(v123 * v25) >> 16)) >> 16;
                v29 = HIWORD(v28);
                v143 = (unsigned int)(HIBYTE(v148) * HIWORD(v26)) >> 16;
                if ( (((v29 + v96 + v143 + v124) << 24)
                    | (((v29 * (v23 & 0xFF00) + v96 * (v111 & 0xFF00) + v143 * (v148 & 0xFF00) + v124 * (v27 & 0xFF00))
                      & 0xFF0000
                      | ((_WORD)v29 * (unsigned __int8)v23
                       + (_WORD)v96 * (unsigned __int8)v111
                       + (_WORD)v143 * (unsigned __int8)v148
                       + (_WORD)v124 * (unsigned __int8)v27)
                      & 0xFF00
                      | (v29 * (v23 & 0xFF0000)
                       + v96 * (v111 & 0xFF0000)
                       + v143 * (v148 & 0xFF0000)
                       + v124 * (v27 & 0xFF0000))
                      & 0xFF0000FF) >> 8)
                    & 0xFF000000) > 0x8000000 )
                {
                  v30 = ((v29 + v96 + v143 + v124) << 24)
                      | (((v29 * (v23 & 0xFF00) + v96 * (v111 & 0xFF00) + v143 * (v148 & 0xFF00) + v124 * (v27 & 0xFF00))
                        & 0xFF0000
                        | ((_WORD)v29 * (unsigned __int8)v23
                         + (_WORD)v96 * (unsigned __int8)v111
                         + (_WORD)v143 * (unsigned __int8)v148
                         + (_WORD)v124 * (unsigned __int8)v27)
                        & 0xFF00
                        | (v29 * (v23 & 0xFF0000)
                         + v96 * (v111 & 0xFF0000)
                         + v143 * (v148 & 0xFF0000)
                         + v124 * (v27 & 0xFF0000))
                        & 0xFF0000FF) >> 8);
                  v31 = ((unsigned __int16)*(_DWORD *)a5
                       * (unsigned __int8)((unsigned __int16)((*(_WORD *)(a5 + 12) * (unsigned __int8)v30) & 0xFF00) >> 8))
                      & 0xFF00
                      | (*(_DWORD *)a5
                       * ((((*(_DWORD *)(a5 + 8) * (v30 & 0xFF00)) & 0xFF0000
                          | (*(_WORD *)(a5 + 12) * (unsigned __int8)v30) & 0xFF00
                          | (*(_DWORD *)(a5 + 4) * (v30 & 0xFF0000)) & 0xFF0000FF) >> 8)
                        & 0xFF00))
                      & 0xFF0000
                      | (*(_DWORD *)a5
                       * ((((*(_DWORD *)(a5 + 8) * (v30 & 0xFF00)) & 0xFF0000
                          | (*(_WORD *)(a5 + 12) * (unsigned __int8)v30) & 0xFF00
                          | (*(_DWORD *)(a5 + 4) * (v30 & 0xFF0000)) & 0xFF0000FF) >> 8)
                        & 0xFF0000))
                      & 0xFF0000FF;
                  v32 = ((v31 >> 8) & 0xFF00FF) + ((8 * (*v138 & 0x1F | (32 * (*v138 & 0xF800)))) | 0x70007);
                  v33 = (255 * ((v32 >> 8) & 1)) | (16711680 * (HIBYTE(v32) & 1)) | v32;
                  *v138 = (v33 >> 8) & 0xF800
                        | ((unsigned __int8)v33 >> 3)
                        | (((((v31 >> 8) & 0xFF00) + 32 * (*v138 & 0x7E0 | 0x18)) >> 5)
                         | (2040 * (((((v31 >> 8) & 0xFF00) + 32 * (*v138 & 0x7E0 | 0x18)) >> 16) & 1)))
                        & 0x7E0;
                }
                v18 = v167 + v134;
                v17 = v161 + v128;
                v16 = v175;
                ++v138;
                v5 = v146;
                v134 += v167;
                v128 += v161;
              }
              while ( v175 > 0 );
              v13 = v162;
            }
            v115 += v174;
            v151 += v177;
            v152 += v173;
            v13 += v174;
            v120 += v105;
            v153 += 2 * v172;
            v162 = v13;
          }
          while ( v126 > 0 );
        }
      }
      result = v131 - v156;
      v127 = v131 - v156;
      if ( v131 != v156 )
      {
        v170 = (unsigned __int64)(0x1000000000000LL / (v171[1] - v159[1]) * (*v171 - *v159)) >> 32;
        result = *v159;
        v53 = *v159 + ((v170 * (__int64)((v156 << 16) - v159[1])) >> 16);
        if ( v155 >= *v159 )
        {
          if ( v155 > *v159 && v127 > 0 )
          {
            v141 = v13 + 0xFFFF;
            v145 = v53 + 0xFFFF;
            do
            {
              --v127;
              v133 = v151 + (((int)((v145 & 0xFFFF0000) - v13) * (__int64)v167) >> 16);
              v76 = v152 + (((int)((v145 & 0xFFFF0000) - v13) * (__int64)v161) >> 16);
              v118 = (_WORD *)(v153 + 2 * ((int)(v145 & 0xFFFF0000) >> 16));
              v77 = (int)((v141 & 0xFFFF0000) - (v145 & 0xFFFF0000)) >> 16;
              if ( v77 > 0 )
              {
                v78 = v76 - 0x8000;
                v79 = v133 - 0x8000;
                v137 = v76 - 0x8000;
                v130 = v133 - 0x8000;
                do
                {
                  v166 = v77 - 1;
                  if ( v79 <= 0 )
                  {
                    v80 = 0;
                    v81 = 0;
                    v108 = 0;
                  }
                  else
                  {
                    v80 = v79 & 0xFFFF0000;
                    v81 = v80 >> 16 < v5 - 1;
                    v108 = v81;
                  }
                  if ( v78 <= 0 )
                  {
                    v82 = 0;
                    v102 = 0;
                  }
                  else
                  {
                    v81 = v108;
                    v102 = (int)(v78 & 0xFFFF0000) >> 16 >= v178 - 1 ? 0 : v5;
                    v82 = v78 & 0xFFFF0000;
                  }
                  v83 = (v80 >> 16) + v5 * (v82 >> 16);
                  v84 = *(_DWORD *)(v6 + 4 * v83);
                  v99 = *(_DWORD *)(v6 + 4 * (v83 + v81));
                  v85 = v102 + v83;
                  v103 = *(_DWORD *)(v6 + 4 * v85);
                  v86 = (((_WORD)v137 - (_WORD)v82) & 0xFFFE) + 1;
                  v109 = (((_WORD)v130 - (_WORD)v80) & 0xFFFE) + 1;
                  v87 = *(_DWORD *)(v6 + 4 * (v81 + v85));
                  v158 = (HIBYTE(v87) * ((unsigned int)(v109 * v86) >> 16)) >> 16;
                  v88 = (HIBYTE(v84) * ((unsigned int)((0x10000 - v86) * (0x10000 - v109)) >> 16)) >> 16;
                  v89 = (HIBYTE(v99) * ((unsigned int)(v109 * (0x10000 - v86)) >> 16)) >> 16;
                  v90 = (HIBYTE(v103) * ((unsigned int)(v86 * (0x10000 - v109)) >> 16)) >> 16;
                  if ( (((v88 + v89 + v90 + v158) << 24)
                      | (((v88 * (v84 & 0xFF00) + v89 * (v99 & 0xFF00) + v90 * (v103 & 0xFF00) + v158 * (v87 & 0xFF00))
                        & 0xFF0000
                        | ((_WORD)v88 * (unsigned __int8)v84
                         + (_WORD)v89 * (unsigned __int8)v99
                         + (_WORD)v90 * (unsigned __int8)v103
                         + (_WORD)v158 * (unsigned __int8)v87)
                        & 0xFF00
                        | (v88 * (v84 & 0xFF0000)
                         + v89 * (v99 & 0xFF0000)
                         + v90 * (v103 & 0xFF0000)
                         + v158 * (v87 & 0xFF0000))
                        & 0xFF0000FF) >> 8)
                      & 0xFF000000) > 0x8000000 )
                  {
                    v91 = ((v88 + v89 + v90 + v158) << 24)
                        | (((v88 * (v84 & 0xFF00) + v89 * (v99 & 0xFF00) + v90 * (v103 & 0xFF00) + v158 * (v87 & 0xFF00))
                          & 0xFF0000
                          | ((_WORD)v88 * (unsigned __int8)v84
                           + (_WORD)v89 * (unsigned __int8)v99
                           + (_WORD)v90 * (unsigned __int8)v103
                           + (_WORD)v158 * (unsigned __int8)v87)
                          & 0xFF00
                          | (v88 * (v84 & 0xFF0000)
                           + v89 * (v99 & 0xFF0000)
                           + v90 * (v103 & 0xFF0000)
                           + v158 * (v87 & 0xFF0000))
                          & 0xFF0000FF) >> 8);
                    v92 = ((unsigned __int16)*(_DWORD *)a5
                         * (unsigned __int8)((unsigned __int16)((*(_WORD *)(a5 + 12) * (unsigned __int8)v91) & 0xFF00) >> 8))
                        & 0xFF00
                        | (*(_DWORD *)a5
                         * ((((*(_DWORD *)(a5 + 8) * (v91 & 0xFF00)) & 0xFF0000
                            | (*(_WORD *)(a5 + 12) * (unsigned __int8)v91) & 0xFF00
                            | (*(_DWORD *)(a5 + 4) * (v91 & 0xFF0000)) & 0xFF0000FF) >> 8)
                          & 0xFF00))
                        & 0xFF0000
                        | (*(_DWORD *)a5
                         * ((((*(_DWORD *)(a5 + 8) * (v91 & 0xFF00)) & 0xFF0000
                            | (*(_WORD *)(a5 + 12) * (unsigned __int8)v91) & 0xFF00
                            | (*(_DWORD *)(a5 + 4) * (v91 & 0xFF0000)) & 0xFF0000FF) >> 8)
                          & 0xFF0000))
                        & 0xFF0000FF;
                    v93 = ((v92 >> 8) & 0xFF00FF) + ((8 * (*v118 & 0x1F | (32 * (*v118 & 0xF800)))) | 0x70007);
                    v94 = (255 * ((v93 >> 8) & 1)) | (16711680 * (HIBYTE(v93) & 1)) | v93;
                    *v118 = (v94 >> 8) & 0xF800
                          | ((unsigned __int8)v94 >> 3)
                          | (((((v92 >> 8) & 0xFF00) + 32 * (*v118 & 0x7E0 | 0x18)) >> 5)
                           | (2040 * (((((v92 >> 8) & 0xFF00) + 32 * (*v118 & 0x7E0 | 0x18)) >> 16) & 1)))
                          & 0x7E0;
                  }
                  v79 = v167 + v130;
                  v78 = v161 + v137;
                  v77 = v166;
                  ++v118;
                  v5 = v146;
                  v130 += v167;
                  v137 += v161;
                }
                while ( v166 > 0 );
                v13 = v162;
              }
              v141 += v174;
              v153 += 2 * v172;
              v151 += v177;
              v13 += v174;
              v145 += v170;
              result = v173;
              v152 += v173;
              v162 = v13;
            }
            while ( v127 > 0 );
          }
        }
        else if ( v127 > 0 )
        {
          v140 = v53 + 0xFFFF;
          v136 = v13 + 0xFFFF;
          do
          {
            --v127;
            v132 = v151 + (((int)((v136 & 0xFFFF0000) - v13) * (__int64)v167) >> 16);
            v121 = (_WORD *)(v153 + 2 * ((int)(v136 & 0xFFFF0000) >> 16));
            v54 = (int)((v140 & 0xFFFF0000) - (v136 & 0xFFFF0000)) >> 16;
            if ( v54 > 0 )
            {
              v55 = v152 + (((int)((v136 & 0xFFFF0000) - v13) * (__int64)v161) >> 16) - 0x8000;
              v56 = v132 - 0x8000;
              v129 = v55;
              v117 = v132 - 0x8000;
              do
              {
                v165 = v54 - 1;
                if ( v56 <= 0 )
                {
                  v57 = 0;
                  v58 = 0;
                  v106 = 0;
                }
                else
                {
                  v57 = v56 & 0xFFFF0000;
                  v58 = v57 >> 16 < v5 - 1;
                  v106 = v58;
                }
                v59 = 0;
                if ( v55 <= 0 )
                {
                  v101 = 0;
                }
                else
                {
                  v60 = (int)(v55 & 0xFFFF0000) >> 16;
                  v62 = __OFSUB__(v60, v178 - 1);
                  v61 = v60 - (v178 - 1) < 0;
                  v58 = v106;
                  v101 = v61 == v62 ? 0 : v5;
                  v59 = v55 & 0xFFFF0000;
                }
                v63 = (v57 >> 16) + v5 * (v59 >> 16);
                v64 = *(_DWORD *)(v6 + 4 * v63);
                v113 = *(_DWORD *)(v6 + 4 * (v63 + v58));
                v65 = v101 + v63;
                v66 = *(_DWORD *)(v6 + 4 * v65);
                v107 = *(_DWORD *)(v6 + 4 * (v58 + v65));
                v67 = (((_WORD)v129 - (_WORD)v59) & 0xFFFE) + 1;
                v98 = v66;
                v68 = (((_WORD)v117 - (_WORD)v57) & 0xFFFE) + 1;
                v157 = (HIBYTE(v107) * ((unsigned int)(v68 * v67) >> 16)) >> 16;
                v69 = (HIBYTE(v66) * ((unsigned int)(v67 * (0x10000 - v68)) >> 16)) >> 16;
                v70 = (HIBYTE(v113) * ((unsigned int)(v68 * (0x10000 - v67)) >> 16)) >> 16;
                v71 = (HIBYTE(v64) * ((unsigned int)((0x10000 - v67) * (0x10000 - v68)) >> 16)) >> 16;
                if ( (((v71 + v70 + v69 + v157) << 24)
                    | ((((_WORD)v71 * (unsigned __int8)v64
                       + (_WORD)v70 * (unsigned __int8)v113
                       + (_WORD)v69 * (unsigned __int8)v98
                       + (_WORD)v157 * (unsigned __int8)v107)
                      & 0xFF00
                      | (v71 * (v64 & 0xFF0000)
                       + v70 * (v113 & 0xFF0000)
                       + v69 * (v98 & 0xFF0000)
                       + v157 * (v107 & 0xFF0000))
                      & 0xFF0000FF
                      | (v71 * (v64 & 0xFF00) + v70 * (v113 & 0xFF00) + v69 * (v98 & 0xFF00) + v157 * (v107 & 0xFF00))
                      & 0xFF0000) >> 8)
                    & 0xFF000000) > 0x8000000 )
                {
                  v72 = ((v71 + v70 + v69 + v157) << 24)
                      | ((((_WORD)v71 * (unsigned __int8)v64
                         + (_WORD)v70 * (unsigned __int8)v113
                         + (_WORD)v69 * (unsigned __int8)v98
                         + (_WORD)v157 * (unsigned __int8)v107)
                        & 0xFF00
                        | (v71 * (v64 & 0xFF0000)
                         + v70 * (v113 & 0xFF0000)
                         + v69 * (v98 & 0xFF0000)
                         + v157 * (v107 & 0xFF0000))
                        & 0xFF0000FF
                        | (v71 * (v64 & 0xFF00) + v70 * (v113 & 0xFF00) + v69 * (v98 & 0xFF00) + v157 * (v107 & 0xFF00))
                        & 0xFF0000) >> 8);
                  v73 = ((unsigned __int16)*(_DWORD *)a5
                       * (unsigned __int8)((unsigned __int16)((*(_WORD *)(a5 + 12) * (unsigned __int8)v72) & 0xFF00) >> 8))
                      & 0xFF00
                      | (*(_DWORD *)a5
                       * ((((*(_DWORD *)(a5 + 8) * (v72 & 0xFF00)) & 0xFF0000
                          | (*(_WORD *)(a5 + 12) * (unsigned __int8)v72) & 0xFF00
                          | (*(_DWORD *)(a5 + 4) * (v72 & 0xFF0000)) & 0xFF0000FF) >> 8)
                        & 0xFF00))
                      & 0xFF0000
                      | (*(_DWORD *)a5
                       * ((((*(_DWORD *)(a5 + 8) * (v72 & 0xFF00)) & 0xFF0000
                          | (*(_WORD *)(a5 + 12) * (unsigned __int8)v72) & 0xFF00
                          | (*(_DWORD *)(a5 + 4) * (v72 & 0xFF0000)) & 0xFF0000FF) >> 8)
                        & 0xFF0000))
                      & 0xFF0000FF;
                  v74 = ((v73 >> 8) & 0xFF00FF) + ((8 * (*v121 & 0x1F | (32 * (*v121 & 0xF800)))) | 0x70007);
                  v75 = (255 * ((v74 >> 8) & 1)) | (16711680 * (HIBYTE(v74) & 1)) | v74;
                  *v121 = (v75 >> 8) & 0xF800
                        | ((unsigned __int8)v75 >> 3)
                        | (((((v73 >> 8) & 0xFF00) + 32 * (*v121 & 0x7E0 | 0x18)) >> 5)
                         | (2040 * (((((v73 >> 8) & 0xFF00) + 32 * (*v121 & 0x7E0 | 0x18)) >> 16) & 1)))
                        & 0x7E0;
                }
                v56 = v167 + v117;
                v55 = v161 + v129;
                v54 = v165;
                ++v121;
                v5 = v146;
                v117 += v167;
                v129 += v161;
              }
              while ( v165 > 0 );
              v13 = v162;
            }
            v136 += v174;
            v153 += 2 * v172;
            v151 += v177;
            v13 += v174;
            v140 += v170;
            result = v173;
            v152 += v173;
            v162 = v13;
          }
          while ( v127 > 0 );
        }
      }
    }
  }
  return result;
}

