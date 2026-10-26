# 来源与许可

本地 Windows 移植，不是原作者发布的官方 Windows 版本。

原项目（Ganzhe2028）：
- https://github.com/Ganzhe2028/SunPet
- https://github.com/Ganzhe2028/ShaolanPet
- https://github.com/Ganzhe2028/FengGe-Desktop-Pet

使用各原仓库的 face.png、clips.json 与 Clips 原声文件。SunPet 的 AAC 语音解码为 PCM WAV 以兼容 Windows，并保留原始 M4A；运行时 clips.json 指向转换结果。没有变声、克隆或剪辑。原始代码 MIT 许可与素材声明完整保留在 licenses 的各子目录。头像、语音版权属于原权利人，不属于 MIT 代码许可；仅供个人非商业学习交流，详情遵循各原仓库 NOTICE.md。

Windows 移植源码位于 src：使用 WPF 透明窗口、Windows MediaPlayer、本机窗口消息互相接话。上游 Swift 源码未改动。
