int __thiscall sub_5C5EB0(double *this, double a2)
{
  this[21] = a2;
  if ( this[21] >= 0.0 )
  {
    if ( this[21] > 1.0 )
      this[21] = 1.0;
  }
  else
  {
    this[21] = 0.0;
  }
  return (*(int (__thiscall **)(double *))(*(_DWORD *)this + 76))(this);
}

