#!/usr/bin/env bash
set -euo pipefail

cd "${CLAUDE_PROJECT_DIR:-.}"

summarize_file() {
  local path="$1"
  local label="$2"
  if [[ ! -f "$path" ]]; then
    echo "$label: file not found"
    return
  fi
  local open_lines high med low
  open_lines=$(grep -E '^- \[ \]' "$path" || true)
  high=$(printf '%s\n' "$open_lines" | grep -c '\[High\]' || true)
  med=$(printf '%s\n' "$open_lines" | grep -c '\[Medium\]' || true)
  low=$(printf '%s\n' "$open_lines" | grep -c '\[Low\]' || true)
  local total=$(( high + med + low ))
  echo "$label: $total open (High: $high, Medium: $med, Low: $low)"
  if [[ $total -gt 0 ]]; then
    printf '%s\n' "$open_lines" | grep '\[High\]'   | head -5 | sed 's/^/  /' || true
    printf '%s\n' "$open_lines" | grep '\[Medium\]' | head -3 | sed 's/^/  /' || true
  fi
}

echo "=== Session briefing ==="
echo
echo "Last commit:"
git log -1 --format='  %h  %s  (%ar, %an)' 2>/dev/null || echo "  (no commits yet)"
echo
summarize_file "PROBLEMS.md" "Problems"
echo
summarize_file "TODO.md" "Todos"
echo
echo "Open with a one-line greeting asking the user whether they want to:"
echo "  - tackle a tracked problem (offer the top-priority ones),"
echo "  - start a new feature (propose using the /plan skill),"
echo "  - pick up a todo, or"
echo "  - work on something else entirely."
