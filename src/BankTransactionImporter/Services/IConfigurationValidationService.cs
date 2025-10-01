using BankTransactionImporter.Configuration;

namespace BankTransactionImporter.Services;

public interface IConfigurationValidationService
{
    ValidationResult ValidateConfiguration(AppSettings settings);
    ValidationResult ValidateMappingRulesPath(string mappingRulesPath);
    ValidationResult ValidateGoogleSheetsConfiguration(GoogleSheetsConfig config);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failed(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    public ValidationResult AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
        return this;
    }

    public ValidationResult AddWarning(string warning)
    {
        Warnings.Add(warning);
        return this;
    }
}