"""Retain NuGet dependency license/notices alongside the distributed macOS app."""
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

assets = json.loads(pathlib.Path(sys.argv[1]).read_text())
output = pathlib.Path(sys.argv[2])
output.mkdir(parents=True, exist_ok=True)
mit = """Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
"""
for identity, library in assets["libraries"].items():
    if library.get("type") != "package":
        continue
    root = next((pathlib.Path(p) / library["path"] for p in assets["packageFolders"]
                 if (pathlib.Path(p) / library["path"]).exists()), None)
    if root is None:
        raise RuntimeError("Missing dependency package: " + identity)
    prefix = identity.replace("/", "-")
    for file in root.iterdir():
        if file.is_file() and re.match(r"(?i)^(licen[sc]e|copying|notice|third.party.notices)", file.name):
            (output / (prefix + "-" + file.name)).write_bytes(file.read_bytes())
    spec = next(root.glob("*.nuspec"), None)
    if spec:
        metadata = ET.parse(spec).getroot()
        def value(name):
            return next((e.text or "" for e in metadata.iter() if e.tag.rsplit("}", 1)[-1] == name), "")
        if value("license") == "MIT":
            copyright = value("copyright") or ("Copyright " + value("authors"))
            (output / (prefix + "-MIT.txt")).write_text(identity + "\n" + copyright + "\n\n" + mit)
