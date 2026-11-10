void __thiscall sub_4571F0(int this, __int16 a2)
{
  int v3; // eax
  int v4; // eax
  int v5; // ecx
  int v6; // eax
  int v7; // ecx
  int v8; // edx
  _BYTE v9[4]; // [esp+4h] [ebp-1Ch] BYREF
  void *v10; // [esp+8h] [ebp-18h]
  unsigned int v11; // [esp+1Ch] [ebp-4h]

  if ( !*(_BYTE *)(this + 232) )
  {
    if ( byte_755DF5 )
    {
      if ( a2 == 117 )
      {
        v3 = *(_DWORD *)(this + 164);
        if ( *(_DWORD *)(v3 + 2384) )
        {
          *(_DWORD *)(*(_DWORD *)(v3 + 2384) + 84) = 2;
          v4 = *(_DWORD *)(*(_DWORD *)(this + 164) + 2384);
          *(_DWORD *)(v4 + 80) += 50000;
          v5 = *(_DWORD *)(v4 + 80);
          if ( v5 <= 99999 )
          {
            if ( v5 < 0 )
              *(_DWORD *)(v4 + 80) = 0;
          }
          else
          {
            *(_DWORD *)(v4 + 80) = 99999;
          }
          *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2384) + 816) = 1;
          *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2384) + 844) = 1;
          *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2384) + 848) = 1;
          *(_DWORD *)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2384) + 868) = 1;
          v6 = 1;
          v7 = 88;
          v8 = 100;
          do
          {
            if ( v6 != 50 && v6 != 60 && v6 != 70 && v6 != 13 )
              *(_DWORD *)(v7 + *(_DWORD *)(*(_DWORD *)(this + 164) + 2384)) = 20;
            v7 += 4;
            ++v6;
            --v8;
          }
          while ( v8 );
          sub_455420(this, 1, 1);
          sub_464510(*(_DWORD *)(*(_DWORD *)(*(_DWORD *)(this + 164) + 2384) + 32), (int)v9);
          sub_620130(v9);
          if ( v11 >= 0x10 )
            operator delete(v10);
        }
      }
    }
  }
}
