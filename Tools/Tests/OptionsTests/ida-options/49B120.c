char __usercall sub_49B120@<al>(int a1@<esi>)
{
  int v1; // ebx
  int v2; // eax
  int v3; // eax
  int v4; // eax
  int v5; // edi
  int v6; // ebx
  int v7; // eax
  int v8; // eax
  int v9; // edi
  int v10; // ebx
  int v11; // ebx

  v1 = *(_DWORD *)(a1 + 3368);
  if ( *(_DWORD *)(v1 + 2332) == 22 && !(unsigned __int8)sub_49C7E0(a1) )
  {
    v2 = sub_45D250(&off_718668, v1, 28, L"[DIALOG_WARNING]", L"[DIALOG_BUTTON_YES]", (int)L"[REPICK_BUTTON]", 1);
    if ( v2 != 1000 )
      return v2;
  }
  v3 = *(_DWORD *)(a1 + 3368);
  if ( !*(_DWORD *)(v3 + 2332) )
  {
    v4 = *(_DWORD *)(v3 + 2384);
    if ( (!v4 || *(int *)(v4 + 84) <= 0)
      && *(_DWORD *)(*(_DWORD *)(a1 + 3372) + 21864) == 11
      && !(unsigned __int8)sub_49C7E0(a1) )
    {
      LOBYTE(v2) = sub_49AE80(a1, &off_7186B0);
      if ( !(_BYTE)v2 )
        return v2;
    }
  }
  if ( !(unsigned __int8)sub_49C7E0(a1) && !(unsigned __int8)sub_49C7E0(a1) && !(unsigned __int8)sub_49C7E0(a1) )
  {
    v5 = *(_DWORD *)(a1 + 3372);
    if ( !(unsigned __int8)sub_446350() && *(_DWORD *)(*(_DWORD *)(a1 + 3368) + 2332) != 31 )
    {
      if ( (unsigned __int8)sub_461580() && *(_DWORD *)(v5 + 21864) == 11 )
        LOBYTE(v2) = sub_49AE80(a1, &off_7186F8);
      else
        LOBYTE(v2) = sub_49AE80(a1, &off_71873C);
      goto LABEL_29;
    }
  }
  v6 = *(_DWORD *)(a1 + 3372);
  v7 = *(_DWORD *)(v6 + 21860);
  if ( (v7 == 2 || v7 == 3)
    && !(unsigned __int8)sub_49C7E0(a1)
    && !(unsigned __int8)sub_49C7E0(a1)
    && !(unsigned __int8)sub_49C7E0(a1)
    && !(unsigned __int8)sub_446350() )
  {
    if ( (unsigned __int8)sub_461580() && *(_DWORD *)(v6 + 21864) == 21 )
      LOBYTE(v2) = sub_49AE80(a1, &off_718774);
    else
      LOBYTE(v2) = sub_49AE80(a1, &off_7187AC);
LABEL_29:
    if ( !(_BYTE)v2 )
      return v2;
  }
  v8 = *(_DWORD *)(*(_DWORD *)(a1 + 3372) + 21860);
  if ( v8 != 4 && v8 != 5
    || (unsigned __int8)sub_49C7E0(a1)
    || (v9 = *(_DWORD *)(a1 + 3368), *(_BYTE *)(v9 + 2536))
    || (!*(_DWORD *)(v9 + 2460) || !(unsigned __int8)sub_651F30()) && *(_DWORD *)(v9 + 2532) == 1
    || sub_460EB0() <= 33
    || (LOBYTE(v2) = sub_49AE80(a1, &off_7187E4), (_BYTE)v2) )
  {
    v10 = *(_DWORD *)(a1 + 3368);
    if ( *(_DWORD *)(v10 + 2332) != 36
      || (unsigned __int8)sub_49C7E0(a1)
      || (v2 = sub_45D250(&off_718820, v10, 28, L"[DIALOG_WARNING]", L"[DIALOG_BUTTON_YES]", (int)L"[REPICK_BUTTON]", 1),
          v2 == 1000) )
    {
      if ( (v11 = *(_DWORD *)(a1 + 3368), *(_DWORD *)(v11 + 2332) != 40)
        || (unsigned __int8)sub_49C7E0(a1) && (unsigned __int8)sub_49C7E0(a1) && (unsigned __int8)sub_49C7E0(a1)
        || (v2 = sub_45D250(
                   &off_718868,
                   v11,
                   28,
                   L"[DIALOG_WARNING]",
                   L"[DIALOG_BUTTON_YES]",
                   (int)L"[REPICK_BUTTON]",
                   1),
            v2 == 1000) )
      {
        if ( !(unsigned __int8)sub_49AEB0()
          || (unsigned __int8)sub_49AF00()
          || (unsigned __int8)sub_49C7E0(a1)
          || (unsigned __int8)sub_49C7E0(a1)
          || (unsigned __int8)sub_49C7E0(a1)
          || (LOBYTE(v2) = sub_49AE80(a1, &off_7188A4), (_BYTE)v2) )
        {
          LOBYTE(v2) = sub_49AF50(a1, 40);
          if ( (_BYTE)v2 )
          {
            LOBYTE(v2) = sub_49AF50(a1, 44);
            if ( (_BYTE)v2 )
            {
              LOBYTE(v2) = sub_49AF50(a1, 41);
              if ( (_BYTE)v2 )
              {
                LOBYTE(v2) = sub_49AF50(a1, 46);
                if ( (_BYTE)v2 )
                {
                  LOBYTE(v2) = sub_49AF50(a1, 47);
                  if ( (_BYTE)v2 )
                  {
                    LOBYTE(v2) = sub_49AF50(a1, 45);
                    if ( (_BYTE)v2 )
                    {
                      LOBYTE(v2) = sub_49AF50(a1, 42);
                      if ( (_BYTE)v2 )
                      {
                        LOBYTE(v2) = sub_49AF50(a1, 43);
                        if ( (_BYTE)v2 )
                          LOBYTE(v2) = sub_49C820(a1);
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
  return v2;
}

