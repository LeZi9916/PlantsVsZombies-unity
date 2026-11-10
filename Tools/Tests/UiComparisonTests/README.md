# UI screenshot comparison

Run `pwsh -File Tools/Tests/UiComparisonTests/Run.ps1` on Windows to check equality,
RGBA error metrics, difference bounds, alpha handling, output protection and
dimension mismatch handling. Test images are written to a unique `artifacts/`
subdirectory.

For a real comparison, capture the original executable and Unity player at the
same viewport size, scene, state, pointer position and animation frame, then:

```powershell
./Tools/CompareUiScreenshots.ps1 -Reference original.png -Candidate unity.png `
  -DifferenceImage diff.png -RequireIdentical
```

This compares decoded RGBA channels, including alpha; it does not align images,
mask regions, or compensate for timing, scaling, color profiles or rendering
backend differences. The difference PNG multiplies RGB error by four by default;
alpha error contributes equally to all three displayed channels. A zero result
only applies to the two supplied frames, not to all UI states or interactions.
The tool refuses to overwrite an existing difference image.
