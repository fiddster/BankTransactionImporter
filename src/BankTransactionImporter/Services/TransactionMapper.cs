using System.Text.Json;
using BankTransactionImporter.Models;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter.Services;

public class TransactionMapper : ITransactionMapper
{
    private readonly ILogger<TransactionMapper> _logger;
    private Dictionary<string, string> _mappingRules = new();

    public TransactionMapper(ILogger<TransactionMapper> logger)
    {
        _logger = logger;
    }

    public void LoadMappingRules(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Mapping rules file not found: {ConfigPath}. Using default rules.", configPath);
                LoadDefaultMappingRules();
                return;
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<MappingConfig>(json);

            _mappingRules = config?.MappingRules ?? new Dictionary<string, string>();

            _logger.LogInformation("Loaded {Count} mapping rules from {ConfigPath}",
                _mappingRules.Count, configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mapping rules from {ConfigPath}. Using defaults.", configPath);
            LoadDefaultMappingRules();
        }
    }

    private void LoadDefaultMappingRules()
    {
        _mappingRules = new Dictionary<string, string>
        {
            // Income
            { "LÖN", "Inkomst" },
            { "LOEN", "Inkomst" },
            { "SALARY", "Inkomst" },
            
            // Shared expenses (Gemensamma)
            { "TELE2", "Tele2" },
            { "NETFLIX", "Netflix" },
            { "MOTORHALLAND FIN", "Billån" },
            { "BILLÅN", "Billån" },
            { "BILLAN", "Billån" },
            { "IF SKADEFÖRS", "IF Skadeförsäkring" },
            { "IF SKADEF", "IF Skadeförsäkring" },
            { "HALMSTADS FASTIG", "Hyra" },
            { "HYRA", "Hyra" },
            { "RENT", "Hyra" },
            
            // Personal expenses (Mina egna)
            { "UNION AKASSA", "A-kassa" },
            { "A-KASSA", "A-kassa" },
            { "AKASSA", "A-kassa" },
            { "BLIWA", "Bliwa Sjuk & Olycksförsäkring" },
            { "COMVIQ", "Comviq mobil" },
            { "UNIONEN", "Fackavgift" },
            { "FACK", "Fackavgift" },
            { "CSN", "CSN" },
            { "SPOTIFY", "Spotify" },
            { "PLAYSTATION", "Playstation+" },
            { "BÄCKAMOT", "Bäckamot" },
            { "BACKAMOT", "Bäckamot" },
            { "MAT", "Mat" },
            { "FOOD", "Mat" },
            { "ICA", "Mat" },
            { "COOP", "Mat" },
            { "WILLYS", "Mat" },
            
            // Savings
            { "SPARANDE", "Kontant" },
            { "SAVING", "Kontant" },
        };

        _logger.LogInformation("Loaded {Count} default mapping rules", _mappingRules.Count);
    }

    public BudgetCategory? MapTransaction(Transaction transaction, SheetStructure sheetStructure)
    {
        // Try direct mapping rules first
        var mappingKey = transaction.MappingKey;

        foreach (var rule in _mappingRules)
        {
            if (mappingKey.Contains(rule.Key, StringComparison.OrdinalIgnoreCase))
            {
                var category = sheetStructure.FindCategory(rule.Value);
                if (category != null)
                {
                    _logger.LogDebug("Mapped '{MappingKey}' to category '{CategoryName}' using rule '{Rule}'",
                        mappingKey, category.Name, rule.Key);
                    return category;
                }
            }
        }

        // Try fuzzy matching with category patterns
        var bestMatch = sheetStructure.FindBestMatch(transaction);
        if (bestMatch != null)
        {
            _logger.LogDebug("Mapped '{MappingKey}' to category '{CategoryName}' using pattern matching",
                mappingKey, bestMatch.Name);
            return bestMatch;
        }

        // Handle income transactions
        if (transaction.IsIncome)
        {
            var incomeCategory = sheetStructure.Categories.FirstOrDefault(c => c.Type == CategoryType.Income);
            if (incomeCategory != null)
            {
                _logger.LogDebug("Mapped income transaction '{MappingKey}' to '{CategoryName}'",
                    mappingKey, incomeCategory.Name);
                return incomeCategory;
            }
        }

        _logger.LogWarning("No mapping found for transaction: {Transaction}", transaction);
        return null;
    }

    public List<BudgetCategory> GetUnmappedCategories(List<Transaction> transactions, SheetStructure sheetStructure)
    {
        var mappedCategoryNames = new HashSet<string>();

        foreach (var transaction in transactions)
        {
            var category = MapTransaction(transaction, sheetStructure);
            if (category != null)
            {
                mappedCategoryNames.Add(category.Name);
            }
        }

        return sheetStructure.Categories
            .Where(c => !mappedCategoryNames.Contains(c.Name))
            .ToList();
    }

    private class MappingConfig
    {
        public Dictionary<string, string> MappingRules { get; set; } = new();
    }
}