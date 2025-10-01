using BankTransactionImporter;
using BankTransactionImporter.Configuration;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Build configuration
var basePath = Directory.GetCurrentDirectory();
// Go up two directories to find the config folder
var configPath = Path.Combine(basePath, "..", "..", "config", "appsettings.json");
var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile(configPath, optional: false, reloadOnChange: true)
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
services.AddSingleton<ICsvParser, CsvParser>();
services.AddSingleton<ITransactionMapper, TransactionMapper>();
services.AddSingleton<IGoogleSheetsService, GoogleSheetsService>();
services.AddSingleton<Application>();

// Build and run
var serviceProvider = services.BuildServiceProvider();

try
{
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
