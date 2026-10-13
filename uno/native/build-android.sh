#!/usr/bin/env bash
#
# Builds the native CalcManager engine as libCalcManager.so for Android, one per ABI.
#
# Needs an Android NDK (r27+): pass its root as ANDROID_NDK_ROOT. libc++ is linked statically,
# so the APK carries no libc++_shared.so.
#
# Every upstream engine source compiles unmodified. The only fork-owned C++ is src/CCalcManager.cpp,
# the C ABI the managed P/Invoke layer binds to.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SRC="$ROOT/src/CalcManager"
SHIM="$ROOT/uno/native/src"
NDK="${ANDROID_NDK_ROOT:?Set ANDROID_NDK_ROOT to an Android NDK (r27+) root}"
API=24

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) HOST=windows-x86_64; EXE=.exe ;;
  Darwin*)              HOST=darwin-x86_64;  EXE= ;;
  *)                    HOST=linux-x86_64;   EXE= ;;
esac
CXX="$NDK/toolchains/llvm/prebuilt/$HOST/bin/clang++$EXE"
NM="$NDK/toolchains/llvm/prebuilt/$HOST/bin/llvm-nm$EXE"
[ -x "$CXX" ] || { echo "No clang++ at $CXX" >&2; exit 1; }

# UnitConverter.cpp and NumberFormattingUtils.cpp are deliberately absent: unit conversion has no
# native ABI at all - it is a pure-managed port in uno/interop - and both pull MSVC-only headers.
SOURCES=("$SRC/CalculatorHistory.cpp" "$SRC/CalculatorManager.cpp" "$SRC/ExpressionCommand.cpp" "$SHIM/CCalcManager.cpp")
for f in "$SRC"/CEngine/*.cpp "$SRC"/Ratpack/*.cpp; do SOURCES+=("$f"); done

CXXFLAGS=(-std=c++17 -O2 -fPIC -fexceptions -frtti -DNDEBUG
          -I"$SRC" -I"$SRC/Header Files" -I"$SRC/CEngine" -I"$SRC/Ratpack" -I"$SHIM")

build_abi() {
  local abi=$1 triple=$2
  local obj="$ROOT/uno/native/obj/android/$abi" out="$ROOT/uno/native/bin/android/$abi"
  rm -rf "$obj"; mkdir -p "$obj" "$out"

  echo "=== $abi ($triple$API), ${#SOURCES[@]} sources ==="
  for f in "${SOURCES[@]}"; do
    "$CXX" --target="$triple$API" "${CXXFLAGS[@]}" -c "$f" -o "$obj/$(basename "$f" .cpp).o" &
  done
  wait

  # 16 KB page alignment is required for Android 15+ devices.
  "$CXX" --target="$triple$API" -shared -static-libstdc++ -Wl,-z,max-page-size=16384 \
    -Wl,--gc-sections -s -o "$out/libCalcManager.so" "$obj"/*.o

  echo "=== $out/libCalcManager.so ($(stat -c%s "$out/libCalcManager.so") bytes, \
$("$NM" -D --defined-only "$out/libCalcManager.so" | grep -cE ' T CalculatorManager_') exports) ==="
}

build_abi arm64-v8a aarch64-linux-android
build_abi x86_64 x86_64-linux-android
