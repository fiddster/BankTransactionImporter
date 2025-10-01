using BankTransactionImporter.Models;
using BankTransactionImporter.Services;
using BankTransactionImporter.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankTransactionImporter.Tests;

/// <summary>
/// Example demonstrating how to use the MockGoogleSheetsService for testing
/// </summary>
public class MockUsageExample
{
    [Fact]
    public async Task ExampleUsage_MockGoogleSheetsService()
    {
        // Arrange - Create logger and service
        var logger = CreateMockLogger<MockGoogleSheetsService>();
        var mockGoogleSheetsService = new MockGoogleSheetsService(logger);

        // Setup a mock spreadsheet with initial data
        var spreadsheetId = "1abc123_example_spreadsheet_id";
        var sheetName = "Budget";
        
        var initialBudgetData = new Dictionary<(int row, int column), decimal>
        {
            { (2, 3), 5000.00m },   // Initial income
            { (16, 3), 0.00m },     // Spotify subscription
            { (25, 3), 1500.00m }   // Food budget
        };
        
        mockGoogleSheetsService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialBudgetData);

        // Act - Use the service as you would the real GoogleSheetsService
        
        // 1. Load sheet structure
        var sheetStructure = await mockGoogleSheetsService.LoadSheetStructureAsync(spreadsheetId, sheetName);
        
        // 2. Read current values
        var currentIncome = await mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, 2, 3);
        var currentFoodBudget = await mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, 25, 3);
        
        // 3. Update some cells
        await mockGoogleSheetsService.UpdateCellAsync(spreadsheetId, sheetName, 16, 3, 109.00m); // Spotify cost
        
        // 4. Batch operations
        var cellsToRead = new List<(int row, int column)> { (2, 3), (16, 3), (25, 3) };
        var batchValues = await mockGoogleSheetsService.BatchGetCellValuesAsync(spreadsheetId, sheetName, cellsToRead);
        
        // Assert - Verify the mock behaves as expected
        Assert.NotNull(sheetStructure);
        Assert.NotEmpty(sheetStructure.Categories);
        
        Assert.Equal(5000.00m, currentIncome);
        Assert.Equal(1500.00m, currentFoodBudget);
        
        // Verify Spotify was updated
        var updatedSpotify = await mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, 16, 3);
        Assert.Equal(109.00m, updatedSpotify);
        
        // Verify batch operations
        Assert.Equal(3, batchValues.Count);
        Assert.Equal(5000.00m, batchValues[(2, 3)]);
        Assert.Equal(109.00m, batchValues[(16, 3)]);
        Assert.Equal(1500.00m, batchValues[(25, 3)]);
    }

    [Fact]
    public async Task ExampleUsage_ErrorSimulation()
    {
        // Arrange
        var logger = CreateMockLogger<MockGoogleSheetsService>();
        var mockGoogleSheetsService = new MockGoogleSheetsService(logger);

        var spreadsheetId = "error-test-spreadsheet";
        var sheetName = "Budget";

        // Simulate an API error
        mockGoogleSheetsService.SimulateApiError(spreadsheetId, sheetName, "Quota exceeded");

        // Act & Assert - Verify error is thrown
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mockGoogleSheetsService.UpdateCellAsync(spreadsheetId, sheetName, 1, 1, 100m)
        );

        Assert.Contains("Quota exceeded", exception.Message);
    }

    [Fact]
    public async Task ExampleUsage_PerformanceTesting()
    {
        // Arrange
        var logger = CreateMockLogger<MockGoogleSheetsService>();
        var mockGoogleSheetsService = new MockGoogleSheetsService(logger);

        var spreadsheetId = "performance-test";
        var sheetName = "Budget";

        // Act - Measure batch vs individual operations
        var coordinates = new List<(int row, int column)>();
        for (int i = 1; i <= 20; i++)
        {
            coordinates.Add((i, 3));
        }

        // Time batch operation
        var batchStart = DateTime.UtcNow;
        await mockGoogleSheetsService.BatchGetCellValuesAsync(spreadsheetId, sheetName, coordinates);
        var batchTime = DateTime.UtcNow - batchStart;

        // Time individual operations
        var individualStart = DateTime.UtcNow;
        foreach (var (row, column) in coordinates)
        {
            await mockGoogleSheetsService.GetCellValueAsync(spreadsheetId, sheetName, row, column);
        }
        var individualTime = DateTime.UtcNow - individualStart;

        // Assert - Batch should be faster
        Assert.True(batchTime < individualTime, 
            $"Batch operation ({batchTime.TotalMilliseconds}ms) should be faster than individual operations ({individualTime.TotalMilliseconds}ms)");
    }

    private static ILogger<T> CreateMockLogger<T>()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILogger<T>>();
    }
}