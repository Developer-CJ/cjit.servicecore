using System;
using System.Drawing;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal abstract class ServiceCoreDialog : Form
{
    protected readonly TableLayoutPanel FormTable = Theme.FormTable(170);

    protected ServiceCoreDialog(string title, int width = 620, int height = 560)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(width, height);
        MinimumSize = new Size(width, height);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.NormalFont;
        KeyPreview = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(16) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        var header = Theme.Label(title, Theme.HeaderFont, Theme.Text, ContentAlignment.MiddleLeft);
        var card = Theme.Card(16);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(FormTable);
        card.Controls.Add(scroll);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var save = Theme.Button("Save", (_, _) => SaveClick(), Theme.Accent);
        var cancel = Theme.Button("Cancel", (_, _) => Close(), Theme.Panel3);
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(card, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
        };
    }

    protected abstract void SaveClick();
}

internal sealed class CustomerDialog : ServiceCoreDialog
{
    private readonly TextBox _first = Theme.TextBox();
    private readonly TextBox _last = Theme.TextBox();
    private readonly TextBox _phone = Theme.TextBox();
    private readonly TextBox _email = Theme.TextBox();
    private readonly TextBox _address = Theme.TextBox();
    private readonly TextBox _business = Theme.TextBox();
    private readonly RichTextBox _notes = Theme.NotesBox("", 90);

    public string FirstName => _first.Text.Trim();
    public string LastName => _last.Text.Trim();
    public string Phone => _phone.Text.Trim();
    public string Email => _email.Text.Trim();
    public string Address => _address.Text.Trim();
    public string Business => _business.Text.Trim();
    public string Notes => _notes.Text.Trim();

    public CustomerDialog() : base("Add Customer")
    {
        Theme.AddRow(FormTable, "First Name", _first);
        Theme.AddRow(FormTable, "Last Name", _last);
        Theme.AddRow(FormTable, "Phone", _phone);
        Theme.AddRow(FormTable, "Email", _email);
        Theme.AddRow(FormTable, "Address", _address);
        Theme.AddRow(FormTable, "Business", _business);
        Theme.AddRow(FormTable, "Notes", _notes, 112);
    }

    protected override void SaveClick()
    {
        if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
        {
            Theme.Warn(this, "Enter at least a first or last name.");
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class InventoryDialog : ServiceCoreDialog
{
    private readonly TextBox _sku = Theme.TextBox("ITEM-" + DateTime.Now.ToString("HHmmss"));
    private readonly TextBox _name = Theme.TextBox();
    private readonly ComboBox _category = Theme.Combo("Accessory", "Phone Part", "Computer Part", "Network Gear", "Used Device", "Service", "Other");
    private readonly NumericUpDown _cost = Theme.MoneyBox();
    private readonly NumericUpDown _price = Theme.MoneyBox();
    private readonly NumericUpDown _qty = Theme.QtyBox(1);
    private readonly NumericUpDown _reorder = Theme.QtyBox(1);
    private readonly TextBox _bin = Theme.TextBox();
    private readonly RichTextBox _notes = Theme.NotesBox("", 80);

    public string Sku => _sku.Text.Trim();
    public string ItemName => _name.Text.Trim();
    public string Category => Convert.ToString(_category.SelectedItem) ?? "Other";
    public decimal Cost => _cost.Value;
    public decimal Price => _price.Value;
    public int Quantity => (int)_qty.Value;
    public int Reorder => (int)_reorder.Value;
    public string Bin => _bin.Text.Trim();
    public string Notes => _notes.Text.Trim();

    public InventoryDialog() : base("Add Inventory Item / Service", 680, 650)
    {
        Theme.AddRow(FormTable, "SKU", _sku);
        Theme.AddRow(FormTable, "Name", _name);
        Theme.AddRow(FormTable, "Category", _category);
        Theme.AddRow(FormTable, "Cost", _cost);
        Theme.AddRow(FormTable, "Price", _price);
        Theme.AddRow(FormTable, "Quantity", _qty);
        Theme.AddRow(FormTable, "Reorder Level", _reorder);
        Theme.AddRow(FormTable, "Bin Location", _bin);
        Theme.AddRow(FormTable, "Notes", _notes, 102);
    }

    protected override void SaveClick()
    {
        if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(ItemName))
        {
            Theme.Warn(this, "SKU and item name are required.");
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class StatusDialog : ServiceCoreDialog
{
    private readonly ComboBox _status = Theme.Combo("Checked In", "Waiting Diagnosis", "Diagnosed", "Waiting Customer Approval", "Waiting Parts", "In Progress", "Ready for Pickup", "Completed", "Cancelled", "Warranty Return");
    public string SelectedStatus => Convert.ToString(_status.SelectedItem) ?? "Checked In";

    public StatusDialog() : base("Change Ticket Status", 520, 300)
    {
        Theme.AddRow(FormTable, "New Status", _status);
    }

    protected override void SaveClick()
    {
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class NoteDialog : ServiceCoreDialog
{
    private readonly RichTextBox _note = Theme.NotesBox("", 170);
    public string NoteText => _note.Text.Trim();

    public NoteDialog(string title) : base(title, 640, 420)
    {
        Theme.AddRow(FormTable, "Note", _note, 200);
    }

    protected override void SaveClick()
    {
        if (string.IsNullOrWhiteSpace(NoteText))
        {
            Theme.Warn(this, "Enter a note first.");
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
