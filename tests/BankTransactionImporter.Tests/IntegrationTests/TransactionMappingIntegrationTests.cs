using BankTransactionImporter.Models;
using BankTransactionImporter.Services;
using BankTransactionImporter.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter.Tests.IntegrationTests;

public class TransactionMappingIntegrationTests
{
    private readonly MockGoogleSheetsService _mockGoogleSheetsService;
    private readonly ILogger<MockGoogleSheetsService> _googleLogger;
    private readonly ILogger<TransactionMapper> _mapperLogger;
    private readonly TransactionMapper _transactionMapper;

    public TransactionMappingIntegrationTests()
    {
        _googleLogger = CreateMockLogger<MockGoogleSheetsService>();
        _mapperLogger = CreateMockLogger<TransactionMapper>();
        _mockGoogleSheetsService = new MockGoogleSheetsService(_googleLogger);
        _transactionMapper = new TransactionMapper(_mapperLogger);
    }

    [Fact]
    public async Task TransactionMapping_WithGoogleSheets_UpdatesCorrectCells()
    {
        // Arrange
        var spreadsheetId = "integration-test-spreadsheet";
        var sheetName = "Budget";
        
        // Setup initial spreadsheet state
        var initialBudgetData = new Dictionary<(int row, int column), decimal>
        {
            { (2, 3), 0m },   // Income row
            { (5, 3), 0m },   // Tele2 row
            { (16, 3), 0m },  // Spotify row
            { (25, 3), 0m }   // Mat (food) row
        };
        _mockGoogleSheetsService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialBudgetData);

