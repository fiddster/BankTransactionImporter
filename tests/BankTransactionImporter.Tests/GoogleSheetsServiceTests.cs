using BankTransactionImporter.Models;
using BankTransactionImporter.Services;
using BankTransactionImporter.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter.Tests;

public class GoogleSheetsServiceTests
{
    private readonly MockGoogleSheetsService _mockService;
    private readonly ILogger<MockGoogleSheetsService> _logger;

    public GoogleSheetsServiceTests()
    {
        _logger = CreateMockLogger<MockGoogleSheetsService>();
        _mockService = new MockGoogleSheetsService(_logger);
    }

    [Fact]
    public async Task LoadSheetStructureAsync_ReturnsValidStructure()
    {
        // Arrange
        var spreadsheetId = "test-spreadsheet-id";
        var sheetName = "Budget";

        // Act
        var structure = await _mockService.LoadSheetStructureAsync(spreadsheetId, sheetName);

        // Assert
        Assert.NotNull(structure);
        Assert.NotEmpty(structure.Categories);
        
        // Verify we have expected categories
        var incomeCategory = structure.Categories.FirstOrDefault(c => c.Type == CategoryType.Income);
        Assert.NotNull(incomeCategory);
        Assert.Equal("Inkomst", incomeCategory.Name);

        var sharedExpenseCategory = structure.Categories.FirstOrDefault(c => c.Type == CategoryType.SharedExpense);
        Assert.NotNull(sharedExpenseCategory);

        var personalExpenseCategory = structure.Categories.FirstOrDefault(c => c.Type == CategoryType.PersonalExpense);
        Assert.NotNull(personalExpenseCategory);

        var savingsCategory = structure.Categories.FirstOrDefault(c => c.Type == CategoryType.Savings);
        Assert.NotNull(savingsCategory);
    }

