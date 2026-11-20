# Cursor 工作流助手（WinForms）

本机小工具，用来**少花 Cursor Token、照样做重活**：先在免费模型 / Trae 里想清楚，再带着**窄范围、可验收**的任务进 Cursor；你写骨架，AI 补细节。

- 目标框架：**.NET 8 Windows Forms**
- 无云端、无付费 API；数据存在本机 `%LocalAppData%\CursorWorkflowHelper\`

---

## 环境要求

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)  
  或 Visual Studio 2022（勾选「.NET 桌面开发」工作负载）

> 本仓库可在 Linux/macOS 上**查看源码**，但 WinForms 只能在 **Windows** 上编译运行。

---

## 用 Visual Studio 打开

1. 双击打开 `CursorWorkflowHelper.sln`
2. 确认启动项目为 `CursorWorkflowHelper`
3. 按 **F5** 运行（Debug）或 **Ctrl+F5**（不调试运行）

---

## 用 `dotnet` CLI 构建 / 运行

在仓库根目录（含 `.sln` 的目录）执行：

```bat
cd CursorWorkflowHelper
dotnet restore
dotnet build -c Release
dotnet run --project CursorWorkflowHelper -c Release
```

发布为单文件夹（可选）：

```bat
dotnet publish CursorWorkflowHelper -c Release -r win-x64 --self-contained false -o .\publish
```

然后运行 `publish\CursorWorkflowHelper.exe`。

---

## 主要功能

| 页签 | 作用 |
|------|------|
| **任务简报** | 填写目标、文件/区域、不做范围、验收、偏好做法；支持从剪贴板导入 Cursor Ask 输出 |
| **生成的提示词** | 一键拆成三类文案：免费问答 / Trae / Cursor Agent，可分别复制 |
| **进 Cursor 前检查** | 勾选清单；未完成复制 Cursor 提示词时会警告 |
| **模板库** | 编辑提示词模板（占位符 `{{GOAL}}` 等），本地 JSON |
| **历史记录** | 保存/加载最近简报 |

顶部按钮：**从剪贴板导入** · **一键拆成三类提示词** · **复制生成简报提示词** · **生成提示词** · **保存简报** · **新建简报** · **导出 Markdown**；可选深色模式、总在最前。

---

## 推荐用法（省 Token）

1. （推荐）点 **复制生成简报提示词** → 粘贴到 Cursor **Ask**（短对话）→ Cursor 读当前打开文件 / diff 后输出结构化「任务简报」。
2. 复制 Cursor 回复 → 助手点 **从剪贴板导入**（解析不全也可手改）→ **一键拆成三类提示词**。
3. 也可手工在「任务简报」把需求压成一句话 + 文件列表 + 验收，再生成。
4. 把「免费问答」贴到 ChatGPT/Claude 免费档等，只要计划与解释；Trae 做探索与骨架。
5. **你自己写主要代码**；过完「进 Cursor 前检查」。
6. 只把短的 Cursor Agent 提示词贴进 Cursor，限定文件改细节。

> 原则：**短 Ask 出简报，窄 Agent 改代码**。Ask 模板正文见 `CursorWorkflowHelper/Data/cursor-brief-ask-prompt.md`。

---

## 本地数据位置

| 文件 | 说明 |
|------|------|
| `%LocalAppData%\CursorWorkflowHelper\history.json` | 历史简报 |
| `%LocalAppData%\CursorWorkflowHelper\templates.json` | 模板库 |
| `%LocalAppData%\CursorWorkflowHelper\settings.json` | 深色 / 置顶 / 清单勾选 |

首次运行会从程序目录 `Data\default-templates.json` 初始化模板。

---

## 项目结构

```
CursorWorkflowHelper/
  CursorWorkflowHelper.sln
  README.md
  CursorWorkflowHelper/
    CursorWorkflowHelper.csproj
    Program.cs
    MainForm.cs
    Models/
    Services/
    Ui/
    Data/default-templates.json
    Data/cursor-brief-ask-prompt.md
    Services/BriefParser.cs
```

---

## 限制说明

- 仅 Windows + .NET 8 WinForms；可在 Linux/macOS **查看源码**，但 **无法在本机编译运行** WinForms（需 Windows +「.NET 桌面开发」）。
- 不在本助手内调用任何 LLM API。
- 不会自动打开 Cursor / Trae，提示词需手动粘贴。
- 检查清单是「提醒/警告」，不会强制禁止复制（避免误伤）。
- 剪贴板导入对 Markdown 标题 /「标签：」行做容错解析；识别不全时保留已填字段供手改。
