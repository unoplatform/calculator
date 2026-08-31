# Act 0 spikes

Timeboxed experiments that gate the Two-Root Re-Fork. See
[`../superpowers/specs/2026-08-30-two-root-refork.md`](../superpowers/specs/2026-08-30-two-root-refork.md)
for the strategy,
[`../superpowers/plans/2026-08-30-act0-spikes-and-hygiene.md`](../superpowers/plans/2026-08-30-act0-spikes-and-hygiene.md)
for the plan, and
[`../decisions/2026-08-30-target-uno-7.md`](../decisions/2026-08-30-target-uno-7.md)
for the Uno 7 retarget that supersedes the spec's SDK/TFM choices.

Spike code lives in `D:\Throwaway\`, never in this repo. Only verdicts are committed.

A spike that **fails is a successful spike**. Failures are recorded, not fixed.

| # | Question | Status | Verdict |
|---|---|---|---|
| [S1](S1-linked-xaml.md) | Does linked upstream XAML pair with rewritten code-behind in `obj/ingest`? | **Done** | **PASS** on both Uno 6 and Uno 7, verified in generated code |
| S1b | Do the 426 slash-path MRT accessibility strings resolve on Uno? | **Open** | Cause confirmed in Uno source; runtime probe still blocked behind the interop shim |
| S2 | Do upstream's 283 MSTest run headless with no display server? | **Open** | Not started |
| S3 | Can `concurrency::task<T>` be completed from C# across the flat C ABI on single-threaded WASM? | **Deferred** | Needs an Emscripten SDK matched to the .NET SDK's Emscripten pack |
| S4 | Do AutomationIds surface through the platform accessibility tree on Skia? | **Open, raised priority** | Uno 7 drops native targets, so the `-renderer native` fallback is gone |
| S5 | Does CMake/emcc `CalcManager.a` link into Uno.Wasm.Bootstrap? | **Deferred** | Needs Emscripten + Android NDK + Xcode |
| S6 | Live store version numbers, staffing, showcase-banner requirement | **Open** | Human task — see [`../RELEASE-BASELINE.md`](../RELEASE-BASELINE.md) |

## Standing conclusion

**The architecture is sound and the evidence is now strong.** A full-glob ingest of all
102 upstream `.cs` and 26 `.xaml` files against Uno 7 leaves **186 errors from three
causes**, two of which are components the spec already plans to build (the
`CalcManager.Interop` seam and the `GraphControl` shim). The other two are a one-line
exclusion and a one-line alias. Nothing unexpected has surfaced.

**Targeting Uno 7 improved the position rather than complicating it.** BC26 retired the
only blocker S1 had found, before any code was written to work around it.

**S6 still gates everything.** No amount of green spikes substitutes for a staffing
commitment against a repository with zero commits in 2026.

## Upstream Uno issues filed from this work

| Issue | Subject |
|---|---|
| [#24284](https://github.com/unoplatform/uno/issues/24284) | `ResourceLoader.GetString` ignores MRT slash-path semantics, silently returns empty |
| [#24285](https://github.com/unoplatform/uno/issues/24285) | ResourceLoader resource-key lookup is case-sensitive; MRT keys are case-insensitive |
| [#24286](https://github.com/unoplatform/uno/issues/24286) | `Windows.Data.Json.JsonObject.TryParse` throws instead of returning `false` |
| [#24287](https://github.com/unoplatform/uno/issues/24287) | `Windows.Globalization.Fonts.LanguageFontGroup` throws on every property |
| [#24288](https://github.com/unoplatform/uno/issues/24288) | `ApplicationView.GetApplicationViewIdForWindow` throws; a sentinel would let WinUI code run unmodified |

All five verified against `feature/breakingchanges` before filing. A sixth candidate — Uno
requiring `partial` on `DependencyObject` subclasses — was **not** filed: it is fixed by BC26
and already tracked as [#17099](https://github.com/unoplatform/uno/issues/17099).

Each of these that lands deletes a permanent override from the fork's ledger, so they are the
mechanism by which the fork shrinks over time.
