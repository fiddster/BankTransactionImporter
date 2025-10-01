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
            // Use UTF-8 encoding to handle Swedish characters
            using var reader = new StreamReader(stream, Encoding.UTF8);

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
                BookingDate = ParseSwedishDate(csv.GetField("Bokföringsdag")),
                TransactionDate = ParseSwedishDate(csv.GetField("Transaktionsdag")),
                CurrencyDate = ParseSwedishDate(csv.GetField("Valutadag")),
                Reference = csv.GetField("Referens") ?? string.Empty,
                Description = csv.GetField("Beskrivning") ?? string.Empty,
                Amount = ParseSwedishDecimal(csv.GetField("Belopp")),
                BookedBalance = ParseSwedishDecimal(csv.GetField("Bokfört saldo"))
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

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return int.TryParse(value, out var result) ? result : 0;
    }

    private static DateTime ParseSwedishDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.MinValue;

        // Expected format: yyyy-MM-dd
        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }

        // Fallback to general parsing
        if (DateTime.TryParse(dateString, out result))
        {
            return result;
        }

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