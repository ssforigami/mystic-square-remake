"""New vector-drawn portraits and midbosses for this project; no image references imported."""
from PIL import Image,ImageDraw
import aggdraw,math
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]/'MysticSquare/Assets/Resources/Art'
S=2
def poly(im,points,fill,line='#211a2f',width=2):
 d=ImageDraw.Draw(im);p=[(int(x*S),int(y*S)) for x,y in points];d.polygon(p,fill);d.line(p+[p[0]],line,width*S) if line else None
def curve(im,points,fill,line='#211a2f',width=2):
 p=aggdraw.Path();p.moveto(points[0][0]*S,points[0][1]*S)
 for v in points[1:]:
  if len(v)==6:p.curveto(*[x*S for x in v])
  else:p.lineto(v[0]*S,v[1]*S)
 p.close();a=aggdraw.Draw(im);a.path(p,aggdraw.Brush(fill) if fill else None,aggdraw.Pen(line,width*S) if line else None);a.flush()
def ellipse(im,box,fill,line=None,width=1):ImageDraw.Draw(im).ellipse(tuple(int(x*S) for x in box),fill,line,width*S)
def line(im,pts,color,width=2):ImageDraw.Draw(im).line([(int(x*S),int(y*S)) for x,y in pts],color,width*S)

def portrait(name,hair,dress,kind,accent='#eee0d1'):
 im=Image.new('RGBA',(500*S,550*S));skin='#f2ccb9';shadow='#d5a4a5';gold='#d4b681'
 if kind=='shinki':
  for side in [-1,1]:
   for k in range(3):
    curve(im,[(250+side*30,270),(250+side*100,205+k*57,250+side*207,91+k*99,250+side*224,107+k*95),(250+side*187,235+k*57,250+side*139,300+k*42,250+side*27,320+k*20)],'#b9a8ce','#6a557d',1)
 if kind=='yuuka':
  curve(im,[(300,62),(351,37,436,67,475,141),(438,129,424,146,410,153),(378,139,365,154,350,167),(329,156,307,165,278,189),(262,133,265,88,300,62)],'#d9c8c4','#837081')
  line(im,[(329,69),(430,417)],'#bda480',5)
 # silhouette of flowing hair, with distinct lengths
 long=kind in ['mima','shinki','marisa','yumeko','sara']
 curve(im,[(170,116),(178,50,306,45,329,120),(356,177,325,245,358,305 if long else 222),(322,294 if long else 230,290,320 if long else 240,240,276),(186,305 if long else 235,149,265,156,218),(143,182,153,136,170,116)],hair)
 # body and skirt / cape
 if kind=='mima':
  curve(im,[(183,221),(142,267,166,362,65,481),(112,476,181,555,230,520),(335,565,345,462,438,469),(346,372,319,281,305,218)],'#283968')
  line(im,[(180,244),(130,447),(214,499)],gold,5);line(im,[(304,244),(359,445),(293,501)],gold,5)
 else:
  curve(im,[(185,234),(168,305,151,413,98,498),(163,477,174,548,238,518),(301,550,377,488,423,503),(372,412,323,300,305,230)],dress)
  poly(im,[(194,310),(162,487),(217,509),(238,337)],dress,None)
  for x in [169,210,297,338]:line(im,[(246+(x-245)*.4,332),(x,493)],'#72516b' if kind!='mai' else '#9aafc5',2)
  curve(im,[(103,491),(168,476,175,540,238,511),(298,542,376,481,418,496),(423,506),(377,503,301,558,238,526),(174,555,165,493,99,506)],accent)
 # arms, tunic
 curve(im,[(185,228),(153,226,129,246,99,299),(122,326,147,329,174,294),(196,263)],accent)
 curve(im,[(303,228),(343,236,351,277,389,257),(414,289),(370,347,333,319,309,293)],accent)
 curve(im,[(189,221),(213,231,275,224,298,218),(320,250,315,296,320,343),(271,365,220,358,176,340),(180,292)],dress)
 poly(im,[(195,223),(238,256),(220,286),(180,245)],accent)
 poly(im,[(286,219),(240,256),(265,279),(307,240)],accent)
 poly(im,[(231,258),(244,252),(254,312),(237,303)],gold if kind=='yuuka' else '#a94866')
 if kind in ['marisa','yumeko']:
  poly(im,[(211,288),(278,286),(310,449),(176,459)],accent)
  line(im,[(192,437),(297,432)],'#bf9eab',3)
 if kind=='yuuka':
  # waistcoat and trousers are plaid, not a skirt.
  poly(im,[(175,350),(248,343),(236,521),(159,515)],dress)
  poly(im,[(248,343),(320,347),(366,511),(279,523)],dress)
  for y in range(363,512,22):line(im,[(178,y),(232,y)],'#dda0a0',1);line(im,[(280,y),(322+(y-350)*.23,y)],'#dda0a0',1)
  for x in [188,212,290,311]:line(im,[(x,360),(x+(-10 if x<240 else 20),512)],'#dda0a0',1)
  for y in [283,307,332]:line(im,[(189,y),(307,y)],'#d98b94',1)
  for x in [203,277,295]:line(im,[(x,268),(x,341)],'#d98b94',1)
 # neck and face
 poly(im,[(218,181),(268,185),(280,231),(240,256),(208,228)],skin)
 curve(im,[(185,119),(208,79,281,84,307,130),(317,175,284,217,243,224),(216,221,181,181,185,119)],skin)
 ellipse(im,(177,153,196,181),skin);ellipse(im,(299,151,313,177),skin)
 curve(im,[(184,113),(205,74,280,76,304,109),(316,131),(291,124),(282,145),(272,112),(254,147),(246,115),(221,147),(218,121),(187,159)],hair)
 line(im,[(195,105),(224,87),(265,88)],'#ffffff44' if False else '#b895a1',2)
 # expressive angled eyes, cream specular points
 line(im,[(201,160),(214,155),(229,161)],'#392538',3);line(im,[(260,158),(276,151),(289,156)],'#392538',3)
 ellipse(im,(211,158,223,176),'#966275' if kind not in ['alice','mai'] else '#557596');ellipse(im,(270,155,281,173),'#966275' if kind not in ['alice','mai'] else '#557596')
 ellipse(im,(213,159,217,164),'#fff0db');ellipse(im,(272,156,276,161),'#fff0db')
 line(im,[(244,170),(241,184),(247,185)],shadow,1)
 curve(im,[(231,199),(241,204,254,202,260,195)],None,'#a36375',1)
 line(im,[(200,185),(217,188)],'#e9a7ad',2);line(im,[(270,181),(286,181)],'#e9a7ad',2)
 # side curls, asymmetry
 curve(im,[(173,129),(168,179,184,229,167,264),(195,247,205,223,197,188),(194,156)],hair)
 curve(im,[(301,121),(331,168,310,203,331,239),(300,233,294,209,298,174)],hair)
 if kind in ['marisa','yuki']:
  poly(im,[(115,119),(212,31),(239,7),(270,80),(326,110),(388,142),(291,132),(201,139)],dress)
  curve(im,[(173,109),(224,109,267,98,292,105),(305,118),(266,116,216,129,169,121)],accent)
  poly(im,[(196,116),(171,94),(156,117),(185,131)],accent)
 elif kind=='mima':
  poly(im,[(169,115),(221,38),(247,22),(279,96),(320,122)],'#2d3f7b');line(im,[(184,108),(291,110)],gold,6)
  line(im,[(396,94),(360,477)],gold,8);ellipse(im,(353,28,431,106),gold);ellipse(im,(372,14,437,83),(0,0,0,0))
 elif kind=='alice':
  line(im,[(177,118),(196,94),(238,87),(288,99),(309,126)],'#bf4c6c',7)
  poly(im,[(325,293),(410,279),(423,399),(332,414)],'#5b416c','#c3a069',4);line(im,[(336,299),(413,289)],'#eee0c0',4)
  line(im,[(367,314),(380,373)],gold,2);line(im,[(349,348),(399,337)],gold,2)
 elif kind=='yumeko':
  for x,y in [(175,110),(192,95),(212,87),(236,85),(260,88),(283,98),(299,114)]:ellipse(im,(x-8,y-8,x+12,y+13),accent,'#cbaeb9')
  poly(im,[(400,211),(430,151),(428,218),(412,340)],'#bfd0db','#6d718e');line(im,[(404,301),(427,307)],gold,5)
 elif kind=='shinki':
  curve(im,[(247,73),(240,33,276,31,279,10),(286,61,268,70,255,81)],hair)
 elif kind=='mai':
  poly(im,[(213,91),(186,68),(173,86),(206,103)],'#e1e8eb');poly(im,[(216,92),(242,66),(254,85),(223,104)],'#e1e8eb')
 # hands
 ellipse(im,(96,298,129,327),skin,'#694153');ellipse(im,(383,254,409,281),skin,'#694153')
 im.save(OUT/(name+'.png'))

