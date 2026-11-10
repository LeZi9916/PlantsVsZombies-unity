int __cdecl sub_4DFF20(_DWORD *a1, int a2, unsigned int a3, int *a4, _DWORD *a5)
{
  int result; // eax
  int v6; // esi
  _DWORD *v7; // ecx
  _DWORD *v8; // ebx
  _DWORD *v9; // edi
  _DWORD *v10; // eax
  _DWORD *v11; // eax
  int v12; // edx
  __int64 v13; // kr08_8
  int v14; // edi
  int v15; // ecx
  int v16; // eax
  int v17; // ecx
  int v18; // edx
  int v19; // edi
  int v20; // eax
  signed int v21; // edi
  signed int v22; // edx
  int v23; // ecx
  unsigned int v24; // ebx
  int v25; // ecx
  unsigned int v26; // ecx
  int v27; // eax
  unsigned int v28; // esi
  unsigned int v29; // eax
  unsigned int v30; // edx
  unsigned int v31; // esi
  unsigned int v32; // eax
  int v33; // eax
  int v34; // eax
  int v35; // ecx
  int v36; // edx
  int v37; // edi
  signed int v38; // edi
  signed int v39; // edx
  int v40; // ecx
  unsigned int v41; // ebx
  int v42; // ecx
  int v43; // eax
  unsigned int v44; // ecx
  unsigned int v45; // eax
  unsigned int v46; // ebx
  unsigned int v47; // ecx
  unsigned int v48; // eax
  int v49; // ecx
  int v50; // eax
  int v51; // ecx
  int v52; // edi
  int v53; // eax
  int v54; // edx
  signed int v55; // eax
  signed int v56; // edi
  int v57; // ecx
  unsigned int v58; // ebx
  int v59; // ecx
  unsigned int v60; // ecx
  __int16 v61; // dx
  int v62; // eax
  unsigned int v63; // esi
  unsigned int v64; // eax
  unsigned int v65; // edx
  unsigned int v66; // esi
  unsigned int v67; // eax
  int v68; // eax
  int v69; // ecx
  int v70; // edi
  int v71; // eax
  int v72; // edx
  signed int v73; // eax
  signed int v74; // edi
  int v75; // ecx
  unsigned int v76; // ebx
  int v77; // ecx
  unsigned int v78; // ecx
  __int16 v79; // dx
  int v80; // eax
  unsigned int v81; // esi
  unsigned int v82; // eax
  unsigned int v83; // edx
  unsigned int v84; // esi
  unsigned int v85; // eax
  int v86; // [esp+10h] [ebp-110h]
  unsigned int v87; // [esp+10h] [ebp-110h]
  int v88; // [esp+14h] [ebp-10Ch]
  unsigned int *v89; // [esp+14h] [ebp-10Ch]
  unsigned int *v90; // [esp+14h] [ebp-10Ch]
  unsigned int *v91; // [esp+14h] [ebp-10Ch]
  int v92; // [esp+14h] [ebp-10Ch]
  unsigned int v93; // [esp+18h] [ebp-108h]
  int v94; // [esp+18h] [ebp-108h]
  int v95; // [esp+18h] [ebp-108h]
  unsigned int v96; // [esp+18h] [ebp-108h]
  int v97; // [esp+18h] [ebp-108h]
  unsigned int v98; // [esp+18h] [ebp-108h]
  int v99; // [esp+1Ch] [ebp-104h]
  int v100; // [esp+1Ch] [ebp-104h]
  unsigned int v101; // [esp+1Ch] [ebp-104h]
  unsigned int v102; // [esp+1Ch] [ebp-104h]
  unsigned int v103; // [esp+1Ch] [ebp-104h]
  int v104; // [esp+20h] [ebp-100h]
  unsigned int v105; // [esp+20h] [ebp-100h]
  unsigned int v106; // [esp+20h] [ebp-100h]
  unsigned int v107; // [esp+20h] [ebp-100h]
  unsigned int v108; // [esp+20h] [ebp-100h]
  int v109; // [esp+24h] [ebp-FCh]
  int v110; // [esp+24h] [ebp-FCh]
  BOOL v111; // [esp+24h] [ebp-FCh]
  unsigned int v112; // [esp+24h] [ebp-FCh]
  int v113; // [esp+24h] [ebp-FCh]
  int v114; // [esp+24h] [ebp-FCh]
  __int64 v115; // [esp+28h] [ebp-F8h]
  __int64 v116; // [esp+28h] [ebp-F8h]
  __int64 v117; // [esp+28h] [ebp-F8h]
  int v118; // [esp+34h] [ebp-ECh]
  int v119; // [esp+34h] [ebp-ECh]
  int v120; // [esp+34h] [ebp-ECh]
  int v121; // [esp+34h] [ebp-ECh]
  int v122; // [esp+34h] [ebp-ECh]
  int v123; // [esp+34h] [ebp-ECh]
  int *v124; // [esp+38h] [ebp-E8h]
  int v125; // [esp+38h] [ebp-E8h]
  int v126; // [esp+3Ch] [ebp-E4h]
  BOOL v127; // [esp+3Ch] [ebp-E4h]
  unsigned int v128; // [esp+3Ch] [ebp-E4h]
  BOOL v129; // [esp+3Ch] [ebp-E4h]
  unsigned int v130; // [esp+3Ch] [ebp-E4h]
  int v131; // [esp+40h] [ebp-E0h]
  int v132; // [esp+40h] [ebp-E0h]
  int v133; // [esp+40h] [ebp-E0h]
  int v134; // [esp+40h] [ebp-E0h]
  int v135; // [esp+44h] [ebp-DCh]
  int v136; // [esp+44h] [ebp-DCh]
  unsigned int v137; // [esp+44h] [ebp-DCh]
  int v138; // [esp+44h] [ebp-DCh]
  int v139; // [esp+44h] [ebp-DCh]
  int v140; // [esp+48h] [ebp-D8h]
  int v141; // [esp+48h] [ebp-D8h]
  int v142; // [esp+4Ch] [ebp-D4h]
  int v143; // [esp+4Ch] [ebp-D4h]
  int v144; // [esp+4Ch] [ebp-D4h]
  int v145; // [esp+4Ch] [ebp-D4h]
  __int64 v146; // [esp+50h] [ebp-D0h]
  int v147; // [esp+50h] [ebp-D0h]
  int v148; // [esp+50h] [ebp-D0h]
  int v149; // [esp+50h] [ebp-D0h]
  int v150; // [esp+50h] [ebp-D0h]
  int v151; // [esp+50h] [ebp-D0h]
  int v152; // [esp+5Ch] [ebp-C4h]
  int v153; // [esp+5Ch] [ebp-C4h]
  unsigned int v154; // [esp+5Ch] [ebp-C4h]
  int v155; // [esp+5Ch] [ebp-C4h]
  int v156; // [esp+60h] [ebp-C0h]
  int v157; // [esp+60h] [ebp-C0h]
  int v158; // [esp+60h] [ebp-C0h]
  int v159; // [esp+60h] [ebp-C0h]
  int v160; // [esp+64h] [ebp-BCh]
  int v161; // [esp+68h] [ebp-B8h]
  int v162; // [esp+6Ch] [ebp-B4h]
  int v163; // [esp+70h] [ebp-B0h]
  int v164; // [esp+74h] [ebp-ACh]
  int v165; // [esp+78h] [ebp-A8h]
  int v166; // [esp+7Ch] [ebp-A4h]
  int v167; // [esp+80h] [ebp-A0h]
  int v168; // [esp+84h] [ebp-9Ch]
  __int64 v169; // [esp+88h] [ebp-98h]
  BOOL v170; // [esp+90h] [ebp-90h]
  unsigned int v171; // [esp+90h] [ebp-90h]
  int v172; // [esp+90h] [ebp-90h]
  _DWORD *v173; // [esp+94h] [ebp-8Ch]
  int v174; // [esp+94h] [ebp-8Ch]
  int v175; // [esp+94h] [ebp-8Ch]
  unsigned int v176; // [esp+98h] [ebp-88h]
  unsigned int *v177; // [esp+98h] [ebp-88h]
  int v178; // [esp+9Ch] [ebp-84h]
  int v179; // [esp+A0h] [ebp-80h]
  int v180; // [esp+A0h] [ebp-80h]
  int v181; // [esp+A0h] [ebp-80h]
  int v182; // [esp+A4h] [ebp-7Ch]
  int v183; // [esp+A4h] [ebp-7Ch]
  int v184; // [esp+A4h] [ebp-7Ch]
  int v185; // [esp+A4h] [ebp-7Ch]
  int v186; // [esp+B0h] [ebp-70h]
  unsigned int v187; // [esp+B0h] [ebp-70h]
  int v188; // [esp+B0h] [ebp-70h]
  int v189; // [esp+B0h] [ebp-70h]
  int v190; // [esp+B0h] [ebp-70h]
  unsigned int v191; // [esp+B0h] [ebp-70h]
  unsigned int v192; // [esp+B0h] [ebp-70h]
  int v193; // [esp+D8h] [ebp-48h]
  int v194; // [esp+DCh] [ebp-44h]
  int v195; // [esp+E0h] [ebp-40h]
  int v196; // [esp+E4h] [ebp-3Ch]
  int v197; // [esp+E8h] [ebp-38h]
  int v198; // [esp+ECh] [ebp-34h]
  unsigned int v199; // [esp+F0h] [ebp-30h]
  int v200; // [esp+F0h] [ebp-30h]
  unsigned int v201; // [esp+F4h] [ebp-2Ch]
  int v202; // [esp+F4h] [ebp-2Ch]
  int v203; // [esp+F8h] [ebp-28h]
  int v204; // [esp+F8h] [ebp-28h]
  int v205; // [esp+FCh] [ebp-24h]
  int v206; // [esp+100h] [ebp-20h]
  int v207; // [esp+104h] [ebp-1Ch]
  int v208; // [esp+108h] [ebp-18h]
  int v209; // [esp+10Ch] [ebp-14h]
  int v210; // [esp+110h] [ebp-10h]
  int v211; // [esp+114h] [ebp-Ch]
  int v212; // [esp+118h] [ebp-8h]
  int v213; // [esp+11Ch] [ebp-4h]

  if ( byte_755E37 )
    return sub_4E2070(a2, a5);
  v176 = a3 >> 2;
  v6 = *a4;
  v165 = a4[4];
  v213 = a4[6];
  v7 = a1 + 8;
  v8 = a1 + 16;
  v205 = *a4;
  v9 = a1;
  v124 = a1 + 8;
  v173 = a1 + 16;
  if ( a1[1] > a1[9] )
  {
    v9 = a1 + 8;
    v7 = a1;
    v124 = a1;
  }
  if ( v7[1] > a1[17] )
  {
    v10 = v7;
    v7 = a1 + 16;
    v124 = a1 + 16;
    v8 = v10;
    v173 = v10;
  }
  if ( v9[1] > v7[1] )
  {
    v11 = v9;
    v9 = v7;
    v7 = v11;
    v124 = v11;
  }
  v9[2] = (unsigned int)(*a5 * v9[2]) >> 8;
  v9[3] = (unsigned int)(v9[3] * a5[1]) >> 8;
  v9[4] = (unsigned int)(v9[4] * a5[2]) >> 8;
  v9[5] = (unsigned int)(v9[5] * a5[3]) >> 8;
  v7[2] = (unsigned int)(*a5 * v7[2]) >> 8;
  v7[3] = (unsigned int)(a5[1] * v7[3]) >> 8;
  v7[4] = (unsigned int)(v7[4] * a5[2]) >> 8;
  v7[5] = (unsigned int)(v7[5] * a5[3]) >> 8;
  v12 = v8[3];
  v8[2] = (unsigned int)(*a5 * v8[2]) >> 8;
  v8[3] = (unsigned int)(a5[1] * v12) >> 8;
  v8[4] = (unsigned int)(v8[4] * a5[2]) >> 8;
  v93 = (unsigned int)(v8[5] * a5[3]) >> 8;
  v8[5] = v93;
  v104 = v9[1];
  result = v8[1];
  v156 = (v104 + 0xFFFF) >> 16;
  v178 = (result + 0xFFFF) >> 16;
  if ( v156 != v178 )
  {
    v179 = (v124[1] + 0xFFFF) >> 16;
    v115 = 0x1000000000000LL / (result - v104);
    v88 = *v9;
    v211 = (unsigned __int64)((*v8 - *v9) * v115) >> 32;
    v118 = v9[2];
    v212 = (unsigned __int64)((v8[2] - v118) * v115) >> 32;
    v142 = v9[3];
    v208 = (unsigned __int64)((v8[3] - v142) * v115) >> 32;
    v109 = v9[4];
    v210 = (unsigned __int64)((v8[4] - v109) * v115) >> 32;
    v135 = v9[5];
    v207 = (unsigned __int64)((int)(v93 - v135) * v115) >> 32;
    v131 = v9[6];
    v206 = (unsigned __int64)((v8[6] - v131) * v115) >> 32;
    v152 = v9[7];
    v209 = (unsigned __int64)((v8[7] - v152) * v115) >> 32;
    v146 = v124[1] - v104;
    result = *v9 + ((v211 * v146) >> 16);
    v126 = result;
    v99 = *v124;
    if ( *v124 != result )
    {
      v13 = (v156 << 16) - v104;
      HIDWORD(v169) = HIDWORD(v13);
      v14 = v88 + ((v211 * v13) >> 16);
      v160 = v14;
      v163 = v118 + ((v212 * v13) >> 16);
      v166 = v142 + ((v208 * v13) >> 16);
      v164 = v109 + ((v210 * v13) >> 16);
      v162 = v135 + ((v207 * v13) >> 16);
      v167 = v131 + ((v206 * v13) >> 16);
      v168 = v152 + ((v209 * v13) >> 16);
      v116 = 0x1000000000000LL / (v99 - result);
      v198 = (unsigned __int64)((v124[2] - ((v212 * v146) >> 16) - v118) * v116) >> 32;
      v197 = (unsigned __int64)((v124[3] - ((v208 * v146) >> 16) - v142) * v116) >> 32;
      v193 = (unsigned __int64)((v124[4] - ((v210 * v146) >> 16) - v109) * v116) >> 32;
      v195 = (unsigned __int64)((v124[5] - ((v207 * v146) >> 16) - v135) * v116) >> 32;
      v194 = (unsigned __int64)((v124[6] - ((v206 * v146) >> 16) - v131) * v116) >> 32;
      v196 = (unsigned __int64)((v124[7] - ((v209 * v146) >> 16) - v152) * v116) >> 32;
      v161 = a2 + 4 * v176 * v156;
      v140 = v179 - v156;
      if ( v179 != v156 )
      {
        v94 = (unsigned __int64)((v99 - v88) * (0x1000000000000LL / v146)) >> 32;
        LODWORD(v169) = (v156 << 16) - v104;
        v15 = v88 + ((v94 * v169) >> 16);
        if ( result >= *v124 )
        {
          if ( result > *v124 && v140 > 0 )
          {
            v33 = (unsigned __int64)((v124[7] - ((v209 * v146) >> 16) - v152) * v116) >> 32;
            HIDWORD(v117) = v196 >> 31;
            v143 = v14 + 0xFFFF;
            LODWORD(v117) = v33;
            v121 = v15 + 0xFFFF;
            do
            {
              --v140;
              v158 = v163 + ((v198 * (__int64)(int)((v121 & 0xFFFF0000) - v14)) >> 16);
              v183 = v166 + ((v197 * (__int64)(int)((v121 & 0xFFFF0000) - v14)) >> 16);
              v204 = v164 + ((v193 * (__int64)(int)((v121 & 0xFFFF0000) - v14)) >> 16);
              v202 = v162 + ((v195 * (__int64)(int)((v121 & 0xFFFF0000) - v14)) >> 16);
              v188 = v167 + ((v194 * (__int64)(int)((v121 & 0xFFFF0000) - v14)) >> 16);
              v34 = v168 + ((v117 * (int)((v121 & 0xFFFF0000) - v14)) >> 16);
              v90 = (unsigned int *)(v161 + 4 * ((int)(v121 & 0xFFFF0000) >> 16));
              v35 = (int)((v143 & 0xFFFF0000) - (v121 & 0xFFFF0000)) >> 16;
              if ( v35 > 0 )
              {
                v36 = v34 - 0x8000;
                v37 = v188 - 0x8000;
                v172 = v34 - 0x8000;
                v200 = v188 - 0x8000;
                do
                {
                  v147 = v35 - 1;
                  if ( v37 <= 0 )
                  {
                    v38 = 0;
                    v111 = 0;
                  }
                  else
                  {
                    v38 = v37 & 0xFFFF0000;
                    v111 = v38 >> 16 < v165 - 1;
                  }
                  if ( v36 <= 0 )
                  {
                    v39 = 0;
                    v189 = 0;
                  }
                  else
                  {
                    v39 = v36 & 0xFFFF0000;
                    v189 = v39 >> 16 >= v213 - 1 ? 0 : v165;
                  }
                  v40 = (v38 >> 16) + v165 * (v39 >> 16);
                  v41 = *(_DWORD *)(v6 + 4 * v40);
                  v101 = *(_DWORD *)(v6 + 4 * (v40 + v111));
                  v42 = v189 + v40;
                  v87 = *(_DWORD *)(v6 + 4 * v42);
                  v112 = *(_DWORD *)(v6 + 4 * (v111 + v42));
                  v43 = (((_WORD)v172 - (_WORD)v39) & 0xFFFE) + 1;
                  v190 = (((_WORD)v200 - (_WORD)v38) & 0xFFFE) + 1;
                  v137 = (HIBYTE(v101) * ((unsigned int)(v190 * (0x10000 - v43)) >> 16)) >> 16;
                  v106 = (HIBYTE(v87) * ((unsigned int)(v43 * (0x10000 - v190)) >> 16)) >> 16;
                  v44 = (HIBYTE(v41) * ((unsigned int)((0x10000 - v43) * (0x10000 - v190)) >> 16)) >> 16;
                  v154 = (HIBYTE(v112) * ((unsigned int)(v190 * v43) >> 16)) >> 16;
                  v45 = ((v44 + v137 + v106 + v154) << 24)
                      | ((((_WORD)v44 * (unsigned __int8)v41
                         + (_WORD)v137 * (unsigned __int8)v101
                         + (_WORD)v106 * (unsigned __int8)v87
                         + (_WORD)v154 * (unsigned __int8)v112)
                        & 0xFF00
                        | (v44 * (v41 & 0xFF0000)
                         + v137 * (v101 & 0xFF0000)
                         + v106 * (v87 & 0xFF0000)
                         + v154 * (v112 & 0xFF0000))
                        & 0xFF0000FF
                        | (v44 * (v41 & 0xFF00) + v137 * (v101 & 0xFF00)
                                                + v106 * (v87 & 0xFF00)
                                                + v154 * (v112 & 0xFF00))
                        & 0xFF0000) >> 8);
                  if ( HIBYTE(v45) > 8u )
                  {
                    v46 = (HIBYTE(v45) * ((unsigned int)(v158 * *a5) >> 24)) >> 8;
                    v47 = (((v45 & 0xFF00) * ((unsigned int)(v204 * a5[2]) >> 24)) & 0xFF0000
                         | ((unsigned __int8)v45 * ((unsigned __int16)((unsigned int)(v202 * a5[3]) >> 16) >> 8))
                         & 0xFF00
                         | ((v45 & 0xFF0000) * ((unsigned int)(v183 * a5[1]) >> 24)) & 0xFF0000FF) >> 8;
                    v48 = ((((unsigned __int16)((unsigned int)(v158 * *a5) >> 16) >> 8) * (unsigned __int8)v47) & 0xFF00
                         | (((unsigned int)(v158 * *a5) >> 24) * (v47 & 0xFF00)) & 0xFF0000
                         | (((unsigned int)(v158 * *a5) >> 24) * (v47 & 0xFF0000)) & 0xFF0000FF) >> 8;
                    if ( v46 >= 0xF0 )
                      *v90 = v48 | 0xFF000000;
                    else
                      *v90 = ((v48 & 0xFFFFFF)
                            + ((((255 - v46) * (*v90 & 0xFF00FF))
                              ^ (((255 - v46) * (*v90 & 0xFF00))
                               ^ ((255 - v46) * (*v90 & 0xFFFF00FF)))
                              & 0xFF0000) >> 8))
                           | 0xFF000000;
                  }
                  v204 += v193;
                  v158 += v198;
                  v183 += v197;
                  v37 = v194 + v200;
                  v36 = v196 + v172;
                  v35 = v147;
                  ++v90;
                  v202 += v195;
                  v200 += v194;
                  v172 += v196;
                }
                while ( v147 > 0 );
                v14 = v160;
              }
              v143 += v211;
              v121 += v94;
              v163 += v212;
              v164 += v210;
              v162 += v207;
              v168 += v209;
              v14 += v211;
              v161 += 4 * v176;
              v166 += v208;
              v167 += v206;
              v160 = v14;
            }
            while ( v140 > 0 );
          }
        }
        else if ( v140 > 0 )
        {
          v110 = v15 + 0xFFFF;
          v136 = v14 + 0xFFFF;
          do
          {
            --v140;
            v203 = v163 + ((v198 * (__int64)(int)((v136 & 0xFFFF0000) - v14)) >> 16);
            v182 = v166 + ((v197 * (__int64)(int)((v136 & 0xFFFF0000) - v14)) >> 16);
            v157 = v164 + ((v193 * (__int64)(int)((v136 & 0xFFFF0000) - v14)) >> 16);
            v153 = v162 + ((v195 * (__int64)(int)((v136 & 0xFFFF0000) - v14)) >> 16);
            v186 = v167 + ((v194 * (__int64)(int)((v136 & 0xFFFF0000) - v14)) >> 16);
            v16 = v168 + ((v196 * (__int64)(int)((v136 & 0xFFFF0000) - v14)) >> 16);
            v89 = (unsigned int *)(v161 + 4 * ((int)(v136 & 0xFFFF0000) >> 16));
            v17 = (int)((v110 & 0xFFFF0000) - (v136 & 0xFFFF0000)) >> 16;
            if ( v17 > 0 )
            {
              v18 = v16 - 0x8000;
              v19 = v186 - 0x8000;
              v100 = v16 - 0x8000;
              v132 = v186 - 0x8000;
              do
              {
                v20 = v165;
                v86 = v17 - 1;
                if ( v19 <= 0 )
                {
                  v21 = 0;
                  v170 = 0;
                }
                else
                {
                  v21 = v19 & 0xFFFF0000;
                  v18 = v100;
                  v170 = v21 >> 16 < v165 - 1;
                }
                if ( v18 <= 0 )
                {
                  v22 = 0;
                  v119 = 0;
                }
                else
                {
                  v22 = v18 & 0xFFFF0000;
                  v20 = v165;
                  v119 = v22 >> 16 >= v213 - 1 ? 0 : v165;
                }
                v23 = (v21 >> 16) + v20 * (v22 >> 16);
                v24 = *(_DWORD *)(v6 + 4 * v23);
                v105 = *(_DWORD *)(v6 + 4 * (v23 + v170));
                v25 = v119 + v23;
                v187 = *(_DWORD *)(v6 + 4 * v25);
                v26 = *(_DWORD *)(v6 + 4 * (v170 + v25));
                v27 = (((_WORD)v100 - (_WORD)v22) & 0xFFFE) + 1;
                v120 = (((_WORD)v132 - (_WORD)v21) & 0xFFFE) + 1;
                v171 = (HIBYTE(v105) * ((unsigned int)(v120 * (0x10000 - v27)) >> 16)) >> 16;
                v199 = (HIBYTE(v26) * ((unsigned int)(v120 * v27) >> 16)) >> 16;
                v28 = (HIBYTE(v24) * ((unsigned int)((0x10000 - v27) * (0x10000 - v120)) >> 16)) >> 16;
                v201 = (HIBYTE(v187) * ((unsigned int)(v27 * (0x10000 - v120)) >> 16)) >> 16;
                v29 = ((v28 + v171 + v201 + v199) << 24)
                    | (((v28 * (v24 & 0xFF00) + v171 * (v105 & 0xFF00) + v201 * (v187 & 0xFF00) + v199 * (v26 & 0xFF00))
                      & 0xFF0000
                      | ((_WORD)v28 * (unsigned __int8)v24
                       + (_WORD)v171 * (unsigned __int8)v105
                       + (_WORD)v201 * (unsigned __int8)v187
                       + (_WORD)v199 * (unsigned __int8)v26)
                      & 0xFF00
                      | (v28 * (v24 & 0xFF0000)
                       + v171 * (v105 & 0xFF0000)
                       + v201 * (v187 & 0xFF0000)
                       + v199 * (v26 & 0xFF0000))
                      & 0xFF0000FF) >> 8);
                if ( HIBYTE(v29) > 8u )
                {
                  v30 = (unsigned int)(v203 * *a5) >> 24;
                  v31 = (HIBYTE(v29) * v30) >> 8;
                  v32 = (((_WORD)v30
                        * (unsigned __int8)((unsigned __int16)(((unsigned __int8)v29
                                                              * ((unsigned __int16)((unsigned int)(v153 * a5[3]) >> 16) >> 8))
                                                             & 0xFF00) >> 8))
                       & 0xFF00
                       | (v30
                        * (((((v29 & 0xFF00) * ((unsigned int)(v157 * a5[2]) >> 24)) & 0xFF0000
                           | ((unsigned __int8)v29 * ((unsigned __int16)((unsigned int)(v153 * a5[3]) >> 16) >> 8))
                           & 0xFF00
                           | ((v29 & 0xFF0000) * ((unsigned int)(v182 * a5[1]) >> 24)) & 0xFF0000FF) >> 8)
                         & 0xFF00))
                       & 0xFF0000
                       | (v30
                        * (((((v29 & 0xFF00) * ((unsigned int)(v157 * a5[2]) >> 24)) & 0xFF0000
                           | ((unsigned __int8)v29 * ((unsigned __int16)((unsigned int)(v153 * a5[3]) >> 16) >> 8))
                           & 0xFF00
                           | ((v29 & 0xFF0000) * ((unsigned int)(v182 * a5[1]) >> 24)) & 0xFF0000FF) >> 8)
                         & 0xFF0000))
                       & 0xFF0000FF) >> 8;
                  if ( v31 >= 0xF0 )
                    *v89 = v32 | 0xFF000000;
                  else
                    *v89 = ((v32 & 0xFFFFFF)
                          + ((((255 - v31) * (*v89 & 0xFF00FF))
                            ^ (((255 - v31) * (*v89 & 0xFF00))
                             ^ ((255 - v31) * (*v89 & 0xFFFF00FF)))
                            & 0xFF0000) >> 8))
                         | 0xFF000000;
                }
                v203 += v198;
                v153 += v195;
                v157 += v193;
                v19 = v194 + v132;
                v18 = v196 + v100;
                v17 = v86;
                ++v89;
                v182 += v197;
                v6 = v205;
                v132 += v194;
                v100 += v196;
              }
              while ( v86 > 0 );
              v14 = v160;
            }
            v136 += v211;
            v163 += v212;
            v166 += v208;
            v162 += v207;
            v167 += v206;
            v14 += v211;
            v110 += v94;
            v161 += 4 * v176;
            v164 += v210;
            v168 += v209;
            v160 = v14;
          }
          while ( v140 > 0 );
        }
      }
      result = v178 - v179;
      v141 = v178 - v179;
      if ( v178 != v179 )
      {
        v159 = (unsigned __int64)(0x1000000000000LL / (v173[1] - v124[1]) * (*v173 - *v124)) >> 32;
        result = *v124;
        v49 = *v124 + ((v159 * (__int64)((v179 << 16) - v124[1])) >> 16);
        if ( v126 >= *v124 )
        {
          if ( v126 > *v124 && v141 > 0 )
          {
            v155 = 4 * v176;
            v139 = v14 + 0xFFFF;
            v134 = v49 + 0xFFFF;
            do
            {
              --v141;
              v181 = v163 + ((v198 * (__int64)(int)((v134 & 0xFFFF0000) - v14)) >> 16);
              v175 = v166 + ((v197 * (__int64)(int)((v134 & 0xFFFF0000) - v14)) >> 16);
              v123 = v164 + ((v193 * (__int64)(int)((v134 & 0xFFFF0000) - v14)) >> 16);
              v145 = v162 + ((v195 * (__int64)(int)((v134 & 0xFFFF0000) - v14)) >> 16);
              v150 = v167 + ((v194 * (__int64)(int)((v134 & 0xFFFF0000) - v14)) >> 16);
              v68 = v168 + ((v196 * (__int64)(int)((v134 & 0xFFFF0000) - v14)) >> 16);
              v177 = (unsigned int *)(v161 + 4 * ((int)(v134 & 0xFFFF0000) >> 16));
              v69 = (int)((v139 & 0xFFFF0000) - (v134 & 0xFFFF0000)) >> 16;
              if ( v69 > 0 )
              {
                v92 = v68 - 0x8000;
                v70 = v68 - 0x8000;
                v71 = v150 - 0x8000;
                v114 = v150 - 0x8000;
                do
                {
                  v72 = v165;
                  v185 = v69 - 1;
                  if ( v71 <= 0 )
                  {
                    v73 = 0;
                    v129 = 0;
                  }
                  else
                  {
                    v73 = v71 & 0xFFFF0000;
                    v70 = v92;
                    v129 = v73 >> 16 < v165 - 1;
                  }
                  if ( v70 <= 0 )
                  {
                    v74 = 0;
                    v97 = 0;
                  }
                  else
                  {
                    v74 = v70 & 0xFFFF0000;
                    v72 = v165;
                    v97 = v74 >> 16 >= v213 - 1 ? 0 : v165;
                  }
                  v75 = (v73 >> 16) + v72 * (v74 >> 16);
                  v76 = *(_DWORD *)(v6 + 4 * v75);
                  v108 = *(_DWORD *)(v6 + 4 * (v75 + v129));
                  v77 = v97 + v75;
                  v103 = *(_DWORD *)(v6 + 4 * v77);
                  v78 = *(_DWORD *)(v6 + 4 * (v77 + v129));
                  v79 = v114 - v73;
                  v80 = (((_WORD)v92 - (_WORD)v74) & 0xFFFE) + 1;
                  v151 = (v79 & 0xFFFE) + 1;
                  v130 = (HIBYTE(v78) * ((unsigned int)(v151 * v80) >> 16)) >> 16;
                  v98 = (HIBYTE(v108) * ((unsigned int)((0x10000 - v80) * v151) >> 16)) >> 16;
                  v81 = (HIBYTE(v76) * ((unsigned int)((0x10000 - v80) * (0x10000 - v151)) >> 16)) >> 16;
                  v192 = (HIBYTE(v103) * ((unsigned int)((0x10000 - v151) * v80) >> 16)) >> 16;
                  v82 = ((v81 + v98 + v192 + v130) << 24)
                      | (((v81 * (v76 & 0xFF00) + v98 * (v108 & 0xFF00) + v192 * (v103 & 0xFF00) + v130 * (v78 & 0xFF00))
                        & 0xFF0000
                        | ((_WORD)v81 * (unsigned __int8)v76
                         + (_WORD)v98 * (unsigned __int8)v108
                         + (_WORD)v192 * (unsigned __int8)v103
                         + (_WORD)v130 * (unsigned __int8)v78)
                        & 0xFF00
                        | (v81 * (v76 & 0xFF0000)
                         + v98 * (v108 & 0xFF0000)
                         + v192 * (v103 & 0xFF0000)
                         + v130 * (v78 & 0xFF0000))
                        & 0xFF0000FF) >> 8);
                  if ( HIBYTE(v82) > 8u )
                  {
                    v83 = (unsigned int)(v181 * *a5) >> 24;
                    v84 = (HIBYTE(v82) * v83) >> 8;
                    v85 = (((_WORD)v83
                          * (unsigned __int8)((unsigned __int16)(((unsigned __int8)v82
                                                                * ((unsigned __int16)((unsigned int)(v145 * a5[3]) >> 16) >> 8))
                                                               & 0xFF00) >> 8))
                         & 0xFF00
                         | (v83
                          * (((((v82 & 0xFF00) * ((unsigned int)(v123 * a5[2]) >> 24)) & 0xFF0000
                             | ((unsigned __int8)v82 * ((unsigned __int16)((unsigned int)(v145 * a5[3]) >> 16) >> 8))
                             & 0xFF00
                             | ((v82 & 0xFF0000) * ((unsigned int)(v175 * a5[1]) >> 24)) & 0xFF0000FF) >> 8)
                           & 0xFF00))
                         & 0xFF0000
                         | (v83
                          * (((((v82 & 0xFF00) * ((unsigned int)(v123 * a5[2]) >> 24)) & 0xFF0000
                             | ((unsigned __int8)v82 * ((unsigned __int16)((unsigned int)(v145 * a5[3]) >> 16) >> 8))
                             & 0xFF00
                             | ((v82 & 0xFF0000) * ((unsigned int)(v175 * a5[1]) >> 24)) & 0xFF0000FF) >> 8)
                           & 0xFF0000))
                         & 0xFF0000FF) >> 8;
                    if ( v84 >= 0xF0 )
                      *v177 = v85 | 0xFF000000;
                    else
                      *v177 = ((v85 & 0xFFFFFF)
                             + ((((255 - v84) * (*v177 & 0xFF00FF))
                               ^ (((255 - v84) * (*v177 & 0xFF00))
                                ^ ((255 - v84) * (*v177 & 0xFFFF00FF)))
                               & 0xFF0000) >> 8))
                            | 0xFF000000;
                  }
                  v175 += v197;
                  v181 += v198;
                  v123 += v193;
                  v71 = v194 + v114;
                  v70 = v196 + v92;
                  v69 = v185;
                  ++v177;
                  v145 += v195;
                  v6 = v205;
                  v114 += v194;
                  v92 += v196;
                }
                while ( v185 > 0 );
                v14 = v160;
              }
              v139 += v211;
              v161 += v155;
              v163 += v212;
              v164 += v210;
              v162 += v207;
              v168 += v209;
              v14 += v211;
              v134 += v159;
              v166 += v208;
              result = v206;
              v167 += v206;
              v160 = v14;
            }
            while ( v141 > 0 );
          }
        }
        else if ( v141 > 0 )
        {
          v133 = v49 + 0xFFFF;
          v138 = v14 + 0xFFFF;
          do
          {
            --v141;
            v180 = v163 + ((v198 * (__int64)(int)((v138 & 0xFFFF0000) - v14)) >> 16);
            v174 = v166 + ((v197 * (__int64)(int)((v138 & 0xFFFF0000) - v14)) >> 16);
            v122 = v164 + ((v193 * (__int64)(int)((v138 & 0xFFFF0000) - v14)) >> 16);
            v144 = v162 + ((v195 * (__int64)(int)((v138 & 0xFFFF0000) - v14)) >> 16);
            v148 = v167 + ((v194 * (__int64)(int)((v138 & 0xFFFF0000) - v14)) >> 16);
            v50 = v168 + ((v196 * (__int64)(int)((v138 & 0xFFFF0000) - v14)) >> 16);
            v91 = (unsigned int *)(v161 + 4 * ((int)(v138 & 0xFFFF0000) >> 16));
            v51 = (int)((v133 & 0xFFFF0000) - (v138 & 0xFFFF0000)) >> 16;
            if ( v51 > 0 )
            {
              v125 = v50 - 0x8000;
              v52 = v50 - 0x8000;
              v53 = v148 - 0x8000;
              v113 = v148 - 0x8000;
              do
              {
                v54 = v165;
                v184 = v51 - 1;
                if ( v53 <= 0 )
                {
                  v55 = 0;
                  v127 = 0;
                }
                else
                {
                  v55 = v53 & 0xFFFF0000;
                  v52 = v125;
                  v127 = v55 >> 16 < v165 - 1;
                }
                if ( v52 <= 0 )
                {
                  v56 = 0;
                  v95 = 0;
                }
                else
                {
                  v56 = v52 & 0xFFFF0000;
                  v54 = v165;
                  v95 = v56 >> 16 >= v213 - 1 ? 0 : v165;
                }
                v57 = (v55 >> 16) + v54 * (v56 >> 16);
                v58 = *(_DWORD *)(v6 + 4 * v57);
                v107 = *(_DWORD *)(v6 + 4 * (v57 + v127));
                v59 = v95 + v57;
                v102 = *(_DWORD *)(v6 + 4 * v59);
                v60 = *(_DWORD *)(v6 + 4 * (v59 + v127));
                v61 = v113 - v55;
                v62 = (((_WORD)v125 - (_WORD)v56) & 0xFFFE) + 1;
                v149 = (v61 & 0xFFFE) + 1;
                v128 = (HIBYTE(v60) * ((unsigned int)(v149 * v62) >> 16)) >> 16;
                v96 = (HIBYTE(v107) * ((unsigned int)((0x10000 - v62) * v149) >> 16)) >> 16;
                v63 = (HIBYTE(v58) * ((unsigned int)((0x10000 - v62) * (0x10000 - v149)) >> 16)) >> 16;
                v191 = (HIBYTE(v102) * ((unsigned int)((0x10000 - v149) * v62) >> 16)) >> 16;
                v64 = ((v63 + v96 + v191 + v128) << 24)
                    | (((v63 * (v58 & 0xFF00) + v96 * (v107 & 0xFF00) + v191 * (v102 & 0xFF00) + v128 * (v60 & 0xFF00))
                      & 0xFF0000
                      | ((_WORD)v63 * (unsigned __int8)v58
                       + (_WORD)v96 * (unsigned __int8)v107
                       + (_WORD)v191 * (unsigned __int8)v102
                       + (_WORD)v128 * (unsigned __int8)v60)
                      & 0xFF00
                      | (v63 * (v58 & 0xFF0000)
                       + v96 * (v107 & 0xFF0000)
                       + v191 * (v102 & 0xFF0000)
                       + v128 * (v60 & 0xFF0000))
                      & 0xFF0000FF) >> 8);
                if ( HIBYTE(v64) > 8u )
                {
                  v65 = (unsigned int)(v180 * *a5) >> 24;
                  v66 = (HIBYTE(v64) * v65) >> 8;
                  v67 = (((_WORD)v65
                        * (unsigned __int8)((unsigned __int16)(((unsigned __int8)v64
                                                              * ((unsigned __int16)((unsigned int)(v144 * a5[3]) >> 16) >> 8))
                                                             & 0xFF00) >> 8))
                       & 0xFF00
                       | (v65
                        * (((((v64 & 0xFF00) * ((unsigned int)(v122 * a5[2]) >> 24)) & 0xFF0000
                           | ((unsigned __int8)v64 * ((unsigned __int16)((unsigned int)(v144 * a5[3]) >> 16) >> 8))
                           & 0xFF00
                           | ((v64 & 0xFF0000) * ((unsigned int)(v174 * a5[1]) >> 24)) & 0xFF0000FF) >> 8)
                         & 0xFF00))
                       & 0xFF0000
                       | (v65
                        * (((((v64 & 0xFF00) * ((unsigned int)(v122 * a5[2]) >> 24)) & 0xFF0000
                           | ((unsigned __int8)v64 * ((unsigned __int16)((unsigned int)(v144 * a5[3]) >> 16) >> 8))
                           & 0xFF00
                           | ((v64 & 0xFF0000) * ((unsigned int)(v174 * a5[1]) >> 24)) & 0xFF0000FF) >> 8)
                         & 0xFF0000))
                       & 0xFF0000FF) >> 8;
                  if ( v66 >= 0xF0 )
                    *v91 = v67 | 0xFF000000;
                  else
                    *v91 = ((v67 & 0xFFFFFF)
                          + ((((255 - v66) * (*v91 & 0xFF00FF))
                            ^ (((255 - v66) * (*v91 & 0xFF00))
                             ^ ((255 - v66) * (*v91 & 0xFFFF00FF)))
                            & 0xFF0000) >> 8))
                         | 0xFF000000;
                }
                v174 += v197;
                v180 += v198;
                v122 += v193;
                v53 = v194 + v113;
                v52 = v196 + v125;
                v51 = v184;
                ++v91;
                v144 += v195;
                v6 = v205;
                v113 += v194;
                v125 += v196;
              }
              while ( v184 > 0 );
              v14 = v160;
            }
            v138 += v211;
            v161 += 4 * v176;
            v163 += v212;
            v164 += v210;
            v162 += v207;
            v168 += v209;
            v14 += v211;
            v133 += v159;
            v166 += v208;
            result = v206;
            v167 += v206;
            v160 = v14;
          }
          while ( v141 > 0 );
        }
      }
    }
  }
  return result;
}

