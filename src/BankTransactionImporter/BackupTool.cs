using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter;

public static class BackupTool
{
    public static async Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("🗂️  Google Sheets Backup Tool");
        Console.WriteLine("==============================");

        if (args.Length > 0 && args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
        {
            ShowUsage();
            return 0;
        }

        try
        {
            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var spreadsheetId = configuration["GoogleSheets:SpreadsheetId"];
            var sheetName = configuration["GoogleSheets:DefaultSheetName"] ?? "2025";

            if (string.IsNullOrEmpty(spreadsheetId) || spreadsheetId == "your-google-sheets-id-here")
            {
                Console.WriteLine("❌ Error: Please configure SpreadsheetId in appsettings.json");
                return 1;
            }

            // Setup services
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            services.AddSingleton<IGoogleSheetsService, GoogleSheetsService>();
            services.AddSingleton<SheetBackupUtility>();

            using var serviceProvider = services.BuildServiceProvider();
            var backupUtility = serviceProvider.GetRequiredService<SheetBackupUtility>();

            // Parse command line arguments
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "list":
                        await backupUtility.ListBackupsAsync();
                        break;

                    case "create":
                        string? customPath = args.Length > 1 ? args[1] : null;
                        Console.WriteLine($"📥 Creating backup of sheet '{sheetName}'...");
                        await backupUtility.CreateBackupAsync(spreadsheetId, sheetName, customPath);
                        break;

                    default:
                        Console.WriteLine($"❌ Unknown command: {args[0]}");
                        ShowUsage();
                        return 1;
                }
            }
            else
            {
                // Default action: create backup
                Console.WriteLine($"📥 Creating backup of sheet '{sheetName}'...");
                await backupUtility.CreateBackupAsync(spreadsheetId, sheetName);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Details: {ex.InnerException.Message}");
            }

            Console.WriteLine("\n🔧 Troubleshooting:");
            Console.WriteLine("1. Make sure appsettings.json exists and contains valid SpreadsheetId");
            Console.WriteLine("2. Ensure google-credentials.json is in the config/ folder");
            Console.WriteLine("3. Verify your service account has access to the Google Sheet");
            Console.WriteLine("4. Check your internet connection");

            return 1;
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage: BackupTool [command] [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  (no args)           Create a backup with automatic filename");
        Console.WriteLine("  create [path]       Create a backup, optionally specify custom path");
        Console.WriteLine("  list                List all available backups");
        Console.WriteLine("  --help              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  BackupTool                           # Create backup");
        Console.WriteLine("  BackupTool create                    # Create backup");
        Console.WriteLine("  BackupTool create my-backup         # Create backup with custom name");
        Console.WriteLine("  BackupTool list                      # List backups");
        Console.WriteLine();
        Console.WriteLine("Output formats:");
        Console.WriteLine("  - CSV:  Comma-separated values for spreadsheet import");
        Console.WriteLine("  - JSON: Structured data with metadata");
        Console.WriteLine("  - Text: Human-readable formatted text");
        Console.WriteLine();
    }
}