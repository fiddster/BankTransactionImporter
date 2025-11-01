using BankTransactionImporter.Configuration;
using BankTransactionImporter.Models;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter;

public static class GoogleSheetsConnectionTester
{
    public static async Task TestConnectionAsync()
    {
        Console.WriteLine("🔍 Testing Google Sheets Connection...");
        Console.WriteLine("=====================================");

        // Set up logging
        using var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<GoogleSheetsService>>();

        try
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            var spreadsheetId = appSettings.GoogleSheets.SpreadsheetId;
            var sheetName = appSettings.GoogleSheets.DefaultSheetName ?? "2025";

            if (string.IsNullOrEmpty(spreadsheetId) || spreadsheetId == "your-google-sheets-id-here")
            {
                Console.WriteLine("❌ ERROR: Please update the SpreadsheetId in appsettings.json");
                return;
            }

            Console.WriteLine($"📋 Spreadsheet ID: {spreadsheetId}");
            Console.WriteLine($"📄 Sheet Name: {sheetName}");
            Console.WriteLine();

            // Test the service
            var service = new GoogleSheetsService(logger, appSettings);

            Console.WriteLine("⏳ Step 1: Loading sheet structure...");
            var structure = await service.LoadSheetStructureAsync(spreadsheetId, sheetName);
            Console.WriteLine($"✅ Success! Found {structure.Categories.Count} categories");
            Console.WriteLine();

            Console.WriteLine("⏳ Step 2: Testing cell read access (A1)...");
            var cellValue = await service.GetCellValueAsync(spreadsheetId, sheetName, 1, 1);
            Console.WriteLine($"✅ Success! Cell A1 value: {cellValue}");
            Console.WriteLine();

            Console.WriteLine("⏳ Step 3: Testing batch read access...");
            var coordinates = new List<(int row, int column)>
            {
                (1, 1), // A1
                (1, 2), // B1
                (2, 1)  // A2
            };

            var batchValues = await service.BatchGetCellValuesAsync(spreadsheetId, sheetName, coordinates);
            Console.WriteLine($"✅ Success! Read {batchValues.Count} cells in batch");

            foreach (var ((row, col), value) in batchValues)
            {
                var cellRef = $"{SheetStructure.IndexToColumnLetter(col)}{row}";
                Console.WriteLine($"   📍 {cellRef}: {value}");
            }

            Console.WriteLine();
            Console.WriteLine("🎉 ALL TESTS PASSED!");
            Console.WriteLine("Your Google Sheets integration is working correctly! ✨");
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("google-credentials.json"))
        {
            Console.WriteLine("❌ ERROR: Google credentials file not found!");
            Console.WriteLine("Make sure google-credentials.json is in the config/ folder.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("🔧 Troubleshooting checklist:");
            Console.WriteLine("1. ✉️  Share your Google Sheet with the service account (check credentials file for email)");
            Console.WriteLine("2. 🔑 Ensure google-credentials.json is in the config/ folder");
            Console.WriteLine("3. 📋 Verify the spreadsheet ID in appsettings.json is correct");
            Console.WriteLine("4. 📄 Check that the sheet name '2025' exists in your spreadsheet");
            Console.WriteLine("5. 🌐 Verify you have internet access");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner exception: {ex.InnerException.Message}");
            }
        }
    }

}