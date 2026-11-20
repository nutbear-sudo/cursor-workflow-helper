using System.Text.Json;
using CursorWorkflowHelper.Models;

namespace CursorWorkflowHelper.Services;

public sealed class TemplateStore
{
    private List<PromptTemplate> _templates = [];

    public IReadOnlyList<PromptTemplate> Templates => _templates;

    public void Load()
    {
        if (File.Exists(AppPaths.TemplatesFile))
        {
            _templates = JsonStore.LoadOrDefault(AppPaths.TemplatesFile, CreateDefaults);
        }
        else
        {
            _templates = LoadBundledOrDefaults();
            Save();
        }

        if (_templates.Count == 0)
        {
            _templates = CreateDefaults();
            Save();
        }
    }

    public void Save()
    {
        JsonStore.Save(AppPaths.TemplatesFile, _templates);
    }

    public void ReplaceAll(IEnumerable<PromptTemplate> templates)
    {
        _templates = templates.ToList();
        Save();
    }

    public void Upsert(PromptTemplate template)
    {
        var idx = _templates.FindIndex(t => t.Id == template.Id);
        if (idx >= 0)
            _templates[idx] = template;
        else
            _templates.Add(template);
        Save();
    }

    public void Delete(string id)
    {
        _templates.RemoveAll(t => t.Id == id && !t.IsBuiltIn);
        Save();
    }

    public void ResetToDefaults()
    {
        _templates = CreateDefaults();
        Save();
    }

    private static List<PromptTemplate> LoadBundledOrDefaults()
    {
        try
        {
            var bundled = Path.Combine(AppContext.BaseDirectory, "Data", "default-templates.json");
            if (File.Exists(bundled))
            {
                var json = File.ReadAllText(bundled);
                var list = JsonSerializer.Deserialize<List<PromptTemplate>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
                if (list is { Count: > 0 })
                    return list;
            }
        }
        catch
        {
            // fall through
        }

        return CreateDefaults();
    }

    public static List<PromptTemplate> CreateDefaults() =>
    [
        new PromptTemplate
        {
            Id = "builtin-free-plan",
            Name = "免费模型 · 规划与拆解",
            Category = "规划",
            Target = TemplateTargets.FreeQa,
            IsBuiltIn = true,
            Body =
                """
                你是资深工程师。请只做规划与解释，不要输出大段可直接粘贴的完整项目代码。

                ## 目标
                {{GOAL}}

                ## 可能涉及的文件 / 区域
                {{FILES}}

                ## 不做的范围
                {{OUT_OF_SCOPE}}

                ## 验收标准
                {{ACCEPTANCE}}

                ## 偏好做法
                {{APPROACH}}

                请输出：
                1. 问题理解（3 条以内）
                2. 推荐步骤（编号，每步可独立验证）
                3. 需要先读的文件 / 符号
                4. 风险与边界
                5. 给 Cursor 的「窄范围改动」一句话描述（≤40 字）
                """
        },
        new PromptTemplate
        {
            Id = "builtin-free-debug",
            Name = "免费模型 · 调试思路",
            Category = "调试",
            Target = TemplateTargets.FreeQa,
            IsBuiltIn = true,
            Body =
                """
                请帮我排查问题，只给诊断思路与检查清单，不要直接改仓库。

                ## 现象 / 目标
                {{GOAL}}

                ## 相关文件
                {{FILES}}

                ## 排除项
                {{OUT_OF_SCOPE}}

                ## 期望结果
                {{ACCEPTANCE}}

                请按：可能原因 → 如何验证 → 最小修复方向（仍由我自己写代码）。
                """
        },
        new PromptTemplate
        {
            Id = "builtin-trae-draft",
            Name = "Trae · 探索与草稿",
            Category = "草稿",
            Target = TemplateTargets.Trae,
            IsBuiltIn = true,
            Body =
                """
                在便宜 IDE / Trae 里做探索与草稿。可以读代码、给草稿片段，但最终合并由我决定。

                ## 目标
                {{GOAL}}

                ## 关注区域
                {{FILES}}

                ## 明确不做
                {{OUT_OF_SCOPE}}

                ## 验收
                {{ACCEPTANCE}}

                ## 协作方式
                {{APPROACH}}

                请：
                1. 指出关键符号与调用链
                2. 给出「骨架级」伪代码或短片段（不要整文件重写）
                3. 列出我进 Cursor 时应打开的文件清单（尽量 ≤5 个）
                """
        },
        new PromptTemplate
        {
            Id = "builtin-cursor-scoped",
            Name = "Cursor · 窄范围改动",
            Category = "改动",
            Target = TemplateTargets.CursorAgent,
            IsBuiltIn = true,
            Body =
                """
                只做窄范围改动。不要扩大范围、不要重构无关代码、不要新建未要求的文件。

                目标：{{GOAL}}

                仅允许触及：
                {{FILES}}

                禁止：
                {{OUT_OF_SCOPE}}

                验收：
                {{ACCEPTANCE}}

                做法：{{APPROACH}}

                约束：
                - 先确认上述文件已打开/相关；若需读其它文件，先列出再问我
                - 优先最小 diff；用户会自己写主要逻辑，你补细节
                - 改完用 3 条要点说明改了什么
                """
        },
        new PromptTemplate
        {
            Id = "builtin-cursor-fill",
            Name = "Cursor · 填细节",
            Category = "改动",
            Target = TemplateTargets.CursorAgent,
            IsBuiltIn = true,
            Body =
                """
                我已写好骨架。请只在下列文件中补全细节（命名、边界、错误处理），不要改架构。

                目标：{{GOAL}}
                文件：{{FILES}}
                不做：{{OUT_OF_SCOPE}}
                验收：{{ACCEPTANCE}}

                若发现骨架有明显问题，先指出并征求同意，再改。
                """
        }
    ];
}
