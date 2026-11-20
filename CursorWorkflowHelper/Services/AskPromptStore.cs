namespace CursorWorkflowHelper.Services;

/// <summary>加载「复制生成简报提示词」用的 Cursor Ask 模板正文。</summary>
public static class AskPromptStore
{
    private const string FileName = "cursor-brief-ask-prompt.md";

    public static string Load()
    {
        foreach (var path in CandidatePaths())
        {
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path).Trim() + Environment.NewLine;
            }
            catch
            {
                // 尝试下一个候选路径
            }
        }

        return FallbackPrompt;
    }

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Data", FileName);
        yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", FileName);

        // 开发时从项目目录旁路读取
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            yield return Path.Combine(dir, "Data", FileName);
            yield return Path.Combine(dir, "CursorWorkflowHelper", "Data", FileName);
            dir = Directory.GetParent(dir)?.FullName ?? "";
        }
    }

    private const string FallbackPrompt =
        """
        请先阅读当前项目上下文：已打开的文件、相关 git diff、最近改动与对话中的需求。用最短篇幅理解意图，不要展开成长对话。

        然后只输出下面两部分，全部用 Markdown，方便我整段复制到「Cursor 工作流助手」：

        ## 任务简报
        ### 目标
        （一句话，可执行）

        ### 触及文件
        （每行一个路径或区域，尽量 ≤5）

        ### 不做范围
        （明确排除项）

        ### 验收标准
        （可勾选的结果，条目列表）

        ### 偏好做法
        （从中选一，或写短句：用户写骨架，AI 填细节 / AI 只提议方案，不直接改代码 / AI 在限定文件内做小范围修改 / 只要解释 / 调试思路，不写完整实现）

        ---

        ## 三类提示词（可直接粘贴）

        ### 免费问答
        （规划/解释/调试用；不要吐整仓代码）

        ### Trae
        （便宜 IDE 探索+骨架草稿）

        ### Cursor Agent
        （窄范围、文件受限、短提示）

        约束：这是 Ask 短对话：输出一次即可；字段标题保持上述中文名，便于助手从剪贴板解析。
        """;
}
