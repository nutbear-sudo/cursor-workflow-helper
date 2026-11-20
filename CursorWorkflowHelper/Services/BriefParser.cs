using System.Text;
using System.Text.RegularExpressions;
using CursorWorkflowHelper.Models;

namespace CursorWorkflowHelper.Services;

/// <summary>
/// 从 Cursor Ask / Markdown 回复中容错解析任务简报字段。
/// 支持 ## / ### 标题、以及「目标：」一类标签行；解析不全时填入已识别部分。
/// </summary>
public static class BriefParser
{
    public sealed class ParseResult
    {
        public TaskBrief Brief { get; init; } = new();
        public string? FreeQaPrompt { get; init; }
        public string? TraePrompt { get; init; }
        public string? CursorPrompt { get; init; }
        public int FilledFieldCount { get; init; }
        public bool IsEmpty => FilledFieldCount == 0;
        public bool IsPartial => FilledFieldCount is > 0 and < 5;
        public bool IsComplete => FilledFieldCount >= 5;
    }

    private static readonly (string Key, string[] Aliases)[] FieldDefs =
    [
        ("goal", ["目标", "一句话目标", "goal"]),
        ("files", ["触及文件", "文件 / 区域", "文件/区域", "文件", "关注区域", "相关文件", "可能涉及的文件", "files"]),
        ("out", ["不做范围", "不做的范围", "明确不做", "排除项", "禁止", "out of scope", "outofscope"]),
        ("accept", ["验收标准", "验收", "期望结果", "acceptance"]),
        ("approach", ["偏好做法", "协作方式", "做法", "approach"])
    ];

    private static readonly (string Key, string[] Aliases)[] PromptDefs =
    [
        ("free", ["免费问答", "免费问答模型", "免费模型", "free qa", "freeqa"]),
        ("trae", ["trae", "便宜 ide", "trae / 便宜 ide"]),
        ("cursor", ["cursor agent", "cursor", "窄范围改动"])
    ];

    public static ParseResult Parse(string? text)
    {
        var result = new ParseResult();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        var sections = SplitIntoSections(normalized);

        var goal = GetSection(sections, "goal");
        var files = GetSection(sections, "files");
        var outOfScope = GetSection(sections, "out");
        var acceptance = GetSection(sections, "accept");
        var approachRaw = GetSection(sections, "approach");

        // 若整段没有标题，尝试「标签：内容」行级解析
        if (sections.Count == 0)
            TryParseLabeledLines(normalized, out goal, out files, out outOfScope, out acceptance, out approachRaw);

        var brief = new TaskBrief
        {
            Goal = CleanField(goal),
            FilesAreas = CleanField(files),
            OutOfScope = CleanField(outOfScope),
            AcceptanceCriteria = CleanField(acceptance),
            PreferredApproach = MapApproach(CleanField(approachRaw))
        };

        var filled = 0;
        if (!string.IsNullOrWhiteSpace(brief.Goal)) filled++;
        if (!string.IsNullOrWhiteSpace(brief.FilesAreas)) filled++;
        if (!string.IsNullOrWhiteSpace(brief.OutOfScope)) filled++;
        if (!string.IsNullOrWhiteSpace(brief.AcceptanceCriteria)) filled++;
        if (!string.IsNullOrWhiteSpace(approachRaw)) filled++;

        return new ParseResult
        {
            Brief = brief,
            FreeQaPrompt = NullIfEmpty(GetSection(sections, "free")),
            TraePrompt = NullIfEmpty(GetSection(sections, "trae")),
            CursorPrompt = NullIfEmpty(GetSection(sections, "cursor")),
            FilledFieldCount = filled
        };
    }

    private static Dictionary<string, string> SplitIntoSections(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Split('\n');
        string? currentKey = null;
        var buf = new StringBuilder();

        void Flush()
        {
            if (currentKey == null)
                return;
            var body = buf.ToString().Trim();
            if (body.Length == 0)
                return;
            // 同名字段：后写覆盖（通常三类提示词在简报后面）
            map[currentKey] = body;
            buf.Clear();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var heading = TryMatchHeading(line);
            if (heading != null)
            {
                Flush();
                currentKey = heading;
                continue;
            }

            var labeled = TryMatchLabeledLine(line);
            if (labeled != null)
            {
                Flush();
                currentKey = labeled.Value.Key;
                if (!string.IsNullOrWhiteSpace(labeled.Value.Inline))
                    buf.AppendLine(labeled.Value.Inline);
                continue;
            }

            if (currentKey != null)
                buf.AppendLine(line);
        }

        Flush();
        return map;
    }

