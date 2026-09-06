"""Package the tested Windows build and its notices for GitHub Releases."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import json
import struct
import subprocess

ROOT = Path(__file__).resolve().parents[1]
RELEASE = ROOT / 'Release'
STAGING = ROOT / '.build' / 'release'
STAGING.mkdir(parents=True, exist_ok=True)
RELEASE.mkdir(parents=True, exist_ok=True)
(ROOT / 'QA').mkdir(parents=True, exist_ok=True)
ARCHIVE = RELEASE / 'MysticSquare-1.0-Windows-x64.zip'
SINGLE = RELEASE / 'MysticSquare-1.0-OneFile.exe'
DOCS = ['README.md', 'NOTICE.md', 'MUSIC-CREDITS.md', 'RELEASE-NOTES.md']

files = sorted(p for p in (ROOT / 'Build').rglob('*') if p.is_file())
files += [ROOT / name for name in DOCS + ['开始游戏.cmd']]
with ZipFile(ARCHIVE, 'w', ZIP_DEFLATED, compresslevel=6) as archive:
    for path in files:
        archive.write(path, path.relative_to(ROOT).as_posix())
with ZipFile(ARCHIVE) as archive:
    assert archive.testzip() is None
    for name in DOCS:
        assert '仅供交流学习使用' in archive.read(name).decode('utf-8') or name == 'MUSIC-CREDITS.md'
    assert 'Build/MysticSquare_Data/Managed/Assembly-CSharp.dll' in archive.namelist()
    assert not any(name.endswith(('.mid', '.mp4', '.log')) for name in archive.namelist())

stub = STAGING / 'OneFileLauncher.exe'
compiler = Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')
subprocess.run([str(compiler), '/nologo', '/target:winexe', '/platform:anycpu',
                '/optimize+', f'/out:{stub}', '/reference:System.IO.Compression.FileSystem.dll',
                '/reference:System.Windows.Forms.dll', str(ROOT / 'Tools/OneFileLauncher.cs')], check=True)
payload = ARCHIVE.read_bytes()
marker = b'MYSTIC_SQUARE_PAYLOAD_V1'
SINGLE.write_bytes(stub.read_bytes() + marker + payload + struct.pack('<q', len(payload)))
packed = SINGLE.read_bytes()
size = struct.unpack('<q', packed[-8:])[0]
assert packed[-8-size:-8] == payload
assert packed[-8-size-len(marker):-8-size] == marker

manifest = []
for path in [SINGLE, ARCHIVE]:
    manifest.append({'file': path.name, 'bytes': path.stat().st_size,
                     'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
(RELEASE / 'SHA256SUMS.txt').write_text(''.join(f'{m["sha256"]}  {m["file"]}\n' for m in manifest), encoding='ascii')
(ROOT / 'QA' / 'github-package-verification.json').write_text(
    json.dumps({'version':'1.0.0', 'notice_embedded':True, 'archive_crc':'PASS',
                'embedded_zip_matches':True, 'artifacts':manifest}, indent=2), encoding='utf-8')
print(json.dumps(manifest, indent=2))
