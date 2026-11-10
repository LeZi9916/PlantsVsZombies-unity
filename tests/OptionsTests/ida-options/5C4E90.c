int __thiscall sub_5C4E90(void *this, int a2, int a3)
{
  int *v3; // eax
  int v4; // edx
  int v5; // eax
  int *v6; // eax
  int v7; // edx
  int v8; // eax
  int v9; // eax
  _BYTE v12[8]; // [esp+64h] [ebp-3Ch] BYREF
  _BYTE v13[8]; // [esp+6Ch] [ebp-34h] BYREF
  int v14; // [esp+74h] [ebp-2Ch]
  int v15; // [esp+78h] [ebp-28h]
  _BYTE v16[8]; // [esp+7Ch] [ebp-24h] BYREF
  int v17; // [esp+84h] [ebp-1Ch]
  int v18; // [esp+88h] [ebp-18h]
  int i; // [esp+9Ch] [ebp-4h]

  v3 = (int *)sub_5B74F0(v16);
  v4 = *v3;
  v5 = v3[1];
  v17 = v4;
  v18 = v5;
  v6 = (int *)sub_5B4100(v13);
  v7 = *v6;
  v8 = v6[1];
  v14 = v7;
  v15 = v8;
  sub_5D2E90(v12, v7, v8, v17, v18);
  for ( i = 0; i < a3; ++i )
  {
    v9 = sub_5CA5A0(
           *(_DWORD *)(a2 + 16 * i),
           *(_DWORD *)(a2 + 16 * i + 4),
           *(_DWORD *)(a2 + 16 * i + 8),
           *(_DWORD *)(a2 + 16 * i + 12));
    (*(void (__thiscall **)(void *, int, int))(*(_DWORD *)this + 136))(this, i, v9);
  }
  return (*(int (__thiscall **)(void *))(*(_DWORD *)this + 68))(this);
}

