using System.Text;
using CursorWorkflowHelper.Models;
using CursorWorkflowHelper.Services;
using CursorWorkflowHelper.Ui;

namespace CursorWorkflowHelper;

public sealed class MainForm : Form
{
    private readonly HistoryStore _history = new();
    private readonly TemplateStore _templates = new();
    private readonly SettingsStore _settings = new();
    private readonly PromptGenerator _generator;

    private TaskBrief _current = new();

    // 任务简报
    private TextBox _txtGoal = null!;
    private TextBox _txtFiles = null!;
    private TextBox _txtOutOfScope = null!;
    private TextBox _txtAcceptance = null!;
    private ComboBox _cmbApproach = null!;

    // 提示词
    private RichTextBox _rtbFree = null!;
    private RichTextBox _rtbTrae = null!;
    private RichTextBox _rtbCursor = null!;
    private ComboBox _cmbTplFree = null!;
    private ComboBox _cmbTplTrae = null!;
    private ComboBox _cmbTplCursor = null!;

    // 检查清单
    private readonly List<(string Key, string Text, CheckBox Box)> _checklist = [];
    private Label _lblChecklistStatus = null!;
    private Button _btnCopyCursorGuarded = null!;

    // 模板
    private ListBox _lstTemplates = null!;
    private TextBox _txtTplName = null!;
    private ComboBox _cmbTplTarget = null!;
    private TextBox _txtTplCategory = null!;
    private TextBox _txtTplBody = null!;

    // 历史
    private ListBox _lstHistory = null!;

    // 外观
    private CheckBox _chkDark = null!;
    private CheckBox _chkTopMost = null!;
    private TabControl _tabs = null!;
    private StatusStrip _status = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public MainForm()
    {
        _generator = new PromptGenerator(_templates);
        Text = "Cursor 工作流助手 — 省 Token";
        Width = 980;
        Height = 720;
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        LoadData();
        BuildUi();
        BindCurrentToForm();
        RefreshTemplateCombos();
        RefreshTemplateList();
        RefreshHistoryList();
        ApplyAppearance();
        UpdateChecklistStatus();
        SetStatus("就绪。先在「任务简报」填好，再生成提示词。");
    }

    private void LoadData()
    {
        AppPaths.EnsureRoot();
        _settings.Load();
        _templates.Load();
        _history.Load();
    }

