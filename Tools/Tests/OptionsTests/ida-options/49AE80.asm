0049AE80  mov     ecx, [eax+0D28h]
0049AE86  push    1; int
0049AE88  push    offset aRepickButton; "[REPICK_BUTTON]"
0049AE8D  push    offset aDialogButtonYe; "[DIALOG_BUTTON_YES]"
0049AE92  push    offset aDialogWarning; "[DIALOG_WARNING]"
0049AE97  push    1Ch; int
0049AE99  push    ecx; int
0049AE9A  mov     ecx, [esp+18h+Source]; Source
0049AE9E  call    sub_45D250
0049AEA3  cmp     eax, 3E8h
0049AEA8  setz    al
0049AEAB  retn    4
