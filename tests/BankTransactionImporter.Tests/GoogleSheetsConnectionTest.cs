using BankTransactionImporter.Configuration;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace BankTransactionImporter.Tests;

public class GoogleSheetsConnectionTest
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<GoogleSheetsService> _logger;

    public GoogleSheetsConnectionTest(ITestOutputHelper output)
    {
        _output = output;
        _logger = new NullLogger<GoogleSheetsService>();
    }

    [Fact]
    public async Task CanConnectToGoogleSheets()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            GoogleSheets = new GoogleSheetsConfig
            {
                SpreadsheetId = "your-google-sheets-id-here",
                DefaultSheetName = "2025",
                CredentialsPath = "config/google-credentials.json",
                DefaultDataRange = "A1:Z1000"
            }
        };
        var service = new GoogleSheetsService(_logger, appSettings);
        const string spreadsheetId = "your-google-sheets-id-here"; // Replace with actual ID for testing
        const string sheetName = "2025";

        try
        {
            _output.WriteLine("Testing Google Sheets connection...");

            // Act - Try to load the sheet structure
            var structure = await service.LoadSheetStructureAsync(spreadsheetId, sheetName);

            // Assert
            Assert.NotNull(structure);
            Assert.NotEmpty(structure.Categories);

            _output.WriteLine($"✅ Successfully connected to Google Sheets!");
            _output.WriteLine($"   Found {structure.Categories.Count} categories in the structure");

            // Try to read a simple cell (A1) to verify read access
            _output.WriteLine("Testing cell read access...");
            var cellValue = await service.GetCellValueAsync(spreadsheetId, sheetName, 1, 1);

            _output.WriteLine($"✅ Successfully read cell A1: {cellValue}");
            _output.WriteLine("Google Sheets connection test PASSED! 🎉");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Google Sheets connection test FAILED: {ex.Message}");

            if (ex.InnerException != null)
            {
                _output.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }

            // Print some helpful troubleshooting info
            _output.WriteLine("\n🔧 Troubleshooting tips:");
            _output.WriteLine("1. Make sure the service account email has been shared with your Google Sheet");
            _output.WriteLine("   (Check the credentials file for the service account email)");
            _output.WriteLine("2. Verify the spreadsheet ID in appsettings.json is correct");
            _output.WriteLine($"   Current ID: {spreadsheetId}");
            _output.WriteLine("3. Check that the credentials file exists in the config folder");
            _output.WriteLine("4. Ensure the sheet name '2025' exists in your spreadsheet");

            throw; // Re-throw to fail the test
        }
    }
}