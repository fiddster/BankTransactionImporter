namespace BankTransactionImporter.Configuration;

public class AppSettings
{
    public GoogleSheetsConfig GoogleSheets { get; set; } = new();
    public ProcessingConfig Processing { get; set; } = new();
}

public class GoogleSheetsConfig
{
    public string SpreadsheetId { get; set; } = string.Empty;
    public string DefaultSheetName { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}

public class ProcessingConfig
{
    public bool BackupBeforeUpdate { get; set; } = true;
    public bool DryRun { get; set; } = true;
    public int DefaultYear { get; set; } = DateTime.Now.Year;
    public string MappingRulesPath { get; set; } = string.Empty;
}