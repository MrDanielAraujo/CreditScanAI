namespace CreditScanAI.Domain.Enums;

public enum DocumentType
{
    BalanceSheet,
    IncomeStatement,
    /// <summary>Um único arquivo com Balanço e DRE juntos - detectado quando a
    /// classificação por palavra-chave encontra contas dos dois grupos.</summary>
    Mixed
}
