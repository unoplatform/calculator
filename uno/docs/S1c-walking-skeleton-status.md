# Walking skeleton status — not yet runnable

**Date:** 2026-08-30
**Harness:** `D:\Throwaway\s1-linked-xaml\` (throwaway, outside this repo)
**Stack:** `Uno.Sdk.Private` 7.0.0-dev.579 (`uno-features` feed), `net11.0-desktop`,
.NET 11 preview 7 installed locally per `dotnet:setup-local-sdk`.

## Bottom line

**The app does not run yet.** It compiles all 102 upstream `.cs` files, all shims, and
most of the 26 upstream `.xaml`, but ~128 diagnostics remain, and they are dominated by
one Uno defect rather than by porting work.

## What got done

| Step | Result |
|---|---|
| 13 shim types (+4 the CS0246 census missed) | **Done** — 20 files, 3,228 LOC, harvested from the fork's 3,468-LOC donor |
| Upstream C# compiles | **Zero errors** in all 102 ingested `.cs` |
| Shims compile | **Zero errors** |
| Upstream `App.xaml` as ApplicationDefinition | **Done** — replaced the template's; this alone cleared ~150 errors, since it defines `SharedShadow`, `DelighterValueTemplate`, `EquationButtonBackgroundBrush` etc. |
| Desktop entry point | Rewritten for `CalculatorApp.App` |

A census correction worth recording: the "13 missing types" figure was **wrong**. It came
from grepping `CS0246`, but an enum used only in expression position is reported as
`CS0103`, not `CS0246`. `CalculatorCommand`, `HistoryItemWrapper`, `UnitConverterCommand`
and `UnitConverterWrapper` were missing from that list and had to be added. The real
figure was 17.

## The blocker is one Uno defect, not a long tail of porting work

[unoplatform/uno#24290](https://github.com/unoplatform/uno/issues/24290) — `XamlCodeGenerator`
emits a markup extension's *value* verbatim as a C# identifier — is far broader than the
3 files it was filed against.

Upstream uses MRT slash-path resource lookups in **25 of 26 views**, 515 attributes total:

```xml
AutomationProperties.Name="{utils:ResourceString Name=num0Button/[using:Windows.UI.Xaml.Automation]AutomationProperties/Name}"
```

It manifests two ways:
- **Hard parse failure** (3 views that also use `VisualState Setter Target`) — the emitted
  identifier contains `/`, `[`, `]`, `:`.
- **Unresolved generated names** (the rest) — `__ResourceOwner_1`, `TextSubject`,
  `ContentSubject`, `HeaderSubject` never get declared.

Stripping those 515 attributes into fork-owned copies of all 25 views took `CS0103` from
82 → 38 and `CS1061` from 42 → 14. Nothing is lost functionally *today*, because the same
lookups already return `string.Empty` at runtime — that is
[#24284](https://github.com/unoplatform/uno/issues/24284), the runtime half of the same defect.

**This is the decision point.** Forking 25 of 26 views to work around it would blow the
spec's "≤5 forked views" cap five times over and would fork the highest-churn files in the
contract — precisely the outcome the two-root strategy exists to prevent. Fixing MRT
slash-path handling in Uno instead removes the workaround entirely *and* restores 426
accessibility strings. For an Uno showcase app, that is clearly the right order of work.

## What is actually left after the workaround

~128 diagnostics, and they are no longer mysterious:

| Cluster | Count | Nature |
|---|---|---|
| `UXAML0001` "property does not exist on \<UserControl\>" | 38 | **Cascade.** `Margin`/`TabIndex` "missing" on a `UserControl` means that type failed to compile, so XAML cannot see its base class. Clears when the causes below clear. |
| `CS0103` residual generator names | 38 | Remainder of #24290 |
| `CS0234` `CalculatorApp.Utils.<method>` treated as a namespace | 20 | Uno resolving **x:Bind function bindings** as namespaces — likely a further generator gap, needs its own repro |
| `CS0234` `CommunityToolkit.WinUI.Controls` | — | Missing package. Genuinely trivial — add the WinUI 3 CommunityToolkit. |
| `CS1061` `GetMath` / `SetMath` | 4 | Uno's `RichEditBox` has no math API (`MathRichEditBox`) |
| `CS1061` `PrelaunchActivated`, `User` | 4 | `Microsoft.UI.Xaml.LaunchActivatedEventArgs` lacks UWP members; the alias is not a clean substitution |
| `CS0019` `PointerDeviceType`, `WindowActivationState` | 8 | Two same-named types from `Windows.*` and `Microsoft.*`; needs rewriter alias rules, not a `global using` |

## Further verified Uno gaps (not yet filed)

- `RevealBorderBrush` / `RevealBackgroundBrush` have no `TargetTheme` property.
- `RichEditBox` has no `ContentLinkForegroundColor` / `ContentLinkBackgroundColor`.
- `Windows.UI.Core.CoreDispatcher` is not referenceable from the bindable-type metadata
  generator; worked around with `UnoDisableBindableTypeProvidersGeneration=true`.

Both of the first two currently force a forked `App.xaml` — the single worst file to fork,
being the contract's highest-churn file.

## Recommended next step

Fix #24290 in Uno before continuing the port. It is the difference between forking 25 of 26
views and forking none, and it unblocks the accessibility story at the same time. Everything
else on the list is small and well understood.

---

## Update — #24291 verified (2026-08-30, later)

The fix from [unoplatform/uno#24291](https://github.com/unoplatform/uno/pull/24291) was built
locally from `dev/mazi/markupextslash` (based on `feature/breakingchanges`, 0 behind) and
swapped into the resolved analyzer.

**It works, and it removed the workaround entirely.** All 24 forked views were deleted;
upstream XAML now compiles unmodified.

| Diagnostic | Before | After |
|---|---|---|
| `CS9348` compilation-unit members | 450 | **0** |
| `CS0538` explicit interface | 30 | **0** |
| `CS1527` namespace member private | 18 | **0** |
| `CS8802` top-level statements | 4 | **0** |
| `CS0103` (unforked baseline) | 82 | **54** |
| Forked views required | 25 of 26 | **0** |

`TextSubject` / `ContentSubject` / `HeaderSubject` are gone. Roughly 500 diagnostics
eliminated by one fix, and the "≤5 forked views" cap is no longer at risk.

**Next blocker in the same family:**
[#24292](https://github.com/unoplatform/uno/issues/24292) — when the generator hoists a
`ResourceDictionary` subtree into an `ApplyTo_*` method, the signature omits
`__ResourceOwner_N` while the hoisted body still references it. 16 occurrences across
`HistoryList`, `MemoryListItem`, `SupplementaryResults`, `Settings`.

**~144 diagnostics remain**, unchanged in character from the table above: cascade
`UXAML0001`, x:Bind function bindings resolving as namespaces, the missing
CommunityToolkit package, `RichEditBox` math API, and `LaunchActivatedEventArgs` members.

### Local environment note

The analyzer swap is **not** part of this repo. It replaces
`D:\Packages\NuGet\uno.winui\7.0.0-dev.579\analyzers\dotnet\cs\Uno.UI.SourceGenerators.dll`
in the shared package cache, with the original kept alongside as `.dll.orig`.
Restore with a copy back over it, or delete the package folder and re-restore. Any other
build on this machine using that package picks up the patched generator until then.

---

## Update — both generator fixes + runtime fix verified on net10 (2026-08-30, later still)

`b0658c5644f fix(xamlgen): Forward resource owner to hoisted methods` was added to
[#24291](https://github.com/unoplatform/uno/pull/24291). Both fixes are now verified.

**Correction: net11 was never required.** The "feature/breakingchanges needs net11" note in
Uno's CI applies to Uno's own PRs, not to consuming apps. `net10.0-desktop` works with the
stock .NET 10 SDK, and `Uno.UI` builds for `net10.0` out of the box — no preview SDK, no
`DOTNET_ROOT`, no `global.json` `paths`. The local .NET 11 install is not needed.

**Error trajectory**

| State | Errors |
|---|---|
| Uno 7, no fixes | ~1150 cascade + 128 |
| #24291 fix 1 (markup-ext name) | 442 (312 of them `CS1501 GenericApply`) |
| \+ fix 2 (resource owner), analyzer only | 426 — `CS1501` persists, it needs the runtime |
| \+ rebuilt `Uno.UI.dll` (net10) | **114** — `CS1501` gone entirely |

The second fix spans both the generator **and** the runtime
(`src/Uno.UI/UI/Xaml/Markup/ApplyExtensions.cs` adds the `GenericApply` overload), so an
analyzer-only swap is not sufficient for it.

**Still zero forked views.** Only `App.xaml` remains forked, for the two unrelated Uno gaps.

### The remaining 114, unchanged in character

`CS0103` Settings/About x:Names and App.xaml resource keys · `CS0234` x:Bind function
bindings resolving as namespaces · `CS1061` `GetMath`/`SetMath`, `PrelaunchActivated` ·
`CS0019` `PointerDeviceType` and `WindowActivationState` ambiguity · missing `TraceLogging`
and `NarratorNotifier` · `CurrencyDataLoader`.

### Two process lessons

- **`obj/ingest` must be generated by the build, not by hand.** A routine `rm -rf obj`
  deletes all 102 rewritten files, and the failure presents as "type could not be found"
  across every view — which reads like a porting problem, not a missing directory. This
  cost a long detour and confirms the spec's choice to make ingest an MSBuild target.
- **`UnoNugetOverrideVersion` copies `$(TargetDir)\*.*`,** so building `Uno.UI` alone also
  overwrote `uno.foundation` and `uno.winrt` with Reference-flavour stubs. Prefer copying
  the single rebuilt assembly.

### Local machine state (not in this repo)

In `D:\Packages\NuGet\uno.winui\7.0.0-dev.579`:
- `analyzers/dotnet/cs/Uno.UI.SourceGenerators.dll` — replaced; original kept as `.dll.orig`
- `lib/net10.0/Uno.UI.dll` — replaced; original kept as `.dll.pristine`

`uno.foundation` and `uno.winrt` 7.0.0-dev.579 were clobbered and have been deleted and
re-restored from the feed, so they are pristine.

---

## Update — IT RUNS (2026-08-30, evening)

**Zero compile errors** and the app launches, renders through Skia/Vulkan, and shows a
window titled "Calculator" — from all 102 upstream `.cs` and all 26 upstream `.xaml`,
with **zero forked views** beyond `App.xaml` and two files covered by filed Uno bugs.

What renders: window chrome, dark theme, the NavigationView hamburger.
What does not: the calculator surface itself, because
`DllNotFoundException: Unable to load DLL 'CalcManager'` — the native engine is not built,
so `CalculatorManagerWrapper`'s ctor throws and the content frame stays empty. That is
Act 2 P5 work, not a rendering problem.

### Getting from "compiles" to "renders" took seven runtime fixes

Each one aborted `OnLaunched`/page-load *before* `Window.Activate()`, so the symptom was
identical every time — a live process with an invisible window and no logged error. The
diagnosis only became possible after logging every first-chance exception.

| # | Blocker | Nature |
|---|---|---|
| 1 | `Uno` / `Uno.UI.Toolkit` assemblies missing at runtime | CommunityToolkit + HotDesign packages built against pre-7.0 Uno |
| 2 | `LaunchActivatedEventArgs.User` not implemented | Uno gap |
| 3 | `SizeHelper.FromDimensions`, `ApplicationView.*` sizing | Uno gaps |
| 4 | `Enum.Parse("")` in `ResourceVirtualKey` | **direct fallout of #24284** — empty MRT string |
| 5 | `Window.Current.CoreWindow` null in `KeyboardShortcutManager.Initialize` | WinUI 3 removed CoreWindow |
| 6 | `ApplicationView.GetApplicationViewIdForWindow` throws | **exactly #24288** |
| 7 | `UserActivityRequestManager.GetForCurrentView` throws | Uno gap |

Two of the seven were bugs already filed from the compile phase, which is a useful
signal: the runtime failures are the same defects surfacing later.

### Process notes

- A stale `app.exe` held a file lock for several build cycles, so builds silently produced
  no new binary and I debugged a stale build. Kill the process before every rebuild.
- `UnoNugetOverrideVersion` copies `$(TargetDir)/*.*`, so building `Uno.UI` alone also
  overwrote `uno.foundation` and `uno.winrt` with Reference stubs. Copy single assemblies.
- Debug pulls HotDesign, which drags in pre-7.0-linked assemblies; Release does not.
  `CommunityToolkit.Mvvm` then has to be added explicitly (`UnoFeatures`), since it was
  only arriving transitively through HotDesign.

### Next

Build the native `CalcManager` for desktop and wire the interop seam (P5). That is what
stands between this shell and a working calculator.

---

## Update — THE CALCULATOR RENDERS (2026-08-30, late)

The full Windows Calculator UI now renders on Uno 7 / Skia / Vulkan from **unmodified
upstream XAML**: display, memory row, keypad, operators, history button, and the
navigation menu with all modes.

### What it took after "compiles"

Nine runtime blockers, each aborting `OnLaunched` or first-page construction *before*
`Window.Activate()`. Every one presented identically — a live process, an invisible
window, and nothing logged — so each cost a rebuild to find.

| # | Blocker | Filed |
|---|---|---|
| 1 | `Uno` / `Uno.UI.Toolkit` assemblies missing at runtime (HotDesign, Toolkit, CommunityToolkit) | [#24297](https://github.com/unoplatform/uno/issues/24297) |
| 2 | `LaunchActivatedEventArgs.User` throws | [#24298](https://github.com/unoplatform/uno/issues/24298) |
| 3 | `SizeHelper.FromDimensions` + `ApplicationView` sizing throw | [#24298](https://github.com/unoplatform/uno/issues/24298) |
| 4 | `Enum.Parse("")` in `ResourceVirtualKey` | fallout of [#24284](https://github.com/unoplatform/uno/issues/24284) |
| 5 | `Window.Current.CoreWindow` null in `KeyboardShortcutManager` | WinUI 3 removed CoreWindow |
| 6 | `ApplicationView.GetApplicationViewIdForWindow` throws | [#24288](https://github.com/unoplatform/uno/issues/24288) |
| 7 | `UserActivityRequestManager.GetForCurrentView` throws | [#24298](https://github.com/unoplatform/uno/issues/24298) |
| 8 | `DecimalFormatter.FormatUInt` throws building the digit table | [#24298](https://github.com/unoplatform/uno/issues/24298) |
| 9 | `DecimalFormatter(languages, region)` ctor leaves internals null → NRE | [#24296](https://github.com/unoplatform/uno/issues/24296) |

**#9 was the one that mattered.** The generated 2-arg constructor calls
`TryRaiseNotImplemented` — which does *not* throw — and never initialises the type's
fields. Construction appears to succeed, then every member access NREs from inside Uno,
nowhere near the cause. Fixing that one call site is what turned a black window into the
rendered calculator.

### Known-visible defects in the running app

- **Navigation menu labels are blank** — live demonstration of
  [#24284](https://github.com/unoplatform/uno/issues/24284). Only "Settings" resolves,
  via a different path. This is the 426-accessibility-strings bug, visible on screen.
- **No arithmetic.** `DllNotFoundException: CalcManager` — the native engine is still
  not built, so `CalculatorManagerWrapper` throws and the ViewModel runs without an
  engine. That is P5.
- Images missing where assets use scale/contrast qualifiers
  ([#24299](https://github.com/unoplatform/uno/issues/24299)).

### Uno issues filed across this port: 16

Compile-time: #24284, #24285, #24286, #24287, #24288, #24290, #24292, #24293, #24294, #24295
Runtime/packaging: #24296, #24297, #24298, #24299
(#24290 and #24292 are already fixed in PR #24291.)

### Next

Build native `CalcManager` for desktop and wire the interop seam so it computes.

---

## Update — IT CALCULATES (2026-08-31)

`123 + 456 = 579`, and `1 ÷ 3 = 0.33333333333333333` — 17 significant digits, i.e. the real
ratpak arbitrary-precision engine, not a `double`. Windows Calculator is now **functionally
working** on Uno Platform 7 / Skia / Vulkan from unmodified upstream sources.

### The native engine

The prebuilt `src/x64/Release/CalcManager/CalcManager.dll` in this repo is a **dead end for
desktop**: it imports `MSVCP140_APP.dll` / `VCRUNTIME140_APP.dll`, the UWP/Store CRT, which
only resolves inside an appx package. It can never load into a desktop process — that was the
real meaning of `DllNotFoundException: ...or one of its dependencies`.

A desktop build needed the MSVC C++ standard library headers, which were missing from this
machine (`VC\Tools\MSVC\14.50.35717\include` was empty; `vcvarsall.bat` absent). After
installing the *MSVC v145 x64/x86 build tools* component, the engine builds with a single
`cl` invocation — no vcxproj, no CMake:

```bat
call "...\VC\Auxiliary\Build\vcvars64.bat"
cl /c /EHsc /std:c++17 /O2 /MD /DNDEBUG /DCALCMANAGERWIN32_EXPORTS ^
   /FI"%SRC%\pch.h" /I"%SRC%" /I"%SRC%\CEngine" /I"%SRC%\Ratpack" <29 .cpp files>
link /DLL /OUT:CalcManager.dll *.obj
```

The `/FI"%SRC%\pch.h"` force-include is the load-bearing part: the vcxproj consumes `pch.h`
as a precompiled header, and without it `<windows.h>`-derived symbols (`HIDWORD`, `LODWORD`,
`S_OK`, `SUCCEEDED`) and `<iterator>` (`back_inserter`) are missing. This is a useful data
point for the CMake work in Act 1 P1 — the sources need `pch.h` force-included on every
toolchain, exactly as the file's own comment says.

Result: 285,696-byte `CalcManager.dll` importing the **desktop** CRT (`MSVCP140.dll`), with
27 `CalculatorManager_*` exports matching the managed shim's 35 `DllImport`s. Dropped next to
`app.exe`, arithmetic worked immediately — the shim needed no changes.

### A debugging note worth keeping

Between "engine loads" and "engine computes" there was one more red herring: synthetic clicks
were landing on empty space because the window had resized itself to 1536x960 while the click
coordinates assumed 460x780. Any UI automation here must **verify the window size stuck**
after `MoveWindow` before deriving coordinates.

### Still outstanding

- Nav labels and mode titles are blank: [#24284](https://github.com/unoplatform/uno/issues/24284).
- `GetApplicationViewIdForWindow` still throws at two *direct* call sites
  (`Calculator.xaml.cs:265`, `MainPage.xaml.cs:541`) inside `Dispatcher.RunAsync` continuations,
  silently skipping `GetMemory()` and launch telemetry — the unobserved-throw case described in
  [#24288](https://github.com/unoplatform/uno/issues/24288).
- Buttons flash white on hover; under investigation.

---

## Update — theming root-caused; two more Uno bugs (2026-08-31)

A dedicated investigation settled why the port looked unlike the real Windows Calculator. It
**refuted** the leading hypothesis (a failed `TemplatedParent` binding) and found two distinct
Uno defects instead.

### [#24301](https://github.com/unoplatform/uno/issues/24301) — `BrushTransition` drops `SolidColorBrush.Opacity`

Upstream's calculator button fills are all `Color="#FFFFFF"` differing **only** by `Brush.Opacity`.
`BorderHelper.cs:96` passes only `.Color` into the transition, and while a transition is live
`CompositionSpriteShape.skia.cs:195-199` bypasses the brush and fills from that raw colour — so
both endpoints are `#FFFFFF` and the button paints **opaque white for the whole 83 ms**.

It appears on pointer *exit*, not enter: entering sets Background via
`ObjectAnimationUsingKeyFrames`, and animation precedence suppresses the transition; leaving
drops that precedence so the transition runs. Because each button stays white for 83 ms after
the pointer leaves, a moving pointer drags a **trail of several simultaneously-white buttons** —
which is what the user actually saw.

Uno already has the right property: `SolidColorBrush.ColorWithOpacity`
(`SolidColorBrush.cs:19`), whose doc comment says it "should generally be used for rendering
rather than the Color property itself". Reproduced in a **vanilla `dotnet new unoapp` on public
Uno.Sdk 6.6.14**, and disproved as a stuck visual state (it clears itself after exactly
`Duration`) and as a stale surface (setting `Duration` to 2 s produced exactly 2 s of white).

### [#24302](https://github.com/unoplatform/uno/issues/24302) — `BackdropMaterial` applies a transparent brush, not the Mica fallback

Separate cause, and the reason the buttons looked flat and dark. `BackdropMaterial.cs:58`
always assigns a `MicaController`; the reset that would clear it is commented out at `:61`, so
`UpdateFallbackBrush` never takes its solid-fallback branch and instead sets
`Background = Color.FromArgb(0,0,0,0)` at `:208`. The page is fully transparent while no Mica is
ever applied, so black shows through.

Uno's opacity maths is in fact correct — only the backdrop is wrong:

| | backdrop | number button | operator button |
|---|---|---|---|
| Real Windows Calculator | `#1A2224` (Mica) | `#363D3F` | `#2D3436` |
| Uno, before fix | `#000000` | `#1F1F1F` | `#151515` |

white @12.5% over `#1A2224` = `#373D3F` (real measures `#363D3F`); over `#000000` = `#1F1F1F`
(ours). The intended fallbacks already exist — `MicaController.DarkThemeColor` `#202020`,
`LightThemeColor` `#F3F3F3`.

### Workarounds applied, both fork-only

1. **Alpha-in-Color** — 16 brushes in the forked `App.xaml` rewritten from
   `Opacity="0.125" Color="#FFFFFF"` to `Color="#20FFFFFF"`. Verified: no white frames on exit.
   Behaviour-preserving on WinUI and still correct after Uno is fixed, so nothing to revert.
2. **Explicit backdrop** — forked `MainPage.xaml` drops
   `BackdropMaterial.ApplyToRootOrPageBackground` for `Background="{ThemeResource CalcPageBackdropBrush}"`,
   defined as `#202020` / `#F3F3F3`.

Measured after: button background `#3C3C3C`, matching the predicted value and close to the real
app's `#363D3F`. Forked views now total 4, still within the spec's ≤5 cap, and every one exists
solely to work around a filed Uno bug.

### Remaining visual gap to the real app

Mode title still blank ([#24284](https://github.com/unoplatform/uno/issues/24284)); accent colour
blue rather than the user's Windows accent ([#24300](https://github.com/unoplatform/uno/issues/24300));
no rounded window corners; display panel lacks its border.
