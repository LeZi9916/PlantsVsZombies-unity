# Adventure native wave fixture

`adventure-wave-native.json` contains all 50 boards at progress values 0, 1 and 2,
with application +0x868 enabled and application +0x9B0 zero. The original
`0x416860` eligibility initializer and full `0x4150C0` constructor were executed.
Only bounded random draws are replaced by zero tickets, retaining their exact
upper-bound sequence. The fixture records both the final special eligibility mask
and every wave's ordered numeric types. It does not prove all ticket paths or a
complete original game startup.

Regenerate the native fixture with Python and Unicorn 2.1.4:

```sh
python3 -B Tools/Tests/Modules/Spawn/GenerateAdventureWaveFixture.py \
  --origin /path/to/pvz-origin
```

The original directory must contain the hash-matched executable and its supplied
`analysis/ida/waves/pe_oracle.py` PE mapping helper. It remains read-only. The
regeneration command executed during this repair produced a byte-identical fixture.
The original executable, emulator dependencies and temporary outputs are excluded.

IDs 25 and 23 in the Boss table retain original forced ordering and cost even
though actual Boss spawning has a separate controller. Do not interpret an unused
plan-table difference as a demonstrated Boss gameplay bug. Separate Unity regressions
cover the explicit forced-Yeti flag and seeded replay-bowling counterexamples.
