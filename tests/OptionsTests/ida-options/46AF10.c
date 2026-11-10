void __thiscall sub_46AF10(int this, int a2)
{
  _BYTE v3[4]; // [esp+20h] [ebp-48h] BYREF
  void *v4; // [esp+24h] [ebp-44h]
  int v5; // [esp+34h] [ebp-34h]
  unsigned int v6; // [esp+38h] [ebp-30h]
  _BYTE v7[4]; // [esp+3Ch] [ebp-2Ch] BYREF
  void *v8; // [esp+40h] [ebp-28h]
  int v9; // [esp+50h] [ebp-18h]
  unsigned int v10; // [esp+54h] [ebp-14h]
  int v11; // [esp+64h] [ebp-4h]

  sub_592F90(dword_752CC8, 0, 0);
  if ( *(_BYTE *)(this + 404) )
  {
    *(_DWORD *)(this + 412) = 5;
    *(_DWORD *)(this + 416) = 10;
    *(_DWORD *)(this + 420) = 15;
    *(_DWORD *)(this + 424) = 20;
  }
  sub_55BBB0(107, 109, 145);
  LOWORD(v4) = 0;
  v6 = 7;
  v5 = 0;
  sub_40D1E0(&off_7112EC, 21);
  v11 = 0;
  sub_526FC0(a2, v3, 186, *(_DWORD *)(this + 412) + 140, dword_752C9C, 1);
  v11 = -1;
  if ( v6 >= 8 )
    operator delete(v4);
  v6 = 7;
  v5 = 0;
  LOWORD(v4) = 0;
  sub_40D1E0(&off_711318, 17);
  v11 = 1;
  sub_526FC0(a2, v3, 186, *(_DWORD *)(this + 416) + 167, dword_752C9C, 1);
  v11 = -1;
  if ( v6 >= 8 )
    operator delete(v4);
  LOWORD(v4) = 0;
  v6 = 7;
  v5 = 0;
  sub_40D1E0(&off_71133C, 25);
  v11 = 2;
  sub_526FC0(a2, v3, 274, *(_DWORD *)(this + 420) + 197, dword_752C9C, 1);
  v11 = -1;
  if ( v6 >= 8 )
    operator delete(v4);
  v10 = 7;
  v9 = 0;
  LOWORD(v8) = 0;
  sub_40D1E0(&off_711370, 21);
  v11 = 3;
  sub_526FC0(a2, v7, 274, *(_DWORD *)(this + 424) + 229, dword_752C9C, 1);
  if ( v10 >= 8 )
    operator delete(v8);
}

