using System.Globalization;
using BankTransactionImporter.Configuration;
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
    private readonly AppSettings _appSettings;
    private readonly SheetsService? _sheetsService;
    private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };

    public GoogleSheetsService(ILogger<GoogleSheetsService> logger, AppSettings appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _sheetsService = InitializeService();
    }

    private SheetsService? InitializeService()
    {
        try
        {
            var credentialsPath = Path.Combine(AppContext.BaseDirectory, "config", "google-credentials.json");

            if (!File.Exists(credentialsPath))
            {
                _logger.LogWarning("Google credentials file not found at: {CredentialsPath}", credentialsPath);
                return null;
            }

            _logger.LogInformation("Initializing Google Sheets service with credentials from: {CredentialsPath}", credentialsPath);

            var credential = GoogleCredential.FromFile(credentialsPath)
                .CreateScoped(_scopes);

            var sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Bank Transaction Importer"
            });

            _logger.LogInformation("Google Sheets service initialized successfully");
            return sheetsService;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Sheets service");
            return null;
        }
    }

    public async Task<SheetStructure> LoadSheetStructureAsync(string spreadsheetId, string sheetName)
    {

        // For now, return a mock structure based on your CSV data
        // In a real implementation, this would read the actual sheet structure
        var structure = CreateMockSheetStructure();

        _logger.LogInformation("Loaded sheet structure for '{SheetName}' with {CategoryCount} categories",
            sheetName, structure.Categories.Count);

        return structure;
    }

    public async Task UpdateCellAsync(string spreadsheetId, string sheetName, int row, int column, decimal value)
    {
        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service not initialized");
            return;
        }

        var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";

        _logger.LogInformation("Updating cell {Range} with numeric value {Value}", range, value);

        try
        {
            var valueRange = new ValueRange()
            {
                Values = new List<IList<object>> { new List<object> { value } }
            };

            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await updateRequest.ExecuteAsync();

            _logger.LogDebug("Successfully updated cell {Range}", range);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cell {Range} with value {Value}", range, value);
            throw;
        }
    }

    public async Task<decimal> GetCellValueAsync(string spreadsheetId, string sheetName, int row, int column)
    {
        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service not initialized");
            return 0m;
        }

        var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";

        _logger.LogInformation("Reading cell {Range} from spreadsheet {SpreadsheetId}", range, spreadsheetId);

        try
        {
            var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            if (response.Values?.Count > 0 && response.Values[0]?.Count > 0)
            {
                var cellValue = response.Values[0][0]?.ToString();
                if (decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read cell {Range} from Google Sheets", range);
        }

        return 0m;
    }

    public async Task<Dictionary<(int row, int column), decimal>> BatchGetCellValuesAsync(string spreadsheetId, string sheetName, IEnumerable<(int row, int column)> coordinates)
    {
        var result = new Dictionary<(int row, int column), decimal>();
        var coordinatesList = coordinates.ToList();

        if (!coordinatesList.Any())
        {
            return result;
        }

        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service not initialized");
            return result;
        }

        _logger.LogInformation("Batch reading {CellCount} cells from sheet '{SheetName}'", coordinatesList.Count, sheetName);

        // Create ranges for batch request
        var ranges = coordinatesList.Select(coord =>
            $"{sheetName}!{SheetStructure.IndexToColumnLetter(coord.column)}{coord.row}").ToList();

        var batchRequest = _sheetsService.Spreadsheets.Values.BatchGet(spreadsheetId);
        batchRequest.Ranges = ranges;
        batchRequest.ValueRenderOption = SpreadsheetsResource.ValuesResource.BatchGetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;

        try
        {
            var batchResponse = await batchRequest.ExecuteAsync();

            for (int i = 0; i < coordinatesList.Count && i < batchResponse.ValueRanges.Count; i++)
            {
                var coord = coordinatesList[i];
                var valueRange = batchResponse.ValueRanges[i];

                decimal cellValue = 0m;
                if (valueRange?.Values?.Count > 0 && valueRange.Values[0]?.Count > 0)
                {
                    var rawValue = valueRange.Values[0][0]?.ToString();
                    if (!string.IsNullOrEmpty(rawValue))
                    {
                        decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out cellValue);
                    }
                }

                result[coord] = cellValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch read cells from Google Sheets");

            // Fall back to individual reads or return zeros
            foreach (var coord in coordinatesList)
            {
                if (!result.ContainsKey(coord))
                {
                    result[coord] = 0m;
                }
            }
        }

        return result;
    }

    public async Task BatchUpdateCellsAsync(string spreadsheetId, string sheetName, Dictionary<(int row, int column), decimal> updates)
    {
        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service not initialized");
            return;
        }

        _logger.LogInformation("Batch updating {UpdateCount} cells in sheet '{SheetName}'",
            updates.Count, sheetName);

        foreach (var ((row, column), value) in updates)
        {
            var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";
            _logger.LogDebug("  {Range}: {Value}", range, value);
        }

        try
        {
            // Use batch values update for better performance
            var data = new List<ValueRange>();

            foreach (var ((row, column), value) in updates)
            {
                var range = $"{sheetName}!{SheetStructure.IndexToColumnLetter(column)}{row}";

                data.Add(new ValueRange
                {
                    Range = range,
                    Values = new List<IList<object>> { new List<object> { value } }
                });
            }

            var batchUpdateRequest = new BatchUpdateValuesRequest
            {
                ValueInputOption = "USER_ENTERED",
                Data = data
            };

            var batchUpdate = _sheetsService.Spreadsheets.Values.BatchUpdate(batchUpdateRequest, spreadsheetId);
            await batchUpdate.ExecuteAsync();

            _logger.LogDebug("Successfully batch updated {UpdateCount} cells", updates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch update cells in Google Sheets");
            throw;
        }
    }

    public async Task<List<List<string>>> GetAllSheetDataAsync(string spreadsheetId, string sheetName)
    {
        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service not initialized");
            return new List<List<string>>();
        }

        try
        {
            _logger.LogInformation("Downloading all data from sheet '{SheetName}'", sheetName);

            // Get the entire sheet data using configurable range
            var dataRange = _appSettings.GoogleSheets.DefaultDataRange;
            var range = $"{sheetName}!{dataRange}";
            var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            var result = new List<List<string>>();

            if (response.Values != null)
            {
                foreach (var row in response.Values)
                {
                    var rowData = new List<string>();
                    if (row != null)
                    {
                        foreach (var cell in row)
                        {
                            rowData.Add(cell?.ToString() ?? "");
                        }
                    }
                    result.Add(rowData);
                }
            }

            _logger.LogInformation("Downloaded {RowCount} rows from sheet '{SheetName}'", result.Count, sheetName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download data from sheet '{SheetName}'", sheetName);
            throw;
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
}