int __thiscall sub_5C06E0(void *this, int a2)
{
  int v2; // eax
  _BYTE v5[16]; // [esp+8h] [ebp-20h] BYREF
  _BYTE v6[16]; // [esp+18h] [ebp-10h] BYREF

  (*(void (__thiscall **)(int, _BYTE *))(*(_DWORD *)a2 + 4))(a2, v5);
  v2 = (*(int (__thiscall **)(void *, _BYTE *))(*(_DWORD *)this + 4))(this, v6);
  return sub_47FBE0(v2);
}

