void *__thiscall sub_46BAD0(void *this, char a2)
{
  sub_46BAF0();
  if ( (a2 & 1) != 0 )
    operator delete(this);
  return this;
}

