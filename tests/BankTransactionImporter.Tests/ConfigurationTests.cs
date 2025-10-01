using BankTransactionImporter.Configuration;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BankTransactionImporter.Tests;

public class ConfigurationTests
{
    [Fact]
    public void AppSettings_ShouldBindFromConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            {"GoogleSheets:SpreadsheetId", "test-spreadsheet-id-1234567890abcdef123456789"},
            {"GoogleSheets:DefaultSheetName", "2025"},
            {"GoogleSheets:CredentialsPath", "credentials.json"},
            {"Processing:BackupBeforeUpdate", "false"},
            {"Processing:DryRun", "true"},
            {"Processing:DefaultYear", "2025"},
            {"Processing:MappingRulesPath", "mapping-rules.json"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var appSettings = new AppSettings();
        configuration.Bind(appSettings);

        // Assert
        Assert.Equal("test-spreadsheet-id-1234567890abcdef123456789", appSettings.GoogleSheets.SpreadsheetId);
        Assert.Equal("2025", appSettings.GoogleSheets.DefaultSheetName);
        Assert.Equal("credentials.json", appSettings.GoogleSheets.CredentialsPath);
        Assert.False(appSettings.Processing.BackupBeforeUpdate);
        Assert.True(appSettings.Processing.DryRun);
        Assert.Equal(2025, appSettings.Processing.DefaultYear);
        Assert.Equal("mapping-rules.json", appSettings.Processing.MappingRulesPath);
    }

    [Fact]
    public void ConfigurationValidationService_ShouldValidateCompleteConfiguration()
    {
        // Arrange
        var logger = CreateMockLogger<ConfigurationValidationService>();
        var validationService = new ConfigurationValidationService(logger);
        
        var appSettings = new AppSettings
        {
            GoogleSheets = new GoogleSheetsConfig
            {
                SpreadsheetId = "test-spreadsheet-id-1234567890abcdef123456789",
                DefaultSheetName = "2025"
                // CredentialsPath intentionally left empty to test warning
            },
            Processing = new ProcessingConfig
            {
                DefaultYear = 2025,
                BackupBeforeUpdate = true,
                DryRun = true
                // MappingRulesPath intentionally left empty to test warning
            }
        };

        // Act
        var result = validationService.ValidateConfiguration(appSettings);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, w => w.Contains("CredentialsPath is not configured"));
        Assert.Contains(result.Warnings, w => w.Contains("MappingRulesPath is not configured"));
    }

    [Fact]
    public void ConfigurationValidationService_ShouldFailForInvalidConfiguration()
    {
        // Arrange
        var logger = CreateMockLogger<ConfigurationValidationService>();
        var validationService = new ConfigurationValidationService(logger);
        
        var appSettings = new AppSettings
        {
            GoogleSheets = new GoogleSheetsConfig
            {
                SpreadsheetId = "", // Invalid - empty
                DefaultSheetName = "2025"
            },
            Processing = new ProcessingConfig
            {
                DefaultYear = 1999 // Warning - unusual year
            }
        };

        // Act
        var result = validationService.ValidateConfiguration(appSettings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("SpreadsheetId is required"));
        Assert.Contains(result.Warnings, w => w.Contains("DefaultYear (1999) seems unusual"));
    }

    [Fact]
    public void ValidateMappingRulesPath_ShouldValidateValidJsonFile()
    {
        // Arrange
        var logger = CreateMockLogger<ConfigurationValidationService>();
        var validationService = new ConfigurationValidationService(logger);
        
        var tempFile = Path.GetTempFileName();
        var validMappingRules = new
        {
            MappingRules = new Dictionary<string, string>
            {
                {"LÖN", "Inkomst"},
                {"NETFLIX", "Netflix"}
            }
        };
        
        File.WriteAllText(tempFile, JsonSerializer.Serialize(validMappingRules));

        try
        {
            // Act
            var result = validationService.ValidateMappingRulesPath(tempFile);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateMappingRulesPath_ShouldFailForInvalidJsonStructure()
    {
        // Arrange
        var logger = CreateMockLogger<ConfigurationValidationService>();
        var validationService = new ConfigurationValidationService(logger);
        
        var tempFile = Path.GetTempFileName();
        var invalidMappingRules = new
        {
            InvalidProperty = "This should be MappingRules"
        };
        
        File.WriteAllText(tempFile, JsonSerializer.Serialize(invalidMappingRules));

        try
        {
            // Act
            var result = validationService.ValidateMappingRulesPath(tempFile);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("must contain a 'MappingRules' property"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateMappingRulesPath_ShouldFailForNonExistentFile()
    {
        // Arrange
        var logger = CreateMockLogger<ConfigurationValidationService>();
        var validationService = new ConfigurationValidationService(logger);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file.json");

        // Act
        var result = validationService.ValidateMappingRulesPath(nonExistentPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Mapping rules file not found"));
    }

    [Fact]
    public void ValidateGoogleSheetsConfiguration_ShouldWarnForShortSpreadsheetId()
    {
        // Arrange
        var logger = CreateMockLogger<ConfigurationValidationService>();
        var validationService = new ConfigurationValidationService(logger);
        
        var config = new GoogleSheetsConfig
        {
            SpreadsheetId = "short-id", // Too short for a real Google Sheets ID
            DefaultSheetName = "2025"
        };

        // Act
        var result = validationService.ValidateGoogleSheetsConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, w => w.Contains("appears to be too short"));
    }

    [Fact]
    public void ValidationResult_ShouldSupportFluentInterface()
    {
        // Arrange & Act
        var result = ValidationResult.Success()
            .AddWarning("First warning")
            .AddWarning("Second warning")
            .AddError("Fatal error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Equal("Fatal error", result.Errors[0]);
        Assert.Equal("First warning", result.Warnings[0]);
        Assert.Equal("Second warning", result.Warnings[1]);
    }

    private static ILogger<T> CreateMockLogger<T>()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILogger<T>>();
    }
}