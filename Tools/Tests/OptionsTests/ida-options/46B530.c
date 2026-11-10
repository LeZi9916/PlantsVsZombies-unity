int __thiscall sub_46B530(_DWORD *this, int a2)
{
  int result; // eax

  result = this[92];
  if ( *(_DWORD *)(result + 2152) )
    result = sub_426CC0(a2);
  if ( a2 == 32 || a2 == 13 )
    return sub_5D1040(1000);
  if ( a2 == 27 )
    return sub_5D1040(1001);
  return result;
}

