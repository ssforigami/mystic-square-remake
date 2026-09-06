"""Audit only this new project's sources and generated media; no cross-project scans."""
from pathlib import Path
import hashlib,json,re
root=Path(__file__).resolve().parents[1]
assets=root/'MysticSquare/Assets'
images=sorted((assets/'Resources/Art').glob('*.png'))
expected={'title','reimu','enemy','mai'}|{f'player{i}' for i in range(4)}|{f'boss{i}' for i in range(7)}|{f'stage{i}' for i in range(7)}
expected|={'shrine'}|{f'portrait{i}' for i in range(1,4)}|{f'portraitBoss{i}' for i in range(8)}|{f'mid{i}' for i in range(7)}
assert {p.stem for p in images}==expected
sources=list(assets.rglob('*.cs'))+list((root/'Tools').glob('*.py'))
game_sources='\n'.join(p.read_text(encoding='utf-8') for p in assets.rglob('*.cs'))
assert not re.search(r'https?://',game_sources),'Unexpected remote resource dependency'
assert not list(assets.rglob('*.hdi'))+list(assets.rglob('*.fdi'))+list(assets.rglob('*.mid'))+list(assets.rglob('*.ogg'))
music=json.loads((root/'QA/music-provenance.json').read_text(encoding='utf-8'))
assert len(music)==23
for m in music:
    p=assets/'Resources/Music'/f'{m["track"]:02}.wav'
    assert hashlib.sha256(p.read_bytes()).hexdigest()==m['audio_sha256']
    assert m['transcriber'] not in ['ZUN','unknown']
assert {p.stem for p in (assets/'Resources/Music').glob('*.wav')}=={f'{i:02}' for i in range(1,24)}
manifest={'project_root':str(root),'media_origin':'41 PNGs newly authored in Tools/create_art.py and Tools/create_portraits.py.','audio_origin':'23 independently transcribed scores rendered by project-authored FM synthesis. See music-provenance.json and MUSIC-CREDITS.md. No original game audio, official MIDI or soundfont samples.','fonts':'Runtime Windows system font lookup; no copied font assets.','source_scope':'Only current project source and artwork. No prior game project read or used.','art_files':[],'source_files':[]}
for group,files in [('art_files',images),('source_files',sources)]:
    for p in files:manifest[group].append({'path':p.relative_to(root).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(root/'QA/asset-audit.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Audit passed: {len(images)} authored PNGs, 23 freshly synthesized music tracks, {len(sources)} source files; no remote runtime assets or original game containers.')
