using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MysticSquare {
public sealed partial class MysticGame : MonoBehaviour {
    enum Page { Title, Character, Difficulty, Options, Help, Records, Game, Music }
    Page page=Page.Title;
    GameModel game;
    int cursor,character,difficulty=1,mode,startingLives=3,startingBombs=3;
    int volume=60; bool paused,fullScreen,savedResult,smoke,qaInput;
    float accumulator,clock; bool bombQueued;
    Vector2 drawOrigin;
    Font body,display,mono;
    GUIStyle textStyle=new GUIStyle();
    Texture2D white,orb,ring,glow,title,reimu;
    Texture2D[] stages=new Texture2D[7],players=new Texture2D[4],bosses=new Texture2D[7];
    Texture2D enemy,mai;
    Color ink=C("181522"),ivory=C("f1e3ce"),muted=C("a79aab"),red=C("cf6174"),gold=C("c9ab75"),blue=C("79b9dc");
    AudioSource audioSource;Dictionary<string,AudioClip> effects=new Dictionary<string,AudioClip>();
    static Color C(string s){ColorUtility.TryParseHtmlString("#"+s,out Color c);return c;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap(){if(FindAnyObjectByType<MysticGame>()==null)new GameObject("Mystic Square").AddComponent<MysticGame>();}
    void Awake(){
        Application.targetFrameRate=60;QualitySettings.vSyncCount=1;
        Application.runInBackground=false;
        var cam=new GameObject("Camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=ink;cam.orthographic=true;cam.gameObject.AddComponent<AudioListener>();
        white=Texture2D.whiteTexture;orb=Circle(false,false);ring=Circle(true,false);glow=Circle(false,true);
        body=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},18);
        display=Font.CreateDynamicFontFromOSFont(new[]{"KaiTi","STKaiti","SimSun","Microsoft YaHei"},72);
        mono=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Courier New"},20);
        title=Load("title");reimu=Load("reimu");enemy=Load("enemy");mai=Load("mai");
        for(int i=0;i<7;i++){stages[i]=Load("stage"+i);bosses[i]=Load("boss"+i);}for(int i=0;i<4;i++)players[i]=Load("player"+i);
        startingLives=PlayerPrefs.GetInt("startingLives",3);startingBombs=PlayerPrefs.GetInt("startingBombs",3);volume=PlayerPrefs.GetInt("volume",60);
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
        foreach(string name in new[]{"move","select","shot","hit","item","bomb","death","phase","clear","boss","danger"})effects[name]=CreateSound(name);
        smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke-test")>=0;
        qaInput=Array.IndexOf(Environment.GetCommandLineArgs(),"--qa-input")>=0;
        InitContent();
        if(smoke){Application.runInBackground=true;StartCoroutine(SmokeTest());}
    }
    Texture2D Load(string name){var t=Resources.Load<Texture2D>("Art/"+name);if(t!=null)t.filterMode=name.StartsWith("player")||name.StartsWith("boss")||name=="enemy"||name=="mai"||name.StartsWith("mid")?FilterMode.Point:FilterMode.Bilinear;return t;}
    Texture2D Circle(bool outline,bool soft){var t=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))/32;float a=soft?Mathf.Pow(Mathf.Clamp01(1-d),2):outline?Mathf.Clamp01((1-d)*32)*Mathf.Clamp01((d-.87f)*32):Mathf.Clamp01((1-d)*32);t.SetPixel(x,y,new Color(1,1,1,a));}t.Apply();return t;}
    AudioClip CreateSound(string name){
        float duration=name=="bomb"?.85f:name=="clear"?.5f:name=="death"?.45f:name=="shot"?.045f:.11f;
        int len=(int)(22050*duration);var data=new float[len];float f=name=="shot"?1600:name=="item"?1300:name=="move"?520:name=="select"?780:name=="bomb"?110:260;
        for(int i=0;i<len;i++){float t=i/22050f,env=Mathf.Pow(1-i/(float)len,2);float sweep=name=="bomb"||name=="death"?1-t/duration*.8f:1+t/duration*.35f;data[i]=Mathf.Sin(t*f*sweep*6.283f)*env*(name=="shot"?.12f:.32f);}
        var clip=AudioClip.Create(name,len,1,22050,false);clip.SetData(data,0);return clip;
    }
    void Sound(string name){if(effects.TryGetValue(name,out var clip)&&volume>0)audioSource.PlayOneShot(clip,volume/100f);}
    bool Key(KeyCode k)=>Input.GetKeyDown(k);
    void Update(){
        clock+=Time.unscaledDeltaTime;
        if(Key(KeyCode.F11)){fullScreen=!Screen.fullScreen;Screen.fullScreen=fullScreen;}
        if(smoke)return;
        if(qaInput&&Key(KeyCode.F12))StartCoroutine(CaptureInputState());
        if(UpdateContent())return;
        if(page==Page.Game){
            if(game.state==RunState.Complete&&!savedResult)SaveResult();
            if(Key(KeyCode.Escape)&&game.state==RunState.Playing){paused=!paused;cursor=0;accumulator=0;Sound("move");}
            if(paused||game.state==RunState.GameOver||game.state==RunState.Complete){
                if(Key(KeyCode.UpArrow)||Key(KeyCode.DownArrow)){cursor=1-cursor;Sound("move");}
                if(Key(KeyCode.Z)||Key(KeyCode.Return))GameMenuActivate();return;
            }
            if(Key(KeyCode.X))bombQueued=true;
            accumulator+=Mathf.Min(Time.deltaTime,.1f);
            Controls input=new Controls{move=new Vector2((Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.UpArrow)?1:0)),focus=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift),fire=Input.GetKey(KeyCode.Z)};
            while(accumulator>=1f/60){input.bomb=bombQueued;bombQueued=false;game.Tick(input);accumulator-=1f/60;if(CheckStory()){accumulator=0;break;}}
            return;
        }
        int count=PageCount();
        if(Key(KeyCode.DownArrow)){cursor=(cursor+1)%count;Sound("move");}
        if(Key(KeyCode.UpArrow)){cursor=(cursor+count-1)%count;Sound("move");}
        if(Key(KeyCode.LeftArrow))Adjust(-1);
        if(Key(KeyCode.RightArrow))Adjust(1);
        if(Key(KeyCode.Z)||Key(KeyCode.Return))Activate();
        if(Key(KeyCode.Escape)||Key(KeyCode.X))Back();
    }
    int PageCount(){switch(page){case Page.Title:return 7;case Page.Character:return 4;case Page.Difficulty:return 4;case Page.Options:return 7;case Page.Music:return 24;default:return 1;}}
    void Adjust(int d){
        if(page==Page.Character){cursor=(cursor+d+4)%4;Sound("move");}
        if(page==Page.Options){switch(cursor){case 0:startingLives=Mathf.Clamp(startingLives+d,1,6);break;case 1:startingBombs=Mathf.Clamp(startingBombs+d,0,3);break;case 2:musicVolume=Mathf.Clamp(musicVolume+d*10,0,100);break;case 3:volume=Mathf.Clamp(volume+d*10,0,100);break;case 4:Screen.fullScreen=!Screen.fullScreen;break;case 5:ToggleGod();break;}SaveOptions();Sound("move");}
    }
    void Activate(){
        Sound("select");
        switch(page){
            case Page.Title:
                if(cursor<=1){mode=cursor;page=Page.Character;cursor=character;}
                else{page=cursor==2?Page.Records:cursor==3?Page.Music:cursor==4?Page.Options:cursor==5?Page.Help:Page.Title;if(cursor==6)Application.Quit();cursor=0;}break;
            case Page.Character:
                character=cursor;
                if(mode==1){if(PlayerPrefs.GetInt("extra"+character,0)==0){Sound("danger");return;}StartRun(6,4,false);}
                else{page=Page.Difficulty;cursor=difficulty;}break;
            case Page.Difficulty:difficulty=cursor;StartRun(0,difficulty,false);break;
            case Page.Options:if(cursor==6)Back();else Adjust(1);break;
            case Page.Music:if(cursor==23)Back();else PlayMusic(cursor+1);break;
            default:Back();break;
        }
    }
    void Back(){Sound("move");if(page==Page.Character){page=Page.Title;cursor=mode;}else if(page==Page.Difficulty){page=Page.Character;cursor=character;}else{page=Page.Title;cursor=0;}}
    void SaveOptions(){if(smoke||qaInput)return;PlayerPrefs.SetInt("startingLives",startingLives);PlayerPrefs.SetInt("startingBombs",startingBombs);PlayerPrefs.SetInt("volume",volume);PlayerPrefs.SetInt("musicVolume",musicVolume);PlayerPrefs.Save();}
    void StartRun(int stage,int diff,bool practice){game=new GameModel(character,diff,stage,practice,startingLives,startingBombs);game.godMode=godEnabled;game.sound=Sound;page=Page.Game;paused=false;savedResult=false;cursor=0;accumulator=0;bombQueued=false;ResetStory();}
    string ScoreKey()=>"high_"+game.character+"_"+game.difficulty;
    long HighScore(int c,int d){long.TryParse(PlayerPrefs.GetString("high_"+c+"_"+d,"0"),out long n);return n;}
    void SaveResult(){if(game==null||savedResult)return;savedResult=true;if(!game.practice&&!smoke&&!qaInput){if(game.score>HighScore(game.character,game.difficulty))PlayerPrefs.SetString(ScoreKey(),game.score.ToString());if(game.state==RunState.Complete&&!game.extra&&game.continues==0)PlayerPrefs.SetInt("extra"+game.character,1);PlayerPrefs.Save();}}
    void GameMenuActivate(){
        Sound("select");
        if(paused&&cursor==0){paused=false;accumulator=0;return;}
        if(game.state==RunState.GameOver&&cursor==0){SaveResult();if(game.Continue()){savedResult=false;cursor=0;return;}}
        if(game.state==RunState.Complete&&cursor==0){StartRun(game.extra?6:0,game.difficulty,false);return;}
        SaveResult();page=Page.Title;paused=false;cursor=0;
    }
    void OnApplicationFocus(bool focus){if(!focus&&!smoke&&page==Page.Game&&game.state==RunState.Playing){paused=true;cursor=0;accumulator=0;}}
    void OnApplicationQuit(){if(!smoke){SaveResult();SaveOptions();}}
    void OnGUI(){
        float scale=Mathf.Min(Screen.width/1024f,Screen.height/720f);float ox=(Screen.width-1024*scale)/2,oy=(Screen.height-720*scale)/2;
        GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
        GUI.color=Color.white;
        if(page==Page.Game){if(endingScreen||creditsScreen)DrawEnding();else{DrawGame();if(tale!=null)DrawTale();}return;}
        Tex(title,new Rect(0,0,1024,720),Color.white);
        if(page==Page.Title)DrawTitle();else DrawSubPage();
        RectFill(24,690,976,1,new Color(.65f,.53f,.58f,.28f));
        Label("↑ ↓  选择     Z / ENTER  确认     X / ESC  返回",40,697,600,18,11,muted,mono);
        Label("F11  全屏",873,697,112,18,11,muted,mono,TextAnchor.MiddleRight);
    }
    void Tex(Texture t,Rect r,Color color){GUI.color=color;if(t!=null)GUI.DrawTexture(r,t,ScaleMode.StretchToFill,true);GUI.color=Color.white;}
    void RectFill(float x,float y,float w,float h,Color c)=>Tex(white,new Rect(x,y,w,h),c);
    void Label(string text,float x,float y,float w,float h,int size,Color color,Font font=null,TextAnchor anchor=TextAnchor.MiddleLeft){textStyle.font=font??body;textStyle.fontSize=size;textStyle.normal.textColor=color;textStyle.alignment=anchor;textStyle.wordWrap=false;textStyle.fontStyle=FontStyle.Normal;GUI.Label(new Rect(x,y,w,h),text,textStyle);}
    void RotateGUI(Vector2 p,float degrees){GUI.matrix=GUI.matrix*Matrix4x4.Translate(new Vector3(p.x,p.y,0))*Matrix4x4.Rotate(Quaternion.Euler(0,0,degrees))*Matrix4x4.Translate(new Vector3(-p.x,-p.y,0));}
    bool ClipEdge(float p,float q,ref float lo,ref float hi){if(Mathf.Abs(p)<.0001f)return q>=0;float r=q/p;if(p<0){if(r>hi)return false;lo=Mathf.Max(lo,r);}else{if(r<lo)return false;hi=Mathf.Min(hi,r);}return true;}
    void Line(Vector2 a,Vector2 b,Color color,float width=1){if(drawOrigin!=Vector2.zero){Vector2 d=b-a;float lo=0,hi=1;if(!ClipEdge(-d.x,a.x,ref lo,ref hi)||!ClipEdge(d.x,576-a.x,ref lo,ref hi)||!ClipEdge(-d.y,a.y,ref lo,ref hi)||!ClipEdge(d.y,672-a.y,ref lo,ref hi))return;b=a+d*hi;a+=d*lo;}var old=GUI.matrix;RotateGUI(a,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);RectFill(a.x,a.y-width/2,(b-a).magnitude,width,color);GUI.matrix=old;}
    void Diamond(float x,float y,float size,Color c){var old=GUI.matrix;RotateGUI(new Vector2(x,y),45);RectFill(x-size/2,y-size/2,size,size,c);GUI.matrix=old;}
    void Outline(Rect r,Color c){RectFill(r.x,r.y,r.width,1,c);RectFill(r.x,r.yMax-1,r.width,1,c);RectFill(r.x,r.y,1,r.height,c);RectFill(r.xMax-1,r.y,1,r.height,c);}
    void Rule(float y){RectFill(656,y,320,1,new Color(.65f,.53f,.58f,.25f));}
    bool MenuRow(string cn,string en,int index,float x,float y,float w=300,bool disabled=false){
        Rect r=new Rect(x,y,w,44);bool hover=r.Contains(Event.current.mousePosition);
        if(hover&&Event.current.type==EventType.MouseMove){cursor=index;}
        bool active=cursor==index;
        if(active){RectFill(x,y+3,2,33,red);RectFill(x+8,y+40,w-8,1,new Color(.8f,.38f,.45f,.42f));Diamond(x+16,y+22,5,red);}
        Label(cn,x+32,y,w-55,42,22,disabled?C("706574"):active?ivory:muted,display);
        Label(en,x+31,y+33,w-40,14,9,disabled?C("625666"):active?gold:C("807383"),mono);
        if(GUI.Button(r,GUIContent.none,GUIStyle.none)){cursor=index;return true;}return false;
    }
    void DrawTitle(){
        Tex(reimu,new Rect(33,-2,572,687),Color.white);
        Label("東方 Project  第五弾",649,65,320,24,16,gold,display);
        Label("東方",649,103,300,67,53,ivory,display);
        Label("怪綺談",636,159,360,115,92,ivory,display);
        RectFill(656,289,40,2,red);Label("M Y S T I C   S Q U A R E",710,278,282,25,13,gold,mono);
        Label("非官方重制",658,315,310,24,12,muted);
        string[] cn={"开始游戏","Extra Start","最高得分","音乐室","游戏设置","操作说明","退出游戏"};
        string[] en={"START","EXTRA STAGE","HIGH SCORE","MUSIC ROOM","OPTION","HOW TO PLAY","QUIT"};
        for(int i=0;i<7;i++)if(MenuRow(cn[i],en[i],i,650,343+i*46,310)){Activate();break;}
        Label("重绘画面 · 独立实现",52,641,500,24,13,C("b5a1b0"),display);
        Label("原作  ZUN / 上海アリス幻樂団",52,664,530,18,10,muted);
    }
    void Header(string cn,string en){Label(cn,70,50,900,66,44,ivory,display);RectFill(74,128,52,2,red);Label(en,145,116,700,24,13,gold,mono);}
    void DrawSubPage(){
        RectFill(0,0,1024,690,new Color(.055f,.045f,.075f,.72f));
        if(page==Page.Character){
            Header("选择自机","PLAYER SELECT   /   "+(mode==0?"START":mode==1?"EXTRA":"PRACTICE"));
            int c=cursor;
            if(c==0)Tex(reimu,new Rect(15,60,510,612),Color.white);
            else Tex(portraits[c],new Rect(10,119,505,556),Color.white);
            for(int i=0;i<4;i++)if(MenuRow(GameModel.Names[i],new[]{"REIMU HAKUREI","MARISA KIRISAME","MIMA","YUUKA"}[i],i,543,188+i*65,375)){Activate();return;}
            RectFill(575,468,340,1,C("5c4358"));
            Label(new[]{"自动追尾型","前方集中型","高速射速型","广范围型"}[c],575,493,340,35,26,ivory,display);
            string[] desc={"追踪符札。攻击范围广，移动较慢。","魔法飞弹。集中火力，正面输出高。","能量沐浴。高速移动，攻击范围窄。","花之射击。广域散射，Bomb 持续较长。"};
            Label(desc[c],575,540,410,30,14,muted);
            Label("移动  "+new[]{"◆","◆◆◆","◆◆◆◆◆","◆"}[c]+"     攻击  "+new[]{"◆","◆◆◆◆◆","◆◆◆◆◆","◆◆"}[c],575,582,390,30,13,gold);
            if(mode==1&&PlayerPrefs.GetInt("extra"+c,0)==0)Label("解锁条件：使用此角色不续关完成六面",575,630,415,28,13,red);
        } else if(page==Page.Difficulty){
            Header("选择难度","SELECT DIFFICULTY");
            Label(GameModel.Names[character],76,205,440,62,34,ivory,display);
            Tex(players[character],new Rect(130,297,160,192),Color.white);
            string[] cn={"简单","普通","困难","狂气"};string[] descriptions={"较少的弹幕，较低的弹速。","标准难度。","更密集的弹幕与更快的弹速。","高密度、高强度的弹幕。"};
            for(int i=0;i<4;i++)if(MenuRow(cn[i],GameModel.Difficulties[i],i,530,216+i*79,350)){Activate();return;}
            Label(descriptions[cursor],562,574,400,32,15,muted);
        } else if(page==Page.Options){
            Header("游戏设置","OPTION");
            string[] rows={"初始生命","初始 Bomb","音乐音量","音效音量","显示模式","无敌开关 · F1","返回"};
            string[] val={startingLives.ToString(),startingBombs.ToString(),musicVolume+" %",volume+" %",Screen.fullScreen?"全屏":"窗口",godEnabled?"ON · 开启":"OFF · 关闭",""};
            for(int i=0;i<7;i++){
                if(MenuRow(rows[i],new[]{"PLAYER STOCK","BOMB STOCK","BGM VOLUME","SOUND EFFECT","DISPLAY","INVINCIBLE","BACK"}[i],i,150,178+i*66,450)){if(i==6)Back();else Adjust(1);return;}
                Label(val[i],650,180+i*66,145,40,19,i==5&&godEnabled?red:ivory,body,TextAnchor.MiddleCenter);
                if(i<6){Label("‹",616,184+i*66,35,35,25,gold,mono,TextAnchor.MiddleCenter);Label("›",799,184+i*66,35,35,25,gold,mono,TextAnchor.MiddleCenter);if(GUI.Button(new Rect(616,184+i*66,35,35),GUIContent.none,GUIStyle.none)){cursor=i;Adjust(-1);}if(GUI.Button(new Rect(799,184+i*66,35,35),GUIContent.none,GUIStyle.none)){cursor=i;Adjust(1);}}
            }
        } else if(page==Page.Help){
            Header("操作说明","HOW TO PLAY");
            string[] keys={"↑ ↓ ← →","Z / ENTER","SHIFT","X","ESC","F1","F11","CTRL"};string[] words={"移动自机","射击 / 推进剧情","低速移动","释放 Bomb","暂停 / 返回","无敌开关","切换全屏","跳过当前剧情"};
            for(int i=0;i<8;i++){Outline(new Rect(104,181+i*54,149,36),C("705264"));Label(keys[i],104,181+i*54,149,36,16,gold,mono,TextAnchor.MiddleCenter);Label(words[i],288,181+i*54,300,36,20,ivory,display);}
            RectFill(598,196,1,329,C("564051"));
            Label("道具与得分",643,194,315,42,27,ivory,display);
            string[] help={"P  提升火力，最高 127。","点  高处回收时得分更高。","每累计 100 个点道具增加生命。","擦过敌弹增加擦弹数与得分。","梦槽满时消弹，点道具得分提高。","Bomb 清弹、收道具并降低梦槽。"};for(int i=0;i<help.Length;i++)Label(help[i],643,253+i*43,342,32,14,muted);
            if(MenuRow("返回","BACK",0,643,583,300))Back();
        } else if(page==Page.Music){DrawMusicRoom();
        } else if(page==Page.Records){
            Header("最高得分","HIGH SCORE");
            Label("PLAYER",97,187,225,30,14,gold,mono);
            for(int d=0;d<5;d++)Label(GameModel.Difficulties[d],326+d*120,187,116,30,12,gold,mono,TextAnchor.MiddleRight);
            for(int c=0;c<4;c++){float y=253+c*78;Tex(players[c],new Rect(98,y,32,38),Color.white);Label(GameModel.Names[c],147,y,178,38,21,ivory,display);for(int d=0;d<5;d++)Label(HighScore(c,d).ToString("D9"),326+d*120,y,116,38,15,muted,mono,TextAnchor.MiddleRight);RectFill(98,y+56,818,1,C("493747"));}
            if(MenuRow("返回","BACK",0,647,602,267))Back();
        }
    }
    void DrawGame(){
        RectFill(0,0,1024,720,ink);
        // Use one explicit coordinate transform. GUI.BeginGroup rotates its clip rectangle
        // with GUI.matrix, which otherwise truncates angled beams before their hitboxes end.
        var playfieldMatrix=GUI.matrix;GUI.matrix=playfieldMatrix*Matrix4x4.Translate(new Vector3(24,24,0));
        drawOrigin=new Vector2(24,24);
        float scroll=(game.StageTime*25)%672;
        Tex(stages[game.stage],new Rect(0,scroll,576,672),Color.white);Tex(stages[game.stage],new Rect(0,scroll-672,576,672),Color.white);
        RectFill(0,0,576,672,new Color(.03f,.025f,.06f,.23f));
        for(int i=0;i<20;i++){float x=(i*79.43f)%576,y=(i*103.7f+game.StageTime*(8+i%4*8))%672;Tex(glow,new Rect(x-4,y-4,8,8),new Color(.8f,.6f,.8f,.3f));}
        if(game.HasBoss){
            var e=game.foes.Find(f=>f.boss);MagicCircle(e.p,80+8*Mathf.Sin(clock),game.stage==3?blue:red,clock*10);
            if(game.stage==5&&game.phase>=2&&game.phase<8)Wings(e.p,game.phase>=5);
        }
        foreach(var s in game.shots){Color c=new[]{C("f3bec6"),C("b7d9ff"),C("9ce4ce"),C("f4bded")}[game.character];float angle=Mathf.Atan2(s.v.y,s.v.x)*Mathf.Rad2Deg+90;RotatedTex(orb,new Vector2(s.p.x*1.5f,s.p.y*1.5f),new Vector2(4.5f,16),angle,c);}
        foreach(var d in game.drops){Color c=d.kind==1?blue:d.kind==3?C("a2d3a1"):d.kind==4?gold:red;float x=d.p.x*1.5f,y=d.p.y*1.5f;RectFill(x-8,y-8,16,16,C("f3e4d0"));RectFill(x-7,y-7,14,14,c*.8f);Label(new[]{"P","点","P","B","1","夢","F"}[d.kind],x-8,y-9,16,18,d.kind==2?14:11,Color.white,mono,TextAnchor.MiddleCenter);}
        foreach(var e in game.foes){Texture2D tex=e.boss?(game.stage==3&&e.side==1?mai:bosses[game.stage]):e.mid?midArt[game.stage]:enemy;float size=e.boss?42:e.mid?35:25;Tex(tex,new Rect((e.p.x-size/2)*1.5f,(e.p.y-size*.6f)*1.5f,size*1.5f,size*1.2f*1.5f),Color.white);}
        DrawLasers();
        DrawEnemyBullets();
        if(game.state!=RunState.GameOver&&(game.invincible<=0||Mathf.FloorToInt(clock*16)%2==0)){
            Vector2 p=game.player*1.5f;
            if(game.character==0){for(int side=-1;side<=1;side+=2){float x=p.x+side*(game.focus?20:30);Tex(orb,new Rect(x-7,p.y-4,14,14),ivory);Tex(orb,new Rect(x-6,p.y-3,11,11),red);Tex(orb,new Rect(x-2,p.y-3,5,5),ivory);}}
            Tex(players[game.character],new Rect(p.x-20,p.y-17,40,48),Color.white);
        }
        foreach(var s in game.sparks){float a=s.life/s.maxLife;Tex(glow,new Rect(s.p.x*1.5f-6,s.p.y*1.5f-6,12,12),new Color(1,.72f,s.color==1?1:.55f,a));}
        DrawBomb();
        if(game.HasBoss){float hp=0,max=0;foreach(var e in game.foes)if(e.boss){hp+=e.hp;max+=e.maxHp;}RectFill(15,14,476,5,C("443545"));RectFill(15,14,476*Mathf.Clamp01(hp/max),5,red);Label(Mathf.CeilToInt(game.bossTime).ToString("D2"),503,0,55,36,25,ivory,mono,TextAnchor.MiddleRight);Label(game.stage==3&&game.phase>0?(game.foes[0].side==0?"雪":"舞"):GameModel.BossNames[game.stage],15,24,400,28,17,ivory,display);}
        if(game.StageTime<3.5f){float a=Mathf.Min(1,3.5f-game.StageTime);RectFill(60,251,456,119,new Color(.08f,.055f,.12f,a*.76f));Label(game.stage==6?"EXTRA STAGE":"STAGE 0"+(game.stage+1),60,257,456,27,12,new Color(gold.r,gold.g,gold.b,a),mono,TextAnchor.MiddleCenter);Label(GameModel.StageNames[game.stage],50,285,476,50,31,new Color(ivory.r,ivory.g,ivory.b,a),display,TextAnchor.MiddleCenter);Label(GameModel.StageEnglish[game.stage],50,334,476,24,12,new Color(muted.r,muted.g,muted.b,a),mono,TextAnchor.MiddleCenter);}
        if(game.noticeTime>0)Label(game.notice,70,174,436,40,25,ivory,display,TextAnchor.MiddleCenter);
        if(game.state==RunState.StageClear){RectFill(54,229,468,172,new Color(.07f,.045f,.11f,.92f));Label("STAGE CLEAR",70,244,436,42,28,gold,mono,TextAnchor.MiddleCenter);Label("过关奖励",70,294,436,32,22,ivory,display,TextAnchor.MiddleCenter);Label(game.clearBonus.ToString("N0"),70,333,436,46,29,ivory,mono,TextAnchor.MiddleCenter);}
        GUI.matrix=playfieldMatrix;drawOrigin=Vector2.zero;
        RectFill(0,0,1024,24,ink);RectFill(0,696,1024,24,ink);RectFill(0,0,24,720,ink);RectFill(600,0,424,720,ink);
        Tex(title,new Rect(620,0,1024,720),new Color(.58f,.5f,.62f,1));RectFill(614,0,410,720,new Color(.05f,.035f,.065f,.64f));
        Outline(new Rect(20,20,584,680),C("745267"));Outline(new Rect(23,23,578,674),C("302332"));DrawHUD();
        if(tale==null&&(paused||game.state==RunState.GameOver||game.state==RunState.Complete))DrawGameMenu();
    }
    void RotatedTex(Texture tex,Vector2 p,Vector2 size,float angle,Color c){var old=GUI.matrix;RotateGUI(p,angle);Tex(tex,new Rect(p.x-size.x/2,p.y-size.y/2,size.x,size.y),c);GUI.matrix=old;}
    void MagicCircle(Vector2 center,float radius,Color color,float rotation){Vector2 p=center*1.5f;float r=radius*1.5f;Color c=new Color(color.r,color.g,color.b,.22f);Tex(ring,new Rect(p.x-r,p.y-r,r*2,r*2),c);Tex(ring,new Rect(p.x-r*.83f,p.y-r*.83f,r*1.66f,r*1.66f),c);for(int i=0;i<6;i++){float a=(i*60+rotation)*Mathf.Deg2Rad,b=(i*60+rotation+120)*Mathf.Deg2Rad;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.83f,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r*.83f,c);}}
    void Wings(Vector2 center,bool purple){Vector2 p=center*1.5f;for(int side=-1;side<=1;side+=2)for(int wing=0;wing<3;wing++)for(int i=0;i<8;i++){Vector2 end=p+new Vector2(side*(65+i*10),(-55+wing*48)+i*2);Line(p+new Vector2(side*12,10),end,purple?new Color(.7f,.45f,.85f,.36f):new Color(.85f,.8f,.9f,.36f),6);}}
    void DrawHUD(){
        Label("東方怪綺談",650,26,345,61,41,ivory,display);Label("M Y S T I C   S Q U A R E",658,88,320,20,12,gold,mono);Rule(121);
        Label(game.extra?"EXTRA STAGE":"STAGE 0"+(game.stage+1),658,139,180,23,13,gold,mono);Label(GameModel.Difficulties[game.difficulty],841,139,132,23,12,red,mono,TextAnchor.MiddleRight);
        Label(GameModel.StageNames[game.stage],655,164,327,38,24,ivory,display);Rule(220);
        Label("最高得分",658,236,160,22,13,muted);Label(Math.Max(game.score,HighScore(game.character,game.difficulty)).ToString("D10"),657,260,320,34,29,gold,mono,TextAnchor.MiddleRight);
        Label("得分",658,308,140,22,13,muted);Label(game.score.ToString("D10"),653,333,324,41,34,ivory,mono,TextAnchor.MiddleRight);Rule(393);
        Label("生命",658,410,68,27,15,muted);for(int i=0;i<Math.Min(9,Math.Max(0,game.lives-1));i++)Diamond(744+i*24,424,9,red);if(game.lives>10)Label("+"+(game.lives-10),944,411,45,26,13,ivory,mono);
        Label("BOMB",658,449,68,27,14,muted,mono);for(int i=0;i<Math.Min(9,game.bombs);i++)Diamond(744+i*24,463,8,gold);if(game.bombs>9)Label("+"+(game.bombs-9),944,450,45,26,13,ivory,mono);
        Rule(492);
        Label("夢",656,505,42,36,27,ivory,display);Label(game.dream>=128?"MAX":Mathf.FloorToInt(game.dream).ToString("D3"),875,506,100,30,18,game.dream>=128?ivory:gold,mono,TextAnchor.MiddleRight);
        RectFill(705,548,270,5,C("463447"));RectFill(705,548,270*game.dream/128,5,game.dream>=128?ivory:red);
        Label("POWER",658,568,105,25,12,muted,mono);Label(game.power==127?"MAX 127":game.power.ToString("D3")+" / 127",827,567,148,26,17,blue,mono,TextAnchor.MiddleRight);
        Label("点  "+game.points.ToString("D3")+" / "+game.totalPoints.ToString("D3"),658,607,188,23,13,muted,mono);Label("擦弹  "+game.graze.ToString("D3"),840,607,138,23,13,muted,mono,TextAnchor.MiddleRight);
        Rule(644);Tex(players[game.character],new Rect(657,658,24,29),Color.white);Label(GameModel.Names[game.character],691,658,217,26,18,ivory,display);if(game.practice)Label("练习",923,658,54,26,12,gold);
        Label(game.godMode?"F1  无敌 ON":"F1  无敌 OFF",848,671,133,20,11,game.godMode?red:muted,mono,TextAnchor.MiddleRight);
        Label("Z 射击   X Bomb   Shift 低速   Esc 暂停",652,693,346,18,10,muted,mono);
    }
    void DrawGameMenu(){
        RectFill(24,24,576,672,new Color(.035f,.025f,.06f,.83f));
        string heading=paused?"暂 停":game.state==RunState.GameOver?"满身疮痍":game.practice?"练习完成":"通 关";
        Label(heading,69,185,486,74,45,ivory,display,TextAnchor.MiddleCenter);
        Label(paused?"PAUSE":game.state==RunState.GameOver?"GAME OVER":game.extra?"EXTRA CLEAR":"ALL CLEAR",69,265,486,26,14,gold,mono,TextAnchor.MiddleCenter);
        if(!paused)Label("SCORE  "+game.score.ToString("D10"),69,312,486,33,22,ivory,mono,TextAnchor.MiddleCenter);
        bool canContinue=!game.extra&&!game.practice&&game.continues<3;
        string primary=paused?"继续游戏":game.state==RunState.GameOver?(canContinue?"续关  ·  剩余 "+(3-game.continues)+" 次":"返回标题"):"再次挑战";
        if(MenuRow(primary,paused?"RESUME":game.state==RunState.GameOver?"CONTINUE":"RETRY",0,133,388,345)){GameMenuActivate();return;}
        if(MenuRow("返回标题","RETURN TO TITLE",1,133,456,345)){GameMenuActivate();return;}
        if(game.state==RunState.Complete&&!game.practice&&!game.extra&&game.continues==0)Label("此角色的 Extra 已解锁",85,552,452,31,17,gold,display,TextAnchor.MiddleCenter);
    }
    IEnumerator SmokeTest(){
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA"));Directory.CreateDirectory(root);
        yield return new WaitForSecondsRealtime(1);yield return Capture(root,"01-title.png");
        cursor=0;Activate();Require(page==Page.Character,"START opens character selection");
        yield return Capture(root,"02-character.png");
        cursor=0;Activate();Require(page==Page.Difficulty,"Character confirmation opens difficulty");
        yield return Capture(root,"06-difficulty.png");
        cursor=1;Activate();Require(page==Page.Game&&game.character==0&&game.difficulty==1,"Difficulty starts selected run");game.power=71;game.stageFrame=3150;game.StartBoss();
        for(int i=0;i<240;i++){game.invincible=10;game.Tick(new Controls{fire=true,focus=true});}
        yield return Capture(root,"03-battle.png");
        character=1;StartRun(5,2,true);game.stageFrame=1200;game.StartBoss();game.phase=4;
        for(int i=0;i<170;i++){game.invincible=10;game.Tick(new Controls{fire=true});}
        yield return Capture(root,"04-shinki.png");paused=true;cursor=0;
        yield return Capture(root,"05-pause.png");GameMenuActivate();Require(!paused,"Pause resume action");
        paused=true;cursor=1;GameMenuActivate();Require(page==Page.Title,"Pause return to title");
        cursor=4;Activate();Require(page==Page.Options,"Options navigation");
        int previous=startingLives;startingLives=3;cursor=0;Adjust(-1);Require(startingLives==2,"Decrease option value");startingLives=previous;
        yield return Capture(root,"07-options.png");Back();cursor=5;Activate();
        Require(page==Page.Help,"Help navigation");yield return Capture(root,"08-help.png");Back();cursor=2;Activate();
        Require(page==Page.Records,"Score navigation");yield return Capture(root,"09-records.png");
        yield return ExtendedSmoke(root);
        File.WriteAllText(Path.Combine(root,"render-smoke.txt"),"Native Unity rendering and navigation checks passed.\nStart, four characters, difficulty, pause/resume, return, options, invincibility ON/OFF, help, records, 23 music tracks, encounter, laser battle, both twins, all 12 ending routes, credits and result flow checked.\nOS input verification is recorded separately in input-verification.txt.\n");Application.Quit();
    }
    void Require(bool value,string name){if(!value){Debug.LogError("UI_TEST_FAILED "+name);Application.Quit(2);throw new Exception(name);}Debug.Log("UI_TEST_PASS "+name);}
    IEnumerator Capture(string root,string name){yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Texture2D capture=ScreenCapture.CaptureScreenshotAsTexture();Require(capture!=null,"capture "+name);string path=Path.Combine(root,name);File.WriteAllBytes(path,capture.EncodeToPNG());Destroy(capture);Require(new FileInfo(path).Length>10000,"PNG "+name);}
}
}
