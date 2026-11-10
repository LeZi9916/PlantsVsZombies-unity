bool __userpurge sub_49AE80@<al>(int a1@<eax>, void *Source)
{
  return sub_45D250(
           Source,
           *(_DWORD *)(a1 + 3368),
           28,
           L"[DIALOG_WARNING]",
           L"[DIALOG_BUTTON_YES]",
           (int)L"[REPICK_BUTTON]",
           1) == 1000;
}

