using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Api.Features.Intelligence.Recommendations;

public enum RecommendationType
{
    SpendingAlert,
    SavingsOpportunity,
    BudgetTip,
    TrendInsight,
    CategoryOptimization
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum RecommendationStatus
{
    Active,
    Dismissed,
    Expired
}

public class Recommendation
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public RecommendationType Type { get; set; }

    public RecommendationPriority Priority { get; set; }

    public RecommendationStatus Status { get; set; } = RecommendationStatus.Active;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTime? ExpiresAt { get; set; }
}

public class RecommendationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationType Type { get; set; }
    public RecommendationPriority Priority { get; set; }
    public decimal? Amount { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public static class RecommendationExtensions
{
    public static RecommendationDto MapToDto(this Recommendation recommendation) =>
        new()
        {
            Id = recommendation.Id,
            Title = recommendation.Title,
            Description = recommendation.Description,
            Type = recommendation.Type,
            Priority = recommendation.Priority,
            Amount = recommendation.Amount,
            Category = recommendation.Category,
            CreatedAt = recommendation.CreatedAt,
            ExpiresAt = recommendation.ExpiresAt
        };
}
