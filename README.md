# Windows 桌宠三件套

Ganzhe2028 的太对了、薄肌、好事三个 macOS 桌宠的非官方 Windows 移植。保留原头像、原声与互相接话玩法。

## 下载与运行

在右侧 Releases 中下载 Windows 压缩包，完整解压后双击「一起启动.exe」。不要只取出单独的 exe，它需要同目录的 assets 文件夹。

也可以单独打开 SunPet.exe（太对了）、ShaolanPet.exe（薄肌）、GoodThingPet.exe（好事）。点击说话、按住拖动、滚轮缩放、右键设置；任务栏右侧托盘可以找回桌宠。任意桌宠右键 → 全部退出。

三个一起开：薄肌 → 太对了 → 好事 → 太对了，四句后结束；单独点击好事，只会由太对了再接一句。

Windows 10/11 与 .NET Framework 4.8。运行无需 Python、Node、管理员权限或联网。Mac 机身传感器功能未移植。

## 源码

- `src/Core.cs`：随机轮播、句子范围、防环路由、位置边界。
- `src/App.cs`：透明 WPF 窗口、Windows 媒体播放、原头像、托盘、设置与本机消息。
- `build.ps1`：调用系统自带 C# 编译器，复制原素材与许可并准备 PCM 音频。
- `tools/prepare_audio.py`：构建时将 28 个 AAC 片段转换为兼容 Windows 的 PCM WAV；保留原始 M4A。
- `tests`：核心逻辑、真实音频解码、实际进程接话、成品完整性检查。

## 重新构建

仓库已包含 `upstream-assets/` 原始素材与对应许可。构建需要 Windows 的 .NET Framework 4.8、PowerShell 和 Python。

```powershell
python -m pip install -r requirements-build.txt
.\build.ps1
.\tests\run-tests.ps1
.\tests\package-check.ps1
```

成品输出至 `release/Windows桌宠三件套/`。运行成品不需要这些构建工具。17 项核心检查、76 段 Windows 音频解码和实际四句接话链已在本机验证。

## 原项目与素材

- [SunPet · 太对了](https://github.com/Ganzhe2028/SunPet)
- [ShaolanPet · 薄肌](https://github.com/Ganzhe2028/ShaolanPet)
- [FengGe-Desktop-Pet · 好事](https://github.com/Ganzhe2028/FengGe-Desktop-Pet)

详见 `NOTICE.md`、`upstream-assets/` 和成品 `licenses/` 中的原始许可与素材权利声明。代码 MIT；头像与语音的版权归原权利人，素材不在 MIT 范围内，仅供个人非商业学习交流。Mac 的机身摇晃功能没有移植。
