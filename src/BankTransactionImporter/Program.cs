using BankTransactionImporter;
using BankTransactionImporter.Configuration;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
services.AddSingleton<Application>();

// Build and run
using var serviceProvider = services.BuildServiceProvider();

try
{
    // Validate configuration before running application
    var validationService = serviceProvider.GetRequiredService<IConfigurationValidationService>();
    var validationResult = validationService.ValidateConfiguration(appSettings);

    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

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
