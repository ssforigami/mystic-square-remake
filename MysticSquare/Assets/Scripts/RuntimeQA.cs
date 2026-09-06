using System.Collections;
using System.IO;
using UnityEngine;
namespace MysticSquare {
public sealed partial class MysticGame {
    IEnumerator CaptureInputState(){
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA"));Directory.CreateDirectory(root);
        yield return Capture(root,"input-last.png");
        File.WriteAllText(Path.Combine(root,"input-last.txt"),"page="+page+"\ncursor="+cursor+"\ncharacter="+character+"\ngod="+godEnabled+"\npaused="+paused+"\ntale="+(tale!=null)+"\nstate="+(game==null?"none":game.state.ToString())+"\nplayer="+(game==null?"none":game.player.ToString())+"\nshots="+(game==null?0:game.shots.Count)+"\nbombs="+(game==null?0:game.bombs)+"\nmusic="+currentMusic);
    }
    IEnumerator ExtendedSmoke(string root){
        page=Page.Character;
        for(int c=1;c<4;c++){cursor=c;yield return Capture(root,"10-character-"+c+".png");}
        page=Page.Options;cursor=5;bool before=godEnabled;Adjust(1);Require(godEnabled!=before,"God mode option ON");yield return Capture(root,"11-god-option.png");Adjust(-1);Require(godEnabled==before,"God mode option OFF");
        page=Page.Music;cursor=14;
        for(int i=1;i<=23;i++){PlayMusic(i);Require(musicSource.clip!=null&&musicSource.clip.length>10,"Track "+i+" ready");}
        PlayMusic(15);yield return Capture(root,"12-music-room.png");
        character=0;StartRun(0,1,false);game.StartBoss();Require(CheckStory(true)&&tale!=null,"Encounter freezes gameplay");yield return Capture(root,"13-encounter.png");AdvanceTale(true);Require(tale==null,"Encounter advances to battle");
        character=1;StartRun(4,2,true);game.StartBoss();game.phase=3;game.stageFrame=6000;game.godMode=true;
        for(int i=0;i<320;i++)game.Tick(new Controls());
        yield return Capture(root,"14-laser-battle.png");
        game.CancelBullets(false);game.foes.Clear();game.lasers.Add(new Laser{p=new Vector2(96,96),angle=Mathf.PI/3,age=1});game.lasers.Add(new Laser{p=new Vector2(288,120),angle=2.1f,age=1});yield return Capture(root,"20-laser-alignment.png");
        character=2;StartRun(6,4,true);game.StartBoss();game.phase=6;game.stageFrame=6000;game.godMode=true;
        for(int i=0;i<220;i++)game.Tick(new Controls());yield return Capture(root,"15-extra-battle.png");
        for(int side=0;side<2;side++){character=0;StartRun(3,1,false);game.StartBoss();game.foes.RemoveAt(1-side);game.phase=1;seenBossStage=3;Require(CheckStory(true)&&tale!=null,"Twin story branch "+side);yield return Capture(root,"16-twin-"+side+".png");AdvanceTale(true);}
        for(int c=0;c<4;c++)for(int route=0;route<3;route++){
            character=c;StartRun(route==2?6:0,1,false);game.state=RunState.Complete;game.continues=route==1?1:0;
            Require(UpdateContent()&&endingScreen&&tale!=null,"Ending route "+c+" "+route);
            yield return Capture(root,"17-ending-"+c+"-"+route+".png");
            AdvanceTale(true);Require(creditsScreen&&!endingScreen&&tale==null,"Ending to credits "+c+" "+route);
        }
        yield return Capture(root,"18-credits.png");creditsClock=36;Require(UpdateContent()&&!creditsScreen,"Credits finish");yield return Capture(root,"19-result.png");
        // Exercise real draw frames under a high-density attack; this measures this host only.
        character=0;StartRun(6,4,true);game.StartBoss();game.phase=8;game.stageFrame=6000;game.godMode=true;
        for(int i=0;i<300;i++)game.Tick(new Controls());
        game.CancelBullets(false);for(int i=0;i<1800;i++)game.Bullet(new Vector2(10+i%60*6,35+i/60*12),Mathf.PI/2,10,i%3);
        float start=Time.realtimeSinceStartup;int peak=game.bullets.Count;
        for(int i=0;i<180;i++){game.Tick(new Controls());peak=Mathf.Max(peak,game.bullets.Count);yield return null;}
        float seconds=Time.realtimeSinceStartup-start;
        File.WriteAllText(Path.Combine(root,"render-performance.txt"),"180 rendered frames in "+seconds.ToString("F2")+" seconds; measured FPS "+(180/seconds).ToString("F1")+"; peak bullets "+peak+". Native current window, VSync enabled.\n");
    }
}
}
