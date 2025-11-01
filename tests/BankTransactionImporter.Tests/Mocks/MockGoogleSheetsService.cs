using BankTransactionImporter.Models;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BankTransactionImporter.Tests.Mocks;

public class MockGoogleSheetsService : IGoogleSheetsService
{
    private readonly ILogger<MockGoogleSheetsService> _logger;
    private readonly Dictionary<string, MockSpreadsheet> _spreadsheets = new();

    public MockGoogleSheetsService(ILogger<MockGoogleSheetsService> logger)
    {
        _logger = logger;
    }

    public void SetupMockSpreadsheet(string spreadsheetId, string sheetName, Dictionary<(int row, int column), decimal> initialData)
    {
        if (!_spreadsheets.ContainsKey(spreadsheetId))
        {
            _spreadsheets[spreadsheetId] = new MockSpreadsheet();
        }

        if (!_spreadsheets[spreadsheetId].Sheets.ContainsKey(sheetName))
        {
            _spreadsheets[spreadsheetId].Sheets[sheetName] = new MockSheet();
        }

        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];
        foreach (var ((row, column), value) in initialData)
        {
            sheet.Cells[(row, column)] = value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    public async Task<SheetStructure> LoadSheetStructureAsync(string spreadsheetId, string sheetName)
    {
        _logger.LogInformation("Loading mock sheet structure for '{SheetName}' in spreadsheet '{SpreadsheetId}'", sheetName, spreadsheetId);
        
        await Task.Delay(10); // Simulate network delay

        // Return the mock structure similar to the real implementation
        var structure = CreateMockSheetStructure();
        _logger.LogInformation("Loaded mock sheet structure with {CategoryCount} categories", structure.Categories.Count);
        
        return structure;
    }

    public async Task UpdateCellAsync(string spreadsheetId, string sheetName, int row, int column, decimal value)
    {
        _logger.LogInformation("Mock updating cell {SheetName}!{Column}{Row} with value {Value}", 
            sheetName, SheetStructure.IndexToColumnLetter(column), row, value);

        await Task.Delay(5); // Simulate network delay

        EnsureSpreadsheetExists(spreadsheetId, sheetName);
        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];
        sheet.Cells[(row, column)] = value.ToString("F2", CultureInfo.InvariantCulture);
    }

    public async Task<decimal> GetCellValueAsync(string spreadsheetId, string sheetName, int row, int column)
    {
        _logger.LogInformation("Mock reading cell {SheetName}!{Column}{Row}", 
            sheetName, SheetStructure.IndexToColumnLetter(column), row);

        await Task.Delay(5); // Simulate network delay

        if (!_spreadsheets.ContainsKey(spreadsheetId) || 
            !_spreadsheets[spreadsheetId].Sheets.ContainsKey(sheetName))
        {
            return 0m;
        }

        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];
        if (sheet.Cells.TryGetValue((row, column), out var cellValue) &&
            decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return 0m;
    }

    public async Task<Dictionary<(int row, int column), decimal>> BatchGetCellValuesAsync(
        string spreadsheetId, 
        string sheetName, 
        IEnumerable<(int row, int column)> coordinates)
    {
        var coordinatesList = coordinates.ToList();
        _logger.LogInformation("Mock batch reading {CellCount} cells from sheet '{SheetName}'", 
            coordinatesList.Count, sheetName);

        await Task.Delay(20); // Simulate network delay for batch operation

        var result = new Dictionary<(int row, int column), decimal>();

        if (!_spreadsheets.ContainsKey(spreadsheetId) || 
            !_spreadsheets[spreadsheetId].Sheets.ContainsKey(sheetName))
        {
            // Return zeros for all coordinates if sheet doesn't exist
            foreach (var coord in coordinatesList)
            {
                result[coord] = 0m;
            }
            return result;
        }

        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];
        foreach (var coord in coordinatesList)
        {
            decimal value = 0m;
            if (sheet.Cells.TryGetValue(coord, out var cellValue) &&
                decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                value = parsedValue;
            }
            result[coord] = value;
        }

