void __thiscall sub_40FE70(_DWORD *this, int a2)
{
  struct _EXCEPTION_REGISTRATION_RECORD *ExceptionList; // eax
  int v4; // eax
  int v5; // edx
  int v6; // esi
  int v7; // ebp
  int v8; // eax
  _BYTE v9[4]; // [esp+30h] [ebp-60h] BYREF
  void *v10; // [esp+34h] [ebp-5Ch]
  int v11; // [esp+44h] [ebp-4Ch]
  unsigned int v12; // [esp+48h] [ebp-48h]
  _BYTE v13[4]; // [esp+4Ch] [ebp-44h] BYREF
  void *v14; // [esp+50h] [ebp-40h]
  int v15; // [esp+60h] [ebp-30h]
  unsigned int v16; // [esp+64h] [ebp-2Ch]
  _BYTE v17[4]; // [esp+68h] [ebp-28h] BYREF
  void *v18; // [esp+6Ch] [ebp-24h]
  unsigned int v19; // [esp+80h] [ebp-10h]
  struct _EXCEPTION_REGISTRATION_RECORD *v20; // [esp+84h] [ebp-Ch]
  void *v21; // [esp+88h] [ebp-8h]
  int v22; // [esp+8Ch] [ebp-4h]

  v22 = -1;
  ExceptionList = NtCurrentTeb()->NtTib.ExceptionList;
  v21 = &loc_6EDD30;
  v20 = ExceptionList;
  v4 = (*(_DWORD *)(*(_DWORD *)(this[44] + 2384) + 76) - 2) / 10;
  v5 = (*(_DWORD *)(*(_DWORD *)(this[44] + 2384) + 76) - 2) % 10;
  v6 = v5 + 1 + 8 * v4;
  if ( v5 + 1 < 10 )
  {
    if ( v5 + 1 >= 5 )
      v6 = v5 + 8 * v4;
  }
  else
  {
    v6 -= 2;
  }
  if ( v6 > 40 )
    v6 = 40;
  sub_4760E0(v17);
  v22 = 1;
  v12 = 7;
  v11 = 0;
  LOWORD(v10) = 0;
  v7 = this[44];
  if ( !*(_BYTE *)(v7 + 2536)
    && (*(_DWORD *)(v7 + 2460) && (unsigned __int8)sub_651F30() || *(_DWORD *)(v7 + 2532) != 1)
    || v6 < 17
    || v6 == 19 )
  {
    v8 = sub_4763C0(v6);
    LOBYTE(v22) = 2;
    sub_40D0E0(v8, 0, -1);
    LOBYTE(v22) = 1;
    if ( v16 >= 8 )
      operator delete(v14);
  }
  else
  {
    sub_40D1E0(&off_708BAC, 27);
  }
  v16 = 7;
  v15 = 0;
  LOWORD(v14) = 0;
  sub_40D1E0(&off_708BE4, 11);
  LOBYTE(v22) = 3;
  sub_40FCF0(this, v13, v17, v9);
  LOBYTE(v22) = 1;
  if ( v16 >= 8 )
    operator delete(v14);
  sub_593AD0(2.0, 2.0, 190.0, 360.0);
  sub_49D260(a2, 190.0, 360.0, v6, -1, 0.0, 255, 1, 0);
  sub_593AD0(1.0, 1.0, 0.0, 0.0);
  if ( v12 >= 8 )
    operator delete(v10);
  v12 = 7;
  v11 = 0;
  LOWORD(v10) = 0;
  if ( v19 >= 8 )
    operator delete(v18);
}

