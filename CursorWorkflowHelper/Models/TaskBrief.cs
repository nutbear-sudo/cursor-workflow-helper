namespace CursorWorkflowHelper.Models;

/// <summary>一次任务的简报，用于生成窄范围提示词。</summary>
public sealed class TaskBrief
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>一句话目标</summary>
    public string Goal { get; set; } = "";

    /// <summary>要改动的文件 / 区域（每行一个）</summary>
    public string FilesAreas { get; set; } = "";

    /// <summary>明确不做的范围</summary>
    public string OutOfScope { get; set; } = "";

    /// <summary>验收标准</summary>
    public string AcceptanceCriteria { get; set; } = "";

    /// <summary>偏好做法：用户写骨架 / AI 填细节 / AI 只提议</summary>
    public string PreferredApproach { get; set; } = ApproachOptions.UserSkeletonAiFills;

    public string Title =>
        string.IsNullOrWhiteSpace(Goal)
            ? $"(未命名) {CreatedAt:MM-dd HH:mm}"
            : (Goal.Length <= 40 ? Goal : Goal[..40] + "…");

    public TaskBrief Clone() => new()
    {
        Id = Id,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        Goal = Goal,
        FilesAreas = FilesAreas,
        OutOfScope = OutOfScope,
        AcceptanceCriteria = AcceptanceCriteria,
        PreferredApproach = PreferredApproach
    };
}

public static class ApproachOptions
{
    public const string UserSkeletonAiFills = "用户写骨架，AI 填细节";
    public const string AiProposesOnly = "AI 只提议方案，不直接改代码";
    public const string AiScopedEdit = "AI 在限定文件内做小范围修改";
    public const string ExplainOnly = "只要解释 / 调试思路，不写完整实现";

    public static readonly string[] All =
    [
        UserSkeletonAiFills,
        AiProposesOnly,
        AiScopedEdit,
        ExplainOnly
    ];
}
