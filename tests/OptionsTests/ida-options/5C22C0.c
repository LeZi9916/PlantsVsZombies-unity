int __thiscall sub_5C22C0(_DWORD *this, _BYTE *a2)
{
  int result; // eax
  int v3; // ecx
  _DWORD *v4; // [esp+0h] [ebp-30h]
  int v5; // [esp+Ch] [ebp-24h]
  _BYTE v6[4]; // [esp+18h] [ebp-18h] BYREF
  int v7; // [esp+1Ch] [ebp-14h]
  _BYTE *v8; // [esp+20h] [ebp-10h]
  _BYTE v9[11]; // [esp+24h] [ebp-Ch] BYREF
  char v10; // [esp+2Fh] [ebp-1h]

  v4 = this;
  result = (int)a2;
  if ( !a2[60] )
  {
    (*(void (__thiscall **)(_DWORD *))(*this + 68))(this);
    result = (int)a2;
    a2[60] = 1;
    if ( !v4[9] )
    {
      if ( a2[80] )
      {
        return (*(int (__thiscall **)(_DWORD *, _BYTE *))(*v4 + 72))(v4, a2);
      }
      else
      {
        v10 = 0;
        sub_62EFA0(v9);
        while ( 1 )
        {
          v5 = v4[6];
          sub_4086A0();
          v3 = v5;
          v7 = v5;
          if ( v4 == (_DWORD *)-4 )
            _invalid_parameter_noinfo();
          sub_408680(v3, v6);
          result = sub_632700(v6);
          if ( (_BYTE)result )
            break;
          v8 = *(_BYTE **)sub_5B7240(v9, v4);
          if ( v8 == a2 )
          {
            v10 = 1;
          }
          else if ( v10 && v8[100] )
          {
            if ( (*(unsigned __int8 (__thiscall **)(_BYTE *, _BYTE *))(*(_DWORD *)v8 + 8))(v8, a2) )
              (*(void (__thiscall **)(_DWORD *, _BYTE *))(*v4 + 64))(v4, v8);
          }
          sub_5D3980(v9);
        }
      }
    }
  }
  return result;
}

