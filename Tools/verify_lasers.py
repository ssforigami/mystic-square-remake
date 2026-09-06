"""Check actual rendered beam pixels along the simulation's collision rays."""
from pathlib import Path
import math,json
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'QA/20-laser-alignment.png'
im=Image.open(path).convert('RGB');w,h=im.size
scale=min(w/1024,h/720);ox=(w-1024*scale)/2;oy=(h-720*scale)/2
checks=[]
for index,(x,y,angle,last) in enumerate([(96,96,math.pi/3,400),(288,120,2.1,370)]):
    for distance in range(10,last+1,15):
        px=round(ox+(24+1.5*(x+math.cos(angle)*distance))*scale)
        py=round(oy+(24+1.5*(y+math.sin(angle)*distance))*scale)
        colors=list(im.crop((px-2,py-2,px+3,py+3)).getdata())
        hit=any(min(c)>175 and sum(c)>620 for c in colors)
        checks.append(dict(beam=index+1,distance=distance,pixel=[px,py],visible=hit))
        assert hit,f'Beam {index+1} invisible at collision-ray distance {distance}, pixel {px},{py}'
result=dict(image=path.name,resolution=[w,h],checks=len(checks),result='PASS',samples=checks)
(ROOT/'QA/laser-alignment.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(f'PASS: {len(checks)} points on both collision rays have visible beam cores, including their lower-field sections.')