    [Fact]
    public async Task UpdateCellAsync_StoresValueCorrectly()
    {
        // Arrange
        var spreadsheetId = "test-spreadsheet-id";
        var sheetName = "Budget";
        var row = 5;
        var column = 3;
        var expectedValue = 1234.56m;

        // Act
        await _mockService.UpdateCellAsync(spreadsheetId, sheetName, row, column, expectedValue);

        // Assert
        var actualValue = await _mockService.GetCellValueAsync(spreadsheetId, sheetName, row, column);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public async Task GetCellValueAsync_ReturnsZeroForNonExistentCell()
    {
        // Arrange
        var spreadsheetId = "test-spreadsheet-id";
        var sheetName = "Budget";
        var row = 100;
        var column = 100;

        // Act
        var value = await _mockService.GetCellValueAsync(spreadsheetId, sheetName, row, column);

        // Assert
        Assert.Equal(0m, value);
    }

    [Fact]
    public async Task BatchGetCellValuesAsync_ReturnsCorrectValues()
    {
        // Arrange
        var spreadsheetId = "test-spreadsheet-id";
        var sheetName = "Budget";
        
        var testData = new Dictionary<(int row, int column), decimal>
        {
            { (2, 3), 100.50m },
            { (5, 7), 250.75m },
            { (10, 2), 500.00m }
        };

        _mockService.SetupMockSpreadsheet(spreadsheetId, sheetName, testData);

        var coordinates = testData.Keys.ToList();

        // Act
        var result = await _mockService.BatchGetCellValuesAsync(spreadsheetId, sheetName, coordinates);

        // Assert
        Assert.Equal(testData.Count, result.Count);
        
        foreach (var ((row, column), expectedValue) in testData)
        {
            Assert.True(result.ContainsKey((row, column)));
            Assert.Equal(expectedValue, result[(row, column)]);
        }
    }

    [Fact]
    public async Task BatchUpdateCellsAsync_UpdatesAllCellsCorrectly()
    {
        // Arrange
        var spreadsheetId = "test-spreadsheet-id";
        var sheetName = "Budget";
        
        var updates = new Dictionary<(int row, int column), decimal>
        {
            { (3, 4), 150.25m },
            { (6, 8), 300.50m },
            { (12, 1), 750.00m }
        };

        // Act
        await _mockService.BatchUpdateCellsAsync(spreadsheetId, sheetName, updates);

        // Assert
        var result = await _mockService.BatchGetCellValuesAsync(spreadsheetId, sheetName, updates.Keys);
        
        Assert.Equal(updates.Count, result.Count);
        foreach (var (coord, expectedValue) in updates)
        {
            Assert.Equal(expectedValue, result[coord]);
        }
    }

    [Fact]
    public async Task BatchOperations_PerformanceComparison()
    {
        // Arrange
        var spreadsheetId = "performance-test-spreadsheet";
        var sheetName = "Budget";
        
        var coordinates = new List<(int row, int column)>();
        for (int i = 1; i <= 50; i++)
        {
            coordinates.Add((i, 3)); // Column C, rows 1-50
        }

        var testData = coordinates.ToDictionary(coord => coord, _ => 100.00m);
        _mockService.SetupMockSpreadsheet(spreadsheetId, sheetName, testData);

        // Act & Time batch operation
        var batchStart = DateTime.UtcNow;
        var batchResult = await _mockService.BatchGetCellValuesAsync(spreadsheetId, sheetName, coordinates);
        var batchEnd = DateTime.UtcNow;

        // Act & Time individual operations
        var individualStart = DateTime.UtcNow;
        var individualResults = new Dictionary<(int row, int column), decimal>();
        foreach (var coord in coordinates)
        {
            individualResults[coord] = await _mockService.GetCellValueAsync(spreadsheetId, sheetName, coord.row, coord.column);
        }
        var individualEnd = DateTime.UtcNow;

        // Assert
        Assert.Equal(coordinates.Count, batchResult.Count);
        Assert.Equal(coordinates.Count, individualResults.Count);
        
        // Batch should be faster (simulated delays make this obvious)
        var batchDuration = batchEnd - batchStart;
        var individualDuration = individualEnd - individualStart;
        
        // Log performance for visibility
        _logger.LogInformation("Batch operation took: {BatchMs}ms", batchDuration.TotalMilliseconds);
        _logger.LogInformation("Individual operations took: {IndividualMs}ms", individualDuration.TotalMilliseconds);
        
        // In real scenarios with actual API calls, batch should be significantly faster
        Assert.True(batchDuration < individualDuration, 
            $"Batch operation ({batchDuration.TotalMilliseconds}ms) should be faster than individual operations ({individualDuration.TotalMilliseconds}ms)");
    }

    [Fact]
    public async Task GetCellValueAsync_HandlesDecimalFormatting()
    {
        // Arrange
        var spreadsheetId = "decimal-test-spreadsheet";
        var sheetName = "Budget";
        var row = 5;
        var column = 3;

        // Test various decimal values
        var testValues = new[] { 0m, 1.5m, 123.45m, 1000.99m, -250.75m };

        foreach (var expectedValue in testValues)
        {
            // Act
            await _mockService.UpdateCellAsync(spreadsheetId, sheetName, row, column, expectedValue);
            var actualValue = await _mockService.GetCellValueAsync(spreadsheetId, sheetName, row, column);

            // Assert
            Assert.Equal(expectedValue, actualValue);
        }
    }

    [Fact]
    public async Task SimulateApiError_ThrowsExpectedException()
    {
        // Arrange
        var spreadsheetId = "error-test-spreadsheet";
        var sheetName = "Budget";
        var errorMessage = "Simulated API quota exceeded";

        _mockService.SimulateApiError(spreadsheetId, sheetName, errorMessage);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockService.UpdateCellAsync(spreadsheetId, sheetName, 1, 1, 100m));
        
        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public async Task SetupMockSpreadsheet_InitializesDataCorrectly()
    {
        // Arrange
        var spreadsheetId = "setup-test-spreadsheet";
        var sheetName = "Budget";
        
        var initialData = new Dictionary<(int row, int column), decimal>
        {
            { (2, 3), 500.00m },  // Income row
            { (16, 3), 12.99m },  // Spotify subscription
            { (25, 3), 450.50m }  // Food expenses
        };

        // Act
        _mockService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialData);

        // Assert
        var allCells = _mockService.GetAllCellValues(spreadsheetId, sheetName);
        
        Assert.Equal(initialData.Count, allCells.Count);
        foreach (var (coord, expectedValue) in initialData)
        {
            Assert.True(allCells.ContainsKey(coord));
            Assert.Equal(expectedValue, allCells[coord]);
        }
    }

    [Theory]
    [InlineData(1, 1, "A1")]
    [InlineData(1, 26, "Z1")]
    [InlineData(1, 27, "AA1")]
    [InlineData(5, 3, "C5")]
    [InlineData(100, 52, "AZ100")]
    public void ColumnIndexToLetter_ReturnsCorrectNotation(int row, int column, string expectedRange)
    {
        // This tests the SheetStructure.IndexToColumnLetter method indirectly
        // by verifying the column notation works as expected in our mock
        
        // Arrange & Act
        var columnLetter = SheetStructure.IndexToColumnLetter(column);
        var actualRange = $"{columnLetter}{row}";

        // Assert
        Assert.Equal(expectedRange, actualRange);
    }

    private static ILogger<T> CreateMockLogger<T>()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILogger<T>>();
    }
}