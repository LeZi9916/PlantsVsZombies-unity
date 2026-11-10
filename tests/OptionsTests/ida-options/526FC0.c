void sub_526FC0(int a1, int a2, int a3, int a4, int a5, int a6, ...)
{
  int v6; // eax
  int v7; // eax
  int v8; // eax
  int v9; // edi
  int v10; // eax
  bool v11; // [esp+17h] [ebp-9Dh]
  void *v12; // [esp+1Ch] [ebp-98h] BYREF
  int v13; // [esp+2Ch] [ebp-88h]
  unsigned int v14; // [esp+30h] [ebp-84h]
  int v15; // [esp+34h] [ebp-80h] BYREF
  _BYTE v16[4]; // [esp+38h] [ebp-7Ch] BYREF
  void *v17; // [esp+3Ch] [ebp-78h]
  unsigned int v18; // [esp+50h] [ebp-64h]
  _BYTE v19[4]; // [esp+54h] [ebp-60h] BYREF
  void *v20; // [esp+58h] [ebp-5Ch]
  unsigned int v21; // [esp+6Ch] [ebp-48h]
  void *v22; // [esp+74h] [ebp-40h]
  int v23; // [esp+84h] [ebp-30h]
  unsigned int v24; // [esp+88h] [ebp-2Ch]
  void *v25; // [esp+90h] [ebp-24h]
  unsigned int v26; // [esp+A4h] [ebp-10h]
  int v27; // [esp+B0h] [ebp-4h]
  va_list va; // [esp+D0h] [ebp+1Ch] BYREF

  va_start(va, a6);
  sub_52EA80(a2);
  v27 = 0;
  LOWORD(v12) = 0;
  v14 = 7;
  v13 = 0;
  sub_40D1E0(&off_70C1D0, 4);
  LOBYTE(v27) = 1;
  v15 = 38;
  if ( sub_43B0F0(&v15) != -1 )
  {
    sub_43B0A0(v16);
    LOBYTE(v27) = 2;
    v11 = sub_43B270(&v12) == 0;
    LOBYTE(v27) = 1;
    if ( v21 >= 8 )
      operator delete(v20);
    if ( v11 )
    {
      sub_43B0A0(v16);
      LOBYTE(v27) = 3;
      v6 = sub_43B0A0(v16);
      LOBYTE(v27) = 4;
      v7 = sub_429DF0(v6);
      LOBYTE(v27) = 5;
      sub_40D0E0(v7, 0, -1);
      if ( v24 >= 8 )
        operator delete(v22);
      LOBYTE(v27) = 3;
      v24 = 7;
      v23 = 0;
      LOWORD(v22) = 0;
      if ( v26 >= 8 )
        operator delete(v25);
      sub_40D0E0(v19, 0, -1);
      LOBYTE(v27) = 1;
      if ( v21 >= 8 )
        operator delete(v20);
    }
  }
  v8 = a3;
  if ( a6 == 1 )
  {
    v10 = (*(int (__thiscall **)(int, _BYTE *))(*(_DWORD *)a5 + 28))(a5, v16);
    v9 = a5;
  }
  else
  {
    v9 = a5;
    if ( a6 != 2 )
      goto LABEL_16;
    v10 = (*(int (__thiscall **)(int, _BYTE *))(*(_DWORD *)a5 + 28))(a5, v16) / 2;
  }
  v8 = a3 - v10;
LABEL_16:
  (*(void (__thiscall **)(int, int, int, int, _BYTE *, char *, int))(*(_DWORD *)v9 + 40))(
    v9,
    a1,
    v8,
    a4,
    v16,
    va,
    a1 + 32);
  if ( v14 >= 8 )
    operator delete(v12);
  v14 = 7;
  v13 = 0;
  LOWORD(v12) = 0;
  if ( v18 >= 8 )
    operator delete(v17);
}

