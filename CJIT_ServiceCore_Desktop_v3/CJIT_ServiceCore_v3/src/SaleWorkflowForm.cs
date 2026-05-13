using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class SaleWorkflowForm : WorkflowForm
{
    private sealed class CartLine
    {
        public int InventoryId { get; init; }
        public string Description { get; init; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

    private int _selectedCustomerId;
    private readonly List<CartLine> _cart = new();

    private readonly TextBox _customerSearch = Theme.TextBox();
    private readonly DataGridView _customerGrid = Theme.Grid();
    private readonly Label _customerStatus = Theme.Label("Walk-in sale selected by default.", Theme.NormalFont, Theme.Warning);

    private readonly TextBox _itemSearch = Theme.TextBox();
    private readonly DataGridView _itemGrid = Theme.Grid();
    private readonly NumericUpDown _quantity = Theme.QtyBox(1);
    private readonly DataGridView _cartGrid = Theme.Grid();
    private readonly Label _cartSummary = Theme.Label("Cart: $0.00", Theme.HeaderFont, Theme.Accent2);

    private readonly ComboBox _paymentMethod = Theme.Combo("Cash", "Card - Manual", "Split Payment", "Store Credit", "Other");
    private readonly NumericUpDown _amountPaid = Theme.MoneyBox();
    private readonly Label _paymentSummary = Theme.Label("Total: $0.00", Theme.HeaderFont, Theme.Accent2);
    private readonly RichTextBox _review = Theme.NotesBox("", 330);

    public SaleWorkflowForm(string actor) : base("New Sale", actor)
    {
        AddStep(new ProcessStep
        {
            Title = "Customer",
            Instruction = "Choose an existing customer for history/warranty tracking, or keep it as a walk-in sale.",
            Content = BuildCustomerStep(),
            Validate = () => true
        });
        AddStep(new ProcessStep
        {
            Title = "Cart",
            Instruction = "Search inventory/services, select an item, set quantity, and add it to the cart.",
            Content = BuildCartStep(),
            Validate = ValidateCart
        });
        AddStep(new ProcessStep
        {
            Title = "Payment",
            Instruction = "Choose payment method and enter the amount paid. This is local/manual payment tracking for now.",
            Content = BuildPaymentStep(),
            OnEnter = UpdatePaymentTotals,
            Validate = ValidatePayment
        });
        AddStep(new ProcessStep
        {
            Title = "Receipt / Finish",
            Instruction = "Review the sale one final time, then ServiceCore saves the sale, line items, payment, and inventory deductions.",
            Content = BuildReviewStep(),
            OnEnter = BuildReview
        });

        StartWorkflow();
        LoadCustomers();
        LoadItems();
        RefreshCart();
    }

    private Control BuildCustomerStep()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 3, ColumnCount = 1, Padding = new Padding(2) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        _customerSearch.Width = 450;
        bar.Controls.Add(Theme.Label("Search customer", Theme.NormalFont, Theme.Muted));
        bar.Controls.Add(_customerSearch);
        bar.Controls.Add(Theme.Button("Search", (_, _) => LoadCustomers(), Theme.Panel3));
        bar.Controls.Add(Theme.Button("Use Selected", (_, _) => SelectCustomer(), Theme.Accent));
        bar.Controls.Add(Theme.Button("Walk-in Sale", (_, _) => UseWalkIn(), Theme.Panel3));
        _customerSearch.TextChanged += (_, _) => LoadCustomers();
        _customerGrid.CellDoubleClick += (_, _) => SelectCustomer();
        _customerStatus.Dock = DockStyle.Fill;
        _customerStatus.BackColor = Theme.Panel2;
        _customerStatus.Padding = new Padding(12, 0, 0, 0);
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(_customerGrid, 0, 1);
        root.Controls.Add(_customerStatus, 0, 2);
        return root;
    }

    private Control BuildCartStep()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 4, ColumnCount = 1, Padding = new Padding(2) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));

        var searchBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        _itemSearch.Width = 430;
        searchBar.Controls.Add(Theme.Label("Search item/service", Theme.NormalFont, Theme.Muted));
        searchBar.Controls.Add(_itemSearch);
        searchBar.Controls.Add(Theme.Button("Search", (_, _) => LoadItems(), Theme.Panel3));
        _itemSearch.TextChanged += (_, _) => LoadItems();
        _itemGrid.CellDoubleClick += (_, _) => AddSelectedItem();

        var addBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
        _quantity.Width = 100;
        addBar.Controls.Add(Theme.Label("Quantity", Theme.NormalFont, Theme.Muted));
        addBar.Controls.Add(_quantity);
        addBar.Controls.Add(Theme.Button("Add Selected Item", (_, _) => AddSelectedItem(), Theme.Accent));
        addBar.Controls.Add(Theme.Button("Remove Selected Cart Line", (_, _) => RemoveSelectedCartLine(), Theme.Panel3));
        addBar.Controls.Add(_cartSummary);

        root.Controls.Add(searchBar, 0, 0);
        root.Controls.Add(_itemGrid, 0, 1);
        root.Controls.Add(addBar, 0, 2);
        root.Controls.Add(_cartGrid, 0, 3);
        return root;
    }

    private Control BuildPaymentStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Payment Method", _paymentMethod);
        Theme.AddRow(form, "Amount Paid", _amountPaid);
        Theme.AddRow(form, "Payment Result", _paymentSummary, 64);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
        _amountPaid.ValueChanged += (_, _) => UpdatePaymentTotals();
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

    private void LoadCustomers()
    {
        var q = _customerSearch.Text.Trim();
        Theme.BindGrid(_customerGrid, Db.Query("""
            SELECT Id, FirstName || ' ' || LastName AS Customer, Phone, Email, BusinessName AS Business, UpdatedAt
            FROM Customers
            WHERE @q='' OR FirstName LIKE @like OR LastName LIKE @like OR Phone LIKE @like OR Email LIKE @like OR BusinessName LIKE @like
            ORDER BY Id DESC LIMIT 100
            """, Db.P("@q", q), Db.P("@like", "%" + q + "%")));
    }

    private void LoadItems()
    {
        var q = _itemSearch.Text.Trim();
        Theme.BindGrid(_itemGrid, Db.Query("""
            SELECT Id, Sku, Name, Category, Price, Quantity, BinLocation AS Bin
            FROM InventoryItems
            WHERE IsActive=1 AND (@q='' OR Sku LIKE @like OR Name LIKE @like OR Category LIKE @like)
            ORDER BY Category, Name LIMIT 150
            """, Db.P("@q", q), Db.P("@like", "%" + q + "%")));
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
        _customerStatus.Text = "Selected customer #" + _selectedCustomerId + ": " + Theme.ReadString(row["Customer"]);
    }

    private void UseWalkIn()
    {
        _selectedCustomerId = 0;
        _customerStatus.Text = "Walk-in sale selected. No customer history will be attached.";
    }

    private void AddSelectedItem()
    {
        var row = Theme.SelectedRow(_itemGrid);
        if (row == null)
        {
            Theme.Warn(this, "Select an item/service first.");
            return;
        }
        var id = Convert.ToInt32(row["Id"], CultureInfo.InvariantCulture);
        var price = Theme.ReadDecimal(row["Price"]);
        var name = Theme.ReadString(row["Name"]);
        var existing = _cart.FirstOrDefault(x => x.InventoryId == id && x.UnitPrice == price);
        if (existing != null)
            existing.Quantity += (int)_quantity.Value;
        else
            _cart.Add(new CartLine { InventoryId = id, Description = name, Quantity = (int)_quantity.Value, UnitPrice = price });
        RefreshCart();
    }

    private void RemoveSelectedCartLine()
    {
        var row = Theme.SelectedRow(_cartGrid);
        if (row == null) return;
        var idx = Convert.ToInt32(row["Line"], CultureInfo.InvariantCulture) - 1;
        if (idx >= 0 && idx < _cart.Count) _cart.RemoveAt(idx);
        RefreshCart();
    }

    private void RefreshCart()
    {
        var table = new DataTable();
        table.Columns.Add("Line", typeof(int));
        table.Columns.Add("Item", typeof(string));
        table.Columns.Add("Qty", typeof(int));
        table.Columns.Add("Unit", typeof(decimal));
        table.Columns.Add("Total", typeof(decimal));
        for (var i = 0; i < _cart.Count; i++)
        {
            var line = _cart[i];
            table.Rows.Add(i + 1, line.Description, line.Quantity, line.UnitPrice, line.LineTotal);
        }
        Theme.BindGrid(_cartGrid, table);
        _cartSummary.Text = $"Subtotal: {Theme.Currency(Subtotal)}   Tax: {Theme.Currency(Tax)}   Total: {Theme.Currency(Total)}";
        UpdatePaymentTotals();
    }

    private decimal TaxRate
    {
        get
        {
            return decimal.TryParse(Db.Setting("TaxRate", "0.06"), NumberStyles.Number, CultureInfo.InvariantCulture, out var tax) ? tax : 0.06m;
        }
    }

    private decimal Subtotal => _cart.Sum(x => x.LineTotal);
    private decimal Tax => Math.Round(Subtotal * TaxRate, 2);
    private decimal Total => Subtotal + Tax;

    private bool ValidateCart()
    {
        if (_cart.Count == 0)
        {
            Theme.Warn(this, "Add at least one item or service to the cart.");
            return false;
        }
        return true;
    }

    private void UpdatePaymentTotals()
    {
        if (_amountPaid.Value == 0 && Total > 0) _amountPaid.Value = Total;
        var change = _amountPaid.Value - Total;
        _paymentSummary.Text = $"Total: {Theme.Currency(Total)}   Paid: {Theme.Currency(_amountPaid.Value)}   Change/Difference: {Theme.Currency(change)}";
    }

    private bool ValidatePayment()
    {
        if (_amountPaid.Value < Total)
        {
            Theme.Warn(this, "Amount paid is less than the sale total.");
            return false;
        }
        return true;
    }

    private void BuildReview()
    {
        var sb = new StringBuilder();
        sb.AppendLine("CJIT SERVICECORE SALE REVIEW");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine(_selectedCustomerId > 0 ? $"Customer ID: {_selectedCustomerId}" : "Customer: Walk-in");
        sb.AppendLine();
        foreach (var line in _cart)
        {
            sb.AppendLine($"{line.Quantity} x {line.Description} @ {Theme.Currency(line.UnitPrice)} = {Theme.Currency(line.LineTotal)}");
        }
        sb.AppendLine();
        sb.AppendLine($"Subtotal: {Theme.Currency(Subtotal)}");
        sb.AppendLine($"Tax:      {Theme.Currency(Tax)}");
        sb.AppendLine($"Total:    {Theme.Currency(Total)}");
        sb.AppendLine($"Paid:     {Theme.Currency(_amountPaid.Value)} via {_paymentMethod.Text}");
        _review.Text = sb.ToString();
    }

    protected override bool FinishWorkflow()
    {
        var now = Db.Now();
        var saleNumber = Db.NextNumber("SALE");
        var saleId = Db.Insert("""
            INSERT INTO Sales(SaleNumber, CustomerId, Subtotal, Tax, Total, PaymentMethod, AmountPaid, CreatedAt)
            VALUES(@num,@customer,@subtotal,@tax,@total,@method,@paid,@now)
            """, Db.P("@num", saleNumber), Db.P("@customer", _selectedCustomerId > 0 ? (object)_selectedCustomerId : DBNull.Value), Db.P("@subtotal", Subtotal), Db.P("@tax", Tax), Db.P("@total", Total), Db.P("@method", _paymentMethod.Text), Db.P("@paid", _amountPaid.Value), Db.P("@now", now));

        foreach (var line in _cart)
        {
            Db.Insert("""
                INSERT INTO SaleItems(SaleId, InventoryItemId, Description, Quantity, UnitPrice, LineTotal)
                VALUES(@sale,@item,@desc,@qty,@unit,@line)
                """, Db.P("@sale", saleId), Db.P("@item", line.InventoryId), Db.P("@desc", line.Description), Db.P("@qty", line.Quantity), Db.P("@unit", line.UnitPrice), Db.P("@line", line.LineTotal));
            Db.Execute("""
                UPDATE InventoryItems
                SET Quantity = CASE WHEN Category='Service' THEN Quantity ELSE MAX(Quantity - @qty, 0) END,
                    UpdatedAt=@now
                WHERE Id=@id
                """, Db.P("@qty", line.Quantity), Db.P("@now", now), Db.P("@id", line.InventoryId));
        }

        Db.Insert("""
            INSERT INTO Payments(CustomerId, SaleId, PaymentType, Method, Amount, CreatedAt, Note)
            VALUES(@customer,@sale,'Sale',@method,@amount,@now,@note)
            """, Db.P("@customer", _selectedCustomerId > 0 ? (object)_selectedCustomerId : DBNull.Value), Db.P("@sale", saleId), Db.P("@method", _paymentMethod.Text), Db.P("@amount", _amountPaid.Value), Db.P("@now", now), Db.P("@note", "Payment recorded through new sale workflow."));

        Db.Audit(Actor, "CREATE_SALE", saleNumber + " total=" + Total.ToString(CultureInfo.InvariantCulture));
        Theme.Toast(this, "Sale completed:\n\n" + saleNumber + "\nTotal: " + Theme.Currency(Total));
        return true;
    }
}
