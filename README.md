# BiliBili Cache Video Merger (BiliBili 缓存视频合并工具)

一个简单易用的命令行工具，用于自动合并哔哩哔哩（Bilibili）手机客户端缓存的视频文件（`video.m4s`）和音频文件（`audio.m4s`），并以视频标题命名，输出为标准的 `.mp4` 格式。

---

## ✨ 功能特性

- **自动扫描**: 只需提供B站缓存的根目录 (`download` 文件夹)，程序即可自动扫描所有视频合集。
- **智能命名**: 自动读取缓存中的 `entry.json` 文件，以视频的 **分P标题** 或 **合集标题** 为最终文件名，方便整理。
- **容错处理**: 能够自动跳过缓存不完整或下载失败的视频，不会因单个文件错误而中断整个流程。
- **路径友好**: 合并后的视频会存放在 `download` 文件夹同级的 `MergedVideos` 文件夹内，不与源文件混淆。
- **跨平台**: 基于 .NET 构建，理论上可在 Windows, macOS, Linux 上运行（需有对应的 .NET 运行环境）。
- **依赖清晰**: 仅需 `ffmpeg.exe` 作为外部依赖，无需安装复杂的环境。

## 📦 运行环境

- **.NET 8.0 (或更高版本) 运行环境**
- **FFmpeg**: 需要将 `ffmpeg.exe` 放置于主程序 `BiliBiliVideoMerger.exe` 相同的目录下。

## 🚀 如何使用

1.  **下载程序**:
    - 前往 [Releases](https://github.com/WinfXK/BiliBiliVideoMerger/releases) 页面下载最新的程序压缩包。
    - 下载 FFmpeg：从 [FFmpeg 官网](https://www.gyan.dev/ffmpeg/builds/) 下载 `ffmpeg-release-essentials.zip`。

2.  **准备文件**:
    - 解压本工具的压缩包，得到 `BiliBiliVideoMerger.exe`。
    - 解压 FFmpeg 压缩包，将其 `bin` 目录下的 `ffmpeg.exe` 文件复制到 `BiliBiliVideoMerger.exe` 旁边。最终，你的文件夹看起来应该像这样：
      ```
      YourFolder/
      ├── BiliBiliVideoMerger.exe  (主程序)
      └── ffmpeg.exe               (依赖组件)
      ```

3.  **运行程序**:
    - 双击运行 `BiliBiliVideoMerger.exe`，会弹出一个命令行窗口。
    - 将你手机B站缓存的 `download` 文件夹（通常位于 `Android/data/tv.danmaku.bili/download`）用鼠标拖入到命令行窗口中。
    - 按下 `Enter` 键。

4.  **查看结果**:
    - 程序将开始合并视频，并显示处理进度。
    - 合并完成后，在你的 `download` 文件夹旁边会生成一个 `MergedVideos` 文件夹，所有合并好的 `.mp4` 文件都在里面。
### 也可以下载[Releases](https://github.com/WinfXK/BiliBiliVideoMerger/releases)的发布包，包含了`ffmpeg.exe`和`BiliBiliVideoMerger.exe`，解压即用

## 📂 目录结构示例

**处理前:**
```
.../SomePath/
└── download/
    ├── 113363765952784/
    │   └── c_26447839526/
    │       ├── 112/
    │       │   ├── audio.m4s
    │       │   └── video.m4s
    │       ├── entry.json
    │       └── ...
    └── ...
```

**处理后:**
```
.../SomePath/
├── download/
│   └── ... (源文件保持不变)
└── MergedVideos/
    ├── [视频标题1].mp4
    ├── [视频标题2].mp4
    └── ...
```

## 🛠️ 从源码构建

如果你想自行修改或编译本项目，请按以下步骤操作：

1.  克隆本仓库：
    ```bash
    git clone [https://github.com/WinfXK/BiliBiliVideoMerger/BiliBiliVideoMerger.git](https://github.com/WinfXK/BiliBiliVideoMerger.git)
    ```
2.  使用 Visual Studio 2022 打开 `.sln` 项目文件。
3.  确保已安装 .NET 6.0 SDK。
4.  点击“生成” -> “生成解决方案” (或按 `F6`)。
5.  生成的可执行文件位于 `bin/Debug/net6.0/` 或 `bin/Release/net6.0/` 目录下。

## 📄 开源许可

本项目基于 [Apache-2.0 license](./LICENSE) 开源。

## 🙏 致谢

- 本工具的核心功能依赖于伟大的 [FFmpeg](https://ffmpeg.org/) 项目。
