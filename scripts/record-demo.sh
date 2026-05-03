#!/usr/bin/env bash
# POSIX counterpart to record-demo.ps1. Usage:
#   ./scripts/record-demo.sh                # default: tour
#   ./scripts/record-demo.sh themes
#   ./scripts/record-demo.sh matrix
#   ./scripts/record-demo.sh boot
set -euo pipefail

DEMO="${1:-tour}"
COLS="${COLS:-80}"
ROWS="${ROWS:-24}"
THEME="${THEME:-monokai}"
SPEED="${SPEED:-1.0}"
FONT_SIZE="${FONT_SIZE:-14}"

case "$DEMO" in
    tour)   PROJECT="samples/Retro.Crt.Demo" ;;
    themes) PROJECT="samples/Retro.Crt.Themes.Demo" ;;
    matrix) PROJECT="samples/Retro.Crt.Matrix.Demo" ;;
    boot)   PROJECT="samples/Retro.Crt.Boot.Demo" ;;
    *)
        echo "ERROR: unknown demo '$DEMO'. Choose one of: tour, themes, matrix, boot." >&2
        exit 2
        ;;
esac

cd "$(dirname "$0")/.."

CAST="docs/images/$DEMO.cast"
GIF="docs/images/$DEMO.gif"

mkdir -p "$(dirname "$CAST")"

echo "==> Building $PROJECT (Release)"
dotnet build "$PROJECT" -c Release --nologo >/dev/null

echo "==> Recording $CAST (${COLS}x${ROWS})"
asciinema rec "$CAST" \
    -c "dotnet run --project $PROJECT -c Release --no-build" \
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
echo "Re-record:  ./scripts/record-demo.sh $DEMO"
