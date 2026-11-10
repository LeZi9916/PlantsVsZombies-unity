void __thiscall sub_4578E0(int this, int a2)
{
  _BYTE *v3; // esi
  int v4; // eax
  int v5; // ecx

  if ( !*(_DWORD *)(this + 188) )
  {
    if ( a2 == 101 )
    {
      if ( *(_BYTE *)(this + 80) )
      {
        sub_45D250(
          L"[MINIGAME_LOCKED_MESSAGE]",
          *(_DWORD *)(this + 4),
          49,
          L"[MODE_LOCKED]",
          L"[DIALOG_BUTTON_OK]",
          (int)&unk_708460,
          3);
        return;
      }
    }
    else if ( a2 == 102 )
    {
      if ( *(_BYTE *)(this + 81) )
      {
        sub_45D250(
          L"[PUZZLE_LOCKED_MESSAGE]",
          *(_DWORD *)(this + 4),
          49,
          L"[MODE_LOCKED]",
          L"[DIALOG_BUTTON_OK]",
          (int)&unk_708460,
          3);
        return;
      }
    }
    else if ( a2 == 110 && *(_BYTE *)(this + 82) )
    {
      sub_45D250(&off_70DC24, *(_DWORD *)(this + 4), 49, L"[MODE_LOCKED]", L"[DIALOG_BUTTON_OK]", (int)&unk_708460, 3);
      return;
    }
    switch ( a2 )
    {
      case 'd':
        sub_457580(this - 160);
        break;
      case 'e':
        sub_45C590();
        sub_45C840(*(_DWORD *)(this + 4), 1);
        break;
      case 'f':
        sub_45C590();
        sub_45C840(*(_DWORD *)(this + 4), 3);
        break;
      case 'g':
        sub_45CCF0(1);
        break;
      case 'h':
        sub_45C590();
        sub_45C5D0(*(_DWORD *)(this + 4), 2, 0);
        break;
      case 'i':
        sub_460410(*(_DWORD *)(this + 4));
        break;
      case 'j':
        sub_45D640();
        break;
      case 'k':
        v3 = (_BYTE *)sub_45C950();
        (*(void (__thiscall **)(_BYTE *, int))(*(_DWORD *)v3 + 300))(v3, 1);
        if ( v3[581] )
        {
          sub_45C590();
          sub_45C100(*(_DWORD *)(this + 4), 50, 0);
        }
        else
        {
          sub_469A10();
        }
        break;
      case 'l':
        v4 = sub_45CDA0();
        (*(void (__thiscall **)(int, int))(*(_DWORD *)v4 + 300))(v4, 1);
        sub_469A10();
        break;
      case 'm':
        sub_45C590();
        sub_45C100(*(_DWORD *)(this + 4), 43, 0);
        if ( (unsigned __int8)sub_4578B0() )
          sub_538080(*(_DWORD *)(*(_DWORD *)(this + 4) + 2368));
        break;
      case 'n':
        sub_45C590();
        sub_45C840(*(_DWORD *)(this + 4), 0);
        break;
      case 'o':
        if ( *(_BYTE *)(*(_DWORD *)(*(_DWORD *)(this + 4) + 2384) + 897) == 1 )
          sub_4584E0();
        else
          sub_45CE50();
        break;
      case 'p':
        sub_4584B0(0, 0);
        break;
      case 'q':
        sub_458570();
        break;
      case 'r':
        sub_458530();
        break;
      case 's':
        v5 = *(_DWORD *)(this + 4);
        if ( *(_DWORD *)(v5 + 2384) )
          sub_45DBC0(v5);
        break;
      default:
        return;
    }
  }
}