        // Create test transactions
        var transactions = new List<Transaction>
        {
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 15), 
                Description = "LÖN ARBETSGIVARE AB", 
                Amount = 25000.00m,
                AccountNumber = "12345-6789" 
            },
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 16), 
                Description = "TELE2 SVERIGE AB", 
                Amount = -299.00m,
                AccountNumber = "12345-6789" 
            },
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 17), 
                Description = "SPOTIFY AB", 
                Amount = -109.00m,
                AccountNumber = "12345-6789" 
            },
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 18), 
                Description = "ICA SUPERMARKET STORGA", 
                Amount = -450.75m,
                AccountNumber = "12345-6789" 
            }
        };

        // Load sheet structure and map transactions
        var sheetStructure = await _mockGoogleSheetsService.LoadSheetStructureAsync(spreadsheetId, sheetName);

        // Act - Process each transaction
        var cellUpdates = new Dictionary<(int row, int column), decimal>();
        
        foreach (var transaction in transactions)
        {
            var category = _transactionMapper.MapTransaction(transaction, sheetStructure);
            if (category != null)
            {
                var cellCoord = (category.RowIndex, 3); // Column C (index 3)
                
                // Get current value and add transaction amount
                var currentValue = await _mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, 
                    category.RowIndex, 3);
                var newValue = currentValue + Math.Abs(transaction.Amount); // Use absolute value for budget tracking
                
                cellUpdates[cellCoord] = newValue;
            }
        }

        // Batch update all cells
        await _mockGoogleSheetsService.BatchUpdateCellsAsync(spreadsheetId, sheetName, cellUpdates);

        // Assert - Verify correct updates
        var finalValues = await _mockGoogleSheetsService.BatchGetCellValuesAsync(spreadsheetId, sheetName, 
            cellUpdates.Keys);

        // Check income (row 2) - should have 25000.00
        Assert.True(finalValues.ContainsKey((2, 3)));
        Assert.Equal(25000.00m, finalValues[(2, 3)]);

        // Check Tele2 (row 5) - should have 299.00
        Assert.True(finalValues.ContainsKey((5, 3)));
        Assert.Equal(299.00m, finalValues[(5, 3)]);

        // Check Spotify (row 16) - should have 109.00
        Assert.True(finalValues.ContainsKey((16, 3)));
        Assert.Equal(109.00m, finalValues[(16, 3)]);

        // Check Mat/Food (row 25) - should have 450.75
        Assert.True(finalValues.ContainsKey((25, 3)));
        Assert.Equal(450.75m, finalValues[(25, 3)]);
    }

    [Fact]
    public async Task TransactionMapping_MultipleTransactionsSameCategory_AccumulatesCorrectly()
    {
        // Arrange
        var spreadsheetId = "accumulation-test-spreadsheet";
        var sheetName = "Budget";
        
        var initialBudgetData = new Dictionary<(int row, int column), decimal>
        {
            { (25, 3), 0m }   // Mat (food) row
        };
        _mockGoogleSheetsService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialBudgetData);

        // Multiple food purchases
        var transactions = new List<Transaction>
        {
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 15), 
                Description = "ICA SUPERMARKET CENTRUM", 
                Amount = -245.50m,
                AccountNumber = "12345-6789" 
            },
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 18), 
                Description = "COOP KONSUM VÄSTER", 
                Amount = -189.25m,
                AccountNumber = "12345-6789" 
            },
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 22), 
                Description = "WILLYS HANDELSBOLAG", 
                Amount = -324.75m,
                AccountNumber = "12345-6789" 
            }
        };

        var sheetStructure = await _mockGoogleSheetsService.LoadSheetStructureAsync(spreadsheetId, sheetName);
        var foodCategory = sheetStructure.Categories.First(c => c.Name == "Mat");

        // Act - Process transactions one by one, accumulating values
        decimal runningTotal = 0m;
        foreach (var transaction in transactions)
        {
            var category = _transactionMapper.MapTransaction(transaction, sheetStructure);
            Assert.NotNull(category);
            Assert.Equal(foodCategory.Name, category.Name);

            // Get current value and add transaction amount
            var currentValue = await _mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, 
                category.RowIndex, 3);
            var newValue = currentValue + Math.Abs(transaction.Amount);
            runningTotal += Math.Abs(transaction.Amount);

            await _mockGoogleSheetsService.UpdateCellAsync(spreadsheetId, sheetName, 
                category.RowIndex, 3, newValue);
        }

        // Assert
        var finalValue = await _mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, 
            foodCategory.RowIndex, 3);
        
        var expectedTotal = 245.50m + 189.25m + 324.75m; // 759.50
        Assert.Equal(expectedTotal, finalValue);
        Assert.Equal(expectedTotal, runningTotal);
    }

    [Fact]
    public async Task TransactionMapping_UnmappedTransactions_DoesNotUpdateSheet()
    {
        // Arrange
        var spreadsheetId = "unmapped-test-spreadsheet";
        var sheetName = "Budget";
        
        var initialBudgetData = new Dictionary<(int row, int column), decimal>
        {
            { (2, 3), 0m },   // Income row
            { (25, 3), 0m }   // Mat row
        };
        _mockGoogleSheetsService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialBudgetData);

        // Transactions that don't match any mapping patterns
        var transactions = new List<Transaction>
        {
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 15), 
                Description = "UNKNOWN MERCHANT XYZ", 
                Amount = -150.00m,
                AccountNumber = "12345-6789" 
            },
            new() 
            { 
                BookingDate = new DateTime(2024, 1, 16), 
                Description = "MYSTERIOUS PAYMENT ABC", 
                Amount = -75.50m,
                AccountNumber = "12345-6789" 
            }
        };

        var sheetStructure = await _mockGoogleSheetsService.LoadSheetStructureAsync(spreadsheetId, sheetName);

        // Act - Try to map unmappable transactions
        var mappedCategories = new List<BudgetCategory?>();
        foreach (var transaction in transactions)
        {
            var category = _transactionMapper.MapTransaction(transaction, sheetStructure);
            mappedCategories.Add(category);
        }

        // Assert - No transactions should be mapped
        Assert.All(mappedCategories, category => Assert.Null(category));

        // Verify sheet values remain unchanged
        var finalValues = _mockGoogleSheetsService.GetAllCellValues(spreadsheetId, sheetName);
        Assert.Equal(0m, finalValues[(2, 3)]);   // Income still 0
        Assert.Equal(0m, finalValues[(25, 3)]);  // Mat still 0
    }

    [Fact]
    public async Task BatchProcessing_LargeTransactionSet_PerformsEfficiently()
    {
        // Arrange
        var spreadsheetId = "batch-performance-spreadsheet";
        var sheetName = "Budget";
        
        // Setup budget rows
        var initialBudgetData = new Dictionary<(int row, int column), decimal>();
        var sheetStructure = await _mockGoogleSheetsService.LoadSheetStructureAsync(spreadsheetId, sheetName);
        
        foreach (var category in sheetStructure.Categories)
        {
            initialBudgetData[(category.RowIndex, 3)] = 0m;
        }
        _mockGoogleSheetsService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialBudgetData);

        // Generate large number of transactions
        var transactions = new List<Transaction>();
        var random = new Random(42); // Fixed seed for reproducible tests
        
        var descriptions = new[] { "ICA SUPERMARKET", "SPOTIFY AB", "TELE2 SVERIGE", "NETFLIX", "LÖN ARBETSGIVARE" };
        
        for (int i = 0; i < 100; i++)
        {
            transactions.Add(new Transaction
            {
                BookingDate = new DateTime(2024, 1, 1).AddDays(i % 30),
                Description = descriptions[i % descriptions.Length] + $" {i}",
                Amount = -(decimal)(random.NextDouble() * 500 + 10),
                AccountNumber = "12345-6789"
            });
        }

        // Act - Process with batch operations
        var categoryUpdates = new Dictionary<(int row, int column), decimal>();
        
        foreach (var transaction in transactions)
        {
            var category = _transactionMapper.MapTransaction(transaction, sheetStructure);
            if (category != null)
            {
                var cellCoord = (category.RowIndex, 3);
                if (!categoryUpdates.ContainsKey(cellCoord))
                {
                    categoryUpdates[cellCoord] = 0m;
                }
                categoryUpdates[cellCoord] += Math.Abs(transaction.Amount);
            }
        }

        var startTime = DateTime.UtcNow;
        await _mockGoogleSheetsService.BatchUpdateCellsAsync(spreadsheetId, sheetName, categoryUpdates);
        var endTime = DateTime.UtcNow;

        // Assert
        var processingTime = endTime - startTime;
        Assert.True(processingTime.TotalSeconds < 1, "Batch processing should complete within 1 second");
        
        // Verify some updates were made
        Assert.NotEmpty(categoryUpdates);
        
        // Verify data integrity
        var finalValues = await _mockGoogleSheetsService.BatchGetCellValuesAsync(spreadsheetId, sheetName, 
            categoryUpdates.Keys);
        
        foreach (var (coord, expectedValue) in categoryUpdates)
        {
            Assert.True(finalValues.ContainsKey(coord));
            // Use decimal comparison with tolerance due to string formatting/parsing precision
            Assert.Equal(expectedValue, finalValues[coord], 2); // Compare with 2 decimal places precision
        }
    }

    [Fact]
    public void GetUnmappedCategories_ReturnsExpectedResults()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() { Description = "LÖN ARBETSGIVARE AB", Amount = 25000.00m },
            new() { Description = "UNKNOWN MERCHANT", Amount = -150.00m },
            new() { Description = "SPOTIFY AB", Amount = -109.00m },
            new() { Description = "ANOTHER UNKNOWN", Amount = -75.00m }
        };

        var sheetStructure = new SheetStructure
        {
            Categories = new List<BudgetCategory>
            {
                new() { Name = "Inkomst", MappingPatterns = new() { "LÖN" } },
                new() { Name = "Spotify", MappingPatterns = new() { "SPOTIFY" } },
                new() { Name = "Unmapped Category", MappingPatterns = new() { "NEVER_MATCHES" } }
            }
        };

        // Act
        var unmappedCategories = _transactionMapper.GetUnmappedCategories(transactions, sheetStructure);

        // Assert
        Assert.Single(unmappedCategories);
        Assert.Equal("Unmapped Category", unmappedCategories[0].Name);
    }

    private static ILogger<T> CreateMockLogger<T>()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILogger<T>>();
    }
}