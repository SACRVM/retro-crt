#!/usr/bin/env bash
# POSIX counterpart to record-demo.ps1. See that file's header for context.
set -euo pipefail

COLS="${COLS:-80}"
ROWS="${ROWS:-24}"
THEME="${THEME:-monokai}"
SPEED="${SPEED:-1.0}"
FONT_SIZE="${FONT_SIZE:-14}"

cd "$(dirname "$0")/.."

CAST="docs/images/demo.cast"
GIF="docs/images/demo.gif"

mkdir -p "$(dirname "$CAST")"

echo "==> Building demo (Release)"
dotnet build samples/Retro.Crt.Demo -c Release --nologo >/dev/null

echo "==> Recording $CAST (${COLS}x${ROWS})"
asciinema rec "$CAST" \
    -c "dotnet run --project samples/Retro.Crt.Demo -c Release --no-build" \
    --overwrite \
    --idle-time-limit 1 \
    --cols "$COLS" --rows "$ROWS"

if command -v agg >/dev/null 2>&1; then
    echo "==> Rendering GIF $GIF (theme=$THEME, speed=${SPEED}x, font=$FONT_SIZE)"
    agg "$CAST" "$GIF" --theme "$THEME" --speed "$SPEED" --font-size "$FONT_SIZE"
    echo "==> Done."
    echo "Cast: $CAST"
    echo "GIF:  $GIF"
else
    echo "WARN: agg not found on PATH — skipping GIF render." >&2
    echo "      Install with: cargo install --git https://github.com/asciinema/agg" >&2
    echo "Cast: $CAST"
fi

echo
echo "Preview:    asciinema play $CAST"
echo "Re-record:  ./scripts/record-demo.sh"
