using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticSquare {
public enum RunState { Playing, StageClear, GameOver, Complete }
public sealed class Shot {
    public Vector2 p,v; public float radius=3, damage=1, age,acceleration,curve,burstAt; public int style,bounces,tint=-1; public bool grazed, homing;
}
public sealed class Foe {
    public Vector2 p; public float hp,maxHp,age,originX; public int kind, tick, side,attack=-1; public bool boss,mid;
}
public sealed class Drop { public Vector2 p,v; public int kind; public float age; }
public sealed class Spark { public Vector2 p,v; public float life,maxLife; public int color; }
public sealed class Laser { public Vector2 p; public float angle,length=520,width=7,age,warn=.8f,duration=1.5f,turn;public int color;public bool tracking,grazed;public bool Active=>age>=warn&&age<warn+duration; }
public struct Controls { public Vector2 move; public bool fire,focus,bomb; }

// Entire simulation is authored for this project. Coordinates use a 384 x 448 playfield.
public sealed partial class GameModel {
    public static readonly string[] Names={"博丽灵梦","雾雨魔理沙","魅魔","幽香"};
    public static readonly string[] BossNames={"萨拉","露易兹","爱丽丝","雪 ＆ 舞","梦子","神绮","爱丽丝"};
    public static readonly string[] StageNames={"万物神気","魔空間","魔界","凍てつく世界で","最後の審判","神戦","絵本のとびら開いて"};
    public static readonly string[] StageEnglish={"Materialization","Border Space","Devil's World","Ice Dream","Judgement Day","Dream Battle","Open Sesame"};
    public static readonly string[] Difficulties={"EASY","NORMAL","HARD","LUNATIC","EXTRA"};
    public readonly List<Shot> bullets=new List<Shot>(2000), shots=new List<Shot>(160);
    public readonly List<Foe> foes=new List<Foe>();
    public readonly List<Drop> drops=new List<Drop>();
    public readonly List<Spark> sparks=new List<Spark>();
    public readonly List<Laser> lasers=new List<Laser>();
    public Vector2 player=new Vector2(192,382);
    public RunState state=RunState.Playing;
    public int character,difficulty,stage,lives=3,bombs=3,power,points,totalPoints,graze,continues,phase,frame,stageFrame;
    public int stageMisses,stageBombs,initialLives=3,initialBombs=3, powerValue=10;
    public long score,clearBonus; public float dream,invincible=2,bombTime,deathWindow,bossTime,transition;
    public float rank; public bool bossStarted,midStarted,practice,extra,focus,missPending,timedOut;
    public bool godMode,usedGodMode;
    public string notice=""; public float noticeTime;
    public Action<string> sound;
    private int fireTick;
    private System.Random random=new System.Random(19981230);
    public float StageTime => stageFrame/60f;
    public bool HasBoss => foes.Exists(e=>e.boss);
    public bool AliceBarrier=>stage==2&&phase<2&&HasBoss&&foes.Find(e=>e.boss).tick%600>=390;
    public int LastPhase=>new[]{2,3,2,4,5,8,8}[stage];
    public int PowerTier { get { int n=0; foreach(int p in new[]{11,15,31,47,71,95,127}) if(power>=p)n++; return n; } }
    public GameModel(int character=0,int difficulty=1,int stage=0,bool practice=false,int lives=3,int bombs=3) {
        this.character=character;this.difficulty=difficulty;this.stage=stage;this.practice=practice;
        extra=stage==6; if(extra)this.difficulty=4;
        initialLives=this.lives=lives;initialBombs=this.bombs=bombs;
        if(practice||extra)power=127;
    }
    float R(float lo,float hi)=>(float)(lo+random.NextDouble()*(hi-lo));
    public void Tick(Controls input) {
        const float dt=1f/60;
        frame++; focus=input.focus;
        for(int i=sparks.Count-1;i>=0;i--){var s=sparks[i];s.p+=s.v*dt;s.life-=dt;if(s.life<=0)sparks.RemoveAt(i);}
        noticeTime=Mathf.Max(0,noticeTime-dt);
        if(state==RunState.StageClear){transition-=dt;if(transition<=0)NextStage();return;}
        if(state!=RunState.Playing)return;
        if(godMode){usedGodMode=true;missPending=false;}
        stageFrame++;invincible=Mathf.Max(0,invincible-dt);bombTime=Mathf.Max(0,bombTime-dt);
        if(input.bomb)Bomb();
        if(missPending){deathWindow-=dt;if(deathWindow<=0)LoseLife();}
        if(state!=RunState.Playing)return;
        float speed=new[]{155f,185f,220f,145f}[character]*(focus?.42f:1);
        player+=Vector2.ClampMagnitude(input.move,1)*speed*dt;
        player=new Vector2(Mathf.Clamp(player.x,9,375),Mathf.Clamp(player.y,18,436));
        if(input.fire&&fireTick<=0){Fire();fireTick=character==2?3:5;}fireTick--;
        if(!bossStarted)StageScript();
        if(bossStarted&&HasBoss){bossTime-=dt;if(bossTime<=0){timedOut=true;EndPhase(true);}}
        bool normalEnemies=false;
        for(int i=foes.Count-1;i>=0;i--){
            var e=foes[i];e.age+=dt;e.tick++;
            if(e.boss){
                float tx=192+Mathf.Sin(e.age*.65f+phase)*86;
                if(stage==3&&phase==0)tx=(e.side==0?108:276)+Mathf.Sin(e.age*.6f+e.side*3)*48;
                e.p=Vector2.Lerp(e.p,new Vector2(tx,76+Mathf.Sin(e.age*.8f)*16),.025f);
                BossPattern(e);
            } else if(e.mid){
                e.p=Vector2.Lerp(e.p,new Vector2(192+Mathf.Sin(e.age)*90,85),.025f);
                MidPattern(e);
                if(e.age>19){foes.RemoveAt(i);CancelBullets(false);}
            } else {
                normalEnemies=true;MoveEnemy(e);
                if(e.p.y>490||e.p.y< -90||e.p.x< -90||e.p.x>474||e.age>15)foes.RemoveAt(i);
            }
        }
        if(normalEnemies)AddDream(.014f*foes.Count);
        rank=Mathf.Min(1,rank+.00001f);
        for(int i=shots.Count-1;i>=0;i--){
            Shot s=shots[i];s.age+=dt;
            if(s.homing&&foes.Count>0){Foe closest=foes[0];float dist=999999;foreach(var e in foes){float d=(e.p-s.p).sqrMagnitude;if(d<dist){dist=d;closest=e;}}s.v=Vector2.Lerp(s.v,(closest.p-s.p).normalized*370,.09f);}
            s.p+=s.v*dt;bool hit=false;
            if(AliceBarrier&&s.p.y<150&&s.p.y>130&&Mathf.Abs(s.p.x-foes[0].p.x)<90){Bullet(s.p,Mathf.PI/2+(s.p.x-foes[0].p.x)*.007f,105,2);hit=true;}
            if(!hit)foreach(var b in bullets)if(b.style==5&&(s.p-b.p).sqrMagnitude<196){hit=true;break;}
            if(!hit)for(int j=foes.Count-1;j>=0;j--){var e=foes[j];if((s.p-e.p).sqrMagnitude<Mathf.Pow(e.boss?19:e.mid?17:13,2)){e.hp-=s.damage;score+=5;hit=true;Burst(s.p,2,1);if(e.hp<=0)Kill(e);break;}}
            if(hit||s.p.y< -20||s.p.x< -30||s.p.x>414||s.age>3)shots.RemoveAt(i);
        }
        if(bombTime>0){
            bullets.Clear();lasers.Clear();
            for(int i=foes.Count-1;i>=0;i--){var e=foes[i];if(character!=1||(Mathf.Abs(e.p.x-player.x)<90&&e.p.y<player.y))e.hp-=new[]{2.5f,7f,6f,3f}[character];if(e.hp<=0)Kill(e);}
        }
        for(int i=bullets.Count-1;i>=0;i--){
            Shot b=bullets[i];b.age+=dt;
            if(b.curve!=0)b.v=Rotate(b.v,b.curve*dt);
            if(b.acceleration!=0)b.v+=b.v.normalized*b.acceleration*dt;
            b.p+=b.v*dt;
            if(b.bounces>0&&(b.p.x<6||b.p.x>378)){b.v.x=-b.v.x;b.p.x=Mathf.Clamp(b.p.x,6,378);b.bounces--;}
            if(b.burstAt>0&&b.age>b.burstAt){Ring(b.p,8+difficulty*2,95+difficulty*8,b.age,1);bullets.RemoveAt(i);continue;}
            if(b.p.x< -28||b.p.x>412||b.p.y< -70||b.p.y>478){bullets.RemoveAt(i);continue;}
            float d=(b.p-player).sqrMagnitude;
            if(b.style!=5&&!b.grazed&&d<225&&graze<999){b.grazed=true;graze++;score+=new[]{250,500,1000,2000,5000}[difficulty];Burst(player,2,2);}
            if(!godMode&&invincible<=0&&!missPending&&d<(b.radius+2.3f)*(b.radius+2.3f)){missPending=true;deathWindow=8f/60;sound?.Invoke("danger");}
        }
        for(int i=lasers.Count-1;i>=0;i--){var l=lasers[i];l.age+=dt;if(l.tracking&&l.age<l.warn*.7f)l.angle=Mathf.Atan2(player.y-l.p.y,player.x-l.p.x);if(l.Active)l.angle+=l.turn*dt; if(l.age>=l.warn+l.duration){lasers.RemoveAt(i);continue;}if(!l.Active)continue;Vector2 dir=new Vector2(Mathf.Cos(l.angle),Mathf.Sin(l.angle));Vector2 near=l.p+dir*Mathf.Clamp(Vector2.Dot(player-l.p,dir),0,l.length);float d=Vector2.Distance(near,player);if(!godMode&&invincible<=0&&!missPending&&d<l.width*.5f+2.3f){missPending=true;deathWindow=8f/60;sound?.Invoke("danger");}}
        if(!godMode&&invincible<=0&&!missPending)foreach(var e in foes)if((e.p-player).sqrMagnitude<144){missPending=true;deathWindow=8f/60;break;}
        for(int i=drops.Count-1;i>=0;i--){var d=drops[i];d.age+=dt;d.v.y=Mathf.Min(65,d.v.y+dt*110);d.p+=d.v*dt;if(bombTime>0)d.p=Vector2.MoveTowards(d.p,player,340*dt);if((d.p-player).sqrMagnitude<196){Collect(d.kind);drops.RemoveAt(i);}else if(d.p.y>466){if(d.kind==1&&dream<128)dream=Mathf.Max(0,dream-3);drops.RemoveAt(i);}}
    }
    static Vector2 Rotate(Vector2 v,float a)=>new Vector2(v.x*Mathf.Cos(a)-v.y*Mathf.Sin(a),v.x*Mathf.Sin(a)+v.y*Mathf.Cos(a));
    public void StartBoss(){
        foes.Clear();CancelBullets(false);bossStarted=true;phase=0;bossTime=45;
        int count=stage==3?2:1;
        for(int i=0;i<count;i++){float hp=BossHP();foes.Add(new Foe{p=new Vector2(count==2?118+i*148:192,-25),hp=hp,maxHp=hp,boss=true,side=i});}
        Notice(GameModel.BossNames[stage]);sound?.Invoke("boss");
    }
    float BossHP()=>new[]{680,810,960,1500,1100,1150,1250}[stage]*(.85f+difficulty*.13f);
    void Fire(){
        int tier=PowerTier;float damage=new[]{1.2f,1.8f,1.7f,1.25f}[character];
        for(int i=-1;i<=1;i+=2)shots.Add(new Shot{p=player+new Vector2(i*4,-10),v=new Vector2(0,-420),damage=damage,style=character});
        if(character==3&&tier>=5)for(int i=-1;i<=1;i++)shots.Add(new Shot{p=player+new Vector2(i*8,-12),v=new Vector2(i*28,-420),damage=damage,style=character});
        int options=1+tier/2;
        for(int i=0;i<options;i++)for(int side=-1;side<=1;side+=2){float x=side*(10+i*5);Vector2 v=new Vector2(character==3?side*(65+i*42):character==0?side*50:side*(5+i*6),-370);shots.Add(new Shot{p=player+new Vector2(x,-4),v=v,damage=damage*(.55f+tier*.045f),homing=character==0,style=character,radius=2.5f});}
        if(frame%10==0)sound?.Invoke("shot");
    }
    public void Bullet(Vector2 p,float angle,float speed,int style=0){if(bullets.Count>=2200)return;bullets.Add(new Shot{p=p,v=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*speed,style=style,radius=style==3?3.2f:3});}
    public void Ring(Vector2 p,int count,float speed,float offset,int style){for(int i=0;i<count;i++)Bullet(p,offset+i*Mathf.PI*2/count,speed,style);}
    void Kill(Foe e){
        Burst(e.p,e.boss?38:12,e.side);sound?.Invoke(e.boss?"phase":"hit");
        if(e.boss){
            if(stage==3&&phase==0){foes.Remove(e);phase=1;bossTime=45;foreach(var other in foes){other.hp=other.maxHp=BossHP()*1.3f;other.tick=0;other.age=0;}CancelBullets(true);return;}
            EndPhase(false);return;
        }
        foes.Remove(e);score+=e.mid?25000:400;
        if(e.mid){CancelBullets(true);for(int i=0;i<8;i++)SpawnDrop(e.p+new Vector2(R(-30,30),R(-10,10)),i==0?2:i%2);if(stage==2||stage==4)SpawnDrop(e.p,4);else SpawnDrop(e.p,3);}
        else {SpawnDrop(e.p,bombTime>0?0:random.Next(0,2));if(random.NextDouble()<.32)SpawnDrop(e.p+new Vector2(10,0),0);}
    }
    public void EndPhase(bool timeout){
        if(!HasBoss)return;
        bool last=phase>=LastPhase;
        Vector2 p=foes.Find(e=>e.boss).p;
        if(extra&&!timeout&&phase<LastPhase)for(int i=0;i<Mathf.Min(35,bullets.Count);i++)SpawnDrop(bullets[i].p,1);
        CancelBullets(!timeout);phase++;if(timeout)rank=Mathf.Max(0,rank-.08f);
        if(last){foes.Clear();if(!timeout)score+=50000;FinishStage(timeout);return;}
        if(stage==3&&phase==1&&foes.Count>1)foes.RemoveAt(0);
        foreach(var e in foes){e.hp=e.maxHp=BossHP();e.tick=0;e.age=0;e.attack=-1;}
        bossTime=45;timedOut=false;
        if(!timeout){score+=10000;for(int i=0;i<5;i++)SpawnDrop(p+new Vector2((i-2)*12,0),power==127?1:i==0?2:0);}
    }
    public void SpawnDrop(Vector2 p,int kind){drops.Add(new Drop{p=p,v=new Vector2(R(-12,12),-38),kind=kind});}
    public void Collect(int kind){
        sound?.Invoke("item");
        if(kind==0||kind==2||kind==6){int before=power;power=Mathf.Min(127,power+(kind==2?10:kind==6?127:1));if(kind==6)score+=1000;else if(before<127)score+=10;else{score+=kind==2&&powerValue==12800?25600:powerValue;if(powerValue==12800)AddDream(2);for(int n=0;n<(kind==2?5:1);n++)powerValue=NextPowerValue(powerValue);rank=Mathf.Min(1,rank+.002f);}if(before<127&&power==127){CancelBullets(false);Notice("POWER MAX");rank=Mathf.Min(1,rank+.06f);}}
        if(kind==1){points++;totalPoints++;int value=dream>=128?new[]{60000,100000,150000,200000,400000}[difficulty]:player.y<96+dream*.9f?51200:Mathf.RoundToInt(Mathf.Lerp(28000,2000,Mathf.Clamp01((player.y-96-dream*.9f)/(352-dream*.9f))));score+=value;if(value==51200)AddDream(4);if(totalPoints%100==0){lives++;CancelBullets(false);Notice("EXTEND");}}
        if(kind==1)rank=Mathf.Min(1,rank+.001f);
        if(kind==3){bombs++;score+=1000;}
        if(kind==4){lives++;score+=1000;rank=Mathf.Min(1,rank+.025f);Notice("EXTEND");}
        if(kind==5){if(dream>=128)score+=128000;else AddDream(128);}
    }
    public void AddDream(float amount){float before=dream;dream=Mathf.Min(128,dream+amount);if(before<128&&dream>=128){CancelBullets(false);Notice("夢  MAX");}}
    public void Bomb(){if(bombs<=0||bombTime>0||state!=RunState.Playing)return;bombs--;stageBombs++;bombTime=new[]{3.2f,1.8f,1.4f,4f}[character];invincible=bombTime+1;dream=Mathf.Max(0,dream-bombTime*(HasBoss?6:12));rank=Mathf.Max(0,rank-.15f);missPending=false;CancelBullets(false);sound?.Invoke("bomb");}
    static int NextPowerValue(int value)=>value<100?value+10:value<1000?value+100:value<11000?value+500:value==11000?12000:value==12000?12500:12800;
    public void LoseLife(){missPending=false;lives--;stageMisses++;power=lives<=0?0:Mathf.Max(0,power-16);dream=Mathf.Max(0,dream-(HasBoss?32:64));rank=Mathf.Max(0,rank-.2f);powerValue=10;Burst(player,35,0);CancelBullets(false);sound?.Invoke("death");SpawnDrop(player,lives<=0?6:2);for(int i=0;i<4;i++)SpawnDrop(player+new Vector2((i-1.5f)*12,0),lives<=0?6:random.Next(2));player=new Vector2(192,382);invincible=3;bombs=initialBombs;if(lives<=0)state=RunState.GameOver;}
    public bool Continue(){if(extra||practice||continues>=3||state!=RunState.GameOver)return false;continues++;score=0;lives=initialLives;bombs=initialBombs;power=0;state=RunState.Playing;invincible=3;return true;}
    void FinishStage(bool timeout){
        long basis=(stage>=5?10000:(stage+1)*1000)+(long)(dream*100)+graze*50+(stage>=5?(lives-1)*10000:0);
        clearBonus=basis*points;
        if(stageMisses==0){int bonus=new[]{100000,150000,200000,250000,300000,500000,500000}[stage];clearBonus+=bonus;if(stageBombs==0)clearBonus+=bonus;}
        if(stage>=5)clearBonus+=totalPoints*2500;
        double multiplier=(difficulty==0?.5:difficulty==2?1.2:difficulty==3?1.4:1)*(initialLives==4?.7:initialLives==5?.5:initialLives==6?.3:1)*(1-continues*.2);
        clearBonus=timeout?0:(long)(clearBonus*multiplier)/10*10;score+=clearBonus;rank=Mathf.Clamp01(rank+(clearBonus>100000?.03f:-.03f)+(stageMisses==0?.04f:0)+(stageBombs==0?.02f:0));state=RunState.StageClear;transition=4;drops.Clear();sound?.Invoke("clear");
    }
    public void NextStage(){if(practice||stage>=5){state=RunState.Complete;return;}stage++;stageFrame=0;phase=0;points=0;graze=0;stageMisses=stageBombs=0;bossStarted=midStarted=false;shots.Clear();bullets.Clear();drops.Clear();foes.Clear();player=new Vector2(192,382);invincible=2;state=RunState.Playing;}
    public void CancelBullets(bool bonus){if(bonus)for(int i=1;i<=bullets.Count;i++)score+=Math.Min(difficulty==0?9600:difficulty==4?16000:12800,i*20);for(int i=0;i<bullets.Count;i+=4)Burst(bullets[i].p,1,2);bullets.Clear();lasers.Clear();}
    public void Burst(Vector2 p,int count,int color){for(int i=0;i<count&&sparks.Count<400;i++){float life=R(.2f,.6f);sparks.Add(new Spark{p=p,v=new Vector2(R(-65,65),R(-65,65)),life=life,maxLife=life,color=color});}}
    public void Notice(string text){notice=text;noticeTime=2;}
}
}
