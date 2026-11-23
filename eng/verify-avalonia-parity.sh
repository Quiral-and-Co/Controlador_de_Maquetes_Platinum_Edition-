#!/usr/bin/env bash
set -euo pipefail
status="docs/avalonia-parity-status.md"
test -f "$status" || { echo "Missing parity status: $status"; exit 1; }
expected=11
completed=$(grep -Ec '^- \[[xX]\] ' "$status" || true)
pending=$(grep -Ec '^- \[ \] ' "$status" || true)
if [[ "$completed" -ne "$expected" || "$pending" -ne 0 ]]; then
  echo "WPF retirement blocked: $completed/$expected parity gates completed."
  grep -E '^- \[ \] ' "$status" | sed 's/^- \[ \] /PENDING: /' || true
  exit 1
fi
if grep -RniE 'Presentation\.Wpf|System\.Windows|WindowsBase|PresentationCore' src/Platinum.Presentation.Avalonia --include='*.cs' --include='*.axaml' --include='*.csproj'; then
  echo "WPF dependency detected in Avalonia project."
  exit 1
fi
if grep -RniE 'System\.Text\.Json|Newtonsoft\.Json|JsonDocument|JsonSerializer|<script|\.html|\.js' src/Platinum.Presentation.Avalonia --include='*.cs' --include='*.axaml' --include='*.csproj'; then
  echo "JSON, HTML or JavaScript runtime dependency detected in Avalonia project."
  exit 1
fi
echo "Parity matrix passed and Avalonia source has no WPF or web/JSON runtime dependencies. Human review is still required before deleting WPF."
