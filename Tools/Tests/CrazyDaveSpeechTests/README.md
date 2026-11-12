# Crazy Dave speech tests

Run the standalone regression checks with .NET 9:

```powershell
dotnet run --project Tools/Tests/CrazyDaveSpeechTests/CrazyDaveSpeechTests.csproj -c Release
```

The tests cover message length boundaries, original control-marker precedence, sound variants, silent messages, store item descriptions, and the original store speech timers. They do not exercise Unity rendering or audio playback.
