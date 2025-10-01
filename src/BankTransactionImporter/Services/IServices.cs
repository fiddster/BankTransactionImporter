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
    Task BatchUpdateCellsAsync(string spreadsheetId, string sheetName, Dictionary<(int row, int column), decimal> updates);
}