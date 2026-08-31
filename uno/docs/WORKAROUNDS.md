# Uno workarounds

Every workaround in this fork is marked in source with the exact string
**`Uno Workaround #NNNNN`** and traces to a filed issue on `unoplatform/uno`.

```bash
grep -rn "Uno Workaround #" uno/
```

Each one is debt with a known expiry: when its issue ships, delete the workaround and the
row below. Nothing here is a permanent design choice — a growing table means we are
drifting, a shrinking one means the fork is converging on plain upstream.

## Open

| Issue | What it works around | Where |
|---|---|---|
| [#24284](https://github.com/unoplatform/uno/issues/24284) | `ResourceLoader` ignores MRT slash-path semantics and silently returns `""`, so 426 accessibility strings and every mode title are blank. An empty string also reaches `Enum.Parse` and throws during XAML load. | `build/ingest/Uno.Ingest.targets` (2 rules) |
| [#24288](https://github.com/unoplatform/uno/issues/24288) | `ApplicationView.GetApplicationViewIdForWindow` throws instead of returning a sentinel, aborting page load. | `build/ingest/Uno.Ingest.targets` |
| [#24293](https://github.com/unoplatform/uno/issues/24293) | `x:Name` inside a `Resources` dictionary generates no backing field, though generated code and upstream code-behind both reference it. | `adapters/ResourceNamedMembers.cs` |
| [#24294](https://github.com/unoplatform/uno/issues/24294) | Inherited DPs cannot be set from another XAML file on a control whose code-behind omits the base class — which upstream does everywhere. | `build/ingest/Uno.Ingest.targets` (base injection) |
| [#24295](https://github.com/unoplatform/uno/issues/24295) | `x:Bind` function binding with a `((TypeName))` cast argument emits an invalid cast. | `forked/Views/GraphingCalculator/EquationStylePanelControl.xaml` |
| [#24296](https://github.com/unoplatform/uno/issues/24296) | Multi-arg `DecimalFormatter` / `DateTimeFormatter` ctors leave internals null, so later member access NREs far from the cause. | `build/ingest/Uno.Ingest.targets` (2 rules) |
| [#24297](https://github.com/unoplatform/uno/issues/24297) | 7.0-dev packages (HotDesign, Toolkit, CommunityToolkit) reference `Uno` / `Uno.UI.Toolkit`, which Uno 7 no longer ships. | `app/Uno.Calculator/Uno.Calculator.csproj`, `adapters/SettingsControls.cs` |
| [#24298](https://github.com/unoplatform/uno/issues/24298) | Unimplemented WinUI members that throw on the app startup path (`ApplicationView.*`, `SizeHelper.FromDimensions`, `LaunchActivatedEventArgs.User`, `UserActivityRequestManager`, `DecimalFormatter.FormatUInt`, `RichEditTextDocument.GetMath`/`SetMath`). | `build/ingest/Uno.Ingest.targets` (7 rules), `adapters/RichEditMathCompat.cs` |
| [#24301](https://github.com/unoplatform/uno/issues/24301) | `BrushTransition` drops `SolidColorBrush.Opacity`, so brushes differing only by opacity paint opaque white for the whole transition. | `forked/App.xaml` (16 brushes as alpha-in-Color), `forked/Views/GraphingCalculator/EquationInputArea.xaml` |
| [#24302](https://github.com/unoplatform/uno/issues/24302) | `BackdropMaterial.ApplyToRootOrPageBackground` applies a transparent brush instead of the Mica fallback, so every translucent Fluent fill composites over black. | `forked/App.xaml` (`CalcPageBackdropBrush`), `forked/Views/MainPage.xaml` |

Also filed and awaiting fixes, but needing no workaround here:
[#24285](https://github.com/unoplatform/uno/issues/24285) (case-sensitive resource lookup),
[#24286](https://github.com/unoplatform/uno/issues/24286) (`JsonObject.TryParse` throws),
[#24287](https://github.com/unoplatform/uno/issues/24287) (`LanguageFontGroup` throws),
[#24299](https://github.com/unoplatform/uno/issues/24299) (MRT-qualified assets unresolved),
[#24300](https://github.com/unoplatform/uno/issues/24300) (system accent colour hardcoded).

## Fixed upstream

| Issue | What it was | Status |
|---|---|---|
| [#24290](https://github.com/unoplatform/uno/issues/24290) | `XamlCodeGenerator` emitted a markup-extension value verbatim as a C# identifier, breaking the build for MRT slash-paths. | Fixed in [#24291](https://github.com/unoplatform/uno/pull/24291) — workaround removed, 24 forked views deleted |
| [#24292](https://github.com/unoplatform/uno/issues/24292) | Hoisted `ApplyTo_*` methods referenced `__ResourceOwner_N` out of scope. | Fixed in [#24291](https://github.com/unoplatform/uno/pull/24291) — no workaround needed |

## Not Uno's fault

These are fork-owned by design and have no issue to wait on:

| What | Why | Where |
|---|---|---|
| `GraphControl` types-and-enums stub | The Uno Calculator does not ship the graphing calculator; the stub exists so upstream's four graphing ViewModels compile and `ApplicationViewModel.cs` stays byte-identical. | `interop/GraphControl.Shim/` |
| `TraceLogging` no-ops | The app ships no telemetry; the types exist so upstream's `TraceLogger.cs` compiles. | `adapters/TraceLogging.cs` |
| kernel32 P/Invokes | `LocalizationSettings` and `LocalizationStringUtil` call `ResolveLocaleName`, `GetLocaleInfoEx` and `FormatMessageW`, which resolve only on Windows - so `MainPage`'s constructor throws `DllNotFoundException` on wasm, Android, iOS, Linux and macOS. Every LCTYPE requested has a `CultureInfo` equivalent. | `adapters/Win32Locale.cs`, `build/ingest/Uno.Ingest.targets` (4 rules) |
| UWP → WinUI type moves | Genuine API differences — upstream's UWP sources would not compile against WinUI either. | `build/ingest/Uno.Ingest.targets` |
| `CoreWindow` keyboard hookup | WinUI 3 removed `CoreWindow`. Shortcuts need rehosting on `UIElement` events; that is fork work, not an Uno defect. | `build/ingest/Uno.Ingest.targets` |
| `Utils.` qualification in ViewModels | An artefact of ingesting upstream's two assemblies into one, which changes C# name resolution. | `build/ingest/Uno.Ingest.targets` |
