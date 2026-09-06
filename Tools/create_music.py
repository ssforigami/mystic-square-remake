"""Render independently transcribed TH05 scores with this project's own FM synthesizer.
No ZUN MIDI, game files, recordings, soundfonts, or samples are used.
"""
from pathlib import Path
import urllib.request, json, zipfile, io, hashlib, wave, re
import numpy as np
import mido

ROOT=Path(__file__).resolve().parents[1]
REF=ROOT/'Tools/MusicScores'; REF.mkdir(exist_ok=True)
OUT=ROOT/'MysticSquare/Assets/Resources/Music'; OUT.mkdir(parents=True,exist_ok=True)
DMBN={2:'de',5:'spirit',6:'roman',7:'mind',8:'maple',9:'forbidden',10:'crimson',11:'judas',12:'Judge',13:'Dmisery',15:'infinite',16:'Alice',17:'Grimoire',18:'jinjya',19:'endless',20:'kuon',22:'pr'}
OTHERS={1:'icebhm23230',3:'icebhm23230',4:'ryoya1295',14:'LeastLPZ',21:'CommonShitSandwich',23:'LeastLPZ'}
TITLES=['怪绮谈 ～ Mystic Square','Dream Express','魔法阵 ～ Magic Square','梦想时空','灵天 ～ Spiritual Heaven','Romantic Children','Plastic Mind','Maple Wise','禁忌的魔法 ～ Forbidden Magic','绯红的少女 ～ Crimson Dead!!','背叛的少女 ～ Judas Kiss','the Last Judgement','可悲的人偶 ～ Doll of Misery','世界的尽头 ～ World’s End','神话幻想 ～ Infinite Being','不可思议之国的爱丽丝','the Grimoire of Alice','神社','Endless','永远的乐园','Mystic Dream','Peaceful Romancer','魂之安眠之地']

def get(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'MysticSquareIndependentRebuild/1.0'}),timeout=30).read()

def notes(data):
    mid=mido.MidiFile(file=io.BytesIO(data)); t=0; active={}; result=[]; programs=[0]*16
    for msg in mid:
        t+=msg.time
        if msg.type=='program_change': programs[msg.channel]=msg.program
        if msg.type=='note_on' and msg.velocity:
            key=(msg.channel,msg.note)
            if key in active:
                start,vel,pg=active.pop(key);result.append((start,t-start,msg.note,vel,msg.channel,pg))
            active[key]=(t,msg.velocity,programs[msg.channel])
        elif msg.type=='note_off' or (msg.type=='note_on' and not msg.velocity):
            key=(msg.channel,msg.note)
            if key in active:
                start,vel,pg=active.pop(key);result.append((start,t-start,msg.note,vel,msg.channel,pg))
    for (ch,n),(start,vel,pg) in active.items():result.append((start,min(2,t-start),n,vel,ch,pg))
    assert len(result)>30,'Empty transcription'
    return result,max(t,max(a+b for a,b,*_ in result))