        return result;
    }

    public async Task BatchUpdateCellsAsync(
        string spreadsheetId, 
        string sheetName, 
        Dictionary<(int row, int column), decimal> updates)
    {
        _logger.LogInformation("Mock batch updating {UpdateCount} cells in sheet '{SheetName}'",
            updates.Count, sheetName);

        await Task.Delay(25); // Simulate network delay for batch operation

        EnsureSpreadsheetExists(spreadsheetId, sheetName);
        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];

        foreach (var ((row, column), value) in updates)
        {
            sheet.Cells[(row, column)] = value.ToString("F2", CultureInfo.InvariantCulture);
            _logger.LogDebug("Mock updated {Column}{Row}: {Value}", 
                SheetStructure.IndexToColumnLetter(column), row, value);
        }
    }

    public Dictionary<(int row, int column), decimal> GetAllCellValues(string spreadsheetId, string sheetName)
    {
        var result = new Dictionary<(int row, int column), decimal>();
        
        if (!_spreadsheets.ContainsKey(spreadsheetId) || 
            !_spreadsheets[spreadsheetId].Sheets.ContainsKey(sheetName))
        {
            return result;
        }

        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];
        foreach (var (coord, cellValue) in sheet.Cells)
        {
            if (decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                result[coord] = value;
            }
        }

        return result;
    }

    public async Task<List<List<string>>> GetAllSheetDataAsync(string spreadsheetId, string sheetName)
    {
        _logger.LogInformation("Mock reading all data from sheet '{SheetName}' in spreadsheet '{SpreadsheetId}'", sheetName, spreadsheetId);
        
        await Task.Delay(50); // Simulate network delay for large data download
        
        var result = new List<List<string>>();
        
        if (!_spreadsheets.ContainsKey(spreadsheetId) || 
            !_spreadsheets[spreadsheetId].Sheets.ContainsKey(sheetName))
        {
            return result; // Return empty list if sheet doesn't exist
        }

        var sheet = _spreadsheets[spreadsheetId].Sheets[sheetName];
        
        // Find the maximum row and column to determine sheet bounds
        int maxRow = 0, maxCol = 0;
        foreach (var ((row, col), _) in sheet.Cells)
        {
            maxRow = Math.Max(maxRow, row);
            maxCol = Math.Max(maxCol, col);
        }
        
        // Build the 2D list structure
        for (int row = 1; row <= maxRow; row++)
        {
            var rowData = new List<string>();
            for (int col = 1; col <= maxCol; col++)
            {
                var cellValue = sheet.Cells.TryGetValue((row, col), out var value) ? value : "";
                rowData.Add(cellValue);
            }
            result.Add(rowData);
        }
        
        return result;
    }

    public void SimulateApiError(string spreadsheetId, string sheetName, string errorMessage)
    {
        if (!_spreadsheets.ContainsKey(spreadsheetId))
        {
            _spreadsheets[spreadsheetId] = new MockSpreadsheet();
        }

        _spreadsheets[spreadsheetId].ErrorMessage = errorMessage;
    }

    private void EnsureSpreadsheetExists(string spreadsheetId, string sheetName)
    {
        if (!_spreadsheets.ContainsKey(spreadsheetId))
        {
            _spreadsheets[spreadsheetId] = new MockSpreadsheet();
        }

        if (!_spreadsheets[spreadsheetId].Sheets.ContainsKey(sheetName))
        {
            _spreadsheets[spreadsheetId].Sheets[sheetName] = new MockSheet();
        }

        // Check for simulated errors
        if (!string.IsNullOrEmpty(_spreadsheets[spreadsheetId].ErrorMessage))
        {
            throw new InvalidOperationException($"Mock Google Sheets API Error: {_spreadsheets[spreadsheetId].ErrorMessage}");
        }
    }

    private SheetStructure CreateMockSheetStructure()
    {
        var structure = new SheetStructure();

        // Define categories based on your CSV structure
        var categories = new List<BudgetCategory>
        {
            // Income
            new() { Name = "Inkomst", Section = "Income", RowIndex = 2, Type = CategoryType.Income,
                   MappingPatterns = new() { "LÖN", "L�N", "LON", "LOEN", "SALARY" } },
            
            // Shared expenses (Gemensamma)
            new() { Name = "Hyra", Section = "Gemensamma", RowIndex = 4, Type = CategoryType.SharedExpense,
                   MappingPatterns = new() { "HALMSTADS FASTIG", "HYRA", "RENT" } },
            new() { Name = "Tele2", Section = "Gemensamma", RowIndex = 5, Type = CategoryType.SharedExpense,
                   MappingPatterns = new() { "TELE2" } },
            new() { Name = "Netflix", Section = "Gemensamma", RowIndex = 7, Type = CategoryType.SharedExpense,
                   MappingPatterns = new() { "NETFLIX" } },
            new() { Name = "Billån", Section = "Gemensamma", RowIndex = 9, Type = CategoryType.SharedExpense,
                   MappingPatterns = new() { "MOTORHALLAND FIN", "BILLÅN", "BILLAN" } },
            new() { Name = "IF Skadeförsäkring", Section = "Gemensamma", RowIndex = 11, Type = CategoryType.SharedExpense,
                   MappingPatterns = new() { "IF SKADEFÖRS", "IF SKADEF" } },
            
            // Personal expenses (Mina egna)
            new() { Name = "Spotify", Section = "Mina egna", RowIndex = 16, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "SPOTIFY" } },
            new() { Name = "Bliwa Sjuk & Olycksförsäkring", Section = "Mina egna", RowIndex = 17, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "BLIWA" } },
            new() { Name = "Comviq mobil", Section = "Mina egna", RowIndex = 18, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "COMVIQ" } },
            new() { Name = "Playstation+", Section = "Mina egna", RowIndex = 19, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "PLAYSTATION" } },
            new() { Name = "A-kassa", Section = "Mina egna", RowIndex = 20, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "UNION AKASSA", "A-KASSA", "AKASSA" } },
            new() { Name = "Fackavgift", Section = "Mina egna", RowIndex = 21, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "UNIONEN", "FACK" } },
            new() { Name = "CSN", Section = "Mina egna", RowIndex = 22, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "CSN" } },
            new() { Name = "Bäckamot", Section = "Mina egna", RowIndex = 24, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "BÄCKAMOT", "BACKAMOT" } },
            new() { Name = "Mat", Section = "Mina egna", RowIndex = 25, Type = CategoryType.PersonalExpense,
                   MappingPatterns = new() { "MAT", "FOOD", "ICA", "COOP", "WILLYS" } },
            
            // Savings
            new() { Name = "Kontant", Section = "Sparande", RowIndex = 32, Type = CategoryType.Savings,
                   MappingPatterns = new() { "SPARANDE", "SAVING" } }
        };

        structure.Categories = categories;
        return structure;
    }

    private class MockSpreadsheet
    {
        public Dictionary<string, MockSheet> Sheets { get; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    private class MockSheet
    {
        public Dictionary<(int row, int column), string> Cells { get; } = new();
    }
}