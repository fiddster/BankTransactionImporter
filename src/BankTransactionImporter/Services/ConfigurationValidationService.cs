using BankTransactionImporter.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BankTransactionImporter.Services;

public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger;

    public ConfigurationValidationService(ILogger<ConfigurationValidationService> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateConfiguration(AppSettings settings)
    {
        var result = ValidationResult.Success();

        if (settings == null)
        {
            return ValidationResult.Failed("AppSettings cannot be null");
        }

        // Validate Google Sheets configuration
        var googleSheetsResult = ValidateGoogleSheetsConfiguration(settings.GoogleSheets);
        if (!googleSheetsResult.IsValid)
        {
            result.Errors.AddRange(googleSheetsResult.Errors);
            result.IsValid = false;
        }
        result.Warnings.AddRange(googleSheetsResult.Warnings);

        // Validate Processing configuration
        var processingResult = ValidateProcessingConfiguration(settings.Processing);
        if (!processingResult.IsValid)
        {
            result.Errors.AddRange(processingResult.Errors);
            result.IsValid = false;
        }
        result.Warnings.AddRange(processingResult.Warnings);

        // Validate mapping rules path if provided
        if (!string.IsNullOrWhiteSpace(settings.Processing.MappingRulesPath))
        {
            var mappingResult = ValidateMappingRulesPath(settings.Processing.MappingRulesPath);
            if (!mappingResult.IsValid)
            {
                result.Errors.AddRange(mappingResult.Errors);
                result.IsValid = false;
            }
            result.Warnings.AddRange(mappingResult.Warnings);
        }

        _logger.LogInformation("Configuration validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
            result.IsValid, result.Errors.Count, result.Warnings.Count);

        return result;
    }

    public ValidationResult ValidateGoogleSheetsConfiguration(GoogleSheetsConfig config)
    {
        var result = ValidationResult.Success();

        if (config == null)
        {
            return ValidationResult.Failed("GoogleSheetsConfig cannot be null");
        }

        if (string.IsNullOrWhiteSpace(config.SpreadsheetId))
        {
            result.AddError("GoogleSheets.SpreadsheetId is required");
        }
        else if (config.SpreadsheetId.Length < 40) // Google Sheets IDs are typically ~44 characters
        {
            result.AddWarning("GoogleSheets.SpreadsheetId appears to be too short for a valid Google Sheets ID");
        }

        if (string.IsNullOrWhiteSpace(config.DefaultSheetName))
        {
            result.AddWarning("GoogleSheets.DefaultSheetName is not configured");
        }

        if (string.IsNullOrWhiteSpace(config.CredentialsPath))
        {
            result.AddWarning("GoogleSheets.CredentialsPath is not configured - will use mock mode");
        }
        else if (!File.Exists(config.CredentialsPath))
        {
            result.AddError($"GoogleSheets.CredentialsPath points to non-existent file: {config.CredentialsPath}");
        }

        return result;
    }

    public ValidationResult ValidateMappingRulesPath(string mappingRulesPath)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(mappingRulesPath))
        {
            return ValidationResult.Failed("MappingRulesPath cannot be null or empty");
        }

        // Resolve relative paths
        var resolvedPath = Path.IsPathRooted(mappingRulesPath) 
            ? mappingRulesPath 
            : Path.Combine(AppContext.BaseDirectory, mappingRulesPath);

        if (!File.Exists(resolvedPath))
        {
            result.AddError($"Mapping rules file not found at path: {resolvedPath}");
            return result;
        }

        // Validate JSON structure
        try
        {
            var jsonContent = File.ReadAllText(resolvedPath);
            var mappingData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
            
            if (mappingData == null || !mappingData.ContainsKey("MappingRules"))
            {
                result.AddError("Mapping rules file must contain a 'MappingRules' property");
            }
            else
            {
                var mappingRulesElement = (JsonElement)mappingData["MappingRules"];
                if (mappingRulesElement.ValueKind != JsonValueKind.Object)
                {
                    result.AddError("'MappingRules' must be a JSON object");
                }
                else if (mappingRulesElement.EnumerateObject().Count() == 0)
                {
                    result.AddWarning("MappingRules is empty - no transaction mapping rules defined");
                }
            }
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON in mapping rules file: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.AddError($"Failed to validate mapping rules file: {ex.Message}");
        }

        return result;
    }

    private ValidationResult ValidateProcessingConfiguration(ProcessingConfig config)
    {
        var result = ValidationResult.Success();

        if (config == null)
        {
            return ValidationResult.Failed("ProcessingConfig cannot be null");
        }

        if (config.DefaultYear < 2000 || config.DefaultYear > DateTime.Now.Year + 10)
        {
            result.AddWarning($"Processing.DefaultYear ({config.DefaultYear}) seems unusual - expected range: 2000 to {DateTime.Now.Year + 10}");
        }

        if (string.IsNullOrWhiteSpace(config.MappingRulesPath))
        {
            result.AddWarning("Processing.MappingRulesPath is not configured - will use default path");
        }

        return result;
    }
}