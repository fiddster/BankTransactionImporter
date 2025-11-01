using BankTransactionImporter.Models;

namespace BankTransactionImporter.Tests;

public class TransactionTests
{
    [Fact]
    public void MappingKey_WithReference_ShouldReturnTrimmedUpperReference()
    {
        // Arrange
        var transaction = new Transaction
        {
            Reference = "  Tele2  ",
            Description = "Monthly phone bill"
        };

        // Act
        var mappingKey = transaction.MappingKey;

        // Assert
        Assert.Equal("TELE2", mappingKey);
    }

    [Fact]
    public void MappingKey_WithEmptyReference_ShouldReturnTrimmedUpperDescription()
    {
        // Arrange
        var transaction = new Transaction
        {
            Reference = "",
            Description = "  Netflix Subscription  "
        };

        // Act
        var mappingKey = transaction.MappingKey;

        // Assert
        Assert.Equal("NETFLIX SUBSCRIPTION", mappingKey);
    }

    [Fact]
    public void IsIncome_WithPositiveAmount_ShouldReturnTrue()
    {
        // Arrange
        var transaction = new Transaction { Amount = 1000.50m };

        // Act & Assert
        Assert.True(transaction.IsIncome);
        Assert.False(transaction.IsExpense);
    }

    [Fact]
    public void IsExpense_WithNegativeAmount_ShouldReturnTrue()
    {
        // Arrange
        var transaction = new Transaction { Amount = -250.75m };

        // Act & Assert
        Assert.True(transaction.IsExpense);
        Assert.False(transaction.IsIncome);
    }

    [Fact]
    public void AbsoluteAmount_ShouldReturnPositiveValue()
    {
        // Arrange
        var negativeTransaction = new Transaction { Amount = -100.00m };
        var positiveTransaction = new Transaction { Amount = 100.00m };

        // Act & Assert
        Assert.Equal(100.00m, negativeTransaction.AbsoluteAmount);
        Assert.Equal(100.00m, positiveTransaction.AbsoluteAmount);
    }

    [Fact]
    public void Year_ShouldReturnBookingDateYear()
    {
        // Arrange
        var transaction = new Transaction
        {
            BookingDate = new DateTime(2025, 10, 1)
        };

        // Act & Assert
        Assert.Equal(2025, transaction.Year);
    }

    [Fact]
    public void Month_ShouldReturnBookingDateMonth()
    {
        // Arrange
        var transaction = new Transaction
        {
            BookingDate = new DateTime(2025, 10, 1)
        };

        // Act & Assert
        Assert.Equal(10, transaction.Month);
    }
}
