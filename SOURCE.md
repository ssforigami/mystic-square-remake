# 源码说明

**仅供交流学习使用。** 请保留 NOTICE.md 和 MUSIC-CREDITS.md 中的说明及署名。

## 工程目录

- `MysticSquare/Assets/Scripts`：战斗模型、敌人弹幕、关卡流程、渲染与输入、剧情和运行时验证。
- `MysticSquare/Assets/Editor/BuildEntry.cs`：构建入口与 211 项模拟检查。
- `MysticSquare/Assets/Scenes`：Unity 主场景。
- `MysticSquare/Assets/Resources`：41 张自绘图像、23 首独立转谱重新合成的音乐及对应导入配置。
- `MysticSquare/Packages`、`MysticSquare/ProjectSettings`：依赖与项目设置。
- `Tools`：图像绘制、音乐合成、素材审计、激光画面检查及单文件 EXE 打包的源码。
- `QA/music-provenance.json`：音乐来源、转谱作者和素材校验值。

源码仓库不包含 Unity 缓存、个人编辑器设置、录屏、运行日志或原始 MIDI 转谱。

## 运行与构建

1. 使用 Unity **6000.6.0f1** 打开 `MysticSquare` 目录。
2. 打开 `Assets/Scenes/Main.unity`，点击 Play 运行。
3. Windows 下在仓库根目录运行 `powershell -ExecutionPolicy Bypass -File .\Build.ps1`。Unity 安装位置不同时，先调整脚本中的 `taskUnity`。
4. 构建完成后，运行 `Build/MysticSquare.exe` 或「开始游戏.cmd」。

项目已包含完整运行素材，正常编译不需要重新下载谱面或生成素材。

## 可选：重新生成素材

Python 脚本依赖 `Pillow`、`aggdraw`、`numpy`、`mido`。在仓库根目录执行：

```powershell
python -m pip install Pillow aggdraw numpy mido
python Tools/create_art.py
python Tools/create_portraits.py
python Tools/create_music.py
python Tools/audit_project.py
```

音乐生成脚本从 MUSIC-CREDITS.md 列出的公开来源获取独立转谱，并缓存到被 Git 忽略的 `Tools/MusicScores`；请遵守各来源使用条件。该步骤需要联网，外部来源可能变化。

## 可选：打包发行文件

先完成 Windows 构建，再执行 `python Tools/package_release.py`。该脚本使用 Windows 自带 .NET Framework C# 编译器构建单文件启动器，生成 EXE、ZIP 和校验文件到 `Release`。

`--smoke-test` 为实际游戏的自动界面检查；`--qa-input` 下 F12 保存窗口截图和输入状态。这两种验证方式不写入最高分或解锁记录。运行界面检查后，可使用 `Tools/verify_lasers.py` 检查生成的激光截图。

自动检查覆盖流程和实现行为，不等同于真人难度评测或与原作逐帧校准。
