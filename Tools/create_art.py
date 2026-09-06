"""Newly authored geometric artwork. No remote images or original game files."""
from PIL import Image, ImageDraw, ImageFilter
import aggdraw, math, random
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]/'MysticSquare/Assets/Resources/Art'
OUT.mkdir(parents=True,exist_ok=True)
S=2
def path(im, coords, fill, stroke='#20192f', width=2):
    p=aggdraw.Path()
    for op,vals in coords:
        getattr(p,op)(*[v*S for v in vals])
    c=aggdraw.Draw(im);c.path(p,aggdraw.Brush(fill) if fill else None,aggdraw.Pen(stroke,width*S) if stroke else None);c.flush()
def P(im,pts,fill,stroke='#20192f',width=2):
    path(im,[('moveto',pts[0])]+[('lineto',v) for v in pts[1:]]+[('close',[])],fill,stroke,width)
def E(im,box,fill,outline=None,width=1):ImageDraw.Draw(im).ellipse(tuple(int(v*S) for v in box),fill,outline,width*S)
def L(im,pts,fill,width=1):ImageDraw.Draw(im).line([(int(x*S),int(y*S)) for x,y in pts],fill,width*S)
def save(im,name):im.save(OUT/(name+'.png'))

# Full-bleed title plate: layered mountain silhouettes, a restrained cinnabar moon.
im=Image.new('RGB',(1024*S,720*S));px=im.load();rng=random.Random(5)
for y in range(720*S):
    for x in range(1024*S):
        r=math.sqrt(((x/S-330)/800)**2+((y/S-240)/700)**2)
        noise=rng.randrange(-2,3)
        px[x,y]=(max(8,int(40-r*23)+noise),max(6,int(31-r*18)+noise),max(12,int(57-r*24)+noise))
E(im,(110,30,532,452),'#5e344d');E(im,(122,42,520,440),None,'#9a666c')
for i in range(180):
    x=rng.randrange(32,990);y=rng.randrange(26,510);E(im,(x,y,x+1,y+1),'#aa8790')
for layer in range(4):
    y0=390+layer*63
    pts=[(-50,720),(-50,y0+100)]+[(x,y0+rng.randrange(-95,70)) for x in range(-40,1100,65)]+[(1100,720)]
    P(im,pts,['#393047','#30283d','#241f32','#171524'][layer],None)
# Distant gate and weathered steps.
P(im,[(8,444),(227,425),(219,438),(17,457)],'#493249',None)
P(im,[(38,452),(47,453),(45,624),(33,637)],'#402b40',None)
P(im,[(186,441),(195,440),(202,577),(190,581)],'#402b40',None)
L(im,[(14,478),(223,455)],'#563c50',5)
for i in range(14):L(im,[(20-i*5,618+i*9),(196+i*18,550+i*12)],'#36283b',2)
# Irregular vermilion paper fragments, each drawn for this plate.
for i in range(18):
    x=rng.randrange(35,580);y=rng.randrange(40,650)
    P(im,[(x,y),(x+9,y-3),(x+4,y+14),(x-3,y+16)],'#9d4d63',None)
save(im,'title')

