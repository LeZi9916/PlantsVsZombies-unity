_DWORD *__stdcall sub_46A260(int a1, int a2, int a3, const void *a4)
{
  _DWORD *result; // eax

  result = operator new(0x48u);
  if ( result )
  {
    *result = a1;
    result[1] = a2;
    result[2] = a3;
    qmemcpy(result + 4, a4, 0x30u);
    *((_BYTE *)result + 64) = 0;
    *((_BYTE *)result + 65) = 0;
  }
  return result;
}

