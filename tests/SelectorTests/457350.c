int __thiscall sub_457350(int this, int a2, int a3, int a4)
{
  float *v5; // ecx
  _DWORD *v6; // ebx
  int v7; // esi
  double v8; // st7
  int result; // eax
  float *v10; // [esp+10h] [ebp-14h]
  float v11; // [esp+14h] [ebp-10h]
  float v12; // [esp+14h] [ebp-10h]
  float v13; // [esp+1Ch] [ebp-8h]
  float v14; // [esp+20h] [ebp-4h]
  float v15; // [esp+20h] [ebp-4h]

  v5 = (float *)&unk_708324;
  v10 = (float *)&unk_708324;
  v6 = (_DWORD *)(this + 304);
  do
  {
    v7 = **(_DWORD **)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2372) + 8) + 160 * (unsigned __int16)*v6;
    if ( *(float *)(v7 + 8) <= 0.0 )
    {
      v11 = (float)a2;
      v13 = (float)a3;
      v14 = (v11 - *(v5 - 1)) * (v11 - *(v5 - 1)) + (v13 - *v5) * (v13 - *v5);
      v15 = sqrt(v14);
      if ( v15 < 20.0 )
      {
        *(float *)(v7 + 8) = 24.0;
        if ( !*(_BYTE *)(*(_DWORD *)(this + 164) + 2537) )
        {
          v12 = 0.0;
          if ( 0.0 != *(float *)(dword_755E50 + 1304) )
          {
            v8 = *(float *)(dword_755E50 + 1304);
            sub_5EECE0(*(float *)(dword_755E50 + 1304));
            v12 = v8;
          }
          sub_52A5A0(v12);
        }
      }
      v5 = v10;
    }
    v5 += 2;
    ++v6;
    v10 = v5;
  }
  while ( (int)v5 < (int)flt_70833C );
  result = *(_DWORD *)(this + 164);
  if ( *(_BYTE *)(result + 2329) && *(_BYTE *)(this + 232) )
  {
    result = 450;
    if ( *(int *)(this + 236) < 450 )
      *(_DWORD *)(this + 236) = 450;
  }
  return result;
}
