namespace CursorWorkflowHelper.Models;

/// <summary>可编辑的提示词模板。占位符见 TemplatePlaceholders。</summary>
public sealed class PromptTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "通用";
    /// <summary>目标场景：FreeQa / Trae / CursorAgent</summary>
    public string Target { get; set; } = TemplateTargets.FreeQa;
    public string Body { get; set; } = "";
    public bool IsBuiltIn { get; set; }
}

public static class TemplateTargets
{
    public const string FreeQa = "FreeQa";
    public const string Trae = "Trae";
    public const string CursorAgent = "CursorAgent";

    public static readonly string[] All = [FreeQa, Trae, CursorAgent];

    public static string DisplayName(string target) => target switch
    {
        FreeQa => "免费问答模型",
        Trae => "Trae / 便宜 IDE",
        CursorAgent => "Cursor Agent",
        _ => target
    };
}

public static class TemplatePlaceholders
{
    public const string Goal = "{{GOAL}}";
    public const string Files = "{{FILES}}";
    public const string OutOfScope = "{{OUT_OF_SCOPE}}";
    public const string Acceptance = "{{ACCEPTANCE}}";
    public const string Approach = "{{APPROACH}}";
}
