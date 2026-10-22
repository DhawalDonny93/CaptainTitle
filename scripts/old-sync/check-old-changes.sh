#!/usr/bin/env bash
# Detects what changed in the OLD Captain Title repo since the recorded baseline and tells you where it lands
# in the new codebase (via docs/captain-title/old-baseline/port-map.tsv).
#
#   scripts/old-sync/check-old-changes.sh /path/to/old/captain-title          # report
#   scripts/old-sync/check-old-changes.sh /path/to/old/captain-title --accept # after porting: record new baseline
#
# Works on git blob hashes (git ls-files -s), so it also catches uncommitted-then-committed edits and renames.
set -euo pipefail
OLD="${1:?usage: $0 <old-repo> [--accept]}"
HERE="$(cd "$(dirname "$0")/../.." && pwd)"
BASE="$HERE/docs/captain-title/old-baseline"
MANIFEST="$BASE/files.manifest"
MAP="$BASE/port-map.tsv"
TMP="$(mktemp)"; trap 'rm -f "$TMP" "$TMP.old" "$TMP.new"' EXIT

git -C "$OLD" ls-files -s | awk '{print $2"\t"$4}' | sed 's/\t/ /' | LC_ALL=C sort -k2 > "$TMP"   # "<blob> <path>"
HEAD_NOW="$(git -C "$OLD" rev-parse --short HEAD)"
DIRTY="$(git -C "$OLD" status --porcelain | wc -l | tr -d ' ')"

if [[ "${2:-}" == "--accept" ]]; then
  cp "$TMP" "$MANIFEST"
  echo "$HEAD_NOW" > "$BASE/BASELINE_COMMIT"
  echo "Baseline updated to old commit $HEAD_NOW ($(wc -l < "$MANIFEST" | tr -d ' ') files). Remember to log it in docs/captain-title/OLD_SYNC_LOG.md"
  exit 0
fi

echo "Old repo HEAD: $HEAD_NOW   (baseline: $(cat "$BASE/BASELINE_COMMIT"))   uncommitted changes in old repo: $DIRTY"
LC_ALL=C sort -k2 "$MANIFEST" > "$TMP.old"; cp "$TMP" "$TMP.new"

lookup() {  # longest-prefix match in port-map.tsv -> "status<TAB>target"
  local path="$1"
  awk -F'\t' -v p="$path" '!/^#/ && NF>=3 { k=$1; if (p==k || (substr(k,length(k))=="/" && index(p,k)==1) || (substr(k,length(k))!="/" && index(p,k)==1 && length(k)>3)) { if (length(k)>best) {best=length(k); out=$2"\t"$3} } } END{ if(out=="") out="UNMAPPED\t(add a row to port-map.tsv)"; print out }' "$MAP"
}

changed=0
while IFS=' ' read -r blob path; do
  old_blob="$(awk -v p="$path" '$2==p{print $1}' "$TMP.old")"
  if [[ -z "$old_blob" ]]; then kind="ADDED   "
  elif [[ "$old_blob" != "$blob" ]]; then kind="MODIFIED"
  else continue; fi
  IFS=$'\t' read -r status target <<<"$(lookup "$path")"
  printf '%s %-55s -> [%s] %s\n' "$kind" "$path" "$status" "$target"; changed=$((changed+1))
done < "$TMP.new"

while IFS=' ' read -r blob path; do
  if ! awk -v p="$path" '$2==p{f=1} END{exit !f}' "$TMP.new"; then
    IFS=$'\t' read -r status target <<<"$(lookup "$path")"
    printf 'DELETED  %-55s -> [%s] %s   (remove/adjust the port too)\n' "$path" "$status" "$target"; changed=$((changed+1))
  fi
done < "$TMP.old"

[[ $changed -eq 0 ]] && echo "No tracked-file changes since baseline." || echo "$changed change(s). Port them, regenerate golden fixtures if backend/caption_ai.py or backend/captions.py changed (scripts/old-sync/generate_golden.py), then re-run with --accept."
