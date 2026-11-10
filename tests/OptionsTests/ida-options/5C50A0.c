_DWORD *__thiscall sub_5C50A0(char *this, _DWORD *a2, int a3, _DWORD *a4)
{
  _DWORD *v4; // eax

  if ( a3 >= sub_43B1D0(this + 108) )
  {
    *a2 = *a4;
    a2[1] = a4[1];
    a2[2] = a4[2];
    a2[3] = a4[3];
  }
  else
  {
    v4 = (_DWORD *)sub_43B0D0();
    *a2 = *v4;
    a2[1] = v4[1];
    a2[2] = v4[2];
    a2[3] = v4[3];
  }
  return a2;
}

