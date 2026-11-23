#!/usr/bin/env bash
set -euo pipefail
status="docs/avalonia-parity-status.json"
test -f "$status" || { echo "Missing parity status: $status"; exit 1; }
if ! jq -e 'all(.[]; . == true)' "$status" >/dev/null; then
  echo "WPF retirement blocked: one or more functional parity gates are still false."
  jq -r 'to_entries[] | select(.value != true) | "PENDING: " + .key' "$status"
  exit 1
fi
if grep -RniE 'Presentation\.Wpf|System\.Windows|WindowsBase|PresentationCore' src/Platinum.Presentation.Avalonia --include='*.cs' --include='*.axaml' --include='*.csproj'; then
  echo "WPF dependency detected in Avalonia project."
  exit 1
fi
echo "Parity matrix passed and Avalonia source has no WPF references. Human review is still required before deleting WPF."
