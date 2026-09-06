using UnityEngine;
namespace MysticSquare {
// Independent implementation of the attacks described in the author's public walkthrough.
public sealed partial class GameModel {
    float Aim(Vector2 p)=>Mathf.Atan2(player.y-p.y,player.x-p.x);
    void Fan(Vector2 p,int count,float angle,float spacing,float speed,int color){for(int i=0;i<count;i++)Bullet(p,angle+(i-(count-1)*.5f)*spacing,speed,color);}
    void Beam(Vector2 p,float angle,int color=1,float turn=0,bool tracking=false,float length=560){if(lasers.Count<30)lasers.Add(new Laser{p=p,angle=angle,color=color,turn=turn,tracking=tracking,length=length,warn=difficulty==0?1.1f:.8f,duration=1.6f});}
    void Special(Vector2 p,float angle,float speed,int style,float acceleration=0,float curve=0,int bounces=0,float burst=0,int tint=-1){if(bullets.Count>=2200)return;Bullet(p,angle,speed,style);if(bullets.Count==0)return;var b=bullets[bullets.Count-1];b.tint=tint;b.acceleration=acceleration;b.curve=curve;b.bounces=bounces;b.burstAt=burst;if(style==5)b.radius=5;}
    void BossPattern(Foe e){
        if(e.tick<90)return;
        int k=e.tick-90;float a=Aim(e.p),v=65+difficulty*15+rank*12;int density=8+difficulty*3+Mathf.FloorToInt(rank*6);
        switch(stage){
        case 0:
            if(phase<2){
                if(k%240==0||e.attack<0)e.attack=(e.attack+1+random.Next(3))%4;
                if(k%Mathf.Max(20,54-difficulty*7)==0)switch(e.attack){
                    case 0:Ring(e.p,density+6,v,k*.005f,phase);break;
                    case 1:Fan(e.p,5+difficulty*2,a,.13f,v+32,0);break;
                    case 2:for(int s=-1;s<=1;s+=2)Ring(e.p+new Vector2(s*34,0),density,v*.7f,s*k*.006f,1);break;
                    case 3:for(int i=0;i<density;i++)Bullet(e.p,R(.2f,2.94f),R(v*.65f,v*1.25f),phase==0?0:2);break;
                }
            }else{
                if(k%135==0){Beam(e.p+new Vector2(-46,0),2.0f,0);Beam(e.p+new Vector2(46,0),1.14f,0);}
                if(k%50==0)Fan(e.p,7+difficulty*2,Mathf.PI/2,.17f,v,1);
            }break;
        case 1:
            if(phase==0&&k%65==0){int col=(k/65)%2;for(int i=0;i<density+12;i++){float angle=i*Mathf.PI*2/(density+12)+k*.001f;Bullet(e.p,angle,v*(col==0?1:.7f+.35f*Mathf.Sin(angle)),col);}}
            if(phase==1&&k%18==0)for(int i=0;i<3+difficulty;i++)Bullet(e.p,R(0,Mathf.PI),R(v*.65f,v*1.5f),i%2);
            if(phase==2){if(k%170==0)for(int i=-2;i<=2;i++)Beam(e.p,a+i*.48f,2);if(k%35==0)Ring(e.p,density,v,k*.011f,1);}
            if(phase==3&&k%28==0)Fan(e.p,11+difficulty*2,Mathf.PI/2+Mathf.Sin(k*.014f)*.3f,.105f,v+22,(k/28)%2);break;
        case 2:
            if(phase<2){
                if(k%72==0)for(int i=0;i<4+phase;i++){float angle=i*Mathf.PI*2/(4+phase)+k*.007f;Vector2 p=e.p+new Vector2(Mathf.Cos(angle)*76,Mathf.Sin(angle)*40);Ring(p,5+difficulty*2,v*.8f,angle,2);}
                if(k%40==0)Fan(e.p,3+difficulty*2,a,.1f,v+12,0);
            }else if(k%20==0){Fan(e.p,5+difficulty*2,a,.17f,v+28,0);if(k%60==0)Ring(e.p,density,v*.8f,k*.013f,2);}break;
        case 3:
            if(phase==0){int joint=Mathf.Clamp(2-(int)(e.hp/e.maxHp*3),0,2);
                if(e.side==1&&joint==1&&k%190==0)for(int i=-1;i<=1;i++)Beam(e.p,a+i*.4f,1);
                if(k%Mathf.Max(23,58-difficulty*6)==0)Fan(e.p,7+difficulty*2,a+Mathf.Sin(k*.02f+e.side)*.3f,.17f,v+joint*10,e.side);
            }else if(e.side==0){
                if(phase==1){if(k%80==0)Ring(e.p,density+6,28,k*.01f,0);if(k%24==0)Fan(e.p,3+difficulty,Mathf.PI/2+Mathf.Sin(k*.019f),.16f,v+20,0);}
                if(phase==2){if(k%65==0)Ring(e.p,density+10,v,k*.007f,0);if(k%110==0)for(int i=-2;i<=2;i++)Special(e.p,a+i*.25f,62,5);}
                if(phase==3&&k%38==0)for(int i=0;i<density+4;i++)Special(e.p,Mathf.PI*2*i/(density+4)+k*.008f,18,0,35+difficulty*6);
                if(phase==4){if(k%85==0)Fan(e.p,5,a,.31f,58,5);if(k%18==0)Fan(e.p,7+difficulty*2,Mathf.PI/2+Mathf.Sin(k*.015f)*.35f,.12f,v+55,0);}
            }else{
                if(phase==1&&k%45==0){if(k/180%2==0)Ring(e.p,density+8,v,k*.011f,1);else Fan(e.p,9+difficulty*2,a,.115f,v+35,1);}
                if(phase==2){if(k%180==0){Beam(e.p,0,1,.65f);Beam(e.p,Mathf.PI,1,-.65f);}if(k%42==0)Fan(e.p,7,a,.16f,v,1);}
                if(phase==3&&k%32==0)for(int s=-1;s<=1;s+=2)for(int i=0;i<4+difficulty;i++)Special(e.p,.22f+i*.12f+(s==1?Mathf.PI*.55f:0),v+10,3,0,s*.23f);
                if(phase==4&&k%65==0){for(int i=0;i<density;i++)Special(e.p,Mathf.PI*2*i/density+k*.01f,v,1,0,0,1);Special(e.p,a,55,5,0,0,0,2);}
            }break;
        case 4:
            int period=Mathf.Max(18,45-difficulty*4-(int)((1-e.hp/e.maxHp)*12));
            if(phase==0){if(k%100==0)Fan(e.p,5,a,.2f,v+60,3);if(k%20==0)for(int i=0;i<4+difficulty;i++)Bullet(e.p,R(.1f,3.04f),R(v*.65f,v*1.25f),0);}
            if(phase==1||phase==3){if(k%period==0){Fan(e.p,5+difficulty,a,.11f,v+70,3);for(int s=-1;s<=1;s+=2)Fan(e.p+new Vector2(s*50,0),5,Mathf.PI/2+s*.22f,.04f,v+25,0);}if(phase==3&&k%190==0)for(int s=-1;s<=1;s+=2)Beam(e.p+new Vector2(s*60,0),a+s*.28f,1);}
            if(phase==2&&k%45==0){if(k/180%2==0)Ring(e.p,density+8,v,k*.008f,3);else Fan(e.p,11,a,.11f,v+15,0);}
            if(phase==4){if(k%55==0)for(int s=0;s<2;s++){Vector2 p=new Vector2(s==0?3:381,70+k%160);Fan(p,3+difficulty,Aim(p),.13f,v+65,3);}if(k%32==0)Fan(e.p,5,a,.23f,v+30,0);}
            if(phase==5&&k%35==0)for(int i=0;i<density+6;i++)Special(e.p,i*Mathf.PI*2/(density+6)+k*.005f,v,2,0,Mathf.Sin(i)*.4f);break;
        case 5:
            if(phase==0&&k%45==0){Ring(e.p,density+8,v,k*.012f,0);if(k%90==0)Fan(e.p,5,a,.12f,v+20,1);}
            if(phase==1&&k%16==0)for(int i=0;i<4+difficulty;i++)Bullet(e.p,R(0,Mathf.PI),R(v*.6f,v*1.25f),i%3);
            if(phase==2&&k%70==0)for(int s=-1;s<=1;s+=2)Ring(e.p+new Vector2(s*95,10),density+8,v,k*.006f*s,2);
            if(phase==3){if(k%55==0)for(int i=0;i<6+difficulty;i++)Special(new Vector2(25+i*334f/(5+difficulty),80),Mathf.PI/2,45+(i%3)*12,5,10,0,0,0,1);if(k%36==0)Fan(e.p,3,a,.2f,v,1);}
            if(phase==4){if(k%150==0)for(int s=-1;s<=1;s+=2)Beam(e.p+new Vector2(s*90,5),a,1,0,true);if(k%28==0)Fan(e.p,5,a,.16f,v,1);}
            if(phase==5&&k%55==0)Ring(e.p,density,62,k*.004f,5);
            if(phase==6){if(k%35==0)Ring(e.p,density+12,v,k*.01f,0);if(k%24==0)Special(e.p,a,v+15,5);}
            if(phase==7){if(k%Mathf.Max(15,45-k/300*4)==0)Fan(e.p,3,a,.18f,v+14,5);if(k%210==0){Beam(new Vector2(5,80),.4f,2,.38f);Beam(new Vector2(379,80),Mathf.PI-.4f,2,-.38f);}}
            if(phase==8&&k%35==0){Ring(e.p,density+8,v,k*.014f,2);if(k%70==0)Fan(e.p,5,a,.26f,65,5);}break;
        case 6:
            int color=phase<2?0:phase<4?1:phase<6?2:phase<8?6:4;
            if(phase<2){if(k%32==0)for(int s=-1;s<=1;s+=2)Ring(e.p+new Vector2(s*72,0),density,v,s*k*.014f,color);if(phase==1&&k%65==0)Fan(e.p,5,a,.2f,80,5);}
            else if(phase<4){if(k%36==0)for(int i=0;i<density+8;i++)Special(e.p,i*Mathf.PI*2/(density+8)+k*.012f,30,color,25,phase==3?.15f:0);}
            else if(phase<6){if(k%155==0)for(int i=-2;i<=2;i++)Beam(e.p,a+i*.44f,2,phase==5?i*.08f:0);if(k%30==0)Ring(e.p,density,v,k*.016f,2);}
            else if(phase<8){if(k%125==0)for(int s=-1;s<=1;s+=2)Beam(e.p+new Vector2(s*95,0),a,6,0,true);if(k%27==0)Ring(e.p,density+4,v,k*.012f,color);}
            else if(k%25==0){for(int i=0;i<density+12;i++)Special(e.p,i*Mathf.PI*2/(density+12)+k*.01f,v*(i%2==0?1:.7f),4,8);if(k%100==0)Fan(e.p,7,a,.18f,v+45,4);}break;
        }
    }
}
}