# Reimu, PC-98 purple hair, red/white miko clothes. Hand-authored Bezier illustration.
im=Image.new('RGBA',(600*S,720*S))
# Rear hair and long locks.
path(im,[('moveto',(249,164)),('curveto',(172,158,144,234,166,322)),('curveto',(133,405,148,451,105,484)),('curveto',(180,479,189,430,205,407)),('curveto',(187,496,137,542,121,587)),('curveto',(208,568,262,511,272,433)),('curveto',(342,418,399,469,430,428)),('curveto',(354,428,377,343,336,291)),('curveto',(351,208,309,168,249,164)),('close',[])],'#37223e')
path(im,[('moveto',(202,253)),('curveto',(177,337,196,402,155,459)),('curveto',(217,421,211,338,230,299))],None,'#73435d',4)
# Legs.
P(im,[(255,502),(292,512),(263,652),(236,647)],'#e9bcaa')
P(im,[(316,506),(352,502),(387,638),(358,648)],'#f2d3be')
P(im,[(236,630),(265,637),(253,681),(212,687),(216,671)],'#eee5d8')
P(im,[(355,625),(384,621),(408,670),(371,680)],'#eee5d8')
P(im,[(214,674),(252,673),(248,693),(199,700),(194,693)],'#442b3e')
P(im,[(369,670),(407,660),(421,680),(378,693)],'#442b3e')
# Flowing skirt, geometric folds.
path(im,[('moveto',(252,344)),('curveto',(190,415,220,464,143,530)),('curveto',(188,518,207,573,246,543)),('curveto',(280,568,307,548,323,541)),('curveto',(359,567,408,522,438,531)),('curveto',(394,451,330,410,323,346)),('close',[])],'#ba3f58')
P(im,[(252,374),(206,521),(253,535),(279,417)],'#db6070',None)
P(im,[(295,387),(304,537),(324,526),(332,480)],'#842e49',None)
P(im,[(317,399),(385,524),(418,520)],'#dd6770',None)
path(im,[('moveto',(150,519)),('curveto',(184,513,213,558,246,530)),('curveto',(280,552,305,538,323,528)),('curveto',(354,553,392,515,430,520)),('lineto',(438,535)),('curveto',(398,531,365,574,322,549)),('curveto',(302,560,273,576,245,551)),('curveto',(208,580,182,529,143,538)),('close',[])],'#f2ded1')
# White blouse and side sleeves.
P(im,[(237,278),(302,281),(329,350),(256,382),(215,325)],'#f0ded0')
P(im,[(241,289),(260,328),(289,346),(258,365),(226,321)],'#d2a9af',None)
path(im,[('moveto',(220,288)),('curveto',(191,293,167,328,138,326)),('lineto',(108,381)),('curveto',(159,402,199,363,235,324)),('close',[])],'#f4e5d4')
P(im,[(113,355),(104,381),(141,399),(156,374)],'#bc3b55')
L(im,[(115,366),(145,380)],'#f4c6c1',2)
path(im,[('moveto',(301,293)),('curveto',(333,309,349,336,390,285)),('lineto',(423,320)),('curveto',(382,392,324,380,310,343)),('close',[])],'#eee0d5')
P(im,[(382,288),(399,278),(433,310),(420,330)],'#ba3f58')
# Neck, collar, front ties.
P(im,[(249,257),(276,255),(286,285),(262,307),(241,280)],'#f4cdbc')
P(im,[(238,276),(261,299),(249,325),(224,288)],'#fff0d8')
P(im,[(280,275),(261,299),(284,318),(301,290)],'#fff0d8')
P(im,[(257,299),(268,299),(279,353),(254,343)],'#ba3b54')
P(im,[(249,348),(324,334),(332,356),(259,375)],'#a9344e')
P(im,[(306,347),(347,329),(349,365),(318,355),(340,410),(312,391)],'#dc6270')
# Hand around gohei staff.
P(im,[(399,287),(414,269),(425,268),(432,281),(421,301)],'#f6d2bd')
L(im,[(394,412),(444,138)],'#352537',8);L(im,[(394,412),(444,138)],'#cfad87',4)
P(im,[(439,153),(479,168),(471,190),(505,200),(486,223),(515,241),(489,258),(472,238),(483,220),(459,207),(470,187),(433,174)],'#f5e9d8')
P(im,[(443,173),(436,199),(453,214),(436,235),(453,252),(433,280),(418,265),(434,247),(418,232),(436,211),(420,202),(428,170)],'#d8bdbe')
# Face profile, ear and bangs.
path(im,[('moveto',(229,196)),('curveto',(254,171,298,185,308,210)),('lineto',(306,239)),('lineto',(316,251)),('lineto',(305,257)),('curveto',(302,279,278,288,258,277)),('curveto',(242,265,223,238,229,196)),('close',[])],'#f5d3bf')
E(im,(229,238,248,258),'#e6adab','#513047',1)
path(im,[('moveto',(203,213)),('curveto',(194,183,221,164,257,165)),('curveto',(291,160,323,183,317,217)),('lineto',(302,230)),('lineto',(295,199)),('lineto',(281,225)),('lineto',(278,198)),('lineto',(253,233)),('lineto',(252,205)),('curveto',(249,239,238,265,226,276)),('lineto',(224,224)),('lineto',(211,250)),('close',[])],'#422940')
path(im,[('moveto',(214,206)),('curveto',(233,179,267,176,289,185))],None,'#926077',3)
L(im,[(274,239),(286,235),(294,239)],'#4c2c43',2)
E(im,(283,238,289,249),'#6c465e');E(im,(284,239,286,242),'#fff0df')
L(im,[(298,267),(289,268)],'#a56872',1)
L(im,[(262,258),(274,260)],'#e8a4a1',2)
# Ribbon, white trim.
P(im,[(250,168),(205,137),(191,169),(230,188),(249,180)],'#c4455b')
P(im,[(255,168),(281,134),(305,159),(275,184)],'#c4455b')
L(im,[(204,143),(197,167),(229,182)],'#f3d9ce',4)
L(im,[(283,140),(298,157),(276,178)],'#f3d9ce',4)
P(im,[(242,167),(260,163),(266,180),(248,184)],'#e77b7c')
# Foreground free hand.
P(im,[(110,367),(92,359),(85,367),(95,380),(111,384)],'#f6d4bd')
save(im,'reimu')

