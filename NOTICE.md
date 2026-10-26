# 来源与许可

本项目是三个上游项目的独立非官方 Windows 移植仓库，不是原作者发布的官方 Windows 版本，也不是 GitHub Fork 网络中的单一上游分叉。main 是本仓库自己的主分支。上游仓库未被修改。

原项目（Ganzhe2028）：
- https://github.com/Ganzhe2028/SunPet
- https://github.com/Ganzhe2028/ShaolanPet
- https://github.com/Ganzhe2028/FengGe-Desktop-Pet

“好事”项目的初版来源还包括 https://github.com/yuhanfu513-ops/FengGe-Desktop-Pet 。本声明不意味着上述作者参与、审核、认可或授权了本 Windows 版本。

## GPT / Codex 声明

Windows 移植的代码编写、调试、测试、打包和文档整理，均由 GPT（通过 Codex）根据维护者 sanzhu8923 的需求完成。维护者提出需求并维护仓库。原头像、原声、原项目创意、语音整理和上游代码并非 GPT 创作，也不属于本仓库原创；相应贡献归原作者及贡献者。

## 代码许可与素材边界

使用各原仓库的 face.png、clips.json 与 Clips 原声文件。SunPet 的 AAC 语音解码为 PCM WAV 以兼容 Windows，并保留原始 M4A；运行时 clips.json 指向转换结果。没有变声、克隆或剪辑。原始代码 MIT 许可与素材声明完整保留在 licenses 的各子目录。头像、语音版权属于原权利人，不属于 MIT 代码许可；仅供个人非商业学习交流，详情遵循各原仓库 NOTICE.md。

Windows 移植源码位于 src：使用 WPF 透明窗口、Windows MediaPlayer、本机窗口消息互相接话。上游 Swift 源码未改动。

LICENSE 中的 Windows port 署名用于标识本仓库移植部分的贡献与维护归属，不表示维护者亲手编写代码，也不取代上游版权声明。根目录 LICENSE 保留 MIT 标准许可；原版完整许可分别保存在 upstream-assets 各项目目录以及分发包的 licenses 中。

头像和音频并未因为代码采用 MIT、项目公开、署名原作者或标注非商业用途而自动获得新的分发授权；原素材权利声明继续适用。
