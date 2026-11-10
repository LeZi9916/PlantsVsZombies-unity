void __thiscall sub_46B5B0(int this, int a2)
{
  int v3; // eax
  int v4; // esi
  int v5; // edx
  int v6; // edi
  int v7; // eax
  int v8; // eax
  _BYTE v9[4]; // [esp+1Ch] [ebp-98h] BYREF
  __int16 v10; // [esp+20h] [ebp-94h]
  int v11; // [esp+30h] [ebp-84h]
  int v12; // [esp+34h] [ebp-80h]
  _BYTE v13[4]; // [esp+38h] [ebp-7Ch] BYREF
  __int16 v14; // [esp+3Ch] [ebp-78h]
  int v15; // [esp+4Ch] [ebp-68h]
  int v16; // [esp+50h] [ebp-64h]
  _BYTE v17[28]; // [esp+54h] [ebp-60h] BYREF
  _BYTE v18[28]; // [esp+70h] [ebp-44h] BYREF
  _DWORD v19[7]; // [esp+8Ch] [ebp-28h] BYREF
  int v20; // [esp+B0h] [ebp-4h]

  sub_5D1040(a2);
  switch ( a2 )
  {
    case 0:
      v3 = sub_45CDA0();
      (*(void (__thiscall **)(int, int))(*(_DWORD *)v3 + 300))(v3, 1);
      break;
    case 1:
      v4 = *(_DWORD *)(this + 208);
      if ( *(_BYTE *)(this + 244) )
      {
        sub_45E740();
        sub_45C590();
        sub_45C5D0(*(_DWORD *)(this + 208), 1, 0);
      }
      else if ( *(_DWORD *)(v4 + 2152)
             && ((unsigned __int8)sub_414B70() || v5 && *(_DWORD *)(v5 + 372) && (unsigned __int8)sub_446350()) )
      {
        sub_45CB10(v4);
      }
      else
      {
        *(_DWORD *)(v4 + 2476) = 4;
        sub_45CAC0();
      }
      break;
    case 2:
      if ( *(_DWORD *)(*(_DWORD *)(this + 208) + 2152) )
      {
        v16 = 7;
        v15 = 0;
        v14 = 0;
        v20 = 1;
        v12 = 7;
        v11 = 0;
        v10 = 0;
        if ( !(unsigned __int8)sub_460AE0() && !(unsigned __int8)sub_460B00() )
          sub_460AC0();
        sub_40D090(v13);
        sub_40D090(v9);
        sub_40D030(&unk_708460);
        LOBYTE(v20) = 2;
        v6 = (*(int (__thiscall **)(_DWORD, int, int, _BYTE *, _BYTE *, _BYTE *, int))(**(_DWORD **)(this + 208) + 288))(
               *(_DWORD *)(this + 208),
               23,
               1,
               v13,
               v9,
               v17,
               1);
        LOBYTE(v20) = 1;
        sub_401330(v17);
        v7 = sub_52EB40();
        LOBYTE(v20) = 3;
        sub_40D0E0(v7, 0, -1);
        LOBYTE(v20) = 1;
        sub_401330(v18);
        v8 = sub_52EB40();
        LOBYTE(v20) = 4;
        sub_40D0E0(v8, 0, -1);
        LOBYTE(v20) = 1;
        sub_401330(v19);
        if ( (*(int (__thiscall **)(int, int))(*(_DWORD *)v6 + 300))(v6, 1) == 1000 )
        {
          sub_468F30();
          sub_52A960();
          sub_45E740();
          *(_DWORD *)(*(_DWORD *)(this + 208) + 2476) = 3;
          *(_BYTE *)(*(_DWORD *)(this + 208) + 2480) = *(_BYTE *)(*(_DWORD *)(*(_DWORD *)(this + 208) + 2152) + 22392);
          sub_45C100(*(_DWORD *)(this + 208), *(_DWORD *)(*(_DWORD *)(this + 208) + 2332), 0);
        }
        sub_401330(v9);
        sub_401330(v13);
      }
      break;
    default:
      return;
  }
}

