int __stdcall sub_46B8F0(int a1, int a2, char a3)
{
  _UNKNOWN **v3; // edx
  int v4; // eax
  _BYTE v6[4]; // [esp+2Ch] [ebp-44h] BYREF
  void *v7; // [esp+30h] [ebp-40h]
  int v8; // [esp+40h] [ebp-30h]
  unsigned int v9; // [esp+44h] [ebp-2Ch]
  _BYTE v10[4]; // [esp+48h] [ebp-28h] BYREF
  void *v11; // [esp+4Ch] [ebp-24h]
  int v12; // [esp+5Ch] [ebp-14h]
  unsigned int v13; // [esp+60h] [ebp-10h]
  int v14; // [esp+6Ch] [ebp-4h]

  v13 = 7;
  v12 = 0;
  LOWORD(v11) = 0;
  sub_40D1E0(&unk_708460, 0);
  v14 = 1;
  LOWORD(v7) = 0;
  v9 = 7;
  v8 = 0;
  sub_40D1E0(&off_711784, 19);
  v3 = &off_7117AC;
  if ( !a3 )
    v3 = &off_7117C8;
  sub_40D1E0(v3, wcslen((const unsigned __int16 *)v3));
  LOBYTE(v14) = 2;
  sub_4646C0(a1, a2, 2 * (a3 != 0) + 30, 1, v6, v10, 2);
  if ( v9 >= 8 )
    operator delete(v7);
  v9 = 7;
  v8 = 0;
  LOWORD(v7) = 0;
  LOBYTE(v14) = 6;
  if ( v13 >= 8 )
    operator delete(v11);
  *(_DWORD *)(a1 + 388) = off_70C644;
  LOWORD(v11) = 0;
  v13 = 7;
  v12 = 0;
  *(_DWORD *)a1 = &off_7117E0;
  *(_DWORD *)(a1 + 160) = &off_718C68;
  *(_DWORD *)(a1 + 388) = &off_711918;
  *(_DWORD *)(a1 + 392) = a2;
  *(_BYTE *)(a1 + 385) = 0;
  v4 = sub_4643D0(a1 + 388);
  *(_DWORD *)(a1 + 396) = v4;
  *(_DWORD *)(v4 + 280) = 12;
  sub_5CAC50(dword_752B48, 220);
  sub_464B40(110, 40, 1);
  return a1;
}

