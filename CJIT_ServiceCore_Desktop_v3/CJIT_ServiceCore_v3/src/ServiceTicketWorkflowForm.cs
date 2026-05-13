using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class ServiceTicketWorkflowForm : WorkflowForm
{
    private int _selectedCustomerId;

    private readonly TextBox _customerSearch = Theme.TextBox();
    private readonly DataGridView _customerGrid = Theme.Grid();
    private readonly TextBox _first = Theme.TextBox();
    private readonly TextBox _last = Theme.TextBox();
    private readonly TextBox _phone = Theme.TextBox();
    private readonly TextBox _email = Theme.TextBox();
    private readonly TextBox _address = Theme.TextBox();
    private readonly TextBox _business = Theme.TextBox();
    private readonly RichTextBox _customerNotes = Theme.NotesBox("", 78);
    private readonly Label _selectedCustomer = Theme.Label("No customer selected yet. Search/select, enter a new customer, or use Walk-in.", Theme.NormalFont, Theme.Warning);

    private readonly ComboBox _deviceType = Theme.Combo("Phone", "Laptop", "Desktop", "Tablet", "Printer", "Router", "Game Console", "POS Terminal", "Other");
    private readonly TextBox _brand = Theme.TextBox();
    private readonly TextBox _model = Theme.TextBox();
    private readonly TextBox _serial = Theme.TextBox();
    private readonly TextBox _imei = Theme.TextBox();
    private readonly TextBox _passcode = Theme.TextBox();
    private readonly RichTextBox _condition = Theme.NotesBox("", 90);
    private readonly RichTextBox _accessories = Theme.NotesBox("", 78);

    private readonly ComboBox _category = Theme.Combo("Computer Repair", "Phone Repair", "Data Recovery", "Network Service", "Business IT", "Warranty Return", "Estimate Only", "Other");
    private readonly ComboBox _priority = Theme.Combo("Normal", "High", "Urgent", "Low");
    private readonly TextBox _assigned = Theme.TextBox("CJ");
    private readonly RichTextBox _issue = Theme.NotesBox("", 120);
    private readonly RichTextBox _internalNote = Theme.NotesBox("", 92);

    private readonly NumericUpDown _diag = Theme.MoneyBox();
    private readonly NumericUpDown _labor = Theme.MoneyBox();
    private readonly NumericUpDown _parts = Theme.MoneyBox();
    private readonly NumericUpDown _deposit = Theme.MoneyBox();
    private readonly Label _moneySummary = Theme.Label("Estimate: $0.00   Deposit: $0.00   Balance: $0.00", Theme.HeaderFont, Theme.Accent2);

    private readonly CheckBox _ackCondition = new() { Text = "Customer acknowledges listed device condition and accessories.", ForeColor = Theme.Text, AutoSize = true, Font = Theme.NormalFont, Margin = new Padding(8) };
    private readonly CheckBox _ackDiag = new() { Text = "Customer approves diagnostic/estimate process.", ForeColor = Theme.Text, AutoSize = true, Font = Theme.NormalFont, Margin = new Padding(8) };
    private readonly CheckBox _ackData = new() { Text = "Customer understands data backup is their responsibility unless data service is purchased.", ForeColor = Theme.Text, AutoSize = true, Font = Theme.NormalFont, Margin = new Padding(8) };
    private readonly RichTextBox _review = Theme.NotesBox("", 320);

    public ServiceTicketWorkflowForm(string actor) : base("New Service Ticket", actor)
    {
        if (decimal.TryParse(Db.Setting("DiagnosticFee", "35.00"), NumberStyles.Number, CultureInfo.InvariantCulture, out var diagnostic))
            _diag.Value = diagnostic;
        else
            _diag.Value = 35m;

        _diag.ValueChanged += (_, _) => UpdateMoneySummary();
        _labor.ValueChanged += (_, _) => UpdateMoneySummary();
        _parts.ValueChanged += (_, _) => UpdateMoneySummary();
        _deposit.ValueChanged += (_, _) => UpdateMoneySummary();

        AddStep(new ProcessStep
        {
            Title = "Customer",
            Instruction = "Find the customer first. If they are new, enter their info here. For quick retail-style service intake, use Walk-in Customer.",
            Content = BuildCustomerStep(),
            Validate = ValidateCustomer
        });
        AddStep(new ProcessStep
        {
            Title = "Device Intake",
            Instruction = "Record the device, serial/IMEI, condition, passcode/access notes, and anything the customer left with the device.",
            Content = BuildDeviceStep(),
            Validate = ValidateDevice
        });
        AddStep(new ProcessStep
        {
            Title = "Problem / Service",
            Instruction = "Capture what the customer says is wrong, assign priority, and choose who owns the next action.",
            Content = BuildIssueStep(),
            Validate = ValidateIssue
        });
        AddStep(new ProcessStep
        {
            Title = "Estimate / Deposit",
            Instruction = "Set diagnostic fee, labor estimate, parts estimate, and any deposit collected now. Balance is calculated automatically.",
            Content = BuildEstimateStep(),
            Validate = ValidateEstimate
        });
        AddStep(new ProcessStep
        {
            Title = "Review / Create",
            Instruction = "Review the intake like a counter contract. Check the acknowledgements, then ServiceCore creates the ticket and deposit record.",
            Content = BuildReviewStep(),
            OnEnter = BuildReview,
            Validate = ValidateAgreements
        });

        StartWorkflow();
        LoadCustomers();
        UpdateMoneySummary();
    }

    private Control BuildCustomerStep()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, RowCount = 4, ColumnCount = 1, Padding = new Padding(2) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var searchBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        _customerSearch.Width = 440;
        searchBar.Controls.Add(Theme.Label("Search customer", Theme.NormalFont, Theme.Muted));
        searchBar.Controls.Add(_customerSearch);
        searchBar.Controls.Add(Theme.Button("Search", (_, _) => LoadCustomers(), Theme.Panel3));
        searchBar.Controls.Add(Theme.Button("Use Selected", (_, _) => SelectCustomer(), Theme.Accent));
        searchBar.Controls.Add(Theme.Button("Walk-in", (_, _) => UseWalkIn(), Theme.Panel3));
        _customerSearch.TextChanged += (_, _) => LoadCustomers();
        _customerGrid.CellDoubleClick += (_, _) => SelectCustomer();

        _selectedCustomer.Dock = DockStyle.Fill;
        _selectedCustomer.BackColor = Theme.Panel2;
        _selectedCustomer.Padding = new Padding(12, 0, 0, 0);

        var form = Theme.FormTable(190);
        Theme.AddRow(form, "First Name", _first);
        Theme.AddRow(form, "Last Name", _last);
        Theme.AddRow(form, "Phone", _phone);
        Theme.AddRow(form, "Email", _email);
        Theme.AddRow(form, "Address", _address);
        Theme.AddRow(form, "Business", _business);
        Theme.AddRow(form, "Customer Notes", _customerNotes, 100);

        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);

        root.Controls.Add(searchBar, 0, 0);
        root.Controls.Add(_customerGrid, 0, 1);
        root.Controls.Add(_selectedCustomer, 0, 2);
        root.Controls.Add(card, 0, 3);
        return root;
    }

    private Control BuildDeviceStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Device Type", _deviceType);
        Theme.AddRow(form, "Brand", _brand);
        Theme.AddRow(form, "Model", _model);
        Theme.AddRow(form, "Serial Number", _serial);
        Theme.AddRow(form, "IMEI", _imei);
        Theme.AddRow(form, "Passcode / Access Notes", _passcode);
        Theme.AddRow(form, "Condition", _condition, 112);
        Theme.AddRow(form, "Accessories Included", _accessories, 100);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
        return card;
    }

    private Control BuildIssueStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Service Category", _category);
        Theme.AddRow(form, "Priority", _priority);
        Theme.AddRow(form, "Assigned Tech", _assigned);
        Theme.AddRow(form, "Customer Complaint", _issue, 142);
        Theme.AddRow(form, "Internal Intake Note", _internalNote, 112);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
        return card;
    }

    private Control BuildEstimateStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Diagnostic Fee", _diag);
        Theme.AddRow(form, "Labor Estimate", _labor);
        Theme.AddRow(form, "Parts Estimate", _parts);
        Theme.AddRow(form, "Deposit Paid Now", _deposit);
        Theme.AddRow(form, "Calculated Result", _moneySummary, 64);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
        return card;
    }

    private Control BuildReviewStep()
    {
        _review.ReadOnly = true;
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _review.Width = 980;
        stack.Controls.Add(_review);
        stack.Controls.Add(_ackCondition);
        stack.Controls.Add(_ackDiag);
        stack.Controls.Add(_ackData);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(stack);
        return card;
    }

    private void LoadCustomers()
    {
        var q = _customerSearch.Text.Trim();
        var table = Db.Query("""
            SELECT Id, FirstName || ' ' || LastName AS Customer, Phone, Email, BusinessName AS Business, UpdatedAt
            FROM Customers
            WHERE @q = '' OR FirstName LIKE @like OR LastName LIKE @like OR Phone LIKE @like OR Email LIKE @like OR BusinessName LIKE @like
            ORDER BY Id DESC LIMIT 150
            """, Db.P("@q", q), Db.P("@like", "%" + q + "%"));
        Theme.BindGrid(_customerGrid, table);
    }

    private void SelectCustomer()
    {
        var row = Theme.SelectedRow(_customerGrid);
        if (row == null)
        {
            Theme.Warn(this, "Select a customer row first.");
            return;
        }
        _selectedCustomerId = Convert.ToInt32(row["Id"], CultureInfo.InvariantCulture);
        var table = Db.Query("SELECT * FROM Customers WHERE Id=@id", Db.P("@id", _selectedCustomerId));
        if (table.Rows.Count == 0) return;
        var customer = table.Rows[0];
        _first.Text = Theme.ReadString(customer["FirstName"]);
        _last.Text = Theme.ReadString(customer["LastName"]);
        _phone.Text = Theme.ReadString(customer["Phone"]);
        _email.Text = Theme.ReadString(customer["Email"]);
        _address.Text = Theme.ReadString(customer["Address"]);
        _business.Text = Theme.ReadString(customer["BusinessName"]);
        _customerNotes.Text = Theme.ReadString(customer["Notes"]);
        _selectedCustomer.Text = $"Selected customer #{_selectedCustomerId}: {_first.Text} {_last.Text}  •  {_phone.Text}";
    }

    private void UseWalkIn()
    {
        _selectedCustomerId = 0;
        _first.Text = "Walk-in";
        _last.Text = "Customer";
        _phone.Text = "";
        _email.Text = "";
        _address.Text = "";
        _business.Text = "";
        _customerNotes.Text = "Quick walk-in intake.";
        _selectedCustomer.Text = "Using Walk-in Customer. A local customer record will be created for this ticket.";
    }

    private bool ValidateCustomer()
    {
        if (string.IsNullOrWhiteSpace(_first.Text) && string.IsNullOrWhiteSpace(_last.Text))
        {
            Theme.Warn(this, "Select a customer, enter a customer, or press Walk-in.");
            return false;
        }
        return true;
    }

    private bool ValidateDevice()
    {
        if (string.IsNullOrWhiteSpace(_brand.Text) && string.IsNullOrWhiteSpace(_model.Text))
        {
            Theme.Warn(this, "Enter at least a brand or model for the device.");
            return false;
        }
        return true;
    }

    private bool ValidateIssue()
    {
        if (string.IsNullOrWhiteSpace(_issue.Text))
        {
            Theme.Warn(this, "Enter the customer complaint or service request.");
            return false;
        }
        return true;
    }

    private bool ValidateEstimate()
    {
        var total = EstimateTotal;
        if (_deposit.Value > total)
        {
            Theme.Warn(this, "Deposit cannot be greater than the total estimate.");
            return false;
        }
        return true;
    }

    private bool ValidateAgreements()
    {
        if (!_ackCondition.Checked || !_ackDiag.Checked || !_ackData.Checked)
        {
            Theme.Warn(this, "Check all customer acknowledgement boxes before creating the ticket.");
            return false;
        }
        return true;
    }

    private decimal EstimateTotal => _diag.Value + _labor.Value + _parts.Value;
    private decimal BalanceDue => EstimateTotal - _deposit.Value;

    private void UpdateMoneySummary()
    {
        _moneySummary.Text = $"Estimate: {Theme.Currency(EstimateTotal)}   Deposit: {Theme.Currency(_deposit.Value)}   Balance: {Theme.Currency(BalanceDue)}";
    }

    private void BuildReview()
    {
        UpdateMoneySummary();
        var sb = new StringBuilder();
        sb.AppendLine("CJIT SERVICECORE SERVICE TICKET REVIEW");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine($"Customer: {_first.Text.Trim()} {_last.Text.Trim()}");
        sb.AppendLine($"Phone: {_phone.Text.Trim()}   Email: {_email.Text.Trim()}");
        sb.AppendLine($"Device: {_deviceType.Text} {_brand.Text.Trim()} {_model.Text.Trim()}");
        sb.AppendLine($"Serial: {_serial.Text.Trim()}   IMEI: {_imei.Text.Trim()}");
        sb.AppendLine($"Condition: {_condition.Text.Trim()}");
        sb.AppendLine($"Accessories: {_accessories.Text.Trim()}");
        sb.AppendLine();
        sb.AppendLine($"Service: {_category.Text}   Priority: {_priority.Text}   Assigned: {_assigned.Text.Trim()}");
        sb.AppendLine($"Issue: {_issue.Text.Trim()}");
        sb.AppendLine();
        sb.AppendLine($"Diagnostic: {Theme.Currency(_diag.Value)}");
        sb.AppendLine($"Labor:      {Theme.Currency(_labor.Value)}");
        sb.AppendLine($"Parts:      {Theme.Currency(_parts.Value)}");
        sb.AppendLine($"Estimate:   {Theme.Currency(EstimateTotal)}");
        sb.AppendLine($"Deposit:    {Theme.Currency(_deposit.Value)}");
        sb.AppendLine($"Balance:    {Theme.Currency(BalanceDue)}");
        _review.Text = sb.ToString();
    }

    protected override bool FinishWorkflow()
    {
        var now = Db.Now();
        var customerId = _selectedCustomerId;
        if (customerId <= 0)
        {
            customerId = (int)Db.Insert("""
                INSERT INTO Customers(FirstName, LastName, Phone, Email, Address, BusinessName, Notes, CreatedAt, UpdatedAt)
                VALUES(@first,@last,@phone,@email,@address,@business,@notes,@now,@now)
                """, Db.P("@first", BlankTo(_first.Text, "Walk-in")), Db.P("@last", BlankTo(_last.Text, "Customer")), Db.P("@phone", _phone.Text.Trim()), Db.P("@email", _email.Text.Trim()), Db.P("@address", _address.Text.Trim()), Db.P("@business", _business.Text.Trim()), Db.P("@notes", _customerNotes.Text.Trim()), Db.P("@now", now));
        }
        else
        {
            Db.Execute("""
                UPDATE Customers SET FirstName=@first, LastName=@last, Phone=@phone, Email=@email, Address=@address, BusinessName=@business, Notes=@notes, UpdatedAt=@now
                WHERE Id=@id
                """, Db.P("@first", BlankTo(_first.Text, "Walk-in")), Db.P("@last", BlankTo(_last.Text, "Customer")), Db.P("@phone", _phone.Text.Trim()), Db.P("@email", _email.Text.Trim()), Db.P("@address", _address.Text.Trim()), Db.P("@business", _business.Text.Trim()), Db.P("@notes", _customerNotes.Text.Trim()), Db.P("@now", now), Db.P("@id", customerId));
        }

        var deviceId = Db.Insert("""
            INSERT INTO Devices(CustomerId, DeviceType, Brand, Model, SerialNumber, Imei, PasscodeNotes, ConditionNotes, Accessories, CreatedAt)
            VALUES(@customer,@type,@brand,@model,@serial,@imei,@passcode,@condition,@accessories,@now)
            """, Db.P("@customer", customerId), Db.P("@type", _deviceType.Text), Db.P("@brand", _brand.Text.Trim()), Db.P("@model", _model.Text.Trim()), Db.P("@serial", _serial.Text.Trim()), Db.P("@imei", _imei.Text.Trim()), Db.P("@passcode", _passcode.Text.Trim()), Db.P("@condition", _condition.Text.Trim()), Db.P("@accessories", _accessories.Text.Trim()), Db.P("@now", now));

        var ticketNumber = Db.NextNumber("SC");
        var ticketId = Db.Insert("""
            INSERT INTO Tickets(TicketNumber, CustomerId, DeviceId, ServiceCategory, Priority, Status, Issue, InternalNote, AssignedTo,
                DiagnosticFee, LaborEstimate, PartsEstimate, EstimateTotal, DepositPaid, BalanceDue, CreatedAt, UpdatedAt)
            VALUES(@num,@customer,@device,@category,@priority,'Checked In',@issue,@internal,@assigned,@diag,@labor,@parts,@total,@deposit,@balance,@now,@now)
            """, Db.P("@num", ticketNumber), Db.P("@customer", customerId), Db.P("@device", deviceId), Db.P("@category", _category.Text), Db.P("@priority", _priority.Text), Db.P("@issue", _issue.Text.Trim()), Db.P("@internal", _internalNote.Text.Trim()), Db.P("@assigned", _assigned.Text.Trim()), Db.P("@diag", _diag.Value), Db.P("@labor", _labor.Value), Db.P("@parts", _parts.Value), Db.P("@total", EstimateTotal), Db.P("@deposit", _deposit.Value), Db.P("@balance", BalanceDue), Db.P("@now", now));

        Db.Insert("""
            INSERT INTO TicketNotes(TicketId, Author, NoteType, NoteText, CreatedAt)
            VALUES(@ticket,@author,'Intake',@note,@now)
            """, Db.P("@ticket", ticketId), Db.P("@author", Actor), Db.P("@note", "Ticket created through ServiceCore counter workflow."), Db.P("@now", now));

        if (_deposit.Value > 0)
        {
            Db.Insert("""
                INSERT INTO Payments(CustomerId, TicketId, PaymentType, Method, Amount, CreatedAt, Note)
                VALUES(@customer,@ticket,'Deposit','Counter Payment',@amount,@now,@note)
                """, Db.P("@customer", customerId), Db.P("@ticket", ticketId), Db.P("@amount", _deposit.Value), Db.P("@now", now), Db.P("@note", "Deposit collected during ticket intake."));
        }

        Db.Audit(Actor, "CREATE_SERVICE_TICKET", ticketNumber + " customer=" + customerId);
        Theme.Toast(this, "Service ticket created:\n\n" + ticketNumber);
        return true;
    }

    private static string BlankTo(string text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    }
}
