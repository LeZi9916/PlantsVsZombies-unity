void __thiscall sub_46B270(_DWORD **this, int a2, char a3)
{
  _BYTE v4[28]; // [esp+1Ch] [ebp-108h] BYREF
  _BYTE v5[28]; // [esp+38h] [ebp-ECh] BYREF
  _BYTE v6[28]; // [esp+54h] [ebp-D0h] BYREF
  _BYTE v7[28]; // [esp+70h] [ebp-B4h] BYREF
  _BYTE v8[28]; // [esp+8Ch] [ebp-98h] BYREF
  _BYTE v9[28]; // [esp+A8h] [ebp-7Ch] BYREF
  _BYTE v10[28]; // [esp+C4h] [ebp-60h] BYREF
  _BYTE v11[28]; // [esp+E0h] [ebp-44h] BYREF
  _DWORD v12[7]; // [esp+FCh] [ebp-28h] BYREF
  int v13; // [esp+120h] [ebp-4h]

  (*(void (__thiscall **)(_DWORD *, int))(*this[1] + 216))(this[1], dword_753038);
  if ( a2 == 6 )
  {
    if ( !a3 && *((_BYTE *)this[1] + 874) )
    {
      sub_40D030(L"[DIALOG_BUTTON_OK]");
      v13 = 2;
      sub_40D030(&off_71139C);
      sub_40D030(L"[NO_WINDOWED_MODE]");
      (*(void (__thiscall **)(_DWORD *, int, int, _BYTE *, _BYTE *, _DWORD *, int))(*this[1] + 288))(
        this[1],
        10,
        1,
        v8,
        v10,
        v12,
        3);
      sub_401330(v8);
      sub_401330(v10);
      v13 = -1;
      sub_401330(v12);
      (*(void (__thiscall **)(_DWORD *, int, _DWORD))(*this[4] + 280))(this[4], 1, 0);
    }
  }
  else if ( a2 == 7 && a3 )
  {
    if ( (unsigned __int8)sub_62D1B0(this[1]) )
    {
      if ( !(unsigned __int8)sub_62D200(this[1]) )
      {
        sub_40D030(L"[DIALOG_BUTTON_OK]");
        v13 = 8;
        sub_40D030(&off_711464);
        sub_40D030(L"[DIALOG_WARNING]");
        (*(void (__thiscall **)(_DWORD *, int, int, _BYTE *, _BYTE *, _BYTE *, int))(*this[1] + 288))(
          this[1],
          16,
          1,
          v11,
          v4,
          v6,
          3);
        sub_401330(v11);
        sub_401330(v4);
        sub_401330(v6);
      }
    }
    else
    {
      (*(void (__thiscall **)(_DWORD *, _DWORD, _DWORD))(*this[5] + 280))(this[5], 0, 0);
      sub_40D030(L"[DIALOG_BUTTON_OK]");
      v13 = 5;
      sub_40D030(L"[HARDWARE_ACCELERATION_NOT_SUPPORTED]");
      sub_40D030(L"[NOT_SUPPORTED]");
      (*(void (__thiscall **)(_DWORD *, int, int, _BYTE *, _BYTE *, _BYTE *, int))(*this[1] + 288))(
        this[1],
        16,
        1,
        v5,
        v9,
        v7,
        3);
      sub_401330(v5);
      sub_401330(v9);
      sub_401330(v7);
    }
  }
}

