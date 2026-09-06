using UnityEngine;
namespace MysticSquare {
public sealed partial class MysticGame {
    bool godEnabled,endingShown,endingScreen,creditsScreen,seenTwin;
    int seenBossStage=-1,seenClearStage=-1,taleIndex,musicVolume=55,currentMusic;
    float taleClock,creditsClock;
    string[] tale;string taleHeading;
    Texture2D[] portraits=new Texture2D[4],bossPortraits=new Texture2D[8],midArt=new Texture2D[7];
    Texture2D shrine;
    Texture2D[] bulletArt=new Texture2D[7];
    AudioSource musicSource;
    public static readonly string[] MusicTitles={"怪绮谈 ～ Mystic Square","Dream Express","魔法阵 ～ Magic Square","梦想时空","灵天 ～ Spiritual Heaven","Romantic Children","Plastic Mind","Maple Wise","禁忌的魔法 ～ Forbidden Magic","绯红的少女 ～ Crimson Dead!!","背叛的少女 ～ Judas Kiss","the Last Judgement","可悲的人偶 ～ Doll of Misery","世界的尽头 ～ World's End","神话幻想 ～ Infinite Being","不可思议之国的爱丽丝","the Grimoire of Alice","神社","Endless","永远的乐园","Mystic Dream","Peaceful Romancer","魂之安眠之地"};
    static readonly int[] StageMusic={2,4,6,8,12,14,16},BossMusic={3,5,7,9,13,15,17};
    void InitContent(){
        musicVolume=PlayerPrefs.GetInt("musicVolume",55);
        musicSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicSource.playOnAwake=false;
        portraits[0]=reimu;for(int i=1;i<4;i++)portraits[i]=Load("portrait"+i);
        for(int i=0;i<8;i++)bossPortraits[i]=Load("portraitBoss"+i);
        for(int i=0;i<7;i++)midArt[i]=Load("mid"+i);
        shrine=Load("shrine");for(int i=0;i<7;i++)bulletArt[i]=MakeBullet(i);PlayMusic(1);
    }
    void PlayMusic(int track){
        if(track==currentMusic)return;
        var clip=Resources.Load<AudioClip>("Music/"+track.ToString("D2"));
        if(clip==null){Debug.LogError("Missing music "+track);return;}
        musicSource.Stop();var old=musicSource.clip;musicSource.clip=clip;currentMusic=track;musicSource.volume=musicVolume/100f*.65f;musicSource.Play();
        if(old!=null)Resources.UnloadAsset(old);
    }
    void ToggleGod(){godEnabled=!godEnabled;if(game!=null){game.godMode=godEnabled;if(godEnabled){game.usedGodMode=true;game.missPending=false;}game.Notice(godEnabled?"无敌 ON":"无敌 OFF");}Sound("select");}
    void ResetStory(){seenBossStage=seenClearStage=-1;seenTwin=false;endingShown=endingScreen=creditsScreen=false;tale=null;PlayMusic(StageMusic[game.stage]);if(!smoke&&!game.practice)ShowTale("序",StoryData.Intro(game.character));}
    void ShowTale(string heading,params string[] lines){taleHeading=heading;tale=lines;taleIndex=0;taleClock=0;accumulator=0;bombQueued=false;}
    void AdvanceTale(bool skip=false){
        if(tale==null)return;taleClock=0;taleIndex=skip?tale.Length:taleIndex+1;
        if(taleIndex<tale.Length)return;
        tale=null;accumulator=0;bombQueued=false;
        if(endingScreen){endingScreen=false;creditsScreen=true;creditsClock=0;PlayMusic(game.extra?22:21);}
    }
    bool UpdateContent(){
        if(Key(KeyCode.F1))ToggleGod();
        musicSource.volume=Mathf.MoveTowards(musicSource.volume,musicVolume/100f*(paused?.28f:.65f),Time.unscaledDeltaTime);
        if(page!=Page.Game){if(page!=Page.Music)PlayMusic(1);return false;}
        if(creditsScreen){creditsClock+=Time.unscaledDeltaTime;if(creditsClock>35||Key(KeyCode.Escape)||Key(KeyCode.Z)||Key(KeyCode.Return)){creditsScreen=false;PlayMusic(23);}return true;}
        if(tale!=null){taleClock+=Time.unscaledDeltaTime;if(taleClock>.25f&&(Key(KeyCode.Z)||Key(KeyCode.Return)||Key(KeyCode.Mouse0)))AdvanceTale();else if(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))AdvanceTale(true);return true;}
        if(game.state==RunState.Complete&&!endingShown&&!game.practice){SaveResult();endingShown=true;endingScreen=true;PlayMusic(game.extra?20:game.continues>0?18:19);ShowTale(StoryData.EndingName(game.character,game.extra,game.continues>0),StoryData.Ending(game.character,game.extra,game.continues>0));return true;}
        if(game.state==RunState.GameOver||game.state==RunState.Complete)PlayMusic(23);
        else if(game.HasBoss){int track=BossMusic[game.stage];if(game.stage==3&&game.phase>0)track=game.foes[0].side==0?10:11;PlayMusic(track);}
        else PlayMusic(StageMusic[game.stage]);
        return false;
    }
    bool CheckStory(bool force=false){
        if((smoke&&!force)||game.practice)return false;
        if(game.bossStarted&&seenBossStage!=game.stage){seenBossStage=game.stage;string text=StoryData.Encounter(game.character,game.stage);ShowTale(GameModel.BossNames[game.stage],game.stage==4?text.Split('\n'):new[]{text});return true;}
        if(game.stage==3&&game.phase>0&&!seenTwin&&game.HasBoss){seenTwin=true;ShowTale(game.foes[0].side==0?"雪":"舞",StoryData.Twin(game.foes[0].side));return true;}
        if(game.state==RunState.StageClear&&game.stage<5&&seenClearStage!=game.stage){seenClearStage=game.stage;ShowTale("战后",StoryData.After(game.stage,game.character));return true;}
        return false;
    }
    void Paragraph(string value,Rect r,int size,Color color){textStyle.font=body;textStyle.fontSize=size;textStyle.normal.textColor=color;textStyle.alignment=TextAnchor.UpperLeft;textStyle.wordWrap=true;textStyle.fontStyle=FontStyle.Normal;GUI.Label(r,value,textStyle);textStyle.wordWrap=false;}
    void DrawTale(){
        RectFill(24,24,576,672,new Color(.035f,.025f,.065f,.6f));
        bool shinkiArrival=game.stage==4&&taleHeading==GameModel.BossNames[4]&&taleIndex==0;
        if(game.bossStarted){int b=shinkiArrival?5:game.stage==3&&game.phase>0?(game.foes.Count>0&&game.foes[0].side==1?7:3):game.stage;Tex(bossPortraits[b],new Rect(184,142,390,429),Color.white);}
        else Tex(portraits[game.character],new Rect(65,98,412,494),Color.white);
        RectFill(38,498,548,170,C("211b2b"));RectFill(38,498,3,170,red);RectFill(41,498,545,1,gold);
        Label(shinkiArrival?"神绮":taleHeading,59,509,507,32,23,gold,display);
        Paragraph(tale[taleIndex],new Rect(59,554,505,82),19,ivory);
        Label("Z / ENTER  继续     CTRL  跳过",59,641,484,18,11,muted,mono);
        Diamond(557,649,5+Mathf.Sin(clock*4),red);
    }
    void DrawEnding(){
        Tex(shrine,new Rect(0,0,1024,720),Color.white);
        RectFill(0,0,1024,720,new Color(.04f,.025f,.06f,.38f));
        if(creditsScreen){
            Header("制作与鸣谢",game.extra?"EXTRA CLEAR":"MYSTIC DREAM");
            float y=205-Mathf.Min(creditsClock*7,100);
            Label("東方怪綺談",104,y,816,78,50,ivory,display,TextAnchor.MiddleCenter);
            string[] lines={"原作・角色・音乐作曲   ZUN","本次重制   独立程序 / 重绘画面 / FM 合成","文字资料   THBWiki · 原作公开 Manual 与攻略","音乐独立转谱   DMBN / 東方ピアノEasyモード","icebhm23230 · ryoya1295 · LeastLPZ","CommonShitSandwich","详细转谱来源见发行包 MUSIC-CREDITS.md","感谢游玩"};
            for(int i=0;i<lines.Length;i++)Label(lines[i],80,y+100+i*43,864,36,i==7?29:17,i==7?gold:ivory,i==7?display:body,TextAnchor.MiddleCenter);
            Label("Z / ENTER  结算",70,677,880,25,12,muted,mono,TextAnchor.MiddleRight);return;
        }
        Tex(portraits[game.character],new Rect(22,47,518,622),Color.white);
        if(game.extra)Tex(bossPortraits[6],new Rect(405,26,320,352),new Color(.8f,.74f,.86f,.85f));
        RectFill(507,80,450,560,new Color(.08f,.05f,.10f,.89f));RectFill(507,80,2,560,red);
        Label(taleHeading,548,112,370,35,17,gold,mono);
        Label(GameModel.Names[game.character],542,167,383,65,36,ivory,display);
        RectFill(547,250,362,1,C("715361"));
        Paragraph(tale[taleIndex],new Rect(548,292,357,229),22,ivory);
        Label((taleIndex+1)+" / "+tale.Length,548,563,170,22,13,gold,mono);
        Label("Z / ENTER  继续",716,563,190,22,12,muted,mono,TextAnchor.MiddleRight);
    }
    void DrawMusicRoom(){
        Header("音乐室","MUSIC ROOM");
        for(int i=0;i<23;i++){float x=i<12?78:530,y=171+(i%12)*37;bool selected=cursor==i;if(selected){RectFill(x,y,2,30,red);Diamond(x+14,y+15,5,red);}Label((i+1).ToString("D2"),x+28,y,32,29,12,gold,mono);Label(MusicTitles[i],x+65,y,378,29,15,selected?ivory:muted);if(GUI.Button(new Rect(x,y,440,32),GUIContent.none,GUIStyle.none)){cursor=i;PlayMusic(i+1);}}
        if(MenuRow("返回","BACK",23,530,615,400))Back();
        Label("正在播放   "+currentMusic.ToString("D2")+"  "+MusicTitles[currentMusic-1],81,638,445,26,12,gold);
    }
    Color BulletColor(int style)=>style==0?C("f375a4"):style==1?C("6cccec"):style==2?C("d6afef"):style==3?C("c5ddf5"):style==4?C("efd277"):style==6?C("86e4b5"):C("efa8c9");
    Texture2D MakeBullet(int style){var t=new Texture2D(32,32,TextureFormat.RGBA32,false);Color col=BulletColor(style);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16))/16;Color c=d<.22f?ivory:d<.49f?col:d<.62f?C("34243e"):new Color(col.r,col.g,col.b,Mathf.Clamp01(1-d)*.7f);t.SetPixel(x,y,c);}t.Apply();return t;}
    void DrawEnemyBullets(){
        // Group by texture so dense patterns do not alternate material bindings for every orb.
        for(int color=0;color<7;color++)foreach(var b in game.bullets){if(b.style==3||b.style==5||(b.tint<0?b.style:b.tint)!=color)continue;Vector2 p=b.p*1.5f;Tex(bulletArt[color],new Rect(p.x-9,p.y-9,18,18),Color.white);}
        foreach(var b in game.bullets){Vector2 p=b.p*1.5f;Color c=BulletColor(b.tint<0?b.style:b.tint);
            if(b.style==3){float a=Mathf.Atan2(b.v.y,b.v.x)*Mathf.Rad2Deg+90;RotatedTex(orb,p,new Vector2(6,23),a,c);RotatedTex(orb,p,new Vector2(2,15),a,ivory);}
            if(b.style==5){Tex(orb,new Rect(p.x-23,p.y-23,46,46),new Color(c.r,c.g,c.b,.72f));Tex(ring,new Rect(p.x-20,p.y-20,40,40),ivory);Tex(orb,new Rect(p.x-6,p.y-6,12,12),ivory);}
        }
    }
    void DrawBomb(){
        if(game.bombTime<=0)return;Vector2 p=game.player*1.5f;float age=new[]{3.2f,1.8f,1.4f,4f}[game.character]-game.bombTime;
        if(game.character==0){for(int i=0;i<8;i++){float a=i*Mathf.PI/4+age*2;Vector2 at=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(70+age*95);Tex(orb,new Rect(at.x-36,at.y-36,72,72),ivory);Tex(orb,new Rect(at.x-34,at.y-34,68,68),red);Tex(orb,new Rect(at.x-30,at.y-31,38,38),ivory);Tex(orb,new Rect(at.x-11,at.y-18,12,12),red);}RectFill(0,0,576,672,new Color(.9f,.5f,.6f,.08f));}
        else if(game.character==1){for(int i=-2;i<=2;i++){float x=p.x+i*34;RectFill(x-15,0,30,p.y,new Color(.65f,.76f,1,.24f));RectFill(x-5,0,10,p.y,new Color(.9f,.9f,1,.7f));}Tex(glow,new Rect(p.x-100,p.y-100,200,200),blue);}
        else if(game.character==2){for(int i=0;i<7;i++){float r=(age*430+i*70)%620;Tex(ring,new Rect(p.x-r,p.y-r,2*r,2*r),new Color(.58f,.94f,.83f,.8f));}RectFill(0,0,576,672,new Color(.35f,.65f,.56f,.17f));}
        else{for(int k=0;k<5;k++){Vector2 at=new Vector2(95+k*96,220+Mathf.Sin(k*2)*140);float r=35+35*Mathf.Sin(Mathf.Min(age,1.5f));for(int i=0;i<6;i++){float a=i*Mathf.PI/3+age;Vector2 petal=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.7f;Tex(orb,new Rect(petal.x-r*.55f,petal.y-r*.55f,r*1.1f,r*1.1f),new Color(.89f,.51f,.7f,.42f));}Tex(ring,new Rect(at.x-r,at.y-r,2*r,2*r),ivory);}}
    }
    void DrawLasers(){
        foreach(var l in game.lasers){Color c=BulletColor(l.color);Vector2 p=l.p*1.5f,end=p+new Vector2(Mathf.Cos(l.angle),Mathf.Sin(l.angle))*l.length*1.5f;
            if(l.Active){Line(p,end,new Color(c.r,c.g,c.b,.2f),l.width*3.2f);Line(p,end,c,l.width*1.5f);Line(p,end,ivory,l.width*.45f);}
            else{Line(p,end,new Color(c.r,c.g,c.b,.45f),1);Tex(ring,new Rect(p.x-11,p.y-11,22,22),c);}
        }
        if(game.AliceBarrier){var e=game.foes.Find(f=>f.boss);RectFill((e.p.x-90)*1.5f,195,270,30,new Color(.6f,.35f,.8f,.15f));Line(new Vector2((e.p.x-90)*1.5f,210),new Vector2((e.p.x+90)*1.5f,210),C("c29ce1"),2);}
    }
}
}
