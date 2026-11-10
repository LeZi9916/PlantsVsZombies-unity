int __userpurge sub_45CCF0@<eax>(int a1@<edi>, char a2)
{
  void *v2; // eax
  int v3; // esi

  v2 = operator new(0x1ACu);
  v3 = 0;
  if ( v2 )
    v3 = sub_46A350((int)v2, (_BYTE *)a1, a2);
  (*(void (__thiscall **)(int, int, int, _DWORD, _DWORD))(*(_DWORD *)v3 + 160))(
    v3,
    (800 - *((_DWORD *)dword_752CC8 + 9)) / 2,
    (600 - *((_DWORD *)dword_752CC8 + 10)) / 2,
    *((_DWORD *)dword_752CC8 + 9),
    *((_DWORD *)dword_752CC8 + 10));
  (*(void (__thiscall **)(int, int, int))(*(_DWORD *)a1 + 300))(a1, 2, v3);
  return (*(int (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 800) + 32))(*(_DWORD *)(a1 + 800), v3);
}

