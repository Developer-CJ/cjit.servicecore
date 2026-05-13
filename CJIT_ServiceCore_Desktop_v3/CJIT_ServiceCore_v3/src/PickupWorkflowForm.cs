using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class PickupWorkflowForm : WorkflowForm
{
    private int _ticketId;
    private int _customerId;
    private decimal _balance;

    private readonly TextBox _search = Theme.TextBox();
    private readonly DataGridView _ticketGrid = Theme.Grid();
    private readonly Label _selectedTicket = Theme.Label("No ticket selected yet.", Theme.NormalFont, Theme.Warning);
    private readonly RichTextBox _workPerformed = Theme.NotesBox("Device returned to customer after final review.", 150);
    private readonly Label _balanceLabel = Theme.Label("Balance: $0.00", Theme.HeaderFont, Theme.Accent2);
    private readonly ComboBox _method = Theme.Combo("Cash", "Card - Manual", "Store Credit", "Other");
    private readonly NumericUpDown _amount = Theme.MoneyBox();
    private readonly CheckBox _chkPower = Check("Device powers on / basic function verified.");
    private readonly CheckBox _chkAccessories = Check("Accessories returned or exceptions noted.");
    private readonly CheckBox _chkBalance = Check("Balance collected or manager-approved exception noted.");
    private readonly CheckBox _chkCustomer = Check("Customer pickup sign-off completed.");
    private readonly RichTextBox _review = Theme.NotesBox("", 330);

    public PickupWorkflowForm(string actor) : base("Pickup Device", actor)
    {
        AddStep(new ProcessStep
        {
            Title = "Find Ticket",
            Instruction = "Search for the work order being picked up. Ready-for-pickup tickets appear first, but any open ticket can be selected.",
            Content = BuildFindStep(),
            Validate = ValidateTicketSelected
        });
        AddStep(new ProcessStep
        {
            Title = "Review Work",
            Instruction = "Review or add what was done before returning the device. This note gets saved to the ticket timeline.",
            Content = BuildWorkStep(),
            Validate = ValidateWork
        });
        AddStep(new ProcessStep
        {
            Title = "Collect Balance",
            Instruction = "Collect the remaining balance before closing the ticket. Manual/local payment tracking for now.",
            Content = BuildBalanceStep(),
            OnEnter = LoadBalance,
            Validate = ValidatePayment
        });
        AddStep(new ProcessStep
        {
            Title = "Final Checklist",
            Instruction = "Use this as the counter handoff checklist so the pickup is not sloppy.",
            Content = BuildChecklistStep(),
            Validate = ValidateChecklist
        });
        AddStep(new ProcessStep
        {
            Title = "Review / Close",
            Instruction = "Review the pickup summary, then ServiceCore marks the ticket completed and records the payment/note.",
            Content = BuildReviewStep(),
            OnEnter = BuildReview
        });
        StartWorkflow();
        LoadTickets();
    }

    private static CheckBox Check(string text) => new() { Text = text, ForeColor = Theme.Text, AutoSize = true, Font = Theme.NormalFont, Margin = new Padding(8) };

    private Control BuildFindStep()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 3, ColumnCount = 1, Padding = new Padding(2) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        _search.Width = 460;
        bar.Controls.Add(Theme.Label("Search ticket/customer/device", Theme.NormalFont, Theme.Muted));
        bar.Controls.Add(_search);
        bar.Controls.Add(Theme.Button("Search", (_, _) => LoadTickets(), Theme.Panel3));
        bar.Controls.Add(Theme.Button("Use Selected Ticket", (_, _) => SelectTicket(), Theme.Accent));
        _search.TextChanged += (_, _) => LoadTickets();
        _ticketGrid.CellDoubleClick += (_, _) => SelectTicket();
        _selectedTicket.Dock = DockStyle.Fill;
        _selectedTicket.BackColor = Theme.Panel2;
        _selectedTicket.Padding = new Padding(12, 0, 0, 0);
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(_ticketGrid, 0, 1);
        root.Controls.Add(_selectedTicket, 0, 2);
        return root;
    }

    private Control BuildWorkStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Work / Pickup Note", _workPerformed, 180);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
        return card;
    }

    private Control BuildBalanceStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Balance Due", _balanceLabel, 60);
        Theme.AddRow(form, "Payment Method", _method);
        Theme.AddRow(form, "Amount Collected", _amount);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
        return card;
    }

    private Control BuildChecklistStep()
    {
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        stack.Controls.Add(_chkPower);
        stack.Controls.Add(_chkAccessories);
        stack.Controls.Add(_chkBalance);
        stack.Controls.Add(_chkCustomer);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(stack);
        return card;
    }

    private Control BuildReviewStep()
    {
        _review.ReadOnly = true;
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(_review);
        return card;
    }

    private void LoadTickets()
    {
        var q = _search.Text.Trim();
        Theme.BindGrid(_ticketGrid, Db.Query("""
            SELECT t.Id, t.TicketNumber AS Ticket, t.Status, t.Priority,
                   c.Id AS CustomerId, c.FirstName || ' ' || c.LastName AS Customer,
                   d.DeviceType || ' ' || d.Brand || ' ' || d.Model AS Device,
                   t.BalanceDue AS Balance, t.UpdatedAt
            FROM Tickets t
            JOIN Customers c ON c.Id=t.CustomerId
            JOIN Devices d ON d.Id=t.DeviceId
            WHERE t.Status NOT IN ('Completed','Cancelled')
              AND (@q='' OR t.TicketNumber LIKE @like OR c.FirstName LIKE @like OR c.LastName LIKE @like OR d.Brand LIKE @like OR d.Model LIKE @like)
            ORDER BY CASE WHEN t.Status='Ready for Pickup' THEN 0 ELSE 1 END, t.Id DESC
            LIMIT 200
            """, Db.P("@q", q), Db.P("@like", "%" + q + "%")));
    }

    private void SelectTicket()
    {
        var row = Theme.SelectedRow(_ticketGrid);
        if (row == null)
        {
            Theme.Warn(this, "Select a ticket first.");
            return;
        }
        _ticketId = Convert.ToInt32(row["Id"], CultureInfo.InvariantCulture);
        _customerId = Convert.ToInt32(row["CustomerId"], CultureInfo.InvariantCulture);
        _balance = Theme.ReadDecimal(row["Balance"]);
        _selectedTicket.Text = $"Selected: {row["Ticket"]}  •  {row["Customer"]}  •  {row["Device"]}  •  Balance {Theme.Currency(_balance)}";
    }

    private bool ValidateTicketSelected()
    {
        if (_ticketId <= 0)
        {
            Theme.Warn(this, "Select a ticket before continuing.");
            return false;
        }
        return true;
    }

    private bool ValidateWork()
    {
        if (string.IsNullOrWhiteSpace(_workPerformed.Text))
        {
            Theme.Warn(this, "Add a brief pickup/work note.");
            return false;
        }
        return true;
    }

    private void LoadBalance()
    {
        var table = Db.Query("SELECT BalanceDue FROM Tickets WHERE Id=@id", Db.P("@id", _ticketId));
        _balance = table.Rows.Count == 0 ? 0m : Theme.ReadDecimal(table.Rows[0]["BalanceDue"]);
        _balanceLabel.Text = "Balance Due: " + Theme.Currency(_balance);
        if (_amount.Value == 0 && _balance > 0) _amount.Value = _balance;
    }

    private bool ValidatePayment()
    {
        if (_balance > 0 && _amount.Value < _balance)
        {
            Theme.Warn(this, "Amount collected is less than the balance due.");
            return false;
        }
        return true;
    }

    private bool ValidateChecklist()
    {
        if (!_chkPower.Checked || !_chkAccessories.Checked || !_chkBalance.Checked || !_chkCustomer.Checked)
        {
            Theme.Warn(this, "Complete all pickup checklist items before closing the ticket.");
            return false;
        }
        return true;
    }

    private void BuildReview()
    {
        var sb = new StringBuilder();
        sb.AppendLine("CJIT SERVICECORE PICKUP REVIEW");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine(_selectedTicket.Text);
        sb.AppendLine();
        sb.AppendLine("Work / pickup note:");
        sb.AppendLine(_workPerformed.Text.Trim());
        sb.AppendLine();
        sb.AppendLine($"Balance Due: {Theme.Currency(_balance)}");
        sb.AppendLine($"Amount Collected: {Theme.Currency(_amount.Value)} via {_method.Text}");
        sb.AppendLine();
        sb.AppendLine("Final checklist complete.");
        _review.Text = sb.ToString();
    }

    protected override bool FinishWorkflow()
    {
        var now = Db.Now();
        if (_amount.Value > 0)
        {
            Db.Insert("""
                INSERT INTO Payments(CustomerId, TicketId, PaymentType, Method, Amount, CreatedAt, Note)
                VALUES(@customer,@ticket,'Balance',@method,@amount,@now,@note)
                """, Db.P("@customer", _customerId), Db.P("@ticket", _ticketId), Db.P("@method", _method.Text), Db.P("@amount", _amount.Value), Db.P("@now", now), Db.P("@note", "Balance collected during pickup workflow."));
        }
        Db.Insert("""
            INSERT INTO TicketNotes(TicketId, Author, NoteType, NoteText, CreatedAt)
            VALUES(@ticket,@author,'Pickup',@note,@now)
            """, Db.P("@ticket", _ticketId), Db.P("@author", Actor), Db.P("@note", _workPerformed.Text.Trim()), Db.P("@now", now));
        Db.Execute("UPDATE Tickets SET Status='Completed', BalanceDue=0, UpdatedAt=@now WHERE Id=@id", Db.P("@now", now), Db.P("@id", _ticketId));
        Db.Audit(Actor, "PICKUP_COMPLETE", "Ticket #" + _ticketId + " closed at pickup.");
        Theme.Toast(this, "Pickup completed and ticket closed.");
        return true;
    }
}
