_BYTE *__cdecl sub_464490(int a1, int a2, char a3)
{
  _BYTE *result; // eax

  if ( operator new(0x104u) )
    result = (_BYTE *)sub_5CE0A0(dword_75382C, dword_752A98, a1, a2);
  else
    result = 0;
  result[168] = a3;
  result[80] = 1;
  result[106] = 1;
  return result;
}

