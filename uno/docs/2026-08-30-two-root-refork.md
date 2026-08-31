# Uno Calculator: Rebasing on microsoft/calculator

## Decision — **Two-Root Re-Fork, shipped in three acts**

Branch a new trunk `uno-next` directly from `msft/main@4fd3fc53`, keep every upstream path byte-identical, and put all fork code under a single `uno/` root that upstream will never create.

But **do not start there.** Spend two weeks on six timeboxed spikes and a staffing gate; then ~11 weeks landing the strategy-agnostic work — one CMake native build for all six targets, 86 ported engine tests, and the store/privacy/compliance fixes — **on the branch that ships today**. Only then build `uno-next`, and build it as a *walking skeleton first*: a running desktop app compiling unmodified upstream XAML in its second phase, never a 20-week dark valley. Cut over head-by-head, not big-bang.

---

## TL;DR for the impatient

| | |
|---|---|
| **Do not** | rebase (969 commits, 284 merges, first commit alone overlaps 242 upstream-changed paths) |
| **Do not** | one-shot merge (real cost is 216 conflict hunks / 28,326 conflicted app-layer lines, and every "ours" resolution silently consumes upstream's version forever) |
| **Do** | branch from `4fd3fc53` so merge-base is an upstream commit — this alone deletes 277 of today's 424 conflict signals, permanently |
| **Do** | keep the native C++ engine. There is no managed engine to keep — `CCalcEngine.cs` is 456 bytes that throws |
| **Do** | adopt upstream's C# ViewModels wholesale. They shipped 2026-08-25 and obsolete 6,100 LOC of hand-port |
| **Do** | ship the CMake build + engine tests **on `uno` first**, so abandonment leaves the repo better, not empty |
| **Effort** | p50 ~72 person-weeks, p90 ~95. At 2 FTE that is ~9 months; at 3 FTE ~6 |
| **Kill switch** | if <1.5 FTE is committed, do Acts 0–1 only (~14 pw) and stop |

---

## 1. The framing everyone had was stale

Three premises in the original brief are false, and correcting them makes the problem **much** cheaper.

**"Upstream is C++/CX."** It is not. `src/Calculator` has been C# since 2021 (fdae1000, "Hello C#"), and commit `4fd3fc53` on **2026-08-25** — five days before this analysis — finished migrating every ViewModel to C#. `src/CalcViewModel` no longer exists. Upstream is now 150 `.cs` files vs 148 `.cpp/.h` under `src/`.

**"The fork has a hand-ported C# calculation engine."** It does not. `src/Calculator.Shared/CalcManager/CCalcEngine.cs` is 456 bytes whose two methods both `throw new NotImplementedException()`. `RatPak.cs` is 647 bytes of two enums. Grepping the entire shared project for `BASEXPWR|mulnumx|addnum|divrat` returns nothing. **All six heads already P/Invoke upstream's C++** through a 512-line flat C ABI.

**"969 commits behind means 969 commits of work."** The fork is 969 commits *ahead* and 726 *behind* (the brief had these inverted). But of 1,149 files the fork touched and 905 upstream touched, only **254 overlap**, and **184 of those are delete/delete** — git resolves them free.

> **The strategic inversion:** the fork's crown jewel — hand-ported C# ViewModels — became a liability five days ago. Upstream's own C# port shares **0–31%** of lines with it, and git pairs **zero** of the 8 fork files with upstream's 46 at the default rename threshold. There is no merge to attempt. Deleting the hand-port and adopting upstream's is the single highest-leverage move available.

---

## 2. What the divergence actually costs, measured

| Surface | Real divergence | Merge character |
|---|---|---|
| **C++ engine** (`src/CalcManager`) | ~40 lines of fork edits; 88 upstream commits, 56 files, +2,008/−2,127 | Routine text merge. Only **21 file-touches in 24 months** — effectively frozen |
| **Localization** (130 `.resw`) | en-US differs from fork point by **exactly one line** | Pure data. Take upstream wholesale |
| **ViewModels** | 0–31% shared lines, zero git pairing | Not mergeable. Delete ours, adopt theirs |
| **XAML** | Reported +8,127/−6,074; **whitespace-insensitive it is +3,437/−1,518** | 2.4× inflation from `indent_style = tab` |
| **Structural noise** | 277 of 424 conflict signals | 100% self-inflicted: one directory rename + 92 renamed icons |

Two thirds of the scary number is formatting and renames the fork chose. A re-fork never introduces them.

---

## 3. Why re-fork, not merge

`git merge-tree --write-tree HEAD msft/main` today produces **424 CONFLICT lines across 474 paths** and a **1,346-file tree containing both app implementations side by side** (633 `Calculator.Shared` + 46 `Calculator.ViewModels` + 19 `Calculator` + 52 `Calculator.UWP`).

Branching from `4fd3fc53` makes `git merge-base uno-next msft/main` an upstream commit. Rename detection has nothing to do because *our tree is upstream's tree plus one directory*.

The merge strategy's fatal flaw is procedural, not technical: **resolving a conflict to "ours" tells git you saw upstream's version, and it will never be offered again.** Twenty-two XAML files and 37 code-behind files resolved under schedule pressure silently discard seven years of upstream UI work — behind a merge commit that claims otherwise. That is strictly *worse* than a re-fork, where the omission is at least visible.

What re-fork genuinely destroys — `git log --follow` on app files, and 120 uno-only commits each encoding a platform workaround with a reason — is preserved cheaply: retain `uno-legacy` forever, record `Harvested-From:` / `Upstream-Commit:` trailers, and mine the 142 `TODO UNO` markers into a `GAPS.md` **before** demolition. Hours of work.

---

## 4. Repository shape: two roots

```
/                       <- UPSTREAM TERRITORY. Byte-identical. CI-enforced.
  .editorconfig         inherited verbatim (spaces, LF). No normalization commit needed.
  build/pipelines/**    KEPT, never run, never deleted (deleting = permanent modify/delete conflict)
  src/Calculator/                60 .cs + 27 .xaml + 120 .resw (60 locales) + 222 png
  src/Calculator.ViewModels/     43 .cs / 12,232 LOC          <- consumed via glob
  src/Calculator.Tests/          283 [TestMethod]             <- IN CONTRACT
  src/CalculatorUITestFramework/ 209 AutomationIds            <- IN CONTRACT
  src/CalcManager/               57 C++ files                 <- compiled by OUR CMake, unmodified
  src/CalcManager.Interop/       the two .idl                 <- never built; it is the SPEC
  src/GraphControl/ GraphingImpl/ TraceLogging/               <- kept, never built

/uno/                   <- FORK TERRITORY. Upstream will never create this path.
  app/Uno.Calculator/          ONE Uno.Sdk single project (see TFM table below)
  interop/CalcManager.Interop.Managed/   THE SEAM
  interop/GraphControl.Shim/             types-only, 8 types
  adapters/                    10 hash-gated overrides
  native/  CMakeLists.txt + abi/ + compat/ + toolchains/
  build/ingest/                the WUX->MUX rewriter + golden tests
  tests/                       engine (86), viewmodel (283), UI (160), smoke head
  ci/  purity-gate.sh, upstream-modified.allow (EMPTY), upstream-pinned.sha256
  pipelines/  packaging/  docs/
```

**The rule, stated once:** upstream owns everything outside `uno/`; we own `uno/` and nothing of theirs. There is no directory where both parties write, which is why there is no directory where they can conflict.

---

## 5. Six targets from five TFMs

| Requirement | TFM | Native engine | Artifact |
|---|---|---|---|
| Android | `net10.0-android` | `libCalcManager.so` per ABI via CMake+NDK → `AndroidNativeLibrary` | `.aab` + `.apk` |
| iOS | `net10.0-ios` | XCFramework (arm64 device + **arm64 sim** + x64 sim), `DllPath="__Internal"` | `.ipa` + `.dSYM` |
| **Win32** | `net10.0-desktop` `.UseWin32()` | `CalcManager.dll` | zip + MSIX |
| **Linux** | `net10.0-desktop` `.UseX11()` | `libCalcManager.so` (x64/arm/arm64) | zips + Snap |
| **macOS** | `net10.0-desktop` `.UseMacOS()` | universal `libCalcManager.dylib` | `.app`/`.pkg`/`.dmg` |
| WASM | `net10.0-browserwasm` | `CalcManager.a` via `WasmShellNativeFileReference` | `wwwroot` / PWA |

`net10.0-desktop` is **one build output** covering three of the six requirements; backends are selected at runtime in `Platforms/Desktop/Program.cs`.

This deletes: the GTK-on-Windows hack (Uno 6.0 removed GTK desktop outright), the stale `Calculator.UWP` head (pinned to Uno.UI 4.2.0-dev.267, referenced by no pipeline), the `.shproj`/`.projitems` 393-line hand-maintained file list, and — critically for merges — **any need to edit a project file when upstream adds a source file.**

Pinned to **Uno.Sdk 6.7.18** stable (Uno.WinUI 6.7.65) via `global.json`, with a parallel CI leg on 6.8.0-dev.24. There are no 7.0 bits; the 7.x/9.x numbers in circulation are Uno.Extensions and Uno.Toolkit, which version independently.

**Two honest caveats.** Linux has no native Wayland backend — X11 via XWayland, needing `UNO_DISPLAY_SCALE_OVERRIDE` for DPI. And Mac Catalyst was dropped from Uno templates in 6.0, so macOS ships via desktop/AppKit — which means **no Mac App Store channel** unless we deliberately retain Catalyst.

---

## 6. The "namespace magic" hypothesis: half true, and the halves need opposite treatments

### XAML — the magic works. Do nothing.

All 27 upstream XAML files use the default `winfx/2006/presentation` xmlns, which Uno maps to `Microsoft.UI.Xaml` (`XamlConstants.cs:20`). `IsApiContractPresent(UniversalApiContract, ≤14)` is true, so contract7/8/14 markup resolves correctly. `muxc:` and CommunityToolkit's `toolkit:` resolve verbatim. All 474 `x:Bind`, 66 function bindings, `x:Load`, `ThemeShadow` are supported.

**Upstream `.xaml` and `.resw` are LINKED, never copied, never transformed.** That is what keeps the 426 markup keys and 373 resw keys in lockstep across 60 locales.

### C# — the magic fails, and no shim can save it.

326 lines across 72 of 103 upstream `.cs` files reference `Windows.UI.Xaml`. Only 130 are `using` directives — **196 are fully-qualified in code.** No CLR mechanism reaches those: `TypeForwardedTo` preserves full names, `extern alias` only disambiguates identical names, and a hand-declared `Windows.UI.Xaml.Visibility` is a *different type* that fails every assignment.

**Kill the shim-assembly idea in writing, week one.** Build a table-driven MSBuild rewriter instead (not a Roslyn generator — generators are strictly additive and cannot rename a namespace). Uno did exactly this for its own WUX→MUX; the marker comment survives at `XamlConstants.cs:18`.

| Rewrite | Deny (rewriting these **breaks** the build) |
|---|---|
| `Windows.UI.Xaml` → `Microsoft.UI.Xaml` | `Windows.UI.Color` — Color is `Windows.*`, Colors is `Microsoft.*` |
| `Windows.UI.Text` → `Microsoft.UI.Text` | `Windows.UI.Xaml.Data.BindableAttribute` — Uno declares it in both and matches by *simple* name, so all 19 `[Bindable]` sites work untouched |
| `Windows.UI.Colors` → `Microsoft.UI.Colors` | `ContentPropertyAttribute`, `Windows.UI.Xaml.Interop.*` |
| `Windows.UI.Composition` → `Microsoft.UI.Composition` | anything inside a string literal |

The nastiest trap: Uno declares **both** `Windows.UI.Text.TextSetOptions` and `Microsoft.UI.Text.TextSetOptions` while `RichEditBox.Document` is typed `Microsoft.UI.Text.RichEditTextDocument` — so a missed rewrite yields a confusing **CS1503 wrong-type** error, not a helpful missing-type one. Golden tests cover exactly these four cases.

---

## 7. The two seams where a shim genuinely works

A shim works only where Uno declares **nothing** in the namespace. There are exactly two such places.

**`namespace CalcManager.Interop`** — a managed assembly with the exact public shape of the two `.idl` files (`CalculatorManagerWrapper` ~42 methods + 12 delegates, `UnitConverterWrapper` + 5 structs + 3 callback bases), implemented over the harvested 36 P/Invokes. Then all 12,232 lines of upstream ViewModels compile unchanged, forever. The `.idl` is 420 lines and is the single recurring merge checkpoint.

**`namespace GraphControl`** — types and enums only, 8 types. This resolves graphing **by inclusion, not exclusion**, which matters because *excluding `GraphingCalculator/**` does not compile*: `ApplicationViewModel.cs:32,231,233` declares and constructs `GraphingCalculatorViewModel`. I verified that **zero `.cs` files outside `GraphingCalculator/` reference any `GraphControl.*` type**, so the shim lets all four graphing ViewModels compile unchanged and keeps `ApplicationViewModel.cs` byte-identical. The mode is gated off at `NavCategoryStates.IsViewModeEnabled`.

> Note: the Uno app **never shipped Graphing** — `NavCategory.cs` on `uno` has zero Graphing references. This is a compile-closure fix, not a feature cut, and the pack over-weighted it. (The real graphing solver isn't even in the OSS repo — `GraphingImpl` is `MockGraphingImpl` only.)

---

## 8. The engine: native C++, and one very neat trick

Seven years of engine fixes arrive as a **text merge into files we do not own**: `ffd05196` (√(2.25)−1.5), `758c5505` (Programmer dec division), `0e6d6693` (implicit multiply after `)`), `59635680` (BASEXPWR in log2), `749491dd` (F-E toggle), and five more.

Upstream's engine is already clean portable C++ — zero `ref class`, zero `winrt::`, zero `Windows.h` — and Microsoft themselves added `sal_cross_platform.h` and `winerror_cross_platform.h`.

**The one blocker, and its zero-edit fix.** Commit `54b3b644` put `#include <ppltasks.h>` into `UnitConverter.h:8`. I checked: it uses **angle brackets**. And `pch.h` unconditionally includes `<intsafe.h>`, `<ppltasks.h>` *and* `<winerror.h>`. So the fix is three fork-owned headers in `uno/native/compat/`, placed on the include path ahead of the system path for clang/g++/emcc only:

```cmake
target_include_directories(CalcManager BEFORE PRIVATE ${CMAKE_CURRENT_LIST_DIR}/compat)  # non-MSVC
```

**Zero upstream edits** — which is what lets `upstream-modified.allow` be enforced as *literally empty*, turning the headline invariant from an audited promise into a CI-provable fact.

**But the ppltasks shim is an async interop bridge, not a header.** `UnitConverter.h:198-200` declares three data-loader methods **pure virtual** returning `concurrency::task<T>` — so the implementer is C#. And the WASM artifact path is `st,simd` — **single-threaded**, where `std::async` has no thread. The shim must create a completion handle natively, marshal an opaque pointer across the C ABI, let managed code complete it, and run continuations **inline**. Budget 1.5–2 weeks and prove it on browserwasm/iOS/Android before anything depends on it.

**One CMake replaces seven recipes**, retiring `CalcManager.Android.vcxproj` (`PlatformToolset=Clang_5_0` — the VS "Mobile development with C++" workload Microsoft removed in VS 2026, the repo's highest bus-factor item) and fixing three things broken *today*: no arm64 iOS-simulator slice, no macOS arm64 slice while the csproj references one, and three scripts globbing `RatPack/*.cpp` against a directory named `Ratpack`.

---

## 9. The gate that makes the claim checkable

Every "never edit an upstream file" policy fails at 2am under a deadline. This one is machine-enforced.

```bash
BASE=$(git merge-base HEAD msft/main)     # self-maintaining. NEVER a self-tag.

# (a) no modifications to upstream files — allow-list must be EMPTY
# (b) no deletions, ever — a delete/modify conflict is forever
# (c) no additions outside uno/ — allow-list must be EMPTY
# (d) sha256sum -c uno/ci/upstream-pinned.sha256
#     ^ THE HOLE EVERY OTHER GATE MISSES: a file excluded from ingest and replaced
#       by a fork-owned equivalent stays byte-identical on disk while OUR replacement
#       rots. That is exactly the UnitConversionManager.cs failure mode, reintroduced.
```

The reference implementation of this idea computed its baseline from a tag it applied to *its own merge commit* — so it diffed the tree against itself and **failed green from sync #2 onward, forever**. Reproduced in a scratch repo: a modified file produced zero reported modifications. Hence `merge-base`, and hence **negative CI tests**: four cases that deliberately modify, delete, add and drift-an-excluded-file, each asserting the gate goes red. A load-bearing gate with no negative test is an unverified claim.

**Hard caps in CI:** modified.allow == 0, added.allow == 0, pinned.sha256 ≤ 12 rows, forked views ≤ 5. Raising a cap is its own reviewed PR that changes a number.

---

## 10. Compile-green, runtime-red — the dominant failure mode

Uno ships `[NotImplemented]` stubs for the entire UWP surface, failing two ways: some **throw**, others merely **log**. A green build proves nothing.

Three silent failures found, none of which any test or compiler would catch:

**426 accessibility strings resolve to empty.** Upstream XAML uses MRT slash-paths (`Name=scrollLeft/[using:...]AutomationProperties/Name`). I counted the resw: **0 slash keys, 373 dot keys.** Uno's `ResourceLoader` does a literal `TryGetValue` and returns `string.Empty`. On a Microsoft accessibility-flagship app.

**`GetApplicationViewIdForWindow` hard-throws** and is called ungated at `ApplicationViewModel.cs:269` on **every cold launch**. Two more call sites sit inside `Dispatcher.RunAsync` continuations — so the exception is *unobserved*: no crash, the memory panel just silently never populates. The clincher: **the fork's own `ApplicationViewModel.cs` has those seven calls commented out.** Seven years of shipping already proved this file cannot be consumed verbatim.

**An x:Uid case bomb.** `x:Uid="LshLogicalButton"` matches resw key `lshLogicalButton.[using:...]` case-insensitively under MRT; Uno uses an ordinal dictionary so it does *not* match — which is the only reason the build survives, because Uno's XAML generator **throws** on an unresolvable type in such a key and Uno's `Windows.UI.Xaml.Automation` has no `AutomationProperties`. The day Uno fixes its casing, 120 resw files across 60 locales become a hard build failure on all six heads. **Land the resw rewrite rule now, disabled, with a golden test.**

**Four required PR gates** answer this: a `[Uno.NotImplemented]` reachability analyzer over compiled ingest output (this replaces hand-enumeration — a ten-minute grep already found four files every plan's list missed); a `Windows.*` namespace census diffed per sync; an unobserved-exception gate plus a scripted five-mode walk; and a non-empty `AutomationProperties.Name` assertion on three heads.

---

## 11. The override ledger — 10 files, hash-gated, each classified

| Upstream file | Why | Retirable? |
|---|---|---|
| `Common/AppResourceProvider.cs` | MRT slash-path → 426 a11y strings | ✅ Uno PR |
| `Common/Utils.cs` | `GetWindowId()` choke point, returns −1 off-Windows | ✅ Uno PR |
| `ApplicationViewModel.cs` | same API, every cold launch | ✅ |
| `Views/Calculator.xaml.cs` `:265` | same, **unobserved** in a `RunAsync` continuation | ✅ |
| `Views/MainPage.xaml.cs` `:541` | same | ✅ |
| `Common/TraceLogger.cs` | calls `TraceLoggingCommon.GetInstance()` at `:369` with **no `#if` anywhere** | absorbed by our shim — file stays byte-identical |
| `Common/LocalizationSettings.cs` | `LanguageFontGroup` throws | ✅ Uno PR |
| `DataLoaders/CurrencyDataLoader.cs` | `Windows.Data.Json` throws — kills currency mode | ✅ Uno PR |
| `DataLoaders/CurrencyHttpClient.cs` | live rates vs upstream's fictional Mars/Moon data | ❌ **permanent** |
| `Common/NetworkManager.cs` | `ConnectionProfile.GetConnectionCost` throws | ✅ Uno PR |

Prefer whole-file replacement over anchored patches on churny files: a hash mismatch says *"port the delta"*; a moved anchor says *"good luck"*. `ApplicationViewModel.cs` saw 6 commits in 24 months.

**Fonts — the mechanism every plan got wrong.** Upstream defines `CalculatorFontFamily` **directly in `Application.Resources` at App.xaml:335**, and a directly-defined key beats anything in MergedDictionaries. So "a fork-owned dictionary merged after App.xaml" — which every strategy proposed — *provably cannot override it*. App-level keys must be overridden imperatively in `App.uno.cs` before the first page parses.

---

## 12. Roadmap

### Act 0 — Spikes and the staffing gate (2 pw)

Six spikes, run in parallel. **S1 (linked XAML build mechanics) is the stop-and-replan gate** for the whole strategy and must run in week 1. **S6 (store baseline + staffing)** decides whether Acts 2–3 happen at all.

Also: unstage the 1.1 GB `emsdk` gitlink (mode 160000, no `.gitmodules`; `.git` is only 67 MB, so committing it as content is a 17× blowup), fix the security-reporting link — README and CONTRIBUTING route **security disclosures** to `github.com/nventive/calculator`, the wrong org — and mine 142 TODO-UNO markers into `GAPS.md`.

### Act 1 — Strategy-agnostic work, on the branch that ships (12 pw)

| Phase | Deliverable |
|---|---|
| P1 (6 pw) | One CMake, six targets. Retires the discontinued VS Android workload. Fixes iOS arm64-sim and macOS arm64. Per-target smoke **launch**, not just build |
| P2 (3 pw) | 70 engine tests over the *existing* C ABI + a committed golden baseline. (70, not 86 — `UnitConverterTest`'s 16 cases need `UnitConverter.cpp`, which the fork deleted. A gate that cannot be met gets waived) |
| P3 (3 pw) | Compliance: privacy manifest, versionCode offsets, telemetry decision, license/NOTICE, snap base, .NET 10 |

> **All of Act 1 merges to `uno` and ships.** This is the crucial structural change: every plan in the pack authored the CMake build and engine tests against post-demolition paths, so abandonment — the statistically likeliest outcome given 14 commits in 2025 and zero in 2026 — yielded **literally zero**. Here it yields a retired dead toolchain, the repo's first automated tests, and a compliant store release.
>
> **P3 is the last point at which stopping costs nothing.**

### Act 2 — The re-fork, continuously green (28 pw)

| Phase | Deliverable | Ends with |
|---|---|---|
| P4 (6 pw) | Walking skeleton: `uno-next`, purity gate, ingest pipeline, one upstream page over a stub interop | **A running desktop app** |
| P5 (6 pw) | Real managed `CalcManager.Interop` + C ABI re-sync + async bridge | Real arithmetic; golden-diff explained |
| P6 (8 pw) | Upstream ViewModels + 283 upstream tests + UnitConverter back to C++ | 283 green |
| P7 (8 pw) | All 27 XAML files, adapters, **accessibility GO/NO-GO** | Feature-complete desktop |

No 20-week valley. Every phase ends with an app you can launch.

### Act 3 — Ship (22 pw)

| Phase | Deliverable |
|---|---|
| P8 (10 pw) | Five more targets, one at a time. Size/startup budgets **measured**. Forked-view count CI-capped at 5 |
| P9 (4 pw) | Five Uno platform PRs. Each landed PR **deletes** an override row and lowers a cap |
| P10 (6 pw) | Eleven artifacts, one pipeline, **head-by-head cutover** |
| P11 (2 pw) | The sync drill — execute and *time* a real sync. This is the verdict on the strategy |

**Circuit breaker:** if no head runs by week 20, cut scope to desktop + WASM as v1 and ship those two while mobile stays on `uno`. Bundle IDs are shared, so a split cutover is feasible.

---

## 13. Effort, honestly

| Act | Person-weeks | Notes |
|---|---:|---|
| Act 0 — spikes & gate | 2 | Mostly lookups and parallel spikes |
| Act 1 — agnostic, ships on `uno` | 12 | **Required under any strategy. Never wasted** |
| Act 2 — the re-fork | 28 | Load-bearing risk concentrated in P4–P6 |
| Act 3 — six targets, ship, prove | 22 | P8 widest variance |
| **p50 total** | **~64** | |
| **+ contingency (attached to P7/P8)** | **~8** | |
| **p90** | **~95** | |

At **2 FTE ≈ 9 months**; at **3 FTE ≈ 6 months**. The critical path (P4→P5→P6→P7) is serial, so extra engineers cannot compress it below ~5 months.

**Read that honestly against the repo's demonstrated capacity: 606 commits in 2019 → 78 (2024) → 14 (2025) → 0 (2026), HEAD dated 2025-08-19.** At the demonstrated rate this is a multi-year effort. That is precisely why Act 1 ships independently and why Act 0 gates on a written staffing commitment.

---

## 14. The payoff: what a sync looks like afterwards

```bash
git switch uno-next && git fetch msft
git switch -c sync/$(date +%Y-%m)
git merge msft/main                    # expected: ZERO conflicts
bash uno/ci/purity-gate.sh
dotnet build uno/Calculator.Uno.slnx
dotnet test  uno/tests/Uno.Calculator.EngineTests uno/tests/Uno.Calculator.ViewModelTests
gh pr create
```

**Zero is structural, not optimistic.** A 3-way merge conflicts only where both sides changed the same file. We change exactly one thing: files under `uno/`, which upstream cannot create. No modifications, no deletions, no additions under upstream paths — all three CI-enforced empty. The intersection of our change set with upstream's is the empty set.

| Upstream does… | We do… |
|---|---|
| Adds a `.cs` file | **Nothing.** Our glob mirrors upstream's own `**\*.cs` |
| Edits a `.cs` file | Nothing; the rewriter re-processes it. A new pattern fails the *build* with a file and line |
| Edits XAML or `.resw` | Nothing. We never reformat, re-indent, or touch xmlns |
| Edits a `.csproj`/`.slnx` | Nothing. Merges into files we never build |
| Edits CalcManager C++ | Nothing. 86 engine tests are the net. *9 commits / 24 months* |
| Changes an overridden file | `sha256` gate fails by name. 15–60 min. **~6–10 per year** |
| Changes either `.idl` | The seam job fails with the exact delta. The *only* signal needing thought |

**Realistic recurring budget: well under 2 person-weeks per year**, against upstream velocity of 70 commits / 24 months.

**And an honest counterweight.** The contract makes ViewModels, engine and resources free — but upstream's *View-layer* changes (41 `.cs` + 28 `.xaml` in 24 months) still need a human. A weekly CI job appends those to `uno/docs/UNABSORBED.md`, so the half we don't auto-absorb is a **visible, reviewed backlog** rather than silence. That is what stops the seven-year failure mode from quietly reproducing itself.

---

## 15. Things I need from you before Monday

1. **Staffing.** Named engineer, weekly hours, in writing. Below 1.5 FTE → Acts 0–1 only.
2. **Live store versions.** Play `versionCode` (ours computes to **1162** today and drops to ~919 from a fresh branch — **Google Play rejects non-increasing versionCodes**, so the four live listings could never be updated again). The offsets already exist in `build.csproj`; nobody set them. 30 minutes of lookups.
3. **The uncommitted TFM edit.** Does dropping `net9.0-maccatalyst;net9.0-macos` mean macOS is already dark? That moves macOS from *migrate* to *repair*.
4. **macOS channel.** Desktop/AppKit means no Mac App Store. Acceptable?
5. **Is the Uno showcase banner a requirement?** If it must live inside upstream's `MainPage`, the zero-edit claim acquires a permanent override on the contract's churniest file.
6. **The 7 extra locales.** `ha-NG` is 62% translated, three others 85%, against upstream's 1,170 strings — and the repo's own issue template says translations *cannot be merged* because the resw come from a Microsoft-internal pipeline. Ship, retire, or stand up a community overlay?
7. **Telemetry.** App Center's remnant ends **March 2027**; four secrets are hardcoded; WASM reports to a Google Universal Analytics property sunset in 2023 and undisclosed in README. My recommendation: ship zero telemetry.

### Five Uno issues I'd like to file

Each one that lands **deletes** a permanent override — the only mechanism that makes the fork shrink:

1. `ResourceLoader.GetString` doesn't implement MRT slash-path semantics → 426 a11y strings silently empty
2. x:Uid↔resw matching is ordinal where MRT is case-insensitive → **live time bomb**, one character from breaking every head
3. `Windows.Data.Json` `Parse`/`TryParse` throw → kills the entire currency converter
4. `LanguageFontGroup` throws → kills font selection and RTL detection
5. `ApplicationView.GetApplicationViewIdForWindow` hard-throws where a sentinel would let upstream's code run unmodified

Say the word and I'll open them.

---

## Appendix — what we're deliberately *not* doing

| Not doing | Because |
|---|---|
| Rebasing 969 commits | First commit alone (782 files) overlaps 242 upstream-changed paths; 284 are merge commits |
| One big merge | 216 conflict hunks / 28,326 conflicted lines, and "ours" resolutions silently consume upstream forever |
| `git subtree` vendoring | Its guarantee depends on commit-message trailers surviving PR merge; one "Squash and merge" click re-adds 774 files as add/add conflicts |
| Rewriting the UI (greenfield) | Upstream's non-graphing view layer is 19,405 LOC and the fork's independent implementation is 19,777 — two teams within 2%. And it permanently forfeits absorbing upstream View changes |
| Shipping Graphing | `GraphControl` is C++/CX + D2D/D3D11 + SwapChainPanel; the real solver isn't in the OSS repo; `MathRichEditBox` needs a licensed COM interface. The Uno app never shipped it |
| Deleting `build/pipelines/`, `internal/`, `GraphControl/` | Deleting an upstream file creates a permanent modify/delete conflict on every future edit. They cost bytes and zero maintenance |
| A "compat shim" for WUX→MUX | There is no CLR mechanism to rename a type across namespaces. Discovering this in month three is the classic failure |
