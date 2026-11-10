int __thiscall sub_5C4F90(char *this, int a2, _DWORD *a3)
{
  _DWORD *v3; // eax
  _DWORD *v4; // eax
  char v7[16]; // [esp+90h] [ebp-10h] BYREF

  if ( a2 >= sub_43B1D0(this + 108) )
  {
    v3 = (_DWORD *)sub_55BAB0(v7);
    sub_5D2D90(a2 + 1, *v3, v3[1], v3[2], v3[3]);
  }
  v4 = (_DWORD *)sub_43B0D0();
  *v4 = *a3;
  v4[1] = a3[1];
  v4[2] = a3[2];
  v4[3] = a3[3];
  return (*(int (__thiscall **)(char *))(*(_DWORD *)this + 68))(this);
}

