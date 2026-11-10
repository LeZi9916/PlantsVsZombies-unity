_DWORD *__thiscall sub_5C53F0(_DWORD *this, char a2)
{
  _DWORD *result; // eax

  result = this;
  if ( this[8] )
  {
    if ( a2 )
      return (_DWORD *)sub_62C810(1);
    else
      return (_DWORD *)sub_62C810(0);
  }
  return result;
}

