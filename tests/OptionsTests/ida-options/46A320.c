int __usercall sub_46A320@<eax>(int result@<eax>)
{
  int i; // ecx

  for ( i = *(_DWORD *)(result + 8); !*(_BYTE *)(i + 65); i = *(_DWORD *)(i + 8) )
    result = i;
  return result;
}

