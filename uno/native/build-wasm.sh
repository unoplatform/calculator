#!/usr/bin/env bash
#
# Builds the native CalcManager engine for WebAssembly.
#
# No emsdk install required: the .NET SDK ships the exact Emscripten the runtime was linked
# against, under packs/Microsoft.NET.Runtime.Emscripten.<ver>.*. Using any other emsdk risks
# an ABI mismatch, since Emscripten does not guarantee compatibility across versions.
#
# Every upstream engine source compiles unmodified. The only fork-owned C++ is src/CCalcManager.cpp,
# the C ABI the managed P/Invoke layer binds to.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SRC="$ROOT/src/CalcManager"
SHIM="$ROOT/uno/native/src"
PACKS="${DOTNET_PACKS:-C:/Program Files/dotnet/packs}"

# Pick the newest Emscripten pack the SDK has, and the newest band within it.
EM_SDK_PACK=$(ls -d "$PACKS"/Microsoft.NET.Runtime.Emscripten.*.Sdk.win-x64 2>/dev/null | sort -V | tail -1)
[ -n "$EM_SDK_PACK" ] || { echo "No Emscripten pack found under $PACKS - install the wasm-tools workload." >&2; exit 1; }
EMVER=$(basename "$EM_SDK_PACK" | sed -E 's/.*Emscripten\.([0-9.]+)\.Sdk.*/\1/')
BAND=$(ls "$EM_SDK_PACK" | sort -V | tail -1)
PACK_BASE="$PACKS/Microsoft.NET.Runtime.Emscripten.$EMVER"

export EMSDK_PATH="$EM_SDK_PACK/$BAND/tools"
export DOTNET_EMSCRIPTEN_LLVM_ROOT="$EMSDK_PATH/bin"
export DOTNET_EMSCRIPTEN_BINARYEN_ROOT="$EMSDK_PATH"
export DOTNET_EMSCRIPTEN_NODE_JS="$PACK_BASE.Node.win-x64/$BAND/tools/bin/node.exe"
export EMSDK_PYTHON="$PACK_BASE.Python.win-x64/$BAND/tools/python.exe"
export EM_CONFIG="$EMSDK_PATH/emscripten/.emscripten"
export EM_CACHE="$PACK_BASE.Cache.win-x64/$BAND/tools/emscripten/cache"
export FROZEN_CACHE=True

emcc() { "$EMSDK_PYTHON" "$EMSDK_PATH/emscripten/emcc.py" "$@"; }

# The bootstrapper picks a bitcode variant by <emscripten version>/<runtime features>; see
# uno.wasm.bootstrap features-module-linking. st,simd matches the default .NET wasm runtime.
OUT="$ROOT/uno/native/bin/wasm/CalcManager.a/$EMVER/st,simd"
OBJ="$ROOT/uno/native/obj/wasm"
rm -rf "$OBJ"; mkdir -p "$OBJ" "$OUT"

# UnitConverter.cpp and NumberFormattingUtils.cpp are deliberately absent: unit conversion has no
# native ABI at all - it is a pure-managed port in uno/interop - and both pull MSVC-only headers.
SOURCES=("$SRC/CalculatorHistory.cpp" "$SRC/CalculatorManager.cpp" "$SRC/ExpressionCommand.cpp" "$SHIM/CCalcManager.cpp")
for f in "$SRC"/CEngine/*.cpp "$SRC"/Ratpack/*.cpp; do SOURCES+=("$f"); done

# Upstream Workaround: Ratpack/conv.cpp gates Calc_UInt32x32To64 on a list of architecture macros
# with no wasm branch, so it hits `#error Must define a target architecture`. Both branches expand
# to the same 64-bit widening multiply, and _M_IX86 appears nowhere else in the engine, so defining
# it here is inert. Delete once microsoft/calculator adds `|| defined(__wasm__)`.
CXXFLAGS=(-std=c++17 -fwasm-exceptions -O2 -D_M_IX86=1
          -I"$SRC" -I"$SRC/Header Files" -I"$SRC/CEngine" -I"$SRC/Ratpack" -I"$SHIM")

echo "=== emscripten $EMVER (band $BAND), $(( ${#SOURCES[@]} )) sources ==="
for f in "${SOURCES[@]}"; do
  emcc "${CXXFLAGS[@]}" -c "$f" -o "$OBJ/$(basename "$f" .cpp).o"
done

echo "=== linking bitcode ==="
emcc -fwasm-exceptions -O2 -r -o "$OUT/CalcManager.a" "$OBJ"/*.o

echo "=== $OUT/CalcManager.a ($(stat -c%s "$OUT/CalcManager.a") bytes, \
$("$DOTNET_EMSCRIPTEN_LLVM_ROOT/llvm-nm.exe" "$OUT/CalcManager.a" | grep -cE ' T CalculatorManager_') exports) ==="
