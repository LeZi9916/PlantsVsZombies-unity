int __userpurge sub_455420@<eax>(int a1@<ecx>, int a2@<edi>, char a3)
{
  struct _EXCEPTION_REGISTRATION_RECORD *ExceptionList; // eax
  int *v5; // ecx
  int v6; // edx
  void (__thiscall *v7)(int *); // eax
  int v8; // ecx
  int v9; // edx
  int v10; // eax
  int v11; // edi
  int v12; // eax
  int v13; // eax
  int v14; // eax
  int v15; // ecx
  int v17; // [esp+14h] [ebp-60h] BYREF
  __int16 v18; // [esp+18h] [ebp-5Ch]
  void *v19; // [esp+1Ch] [ebp-58h]
  int v20; // [esp+28h] [ebp-4Ch]
  int v21; // [esp+2Ch] [ebp-48h]
  unsigned int v22; // [esp+30h] [ebp-44h] BYREF
  __int16 v23; // [esp+34h] [ebp-40h]
  void *v24; // [esp+38h] [ebp-3Ch]
  int v25; // [esp+44h] [ebp-30h]
  int v26; // [esp+48h] [ebp-2Ch]
  unsigned int v27; // [esp+4Ch] [ebp-28h] BYREF
  __int16 v28; // [esp+50h] [ebp-24h]
  void *v29; // [esp+54h] [ebp-20h]
  int v30; // [esp+60h] [ebp-14h]
  int v31; // [esp+64h] [ebp-10h]
  unsigned int v32; // [esp+68h] [ebp-Ch]
  void *v33; // [esp+6Ch] [ebp-8h]
  int v34; // [esp+70h] [ebp-4h]
  int retaddr; // [esp+74h] [ebp+0h]

  v34 = -1;
  ExceptionList = NtCurrentTeb()->NtTib.ExceptionList;
  v33 = &SEH_43B380;
  v32 = (unsigned int)ExceptionList;
  if ( a3 )
  {
    *(_BYTE *)(a1 + 336) = 1;
    v31 = 7;
    v30 = 0;
    v28 = 0;
    sub_40D1E0(&unk_708460, 0);
    v34 = 2;
    v23 = 0;
    v26 = 7;
    v25 = 0;
    sub_40D1E0(&unk_708460, 0);
    v21 = 7;
    v20 = 0;
    v18 = 0;
    sub_40D1E0(&off_70D88C, 9);
    (*(void (__thiscall **)(_DWORD, int, int, int *, unsigned int *, unsigned int *, _DWORD, int))(**(_DWORD **)(a1 + 164)
                                                                                                 + 288))(
      *(_DWORD *)(a1 + 164),
      49,
      1,
      &v17,
      &v22,
      &v27,
      0,
      a2);
    if ( v22 >= 8 )
      operator delete(v19);
    v22 = 7;
    v21 = 0;
    LOWORD(v19) = 0;
    if ( v27 >= 8 )
      operator delete(v24);
    retaddr = -1;
    v27 = 7;
    v26 = 0;
    LOWORD(v24) = 0;
    if ( v32 >= 8 )
      operator delete(v29);
    v5 = *(int **)(a1 + 164);
    v6 = *v5;
    LOWORD(v29) = 0;
    v7 = *(void (__thiscall **)(int *))(v6 + 364);
    v32 = 7;
    v31 = 0;
    v7(v5);
    sub_462C30();
    (*(void (__thiscall **)(_DWORD, int))(**(_DWORD **)(a1 + 164) + 308))(*(_DWORD *)(a1 + 164), 49);
    *(_BYTE *)(a1 + 336) = 0;
  }
  v8 = *(_DWORD *)(a1 + 248);
  v9 = **(_DWORD **)(*(_DWORD *)(a1 + 164) + 2372);
  if ( v8 )
  {
    if ( (unsigned int)(unsigned __int16)v8 < *(_DWORD *)(v9 + 8) )
    {
      v10 = *(_DWORD *)v9 + 44 * (unsigned __int16)v8;
      if ( *(_DWORD *)(v10 + 40) == v8 )
      {
        sub_52B620(v10);
        *(_DWORD *)(a1 + 248) = 0;
      }
    }
  }
  v11 = *(_DWORD *)(a1 + 164);
  *(_DWORD *)(a1 + 332) = 1;
  v12 = *(_DWORD *)(v11 + 2384);
  if ( v12 )
    *(_DWORD *)(a1 + 332) = *(_DWORD *)(v12 + 76);
  *(_BYTE *)(a1 + 243) = 1;
  *(_BYTE *)(a1 + 240) = 1;
  *(_BYTE *)(a1 + 241) = 1;
  *(_BYTE *)(a1 + 242) = 1;
  *(_BYTE *)(a1 + 244) = 0;
  if ( *(_DWORD *)(v11 + 2384) )
  {
    if ( *(int *)(a1 + 332) >= 2 )
      *(_BYTE *)(a1 + 243) = 0;
    v13 = *(_DWORD *)(v11 + 2384);
    if ( v13 && *(int *)(v13 + 84) > 0 )
    {
      *(_BYTE *)(a1 + 240) = 0;
      *(_BYTE *)(a1 + 242) = 0;
      *(_BYTE *)(a1 + 241) = 0;
      *(_BYTE *)(a1 + 243) = 0;
    }
    if ( *(int *)(*(_DWORD *)(v11 + 2384) + 844) > 0 )
      *(_BYTE *)(a1 + 240) = 0;
    if ( *(int *)(*(_DWORD *)(v11 + 2384) + 848) > 0 )
      *(_BYTE *)(a1 + 241) = 0;
    if ( *(int *)(*(_DWORD *)(v11 + 2384) + 868) > 0 )
      *(_BYTE *)(a1 + 242) = 0;
    if ( *(_BYTE *)(v11 + 2536)
      || (!*(_DWORD *)(v11 + 2460) || !(unsigned __int8)((int (*)(void))sub_651F30)()) && *(_DWORD *)(v11 + 2532) == 1 )
    {
      *(_BYTE *)(a1 + 241) = 1;
      *(_BYTE *)(a1 + 242) = 1;
    }
    v11 = *(_DWORD *)(a1 + 164);
    v9 = *(_DWORD *)(v11 + 2384);
    if ( *(_BYTE *)(v9 + 832) )
      *(_BYTE *)(a1 + 244) = 1;
  }
  v14 = *(_DWORD *)(v11 + 2384);
  *(_BYTE *)(a1 + 344) = v14
                      && *(int *)(v14 + 84) > 0
                      && !*(_BYTE *)(v11 + 2536)
                      && ((v15 = *(_DWORD *)(v11 + 2460)) != 0 && (unsigned __int8)sub_651F30(v15, v9)
                       || *(_DWORD *)(v11 + 2532) != 1);
  if ( *(_BYTE *)(a1 + 344) && *(_DWORD *)(a1 + 328) )
    sub_4553E0();
  sub_454F30();
  dword_752A58 = 0;
  dword_752A5C = 0;
  dword_752A60 = 0;
  dword_752A64 = 0;
  dword_752A68 = 0;
  dword_752A6C = 0;
  dword_752A70 = 0;
  dword_752A74 = 0;
  word_752A78 = 0;
  byte_755DF4 = 0;
  return sub_455780(a1);
}
