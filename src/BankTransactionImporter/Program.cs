using System.Text;
using BankTransactionImporter;
using BankTransactionImporter.Configuration;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Register encoding providers for better Swedish character support
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Build configuration using a robust path resolution strategy
// 1. Use AppContext.BaseDirectory (executable location) as the primary base path
// 2. Allow config file override via BANKTRANSACTIONIMPORTER_CONFIG environment variable
// 3. Fall back to development path structure for local debugging
var basePath = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();

// Allow config file override via environment variable (useful for deployments)
var configFileName = Environment.GetEnvironmentVariable("BANKTRANSACTIONIMPORTER_CONFIG") ?? "appsettings.json";
var configPath = Path.GetFullPath(Path.Combine(basePath, configFileName));

// Check if config file exists, if not, try in project directory during development
if (!File.Exists(configPath))
{
    var developmentConfigPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "src", "BankTransactionImporter", configFileName));
    if (File.Exists(developmentConfigPath))
    {
        configPath = developmentConfigPath;
    }
    else
    {
        throw new FileNotFoundException($"Configuration file not found. Searched in: '{configPath}' and '{developmentConfigPath}'. Set BANKTRANSACTIONIMPORTER_CONFIG environment variable to specify a custom path.");
    }
}

var configDirectory = Path.GetDirectoryName(configPath) ?? basePath;
var configuration = new ConfigurationBuilder()
    .SetBasePath(configDirectory)
    .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: true)
    .Build();

// Build service provider
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddConfiguration(configuration.GetSection("Logging"));
});

// Add configuration
var appSettings = new AppSettings();
configuration.Bind(appSettings);
services.AddSingleton(appSettings);

// Add services
services.AddSingleton<IConfigurationValidationService, ConfigurationValidationService>();
services.AddSingleton<ICsvParser, CsvParser>();
services.AddSingleton<ITransactionMapper, TransactionMapper>();
services.AddSingleton<IGoogleSheetsService, GoogleSheetsService>();
services.AddSingleton<SheetBackupUtility>();
services.AddSingleton<Application>();

// Build and run
using var serviceProvider = services.BuildServiceProvider();

try
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Check for command-line commands (only if first arg doesn't start with --)
    if (args.Length > 0 && !args[0].StartsWith("--"))
    {
        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "test-connection":
                await GoogleSheetsConnectionTester.TestConnectionAsync();
                return 0;

            case "backup":
                var backupUtility = serviceProvider.GetRequiredService<SheetBackupUtility>();
                var spreadsheetId = appSettings.GoogleSheets.SpreadsheetId;
                var sheetName = appSettings.GoogleSheets.DefaultSheetName;

                if (string.IsNullOrEmpty(spreadsheetId) || spreadsheetId == "your-google-sheets-id-here")
                {
                    Console.WriteLine("❌ Error: Please configure SpreadsheetId in appsettings.json");
                    return 1;
                }

                // Check for custom backup path
                string? customPath = null;
                if (args.Length > 1)
                {
                    customPath = args[1];
                    if (customPath.StartsWith("--path="))
                    {
                        customPath = customPath.Substring("--path=".Length);
                    }
                    else if (customPath == "--path" && args.Length > 2)
                    {
                        customPath = args[2];
                    }

                    // Expand environment variables and relative paths
                    customPath = Environment.ExpandEnvironmentVariables(customPath);
                    if (!Path.IsPathRooted(customPath))
                    {
                        customPath = Path.GetFullPath(customPath);
                    }

                    Console.WriteLine($"📥 Creating backup of Google Sheets data to: {customPath}");
                }
                else
                {
                    Console.WriteLine("📥 Creating backup of Google Sheets data...");
                }

                await backupUtility.CreateBackupAsync(spreadsheetId, sheetName, customPath);
                return 0;

            case "list-backups":
                var listBackupUtility = serviceProvider.GetRequiredService<SheetBackupUtility>();
                await listBackupUtility.ListBackupsAsync();
                return 0;

            case "help":
                ShowHelp();
                return 0;

            default:
                Console.WriteLine($"❌ Unknown command: {args[0]}");
                ShowHelp();
                return 1;
        }
    }

    // Validate configuration before running application
    var validationService = serviceProvider.GetRequiredService<IConfigurationValidationService>();
    var validationResult = validationService.ValidateConfiguration(appSettings);

    foreach (var warning in validationResult.Warnings)
    {
        logger.LogWarning("Configuration warning: {Warning}", warning);
    }

    if (!validationResult.IsValid)
    {
        logger.LogError("Configuration validation failed:");
        foreach (var error in validationResult.Errors)
        {
            logger.LogError("  - {Error}", error);
        }
        return 1;
    }

    logger.LogInformation("Configuration validation passed successfully");

    var app = serviceProvider.GetRequiredService<Application>();
    await app.RunAsync(args);
}
catch (Exception ex)
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Application terminated unexpectedly");
    return 1;
}

return 0;

static void ShowHelp()
{
    Console.WriteLine("🏦 Bank Transaction Importer");
    Console.WriteLine("============================");
    Console.WriteLine();
    Console.WriteLine("Usage: BankTransactionImporter [command] [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  (no args)                    Process bank transactions and update Google Sheets");
    Console.WriteLine("  test-connection              Test connection to Google Sheets");
    Console.WriteLine("  backup [path]                Create a backup of your Google Sheets data");
    Console.WriteLine("  list-backups                 List all available backups");
    Console.WriteLine("  help                         Show this help message");
    Console.WriteLine();
    Console.WriteLine("Backup Options:");
    Console.WriteLine("  backup                       Create backup in default location (./backups/)");
    Console.WriteLine("  backup /path/to/folder       Create backup in specified folder");
    Console.WriteLine("  backup /path/to/filename     Create backup with custom filename");
    Console.WriteLine("  backup --path=/custom/path   Create backup using --path flag");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run                                    # Process transactions");
    Console.WriteLine("  dotnet run test-connection                    # Test Google Sheets connection");
    Console.WriteLine("  dotnet run backup                            # Create backup (default location)");
    Console.WriteLine("  dotnet run backup C:\\MyBackups               # Backup to custom folder");
    Console.WriteLine("  dotnet run backup %USERPROFILE%\\Documents   # Backup to Documents folder");
    Console.WriteLine("  dotnet run backup my-backup                  # Custom filename in current dir");
    Console.WriteLine("  dotnet run list-backups                      # Show available backups");
    Console.WriteLine();
    Console.WriteLine("Configuration:");
    Console.WriteLine("  appsettings.json → Backup.DefaultPath    Set permanent default backup location");
    Console.WriteLine();
    Console.WriteLine("Environment Variables:");
    Console.WriteLine("  BACKUP_PATH                              Override all backup path settings");
    Console.WriteLine();
    Console.WriteLine("Priority Order (highest to lowest):");
    Console.WriteLine("  1. Command line path argument");
    Console.WriteLine("  2. BACKUP_PATH environment variable");
    Console.WriteLine("  3. Backup.DefaultPath in appsettings.json");
    Console.WriteLine("  4. Built-in default (./backups/)");
    Console.WriteLine();
}
