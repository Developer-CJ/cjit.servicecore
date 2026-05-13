using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(7, 12, 20);
    public static readonly Color Panel = Color.FromArgb(15, 25, 39);
    public static readonly Color Panel2 = Color.FromArgb(22, 35, 53);
    public static readonly Color Panel3 = Color.FromArgb(32, 49, 72);
    public static readonly Color Border = Color.FromArgb(63, 88, 121);
    public static readonly Color Accent = Color.FromArgb(0, 158, 222);
    public static readonly Color Accent2 = Color.FromArgb(0, 211, 156);
    public static readonly Color Warning = Color.FromArgb(255, 193, 77);
    public static readonly Color Danger = Color.FromArgb(235, 84, 84);
    public static readonly Color Text = Color.FromArgb(239, 247, 255);
    public static readonly Color Muted = Color.FromArgb(164, 184, 205);
    public static readonly Color Input = Color.FromArgb(5, 10, 18);

    public static readonly Font TitleFont = new("Segoe UI Semibold", 24F, FontStyle.Bold);
    public static readonly Font HeaderFont = new("Segoe UI Semibold", 15F, FontStyle.Bold);
    public static readonly Font NormalFont = new("Segoe UI", 10.5F, FontStyle.Regular);
    public static readonly Font SmallFont = new("Segoe UI", 9F, FontStyle.Regular);
    public static readonly Font ButtonFont = new("Segoe UI Semibold", 11F, FontStyle.Bold);

    public static Label Label(string text, Font? font = null, Color? color = null, ContentAlignment align = ContentAlignment.MiddleLeft)
    {
        return new Label
        {
            Text = text,
            ForeColor = color ?? Text,
            Font = font ?? NormalFont,
            AutoSize = false,
            Height = 30,
            Dock = DockStyle.Fill,
            TextAlign = align,
            Margin = new Padding(6)
        };
    }

    public static Label AutoLabel(string text, Font? font = null, Color? color = null)
    {
        return new Label
        {
            Text = text,
            ForeColor = color ?? Text,
            Font = font ?? NormalFont,
            AutoSize = true,
            MaximumSize = new Size(1100, 0),
            Margin = new Padding(6)
        };
    }

    public static Button Button(string text, EventHandler? onClick = null, Color? back = null)
    {
        var button = new Button
        {
            Text = text,
            Height = 62,
            MinimumSize = new Size(160, 62),
            BackColor = back ?? Panel2,
            ForeColor = Text,
            Font = ButtonFont,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(7),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        if (onClick != null) button.Click += onClick;
        return button;
    }

    public static TextBox TextBox(string text = "")
    {
        return new TextBox
        {
            Text = text,
            BackColor = Input,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = NormalFont,
            Margin = new Padding(6),
            MinimumSize = new Size(140, 34),
            Dock = DockStyle.Fill
        };
    }

    public static RichTextBox NotesBox(string text = "", int height = 96)
    {
        return new RichTextBox
        {
            Text = text,
            BackColor = Input,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = NormalFont,
            Height = height,
            MinimumSize = new Size(140, height),
            Margin = new Padding(6),
            Dock = DockStyle.Fill
        };
    }

    public static ComboBox Combo(params string[] items)
    {
        var combo = new ComboBox
        {
            BackColor = Input,
            ForeColor = Text,
            FlatStyle = FlatStyle.Flat,
            Font = NormalFont,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(6),
            MinimumSize = new Size(140, 34),
            Dock = DockStyle.Fill
        };
        combo.Items.AddRange(items.Cast<object>().ToArray());
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    public static NumericUpDown MoneyBox(decimal value = 0)
    {
        return new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999999,
            DecimalPlaces = 2,
            Increment = 1,
            Value = value,
            BackColor = Input,
            ForeColor = Text,
            Font = NormalFont,
            Margin = new Padding(6),
            MinimumSize = new Size(140, 34),
            Dock = DockStyle.Fill,
            ThousandsSeparator = true
        };
    }

    public static NumericUpDown QtyBox(decimal value = 1)
    {
        return new NumericUpDown
        {
            Minimum = 1,
            Maximum = 9999,
            DecimalPlaces = 0,
            Increment = 1,
            Value = value,
            BackColor = Input,
            ForeColor = Text,
            Font = NormalFont,
            Margin = new Padding(6),
            MinimumSize = new Size(140, 34),
            Dock = DockStyle.Fill
        };
    }

    public static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            BackgroundColor = Background,
            BorderStyle = BorderStyle.FixedSingle,
            EnableHeadersVisualStyles = false,
            GridColor = Border,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoGenerateColumns = true,
            Dock = DockStyle.Fill,
            Font = SmallFont,
            Margin = new Padding(0),
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
            ColumnHeadersHeight = 34,
            RowTemplate = { Height = 38 }
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = Color.FromArgb(10, 16, 25);
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 95, 145);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(14, 23, 36);
        return grid;
    }

    public static void BindGrid(DataGridView grid, DataTable table)
    {
        grid.SuspendLayout();
        try
        {
            grid.DataSource = null;
            grid.AutoGenerateColumns = true;
            grid.DataSource = table;
            grid.ClearSelection();
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (string.Equals(col.HeaderText, "Id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(col.DataPropertyName, "Id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(col.Name, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    col.MinimumWidth = 55;
                    col.FillWeight = 35;
                }
                else
                {
                    col.MinimumWidth = 90;
                }
            }
        }
        finally
        {
            grid.ResumeLayout();
        }
    }

    public static DataRowView? SelectedRow(DataGridView grid)
    {
        return grid.CurrentRow?.DataBoundItem as DataRowView;
    }

    public static Panel Card(int padding = 16)
    {
        return new Panel
        {
            BackColor = Panel,
            Padding = new Padding(padding),
            Margin = new Padding(8),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill
        };
    }

    public static TableLayoutPanel FormTable(int labelWidth = 230)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(2),
            Margin = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    public static void AddRow(TableLayoutPanel table, string label, Control control, int height = 48)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, Math.Max(height, control.MinimumSize.Height + 14)));
        var lbl = Label(label, NormalFont, Muted, ContentAlignment.MiddleRight);
        lbl.Dock = DockStyle.Fill;
        control.Dock = DockStyle.Fill;
        table.Controls.Add(lbl, 0, row);
        table.Controls.Add(control, 1, row);
    }

    public static Panel Section(string title, Control content)
    {
        var card = Card();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(Label(title, HeaderFont, Text), 0, 0);
        root.Controls.Add(content, 0, 1);
        card.Controls.Add(root);
        return card;
    }

    public static string Currency(decimal value) => value.ToString("C", CultureInfo.CurrentCulture);

    public static decimal ReadDecimal(object? value)
    {
        if (value == null || value == DBNull.Value) return 0m;
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    public static int ReadInt(object? value)
    {
        if (value == null || value == DBNull.Value) return 0;
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public static string ReadString(object? value)
    {
        if (value == null || value == DBNull.Value) return string.Empty;
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static void Toast(IWin32Window owner, string message, string title = "CJIT ServiceCore")
    {
        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void Warn(IWin32Window owner, string message, string title = "CJIT ServiceCore")
    {
        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static bool Confirm(IWin32Window owner, string message, string title = "CJIT ServiceCore")
    {
        return MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
