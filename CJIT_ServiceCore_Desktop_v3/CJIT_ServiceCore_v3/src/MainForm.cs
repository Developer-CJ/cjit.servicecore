using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class MainForm : Form
{
    private readonly string _actor;
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Label _status = new();

    public MainForm(string actor)
    {
        _actor = actor;
        Text = "CJIT ServiceCore Desktop Edition v3";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1220, 780);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.NormalFont;
        KeyPreview = true;

        BuildShell();
        ShowDashboard();
        Db.Audit(_actor, "OPEN_MAIN", "Main ServiceCore shell opened.");

        KeyDown += (_, e) =>
        {
            if (e.Alt || e.Control || e.Shift) return;
            switch (e.KeyCode)
            {
                case Keys.F1: ShowDashboard(); break;
                case Keys.F2: ShowCustomers(); break;
                case Keys.F3: StartServiceTicketWorkflow(); break;
                case Keys.F4: ShowTickets(); break;
                case Keys.F5: StartSaleWorkflow(); break;
                case Keys.F6: ShowTechBench(); break;
                case Keys.F7: ShowInventory(); break;
                case Keys.F8: StartPickupWorkflow(); break;
                case Keys.F9: StartDailyCloseWorkflow(); break;
                case Keys.F10: ShowAdmin(); break;
            }
        };
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Theme.Panel, Padding = new Padding(18, 8, 18, 8) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        var brand = Theme.Label("CJIT ServiceCore", Theme.TitleFont, Theme.Text, ContentAlignment.MiddleLeft);
        _status.Text = BuildStatusText();
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Theme.Accent2;
        _status.Font = Theme.NormalFont;
        _status.TextAlign = ContentAlignment.MiddleRight;
        header.Controls.Add(brand, 0, 0);
        header.Controls.Add(_status, 1, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 292));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Panel,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12)
        };
        AddNav(nav, "F1  Dashboard", (_, _) => ShowDashboard());
        AddNav(nav, "F2  Customers", (_, _) => ShowCustomers());
        AddNav(nav, "F3  New Service Ticket", (_, _) => StartServiceTicketWorkflow(), Theme.Accent);
        AddNav(nav, "F4  Ticket Queue", (_, _) => ShowTickets());
        AddNav(nav, "F5  New Sale", (_, _) => StartSaleWorkflow(), Theme.Accent2);
        AddNav(nav, "F6  Tech Bench", (_, _) => ShowTechBench());
        AddNav(nav, "F7  Inventory / Parts", (_, _) => ShowInventory());
        AddNav(nav, "F8  Pickup Device", (_, _) => StartPickupWorkflow(), Theme.Warning);
        AddNav(nav, "F9  Daily Close", (_, _) => StartDailyCloseWorkflow());
        AddNav(nav, "F10 Admin Settings", (_, _) => ShowAdmin());
        AddNav(nav, "Process Center", (_, _) => ShowProcessCenter(), Theme.Panel3);

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Theme.Background;
        _content.Padding = new Padding(16);
        _content.AutoScroll = false;

        body.Controls.Add(nav, 0, 0);
        body.Controls.Add(_content, 1, 0);

        var footer = Theme.Label("Keyboard: F1 Dashboard • F3 New Ticket • F5 New Sale • F8 Pickup • F9 Daily Close • Esc closes workflow windows", Theme.SmallFont, Theme.Muted, ContentAlignment.MiddleLeft);
        footer.BackColor = Color.FromArgb(5, 9, 15);
        footer.Padding = new Padding(14, 0, 0, 0);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }

    private string BuildStatusText()
    {
        return $"{Db.Setting("StoreName", "CJIT ServiceCore")}  •  {Db.Setting("TerminalName", "COUNTER-01")}  •  LOCAL DATABASE ONLINE  •  USER: {_actor}";
    }

    private static void AddNav(FlowLayoutPanel nav, string text, EventHandler click, Color? color = null)
    {
        var button = Theme.Button(text, click, color ?? Theme.Panel2);
        button.Width = 248;
        button.Height = 58;
        nav.Controls.Add(button);
    }

    private void ShowPage(string title, string subtitle, Control page)
    {
        _content.Controls.Clear();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(2, 0, 2, 8) };
        _title.Text = title;
        _title.Dock = DockStyle.Top;
        _title.Height = 38;
        _title.ForeColor = Theme.Text;
        _title.Font = Theme.HeaderFont;
        _subtitle.Text = subtitle;
        _subtitle.Dock = DockStyle.Fill;
        _subtitle.ForeColor = Theme.Muted;
        _subtitle.Font = Theme.NormalFont;
        _subtitle.AutoEllipsis = true;
        header.Controls.Add(_subtitle);
        header.Controls.Add(_title);

        page.Dock = DockStyle.Fill;
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(page, 0, 1);
        _content.Controls.Add(root);
        _status.Text = BuildStatusText();
    }

    private void ShowDashboard()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
        for (var i = 0; i < 5; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        stats.Controls.Add(StatCard("Open Tickets", Count("SELECT COUNT(*) FROM Tickets WHERE Status NOT IN ('Completed','Cancelled')")), 0, 0);
        stats.Controls.Add(StatCard("Ready Pickup", Count("SELECT COUNT(*) FROM Tickets WHERE Status='Ready for Pickup'")), 1, 0);
        stats.Controls.Add(StatCard("Waiting Parts", Count("SELECT COUNT(*) FROM Tickets WHERE Status='Waiting Parts'")), 2, 0);
        stats.Controls.Add(StatCard("Customers", Count("SELECT COUNT(*) FROM Customers")), 3, 0);
        stats.Controls.Add(StatCard("Low Stock", Count("SELECT COUNT(*) FROM InventoryItems WHERE IsActive=1 AND Category <> 'Service' AND Quantity <= ReorderLevel")), 4, 0);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Padding = new Padding(0, 12, 0, 12) };
        for (var i = 0; i < 5; i++) actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        actions.Controls.Add(ActionButton("F3\nNew Service Ticket", (_, _) => StartServiceTicketWorkflow(), Theme.Accent), 0, 0);
        actions.Controls.Add(ActionButton("F5\nNew Sale", (_, _) => StartSaleWorkflow(), Theme.Accent2), 1, 0);
        actions.Controls.Add(ActionButton("F8\nPickup Device", (_, _) => StartPickupWorkflow(), Theme.Warning), 2, 0);
        actions.Controls.Add(ActionButton("Process\nCenter", (_, _) => ShowProcessCenter(), Theme.Panel3), 3, 0);
        actions.Controls.Add(ActionButton("Add\nCustomer", (_, _) => { AddCustomerDialog(); ShowDashboard(); }, Theme.Panel3), 4, 0);

        var recent = Theme.Grid();
        Theme.BindGrid(recent, TicketData(100));
        root.Controls.Add(stats, 0, 0);
        root.Controls.Add(actions, 0, 1);
        root.Controls.Add(Theme.Section("Recent Work Orders", recent), 0, 2);
        ShowPage("Dashboard", "Counter command center: start service processes, review daily status, and jump into active work orders.", root);
    }

    private static Button ActionButton(string text, EventHandler click, Color color)
    {
        var b = Theme.Button(text, click, color);
        b.Dock = DockStyle.Fill;
        b.Height = 86;
        b.MinimumSize = new Size(120, 86);
        return b;
    }

    private static Panel StatCard(string name, int count)
    {
        var card = Theme.Card(10);
        var label = Theme.Label($"{count}\n{name}", Theme.HeaderFont, Theme.Text, ContentAlignment.MiddleCenter);
        label.Dock = DockStyle.Fill;
        card.Controls.Add(label);
        return card;
    }

    private static int Count(string sql) => Convert.ToInt32(Db.Scalar(sql) ?? 0, CultureInfo.InvariantCulture);

    private void ShowProcessCenter()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(4) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.Controls.Add(ProcessCard("New Service Ticket", "Customer → Device intake → Problem → Estimate/deposit → Review/sign-off", "Start Ticket", (_, _) => StartServiceTicketWorkflow(), Theme.Accent), 0, 0);
        root.Controls.Add(ProcessCard("New Sale", "Customer/walk-in → Cart → Payment → Receipt", "Start Sale", (_, _) => StartSaleWorkflow(), Theme.Accent2), 1, 0);
        root.Controls.Add(ProcessCard("Pickup Device", "Find ticket → Review work → Collect balance → Checklist → Close", "Start Pickup", (_, _) => StartPickupWorkflow(), Theme.Warning), 0, 1);
        root.Controls.Add(ProcessCard("Daily Close", "Review totals → Count cash → Over/short → Save closeout", "Start Close", (_, _) => StartDailyCloseWorkflow(), Theme.Panel3), 1, 1);
        ShowPage("Process Center", "U-Haul-style counter flows: pick what the customer is here for and let the system walk the employee through it.", root);
    }

    private static Panel ProcessCard(string title, string description, string buttonText, EventHandler click, Color color)
    {
        var card = Theme.Card(18);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.Controls.Add(Theme.Label(title, Theme.HeaderFont, Theme.Text), 0, 0);
        var desc = Theme.AutoLabel(description, Theme.NormalFont, Theme.Muted);
        desc.Dock = DockStyle.Fill;
        root.Controls.Add(desc, 0, 1);
        var btn = Theme.Button(buttonText, click, color);
        btn.Dock = DockStyle.Fill;
        root.Controls.Add(btn, 0, 2);
        card.Controls.Add(root);
        return card;
    }

    private void ShowCustomers()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var grid = Theme.Grid();
        var search = Theme.TextBox();
        search.Width = 460;

        void Load()
        {
            Theme.BindGrid(grid, Db.Query("""
                SELECT Id, FirstName || ' ' || LastName AS Customer, Phone, Email, BusinessName AS Business, UpdatedAt
                FROM Customers
                WHERE @q='' OR FirstName LIKE @like OR LastName LIKE @like OR Phone LIKE @like OR Email LIKE @like OR BusinessName LIKE @like
                ORDER BY Id DESC LIMIT 300
                """, Db.P("@q", search.Text.Trim()), Db.P("@like", "%" + search.Text.Trim() + "%")));
        }

        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        bar.Controls.Add(Theme.Label("Search", Theme.NormalFont, Theme.Muted));
        bar.Controls.Add(search);
        bar.Controls.Add(Theme.Button("Add Customer", (_, _) => { AddCustomerDialog(); Load(); }, Theme.Accent));
        bar.Controls.Add(Theme.Button("Refresh", (_, _) => Load(), Theme.Panel3));
        search.TextChanged += (_, _) => Load();
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Load();
        ShowPage("Customers", "Customer records, contact information, history starting points, and service ownership trail.", root);
    }

    private void ShowTickets()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var grid = Theme.Grid();
        var search = Theme.TextBox();
        search.Width = 420;

        void Load() => Theme.BindGrid(grid, TicketData(300, search.Text.Trim()));

        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        bar.Controls.Add(Theme.Label("Search", Theme.NormalFont, Theme.Muted));
        bar.Controls.Add(search);
        bar.Controls.Add(Theme.Button("New Service Ticket", (_, _) => { StartServiceTicketWorkflow(); Load(); }, Theme.Accent));
        bar.Controls.Add(Theme.Button("Change Status", (_, _) => ChangeStatus(grid, Load), Theme.Panel3));
        bar.Controls.Add(Theme.Button("Open Summary", (_, _) => ShowTicketSummary(grid), Theme.Panel3));
        search.TextChanged += (_, _) => Load();
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Load();
        ShowPage("Ticket Queue", "Work orders grouped by status, assignment, customer, device, and balance due.", root);
    }

    private void ShowTechBench()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var grid = Theme.Grid();

        void Load() => Theme.BindGrid(grid, Db.Query("""
            SELECT t.Id, t.TicketNumber AS Ticket, t.Priority, t.Status, t.AssignedTo AS Tech,
                   c.FirstName || ' ' || c.LastName AS Customer,
                   d.DeviceType || ' ' || d.Brand || ' ' || d.Model AS Device,
                   t.Issue, t.UpdatedAt
            FROM Tickets t
            JOIN Customers c ON c.Id=t.CustomerId
            JOIN Devices d ON d.Id=t.DeviceId
            WHERE t.Status NOT IN ('Completed','Cancelled')
            ORDER BY CASE t.Priority WHEN 'Urgent' THEN 0 WHEN 'High' THEN 1 WHEN 'Normal' THEN 2 ELSE 3 END, t.UpdatedAt DESC
            LIMIT 300
            """));

        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        bar.Controls.Add(Theme.Button("Set In Progress", (_, _) => UpdateSelectedStatus(grid, "In Progress", Load), Theme.Accent));
        bar.Controls.Add(Theme.Button("Waiting Parts", (_, _) => UpdateSelectedStatus(grid, "Waiting Parts", Load), Theme.Panel3));
        bar.Controls.Add(Theme.Button("Ready for Pickup", (_, _) => UpdateSelectedStatus(grid, "Ready for Pickup", Load), Theme.Accent2));
        bar.Controls.Add(Theme.Button("Add Work Note", (_, _) => AddWorkNote(grid), Theme.Panel3));
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Load();
        ShowPage("Tech Bench", "Technician queue for diagnosis, repair progress, work notes, and ready-for-pickup handoff.", root);
    }

    private void ShowInventory()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var grid = Theme.Grid();
        var search = Theme.TextBox();
        search.Width = 420;

        void Load() => Theme.BindGrid(grid, Db.Query("""
            SELECT Id, Sku, Name, Category, Cost, Price, Quantity, ReorderLevel AS Reorder, BinLocation AS Bin, Notes
            FROM InventoryItems
            WHERE IsActive=1 AND (@q='' OR Sku LIKE @like OR Name LIKE @like OR Category LIKE @like OR BinLocation LIKE @like)
            ORDER BY Category, Name LIMIT 400
            """, Db.P("@q", search.Text.Trim()), Db.P("@like", "%" + search.Text.Trim() + "%")));

        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        bar.Controls.Add(Theme.Label("Search", Theme.NormalFont, Theme.Muted));
        bar.Controls.Add(search);
        bar.Controls.Add(Theme.Button("Add Item / Service", (_, _) => { AddInventoryDialog(); Load(); }, Theme.Accent));
        bar.Controls.Add(Theme.Button("Refresh", (_, _) => Load(), Theme.Panel3));
        search.TextChanged += (_, _) => Load();
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Load();
        ShowPage("Inventory / Parts", "Retail items, repair parts, services, bin locations, stock levels, and reorder warnings.", root);
    }

    private void ShowAdmin()
    {
        var card = Theme.Card(18);
        var form = Theme.FormTable(230);
        var store = Theme.TextBox(Db.Setting("StoreName", "CJIT ServiceCore Demo Store"));
        var terminal = Theme.TextBox(Db.Setting("TerminalName", "COUNTER-01"));
        var tax = Theme.TextBox(Db.Setting("TaxRate", "0.06"));
        var diag = Theme.TextBox(Db.Setting("DiagnosticFee", "35.00"));
        Theme.AddRow(form, "Store Name", store);
        Theme.AddRow(form, "Terminal Name", terminal);
        Theme.AddRow(form, "Tax Rate", tax);
        Theme.AddRow(form, "Default Diagnostic Fee", diag);
        var save = Theme.Button("Save Settings", (_, _) =>
        {
            Db.SetSetting("StoreName", store.Text.Trim());
            Db.SetSetting("TerminalName", terminal.Text.Trim());
            Db.SetSetting("TaxRate", tax.Text.Trim());
            Db.SetSetting("DiagnosticFee", diag.Text.Trim());
            Db.Audit(_actor, "SAVE_SETTINGS", "Updated local app settings.");
            _status.Text = BuildStatusText();
            Theme.Toast(this, "Settings saved.");
        }, Theme.Accent);
        save.Dock = DockStyle.Top;
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.Controls.Add(form, 0, 0);
        root.Controls.Add(save, 0, 1);
        card.Controls.Add(root);
        ShowPage("Admin Settings", "Local terminal configuration for the desktop edition.", card);
    }

    private static DataTable TicketData(int limit, string q = "")
    {
        return Db.Query("""
            SELECT t.Id, t.TicketNumber AS Ticket, t.Status, t.Priority,
                   c.FirstName || ' ' || c.LastName AS Customer,
                   d.DeviceType || ' ' || d.Brand || ' ' || d.Model AS Device,
                   t.ServiceCategory AS Service, t.BalanceDue AS Balance, t.AssignedTo AS Tech, t.UpdatedAt
            FROM Tickets t
            JOIN Customers c ON c.Id=t.CustomerId
            JOIN Devices d ON d.Id=t.DeviceId
            WHERE @q='' OR t.TicketNumber LIKE @like OR t.Status LIKE @like OR c.FirstName LIKE @like OR c.LastName LIKE @like OR d.Brand LIKE @like OR d.Model LIKE @like
            ORDER BY t.Id DESC LIMIT @limit
            """, Db.P("@q", q), Db.P("@like", "%" + q + "%"), Db.P("@limit", limit));
    }

    private void StartServiceTicketWorkflow()
    {
        using var wizard = new ServiceTicketWorkflowForm(_actor);
        wizard.ShowDialog(this);
        ShowTickets();
    }

    private void StartSaleWorkflow()
    {
        using var wizard = new SaleWorkflowForm(_actor);
        wizard.ShowDialog(this);
        ShowDashboard();
    }

    private void StartPickupWorkflow()
    {
        using var wizard = new PickupWorkflowForm(_actor);
        wizard.ShowDialog(this);
        ShowDashboard();
    }

    private void StartDailyCloseWorkflow()
    {
        using var wizard = new DailyCloseWorkflowForm(_actor);
        wizard.ShowDialog(this);
        ShowDashboard();
    }

    private static int SelectedId(DataGridView grid)
    {
        var row = Theme.SelectedRow(grid);
        if (row == null || !row.Row.Table.Columns.Contains("Id")) return 0;
        return Convert.ToInt32(row["Id"], CultureInfo.InvariantCulture);
    }

    private void ChangeStatus(DataGridView grid, Action reload)
    {
        var id = SelectedId(grid);
        if (id <= 0)
        {
            Theme.Warn(this, "Select a ticket first.");
            return;
        }
        using var dlg = new StatusDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            UpdateStatus(id, dlg.SelectedStatus);
            reload();
        }
    }

    private void UpdateSelectedStatus(DataGridView grid, string status, Action reload)
    {
        var id = SelectedId(grid);
        if (id <= 0)
        {
            Theme.Warn(this, "Select a ticket first.");
            return;
        }
        UpdateStatus(id, status);
        reload();
    }

    private void UpdateStatus(int ticketId, string status)
    {
        Db.Execute("UPDATE Tickets SET Status=@status, UpdatedAt=@updated WHERE Id=@id", Db.P("@status", status), Db.P("@updated", Db.Now()), Db.P("@id", ticketId));
        Db.Insert("""
            INSERT INTO TicketNotes(TicketId, Author, NoteType, NoteText, CreatedAt)
            VALUES(@ticket, @author, 'Status', @note, @created)
            """, Db.P("@ticket", ticketId), Db.P("@author", _actor), Db.P("@note", "Status changed to " + status), Db.P("@created", Db.Now()));
        Db.Audit(_actor, "UPDATE_TICKET_STATUS", $"Ticket #{ticketId} => {status}");
    }

    private void AddWorkNote(DataGridView grid)
    {
        var id = SelectedId(grid);
        if (id <= 0)
        {
            Theme.Warn(this, "Select a ticket first.");
            return;
        }
        using var dlg = new NoteDialog("Add Work Note");
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            Db.Insert("""
                INSERT INTO TicketNotes(TicketId, Author, NoteType, NoteText, CreatedAt)
                VALUES(@ticket, @author, 'Work Note', @note, @created)
                """, Db.P("@ticket", id), Db.P("@author", _actor), Db.P("@note", dlg.NoteText), Db.P("@created", Db.Now()));
            Db.Execute("UPDATE Tickets SET UpdatedAt=@updated WHERE Id=@id", Db.P("@updated", Db.Now()), Db.P("@id", id));
        }
    }

    private void ShowTicketSummary(DataGridView grid)
    {
        var id = SelectedId(grid);
        if (id <= 0)
        {
            Theme.Warn(this, "Select a ticket first.");
            return;
        }
        var data = Db.Query("""
            SELECT t.*, c.FirstName || ' ' || c.LastName AS Customer, c.Phone, c.Email,
                   d.DeviceType || ' ' || d.Brand || ' ' || d.Model AS Device, d.SerialNumber, d.Imei
            FROM Tickets t
            JOIN Customers c ON c.Id=t.CustomerId
            JOIN Devices d ON d.Id=t.DeviceId
            WHERE t.Id=@id
            """, Db.P("@id", id));
        if (data.Rows.Count == 0) return;
        var r = data.Rows[0];
        var msg = $"Ticket: {r["TicketNumber"]}\nStatus: {r["Status"]}\nPriority: {r["Priority"]}\nCustomer: {r["Customer"]}\nPhone: {r["Phone"]}\nDevice: {r["Device"]}\nIssue: {r["Issue"]}\n\nEstimate: {Theme.Currency(Theme.ReadDecimal(r["EstimateTotal"]))}\nDeposit: {Theme.Currency(Theme.ReadDecimal(r["DepositPaid"]))}\nBalance: {Theme.Currency(Theme.ReadDecimal(r["BalanceDue"]))}";
        MessageBox.Show(this, msg, "Ticket Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AddCustomerDialog()
    {
        using var dlg = new CustomerDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            Db.Insert("""
                INSERT INTO Customers(FirstName, LastName, Phone, Email, Address, BusinessName, Notes, CreatedAt, UpdatedAt)
                VALUES(@first,@last,@phone,@email,@address,@business,@notes,@now,@now)
                """, Db.P("@first", dlg.FirstName), Db.P("@last", dlg.LastName), Db.P("@phone", dlg.Phone), Db.P("@email", dlg.Email), Db.P("@address", dlg.Address), Db.P("@business", dlg.Business), Db.P("@notes", dlg.Notes), Db.P("@now", Db.Now()));
            Db.Audit(_actor, "ADD_CUSTOMER", dlg.FirstName + " " + dlg.LastName);
        }
    }

    private void AddInventoryDialog()
    {
        using var dlg = new InventoryDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            Db.Insert("""
                INSERT INTO InventoryItems(Sku, Name, Category, Cost, Price, Quantity, ReorderLevel, BinLocation, Notes, IsActive, CreatedAt, UpdatedAt)
                VALUES(@sku,@name,@category,@cost,@price,@qty,@reorder,@bin,@notes,1,@now,@now)
                """, Db.P("@sku", dlg.Sku), Db.P("@name", dlg.ItemName), Db.P("@category", dlg.Category), Db.P("@cost", dlg.Cost), Db.P("@price", dlg.Price), Db.P("@qty", dlg.Quantity), Db.P("@reorder", dlg.Reorder), Db.P("@bin", dlg.Bin), Db.P("@notes", dlg.Notes), Db.P("@now", Db.Now()));
            Db.Audit(_actor, "ADD_INVENTORY", dlg.Sku + " " + dlg.ItemName);
        }
    }
}
