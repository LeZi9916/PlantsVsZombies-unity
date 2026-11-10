int __thiscall sub_46BE50(_DWORD *this, unsigned int a2)
{
  if ( a2 > this[5] )
    _invalid_parameter_noinfo();
  if ( this[6] < 8u )
    return (int)this + 2 * a2 + 4;
  else
    return this[1] + 2 * a2;
}

