using System.Globalization;
using System.Text;
using BankTransactionImporter.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter.Services;

public class CsvParser : ICsvParser
{
    private readonly ILogger<CsvParser> _logger;

    public CsvParser(ILogger<CsvParser> logger)
    {
        _logger = logger;
    }

    public async Task<List<Transaction>> ParseTransactionsAsync(string filePath)
    {
        _logger.LogInformation("Parsing transactions from file: {FilePath}", filePath);

        using var stream = File.OpenRead(filePath);
        return await ParseTransactionsAsync(stream);
    }

    public async Task<List<Transaction>> ParseTransactionsAsync(Stream stream)
    {
        var transactions = new List<Transaction>();

        try
        {
            // Use Windows-1252 encoding which is common for Swedish bank files
            using var reader = new StreamReader(stream, Encoding.GetEncoding("Windows-1252"), detectEncodingFromByteOrderMarks: true);

            // Skip the first line (metadata)
            var firstLine = await reader.ReadLineAsync();
            _logger.LogDebug("Skipping metadata line: {FirstLine}", firstLine);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                BadDataFound = null, // Ignore bad data
                MissingFieldFound = null, // Ignore missing fields
            };

            using var csv = new CsvReader(reader, config);

            // Read header
            await csv.ReadAsync();
            csv.ReadHeader();

            // Parse each row
            while (await csv.ReadAsync())
            {
                try
                {
                    var transaction = ParseTransactionRow(csv);
                    if (transaction != null)
                    {
                        transactions.Add(transaction);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse transaction row. Skipping.");
                }
            }

            _logger.LogInformation("Successfully parsed {Count} transactions", transactions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse transactions from CSV");
            throw;
        }

        return transactions.OrderBy(t => t.BookingDate).ToList();
    }

    private Transaction? ParseTransactionRow(CsvReader csv)
    {
        try
        {
            var transaction = new Transaction
            {
                RowNumber = ParseInt(csv.GetField("Radnummer")),
                ClearingNumber = csv.GetField("Clearingnummer") ?? string.Empty,
                AccountNumber = csv.GetField("Kontonummer") ?? string.Empty,
                Product = csv.GetField("Produkt") ?? string.Empty,
                Currency = csv.GetField("Valuta") ?? string.Empty,
                BookingDate = ParseSwedishDate(GetFieldWithEncodingFallback(csv, "Bokföringsdag", "Bokf�ringsdag")),
                TransactionDate = ParseSwedishDate(csv.GetField("Transaktionsdag")),
                CurrencyDate = ParseSwedishDate(csv.GetField("Valutadag")),
                Reference = csv.GetField("Referens") ?? string.Empty,
                Description = csv.GetField("Beskrivning") ?? string.Empty,
                Amount = ParseSwedishDecimal(csv.GetField("Belopp")),
                BookedBalance = ParseSwedishDecimal(GetFieldWithEncodingFallback(csv, "Bokfört saldo", "Bokf�rt saldo"))
            };

            // Clean up reference and description fields
            transaction.Reference = CleanTextField(transaction.Reference);
            transaction.Description = CleanTextField(transaction.Description);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse individual transaction row");
            return null;
        }
    }

    private static string CleanTextField(string field)
    {
        return field?.Replace("\"", "").Trim() ?? string.Empty;
    }

    private static string? GetFieldWithEncodingFallback(CsvReader csv, string preferredFieldName, string fallbackFieldName)
    {
        // Try the preferred field name first
        try
        {
            var value = csv.GetField(preferredFieldName);
            if (value != null) return value;
        }
        catch
        {
            // If the preferred field doesn't exist, try the fallback
        }
        
        // Try the fallback field name (for malformed encoding)
        try
        {
            return csv.GetField(fallbackFieldName);
        }
        catch
        {
            return null;
        }
    }

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return int.TryParse(value, out var result) ? result : 0;
    }

    private DateTime ParseSwedishDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            _logger.LogDebug("Empty date string received");
            return DateTime.MinValue;
        }

        // Expected format: yyyy-MM-dd
        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            _logger.LogDebug("Successfully parsed date '{DateString}' as {ParsedDate}", dateString, result);
            return result;
        }

        // Fallback to general parsing
        if (DateTime.TryParse(dateString, out result))
        {
            _logger.LogDebug("Parsed date '{DateString}' using fallback as {ParsedDate}", dateString, result);
            return result;
        }

        _logger.LogWarning("Failed to parse date string: '{DateString}'", dateString);
        return DateTime.MinValue;
    }

    private static decimal ParseSwedishDecimal(string? decimalString)
    {
        if (string.IsNullOrWhiteSpace(decimalString))
            return 0m;

        // Handle Swedish number format (comma as decimal separator)
        var normalizedString = decimalString
            .Replace(" ", "") // Remove spaces
            .Replace(",", "."); // Convert comma to dot

        if (decimal.TryParse(normalizedString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0m;
    }
}