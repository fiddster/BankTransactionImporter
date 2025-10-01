using System.Globalization;

namespace BankTransactionImporter.Models;

public class Transaction
{
    public int RowNumber { get; set; }
    public string ClearingNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CurrencyDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BookedBalance { get; set; }

    /// <summary>
    /// Gets the year from the booking date
    /// </summary>
    public int Year => BookingDate.Year;

    /// <summary>
    /// Gets the month from the booking date (1-12)
    /// </summary>
    public int Month => BookingDate.Month;

    /// <summary>
    /// Gets the Swedish month name for the booking date
    /// </summary>
    public string MonthNameSwedish => BookingDate.ToString("MMMM", new CultureInfo("sv-SE"));

    /// <summary>
    /// Gets a simplified key for mapping to budget categories
    /// </summary>
    public string MappingKey => !string.IsNullOrEmpty(Reference) ? Reference.Trim().ToUpper() : Description.Trim().ToUpper();

    /// <summary>
    /// Determines if this is an income transaction (positive amount)
    /// </summary>
    public bool IsIncome => Amount > 0;

    /// <summary>
    /// Determines if this is an expense transaction (negative amount)
    /// </summary>
    public bool IsExpense => Amount < 0;

    /// <summary>
    /// Gets the absolute amount (always positive)
    /// </summary>
    public decimal AbsoluteAmount => Math.Abs(Amount);

    public override string ToString()
    {
        return $"{BookingDate:yyyy-MM-dd} {Description} {Amount:C} ({Reference})";
    }
}