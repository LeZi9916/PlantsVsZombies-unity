void *__thiscall sub_5C5E80(void *this, char a2)
{
  sub_5C4A90(this);
  if ( (a2 & 1) != 0 )
    operator delete(this);
  return this;
}

