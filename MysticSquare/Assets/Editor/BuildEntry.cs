using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using MysticSquare;

public static class BuildEntry {
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception("FAILED: "+name);checks++;Debug.Log("PASS: "+name);}
    public static void Run(){
        try {
            PlayerSettings.companyName="IndependentRebuild";
            PlayerSettings.productName="東方怪綺談 · Mystic Square";
            PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.defaultScreenWidth=1024;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            QualitySettings.vSyncCount=1;
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"})){
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();
            }
            var audioGuids=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Music"});Check(audioGuids.Length==23,"All 23 independently rendered music tracks present");
            foreach(string guid in audioGuids){var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.55f;settings.preloadAudioData=false;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
            RunTests();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity");
            string output=Path.GetFullPath("../Build/MysticSquare.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.WriteAllText("../QA/build-result.txt","Unity "+Application.unityVersion+"\n"+checks+" simulation checks passed.\nWindows x64 build succeeded.\nSize: "+report.summary.totalSize+" bytes\n");
            Debug.Log("BUILD_AND_TEST_SUCCESS "+checks);EditorApplication.Exit(0);
        }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void RunTests(){
        Directory.CreateDirectory("../QA");
        var g=new GameModel();Check(g.lives==3&&g.bombs==3&&g.power==0,"Default resources");
        var p=g.player;g.Tick(new Controls{move=Vector2.right});float fast=g.player.x-p.x;
        p=g.player;g.Tick(new Controls{move=Vector2.right,focus=true});Check(g.player.x-p.x<fast*.5f,"Focus movement");
        for(int i=0;i<300;i++)g.Tick(new Controls{move=new Vector2(1,1)});Check(g.player.x<=375&&g.player.y<=436,"Playfield bounds");
        g=new GameModel();g.Tick(new Controls{fire=true});Check(g.shots.Count>0,"Continuous shot spawn");
        for(int c=0;c<4;c++){g=new GameModel(c);g.Tick(new Controls{fire=true});Check(g.shots.Exists(s=>s.homing)==(c==0),"Character "+c+" shot identity");}
        g=new GameModel();g.Bullet(g.player,0,0);g.invincible=0;g.Tick(new Controls());Check(g.missPending&&g.lives==3,"Deathbomb grace window");g.Bomb();Check(!g.missPending&&g.bombs==2&&g.lives==3&&g.bullets.Count==0,"Deathbomb saves life");
        g=new GameModel();g.invincible=0;g.Bullet(g.player,0,0);for(int i=0;i<12;i++)g.Tick(new Controls());Check(g.lives==2&&g.invincible>0,"Collision loses exactly one life");
        g=new GameModel();g.invincible=0;g.godMode=true;g.Bullet(g.player,0,0);for(int i=0;i<30;i++)g.Tick(new Controls());Check(g.lives==3&&!g.missPending&&g.usedGodMode,"Invincibility switch prevents bullet death");g.godMode=false;for(int i=0;i<12;i++)g.Tick(new Controls());Check(g.lives==2,"Switch off restores bullet collision");
        g=new GameModel();g.invincible=0;g.lasers.Add(new Laser{p=new Vector2(g.player.x,0),angle=Mathf.PI/2});for(int i=0;i<35;i++)g.Tick(new Controls());Check(!g.missPending&&g.lives==3,"Laser warning has no collision");for(int i=0;i<40;i++)g.Tick(new Controls());Check(g.lives==2,"Active laser collision");
        g=new GameModel();g.invincible=0;g.godMode=true;g.lasers.Add(new Laser{p=new Vector2(g.player.x,0),angle=Mathf.PI/2});for(int i=0;i<120;i++)g.Tick(new Controls());Check(g.lives==3,"Invincibility prevents laser death");
        g=new GameModel();g.dream=127;g.Bullet(Vector2.one,0,0);g.AddDream(1);Check(g.dream==128&&g.bullets.Count==0,"Dream max clears bullets");
        g=new GameModel();g.power=126;g.Bullet(Vector2.one,0,0);g.Collect(0);Check(g.power==127&&g.bullets.Count==0,"Power max clears bullets");
        g=new GameModel();g.power=127;for(int i=0;i<41;i++)g.Collect(0);Check(g.powerValue==12800,"Exact 42-entry power scoring progression");long oldScore=g.score;g.Collect(2);Check(g.score-oldScore==25600,"Large max-value power item");
        g=new GameModel();g.Collect(6);Check(g.power==127&&g.score==1000,"Full power item scoring");
        g=new GameModel();g.Bullet(g.player+new Vector2(12,0),0,0,5);g.Tick(new Controls());Check(g.graze==0,"Giant bullets cannot be grazed");
        g=new GameModel();for(int i=0;i<3;i++)g.LoseLife();Check(g.power==0&&g.drops.FindAll(d=>d.kind==6).Count==5,"Last life leaves five full-power items");
        g=new GameModel();for(int i=0;i<100;i++)g.Collect(1);Check(g.totalPoints==100&&g.lives==4,"Point extend");
        g=new GameModel();g.dream=128;g.Collect(1);Check(g.score==100000,"Normal max dream point value");
        g=new GameModel();g.invincible=0;g.Bullet(g.player+new Vector2(12,0),0,0);for(int i=0;i<6;i++)g.Tick(new Controls());Check(g.graze==1,"Bullet grazes once");
        g=new GameModel();g.score=12345;for(int i=0;i<3;i++)g.LoseLife();Check(g.state==RunState.GameOver,"Game over");Check(g.Continue()&&g.score==0&&g.continues==1,"Continue resets score");
        g=new GameModel(0,4,6);for(int i=0;i<3;i++)g.LoseLife();Check(!g.Continue(),"Extra forbids continue");
        g=new GameModel(0,1,0,true);for(int i=0;i<3;i++)g.LoseLife();Check(!g.Continue(),"Practice forbids continue");
        for(int side=0;side<2;side++){
            g=new GameModel(0,1,3);g.StartBoss();var victim=g.foes[side];var survivor=g.foes[1-side];survivor.hp=1;g.shots.Add(new Shot{p=victim.p+new Vector2(0,3),v=Vector2.zero,damage=99999});g.Tick(new Controls());
            Check(g.phase==1&&g.foes.Count==1&&g.foes[0].side==1-side&&g.foes[0].hp==g.foes[0].maxHp,"Twin branch "+side+" survivor refills");
        }
        for(int stage=0;stage<7;stage++)for(int diff=0;diff<4;diff++){
            g=new GameModel(0,diff,stage,true);g.stageFrame=3200;g.StartBoss();
            for(int i=0;i<600;i++){g.invincible=100;g.Tick(new Controls());}
            Check(g.bullets.Count>0&&g.bullets.Count<=2200,"Stage "+stage+" difficulty "+diff+" emits bounded bullets");
            int safety=0;while(g.state==RunState.Playing&&safety++<12)g.EndPhase(false);
            Check(g.state==RunState.StageClear,"Stage "+stage+" difficulty "+diff+" clear transition");g.NextStage();Check(g.state==RunState.Complete,"Practice ends after stage");
        }
        for(int stage=0;stage<7;stage++){
            g=new GameModel(0,1,stage,true);g.StartBoss();int expected=new[]{3,4,3,5,6,9,9}[stage];Check(g.LastPhase+1==expected,"Documented phase count stage "+stage);
            for(int ph=0;ph<expected;ph++){
                g.phase=ph;g.CancelBullets(false);foreach(var e in g.foes){e.tick=0;e.age=0;}int emissions=0;
                for(int i=0;i<360;i++){g.godMode=true;g.Tick(new Controls());emissions=Mathf.Max(emissions,g.bullets.Count+g.lasers.Count);}
                Check(emissions>0,"Every documented attack emits: "+stage+" phase "+ph);
            }
        }
        g=new GameModel(0,1,2,true);g.StartBoss();g.foes[0].p=new Vector2(192,76);g.foes[0].tick=390;g.shots.Add(new Shot{p=new Vector2(192,147),v=new Vector2(0,-420)});g.Tick(new Controls());Check(g.shots.Count==0&&g.bullets.Count>0,"Alice barrier reflects player's shot");
        for(int c=0;c<4;c++){Check(StoryData.Ending(c,false,false).Length>0&&StoryData.Ending(c,false,true).Length>0&&StoryData.Ending(c,true,false).Length>0,"All three ending routes for character "+c);for(int st=0;st<7;st++)Check(!string.IsNullOrEmpty(StoryData.Encounter(c,st)),"Encounter route "+c+" stage "+st);}
        g=new GameModel();for(int stage=0;stage<6;stage++){Check(g.stage==stage,"Main sequence stage "+stage);g.StartBoss();int safety=0;while(g.state==RunState.Playing&&safety++<12)g.EndPhase(false);g.NextStage();}Check(g.state==RunState.Complete,"Six-stage main completion");
        for(int c=0;c<4;c++){
            g=new GameModel(c,1);int ticks=0;while(g.state!=RunState.Complete&&ticks++<150000){g.godMode=true;g.Tick(new Controls{fire=true});}
            Check(g.state==RunState.Complete,"Natural full run character "+c+" under 150000 ticks");
            g=new GameModel(c,4,6);ticks=0;while(g.state!=RunState.Complete&&ticks++<75000){g.godMode=true;g.Tick(new Controls{fire=true});}Check(g.state==RunState.Complete,"Natural Extra run character "+c);
        }
        File.WriteAllText("../QA/simulation-tests.txt",checks+" checks passed: movement, bounds, four shots, deathbomb, collision, Dream/Power cancellation, extend, graze, continues, twin branches, all stage/difficulty patterns, practice completion, main sequence and four natural full runs.\n");
    }
}
