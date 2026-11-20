namespace CursorWorkflowHelper.Ui;

public static class Theme
{
    public static void Apply(Form form, bool dark)
    {
        var back = dark ? Color.FromArgb(32, 32, 36) : SystemColors.Control;
        var panel = dark ? Color.FromArgb(40, 40, 46) : SystemColors.Window;
        var fore = dark ? Color.FromArgb(230, 230, 235) : SystemColors.ControlText;
        var accent = dark ? Color.FromArgb(55, 55, 62) : SystemColors.ControlLight;

        form.BackColor = back;
        form.ForeColor = fore;
        ApplyRecursive(form, back, panel, fore, accent, dark);
    }

    private static void ApplyRecursive(Control root, Color back, Color panel, Color fore, Color accent, bool dark)
    {
        foreach (Control c in root.Controls)
        {
            switch (c)
            {
                case TextBox or RichTextBox or ListBox or ComboBox:
                    c.BackColor = panel;
                    c.ForeColor = fore;
                    break;
                case TabControl tc:
                    tc.BackColor = back;
                    tc.ForeColor = fore;
                    break;
                case TabPage tp:
                    tp.BackColor = back;
                    tp.ForeColor = fore;
                    break;
                case GroupBox:
                    c.ForeColor = fore;
                    break;
                case CheckBox or Label or RadioButton:
                    c.ForeColor = fore;
                    c.BackColor = Color.Transparent;
                    break;
                case Button btn:
                    btn.FlatStyle = FlatStyle.System;
                    btn.BackColor = accent;
                    btn.ForeColor = fore;
                    break;
                case Panel or SplitContainer or FlowLayoutPanel or TableLayoutPanel:
                    c.BackColor = back;
                    c.ForeColor = fore;
                    break;
            }

            if (c.HasChildren)
                ApplyRecursive(c, back, panel, fore, accent, dark);
        }
    }
}
