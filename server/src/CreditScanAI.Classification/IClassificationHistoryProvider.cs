namespace CreditScanAI.Classification;

/// <summary>Uma classificação anterior, já validada por um humano (Approved/Overridden), da mesma empresa.</summary>
public sealed record HistoricalClassification(Guid StandardAccountId, string StandardAccountName, DateTime DecidedAt);

/// <summary>
/// Camada 4 (Histórico): busca se esta mesma empresa já teve uma
/// classificação humanamente validada para o mesmo nome normalizado de
/// conta, em outro documento. Implementado na camada Api (precisa de
/// acesso ao banco) - este projeto permanece agnóstico de EF/banco.
/// </summary>
public interface IClassificationHistoryProvider
{
    Task<HistoricalClassification?> FindPreviousDecisionAsync(
        Guid companyId,
        Guid excludeDocumentId,
        string normalizedSourceAccountName,
        CancellationToken cancellationToken);
}
