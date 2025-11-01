using System.Text;
using System.Text.Json;
using BankTransactionImporter.Configuration;
using BankTransactionImporter.Models;
using BankTransactionImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter;

public class SheetBackupUtility
{
    private readonly IGoogleSheetsService _sheetsService;
    private readonly ILogger<SheetBackupUtility> _logger;
    private readonly AppSettings _appSettings;

    public SheetBackupUtility(IGoogleSheetsService sheetsService, ILogger<SheetBackupUtility> logger, AppSettings appSettings)
    {
        _sheetsService = sheetsService ?? throw new ArgumentNullException(nameof(sheetsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public async Task CreateBackupAsync(string spreadsheetId, string sheetName, string? backupPath = null)
    {
        try
        {
            _logger.LogInformation("Starting backup of Google Sheet '{SheetName}' (ID: {SpreadsheetId})", sheetName, spreadsheetId);

            // Download all data from the sheet
            var sheetData = await _sheetsService.GetAllSheetDataAsync(spreadsheetId, sheetName);

            // Determine the final backup path
            backupPath = ResolveBackupPath(backupPath, sheetName);

            // Create backups directory if it doesn't exist
            var directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created backup directory: {Directory}", directory);
            }

            // Save in multiple formats
            await SaveAsCsvAsync(sheetData, $"{backupPath}.csv");
            await SaveAsJsonAsync(sheetData, spreadsheetId, sheetName, $"{backupPath}.json");
            await SaveAsTextAsync(sheetData, $"{backupPath}.txt");

            _logger.LogInformation("✅ Backup completed successfully!");
            _logger.LogInformation("   📁 CSV: {CsvPath}", $"{backupPath}.csv");
            _logger.LogInformation("   📁 JSON: {JsonPath}", $"{backupPath}.json");
            _logger.LogInformation("   📁 Text: {TextPath}", $"{backupPath}.txt");
            _logger.LogInformation("   📊 Total rows: {RowCount}", sheetData.Count);

            Console.WriteLine($"🎉 Backup completed! Files saved:");
            Console.WriteLine($"   📄 CSV:  {backupPath}.csv");
            Console.WriteLine($"   📄 JSON: {backupPath}.json");
            Console.WriteLine($"   📄 Text: {backupPath}.txt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            Console.WriteLine($"❌ Backup failed: {ex.Message}");
            throw;
        }
    }

    private async Task SaveAsCsvAsync(List<List<string>> data, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        foreach (var row in data)
        {
            // Escape CSV values properly
            var escapedValues = row.Select(cell =>
            {
                if (string.IsNullOrEmpty(cell))
                    return "";

                // If contains comma, quote, or newline, wrap in quotes and escape quotes
                if (cell.Contains(',') || cell.Contains('"') || cell.Contains('\n') || cell.Contains('\r'))
                {
                    return $"\"{cell.Replace("\"", "\"\"")}\"";
                }
                return cell;
            });

            await writer.WriteLineAsync(string.Join(",", escapedValues));
        }

        _logger.LogDebug("Saved CSV backup to: {FilePath}", filePath);
    }

    private async Task SaveAsJsonAsync(List<List<string>> data, string spreadsheetId, string sheetName, string filePath)
    {
        var backup = new
        {
            Metadata = new
            {
                SpreadsheetId = spreadsheetId,
                SheetName = sheetName,
                BackupTimestamp = DateTime.UtcNow,
                RowCount = data.Count,
                MaxColumnCount = data.Count > 0 ? data.Max(row => row.Count) : 0
            },
            Data = data.Select((row, index) => new
            {
                RowIndex = index + 1,
                Cells = row.Select((cell, colIndex) => new
                {
                    Column = SheetStructure.IndexToColumnLetter(colIndex + 1),
                    ColumnIndex = colIndex + 1,
                    Value = cell
                }).ToArray()
            }).ToArray()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(backup, options);
        await File.WriteAllTextAsync(filePath, json);

        _logger.LogDebug("Saved JSON backup to: {FilePath}", filePath);
    }

    private async Task SaveAsTextAsync(List<List<string>> data, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        await writer.WriteLineAsync($"Google Sheets Backup");
        await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"Total Rows: {data.Count}");
        await writer.WriteLineAsync(new string('=', 50));
        await writer.WriteLineAsync();

        for (int i = 0; i < data.Count; i++)
        {
            var row = data[i];
            await writer.WriteLineAsync($"Row {i + 1}:");

            for (int j = 0; j < row.Count; j++)
            {
                var cell = row[j];
                if (!string.IsNullOrEmpty(cell))
                {
                    await writer.WriteLineAsync($"  {SheetStructure.IndexToColumnLetter(j + 1)}{i + 1}: {cell}");
                }
            }

            if (i < data.Count - 1)
                await writer.WriteLineAsync();
        }

        _logger.LogDebug("Saved text backup to: {FilePath}", filePath);
    }

    private string ResolveBackupPath(string? customPath, string sheetName)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var baseFileName = $"sheet-backup_{sheetName}_{timestamp}";

        if (string.IsNullOrEmpty(customPath))
        {
            // Check for environment variable override (highest priority)
            var envBackupPath = Environment.GetEnvironmentVariable("BACKUP_PATH");
            if (!string.IsNullOrEmpty(envBackupPath))
            {
                var expandedEnvPath = Environment.ExpandEnvironmentVariables(envBackupPath);
                _logger.LogInformation("Using backup path from BACKUP_PATH environment variable: {Path}", expandedEnvPath);
                return Path.Combine(expandedEnvPath, baseFileName);
            }

            // Check configuration setting (second priority)
            if (!string.IsNullOrEmpty(_appSettings.Backup.DefaultPath))
            {
                var expandedConfigPath = Environment.ExpandEnvironmentVariables(_appSettings.Backup.DefaultPath);
                _logger.LogInformation("Using backup path from configuration: {Path}", expandedConfigPath);
                return Path.Combine(expandedConfigPath, baseFileName);
            }

            // Default: use backups directory in application folder (lowest priority)
            var backupDir = Path.Combine(AppContext.BaseDirectory, "backups");
            return Path.Combine(backupDir, baseFileName);
        }

        // Expand environment variables in custom path
        customPath = Environment.ExpandEnvironmentVariables(customPath);

        // If custom path is provided, determine if it's a directory or file
        if (Directory.Exists(customPath) ||
            customPath.EndsWith(Path.DirectorySeparatorChar) ||
            customPath.EndsWith(Path.AltDirectorySeparatorChar) ||
            string.IsNullOrEmpty(Path.GetExtension(customPath)))
        {
            // It's a directory - use it with the default filename
            return Path.Combine(customPath, baseFileName);
        }
        else
        {
            // It's a file path - use it as-is (without extension, we'll add .csv, .json, .txt)
            return customPath;
        }
    }


