using System.Text;
using CursorWorkflowHelper.Models;

namespace CursorWorkflowHelper.Services;

public sealed class PromptGenerator
{
    private readonly TemplateStore _templates;

    public PromptGenerator(TemplateStore templates)
    {
        _templates = templates;
    }

    public string Generate(TaskBrief brief, string target, string? templateId = null)
    {
        var template = ResolveTemplate(target, templateId);
        return Apply(template.Body, brief);
    }

    public string GenerateAllMarkdown(TaskBrief brief)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# 任务简报 — {brief.Title}");
        sb.AppendLine();
        sb.AppendLine($"- 更新时间：{brief.UpdatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- 偏好做法：{brief.PreferredApproach}");
        sb.AppendLine();
        sb.AppendLine("## 目标");
        sb.AppendLine(brief.Goal.Trim());
        sb.AppendLine();
        sb.AppendLine("## 文件 / 区域");
        sb.AppendLine(brief.FilesAreas.Trim());
        sb.AppendLine();
        sb.AppendLine("## 不做的范围");
        sb.AppendLine(brief.OutOfScope.Trim());
        sb.AppendLine();
        sb.AppendLine("## 验收标准");
        sb.AppendLine(brief.AcceptanceCriteria.Trim());
        sb.AppendLine();

        foreach (var target in TemplateTargets.All)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"## {TemplateTargets.DisplayName(target)}");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(Generate(brief, target).Trim());
            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string Apply(string body, TaskBrief brief)
    {
        return body
            .Replace(TemplatePlaceholders.Goal, NullToDash(brief.Goal))
            .Replace(TemplatePlaceholders.Files, NullToDash(brief.FilesAreas))
            .Replace(TemplatePlaceholders.OutOfScope, NullToDash(brief.OutOfScope))
            .Replace(TemplatePlaceholders.Acceptance, NullToDash(brief.AcceptanceCriteria))
            .Replace(TemplatePlaceholders.Approach, NullToDash(brief.PreferredApproach))
            .Trim() + Environment.NewLine;
    }

    private PromptTemplate ResolveTemplate(string target, string? templateId)
    {
        if (!string.IsNullOrEmpty(templateId))
        {
            var byId = _templates.Templates.FirstOrDefault(t => t.Id == templateId);
            if (byId != null)
                return byId;
        }

        var match = _templates.Templates.FirstOrDefault(t => t.Target == target);
        if (match != null)
            return match;

        // 兜底内置文案
        var defaults = TemplateStore.CreateDefaults();
        return defaults.First(t => t.Target == target);
    }

    private static string NullToDash(string? s) =>
        string.IsNullOrWhiteSpace(s) ? "（未填写）" : s.Trim();
}
