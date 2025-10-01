using BankTransactionImporter.Configuration;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter;

public class Application
{
    private readonly ILogger<Application> _logger;
    private readonly ICsvParser _csvParser;
    private readonly ITransactionMapper _transactionMapper;
    private readonly IGoogleSheetsService _googleSheetsService;
    private readonly AppSettings _settings;

    public Application(
        ILogger<Application> logger,
        ICsvParser csvParser,
        ITransactionMapper transactionMapper,
        IGoogleSheetsService googleSheetsService,
        AppSettings settings)
    {
        _logger = logger;
        _csvParser = csvParser;
        _transactionMapper = transactionMapper;
        _googleSheetsService = googleSheetsService;
        _settings = settings;
    }

    public async Task RunAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("Starting Bank Transaction Importer");

            var options = ParseCommandLineOptions(args);
            await ProcessTransactionsAsync(options);

            _logger.LogInformation("Bank Transaction Importer completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during processing");
            throw;
        }
    }

    private async Task ProcessTransactionsAsync(CommandLineOptions options)
    {
        // Load mapping rules
        var mappingRulesPath = GetMappingRulesPath();

        if (!File.Exists(mappingRulesPath))
        {
            _logger.LogError("Mapping rules file not found at path: {MappingRulesPath}", mappingRulesPath);
            throw new FileNotFoundException($"Mapping rules file not found at path: {mappingRulesPath}");
        }

        try
        {
            _transactionMapper.LoadMappingRules(mappingRulesPath);
            _logger.LogInformation("Successfully loaded mapping rules from {MappingRulesPath}", mappingRulesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mapping rules from {MappingRulesPath}", mappingRulesPath);
            throw new InvalidOperationException($"Failed to load mapping rules from {mappingRulesPath}", ex);
        }

        // Parse transactions from file
        var transactions = await _csvParser.ParseTransactionsAsync(options.FilePath);
        _logger.LogInformation("Loaded {Count} transactions from {FilePath}",
            transactions.Count, options.FilePath);

        if (!transactions.Any())
        {
            _logger.LogWarning("No transactions found in the file");
            return;
        }

        // Load sheet structure
        var sheetName = options.SheetName ?? _settings.GoogleSheets.DefaultSheetName;
        var sheetStructure = await _googleSheetsService.LoadSheetStructureAsync(
            _settings.GoogleSheets.SpreadsheetId, sheetName);

        // Process and map transactions
        var mappedTransactions = new Dictionary<string, List<(Models.Transaction transaction, Models.BudgetCategory category)>>();
        var unmappedTransactions = new List<Models.Transaction>();

        foreach (var transaction in transactions)
        {
            var category = _transactionMapper.MapTransaction(transaction, sheetStructure);
            if (category != null)
            {
                var key = $"{category.Name}_{transaction.Month}";
                if (!mappedTransactions.ContainsKey(key))
                {
                    mappedTransactions[key] = new List<(Models.Transaction, Models.BudgetCategory)>();
                }
                mappedTransactions[key].Add((transaction, category));
            }
            else
            {
                unmappedTransactions.Add(transaction);
            }
        }

        // Display mapping results
        DisplayMappingResults(mappedTransactions, unmappedTransactions);

        // Update spreadsheet if not in dry run mode
        if (!_settings.Processing.DryRun && !options.DryRun)
        {
            await UpdateSpreadsheetAsync(sheetName, mappedTransactions, sheetStructure);
        }
        else
        {
            _logger.LogInformation("Dry run mode enabled. No changes will be made to the spreadsheet.");
        }
    }

    private void DisplayMappingResults(
        Dictionary<string, List<(Models.Transaction transaction, Models.BudgetCategory category)>> mappedTransactions,
        List<Models.Transaction> unmappedTransactions)
    {
        _logger.LogInformation("=== TRANSACTION MAPPING RESULTS ===");

        // Group by category and month
        var categoryTotals = new Dictionary<string, Dictionary<int, decimal>>();

        foreach (var kvp in mappedTransactions)
        {
            var categoryName = kvp.Value.First().category.Name;
            var month = kvp.Value.First().transaction.Month;
            var total = kvp.Value.Sum(x => Math.Abs(x.transaction.Amount));

            if (!categoryTotals.ContainsKey(categoryName))
            {
                categoryTotals[categoryName] = new Dictionary<int, decimal>();
            }
            categoryTotals[categoryName][month] = total;

            _logger.LogInformation("  {Category} ({Month:00}): {Total:C} ({Count} transactions)",
                categoryName, month, total, kvp.Value.Count);
        }

        if (unmappedTransactions.Any())
        {
            _logger.LogWarning("=== UNMAPPED TRANSACTIONS ===");
            foreach (var transaction in unmappedTransactions)
            {
                _logger.LogWarning("  {Date} {Description} {Amount:C} (Ref: {Reference})",
                    transaction.BookingDate.ToString("yyyy-MM-dd"),
                    transaction.Description,
                    transaction.Amount,
                    transaction.Reference);
            }
        }
    }

    private async Task UpdateSpreadsheetAsync(
        string sheetName,
        Dictionary<string, List<(Models.Transaction transaction, Models.BudgetCategory category)>> mappedTransactions,
        Models.SheetStructure sheetStructure)
    {
        _logger.LogInformation("Updating Google Sheets...");

        var updates = new Dictionary<(int row, int column), decimal>();

        // Optimization: Collect all coordinates that need to be read to avoid N sequential API calls
        var coordinatesToRead = new List<(int row, int column)>();
        var transactionData = new List<(int row, int column, decimal total)>();

        foreach (var kvp in mappedTransactions)
        {
            var category = kvp.Value.First().category;
            var month = kvp.Value.First().transaction.Month;
            var total = kvp.Value.Sum(x => Math.Abs(x.transaction.Amount));

            var row = category.RowIndex;
            var column = sheetStructure.GetColumnForMonth(month);

            coordinatesToRead.Add((row, column));
            transactionData.Add((row, column, total));
        }

        // Batch read all current cell values in a single API call instead of individual GetCellValueAsync calls
        var currentValues = await _googleSheetsService.BatchGetCellValuesAsync(
            _settings.GoogleSheets.SpreadsheetId, sheetName, coordinatesToRead);

        // Process updates using the batched values
        foreach (var (row, column, total) in transactionData)
        {
            var currentValue = currentValues.GetValueOrDefault((row, column), 0m);
            updates[(row, column)] = currentValue + total;
        }

        if (updates.Any())
        {
            await _googleSheetsService.BatchUpdateCellsAsync(
                _settings.GoogleSheets.SpreadsheetId, sheetName, updates);

            _logger.LogInformation("Successfully updated {Count} cells", updates.Count);
        }
    }

    private CommandLineOptions ParseCommandLineOptions(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--file" or "-f":
                    if (i + 1 < args.Length)
                        options.FilePath = args[++i];
                    break;
                case "--sheet" or "-s":
                    if (i + 1 < args.Length)
                        options.SheetName = args[++i];
                    break;
                case "--dry-run" or "-d":
                    options.DryRun = true;
                    break;
                case "--help" or "-h":
                    DisplayHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        ValidateOptions(options);
        return options;
    }

    private void ValidateOptions(CommandLineOptions options)
    {
        if (string.IsNullOrEmpty(options.FilePath))
        {
            throw new ArgumentException("File path is required. Use --file or -f to specify the CSV file path.");
        }

        if (!File.Exists(options.FilePath))
        {
            throw new FileNotFoundException($"File not found: {options.FilePath}");
        }
    }

    private void DisplayHelp()
    {
        Console.WriteLine("Bank Transaction Importer");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- --file <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --file, -f <path>     Path to the CSV transaction file (required)");
        Console.WriteLine("  --sheet, -s <name>    Name of the Google Sheets tab (optional)");
        Console.WriteLine("  --dry-run, -d         Preview changes without updating (optional)");
        Console.WriteLine("  --help, -h            Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- --file \"data/transactions.csv\"");
        Console.WriteLine("  dotnet run -- --file \"data/transactions.csv\" --sheet \"2025\" --dry-run");
    }

    private string GetMappingRulesPath()
    {
        // Use configured path if available and not empty
        if (!string.IsNullOrWhiteSpace(_settings.Processing.MappingRulesPath))
        {
            // If the configured path is absolute, use it directly
            if (Path.IsPathRooted(_settings.Processing.MappingRulesPath))
            {
                return _settings.Processing.MappingRulesPath;
            }

            // If relative, combine with application base directory
            return Path.Combine(AppContext.BaseDirectory, _settings.Processing.MappingRulesPath);
        }

        // Fallback to default path in application directory
        _logger.LogWarning("MappingRulesPath not configured, using default path");
        return Path.Combine(AppContext.BaseDirectory, "mapping-rules.json");
    }

    private class CommandLineOptions
    {
        public string FilePath { get; set; } = string.Empty;
        public string? SheetName { get; set; }
        public bool DryRun { get; set; }
    }
}