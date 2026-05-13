using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal sealed class DailyCloseWorkflowForm : WorkflowForm
{
    private decimal _cashExpected;
    private decimal _cardTotal;
    private decimal _depositTotal;
    private decimal _salesTotal;

    private readonly Label _systemTotals = Theme.Label("Totals loading...", Theme.HeaderFont, Theme.Accent2);
    private readonly NumericUpDown _cashCounted = Theme.MoneyBox();
    private readonly RichTextBox _notes = Theme.NotesBox("", 120);
    private readonly RichTextBox _review = Theme.NotesBox("", 330);

    public DailyCloseWorkflowForm(string actor) : base("Daily Close", actor)
    {
        AddStep(new ProcessStep
        {
            Title = "Review Totals",
            Instruction = "Review today's local sales and payments before counting the drawer.",
            Content = BuildTotalsStep(),
            OnEnter = LoadTotals
        });
        AddStep(new ProcessStep
        {
            Title = "Count Cash",
            Instruction = "Enter the actual cash counted in the drawer. ServiceCore calculates over/short.",
            Content = BuildCountStep(),
            OnEnter = () => { if (_cashCounted.Value == 0) _cashCounted.Value = _cashExpected; }
        });
        AddStep(new ProcessStep
        {
            Title = "Review / Save Close",
            Instruction = "Review the closeout report, then save the daily close record.",
            Content = BuildReviewStep(),
            OnEnter = BuildReview
        });
        StartWorkflow();
    }

    private Control BuildTotalsStep()
    {
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        _systemTotals.Height = 180;
        _systemTotals.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        card.Controls.Add(_systemTotals);
        return card;
    }

    private Control BuildCountStep()
    {
        var form = Theme.FormTable(220);
        Theme.AddRow(form, "Cash Counted", _cashCounted);
        Theme.AddRow(form, "Close Notes", _notes, 150);
        var card = Theme.Card();
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Controls.Add(form);
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

    private void LoadTotals()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _cashExpected = Money("""
            SELECT IFNULL(SUM(Amount),0) FROM Payments
            WHERE date(CreatedAt)=@date AND Method LIKE 'Cash%'
            """, today);
        _cardTotal = Money("""
            SELECT IFNULL(SUM(Amount),0) FROM Payments
            WHERE date(CreatedAt)=@date AND Method LIKE 'Card%'
            """, today);
        _depositTotal = Money("""
            SELECT IFNULL(SUM(Amount),0) FROM Payments
            WHERE date(CreatedAt)=@date AND PaymentType='Deposit'
            """, today);
        _salesTotal = Money("""
            SELECT IFNULL(SUM(Total),0) FROM Sales
            WHERE date(CreatedAt)=@date
            """, today);
        _systemTotals.Text =
            "Business Date: " + today + "\n\n" +
            "Cash Expected: " + Theme.Currency(_cashExpected) + "\n" +
            "Card / Manual Card: " + Theme.Currency(_cardTotal) + "\n" +
            "Deposits Collected: " + Theme.Currency(_depositTotal) + "\n" +
            "Sales Total: " + Theme.Currency(_salesTotal) + "\n\n" +
            "Note: totals are based on local ServiceCore payment records.";
    }

    private static decimal Money(string sql, string date)
    {
        return Convert.ToDecimal(Db.Scalar(sql, Db.P("@date", date)) ?? 0, CultureInfo.InvariantCulture);
    }

    private void BuildReview()
    {
        var overShort = _cashCounted.Value - _cashExpected;
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine("CJIT SERVICECORE DAILY CLOSE REVIEW");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine("Business Date: " + today);
        sb.AppendLine("Cash Expected: " + Theme.Currency(_cashExpected));
        sb.AppendLine("Cash Counted:  " + Theme.Currency(_cashCounted.Value));
        sb.AppendLine("Over / Short:  " + Theme.Currency(overShort));
        sb.AppendLine("Card Total:    " + Theme.Currency(_cardTotal));
        sb.AppendLine("Deposits:      " + Theme.Currency(_depositTotal));
        sb.AppendLine("Sales Total:   " + Theme.Currency(_salesTotal));
        sb.AppendLine();
        sb.AppendLine("Notes:");
        sb.AppendLine(_notes.Text.Trim());
        _review.Text = sb.ToString();
    }

    protected override bool FinishWorkflow()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var overShort = _cashCounted.Value - _cashExpected;
        Db.Insert("""
            INSERT INTO DailyCloseReports(BusinessDate, CashExpected, CashCounted, OverShort, CardTotal, DepositTotal, SalesTotal, Notes, ClosedBy, CreatedAt)
            VALUES(@date,@cashExpected,@cashCounted,@overShort,@card,@deposit,@sales,@notes,@closedBy,@now)
            """, Db.P("@date", today), Db.P("@cashExpected", _cashExpected), Db.P("@cashCounted", _cashCounted.Value), Db.P("@overShort", overShort), Db.P("@card", _cardTotal), Db.P("@deposit", _depositTotal), Db.P("@sales", _salesTotal), Db.P("@notes", _notes.Text.Trim()), Db.P("@closedBy", Actor), Db.P("@now", Db.Now()));
        Db.Audit(Actor, "DAILY_CLOSE", "Business date " + today + " over/short=" + overShort.ToString(CultureInfo.InvariantCulture));
        Theme.Toast(this, "Daily close saved.\nOver/Short: " + Theme.Currency(overShort));
        return true;
    }
}
