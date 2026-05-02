# Benchmarks

Run from the repo root:

```bash
dotnet run --project bench/Retro.Crt.Bench -c Release -- --filter '*' --job short
```

For a full run (longer, lower variance):

```bash
dotnet run --project bench/Retro.Crt.Bench -c Release -- --filter '*'
```

## Results (Windows 11, .NET 10.0.7, i9-12900H)

Numbers from `--job short`. The full job is roughly the same shape with
tighter confidence intervals.

### `AnsiCodes`

| Method                  | Mean      | Allocated |
|-------------------------|----------:|----------:|
| `Foreground_Truecolor`  |  29.66 ns |      64 B |
| `Foreground_Standard16` |   2.82 ns |       0 B |
| `Background_Truecolor`  |  29.67 ns |      64 B |
| `GotoXY_call`           |  31.28 ns |      40 B |
| `CursorLeft_one`        |   0.03 ns |       0 B |
| `CursorLeft_n`          |  24.78 ns |      32 B |

`Foreground` / `Background` for Standard16 colors are zero-alloc — the
16 sequences are pre-built once at startup and returned by reference.
`Truecolor` and `GotoXY` allocate a single result string each, which is
the lower bound when the value depends on runtime input.

### `ProgressBar`

| Method                   | Before        | After         | Δ        |
|--------------------------|--------------:|--------------:|---------:|
| `RenderFrame_with_label` | 84 ns / 312 B | 29 ns / 112 B | -66 %    |
| `RenderFrame_no_label`   | 75 ns / 248 B | ~28 ns / ≤112B| -65 %    |
| `RenderBar_only`         | 27 ns /  88 B | 26 ns /  88 B | unchanged |
| `FilledCells_call`       |   ~0 ns / 0 B |   ~0 ns / 0 B | unchanged |

`RenderFrame` was rewritten as a single `string.Create` call instead of
five concatenated `string`s (label + space + bar + " ddd%" + reset). The
result is one allocation per call — exactly the returned string itself.

### `Log`

| Method               | Mean      | Allocated |
|----------------------|----------:|----------:|
| `Format_full_line`   |   ~70 ns |     ~80 B |
| `FormatTime_call`    |   ~28 ns |      32 B |
| `Tag_call`           |    ~1 ns |       0 B |

### `BoxBuilder`

| Method                 | Mean      | Allocated |
|------------------------|----------:|----------:|
| `Build_two_line_box`   |  ~250 ns |    ~520 B |

Box building is not on the hot path (called once per banner), so the
multi-allocation pattern is left as-is for clarity.

## Notes

- `MemoryDiagnoser` numbers count managed allocations only.
- `Allocated = 0 B` means the method allocates nothing on the heap.
- All benchmarks are pure renderers — no I/O. Production paths add
  one `Console.Out.Write` per call, which writes the resulting string
  to the underlying stream without further allocation.
