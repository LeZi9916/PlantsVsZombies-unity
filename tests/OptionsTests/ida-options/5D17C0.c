_DWORD *__thiscall sub_5D17C0(_DWORD *this)
{
  int v1; // eax
  int v2; // eax
  _DWORD v4[2]; // [esp+0h] [ebp-4Ch] BYREF
  _DWORD *v5; // [esp+Ch] [ebp-40h]
  _DWORD *v6; // [esp+38h] [ebp-14h]
  _DWORD *v7; // [esp+3Ch] [ebp-10h]
  int v8; // [esp+48h] [ebp-4h]

  v5 = this;
  v7 = v4;
  v6 = v4;
  v1 = sub_605A50(v4[0]);
  v8 = 0;
  v2 = sub_5D2890(v4[1], v1);
  v5[5] = v2;
  v5[6] = 0;
  return v5;
}

