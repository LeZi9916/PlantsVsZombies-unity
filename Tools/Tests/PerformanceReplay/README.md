# Complete Player performance replays

Requires the exact Unity **7000.0.0a16** editor, Windows graphics hardware and
PowerShell 7. The source checkpoint is private local input and must never be
committed. Supply an ASCII work directory to avoid Unity 7 command-line Unicode
decoding issues. No run uses `-nographics` or changes the main project settings.

```powershell
./Tools/Tests/PerformanceReplay/Run-PerformanceReplay.ps1 `
  -SourceProject . -Checkpoint 'C:/private/checkpoint.dat' `
  -WorkingRoot C:/pvz-replay-candidate -Label candidate -Backend CoreCLR -Graphics Vulkan
```

Use a frozen baseline source project for `-SourceProject` to build the unchanged
implementation with the same replay helper, original artwork, generated atlases,
backend and graphics API. Before/after runs must execute sequentially without
concurrent import/build load. Reverse run order for paired trials. The wrapper
creates a fresh isolated project and refuses to overwrite an existing copy.

`-AdvanceTicks 6000` continues a saved stage boundary with the stored seed bank,
then advances the detached board using its native 0.01-second update. After normal
startup/audio and any candidate preparation finish, the complete normal host
warms for exactly 120 real frames by default with simulation time held at the
fixed checkpoint point. `-WarmupFrames` can request a larger explicitly reported
warm-up; formal acceptance runs use the default 120.
Recorder setup and its deliberate allocation positive control precede this
warm-up. Initial model/pool/resource evidence is collected after warm-up, then
`CounterBoundaryFrames=2` extra frozen frames clear evidence serialization from
complete-frame counters. Rendering capacity counters are read after those
boundaries. The measured wall-clock boundary precedes the first complete
measured frame; no live gameplay frame is discarded from the measured replay.
The replay yields one normal production Update after attaching the checkpoint,
so preparation observes the new world before its ready flag is accepted.
`PreparationGateAvailable` distinguishes a candidate gate from a baseline without
one, and `PreparationReadyAtMeasurement` must remain true at the first measured
boundary. A stale or lost readiness rejects the run.
It then resumes normal delta-time playback and records 900 real frames by default. Production input,
audio, Canvas, cameras and `Time.deltaTime` updates remain enabled. The generated
empty bootstrap scene permits `_smokeMode` to be set before the real host's Awake;
profiles, accounts and match storage are redirected to isolated paths.

Use `-AdvanceTicks 12000 -Frozen` for the same fixed dense board, or
`-DurationSeconds 300` for a five-minute lifetime observation. A live duration
run reloads the same private checkpoint on loss, victory or seed re-selection,
then runs normal candidate preparation again. It keeps every restore/preparation
frame and its GC in the raw samples. `Phase` identifies ready combat (0), restore
(1), preparation (2) and resource evidence collection (3); only ready playing
frames are eligible for a steady-state zero-allocation claim. `Cycles` records
pool and native/managed/resource context after preparation (`Boundary=0`) and
before each restore or measurement completion (`Boundary=1`). This preserves
the old world's ending pool counts before a restored world replaces it. Ending
snapshot work before a reload belongs to its already classified restore frame.
`CycleEvidenceTruncated` explicitly identifies a full bounded evidence array.
Frozen dense runs are separate rendering/lifetime scenarios.
`RecordedSeconds` is the wall-clock interval from the boundary before the first
complete measured frame to the boundary reading the final sample. It is not a
sum of Unity frame deltas. Duration runs fail if their preallocated sample array
fills before the requested interval has elapsed.

Hidden Players render the complete camera and Screen Space Camera Canvas into a
1920x1080 RenderTexture through the production render pipeline after normal
LateUpdate. `frame.png` must contain actual nonuniform opaque GPU pixels, and
production Present/draw counters must be nonzero in at least half the frames;
otherwise the run fails. This is explicit offscreen Player evidence and its wall
frame interval is not an on-screen swapchain FPS guarantee.
The replay alone uses VSync 0 and a target of 60 because hidden offscreen targets
have no presentation interval to honor VSync; the main project's quality settings
remain unchanged. Frozen before/after workloads have identical model starting
points. Live real-time runs can advance different total simulation durations when
one implementation is slower; record their population/time context and compare
fixed-duration or frozen workloads separately.