    private void BuildUi()
    {
        _tabs = new TabControl { Dock = DockStyle.Fill };

        _tabs.TabPages.Add(BuildBriefTab());
        _tabs.TabPages.Add(BuildPromptsTab());
        _tabs.TabPages.Add(BuildChecklistTab());
        _tabs.TabPages.Add(BuildTemplatesTab());
        _tabs.TabPages.Add(BuildHistoryTab());

        var top = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 6, 8, 4) };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var btnGenerate = MakeButton("生成提示词", PrimaryClick);
        var btnSave = MakeButton("保存简报", (_, _) => SaveBrief());
        var btnNew = MakeButton("新建简报", (_, _) => NewBrief());
        var btnExport = MakeButton("导出 Markdown", (_, _) => ExportMarkdown());
        _chkDark = new CheckBox { Text = "深色", AutoSize = true, Margin = new Padding(16, 8, 8, 0) };
        _chkTopMost = new CheckBox { Text = "总在最前", AutoSize = true, Margin = new Padding(8, 8, 8, 0) };
        _chkDark.CheckedChanged += (_, _) =>
        {
            _settings.Current.DarkMode = _chkDark.Checked;
            _settings.Save();
            ApplyAppearance();
        };
        _chkTopMost.CheckedChanged += (_, _) =>
        {
            _settings.Current.AlwaysOnTop = _chkTopMost.Checked;
            TopMost = _chkTopMost.Checked;
            _settings.Save();
        };

        flow.Controls.AddRange([btnGenerate, btnSave, btnNew, btnExport, _chkDark, _chkTopMost]);
        top.Controls.Add(flow);

        _status = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("就绪") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _status.Items.Add(_statusLabel);

        // WinForms docks in reverse z-order: add Fill first, then Top/Bottom.
        Controls.Add(_tabs);
        Controls.Add(top);
        Controls.Add(_status);
    }

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 0, 8, 0),
            Height = 30
        };
        b.Click += onClick;
        return b;
    }

    private TabPage BuildBriefTab()
    {
        var page = new TabPage("任务简报");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        _txtGoal = new TextBox { Dock = DockStyle.Fill };
        _txtFiles = Multiline();
        _txtOutOfScope = Multiline();
        _txtAcceptance = Multiline();
        _cmbApproach = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbApproach.Items.AddRange(ApproachOptions.All);

        layout.Controls.Add(MakeLabel("目标（一句话）"), 0, 0);
        layout.Controls.Add(_txtGoal, 1, 0);
        layout.Controls.Add(MakeLabel("文件 / 区域"), 0, 1);
        layout.Controls.Add(_txtFiles, 1, 1);
        layout.Controls.Add(MakeLabel("不做的范围"), 0, 2);
        layout.Controls.Add(_txtOutOfScope, 1, 2);
        layout.Controls.Add(MakeLabel("验收标准"), 0, 3);
        layout.Controls.Add(_txtAcceptance, 1, 3);
        layout.Controls.Add(MakeLabel("偏好做法"), 0, 4);
        layout.Controls.Add(_cmbApproach, 1, 4);

        var tip = new Label
        {
            Dock = DockStyle.Fill,
            Text = "提示：文件 / 区域尽量 ≤5 个；验收写成可勾选的结果。填完后点顶部「生成提示词」。",
            ForeColor = Color.DimGray
        };
        layout.Controls.Add(new Label(), 0, 5);
        layout.Controls.Add(tip, 1, 5);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildPromptsTab()
    {
        var page = new TabPage("生成的提示词");
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(8)
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

        split.Controls.Add(BuildPromptColumn(
            "免费问答模型（规划 / 解释 / 调试）",
            out _cmbTplFree, out _rtbFree, TemplateTargets.FreeQa), 0, 0);
        split.Controls.Add(BuildPromptColumn(
            "Trae / 便宜 IDE（探索 + 草稿）",
            out _cmbTplTrae, out _rtbTrae, TemplateTargets.Trae), 1, 0);
        split.Controls.Add(BuildPromptColumn(
            "Cursor Agent（窄范围改动）",
            out _cmbTplCursor, out _rtbCursor, TemplateTargets.CursorAgent), 2, 0);

        page.Controls.Add(split);
        return page;
    }

    private Control BuildPromptColumn(string title, out ComboBox cmb, out RichTextBox rtb, string target)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var lbl = new Label { Text = title, Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) };
        cmb = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5F),
            DetectUrls = false
        };

        var capturedTarget = target;
        var capturedCmb = cmb;
        var capturedRtb = rtb;
        cmb.SelectedIndexChanged += (_, _) =>
        {
            if (capturedCmb.SelectedItem is PromptTemplateItem item)
                capturedRtb.Text = _generator.Generate(ReadBriefFromForm(), capturedTarget, item.Id);
        };

        var btnCopy = MakeButton("复制到剪贴板", (_, _) =>
        {
            CopyText(capturedRtb.Text, TemplateTargets.DisplayName(capturedTarget));
        });
        btnCopy.Dock = DockStyle.Fill;

        layout.Controls.Add(lbl, 0, 0);
        layout.Controls.Add(cmb, 0, 1);
        layout.Controls.Add(rtb, 0, 2);
        layout.Controls.Add(btnCopy, 0, 3);
        panel.Controls.Add(layout);
        return panel;
    }

    private TabPage BuildChecklistTab()
    {
        var page = new TabPage("进 Cursor 前检查");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        root.Controls.Add(new Label
        {
            Text = "全部勾选后，才建议复制 Cursor 提示词并打开 Cursor。未完成时会警告。",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold)
        }, 0, 0);

        var listPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        string[] items =
        [
            "goal_clear|目标已缩成一句话，不含「顺便重构」",
            "files_limited|触及文件 / 区域已列出，且尽量 ≤5 个",
            "files_open|相关文件已在编辑器中打开（或路径已写清）",
            "acceptance_clear|验收标准可勾选、可验证",
            "out_of_scope|不做的范围已写明",
            "skeleton_ready|主要逻辑骨架已由我写好，或明确只要 AI 提议",
            "history_not_needed|不需要把整段聊天历史贴进 Cursor",
            "prompt_short|Cursor 提示词已尽量短、文件受限"
        ];

        foreach (var raw in items)
        {
            var parts = raw.Split('|', 2);
            var key = parts[0];
            var text = parts[1];
            var box = new CheckBox
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(4, 8, 4, 8)
            };
            if (_settings.Current.ChecklistState.TryGetValue(key, out var on))
                box.Checked = on;
            box.CheckedChanged += (_, _) =>
            {
                _settings.Current.ChecklistState[key] = box.Checked;
                _settings.Save();
                UpdateChecklistStatus();
            };
            _checklist.Add((key, text, box));
            listPanel.Controls.Add(box);
        }

        _lblChecklistStatus = new Label { Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) };

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _btnCopyCursorGuarded = MakeButton("检查通过 → 复制 Cursor 提示词", (_, _) => CopyCursorGuarded());
        var btnReset = MakeButton("清空勾选", (_, _) =>
        {
            foreach (var item in _checklist)
                item.Box.Checked = false;
        });
        actions.Controls.Add(_btnCopyCursorGuarded);
        actions.Controls.Add(btnReset);

        root.Controls.Add(listPanel, 0, 1);
        root.Controls.Add(_lblChecklistStatus, 0, 2);
        root.Controls.Add(actions, 0, 3);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildTemplatesTab()
    {
        var page = new TabPage("模板库");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 280,
            Padding = new Padding(8)
        };

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        _lstTemplates = new ListBox { Dock = DockStyle.Fill, DisplayMember = "Display" };
        _lstTemplates.SelectedIndexChanged += (_, _) => LoadSelectedTemplateToEditor();
        var leftBtns = new FlowLayoutPanel { Dock = DockStyle.Fill };
        leftBtns.Controls.Add(MakeButton("新建", (_, _) => NewTemplate()));
        leftBtns.Controls.Add(MakeButton("删除", (_, _) => DeleteSelectedTemplate()));
        leftBtns.Controls.Add(MakeButton("恢复默认", (_, _) =>
        {
            if (MessageBox.Show(this, "恢复内置默认模板？（会覆盖当前模板文件）", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _templates.ResetToDefaults();
                RefreshTemplateList();
                RefreshTemplateCombos();
                SetStatus("已恢复默认模板。");
            }
        }));
        left.Controls.Add(_lstTemplates, 0, 0);
        left.Controls.Add(leftBtns, 0, 1);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(8, 0, 0, 0)
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        _txtTplName = new TextBox { Dock = DockStyle.Fill };
        _cmbTplTarget = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbTplTarget.Items.AddRange(new object[]
        {
            new TargetItem(TemplateTargets.FreeQa),
            new TargetItem(TemplateTargets.Trae),
            new TargetItem(TemplateTargets.CursorAgent)
        });
        _txtTplCategory = new TextBox { Dock = DockStyle.Fill };
        _txtTplBody = Multiline();
        _txtTplBody.Font = new Font("Consolas", 9.5F);

        right.Controls.Add(MakeLabel("名称"), 0, 0);
        right.Controls.Add(_txtTplName, 1, 0);
        right.Controls.Add(MakeLabel("场景"), 0, 1);
        right.Controls.Add(_cmbTplTarget, 1, 1);
        right.Controls.Add(MakeLabel("分类"), 0, 2);
        right.Controls.Add(_txtTplCategory, 1, 2);
        right.Controls.Add(MakeLabel("正文"), 0, 3);
        right.Controls.Add(_txtTplBody, 1, 3);

        var saveRow = new FlowLayoutPanel { Dock = DockStyle.Fill };
        saveRow.Controls.Add(MakeButton("保存模板", (_, _) => SaveTemplateFromEditor()));
        saveRow.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "占位符：{{GOAL}} {{FILES}} {{OUT_OF_SCOPE}} {{ACCEPTANCE}} {{APPROACH}}",
            Margin = new Padding(12, 8, 0, 0)
        });
        right.Controls.Add(new Label(), 0, 4);
        right.Controls.Add(saveRow, 1, 4);

        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(right);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildHistoryTab()
    {
        var page = new TabPage("历史记录");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        _lstHistory = new ListBox { Dock = DockStyle.Fill, DisplayMember = "Display" };
        _lstHistory.DoubleClick += (_, _) => LoadSelectedHistory();

        var btns = new FlowLayoutPanel { Dock = DockStyle.Fill };
        btns.Controls.Add(MakeButton("加载选中", (_, _) => LoadSelectedHistory()));
        btns.Controls.Add(MakeButton("删除选中", (_, _) => DeleteSelectedHistory()));
        btns.Controls.Add(MakeButton("清空历史", (_, _) =>
        {
            if (MessageBox.Show(this, "确定清空全部历史？", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _history.Clear();
                RefreshHistoryList();
                SetStatus("历史已清空。");
            }
        }));

        layout.Controls.Add(_lstHistory, 0, 0);
        layout.Controls.Add(btns, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static TextBox Multiline() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        AcceptsReturn = true,
        AcceptsTab = true
    };

    private void PrimaryClick(object? sender, EventArgs e)
    {
        GeneratePrompts();
        _tabs.SelectedIndex = 1;
    }

    private void GeneratePrompts()
    {
        ReadBriefFromFormIntoCurrent();
        if (string.IsNullOrWhiteSpace(_current.Goal))
        {
            MessageBox.Show(this, "请先填写「目标（一句话）」。", "缺少内容",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _tabs.SelectedIndex = 0;
            return;
        }

        _rtbFree.Text = _generator.Generate(_current, TemplateTargets.FreeQa, SelectedTemplateId(_cmbTplFree));
        _rtbTrae.Text = _generator.Generate(_current, TemplateTargets.Trae, SelectedTemplateId(_cmbTplTrae));
        _rtbCursor.Text = _generator.Generate(_current, TemplateTargets.CursorAgent, SelectedTemplateId(_cmbTplCursor));
        SetStatus("已生成三类提示词。可分别复制；进 Cursor 前请过检查清单。");
    }

    private static string? SelectedTemplateId(ComboBox cmb) =>
        cmb.SelectedItem is PromptTemplateItem item ? item.Id : null;

    private TaskBrief ReadBriefFromForm()
    {
        return new TaskBrief
        {
            Id = _current.Id,
            CreatedAt = _current.CreatedAt,
            UpdatedAt = DateTime.Now,
            Goal = _txtGoal.Text,
            FilesAreas = _txtFiles.Text,
            OutOfScope = _txtOutOfScope.Text,
            AcceptanceCriteria = _txtAcceptance.Text,
            PreferredApproach = _cmbApproach.SelectedItem as string ?? ApproachOptions.UserSkeletonAiFills
        };
    }

    private void ReadBriefFromFormIntoCurrent()
    {
        _current = ReadBriefFromForm();
    }

    private void BindCurrentToForm()
    {
        _txtGoal.Text = _current.Goal;
        _txtFiles.Text = _current.FilesAreas;
        _txtOutOfScope.Text = _current.OutOfScope;
        _txtAcceptance.Text = _current.AcceptanceCriteria;
        var idx = Array.IndexOf(ApproachOptions.All, _current.PreferredApproach);
        _cmbApproach.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void NewBrief()
    {
        ReadBriefFromFormIntoCurrent();
        _current = new TaskBrief();
        BindCurrentToForm();
        _rtbFree.Clear();
        _rtbTrae.Clear();
        _rtbCursor.Clear();
        _tabs.SelectedIndex = 0;
        SetStatus("已新建空白简报。");
    }

    private void SaveBrief()
    {
        ReadBriefFromFormIntoCurrent();
        if (string.IsNullOrWhiteSpace(_current.Goal))
        {
            MessageBox.Show(this, "目标为空，无法保存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _history.Upsert(_current);
        RefreshHistoryList();
        SetStatus($"已保存简报：{_current.Title}");
    }

    private void ExportMarkdown()
    {
        ReadBriefFromFormIntoCurrent();
        using var dlg = new SaveFileDialog
        {
            Filter = "Markdown (*.md)|*.md|所有文件 (*.*)|*.*",
            FileName = $"brief-{DateTime.Now:yyyyMMdd-HHmm}.md",
            Title = "导出任务简报"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        // 确保提示词最新
        if (string.IsNullOrWhiteSpace(_rtbCursor.Text))
            GeneratePrompts();

        File.WriteAllText(dlg.FileName, _generator.GenerateAllMarkdown(_current), Encoding.UTF8);
        SetStatus($"已导出：{dlg.FileName}");
    }

    private void CopyText(string text, string label)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "内容为空，请先生成提示词。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Clipboard.SetText(text.Trim());
            SetStatus($"已复制「{label}」提示词到剪贴板。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "复制失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateChecklistStatus()
    {
        var done = _checklist.Count(x => x.Box.Checked);
        var total = _checklist.Count;
        var ok = done == total;
        _lblChecklistStatus.Text = ok
            ? $"检查通过（{done}/{total}）。可以复制 Cursor 提示词了。"
            : $"尚未完成（{done}/{total}）。建议补齐后再进 Cursor，以免浪费 Token。";
        _lblChecklistStatus.ForeColor = ok ? Color.SeaGreen : Color.DarkOrange;
        _btnCopyCursorGuarded.Enabled = true; // 仍可点，但会警告
    }

    private void CopyCursorGuarded()
    {
        var missing = _checklist.Where(x => !x.Box.Checked).Select(x => "· " + x.Text).ToList();
        if (missing.Count > 0)
        {
            var msg = "以下项尚未勾选：\n\n" + string.Join("\n", missing) +
                      "\n\n仍要复制 Cursor 提示词吗？（不建议）";
            var r = MessageBox.Show(this, msg, "检查未完成",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes)
            {
                SetStatus("已取消复制。请先完成检查清单。");
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(_rtbCursor.Text))
            GeneratePrompts();

        CopyText(_rtbCursor.Text, "Cursor Agent");
    }

    private void RefreshTemplateCombos()
    {
        FillTemplateCombo(_cmbTplFree, TemplateTargets.FreeQa);
        FillTemplateCombo(_cmbTplTrae, TemplateTargets.Trae);
        FillTemplateCombo(_cmbTplCursor, TemplateTargets.CursorAgent);
    }

    private void FillTemplateCombo(ComboBox cmb, string target)
    {
        var selected = cmb.SelectedItem as PromptTemplateItem;
        cmb.Items.Clear();
        foreach (var t in _templates.Templates.Where(x => x.Target == target))
            cmb.Items.Add(new PromptTemplateItem(t));
        if (cmb.Items.Count == 0)
            return;

        if (selected != null)
        {
            for (var i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is PromptTemplateItem p && p.Id == selected.Id)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
        }

        cmb.SelectedIndex = 0;
    }

    private void RefreshTemplateList()
    {
        _lstTemplates.Items.Clear();
        foreach (var t in _templates.Templates)
            _lstTemplates.Items.Add(new PromptTemplateItem(t));
        if (_lstTemplates.Items.Count > 0)
            _lstTemplates.SelectedIndex = 0;
    }

    private void LoadSelectedTemplateToEditor()
    {
        if (_lstTemplates.SelectedItem is not PromptTemplateItem item)
            return;
        var t = _templates.Templates.FirstOrDefault(x => x.Id == item.Id);
        if (t == null)
            return;

        _txtTplName.Text = t.Name;
        _txtTplCategory.Text = t.Category;
        _txtTplBody.Text = t.Body;
        for (var i = 0; i < _cmbTplTarget.Items.Count; i++)
        {
            if (_cmbTplTarget.Items[i] is TargetItem ti && ti.Value == t.Target)
            {
                _cmbTplTarget.SelectedIndex = i;
                break;
            }
        }
    }

    private void NewTemplate()
    {
        var t = new PromptTemplate
        {
            Name = "自定义模板",
            Category = "自定义",
            Target = TemplateTargets.CursorAgent,
            Body = "目标：{{GOAL}}\n文件：{{FILES}}\n不做：{{OUT_OF_SCOPE}}\n验收：{{ACCEPTANCE}}\n做法：{{APPROACH}}\n",
            IsBuiltIn = false
        };
        _templates.Upsert(t);
        RefreshTemplateList();
        RefreshTemplateCombos();
        for (var i = 0; i < _lstTemplates.Items.Count; i++)
        {
            if (_lstTemplates.Items[i] is PromptTemplateItem p && p.Id == t.Id)
            {
                _lstTemplates.SelectedIndex = i;
                break;
            }
        }
        SetStatus("已新建模板。");
    }

    private void SaveTemplateFromEditor()
    {
        if (_lstTemplates.SelectedItem is not PromptTemplateItem item)
        {
            MessageBox.Show(this, "请先选择左侧模板。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var existing = _templates.Templates.FirstOrDefault(x => x.Id == item.Id);
        if (existing == null)
            return;

        existing.Name = _txtTplName.Text.Trim();
        existing.Category = string.IsNullOrWhiteSpace(_txtTplCategory.Text) ? "通用" : _txtTplCategory.Text.Trim();
        existing.Body = _txtTplBody.Text;
        if (_cmbTplTarget.SelectedItem is TargetItem ti)
            existing.Target = ti.Value;

        _templates.Upsert(existing);
        RefreshTemplateList();
        RefreshTemplateCombos();
        SetStatus($"已保存模板：{existing.Name}");
    }

    private void DeleteSelectedTemplate()
    {
        if (_lstTemplates.SelectedItem is not PromptTemplateItem item)
            return;
        var t = _templates.Templates.FirstOrDefault(x => x.Id == item.Id);
        if (t == null)
            return;
        if (t.IsBuiltIn)
        {
            MessageBox.Show(this, "内置模板不能删除，可编辑后保存，或「恢复默认」。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this, $"删除模板「{t.Name}」？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _templates.Delete(t.Id);
        RefreshTemplateList();
        RefreshTemplateCombos();
        SetStatus("模板已删除。");
    }

    private void RefreshHistoryList()
    {
        _lstHistory.Items.Clear();
        foreach (var h in _history.Items)
            _lstHistory.Items.Add(new HistoryItem(h));
    }

    private void LoadSelectedHistory()
    {
        if (_lstHistory.SelectedItem is not HistoryItem item)
            return;
        var h = _history.Items.FirstOrDefault(x => x.Id == item.Id);
        if (h == null)
            return;
        _current = h.Clone();
        BindCurrentToForm();
        GeneratePrompts();
        _tabs.SelectedIndex = 0;
        SetStatus($"已加载历史：{_current.Title}");
    }

    private void DeleteSelectedHistory()
    {
        if (_lstHistory.SelectedItem is not HistoryItem item)
            return;
        _history.Delete(item.Id);
        RefreshHistoryList();
        SetStatus("已删除一条历史。");
    }

    private void ApplyAppearance()
    {
        _chkDark.Checked = _settings.Current.DarkMode;
        _chkTopMost.Checked = _settings.Current.AlwaysOnTop;
        TopMost = _settings.Current.AlwaysOnTop;
        Theme.Apply(this, _settings.Current.DarkMode);
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private sealed class PromptTemplateItem(PromptTemplate t)
    {
        public string Id { get; } = t.Id;
        public string Display { get; } = $"[{TemplateTargets.DisplayName(t.Target)}] {t.Name}";
        public override string ToString() => Display;
    }

    private sealed class HistoryItem(TaskBrief b)
    {
        public string Id { get; } = b.Id;
        public string Display { get; } = $"{b.UpdatedAt:MM-dd HH:mm}  {b.Title}";
        public override string ToString() => Display;
    }

    private sealed class TargetItem(string value)
    {
        public string Value { get; } = value;
        public override string ToString() => TemplateTargets.DisplayName(Value);
    }
}
