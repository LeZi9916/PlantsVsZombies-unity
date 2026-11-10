int __userpurge sub_46BE80@<eax>(int a1@<eax>, _DWORD *a2, int a3, int a4, int a5)
{
  int v5; // ecx
  _DWORD *v6; // ebx
  int v7; // edx
  int v8; // edi
  int v9; // edi
  int v10; // eax
  int v11; // eax
  int v12; // esi
  int v13; // ebx
  int v14; // ecx
  int v15; // eax
  int v16; // eax
  double v17; // st7
  int v18; // ecx
  int v19; // eax
  int v20; // eax
  int v21; // eax
  int v22; // eax
  int v23; // ecx
  int result; // eax
  bool v25; // zf
  int v26; // eax
  int v27; // ebx
  int v28; // esi
  int v29; // ebx
  int v30; // esi
  int v31; // esi
  int v32; // edi
  int *v33; // ecx
  int v34; // edx
  float v35; // [esp+4h] [ebp-30h]
  float v36; // [esp+8h] [ebp-2Ch]
  int v37; // [esp+8h] [ebp-2Ch]
  int v38; // [esp+8h] [ebp-2Ch]
  int v39; // [esp+8h] [ebp-2Ch]
  int v40; // [esp+20h] [ebp-14h]
  float v41; // [esp+20h] [ebp-14h]

  v5 = a3;
  v6 = a2;
  v7 = a2[1];
  v8 = 0;
  a2[10] = a3;
  a2[7] = a1;
  if ( v7 )
  {
    if ( *(_DWORD *)(*(_DWORD *)(v7 + 164) + 2332) == 43
      && ((v9 = *(_DWORD *)(v7 + 21860), v9 == 6) || v9 == 8 || v9 == 7) )
    {
      v10 = sub_537170();
      v5 = a3;
    }
    else
    {
      v10 = 80 * a3 + 40;
    }
    a2[2] = v10;
    a2[3] = sub_428350(v5);
    v6 = a2;
    v8 = 0;
  }
  v6[11] = 0;
  *((_BYTE *)v6 + 320) = 1;
  v6[12] = 0;
  v6[36] = 0;
  v11 = sub_5EECC0(7);
  *((float *)v6 + 48) = 0.0;
  *((float *)v6 + 49) = 0.0;
  v6[13] = v11 + 12;
  v6[32] = -1;
  v6[33] = -1;
  v6[34] = v6[7];
  v6[14] = 5;
  v6[15] = 0;
  *((_BYTE *)v6 + 321) = 0;
  *((_BYTE *)v6 + 322) = 0;
  v6[9] = a4;
  v6[78] = a5;
  v6[16] = 300;
  v6[20] = 0;
  v6[19] = 200;
  v6[21] = 0;
  v6[35] = 0;
  v6[37] = 0;
  v6[38] = 0;
  v6[39] = 0;
  v6[40] = 0;
  v6[41] = 0;
  v6[42] = 0;
  v6[43] = 0;
  v6[44] = 0;
  v6[45] = 0;
  v6[46] = 0;
  v6[47] = 0;
  v6[4] = 80;
  v6[5] = 80;
  memset(v6 + 50, 0, 0x64u);
  *((_BYTE *)v6 + 323) = 0;
  v6[76] = 0;
  v6[77] = 0;
  v6[79] = -1;
  v6[23] = *((_DWORD *)&unk_74AA1C + 9 * a4);
  v6[18] = *((_DWORD *)&unk_74AA18 + 9 * a4);
  v12 = 0;
  v6[8] = sub_46CAF0();
  v40 = 0;
  if ( dword_74AA08[9 * a4] != -1 )
  {
    v41 = sub_473490(v6[10], v6[7]);
    v13 = sub_461000(0.0, v41, v6[8] + 1);
    v40 = v13;
    *(_DWORD *)(v13 + 16) = 0;
    sub_5EECE0(5.0);
    *(float *)(v13 + 8) = 5.0 + 10.0;
    if ( (unsigned __int8)sub_482780("anim_idle") )
      sub_482740("anim_idle");
    v14 = *a2;
    if ( !*(_DWORD *)(*a2 + 2152) )
      goto LABEL_24;
    v15 = *(_DWORD *)(v14 + 2332);
    if ( v15 != 17 && v15 != 33 && (v15 || *(_DWORD *)(*(_DWORD *)(v14 + 2384) + 76) != 5) )
      goto LABEL_24;
    if ( !(unsigned __int8)sub_482780("_ground") )
      goto LABEL_24;
    sub_482740("_ground");
    v16 = a2[9];
    if ( v16 == 3 || v16 == 49 )
    {
      v36 = 18.0;
      v17 = 12.0;
    }
    else
    {
      if ( v16 != 50 )
      {
LABEL_24:
        v18 = *(_DWORD *)(v13 + 156);
        *(_BYTE *)(v13 + 100) = 1;
        a2[37] = v18;
        a2[44] = sub_5EECC0(400) + 400;
        v6 = a2;
        v12 = v40;
        v8 = 0;
        goto LABEL_25;
      }
      v36 = 10.0;
      v17 = 6.0;
    }
    v35 = v17;
    sub_526F90(v35, v36);
    *(float *)(v13 + 8) = v17;
    goto LABEL_24;
  }
LABEL_25:
  v19 = v6[9];
  if ( v19 == 8
    || v19 == 24
    || v19 == 9
    || v19 == 10
    || v19 == 12
    || v19 == 15
    || v19 == 14
    || v19 == 31
    || v19 == 13
    || v19 == 42 )
  {
    v20 = v6[1];
    if ( v20 )
    {
      v21 = *(_DWORD *)(v20 + 21860);
      if ( v21 != 1 && v21 != 3 && v21 != 5 && v21 != 6 && v21 != 8 )
      {
        sub_46CB90(COERCE_FLOAT(1));
        v8 = 0;
      }
    }
  }
  v22 = v6[23];
  if ( v22 <= 0 )
  {
    v6[22] = 0;
  }
  else
  {
    v23 = v6[9];
    if ( v23 == 1 || v23 == 41 || v23 == 9 )
      v6[22] = sub_5EECC0(v22 / 2 - 299) + 300;
    else
      v6[22] = sub_5EECC0(v22 + 1);
  }
  result = a4;
  switch ( a4 )
  {
    case 27:
      v25 = *((_BYTE *)v6 + 324) == 0;
      v6[20] = 50;
      if ( v25 || (v26 = *(_DWORD *)(*v6 + 2332), v26 == 43) || v26 == 50 )
      {
        result = sub_482740("anim_idle");
        *(float *)(v12 + 8) = 10.0;
      }
      else
      {
        result = sub_482740("anim_blow");
        *(float *)(v12 + 8) = 20.0;
        *(_DWORD *)(v12 + 16) = 3;
      }
      break;
    case 0:
    case 5:
    case 7:
    case 52:
    case 40:
      if ( !v12 )
        break;
      sub_5EECE0(5.0);
      *(float *)(v12 + 8) = 5.0 + 15.0;
      v32 = sub_461000(0.0, 0.0, v6[8] + 2);
      *(_DWORD *)(v32 + 16) = 0;
      *(float *)(v32 + 8) = *(float *)(v12 + 8);
      sub_482740("anim_head_idle");
      a2[38] = *(_DWORD *)(v32 + 156);
      if ( (unsigned __int8)sub_482780("anim_stem") || (result = sub_482780("anim_idle"), (_BYTE)result) )
        result = sub_482430();
LABEL_150:
      v6 = a2;
      break;
    case 28:
      sub_526F90(15.0, 20.0);
      *(float *)(v12 + 8) = 15.0;
      v27 = sub_461000(0.0, 0.0, v6[8] + 2);
      *(_DWORD *)(v27 + 16) = 0;
      *(float *)(v27 + 8) = *(float *)(v12 + 8);
      sub_482740("anim_head_idle");
      sub_482430();
      v37 = a2[8] + 2;
      a2[38] = *(_DWORD *)(v27 + 156);
      v28 = sub_461000(0.0, 0.0, v37);
      *(_DWORD *)(v28 + 16) = 0;
      *(float *)(v28 + 8) = *(float *)(v40 + 8);
      sub_482740("anim_splitpea_idle");
      result = sub_482430();
      a2[39] = *(_DWORD *)(v28 + 156);
      v6 = a2;
      break;
    case 18:
      sub_526F90(15.0, 20.0);
      *(float *)(v12 + 8) = 15.0;
      v29 = sub_461000(0.0, 0.0, v6[8] + 2);
      *(_DWORD *)(v29 + 16) = 0;
      *(float *)(v29 + 8) = *(float *)(v12 + 8);
      sub_482740("anim_head_idle1");
      sub_482430();
      v38 = a2[8] + 2;
      a2[38] = *(_DWORD *)(v29 + 156);
      v30 = sub_461000(0.0, 0.0, v38);
      *(_DWORD *)(v30 + 16) = 0;
      *(float *)(v30 + 8) = *(float *)(v40 + 8);
      sub_482740("anim_head_idle2");
      sub_482430();
      v39 = a2[8] + 2;
      a2[39] = *(_DWORD *)(v30 + 156);
      v31 = sub_461000(0.0, 0.0, v39);
      *(_DWORD *)(v31 + 16) = 0;
      *(float *)(v31 + 8) = *(float *)(v40 + 8);
      sub_482740("anim_head_idle3");
      result = sub_482430();
      a2[40] = *(_DWORD *)(v31 + 156);
      v6 = a2;
      break;
    case 3:
      v6[16] = 4000;
LABEL_72:
      result = sub_5EECC0(1000) + 1000;
      v6[44] = result;
      break;
    case 49:
      v6[16] = 4000;
      v6[44] = sub_5EECC0(1000) + 1000;
      result = sub_55BBB0(255, 64, 64);
      *(_DWORD *)(v12 + 72) = *(_DWORD *)result;
      *(_DWORD *)(v12 + 76) = *(_DWORD *)(result + 4);
      *(_DWORD *)(v12 + 80) = *(_DWORD *)(result + 8);
      *(_DWORD *)(v12 + 84) = *(_DWORD *)(result + 12);
      break;
    case 50:
      v6[16] = 4000;
      goto LABEL_72;
    case 23:
      v6[16] = 8000;
      v6[5] = 80;
      goto LABEL_72;
    case 36:
      v6[16] = 400;
      for ( result = 0; result < *(_DWORD *)(*(_DWORD *)(v12 + 12) + 4); v8 += 96 )
      {
        *(_BYTE *)(v8 + *(_DWORD *)(v12 + 88) + 93) = 0;
        ++result;
      }
      break;
    case 45:
      for ( result = 0; result < *(_DWORD *)(*(_DWORD *)(v12 + 12) + 4); v8 += 96 )
      {
        *(_BYTE *)(v8 + *(_DWORD *)(v12 + 88) + 93) = 0;
        ++result;
      }
      break;
    case 2:
      goto LABEL_85;
    case 48:
      result = sub_526F90(25.0, 30.0);
      *(float *)(v12 + 8) = 25.0;
      v6[21] = 200;
      break;
    case 20:
LABEL_85:
      result = sub_471D30();
      if ( (_BYTE)result )
      {
        v6[20] = 100;
        sub_482740("anim_explode");
        *(_DWORD *)(v12 + 16) = 3;
        result = sub_460A20();
      }
      break;
    case 4:
      *(float *)(v12 + 8) = 12.0;
      if ( (unsigned __int8)sub_471D30() )
      {
        sub_482EC0("anim_glow", -1);
        result = (int)a2;
        a2[21] = 1500;
        v6 = a2;
      }
      else
      {
        result = sub_482740("anim_armed");
        v6[15] = 16;
      }
      break;
    case 11:
      result = sub_471D30();
      if ( (_BYTE)result )
      {
        sub_482740("anim_land");
        *(_DWORD *)(v12 + 16) = 3;
        v6[15] = 8;
        result = sub_460A20();
      }
      break;
    case 9:
      *(_DWORD *)(v12 + 32) = 6;
      if ( (unsigned __int8)sub_471D30() )
      {
        v6[2] += sub_5EECC0(10) - 5;
        result = sub_5EECC0(10) - 5;
        v6[3] += result;
        v6[15] = 23;
        v6[21] = 12000;
      }
      else
      {
        if ( *((_BYTE *)v6 + 323) )
          result = sub_482740("anim_bigsleep");
        else
          result = sub_482740("anim_bigidle");
        v6[15] = 23;
        v6[21] = 12000;
      }
      break;
    case 8:
    case 24:
      result = sub_471D30();
      if ( (_BYTE)result )
      {
        v6[2] += sub_5EECC0(10) - 5;
        result = sub_5EECC0(6) - 3;
        v6[3] += result;
      }
      break;
    case 30:
      v6[16] = 4000;
      v6[4] = 120;
      result = sub_482EC0("Pumpkin_back", 1);
      goto LABEL_150;
    case 6:
      v6[15] = 1;
      break;
    case 25:
      v25 = *((_BYTE *)v6 + 324) == 0;
      v6[21] = 50;
      if ( v25 || *(_DWORD *)(*v6 + 2332) != 43 )
        sub_46D1D0(v6[2] + 40, v6[3] + 40, 500001, 46);
      result = sub_471D30();
      if ( (_BYTE)result )
        result = (*(int (__thiscall **)(_DWORD, int))(*(_DWORD *)*v6 + 216))(*v6, dword_7530D8);
      break;
    default:
      if ( a4 != 22 )
      {
        switch ( a4 )
        {
          case 38:
            result = sub_526F90(15.0, 20.0);
            *(float *)(v12 + 8) = 15.0;
            break;
          case 26:
            v6[15] = 30;
            break;
          case 35:
            v6[20] = 100;
            break;
          case 13:
            v6[15] = 1;
            break;
          case 47:
            result = sub_471D30();
            if ( (_BYTE)result )
            {
              v6[15] = 35;
              v6[21] = 500;
              result = sub_482740("anim_unarmed_idle");
            }
            break;
          case 34:
            result = sub_482F10(v12, "Cornpult_butter", -1);
            break;
          case 31:
            for ( result = 0; result < *(_DWORD *)(*(_DWORD *)(v12 + 12) + 4); v8 += 96 )
            {
              *(_BYTE *)(v8 + *(_DWORD *)(v12 + 88) + 93) = 0;
              ++result;
            }
            break;
          case 46:
            v6[16] = 450;
            break;
          default:
            if ( a4 != 51 )
            {
              switch ( a4 )
              {
                case 33:
                  result = sub_471D30();
                  if ( (_BYTE)result )
                  {
                    v6[15] = 47;
                    v6[21] = 100;
                  }
                  break;
                case 16:
                  result = sub_471D30();
                  if ( (_BYTE)result )
                  {
                    v6[15] = 48;
                    v6[21] = 100;
                  }
                  break;
                case 19:
                  for ( result = 0; result < *(_DWORD *)(*(_DWORD *)(v12 + 12) + 4); v8 += 96 )
                  {
                    *(_BYTE *)(v8 + *(_DWORD *)(v12 + 88) + 93) = 0;
                    ++result;
                  }
                  break;
              }
            }
            break;
        }
      }
      break;
  }
  if ( *(_DWORD *)(*v6 + 2332) == 39 )
  {
    result = v6[9];
    if ( result == 3 || result == 1 || result == 38 )
    {
      result = 2 * v6[16];
      v6[16] = result;
    }
  }
  v25 = v6[9] == 33;
  v6[17] = v6[16];
  if ( !v25 && *((_BYTE *)v6 + 324) )
  {
    result = sub_418F80(v6[10]);
    if ( result )
    {
      v33 = *(int **)(*(_DWORD *)(*a2 + 2372) + 8);
      v34 = 5 * (unsigned __int16)*(_DWORD *)(result + 148);
      result = *v33;
      *(float *)(*v33 + 32 * v34 + 8) = 0.0;
    }
  }
  return result;
}