The complete `replay.json` contains raw frame interval, complete-frame GC bytes,
inclusive production Update/Present markers, reported GPU frame timings, draw
calls and populations. It also records original input hashes, model hashes,
available pool counters and actor inspection component counts at both ends.
The rendering counters are bound once through typed getters; absent
baseline counters use -1. Initial values precede measurement so growth in the
first observed frame is detectable. `InstancingGpuBytes` is the logical capacity
of owned instance buffers. It does not include driver overhead or other graphics
resources. Each cycle's `GraphicsDriverAllocatedBytes` separately reads Unity's
`Profiler.GetAllocatedMemoryForGraphicsDriver` counter; nonpositive unavailable
readings become -1. This whole-driver counter is not dedicated physical VRAM
usage or an isolated instance-buffer allocation measurement.
`GpuBufferCreationCount` is the explicit sum of structured-buffer constructions
and mesh vertex/index layout reconfigurations, not a count of unknown
driver-internal objects. `InstanceBufferCreationCount`,
`MeshBufferReconfigurationCount` and `MeshCreationCount` expose their distinct
events. `MeshUploadBytes` is cumulative actual vertex/index upload bytes; use
its initial value and frame differences instead of summing cumulative samples.
`InstanceSubmissionBindingCreationCount` tracks immutable material/property-block
pairs, while `InstanceSameFrameBufferFallbackCount` identifies scene fallback
that protects data already submitted earlier in the same Unity frame. Their
initial values and frame samples distinguish preparation reuse from accidental
binding growth or hidden mesh fallback.
GPU values of -1 mean unavailable. FrameTimingManager GPU values must not be
interpreted as an isolated render-pass duration. Marker times are nested and
must not be summed together. Model/population records read immediately after the
previous completed profiler frame and can be displaced by one normal update;
they are context, not a deterministic per-tick state comparison.
For live observations the coroutine resumes after the current production Update,
while profiler recorders report the preceding completed frame. The first raw
counter may still describe the transition from the frozen boundary, and the final
current Update is not a separately appended counter sample. Keep the complete
original window, including every event and spike. A 900-sample live observation
does not establish exactly 900 native 0.01-second simulation updates or an exact
sample-by-sample model correspondence; use the deterministic Editor fixture for
that comparison. Frozen rendering observations retain the identical model state.
`-NativeProfiler` additionally enables native CPU/GPU profiling for a short probe,
limited to at most 900 frames and never a duration stress run. Some Vulkan/headless
driver combinations still expose no GPU timestamp data; zero GPU samples means
GPU timing remains unverified even when rendered pixels and CPU counters succeed.
Short native probes request `replay.raw` with allocation call stacks, strictly
from `ProfilerCaptureStartFrame` through
`ProfilerCaptureEndFrame`; warm-up and post-capture evidence serialization are
outside the binary-log interval. `MainThreadGcBytes` independently observes the
current thread between replay boundaries and is used only when its own 32 KiB
positive control passes. It may differ from Unity's completed-frame counter
because the boundaries differ and other threads allocate. Keep both raw fields;
do not subtract the main-thread value from the whole-frame value to invent an
exact background-thread total.
The binary log is enabled before a fresh profiler session starts. After disabling the
profiler at the final sample boundary, one frozen unmeasured frame lets the writer
flush before setting `Profiler.logFile` to the empty string, explicitly closing
its file writer as required by the Unity 7000 API example, and disabling its
binary sink. This diagnostic-only flush frame adds no
measured sample and is never used by ordinary timing or duration runs.
In the tested Unity 7000.0.0a16 Windows CoreCLR Player, these direct binary files
still failed `ProfilerDriver.LoadProfile`, including ASCII paths and explicit
writer closure. A file's existence is not parsed allocation evidence. The verified
alternative is a live same-version Editor connection: start the Development
Player with `-profiler-enable`, connect `ProfilerDriver.DirectIPConnect` to its
advertised local port, set `memoryRecordMode=ProfilerMemoryRecordMode.GCAlloc`,
receive complete native frames, then use `ProfilerDriver.SaveProfile` and inspect
all-thread raw frame views. Editor profiler frame indices differ from Player
`Time.frameCount`; retain setup/completion frames and establish measurement
boundaries before attributing any allocation. Live profiler runs and extended
connection warm-ups are diagnostic observations, never formal timing trials.

The current-thread allocation recorder must detect a deliberate 32 KiB array;
the whole-frame byte counter must also report it before measurement starts. A
failed positive control rejects the allocation evidence instead of claiming
zero allocation. Report setup, JSON serialization, reflection for optional
baseline pool statistics and object enumeration occur outside the measured
frame window. The sampling loop stores value records in one preallocated array.

The replay runtime is compiled only with `PVZ_PERFORMANCE_REPLAY`. The editor
entry adds that define and `ENABLE_PROFILER` through `BuildPlayerOptions.extraScriptingDefines` and
only accepts projects carrying the explicit `.pvz-replay-isolated` marker. CoreCLR
and IL2CPP, Vulkan and D3D11 are independent validation targets. A successful
build is not runtime verification; report unavailable toolchains or failed
replays explicitly. Normal Players contain no replay controller or samplers.
Unity 7 uses the explicit CoreCLR backend. The legacy `Mono` wrapper argument is
accepted as a CoreCLR alias; it never claims traditional Mono verification. The
obsolete `Mono2x` editor selection can be ignored after an IL2CPP build, so the
builder selects `ScriptingImplementation.CoreCLR` directly and checks the setting.
The report and wrapper also check `ActualScriptingBackend` in the built Player.
