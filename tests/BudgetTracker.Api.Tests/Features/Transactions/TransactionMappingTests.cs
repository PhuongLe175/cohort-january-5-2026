using BudgetTracker.Api.Features.Transactions;

namespace BudgetTracker.Api.Tests.Features.Transactions;

public class TransactionMappingTests
{
    [Fact]
    public void Should_map_all_properties_correctly()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Date = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Description = "Test Transaction",
            Amount = 100.50m,
            Balance = 500.00m,
            Category = "Food",
            Labels = "lunch,work",
            ImportedAt = new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Utc),
            Account = "Checking",
            UserId = "user123"
        };

        var dto = transaction.MapToDto();

        Assert.Equal(transaction.Id, dto.Id);
        Assert.Equal(transaction.Date, dto.Date);
        Assert.Equal(transaction.Description, dto.Description);
        Assert.Equal(transaction.Amount, dto.Amount);
        Assert.Equal(transaction.Balance, dto.Balance);
        Assert.Equal(transaction.Category, dto.Category);
        Assert.Equal(transaction.Labels, dto.Labels);
        Assert.Equal(transaction.ImportedAt, dto.ImportedAt);
        Assert.Equal(transaction.Account, dto.Account);
    }

    [Fact]
    public void Should_handle_nullable_fields()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Description = "Transaction without optional fields",
            Amount = 50.00m,
            Balance = null,
            Category = null,
            Labels = null,
            ImportedAt = DateTime.UtcNow,
            Account = "Savings",
            UserId = "user456"
        };

        var dto = transaction.MapToDto();

        Assert.Null(dto.Balance);
        Assert.Null(dto.Category);
        Assert.Null(dto.Labels);
    }
}