# Newly drawn game sprites. Crisp small shapes deliberately use nearest-neighbour scaling.
def sprite(name,hair,dress,kind='girl',accent='#f3e5d3'):
    global S
    old=S;S=1
    z=Image.new('RGBA',(40,48));d=ImageDraw.Draw(z)
    if kind=='shinki':
        for side in [-1,1]:
            for k in range(3):P(z,[(20+side*4,20+k*4),(20+side*20,9+k*9),(20+side*15,27+k*4)],'#c8b5df',None)
    E(z,(9,5,31,30),hair,'#1b1529')
    P(z,[(14,24),(26,24),(34,42),(6,42)],dress,'#1b1529',1)
    P(z,[(14,27),(6,24),(2,33),(11,36)],accent,'#1b1529',1)
    P(z,[(26,27),(34,24),(38,33),(29,36)],accent,'#1b1529',1)
    P(z,[(13,40),(17,40),(17,47),(12,47)],'#422d4a',None)
    P(z,[(24,40),(28,40),(29,47),(24,47)],'#422d4a',None)
    E(z,(13,12,28,27),'#f2d1b8','#37253e')
    P(z,[(11,12),(17,8),(28,10),(29,18),(23,15),(20,19),(18,14),(12,21)],hair,None)
    d=ImageDraw.Draw(z);d.point((17,21),'#34253d');d.point((24,21),'#34253d')
    if kind=='reimu':
        P(z,[(18,9),(9,3),(8,11),(18,12)],'#d84963','#eadaca',1)
        P(z,[(21,9),(31,3),(32,11),(21,12)],'#d84963','#eadaca',1)
        L(z,[(13,38),(28,38)],'#f7ead4',2)
    elif kind=='witch':
        P(z,[(7,12),(19,0),(26,10),(36,14),(4,15)],dress,'#1c172a',1)
        L(z,[(14,10),(28,12)],accent,2)
    elif kind=='mima':
        E(z,(29,2,39,12),'#f2d385');E(z,(33,0,41,9),(0,0,0,0));L(z,[(32,12),(32,39)],'#dfca93',1)
        P(z,[(8,40),(31,40),(23,46),(14,43),(10,46)],'#80cfc3',None)
    elif kind=='alice':
        L(z,[(11,11),(27,10)],'#d56271',2);P(z,[(25,28),(37,27),(38,39),(27,40)],'#775287','#e1c689',1)
    elif kind=='maid':L(z,[(11,10),(16,6),(27,8),(30,12)],'#fff1da',3);P(z,[(16,27),(25,27),(29,39),(12,39)],accent,None)
    z.save(OUT/(name+'.png'));S=old
