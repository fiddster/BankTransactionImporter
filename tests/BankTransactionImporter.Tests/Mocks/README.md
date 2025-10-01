# Google Sheets API Mock Implementation

This directory contains a comprehensive mock implementation of the Google Sheets API for testing purposes.

## Overview

The `MockGoogleSheetsService` provides a full in-memory implementation of the `IGoogleSheetsService` interface, allowing you to test your application logic without making actual API calls to Google Sheets.

## Features

### Core Functionality
- **Sheet Structure Loading**: Returns the same mock structure as the real service
- **Cell Operations**: Read and write individual cells with proper formatting
- **Batch Operations**: Efficient batch reading and writing of multiple cells
- **Error Simulation**: Simulate API errors for testing error handling

### Key Benefits
- **No API Dependencies**: Run tests without internet connection or API credentials
- **Fast Execution**: In-memory operations are much faster than network calls
- **Predictable Results**: Consistent behavior for reliable testing
- **Full Coverage**: Supports all methods from the `IGoogleSheetsService` interface

## Usage Examples

### Basic Setup
```csharp
var mockLogger = new Mock<ILogger<MockGoogleSheetsService>>();
var mockService = new MockGoogleSheetsService(mockLogger.Object);

// Setup initial spreadsheet data
var spreadsheetId = "test-spreadsheet-id";
var sheetName = "Budget";
var initialData = new Dictionary<(int row, int column), decimal>
{
    { (2, 3), 5000.00m },  // Income
    { (16, 3), 109.00m },  // Spotify
    { (25, 3), 450.75m }   // Food
};
mockService.SetupMockSpreadsheet(spreadsheetId, sheetName, initialData);
```

### Individual Operations
```csharp
// Read a cell
var value = await mockService.GetCellValueAsync(spreadsheetId, sheetName, 2, 3);

// Write a cell
await mockService.UpdateCellAsync(spreadsheetId, sheetName, 16, 3, 109.00m);
```

### Batch Operations
```csharp
// Batch read
var coordinates = new List<(int row, int column)> { (2, 3), (16, 3), (25, 3) };
var values = await mockService.BatchGetCellValuesAsync(spreadsheetId, sheetName, coordinates);

// Batch write
var updates = new Dictionary<(int row, int column), decimal>
{
    { (2, 3), 5500.00m },
    { (16, 3), 109.00m }
};
await mockService.BatchUpdateCellsAsync(spreadsheetId, sheetName, updates);
```

### Error Simulation
```csharp
// Simulate API quota exceeded error
mockService.SimulateApiError(spreadsheetId, sheetName, "Quota exceeded");

// Now any operation will throw an exception
var exception = await Assert.ThrowsAsync<InvalidOperationException>(
    () => mockService.UpdateCellAsync(spreadsheetId, sheetName, 1, 1, 100m)
);
```

## Implementation Details

### Data Storage
- Uses in-memory `Dictionary` structures to store spreadsheet data
- Each spreadsheet contains multiple sheets
- Each sheet contains cells indexed by (row, column) coordinates
- Values are stored as formatted strings with 2 decimal places precision

### Performance Simulation
- Includes realistic delays to simulate network operations:
  - Individual operations: 5-10ms delay
  - Batch operations: 20-25ms delay
  - Sheet structure loading: 10ms delay

### Error Handling
- Supports error simulation per spreadsheet
- Throws `InvalidOperationException` when errors are simulated
- Graceful handling of non-existent spreadsheets/sheets (returns 0 values)

## Testing Integration

The mock service integrates seamlessly with your existing test infrastructure:

1. **Unit Tests**: Test individual service methods in isolation
2. **Integration Tests**: Test complete workflows with transaction mapping
3. **Performance Tests**: Measure batch vs individual operation efficiency
4. **Error Handling Tests**: Verify proper error handling and recovery

## File Structure

```
Mocks/
├── MockGoogleSheetsService.cs  # Main mock implementation
└── README.md                   # This documentation

Tests/
├── GoogleSheetsServiceTests.cs              # Unit tests for mock service
├── IntegrationTests/
│   └── TransactionMappingIntegrationTests.cs  # Integration tests
└── MockUsageExample.cs                      # Usage examples
```

## Benefits for Development

1. **Faster Development**: No need to set up Google API credentials during development
2. **Reliable Testing**: Tests don't fail due to network issues or API quotas
3. **Edge Case Testing**: Easy to test error conditions and edge cases
4. **CI/CD Friendly**: Tests run in any environment without external dependencies
5. **Performance Insights**: Compare batch vs individual operation performance

## Next Steps

When ready to integrate with the real Google Sheets API:

1. Replace the mock service with the real `GoogleSheetsService`
2. Add proper authentication configuration
3. Update the real service to remove TODO comments and implement actual API calls
4. Keep the mock for testing purposes

The mock service provides the same interface, so the transition should be seamless.