void *__thiscall sub_5C5040(char *this, int a2)
{
  if ( (dword_815EAC & 1) == 0 )
  {
    dword_815EAC |= 1u;
    sub_55BAB0(&unk_815E9C);
  }
  if ( a2 >= sub_43B1D0(this + 108) )
    return &unk_815E9C;
  else
    return (void *)sub_43B0D0();
}

