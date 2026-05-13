using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class ProcessStep
{
    public required string Title { get; init; }
    public required string Instruction { get; init; }
    public required Control Content { get; init; }
    public Func<bool>? Validate { get; init; }
    public Action? OnEnter { get; init; }
}

internal abstract class WorkflowForm : Form
{
    private readonly List<ProcessStep> _steps = new();
    private readonly FlowLayoutPanel _stepRail = new();
    private readonly Panel _contentHost = new();
    private readonly Label _workflowTitle = new();
    private readonly Label _stepTitle = new();
    private readonly Label _instruction = new();
    private readonly Button _back;
    private readonly Button _next;
    private readonly Button _cancel;
    private int _index;

    protected string Actor { get; }
    protected string WorkflowName { get; }
    public bool Saved { get; private set; }

    protected WorkflowForm(string workflowName, string actor)
    {
        WorkflowName = workflowName;
        Actor = actor;
        Text = "CJIT ServiceCore - " + workflowName;
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1180, 760);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.NormalFont;
        KeyPreview = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(14) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Theme.Panel, Padding = new Padding(20, 12, 20, 10) };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _workflowTitle.Text = WorkflowName;
        _workflowTitle.Dock = DockStyle.Fill;
        _workflowTitle.Font = Theme.HeaderFont;
        _workflowTitle.ForeColor = Theme.Text;
        _workflowTitle.TextAlign = ContentAlignment.MiddleLeft;
        _instruction.Dock = DockStyle.Fill;
        _instruction.Font = Theme.NormalFont;
        _instruction.ForeColor = Theme.Muted;
        _instruction.TextAlign = ContentAlignment.TopLeft;
        _instruction.AutoEllipsis = true;
        header.Controls.Add(_workflowTitle, 0, 0);
        header.Controls.Add(_instruction, 0, 1);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _stepRail.Dock = DockStyle.Fill;
        _stepRail.FlowDirection = FlowDirection.TopDown;
        _stepRail.WrapContents = false;
        _stepRail.AutoScroll = true;
        _stepRail.BackColor = Theme.Panel;
        _stepRail.Padding = new Padding(10);
        _stepRail.BorderStyle = BorderStyle.FixedSingle;

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10, 0, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _stepTitle.Dock = DockStyle.Fill;
        _stepTitle.Font = Theme.HeaderFont;
        _stepTitle.ForeColor = Theme.Accent2;
        _stepTitle.TextAlign = ContentAlignment.MiddleLeft;
        _contentHost.Dock = DockStyle.Fill;
        _contentHost.AutoScroll = true;
        _contentHost.BackColor = Theme.Background;
        _contentHost.Padding = new Padding(0);
        right.Controls.Add(_stepTitle, 0, 0);
        right.Controls.Add(_contentHost, 0, 1);

        body.Controls.Add(_stepRail, 0, 0);
        body.Controls.Add(right, 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.Panel,
            Padding = new Padding(12),
            BorderStyle = BorderStyle.FixedSingle,
            WrapContents = false
        };
        _cancel = Theme.Button("Esc  Cancel", (_, _) => CancelWorkflow(), Theme.Panel3);
        _next = Theme.Button("Next  Enter", (_, _) => Next(), Theme.Accent);
        _back = Theme.Button("Back", (_, _) => Back(), Theme.Panel3);
        footer.Controls.Add(_cancel);
        footer.Controls.Add(_next);
        footer.Controls.Add(_back);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelWorkflow();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && !IsMultilineInput(ActiveControl))
            {
                Next();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back && e.Control)
            {
                Back();
                e.Handled = true;
            }
        };
    }

    protected void AddStep(ProcessStep step) => _steps.Add(step);

    protected void StartWorkflow()
    {
        if (_steps.Count == 0) throw new InvalidOperationException("Workflow has no steps.");
        ShowStep(0);
    }

    private static bool IsMultilineInput(Control? control)
    {
        return control is RichTextBox || control is TextBox { Multiline: true };
    }

    private void DrawStepRail()
    {
        _stepRail.SuspendLayout();
        _stepRail.Controls.Clear();
        for (var i = 0; i < _steps.Count; i++)
        {
            var label = new Label
            {
                Text = $"{i + 1}. {_steps[i].Title}",
                Width = 270,
                Height = 58,
                ForeColor = i == _index ? Color.White : Theme.Text,
                BackColor = i == _index ? Theme.Accent : i < _index ? Color.FromArgb(0, 103, 77) : Theme.Panel2,
                Font = Theme.ButtonFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 8)
            };
            _stepRail.Controls.Add(label);
        }
        _stepRail.ResumeLayout();
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= _steps.Count) return;
        _index = index;
        var step = _steps[_index];
        _workflowTitle.Text = WorkflowName + $"   •   Step {_index + 1} of {_steps.Count}";
        _stepTitle.Text = step.Title;
        _instruction.Text = step.Instruction;
        _contentHost.SuspendLayout();
        _contentHost.Controls.Clear();
        step.Content.Dock = DockStyle.Top;
        step.Content.MinimumSize = new Size(Math.Max(800, _contentHost.ClientSize.Width - 30), 0);
        _contentHost.Controls.Add(step.Content);
        _contentHost.ResumeLayout();
        step.OnEnter?.Invoke();
        _back.Enabled = _index > 0;
        _next.Text = _index == _steps.Count - 1 ? "Finish  Enter" : "Next  Enter";
        DrawStepRail();
    }

    private void Back()
    {
        if (_index > 0) ShowStep(_index - 1);
    }

    private void Next()
    {
        var step = _steps[_index];
        if (step.Validate != null && !step.Validate()) return;
        if (_index == _steps.Count - 1)
        {
            try
            {
                if (FinishWorkflow())
                {
                    Saved = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                CrashReporter.Show(ex);
            }
            return;
        }
        ShowStep(_index + 1);
    }

    private void CancelWorkflow()
    {
        if (Theme.Confirm(this, "Cancel this process and return to ServiceCore?")) Close();
    }

    protected abstract bool FinishWorkflow();
}
