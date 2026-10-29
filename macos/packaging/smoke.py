"""Launch the packaged native application with a synthetic CNF. Never user data."""
import pathlib
import subprocess
import sys

exe, fixture, evidence = sys.argv[1:]
folder = pathlib.Path(evidence)
try:
    result = subprocess.run([exe, "--smoke-test", fixture, evidence], timeout=90, check=False)
except subprocess.TimeoutExpired:
    print("Native macOS UI smoke test timed out", file=sys.stderr)
    result = None
error = folder / "ui-smoke-error.txt"
if error.exists():
    print(error.read_text(), file=sys.stderr)
if result is None or result.returncode != 0 or not (folder / "ui-smoke-success.txt").exists():
    sys.exit(1)
for name in ("plot.png", "efficiency.png", "automatic-peaks.png", "activity.txt", "automatic-roi.txt"):
    if not (folder / name).is_file() or (folder / name).stat().st_size < 50:
        raise RuntimeError(f"Missing/empty smoke evidence: {name}")
print((folder / "ui-smoke-success.txt").read_text())