portrait('portrait1','#d8ad64','#30283f','marisa')
portrait('portrait2','#439986','#34417d','mima')
portrait('portrait3','#66a07f','#a84461','yuuka')
for idx,hair,dress,kind in [(0,'#b77fa4','#b34460','sara'),(1,'#d5a562','#b64d7d','louise'),(2,'#dfb668','#5279b5','alice'),(3,'#d8a564','#35263e','yuki'),(4,'#dda963','#a7415d','yumeko'),(5,'#d7d0e2','#be4865','shinki'),(6,'#dfb668','#5279b5','alice'),(7,'#83aed3','#d8e0e5','mai')]:portrait('portraitBoss'+str(idx),hair,dress,kind)

# Unnamed original midboss types. Do not substitute a stage boss or assign fan names.
S=1
for stage in range(7):
 im=Image.new('RGBA',(48,56))
 if stage==0:
  ellipse(im,(3,6,45,48),'#493a62','#dbb487',2);ellipse(im,(10,13,38,41),'#78679a','#e1c5a4',2)
  for k in range(8):
   a=k*math.pi/4;line(im,[(24+math.cos(a)*12,27+math.sin(a)*12),(24+math.cos(a)*20,27+math.sin(a)*20)],'#d5ac79',2)
  ellipse(im,(17,20,31,33),'#dfa8b7');ellipse(im,(22,23,26,28),'#3b2949')
 elif stage==1:
  poly(im,[(24,3),(43,14),(39,41),(24,51),(8,40),(5,14)],'#695880','#d3b594',2);ellipse(im,(12,13,36,42),'#7fadc7','#e5dfcf',2);ellipse(im,(19,24,29,30),'#e9cfdf')
 elif stage==6:
  poly(im,[(8,4),(41,4),(41,51),(8,51)],'#e5d5c5','#8c5477',2)
  for x,y in [(14,13),(34,42)]:poly(im,[(x,y-4),(x+4,y),(x,y+4),(x-4,y)],'#be4968',None)
  poly(im,[(17,17),(21,13),(25,18),(29,12),(32,19),(29,29),(18,29)],'#b79868')
  ellipse(im,(18,25,30,37),'#dcaea9');poly(im,[(19,36),(30,35),(34,47),(14,47)],'#596793')
 else:
  hair=['','','#bbd27e','#a9cce0','#cab78d','#c8b9d7'][stage]
  ellipse(im,(10,5,37,34),hair,'#3e2e49');poly(im,[(17,25),(30,25),(43,48),(5,48)],['','','#56704f','#4278aa','#805680','#635071'][stage])
  ellipse(im,(16,13,31,29),'#efd2bd');line(im,[(18,21),(21,21)],'#433147',2);line(im,[(26,21),(29,21)],'#433147',2)
  poly(im,[(11,29),(1,21),(3,36),(15,37)],'#c7ccdf');poly(im,[(35,29),(47,21),(45,36),(33,37)],'#c7ccdf')
 im.save(OUT/f'mid{stage}.png')

