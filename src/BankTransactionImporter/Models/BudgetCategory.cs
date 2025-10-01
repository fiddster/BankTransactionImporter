namespace BankTransactionImporter.Models;

public class BudgetCategory
{
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty; // "Gemensamma", "Mina egna", "Sparande", etc.
    public int RowIndex { get; set; }
    public CategoryType Type { get; set; }
    public List<string> MappingPatterns { get; set; } = new();

    /// <summary>
    /// Determines if a transaction matches this category based on mapping patterns
    /// </summary>
    public bool MatchesTransaction(Transaction transaction)
    {
        var mappingKey = transaction.MappingKey;

        return MappingPatterns.Any(pattern =>
            mappingKey.Contains(pattern.ToUpper(), StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString()
    {
        return $"{Section} - {Name}";
    }
}

public enum CategoryType
{
    Income,
    SharedExpense,
    PersonalExpense,
    Savings,
    Other
}

public class SheetStructure
{
    public int YearRow { get; set; } = 1;
    public int IncomeRow { get; set; } = 2;
    public int MonthHeaderRow { get; set; } = 3;
    public int FirstDataRow { get; set; } = 4;
    public int MonthStartColumn { get; set; } = 2; // Column B (1-indexed: A=1, B=2, etc.)

    public List<BudgetCategory> Categories { get; set; } = new();

    /// <summary>
    /// Gets the column index for a specific month (1-12)
    /// </summary>
    public int GetColumnForMonth(int month)
    {
        // Months start at column B (index 2) and go through column M (index 13)
        return MonthStartColumn + (month - 1);
    }

    /// <summary>
    /// Gets the Excel column letter for a specific month
    /// </summary>
    public string GetColumnLetterForMonth(int month)
    {
        var columnIndex = GetColumnForMonth(month);
        return IndexToColumnLetter(columnIndex);
    }

    /// <summary>
    /// Converts a column index to Excel column letter (A, B, C, etc.)
    /// </summary>
    public static string IndexToColumnLetter(int columnIndex)
    {
        var columnLetter = "";
        while (columnIndex > 0)
        {
            columnIndex--;
            columnLetter = (char)('A' + columnIndex % 26) + columnLetter;
            columnIndex /= 26;
        }
        return columnLetter;
    }

    /// <summary>
    /// Finds a category by name
    /// </summary>
    public BudgetCategory? FindCategory(string name)
    {
        return Categories.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds the best matching category for a transaction
    /// </summary>
    public BudgetCategory? FindBestMatch(Transaction transaction)
    {
        // Try direct pattern matching first
        var directMatch = Categories.FirstOrDefault(c => c.MatchesTransaction(transaction));
        if (directMatch != null)
            return directMatch;

        // Don't automatically map positive amounts as income - they could be transfers
        // Only explicit income patterns should be mapped to income categories
        return null;
    }
}