sprite('player0','#67426f','#ca4860','reimu')
sprite('player1','#e7ba62','#322840','witch')
sprite('player2','#4eaa98','#303e82','mima')
sprite('player3','#72a274','#ba4568')
sprite('boss0','#b27394','#b94554')
sprite('boss1','#e2b167','#ae4673')
sprite('boss2','#e4bd64','#557bd0','alice')
sprite('boss3','#e3b06e','#39253f','witch')
sprite('mai','#77afdb','#dbe3e7','girl','#e5eeed')
sprite('boss4','#e7b867','#a23d55','maid')
sprite('boss5','#e0d7e9','#c54a67','shinki')
sprite('boss6','#e9bd6a','#5479c5','alice')
sprite('enemy','#9ea7d5','#7e5b9a','girl','#a5a8d6')

# Tileable vertical scenery, each stage uses a separately drawn material language.
for stage in range(7):
    S=2;w,h=384,448
    palette=[('#211b2b','#403140'),('#181e32','#363457'),('#251c30','#564051'),('#172333','#344b61'),('#221d2b','#493544'),('#291c30','#663847'),('#221f37','#49415d')][stage]
    b=Image.new('RGB',(w*S,h*S),palette[0]);d=ImageDraw.Draw(b);r=random.Random(80+stage)
    if stage==0:
        for edge in [0,384]:
            for y in range(-50,500,42):
                width=r.randint(22,55);P(b,[(edge,y),(edge+(width if edge==0 else -width),y+12),(edge+(width*.7 if edge==0 else -width*.7),y+55),(edge,y+75)],palette[1],'#2c2435',1)
        for y in range(-30,500,48):L(b,[(66,y),(318,y)],'#2e2736',1)
        for x in [98,160,224,286]:L(b,[(x,0),(x,448)],'#2a2331',1)
    elif stage==1:
        for y in range(-100,550,120):
            for rad in [40,62,84]:E(b,(192-rad,y-rad,192+rad,y+rad),None,palette[1])
            for a in range(0,360,60):L(b,[(192+84*math.cos(math.radians(a)),y+84*math.sin(math.radians(a))),(192+84*math.cos(math.radians(a+120)),y+84*math.sin(math.radians(a+120)))],'#2a2a45',1)
    elif stage in [2,5]:
        for side in [0,1]:
            for y in range(-40,500,74):
                width=r.randint(35,85);x=0 if side==0 else 384-width
                P(b,[(x,y),(x+width,y),(x+width,y+67),(x,y+67)],palette[1],'#181b2b')
                for xx in range(x+6,x+width-6,14):
                    for yy in range(y+8,y+65,20):P(b,[(xx,yy),(xx+4,yy),(xx+4,yy+7),(xx,yy+7)],'#754c5d' if stage==5 else '#74606c',None)
        if stage==5:
            for i in range(30):
                x=r.choice([r.randint(0,55),r.randint(329,384)]);y=r.randrange(448);P(b,[(x,y+30),(x-9,y+10),(x-2,y+15),(x+4,y-15),(x+10,y+20)],'#915060',None)
    elif stage==3:
        for edge in [0,384]:
            for y in range(-50,500,64):
                x=edge+(r.randint(10,45) if edge==0 else -r.randint(10,45));P(b,[(edge,y-25),(x,y),(x+(-15 if edge==0 else 15),y+55),(edge,y+75)],palette[1],'#526c7c',1)
        for i in range(50):
            x=r.randrange(384);y=r.randrange(448);L(b,[(x,y),(x+2,y+2)],'#526374',1)
    else:
        for y in range(-80,540,120):
            for x in [5,327]:
                P(b,[(x,y),(x+50,y),(x+50,y+100),(x,y+100)],palette[1],'#75616a',1)
                L(b,[(x+10,y+10),(x+10,y+90),(x+40,y+90),(x+40,y+10)],'#292438',2)
        for y in range(0,448,32):L(b,[(62,y),(322,y)],'#30283b',1)
        for x in [100,160,224,284]:L(b,[(x,0),(x,448)],'#30283b',1)
    save(b,'stage'+str(stage))
print('Authored 22 PNG assets; no external media used.')
