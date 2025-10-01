using System.Globalization;
using BankTransactionImporter.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter.Services;

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly ILogger<GoogleSheetsService> _logger;
    private SheetsService? _sheetsService;
    private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };

    public GoogleSheetsService(ILogger<GoogleSheetsService> logger)
    {
        _logger = logger;
    }

    private async Task InitializeServiceAsync()
    {
        if (_sheetsService != null)
            return;

        try
        {
            // For now, we'll create a minimal mock SheetsService to prevent NullReferenceExceptions
            // when TODO code is enabled. In a real implementation, you would need to set up 
            // OAuth2 or service account credentials
            _logger.LogWarning("Google Sheets service not yet configured with real credentials. Creating mock SheetsService instance to prevent null reference issues.");

            // Create a minimal SheetsService instance without credentials (mock mode)
            // This prevents NullReferenceExceptions when TODO code is uncommented
            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                ApplicationName = "BankTransactionImporter-MockMode"
            });

            // TODO: Implement actual Google Sheets authentication
            // var credential = GoogleCredential.FromFile("path-to-credentials.json")
            //     .CreateScoped(_scopes);
            //
            // _sheetsService = new SheetsService(new BaseClientService.Initializer()
            // {
            //     HttpClientInitializer = credential,
            //     ApplicationName = "Bank Transaction Importer"
            // });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Sheets service");
            throw;
        }
    }

    public async Task<SheetStructure> LoadSheetStructureAsync(string spreadsheetId, string sheetName)
    {
        await InitializeServiceAsync();

        // For now, return a mock structure based on your CSV data
        // In a real implementation, this would read the actual sheet structure
        var structure = CreateMockSheetStructure();

        _logger.LogInformation("Loaded sheet structure for '{SheetName}' with {CategoryCount} categories",
            sheetName, structure.Categories.Count);

        return structure;
    }

    public async Task UpdateCellAsync(string spreadsheetId, string sheetName, int row, int column, decimal value)
    {
        await InitializeServiceAsync();

        var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";
        var formattedValue = value.ToString("F2", CultureInfo.InvariantCulture);

        _logger.LogInformation("Would update cell {Range} with value {Value}", range, formattedValue);

        // TODO: Implement actual Google Sheets update
        // var valueRange = new ValueRange()
        // {
        //     Values = new List<IList<object>> { new List<object> { formattedValue } }
        // };
        //
        // var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        // updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        // await updateRequest.ExecuteAsync();
    }

    public async Task<decimal> GetCellValueAsync(string spreadsheetId, string sheetName, int row, int column)
    {
        await InitializeServiceAsync();

        var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";

        _logger.LogInformation("Would read cell {Range} from spreadsheet {SpreadsheetId}", range, spreadsheetId);

        // TODO: Implement actual Google Sheets read
        // var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
        // var response = await request.ExecuteAsync();
        //
        // if (response.Values?.Count > 0 && response.Values[0]?.Count > 0)
        // {
        //     var cellValue = response.Values[0][0]?.ToString();
        //     if (decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        //     {
        //         return value;
        //     }
        // }

        return 0m;
    }

    public async Task<Dictionary<(int row, int column), decimal>> BatchGetCellValuesAsync(string spreadsheetId, string sheetName, IEnumerable<(int row, int column)> coordinates)
    {
        await InitializeServiceAsync();

        var result = new Dictionary<(int row, int column), decimal>();
        var coordinatesList = coordinates.ToList();

        if (!coordinatesList.Any())
        {
            return result;
        }

        _logger.LogInformation("Would batch read {CellCount} cells from sheet '{SheetName}'", coordinatesList.Count, sheetName);

        // Log the ranges that would be read
        foreach (var (row, column) in coordinatesList)
        {
            var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";
            _logger.LogInformation("  Reading {Range}", range);

            // For mock implementation, return 0 for all cells
            result[(row, column)] = 0m;
        }

        // TODO: Implement actual Google Sheets batch read
        // Create ranges for batch request
        // var ranges = coordinatesList.Select(coord => 
        //     $"{sheetName}!{SheetStructure.IndexToColumnLetter(coord.column)}{coord.row}").ToList();
        //
        // var batchRequest = _sheetsService.Spreadsheets.Values.BatchGet(spreadsheetId);
        // batchRequest.Ranges = ranges;
        // batchRequest.ValueRenderOption = SpreadsheetsResource.ValuesResource.BatchGetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
        //
        // try
        // {
        //     var batchResponse = await batchRequest.ExecuteAsync();
        //
        //     for (int i = 0; i < coordinatesList.Count && i < batchResponse.ValueRanges.Count; i++)
        //     {
        //         var coord = coordinatesList[i];
        //         var valueRange = batchResponse.ValueRanges[i];
        //
        //         decimal cellValue = 0m;
        //         if (valueRange?.Values?.Count > 0 && valueRange.Values[0]?.Count > 0)
        //         {
        //             var rawValue = valueRange.Values[0][0]?.ToString();
        //             if (!string.IsNullOrEmpty(rawValue))
        //             {
        //                 decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out cellValue);
        //             }
        //         }
        //
        //         result[coord] = cellValue;
        //     }
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "Failed to batch read cells from Google Sheets");
        //     
        //     // Fall back to individual reads or return zeros
        //     foreach (var coord in coordinatesList)
        //     {
        //         if (!result.ContainsKey(coord))
        //         {
        //             result[coord] = 0m;
        //         }
        //     }
        // }

        return result;
    }

    public async Task BatchUpdateCellsAsync(string spreadsheetId, string sheetName, Dictionary<(int row, int column), decimal> updates)
    {
        await InitializeServiceAsync();

        _logger.LogInformation("Would batch update {UpdateCount} cells in sheet '{SheetName}'",
            updates.Count, sheetName);

        foreach (var ((row, column), value) in updates)
        {
            var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";
            _logger.LogDebug("  {Range}: {Value}", range, value);
        }

        // TODO: Implement actual batch update
        // var requests = new List<Request>();
        //
        // foreach (var ((row, column), value) in updates)
        // {
        //     // Create batch update requests
        // }
        //
        // var batchUpdateRequest = new BatchUpdateSpreadsheetRequest()
        // {
        //     Requests = requests
        // };
        //
        // var batchUpdate = _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId);
        // await batchUpdate.ExecuteAsync();
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
}