    public Task ListBackupsAsync()
    {
        ListBackupsFromDirectory(Path.Combine(AppContext.BaseDirectory, "backups"));
        return Task.CompletedTask;
    }

    public Task ListBackupsAsync(string? customDirectory = null)
    {
        if (!string.IsNullOrEmpty(customDirectory))
        {
            ListBackupsFromDirectory(customDirectory);
        }
        else
        {
            ListBackupsFromDirectory(Path.Combine(AppContext.BaseDirectory, "backups"));
        }
        return Task.CompletedTask;
    }

    private void ListBackupsFromDirectory(string backupDir)
    {
        if (!Directory.Exists(backupDir))
        {
            Console.WriteLine($"📂 Backup directory not found: {backupDir}");
            Console.WriteLine("   Use 'dotnet run backup' to create your first backup.");
            return;
        }

        var backupFiles = Directory.GetFiles(backupDir, "sheet-backup_*.csv")
            .OrderByDescending(f => new FileInfo(f).CreationTime)
            .ToArray();

        if (backupFiles.Length == 0)
        {
            Console.WriteLine($"📂 No backup files found in: {backupDir}");
            Console.WriteLine("   Use 'dotnet run backup' to create your first backup.");
            return;
        }

        Console.WriteLine($"📂 Found {backupFiles.Length} backup(s) in: {backupDir}");
        Console.WriteLine();

        foreach (var file in backupFiles)
        {
            var fileInfo = new FileInfo(file);
            var baseName = Path.GetFileNameWithoutExtension(file);

            Console.WriteLine($"📄 {baseName}");
            Console.WriteLine($"   📅 Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   📏 Size: {FormatFileSize(fileInfo.Length)}");

            // Check for associated files
            var jsonFile = Path.ChangeExtension(file, ".json");
            var textFile = Path.ChangeExtension(file, ".txt");

            var formats = new List<string> { "CSV" };
            if (File.Exists(jsonFile)) formats.Add("JSON");
            if (File.Exists(textFile)) formats.Add("Text");

            Console.WriteLine($"   📋 Formats: {string.Join(", ", formats)}");
            Console.WriteLine();
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}