# Newly composed shrine ending plate, not copied from the game's ending illustrations.
S=2;im=Image.new('RGB',(1024*S,720*S),'#26213a')
for y in range(720):line(im,[(0,y),(1024,y)],(38+int(y/40),33+int(y/70),58+int(y/70)),1)
ellipse(im,(65,54,343,332),'#745064')
poly(im,[(0,400),(150,225),(295,353),(446,197),(691,391),(900,254),(1024,414),(1024,720),(0,720)],'#332940',None)
poly(im,[(0,505),(380,412),(1024,573),(1024,720),(0,720)],'#221d31',None)
poly(im,[(590,262),(814,237),(941,318),(549,348)],'#584454');poly(im,[(542,341),(943,311),(925,333),(557,364)],'#b08380')
poly(im,[(589,365),(887,342),(887,527),(586,527)],'#503849');poly(im,[(651,383),(807,371),(807,527),(651,527)],'#211b2d')
for x in [605,830,873]:line(im,[(x,354),(x,548)],'#bc8c80',10)
for y in [539,558,579,603,630,661,698]:line(im,[(451-(y-539)*1.4,y),(1024,y)],'#614551',4)
line(im,[(627,380),(837,365)],'#c2a58b',3)
for x in [659,724,789]:poly(im,[(x,378),(x+13,389),(x+3,401),(x+13,413),(x,427),(x-6,411),(x+1,399),(x-8,389)],'#d9cbba',None)
im.save(OUT/'shrine.png')
print('Created 11 independent portraits, 7 midboss sprites, and one shrine plate.')