def render(data,path,index):
    ns,length=notes(data); rate=22050
    # Preserve all written notes and tempo. Only instrument synthesis is new.
    length=min(length,600); out=np.zeros((int((length+.8)*rate)+2,2),np.float32)
    rng=np.random.default_rng(index)
    for start,dur,n,vel,ch,pg in ns:
        if start>length: continue
        dur=min(max(dur,.025),12); release=.13; count=int((dur+release)*rate)
        tt=np.arange(count,dtype=np.float32)/rate; freq=440*2**((n-69)/12)
        env=np.minimum(tt/.006,1)*np.where(tt<dur,np.exp(-tt*.45),np.exp(-dur*.45)*(1-(tt-dur)/release))
        env=np.maximum(env,0)
        if ch==9:
            sig=(np.sin(2*np.pi*(70*tt+40*(1-np.exp(-tt*30)))) if n<40 else rng.uniform(-1,1,count)) * np.exp(-tt*20)
        else:
            phase=2*np.pi*freq*tt
            ratio=2 if n>=60 else 1
            indexFM=(1.1 if n>=60 else .7)*np.exp(-tt*1.3)
            sig=np.sin(phase+indexFM*np.sin(phase*ratio))*.8+np.sin(phase*2)*.12+np.sin(phase*.5)*.06
        sig=(sig*env*(vel/127)**.7*.15).astype(np.float32)
        at=int(start*rate); end=min(len(out),at+len(sig));sig=sig[:end-at]
        pan=.5+(.12 if n>=60 else -.12);out[at:end,0]+=sig*(1-pan);out[at:end,1]+=sig*pan
    # Quiet stereo ambience, synthesized from the same notes.
    delay=int(.113*rate);out[delay:,0]+=out[:-delay,1]*.13
    delay=int(.157*rate);out[delay:,1]+=out[:-delay,0]*.11
    peak=float(np.max(np.abs(out)));out*=.82/max(.1,peak)
    fade=int(.025*rate);out[:fade]*=np.linspace(0,1,fade)[:,None];out[-fade:]*=np.linspace(1,0,fade)[:,None]
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes((out*32767).astype('<i2').tobytes())
    return len(ns),round(len(out)/rate,2)

def main():
    listing=json.loads(get('https://api.github.com/repos/AyHa1810/touhou-midi-collection/contents/1%20-%20Shooter%20Games/5%20-%20Mystic%20Square'))
    manifest=[]
    for i in range(1,24):
        author='DMBN' if i in DMBN else OTHERS[i]
        if i in DMBN:
            url=f'https://www.easypianoscore.jp/download.php?musicName={DMBN[i]}&musicLevel=easy&ext=zip&inst='
            license_url='https://www.easypianoscore.jp/kenri.html'
        else:
            item=next(x for x in listing if x['name'].startswith(str(i)+'. ') and '('+author+')' in x['name'])
            assert '(ZUN)' not in item['name'] and '(unknown)' not in item['name']
            url=item['download_url'];license_url='https://github.com/AyHa1810/touhou-midi-collection#usage'
        score=REF/f'{i:02}.mid'
        if not score.exists():
            data=get(url)
            if i in DMBN:
                z=zipfile.ZipFile(io.BytesIO(data));names=[n for n in z.namelist() if n.lower().endswith('.mid')];assert len(names)==1;data=z.read(names[0])
            assert data[:4]==b'MThd';score.write_bytes(data)
        data=score.read_bytes(); audio=OUT/f'{i:02}.wav'
        count,seconds=render(data,audio,i)
        manifest.append(dict(track=i,title=TITLES[i-1],composer='ZUN',transcriber=author,source=url,usage=license_url,notes=count,seconds=seconds,score_sha256=hashlib.sha256(data).hexdigest(),audio_sha256=hashlib.sha256(audio.read_bytes()).hexdigest(),synthesis='Project-authored FM; no samples'))
        print(f'{i:02} {author}: {count} notes, {seconds}s',flush=True)
    (ROOT/'QA/music-provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    (ROOT/'MUSIC-CREDITS.md').write_text('# 音乐来源\n\n原曲作曲：ZUN。音频全部由本项目的 FM 合成器重新生成，没有使用原版音频、官方 MIDI 或采样音色。\n\n采用以下独立耳抄／转谱，保留旋律与谱面节奏，音色重新合成。DMBN 部分为钢琴 Easy 编曲，并非原版配器。\n\n'+ '\n'.join(f'- {m["track"]:02}. {m["title"]} — {m["transcriber"]}（[谱面来源]({m["source"]}) / [使用说明]({m["usage"]})）' for m in manifest)+'\n\nDMBN：東方ピアノEasyモード，非营利用途依作者公开指引使用并署名。其余转谱依收集库 Usage 指引署名。原始转谱仅保存在工程 Tools/MusicScores，发行包不包含 MIDI。\n',encoding='utf-8')

if __name__=='__main__':main()
