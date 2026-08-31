#!/usr/bin/env bash
#
# Negative tests for purity-gate.sh. A load-bearing gate with no negative test is an
# unverified claim - this is the exact defect that made the originally-proposed gate
# vacuous (it tagged its own merge commit, then diffed the tree against itself and
# passed green forever).
#
# Each case deliberately violates one rule and asserts the gate goes RED, then reverts.
#
set -uo pipefail
cd "$(git rev-parse --show-toplevel)" || exit 1

GATE=uno/ci/purity-gate.sh
pass=0
fail=0

expect() {   # expect <want:pass|fail> <label>
  local want="$1" label="$2"
  if bash "$GATE" >/dev/null 2>&1; then local got=pass; else local got=fail; fi
  if [ "$got" = "$want" ]; then
    echo "  ok   - $label (gate=$got)"; pass=$((pass + 1))
  else
    echo "  FAIL - $label (gate=$got, wanted=$want)"; fail=$((fail + 1))
  fi
}

echo "purity-gate negative tests"

# 0. Baseline: a clean tree must pass, or every case below is meaningless.
expect pass "clean tree passes"

# 1. Modifying an upstream file must fail.
victim=README.md
cp "$victim" "$victim.bak"
printf '\n<!-- purity gate probe -->\n' >> "$victim"
expect fail "modifying an upstream file is rejected"
mv "$victim.bak" "$victim"

# 2. Adding a file under an upstream path must fail.
probe=src/.purity-probe
: > "$probe"
git add -N "$probe" >/dev/null 2>&1
expect fail "adding a file outside uno/ is rejected"
git rm --cached -q "$probe" >/dev/null 2>&1; rm -f "$probe"

# 3. Deleting an upstream file must fail.
victim2=NOTICE.txt
cp "$victim2" "$victim2.bak"
rm -f "$victim2"
expect fail "deleting an upstream file is rejected"
mv "$victim2.bak" "$victim2"

# 4. A non-empty allow-list must fail (raising a cap is its own reviewed PR).
echo "src/Calculator/App.xaml.cs" > uno/ci/upstream-modified.allow
expect fail "a non-empty upstream-modified.allow is rejected"
: > uno/ci/upstream-modified.allow

# 5. Exceeding the forked-view cap must fail.
mkdir -p uno/forked/.probe
for i in 1 2 3 4 5 6; do : > "uno/forked/.probe/p$i.xaml"; done
expect fail "exceeding the forked-view cap is rejected"
rm -rf uno/forked/.probe

echo
echo "negative tests: $pass passed, $fail failed"
[ "$fail" -eq 0 ] || exit 1
