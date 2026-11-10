int __userpurge sub_4812C0@<eax>(int a1@<eax>, int a2@<ebx>, int a3)
{
  int *v4; // edi
  double v5; // st7
  double v6; // st7
  int result; // eax
  float v8; // [esp+20h] [ebp+4h]

  sub_482180();
  sub_481350(a3);
  v4 = (int *)(*(_DWORD *)(a3 + 88) + 96 * a2);
  v5 = *(float *)(a1 + 24);
  if ( v5 <= 0.0 )
    v6 = v5 - 0.5;
  else
    v6 = v5 + 0.5;
  result = (int)v6;
  if ( (int)v6 >= 0 )
  {
    result = *v4;
    if ( *v4 > 0 )
    {
      v8 = (double)*v4 / (double)v4[1];
      return sub_481180(v8);
    }
  }
  return result;
}
