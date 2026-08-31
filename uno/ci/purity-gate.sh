#!/usr/bin/env bash
#
# The two-root invariant, enforced.
#
# Everything outside uno/ belongs to microsoft/calculator and must stay byte-identical
# to it. Everything inside uno/ is ours, at a path upstream will never create. Because
# a three-way merge only conflicts where BOTH sides changed the same file, and our change
# set is confined to uno/, the intersection with upstream's change set is empty - so
# `git merge msft/main` is conflict-free by construction rather than by discipline.
#
# Required PR check. Has negative tests in uno/ci/purity-gate-tests.sh: a gate with no
# negative test is an unverified claim.
#
set -uo pipefail

BASE=$(git merge-base HEAD msft/main 2>/dev/null)
if [ -z "$BASE" ]; then
  echo "FAIL: cannot resolve merge-base with msft/main (is the remote fetched?)"
  exit 1
fi

fail=0
allow_dir="$(git rev-parse --show-toplevel)/uno/ci"

# (a) No modifications to upstream-owned files. The allow-list must stay EMPTY.
if [ -s "$allow_dir/upstream-modified.allow" ]; then
  echo "FAIL: upstream-modified.allow must be empty (raising the cap is its own reviewed PR)"
  fail=1
fi
modified=$(git diff --diff-filter=M --name-only "$BASE" -- . ':(exclude)uno/')
if [ -n "$modified" ]; then
  echo "FAIL: upstream file(s) modified:"; echo "$modified" | sed 's/^/  /'
  fail=1
fi

# (b) No deletions of upstream files, ever. A delete/modify conflict is forever.
deleted=$(git diff --diff-filter=D --name-only "$BASE" -- . ':(exclude)uno/')
if [ -n "$deleted" ]; then
  echo "FAIL: upstream file(s) deleted:"; echo "$deleted" | sed 's/^/  /'
  fail=1
fi

# (c) No additions outside uno/. Even CMakeLists.txt lives under uno/, because upstream
#     had src/CalcManager/CMakeLists.txt once (fe30c7ca) and we intend to offer it back.
if [ -s "$allow_dir/upstream-added.allow" ]; then
  echo "FAIL: upstream-added.allow must be empty"
  fail=1
fi
added=$(git diff --diff-filter=A --name-only "$BASE" -- . ':(exclude)uno/')
if [ -n "$added" ]; then
  echo "FAIL: file(s) added under an upstream path:"; echo "$added" | sed 's/^/  /'
  fail=1
fi

# (d) Forked-view cap. Each forked view is upstream's file plus a workaround for a filed
#     Uno bug; every one is debt that disappears when its fix ships.
forked=$(find uno/forked -name '*.xaml' 2>/dev/null | wc -l | tr -d ' ')
if [ "$forked" -gt 5 ]; then
  echo "FAIL: $forked forked views exceeds the cap of 5"
  fail=1
fi

if [ "$fail" -eq 0 ]; then
  echo "purity-gate: PASS (upstream untouched, $forked/5 forked views)"
fi
exit $fail
