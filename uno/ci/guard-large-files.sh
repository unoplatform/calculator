#!/usr/bin/env bash
#
# Repository hygiene guard. Wire in as a required CI check.
#
# Prevents three ways this repo can be made un-clonable or bloated:
#   1. A blob over 10 MB entering history.
#   2. A gitlink (mode 160000) with no .gitmodules describing it — history that
#      clones without the directory and with no way to restore it. This is the
#      exact state src/CalcManager/emsdk was staged in.
#   3. Emscripten SDK contents being committed as files, which would grow the
#      repository roughly 17x.
#
set -uo pipefail

MAX_BLOB_BYTES=10485760   # 10 MB
fail=0

# --- 1. No oversized blobs in the committed tree -----------------------------
big=$(git ls-tree -r -l HEAD \
      | awk -v max="$MAX_BLOB_BYTES" '
          $2 == "blob" && $4 + 0 > max {
            size = $4; $1 = $2 = $3 = $4 = ""; sub(/^ +/, "")
            printf "  %s (%d bytes)\n", $0, size
          }')
if [ -n "$big" ]; then
  echo "FAIL: blob(s) larger than ${MAX_BLOB_BYTES} bytes are committed:"
  echo "$big"
  fail=1
fi

# --- 2. No gitlink without a .gitmodules -------------------------------------
links=$(git ls-files -s | awk '$1 == "160000" { $1 = $2 = $3 = ""; sub(/^ +/, ""); print "  " $0 }')
if [ -n "$links" ] && [ ! -f .gitmodules ]; then
  echo "FAIL: gitlink(s) present with no .gitmodules to describe them:"
  echo "$links"
  echo "      Such history cannot be cloned correctly. Either add a proper"
  echo "      submodule entry, or: git rm --cached -f <path>"
  fail=1
fi

# --- 3. Nothing under an emsdk/ directory ------------------------------------
emsdk=$(git ls-files | grep -E '(^|/)emsdk/' || true)
if [ -n "$emsdk" ]; then
  echo "FAIL: Emscripten SDK contents are indexed (inflates the repo ~17x):"
  echo "$emsdk" | head -5 | sed 's/^/  /'
  echo "  ... $(echo "$emsdk" | wc -l) path(s) total"
  fail=1
fi

if [ "$fail" -eq 0 ]; then
  echo "guard-large-files: PASS"
fi
exit $fail
