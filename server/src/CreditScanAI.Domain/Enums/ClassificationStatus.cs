namespace CreditScanAI.Domain.Enums;

/// <summary>Document-level status: whether the classification pass has run.</summary>
public enum ClassificationStatus
{
    NotStarted,
    AwaitingDefaultChartOfAccounts,
    Processing,
    Completed,
    Failed
}
