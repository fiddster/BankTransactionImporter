using BankTransactionImporter.Models;

namespace BankTransactionImporter.Services;

public interface ICsvParser
{
    Task<List<Transaction>> ParseTransactionsAsync(string filePath);
    Task<List<Transaction>> ParseTransactionsAsync(Stream stream);
}

public interface ITransactionMapper
{
    void LoadMappingRules(string configPath);
    BudgetCategory? MapTransaction(Transaction transaction, SheetStructure sheetStructure);
    List<BudgetCategory> GetUnmappedCategories(List<Transaction> transactions, SheetStructure sheetStructure);
}

public interface IGoogleSheetsService
{
    Task<SheetStructure> LoadSheetStructureAsync(string spreadsheetId, string sheetName);
    Task UpdateCellAsync(string spreadsheetId, string sheetName, int row, int column, decimal value);
    Task<decimal> GetCellValueAsync(string spreadsheetId, string sheetName, int row, int column);
    /// <summary>
    /// Batch read multiple cell values in a single API call to avoid N sequential requests.
    /// Returns a dictionary keyed by (row, column) coordinates with the decimal values.
    /// </summary>
    Task<Dictionary<(int row, int column), decimal>> BatchGetCellValuesAsync(string spreadsheetId, string sheetName, IEnumerable<(int row, int column)> coordinates);
    Task BatchUpdateCellsAsync(string spreadsheetId, string sheetName, Dictionary<(int row, int column), decimal> updates);
    /// <summary>
    /// Downloads all data from a Google Sheet and returns it as a list of rows, where each row is a list of cell values.
    /// </summary>
    Task<List<List<string>>> GetAllSheetDataAsync(string spreadsheetId, string sheetName);
}