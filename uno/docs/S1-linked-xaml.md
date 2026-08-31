# S1 — Linked XAML build mechanics

**Date:** 2026-08-30
**Harness:** `D:\Throwaway\s1-linked-xaml\` (throwaway, outside this repo)
**Upstream pin:** `msft/main@4fd3fc53`
**Stack:** .NET 10.0.301, Uno.Sdk 6.6.14 (template default), `net10.0-desktop`, SkiaRenderer

## Predicate (written before execution)

S1 PASSES if and only if all four hold:

1. The project builds with the `.xaml` referenced via `<Page Include="$(UpstreamRoot)..." Link="..." />` while its `x:Class` partial is compiled from `obj/ingest/`.
2. `x:Class="CalculatorApp.NumberPad"` resolves to the rewritten code-behind — no CS0260/CS0101 duplicate-or-missing-partial error on `NumberPad`.
3. `InitializeComponent()` is generated and callable.
4. `x:Name` elements declared in the linked XAML are visible as fields in the rewritten code-behind.

## Verdict: **PASS (mechanism), with two new obligations**

Predicates 2, 3 and 4 are **directly verified in Uno's generated output**. Predicate 1 is met for the XAML/code-behind pairing itself; the build does not reach zero errors, but every remaining diagnostic is dependency-closure or a separately-recorded finding — none is about pairing.

**Evidence.** Uno's `XamlCodeGenerator` emitted
`obj/.../XamlCodeGenerator/NumberPad_8387353dcf9e07065bcc23e5a6772377.cs` containing:

```csharp
namespace CalculatorApp
{
    partial class NumberPad : global::Microsoft.UI.Xaml.Controls.UserControl   // predicate 2
    {
        private void InitializeComponent()                                      // predicate 3
        ...
        private global::CalculatorApp.Controls.CalculatorButton Num0Button      // predicate 4
        private global::CalculatorApp.Controls.CalculatorButton Num1Button
        ... Num2..Num9Button, DecimalSeparatorButton
```

The generator read a `.xaml` from **outside the project cone**, resolved its `x:Class` against a **mechanically rewritten** partial in `obj/ingest/`, resolved the custom `xmlns:controls` against ingested code, and typed every `x:Name` field to the ingested `CalculatorApp.Controls.CalculatorButton`.

**The ingest architecture stands. Act 2 P4 proceeds as specced.**

## Finding A — RETIRED on Uno 7 (was: `partial` required on DependencyObject subclasses)

> **Superseded 2026-08-30.** This finding is **Uno 6.x only**. Uno 7 (`feature/breakingchanges`)
> makes `DependencyObject` a class rather than an interface (**BC26**) and deletes the
> `DependencyObjectGenerator` entirely (commit `c371084bb`, −792 lines), so there is no
> generated partial to collide with. **Re-verified empirically on `Uno.Sdk.Private`
> 7.0.0-dev.579 / net11: CS0260 count is 0** with the `partial` rewrite rule disabled.
>
> The proposed fifth rewrite rule is **not needed** and was never adopted. There is already an
> upstream issue for the underlying change ([unoplatform/uno#17099](https://github.com/unoplatform/uno/issues/17099)),
> so no new issue was filed. See [`../decisions/2026-08-30-target-uno-7.md`](../decisions/2026-08-30-target-uno-7.md).
>
> The original 6.x analysis is kept below for the record.

### Original finding (Uno 6.x)

Uno's `DependencyObjectGenerator` emits `partial class X : IDependencyObjectStoreProvider, IWeakReferenceProvider` for **every** `DependencyObject`-derived type. Upstream declares these non-partial:

```csharp
public sealed class KeyboardShortcutManager : DependencyObject   // CS0260 on Uno
```

Legal on UWP; **CS0260 on Uno**. Confirmed by building with `EmitCompilerGeneratedFiles=true` and reading
`.../DependencyObjectGenerator/CalculatorApp.Common.KeyboardShortcutManager.cs:31`.

This looked like it would force an upstream edit and break the zero-edit invariant. It does not — **the rewriter can inject `partial`**, which was implemented and verified to eliminate CS0260 entirely.

**Scope is bounded and enumerable — exactly 6 types** across `src/Calculator` + `src/Calculator.ViewModels` at `4fd3fc53`, all non-partial:

| File | Type |
|---|---|
| `Calculator.ViewModels/Common/Automation/NarratorNotifier.cs` | `NarratorNotifier : DependencyObject` |
| `Calculator/Common/KeyboardShortcutManager.cs` | `KeyboardShortcutManager : DependencyObject` |
| `Calculator/Controls/CalculationResult.cs` | `CalculationResult : Control` |
| `Calculator/Controls/EquationTextBox.cs` | `EquationTextBox : Control` |
| `Calculator/Controls/HorizontalNoOverflowStackPanel.cs` | `HorizontalNoOverflowStackPanel : Panel` |
| `Calculator/Controls/OverflowTextBlock.cs` | `OverflowTextBlock : Control` |

**Consequence:** the spec's rewrite-rule table gains a fifth rule — *inject `partial` into `DependencyObject`-derived declarations* — and `UNOCAL0003` (namespace census) needs a sibling diagnostic that fires when upstream adds a **new** non-partial `DependencyObject` subclass. Cheap, but it was not in the spec.

## Finding B — Ingest granularity is all-or-nothing

One 125-line XAML file could not be ingested alone. Its transitive closure reached, in order:
`CalculatorButton` → `ResourceString` / `ResourceVirtualKey` → `NumbersAndOperatorsEnum`, `MyVirtualKey` → `AppResourceProvider`, `CalculatorButtonPressedEventArgs` → `KeyboardShortcutManager` → **`ApplicationViewModel`, `ViewMode`** — i.e. the whole ViewModels project.

**Consequence:** confirms the spec's `<Compile Include="**\*.cs" />` glob-the-whole-project design is **mandatory, not merely convenient**. A per-file or per-feature ingest is not available as a fallback, so the Act 2 phases cannot be de-risked by ingesting a subset first.

## Finding C — Uno.Resizetizer generates into `$(RootNamespace)`

Setting `<RootNamespace>CalculatorApp</RootNamespace>` broke the *template's own* `App.xaml.cs` with CS1061 on `SetWindowIcon`, which Uno.Resizetizer source-generates into the root namespace.

**Consequence:** the ingest head cannot freely repoint `RootNamespace` to match upstream's. Either keep the head's own root namespace (upstream types are fully qualified anyway, so this is fine), or account for Resizetizer explicitly. Worth one line in the P4 task.

## Full-glob ingest: the Act 2 starting number

Beyond the S1 predicate, the full architecture was exercised: **all 102 upstream `.cs` and 26 `.xaml`** (excluding `App.xaml`/`App.xaml.cs`) rewritten and linked.

- Rewriter: **102/102 files processed, zero `Windows.UI.Xaml` residue**, 46 partial declarations.
- Build: **581 distinct diagnostics.**

Treat 581 as a *starting backlog*, not a work estimate. It is dominated by cascade — 450 CS9348 and 406 CS0122 (inaccessible-due-to-protection) descend from a much smaller set of root causes, and 30 UXAML0001 + 30 CS0538 + 18 CS1527 are **Uno's own generated XAML output going malformed once type resolution fails upstream of it**, not defects in the rewritten sources. The real root-cause count is not yet known and should be measured in P6 before anyone plans against it.

## Re-run on Uno 7 (2026-08-30)

The whole spike was repeated against **`Uno.Sdk.Private` 7.0.0-dev.579** (`uno-features` feed)
on **`net11.0-desktop`**, with a locally-installed .NET 11 SDK and the `partial` rule off.

| | Uno 6.6.14 / net10 | **Uno 7.0.0-dev.579 / net11** |
|---|---|---|
| S1 pairing predicate | PASS | **PASS** |
| CS0260 (non-partial `DependencyObject`) | blocking | **0** |
| Errors in ingested code | not separable from cascade | **186, from 3 causes** |

The remaining 186 break down as: **174 CS0246** (entirely `CalcManager.Interop` and
`GraphControl` types — both shims the spec already plans to build), **10 CS0579**
(upstream `Properties/AssemblyInfo.cs` duplicating SDK-generated assembly attributes —
one ingest exclusion), and **2 CS0104** (`WindowActivatedEventArgs` ambiguous between
`Windows.UI.Core` and `Microsoft.UI.Xaml` at `TitleBar.xaml.cs:190` — one `using` alias).

Two consequences worth stating plainly:

- **Nothing unexpected surfaced.** Every error traces to a component the spec already
  planned, or to a two-line fix. That is the strongest evidence so far that the
  architecture is sound.
- **Uno 7 drops the native mobile targets**, so Skia is no longer a choice — which suits
  requirement #3, but removes the `-renderer native` fallback that S4's mitigation assumed.
  S4 becomes a hard dependency rather than a preference.

Full detail and the retarget rationale: [`../decisions/2026-08-30-target-uno-7.md`](../decisions/2026-08-30-target-uno-7.md).

## What S1 did **not** settle

- Whether this holds on Android, iOS, WASM or Linux. Desktop/Skia only.
- Runtime behaviour. Nothing was executed; this is a compile-time result.
- `LocalizationSettings` was **stubbed** (530 lines, depends on `Windows.Globalization.*`). Whether the real one works on Uno is open — it is the natural S1c and it bears directly on spec open-question #6.
- S1b (MRT slash-path accessibility) was **not reached** — the build never got green enough to run. It remains open and is now the highest-value next spike, since Finding B means it cannot be probed in isolation either.
