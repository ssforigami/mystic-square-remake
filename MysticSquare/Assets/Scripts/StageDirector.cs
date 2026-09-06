using UnityEngine;
namespace MysticSquare {
public sealed partial class GameModel {
    public static readonly int[] BossEntry={4200,4500,4860,5160,5460,540,5400};
    void SpawnEnemy(float x,float y,int kind,int side=0){float hp=kind==1?26+stage*3:7+stage*2;foes.Add(new Foe{p=new Vector2(x,y),originX=x,kind=kind,side=side,hp=hp,maxHp=hp});}
    void Stream(int from,int to,int spacing,int kind,float x,int side=0){if(stageFrame>=from&&stageFrame<to&&(stageFrame-from)%spacing==0)SpawnEnemy(x,kind==4?465:-22,kind,side);}
    void SideStream(int from,int to,int spacing,int side){if(stageFrame>=from&&stageFrame<to&&(stageFrame-from)%spacing==0)SpawnEnemy(side==0?-25:409,50+(stageFrame/spacing%4)*32,2,side);}
    void StageScript(){
        int f=stageFrame;int gap=Mathf.Max(24,50-difficulty*5);
        switch(stage){
        case 0:
            Stream(120,840,gap,0,76);Stream(480,1100,gap,0,308,1);
            Stream(960,1500,160,1,192);Stream(2520,3300,gap,0,75);Stream(2760,3600,gap,0,309,1);
            Stream(3420,3950,125,1,100);Stream(3480,4000,125,1,284,1);break;
        case 1:
            Stream(120,1080,gap,3,65);Stream(390,1350,gap,3,319,1);Stream(1080,1650,145,1,192);
            SideStream(2760,3540,gap,0);SideStream(3030,3810,gap,1);Stream(3540,4210,130,1,100);Stream(3600,4210,130,1,284,1);break;
        case 2:
            SideStream(150,1000,gap,0);SideStream(570,1380,gap,1);Stream(1140,1800,120,1,85);Stream(1200,1800,120,1,299,1);
            SideStream(2880,3750,gap,0);SideStream(3120,4050,gap,1);Stream(3780,4500,75,3,140);Stream(3840,4560,75,3,244,1);break;
        case 3:
            Stream(120,1080,gap,0,90);Stream(240,1200,gap,0,294,1);Stream(1150,1770,100,1,192);
            Stream(2940,3300,gap,4,42);Stream(3060,3420,gap,4,342,1);
            SideStream(3540,4250,gap,0);SideStream(3780,4530,gap,1);Stream(4470,4830,120,1,100);Stream(4530,4830,120,1,284,1);break;
        case 4:
            Stream(120,1200,gap,3,72);Stream(120,1200,gap,3,312,1);SideStream(1140,1890,70,0);SideStream(1350,1950,70,1);
            Stream(3060,4800,gap+10,1,90);Stream(3090,4860,gap+10,1,294,1);
            SideStream(3600,5100,gap,0);SideStream(3900,5160,gap,1);break;
        case 5:
            if(f>=60&&f<300&&f%12==0)SpawnDrop(new Vector2(35+(f/12%10)*35,-10),1);break;
        case 6:
            Stream(120,1440,36,0,80);Stream(300,1620,36,0,304,1);SideStream(1440,2160,45,0);SideStream(1620,2160,45,1);
            Stream(3420,4680,60,1,95);Stream(3420,4680,60,1,289,1);SideStream(4320,5100,36,0);SideStream(4500,5160,36,1);break;
        }
        int midAt=stage==6?2220:stage==0?1620:stage==1?1740:1980;
        if(stage!=5&&!midStarted&&f>=midAt){midStarted=true;CancelBullets(false);float hp=380+stage*55;foes.Add(new Foe{p=new Vector2(192,-30),hp=hp,maxHp=hp,mid=true});}
        if(f>=BossEntry[stage]&&!foes.Exists(e=>e.mid))StartBoss();
    }
    void MoveEnemy(Foe e){
        float dt=1f/60;
        switch(e.kind){
            case 0:e.p.y+=55*dt;e.p.x=e.originX+Mathf.Sin(e.age*1.8f+e.side)*38;break;
            case 1:e.p.y+= (e.age<2?52:e.age<6?0:90)*dt;e.p.x=e.originX+Mathf.Sin(e.age)*18;break;
            case 2:e.p.x+=(e.side==0?78:-78)*dt;e.p.y+=20*dt;break;
            case 3:e.p.y+=(70+e.age*20)*dt;e.p.x=e.originX+Mathf.Sin(e.age*2)*23;break;
            case 4:e.p.y-=105*dt;e.p.x=e.originX+(e.side==0?1:-1)*e.age*14;break;
        }
        int interval=e.kind==1?Mathf.Max(30,85-difficulty*10):Mathf.Max(42,125-difficulty*15);
        if(e.tick%interval==30&&e.p.y>5&&e.p.y<370){float a=Aim(e.p);int count=e.kind==1?3+difficulty*2:1+(difficulty>=2?2:0);Fan(e.p,count,a,.15f,70+difficulty*15+stage*4,stage%3);if(e.kind==1&&difficulty>=2)Ring(e.p,8+difficulty*2,55,e.age*.5f,1);}
    }
    void MidPattern(Foe e){
        if(e.tick<90)return;int k=e.tick-90;float v=60+difficulty*14;
        if(stage==0&&k%65==0){Ring(e.p,10+difficulty*3,v,k*.01f,0);}
        if(stage==1&&k%48==0){Fan(e.p,9+difficulty*2,Mathf.PI/2+Mathf.Sin(k*.016f)*.6f,.14f,v,1);}
        if(stage==2&&k%70==0){for(int s=-1;s<=1;s+=2)Ring(e.p+new Vector2(s*50,0),8+difficulty*2,v,s*k*.008f,2);}
        if(stage==3&&k%45==0){Fan(e.p,11+difficulty*2,Mathf.PI/2,.16f,v,0);if(k%90==0)Ring(e.p,12+difficulty*2,v*.65f,0,1);}
        if(stage==4&&k%45==0){Fan(e.p,5+difficulty,Aim(e.p),.16f,v+45,3);if(k%90==0)Ring(e.p,12,v,k*.01f,0);}
        if(stage==6&&k%40==0){for(int i=0;i<4;i++){Vector2 p=e.p+new Vector2((i-1.5f)*45,0);Fan(p,5,Aim(p),.16f,v+10,i%3);}if(k%160==0)SpawnDrop(e.p,0);}
    }
}
}
