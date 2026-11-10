void __thiscall sub_453480(_BYTE *this, int a2)
{
  bool v3; // bl
  char v4; // bl
  int v5; // ebp
  int v6; // edi
  int v7; // eax
  int v8; // edi
  int v9; // edi
  unsigned int v10; // ecx
  int v11; // eax
  int v12; // eax
  int v13; // [esp+28h] [ebp-8h]
  int v14; // [esp+2Ch] [ebp-4h]

  if ( this[281] )
    return;
  v3 = this[104] && this[105] && !this[102];
  v4 = this[280] ^ v3;
  v5 = *((_DWORD *)this + 80) + *((_DWORD *)this + 82);
  v6 = *((_DWORD *)this + 79) + *((_DWORD *)this + 81);
  v13 = v6;
  v14 = v5;
  if ( *((_DWORD *)this + 49) )
  {
    v7 = *((_DWORD *)this + 48);
    if ( v7 )
    {
      if ( v7 != 1 )
      {
LABEL_13:
        v9 = *((_DWORD *)this + 19)
           - (*(int (__thiscall **)(_DWORD))(**((_DWORD **)this + 49) + 4))(*((_DWORD *)this + 49)) / 6
           - 1;
        v5 += (v9 + (*(int (__thiscall **)(_DWORD))(**((_DWORD **)this + 49) + 4))(*((_DWORD *)this + 49))) / 2;
        v14 = v5;
        goto LABEL_14;
      }
      v8 = *((_DWORD *)this + 18)
         - (*(int (__thiscall **)(_DWORD, _BYTE *))(**((_DWORD **)this + 49) + 28))(*((_DWORD *)this + 49), this + 164)
         + v6;
    }
    else
    {
      v8 = (*((_DWORD *)this + 18)
          - (*(int (__thiscall **)(_DWORD, _BYTE *))(**((_DWORD **)this + 49) + 28))(*((_DWORD *)this + 49), this + 164))
         / 2
         + v6;
    }
    v13 = v8;
    goto LABEL_13;
  }
LABEL_14:
  sub_592910(1);
  v10 = (*((_DWORD *)this + 31) - *((_DWORD *)this + 30)) >> 4;
  if ( !v4 )
  {
    if ( v10 <= 5 )
      _invalid_parameter_noinfo();
    sub_592880(*((_DWORD *)this + 30) + 80);
    if ( this[102] && (unsigned __int8)sub_5CE720(*((_DWORD *)this + 53), this + 264) )
    {
      (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(
        this,
        a2,
        *((_DWORD *)this + 53),
        this + 264,
        *((_DWORD *)this + 87),
        *((_DWORD *)this + 88));
LABEL_33:
      sub_592910(0);
      if ( this[105] )
      {
        v11 = *((_DWORD *)this + 84);
        if ( !v11 )
          v11 = *((_DWORD *)this + 49);
        sub_592860(v11);
        if ( (unsigned int)((*((_DWORD *)this + 31) - *((_DWORD *)this + 30)) >> 4) <= 1 )
          _invalid_parameter_noinfo();
        sub_592880(*((_DWORD *)this + 30) + 16);
        sub_592F40(this + 164, v13, v5);
      }
      else
      {
        sub_592860(*((_DWORD *)this + 49));
        if ( !((*((_DWORD *)this + 31) - *((_DWORD *)this + 30)) >> 4) )
          _invalid_parameter_noinfo();
        sub_592880(*((_DWORD *)this + 30));
        sub_592F40(this + 164, v13, v5);
      }
      return;
    }
    if ( *((double *)this + 36) > 0.0 )
    {
      if ( (unsigned __int8)sub_5CE720(*((_DWORD *)this + 51), this + 232) )
      {
        if ( (unsigned __int8)sub_5CE720(*((_DWORD *)this + 50), this + 216) && *((double *)this + 36) < 1.0 )
          (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(
            this,
            a2,
            *((_DWORD *)this + 50),
            this + 216,
            *((_DWORD *)this + 87),
            *((_DWORD *)this + 88));
        *(_DWORD *)(a2 + 60) = (int)(*((double *)this + 36) * 255.0);
        (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(
          this,
          a2,
          *((_DWORD *)this + 51),
          this + 232,
          *((_DWORD *)this + 87),
          *((_DWORD *)this + 88));
        v5 = v14;
        goto LABEL_33;
      }
      v5 = v14;
    }
    if ( (this[105] || this[104]) && (unsigned __int8)sub_5CE720(*((_DWORD *)this + 51), this + 232) )
    {
      (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(
        this,
        a2,
        *((_DWORD *)this + 51),
        this + 232,
        *((_DWORD *)this + 87),
        *((_DWORD *)this + 88));
    }
    else if ( (unsigned __int8)sub_5CE720(*((_DWORD *)this + 50), this + 216) )
    {
      (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, _DWORD, _DWORD))(*(_DWORD *)this + 280))(
        this,
        a2,
        *((_DWORD *)this + 50),
        this + 216,
        *((_DWORD *)this + 87),
        *((_DWORD *)this + 88));
    }
    goto LABEL_33;
  }
  if ( v10 <= 5 )
    _invalid_parameter_noinfo();
  sub_592880(*((_DWORD *)this + 30) + 80);
  if ( (unsigned __int8)sub_5CE720(*((_DWORD *)this + 52), this + 248) )
  {
    (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, int, int))(*(_DWORD *)this + 280))(
      this,
      a2,
      *((_DWORD *)this + 52),
      this + 248,
      *((_DWORD *)this + 79) + *((_DWORD *)this + 87),
      *((_DWORD *)this + 80) + *((_DWORD *)this + 88));
  }
  else if ( (unsigned __int8)sub_5CE720(*((_DWORD *)this + 51), this + 232) )
  {
    (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, int, int))(*(_DWORD *)this + 280))(
      this,
      a2,
      *((_DWORD *)this + 51),
      this + 232,
      *((_DWORD *)this + 79) + *((_DWORD *)this + 87),
      *((_DWORD *)this + 80) + *((_DWORD *)this + 88));
  }
  else if ( (unsigned __int8)sub_5CE720(*((_DWORD *)this + 50), this + 216) )
  {
    (*(void (__thiscall **)(_BYTE *, int, _DWORD, _BYTE *, int, int))(*(_DWORD *)this + 280))(
      this,
      a2,
      *((_DWORD *)this + 50),
      this + 216,
      *((_DWORD *)this + 79) + *((_DWORD *)this + 87),
      *((_DWORD *)this + 80) + *((_DWORD *)this + 88));
  }
  sub_592910(0);
  v12 = *((_DWORD *)this + 84);
  if ( !v12 )
    v12 = *((_DWORD *)this + 49);
  sub_592860(v12);
  if ( (unsigned int)((*((_DWORD *)this + 31) - *((_DWORD *)this + 30)) >> 4) <= 1 )
    _invalid_parameter_noinfo();
  sub_592880(*((_DWORD *)this + 30) + 16);
  sub_592F40(this + 164, v13 + *((_DWORD *)this + 85), v5 + *((_DWORD *)this + 86));
}
