int __thiscall sub_5C1BB0(int this, int a2)
{
  _DWORD *v4; // [esp+4h] [ebp-18h]
  _DWORD *v5; // [esp+8h] [ebp-14h]
  _BYTE v6[8]; // [esp+Ch] [ebp-10h] BYREF

  if ( *(_DWORD *)(this + 36) )
  {
    v4 = (_DWORD *)(*(int (__thiscall **)(_DWORD, _BYTE *))(**(_DWORD **)(this + 36) + 60))(*(_DWORD *)(this + 36), v6);
    v5 = (_DWORD *)sub_409250(*(_DWORD *)(this + 64), *(_DWORD *)(this + 68));
    sub_409250(*v4 + *v5, v4[1] + v5[1]);
  }
  else
  {
    sub_409250(*(_DWORD *)(this + 64), *(_DWORD *)(this + 68));
  }
  return a2;
}