    private static string? TryMatchHeading(string line)
    {
        var m = Regex.Match(line.Trim(), @"^#{1,6}\s*(.+?)(?:\s*[:：].*)?$");
        if (!m.Success)
            return null;

        var title = StripDecor(m.Groups[1].Value);
        // 跳过容器标题
        if (IsContainerTitle(title))
            return null;

        return MatchKey(title, FieldDefs) ?? MatchKey(title, PromptDefs);
    }

    private static (string Key, string Inline)? TryMatchLabeledLine(string line)
    {
        var m = Regex.Match(line.Trim(), @"^\*?\*?([^*：:]{1,40})[*]*\s*[:：]\s*(.*)$");
        if (!m.Success)
            return null;

        var label = StripDecor(m.Groups[1].Value);
        if (IsContainerTitle(label))
            return null;

        var key = MatchKey(label, FieldDefs) ?? MatchKey(label, PromptDefs);
        if (key == null)
            return null;

        return (key, m.Groups[2].Value.Trim());
    }

    private static void TryParseLabeledLines(
        string text,
        out string? goal,
        out string? files,
        out string? outOfScope,
        out string? acceptance,
        out string? approach)
    {
        goal = files = outOfScope = acceptance = approach = null;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in text.Split('\n'))
        {
            var labeled = TryMatchLabeledLine(raw);
            if (labeled == null)
                continue;
            map[labeled.Value.Key] = labeled.Value.Inline;
        }

        map.TryGetValue("goal", out goal);
        map.TryGetValue("files", out files);
        map.TryGetValue("out", out outOfScope);
        map.TryGetValue("accept", out acceptance);
        map.TryGetValue("approach", out approach);
    }

    private static string? MatchKey(string title, (string Key, string[] Aliases)[] defs)
    {
        var t = title.Trim().ToLowerInvariant();
        // 先精确 / 前缀，再按别名长度做包含匹配，减少短词误伤
        string? best = null;
        var bestLen = -1;
        foreach (var (key, aliases) in defs)
        {
            foreach (var a in aliases)
            {
                var al = a.ToLowerInvariant();
                if (t == al || t.StartsWith(al + " ", StringComparison.Ordinal) || t.StartsWith(al + "·", StringComparison.Ordinal)
                    || t.StartsWith(al + "/", StringComparison.Ordinal) || t.StartsWith(al + "（", StringComparison.Ordinal)
                    || t.StartsWith(al + "(", StringComparison.Ordinal))
                    return key;

                if (t.Contains(al, StringComparison.Ordinal) && al.Length > bestLen)
                {
                    best = key;
                    bestLen = al.Length;
                }
            }
        }

        return best;
    }

    private static bool IsContainerTitle(string title)
    {
        var t = title.Trim().ToLowerInvariant();
        return t is "任务简报" or "三类提示词" or "三类提示词（可直接粘贴）" or "brief" or "prompts"
            || t.Contains("三类提示词", StringComparison.Ordinal)
            || t.Contains("可直接粘贴", StringComparison.Ordinal);
    }

    private static string StripDecor(string s)
    {
        return s.Trim()
            .Trim('*', '`', '_', '（', '）', '(', ')', '【', '】', '[', ']')
            .Trim();
    }

    private static string? GetSection(Dictionary<string, string> sections, string key) =>
        sections.TryGetValue(key, out var v) ? v : null;

    private static string CleanField(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        var lines = s.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => l.Trim() is not "---" and not "***" and not "```" and not "```text")
            .Select(l => Regex.Replace(l.TrimStart(), @"^[-*+]\s+", ""))
            .Select(l => Regex.Replace(l, @"^\d+[\.\)]\s+", ""))
            .ToList();

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private static string MapApproach(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ApproachOptions.UserSkeletonAiFills;

        var t = raw.Replace("\r\n", "\n").Split('\n')[0].Trim();
        foreach (var opt in ApproachOptions.All)
        {
            if (t.Contains(opt, StringComparison.OrdinalIgnoreCase) ||
                opt.Contains(t, StringComparison.OrdinalIgnoreCase))
                return opt;
        }

        if (t.Contains("骨架", StringComparison.Ordinal) || t.Contains("填细节", StringComparison.Ordinal))
            return ApproachOptions.UserSkeletonAiFills;
        if (t.Contains("只提议", StringComparison.Ordinal) || t.Contains("不直接改", StringComparison.Ordinal))
            return ApproachOptions.AiProposesOnly;
        if (t.Contains("小范围", StringComparison.Ordinal) || t.Contains("限定文件", StringComparison.Ordinal))
            return ApproachOptions.AiScopedEdit;
        if (t.Contains("解释", StringComparison.Ordinal) || t.Contains("调试", StringComparison.Ordinal))
            return ApproachOptions.ExplainOnly;

        // 无法映射时保留原文到偏好：ComboBox 无匹配则 Bind 时回退默认
        return t.Length <= 40 ? t : ApproachOptions.UserSkeletonAiFills;
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
