int __thiscall sub_5C1E60(_DWORD *this, int a2)
{
  int result; // eax
  int v3; // esi
  int v4; // eax
  int v5; // ecx
  bool v6; // dl
  int v7; // [esp+0h] [ebp-110h]
  int v9; // [esp+14h] [ebp-FCh]
  int v10; // [esp+30h] [ebp-E0h]
  int v11; // [esp+34h] [ebp-DCh]
  int v12; // [esp+54h] [ebp-BCh]
  int v13; // [esp+58h] [ebp-B8h]
  _DWORD *v14; // [esp+5Ch] [ebp-B4h]
  _BYTE v15[8]; // [esp+64h] [ebp-ACh] BYREF
  _DWORD v16[2]; // [esp+6Ch] [ebp-A4h] BYREF
  _DWORD v17[3]; // [esp+74h] [ebp-9Ch] BYREF
  int v18; // [esp+80h] [ebp-90h]
  int *v19; // [esp+84h] [ebp-8Ch]
  _DWORD *v20; // [esp+88h] [ebp-88h]
  int v21; // [esp+8Ch] [ebp-84h]
  _BYTE v22[4]; // [esp+90h] [ebp-80h] BYREF
  int v23; // [esp+94h] [ebp-7Ch]
  _BYTE v24[8]; // [esp+98h] [ebp-78h] BYREF
  int v25; // [esp+C0h] [ebp-50h]
  int v26; // [esp+C4h] [ebp-4Ch]
  _BYTE v27[8]; // [esp+C8h] [ebp-48h] BYREF
  _BYTE v28[8]; // [esp+D0h] [ebp-40h] BYREF
  int v29; // [esp+D8h] [ebp-38h]
  int v30; // [esp+DCh] [ebp-34h]
  _BYTE *v31; // [esp+E8h] [ebp-28h]
  int v32; // [esp+ECh] [ebp-24h]
  int v33; // [esp+F0h] [ebp-20h]
  int v34; // [esp+F4h] [ebp-1Ch]
  int v35; // [esp+F8h] [ebp-18h]
  _BYTE *v36; // [esp+FCh] [ebp-14h]
  int v37; // [esp+100h] [ebp-10h] BYREF
  int v38; // [esp+104h] [ebp-Ch]
  int v39; // [esp+108h] [ebp-8h]
  int v40; // [esp+10Ch] [ebp-4h]

  result = (*(int (__thiscall **)(_DWORD *))(*this + 76))(this);
  *(_BYTE *)(a2 + 60) = 1;
  if ( !this[9] )
  {
    v20 = this + 1;
    v21 = this[6];
    sub_65DB10(v21, this + 1);
    v17[2] = this + 1;
    v19 = (int *)this[6];
    v18 = *v19;
    sub_65DB10(v18, this + 1);
    v12 = v29;
    v13 = v30;
    sub_613C40(v17);
    sub_613C40(v16);
    v14 = (_DWORD *)sub_5D4C90(v15, v16[0], v16[1], v17[0], v17[1], &a2);
    sub_5D5240(*v14, v14[1]);
    v39 = v12;
    v40 = v13;
    sub_65DB10(this[6], this + 1);
    result = sub_632700(v28);
    if ( !(_BYTE)result )
    {
      v37 = v39;
      v38 = v40;
      sub_65DB10(*(_DWORD *)this[6], this + 1);
      if ( !(unsigned __int8)sub_632700(v27) )
      {
        v10 = v37;
        v11 = v38;
        sub_635720(&v37);
        v25 = v10;
        v26 = v11;
        while ( 1 )
        {
          v36 = *(_BYTE **)sub_5B7240(&v37, v7);
          if ( v36[100] )
          {
            if ( !v36[106] && !v36[80] )
            {
              v3 = sub_4091F0(this[18], this[19]);
              sub_4091F0(*(_DWORD *)(a2 + 72), *(_DWORD *)(a2 + 76));
              sub_464640(v3);
              if ( (*(unsigned __int8 (__thiscall **)(_BYTE *, int, int))(*(_DWORD *)v36 + 272))(v36, v32, v33) )
              {
                if ( (*(unsigned __int8 (__thiscall **)(_BYTE *, int, int))(*(_DWORD *)v36 + 272))(
                       v36,
                       v32 + v34 - 1,
                       v33 + v35 - 1) )
                {
                  (*(void (__thiscall **)(_BYTE *))(*(_DWORD *)v36 + 68))(v36);
                  break;
                }
              }
            }
            if ( (*(unsigned __int8 (__thiscall **)(_BYTE *, int))(*(_DWORD *)v36 + 8))(v36, a2) )
              (*(void (__thiscall **)(_DWORD *, _BYTE *))(*this + 64))(this, v36);
          }
          v4 = sub_62EFA0(v24);
          if ( (unsigned __int8)sub_632700(v4) )
            break;
          sub_635720(&v37);
        }
      }
      v37 = v39;
      v38 = v40;
      while ( 1 )
      {
        v9 = this[6];
        sub_4086A0();
        v23 = v9;
        if ( this == (_DWORD *)-4 )
          _invalid_parameter_noinfo();
        sub_408680(v5, v22);
        v6 = (unsigned __int8)sub_632700(v22) == 0;
        result = v6;
        if ( !v6 )
          break;
        v31 = *(_BYTE **)sub_5B7240(&v37, v7);
        if ( v31[100] )
        {
          if ( (*(unsigned __int8 (__thiscall **)(_BYTE *, int))(*(_DWORD *)v31 + 8))(v31, a2) )
            (*(void (__thiscall **)(_DWORD *, _BYTE *))(*this + 64))(this, v31);
        }
        sub_5D3980(&v37);
      }
    }
  }
  return result;
}

