using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class LoginForm : Form
{
    private readonly TextBox _username = Theme.TextBox("chris");
    private readonly TextBox _password = Theme.TextBox("1228");
    public string Actor { get; private set; } = "chris";

    public LoginForm()
    {
        Text = "CJIT ServiceCore Login";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(620, 440);
        Size = new Size(720, 500);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.NormalFont;
        KeyPreview = true;

        _password.PasswordChar = '●';

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(28) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var title = Theme.Label("CJIT ServiceCore", Theme.TitleFont, Theme.Text, ContentAlignment.MiddleCenter);
        var formCard = Theme.Card(22);
        var form = Theme.FormTable(180);
        Theme.AddRow(form, "Username", _username);
        Theme.AddRow(form, "Password", _password);
        formCard.Controls.Add(form);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var login = Theme.Button("Login  Enter", (_, _) => TryLogin(), Theme.Accent);
        var cancel = Theme.Button("Cancel", (_, _) => Close(), Theme.Panel3);
        buttons.Controls.Add(login);
        buttons.Controls.Add(cancel);

        var hint = Theme.Label("Default demo login: chris / 1228", Theme.SmallFont, Theme.Muted, ContentAlignment.MiddleCenter);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(formCard, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        root.Controls.Add(hint, 0, 3);
        Controls.Add(root);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) TryLogin();
            if (e.KeyCode == Keys.Escape) Close();
        };
    }

    private void TryLogin()
    {
        var table = Db.Query("""
            SELECT DisplayName, Username FROM Users
            WHERE Username=@u AND Password=@p AND IsActive=1
            LIMIT 1
            """, Db.P("@u", _username.Text.Trim()), Db.P("@p", _password.Text));

        if (table.Rows.Count == 0)
        {
            Theme.Warn(this, "Invalid username or password.");
            return;
        }

        Actor = Convert.ToString(table.Rows[0]["Username"]) ?? _username.Text.Trim();
        Db.Audit(Actor, "LOGIN", "User logged into ServiceCore desktop terminal.");
        DialogResult = DialogResult.OK;
        Close();
    }
}
