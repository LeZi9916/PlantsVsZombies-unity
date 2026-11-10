_DWORD *__thiscall sub_5CE7D0(_DWORD *this, int a2)
{
  _DWORD *result; // eax
  int v3; // esi
  int v4; // eax
  int v5; // eax
  int v6; // eax
  int v7; // eax
  int v8; // eax
  int v9; // eax
  int v10; // eax
  int v11; // eax
  int v12; // eax
  int v13; // eax
  bool v14; // [esp+Ch] [ebp-74h]
  int v15; // [esp+10h] [ebp-70h]
  char v17; // [esp+30h] [ebp-50h]
  int v18[7]; // [esp+44h] [ebp-3Ch] BYREF
  void *v19; // [esp+60h] [ebp-20h]
  int v20; // [esp+64h] [ebp-1Ch]
  int v21; // [esp+68h] [ebp-18h]
  char v22; // [esp+6Fh] [ebp-11h]
  int v23; // [esp+70h] [ebp-10h]
  int v24; // [esp+7Ch] [ebp-4h]

  v17 = 0;
  result = this;
  if ( !*((_BYTE *)this + 281) )
  {
    if ( !this[49] && sub_43B340() )
    {
      v19 = operator new(0x20u);
      v24 = 0;
      if ( v19 )
      {
        sub_4033F0("Arial Unicode MS");
        v17 = 1;
        v15 = sub_5AF2A0(*(_DWORD *)(this[8] + 108), (int)v18, 10, 0, 0, 0, 0);
      }
      else
      {
        v15 = 0;
      }
      v20 = v15;
      v24 = -1;
      this[49] = v15;
      if ( (v17 & 1) != 0 )
        sub_401E90(v18);
    }
    v14 = *((_BYTE *)this + 104) && *((_BYTE *)this + 105) && !*((_BYTE *)this + 102);
    v22 = v14;
    v22 = *((_BYTE *)this + 280) ^ v14;
    v23 = 0;
    v21 = 0;
    if ( this[49] )
    {
      if ( this[48] )
      {
        if ( this[48] == 1 )
          v23 = this[18] - (*(int (__thiscall **)(_DWORD, _DWORD *))(*(_DWORD *)this[49] + 28))(this[49], this + 41);
      }
      else
      {
        v23 = (this[18] - (*(int (__thiscall **)(_DWORD, _DWORD *))(*(_DWORD *)this[49] + 28))(this[49], this + 41)) / 2;
      }
      v3 = (*(int (__thiscall **)(_DWORD))(*(_DWORD *)this[49] + 4))(this[49]) + this[19];
      v21 = (v3 - (*(int (__thiscall **)(_DWORD))(*(_DWORD *)this[49] + 4))(this[49]) / 6 - 1) / 2;
    }
    sub_592860(this[49]);
    if ( this[50] || this[52] )
    {
      if ( v22 )
      {
        if ( sub_5CE720(this[52], (int)(this + 62)) )
        {
          (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, _DWORD, _DWORD))(*this + 280))(
            this,
            a2,
            this[52],
            this + 62,
            0,
            0);
        }
        else if ( sub_5CE720(this[51], (int)(this + 58)) )
        {
          (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, int, int))(*this + 280))(
            this,
            a2,
            this[51],
            this + 58,
            1,
            1);
        }
        else
        {
          (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, int, int))(*this + 280))(
            this,
            a2,
            this[50],
            this + 54,
            1,
            1);
        }
        goto LABEL_53;
      }
      if ( *((_BYTE *)this + 102) && sub_5CE720(this[53], (int)(this + 66)) )
      {
        (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, _DWORD, _DWORD))(*this + 280))(
          this,
          a2,
          this[53],
          this + 66,
          0,
          0);
      }
      else if ( *((double *)this + 36) > 0.0 && sub_5CE720(this[51], (int)(this + 58)) )
      {
        if ( sub_5CE720(this[50], (int)(this + 54)) && *((double *)this + 36) < 1.0 )
          (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, _DWORD, _DWORD))(*this + 280))(
            this,
            a2,
            this[50],
            this + 54,
            0,
            0);
        sub_592910(1);
        v12 = sub_5CA5A0(255, 255, 255, (int)(*((double *)this + 36) * 255.0));
        sub_592880(v12);
        (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, _DWORD, _DWORD))(*this + 280))(
          this,
          a2,
          this[51],
          this + 58,
          0,
          0);
        sub_592910(0);
      }
      else if ( (*((_BYTE *)this + 105) || *((_BYTE *)this + 104)) && sub_5CE720(this[51], (int)(this + 58)) )
      {
        (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, _DWORD, _DWORD))(*this + 280))(
          this,
          a2,
          this[51],
          this + 58,
          0,
          0);
      }
      else if ( sub_5CE720(this[50], (int)(this + 54)) )
      {
        (*(void (__thiscall **)(_DWORD *, int, _DWORD, _DWORD *, _DWORD, _DWORD))(*this + 280))(
          this,
          a2,
          this[50],
          this + 54,
          0,
          0);
      }
    }
    else
    {
      if ( !*((_BYTE *)this + 282) )
      {
        v4 = sub_43B0D0();
        sub_592880(v4);
        sub_592990(0, 0, this[18], this[19]);
      }
      if ( v22 )
      {
        if ( !*((_BYTE *)this + 282) )
        {
          v5 = sub_43B0D0();
          sub_592880(v5);
          sub_592990(0, 0, this[18] - 1, 1);
          sub_592990(0, 0, 1, this[19] - 1);
          v6 = sub_43B0D0();
          sub_592880(v6);
          sub_592990(0, this[19] - 1, this[18], 1);
          sub_592990(this[18] - 1, 0, 1, this[19]);
          v7 = sub_43B0D0();
          sub_592880(v7);
          sub_592990(1, 1, this[18] - 3, 1);
          sub_592990(1, 1, 1, this[19] - 3);
        }
LABEL_53:
        v13 = sub_43B0D0();
        sub_592880(v13);
        return (_DWORD *)sub_592F40(this + 41, v23 + 1, v21 + 1);
      }
      if ( !*((_BYTE *)this + 282) )
      {
        v8 = sub_43B0D0();
        sub_592880(v8);
        sub_592990(0, 0, this[18] - 1, 1);
        sub_592990(0, 0, 1, this[19] - 1);
        v9 = sub_43B0D0();
        sub_592880(v9);
        sub_592990(0, this[19] - 1, this[18], 1);
        sub_592990(this[18] - 1, 0, 1, this[19]);
        v10 = sub_43B0D0();
        sub_592880(v10);
        sub_592990(1, this[19] - 2, this[18] - 2, 1);
        sub_592990(this[18] - 2, 1, 1, this[19] - 2);
      }
    }
    v11 = sub_43B0D0();
    sub_592880(v11);
    return (_DWORD *)sub_592F40(this + 41, v23, v21);
  }
  return result;
}

