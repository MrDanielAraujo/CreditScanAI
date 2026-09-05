namespace CreditScanAI.Domain.Enums;

/// <summary>Per-account review status: what a human has (or hasn't) done with this suggestion.</summary>
public enum ClassificationReviewStatus
{
    Pending,
    NeedsReview,
    Approved,
    Rejected,
    Overridden
}
